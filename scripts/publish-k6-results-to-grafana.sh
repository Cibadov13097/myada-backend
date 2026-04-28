#!/usr/bin/env bash
# Publishes k6 summary metrics to Prometheus Pushgateway (for Grafana).
# Linux / macOS. Requires: k6, curl, python3 (stdlib only).
#
# Usage:
#   export BASE_URL="https://your-gateway.example.com"
#   export ADMIN_USERNAME=... ADMIN_PASSWORD=...
#   export STUDENT_USERNAMES="u1,u2" STUDENT_PASSWORDS="p1,p2"
#   ./scripts/publish-k6-results-to-grafana.sh load-tests/k6/club/load.js club_load
#
# Env:
#   PUSHGATEWAY_URL  (default http://localhost:9091)

set -euo pipefail

SCRIPT_PATH="${1:?Usage: $0 <path-to-k6-script.js> [job_name] [instance]}"
JOB_NAME="${2:-k6}"
INSTANCE="${3:-last}"
PUSHGATEWAY_URL="${PUSHGATEWAY_URL:-http://localhost:9091}"
PUSHGATEWAY_URL="${PUSHGATEWAY_URL%/}"

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT_DIR="${REPO_ROOT}/.artifacts/k6"
mkdir -p "${OUT_DIR}"
SUMMARY="${OUT_DIR}/k6-summary-$(date -u +%Y%m%d-%H%M%S).json"

if ! command -v k6 >/dev/null 2>&1; then
  echo "k6 not found. Install: https://grafana.com/docs/k6/latest/set-up/install-k6/" >&2
  exit 1
fi
if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required to parse k6 summary JSON." >&2
  exit 1
fi
if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required to push metrics." >&2
  exit 1
fi

echo "Running k6: ${SCRIPT_PATH}"
k6 run --summary-export "${SUMMARY}" "${SCRIPT_PATH}"

METRICS_TEXT="$(python3 - "${SUMMARY}" <<'PY'
import json, sys, time

path = sys.argv[1]
with open(path, encoding="utf-8") as f:
    data = json.load(f)

metrics = data.get("metrics") or {}

def stat(name, key):
    m = metrics.get(name) or {}
    vals = m.get("values") or {}
    v = vals.get(key)
    return v

p95 = stat("http_req_duration", "p(95)")
avg = stat("http_req_duration", "avg")
mx = stat("http_req_duration", "max")
fail_rate = stat("http_req_failed", "rate")
checks_rate = stat("checks", "rate")
reqs = stat("http_reqs", "count")
rps = stat("http_reqs", "rate")

ts = int(time.time())
lines = [
    "# TYPE myada_k6_run_last_unix gauge",
    f"myada_k6_run_last_unix {ts}",
]
if p95 is not None:
    lines += ["# TYPE myada_k6_http_req_duration_p95_ms gauge", f"myada_k6_http_req_duration_p95_ms {p95}"]
if avg is not None:
    lines += ["# TYPE myada_k6_http_req_duration_avg_ms gauge", f"myada_k6_http_req_duration_avg_ms {avg}"]
if mx is not None:
    lines += ["# TYPE myada_k6_http_req_duration_max_ms gauge", f"myada_k6_http_req_duration_max_ms {mx}"]
if fail_rate is not None:
    lines += ["# TYPE myada_k6_http_req_failed_rate gauge", f"myada_k6_http_req_failed_rate {fail_rate}"]
if checks_rate is not None:
    lines += ["# TYPE myada_k6_checks_rate gauge", f"myada_k6_checks_rate {checks_rate}"]
if reqs is not None:
    lines += ["# TYPE myada_k6_http_reqs_total gauge", f"myada_k6_http_reqs_total {reqs}"]
if rps is not None:
    lines += ["# TYPE myada_k6_http_reqs_per_s gauge", f"myada_k6_http_reqs_per_s {rps}"]

sys.stdout.write("\n".join(lines) + "\n")
PY
)"

sanitize() {
  local s="$1"
  s="$(echo -n "$s" | sed 's/[^a-zA-Z0-9_]/_/g')"
  if [[ -z "$s" ]]; then s="unknown"; fi
  if [[ ! "$s" =~ ^[a-zA-Z_] ]]; then s="a${s}"; fi
  echo -n "$s" | cut -c1-200
}

JOB_SAFE="$(sanitize "$JOB_NAME")"
INST_SAFE="$(sanitize "$INSTANCE")"
PUT_URL="${PUSHGATEWAY_URL}/metrics/job/${JOB_SAFE}/instance/${INST_SAFE}"

echo "Pushing metrics to ${PUT_URL}"
printf '%s' "${METRICS_TEXT}" | curl -fsS -X PUT \
  --data-binary @- \
  -H "Content-Type: text/plain; version=0.0.4; charset=utf-8" \
  "${PUT_URL}"

echo ""
echo "Done. Summary JSON: ${SUMMARY}"
