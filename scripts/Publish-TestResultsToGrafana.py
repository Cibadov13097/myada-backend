#!/usr/bin/env python3
r"""Publish MyAda test results to Prometheus Pushgateway and Loki.

Linux/macOS usage:
    python3 ./scripts/Publish-TestResultsToGrafana.py

Windows usage:
    py .\scripts\Publish-TestResultsToGrafana.py

Environment variables:
    PUSHGATEWAY_URL  default http://localhost:9091
    LOKI_URL         default http://localhost:3100
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def sanitize_label(value: str) -> str:
    if not value:
        return "unknown"

    chars = []
    for char in value:
        chars.append(char if (char.isalnum() or char == "_") else "_")

    sanitized = "".join(chars) or "a"
    if not (sanitized[0].isalpha() or sanitized[0] == "_"):
        sanitized = f"a{sanitized}"
    return sanitized[:200]


def parse_counter(element: ET.Element | None, names: tuple[str, ...]) -> int:
    if element is None:
        return 0

    for name in names:
        value = element.attrib.get(name)
        if value:
            try:
                return int(value)
            except ValueError:
                return 0
    return 0


def push_request(url: str, method: str, content_type: str, body: bytes) -> None:
    request = urllib.request.Request(url, data=body, method=method)
    request.add_header("Content-Type", content_type)
    with urllib.request.urlopen(request, timeout=30) as response:
        response.read()


def main() -> int:
    repo_root = Path(__file__).resolve().parent.parent
    push_url = os.environ.get("PUSHGATEWAY_URL", "http://localhost:9091").rstrip("/")
    loki_url = os.environ.get("LOKI_URL", "http://localhost:3100").rstrip("/")
    out_dir = repo_root / ".artifacts" / "test-results"
    out_dir.mkdir(parents=True, exist_ok=True)

    print(f"Running dotnet test - results: {out_dir}")
    test_command = [
        "dotnet",
        "test",
        str(repo_root / "MyAda.sln"),
        "--results-directory",
        str(out_dir),
        "--logger",
        "trx",
        "--nologo",
    ]

    try:
        test_result = subprocess.run(test_command, check=False, cwd=repo_root)
        if test_result.returncode != 0:
            print(
                f"Warning: dotnet test exited with code {test_result.returncode}. "
                "Continuing so results can still be published.",
                file=sys.stderr,
            )
    except FileNotFoundError:
        print("Error: 'dotnet' was not found on PATH.", file=sys.stderr)
        return 1

    trx_files = sorted(out_dir.rglob("*.trx"))
    if not trx_files:
        print("Warning: No .trx files found. Did tests run?", file=sys.stderr)

    totals = {
        "Passed": 0,
        "Failed": 0,
        "NotExecuted": 0,
        "Inconclusive": 0,
    }
    by_assembly: dict[str, dict[str, int]] = {}
    failed_test_names: list[str] = []

    def ensure_assembly(key: str) -> dict[str, int]:
        return by_assembly.setdefault(
            key,
            {"Passed": 0, "Failed": 0, "NotExecuted": 0, "Inconclusive": 0},
        )

    for trx_file in trx_files:
        assembly_key = trx_file.stem
        assembly = ensure_assembly(assembly_key)

        try:
            root = ET.fromstring(trx_file.read_text(encoding="utf-8-sig"))
        except Exception as exc:  # noqa: BLE001
            print(f"Warning: failed to parse {trx_file}: {exc}", file=sys.stderr)
            continue

        result_nodes = [
            node for node in root.iter() if local_name(node.tag) == "UnitTestResult"
        ]

        if result_nodes:
            for node in result_nodes:
                outcome = node.attrib.get("outcome", "")
                test_name = node.attrib.get("testName", "")
                if outcome in totals:
                    totals[outcome] += 1
                    assembly[outcome] += 1
                    if outcome == "Failed" and test_name:
                        failed_test_names.append(test_name)
        else:
            counters_node = None
            for node in root.iter():
                if local_name(node.tag) == "Counters":
                    counters_node = node
                    break

            passed = parse_counter(counters_node, ("passed", "Passed"))
            failed = parse_counter(counters_node, ("failed", "Failed"))
            not_executed = parse_counter(
                counters_node, ("notexecuted", "notExecuted", "NotExecuted")
            )
            inconclusive = parse_counter(counters_node, ("inconclusive", "Inconclusive"))

            totals["Passed"] += passed
            totals["Failed"] += failed
            totals["NotExecuted"] += not_executed
            totals["Inconclusive"] += inconclusive
            assembly["Passed"] = passed
            assembly["Failed"] = failed
            assembly["NotExecuted"] = not_executed
            assembly["Inconclusive"] = inconclusive

    unix_seconds = int(time.time())
    metric_lines = [
        "# TYPE myada_test_outcome_last gauge",
        f'myada_test_outcome_last{{outcome="Passed"}} {totals["Passed"]}',
        f'myada_test_outcome_last{{outcome="Failed"}} {totals["Failed"]}',
        f'myada_test_outcome_last{{outcome="NotExecuted"}} {totals["NotExecuted"]}',
        f'myada_test_outcome_last{{outcome="Inconclusive"}} {totals["Inconclusive"]}',
        "# TYPE myada_test_run_last_unix gauge",
        f"myada_test_run_last_unix {unix_seconds}",
        "# TYPE myada_test_by_assembly_outcome gauge",
    ]

    for assembly_name, counts in by_assembly.items():
        label = sanitize_label(assembly_name)
        for outcome, count in counts.items():
            if count > 0:
                metric_lines.append(
                    f'myada_test_by_assembly_outcome{{assembly="{label}",outcome="{outcome}"}} {count}'
                )

    push_body = ("\n".join(metric_lines) + "\n").encode("utf-8")
    put_url = f"{push_url}/metrics/job/dotnet_test/instance/last"
    print(f"Pushing metrics to {put_url}")
    try:
        push_request(
            put_url,
            "PUT",
            "text/plain; version=0.0.4; charset=utf-8",
            push_body,
        )
    except urllib.error.URLError as exc:
        print(f"Warning: Pushgateway failed. Is it running? {exc}", file=sys.stderr)

    loki_path = f"{loki_url}/loki/api/v1/push"
    nanoseconds = str(time.time_ns())
    fail_text = (
        " | failed_tests: " + " | ".join(failed_test_names)
        if failed_test_names
        else ""
    )
    max_len = 240 * 1024
    if len(fail_text) > max_len:
        fail_text = fail_text[:max_len] + "..."

    line = (
        f"trx={len(trx_files)} P={totals['Passed']} F={totals['Failed']} "
        f"N={totals['NotExecuted']} I={totals['Inconclusive']} unix={unix_seconds}{fail_text}"
    ).lstrip("\ufeff")
    loki_body = json.dumps(
        {
            "streams": [
                {
                    "stream": {"job": "myada_test_results"},
                    "values": [[nanoseconds, line]],
                }
            ]
        },
        separators=(",", ":"),
    ).encode("utf-8")

    try:
        print(f"Pushing test run summary to Loki ({loki_path})")
        push_request(loki_path, "POST", "application/json; charset=utf-8", loki_body)
    except urllib.error.URLError as exc:
        print(f"Warning: Loki push failed (is Loki up?). {exc}", file=sys.stderr)

    print("Done. Open Grafana: http://localhost:3000 -> dashboard MyADA - Logs and test results")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
