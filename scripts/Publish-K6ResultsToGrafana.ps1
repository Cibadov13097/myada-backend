# Publishes k6 summary metrics to Prometheus Pushgateway (for Grafana).
# Prerequisites: docker compose up -d pushgateway prometheus grafana
#
# Usage examples:
#   powershell -File .\scripts\Publish-K6ResultsToGrafana.ps1 -ScriptPath .\load-tests\k6\club\load.js
#   powershell -File .\scripts\Publish-K6ResultsToGrafana.ps1 -ScriptPath .\load-tests\k6\attendance\stress.js -JobName attendance_stress
#
# Env:
#   PUSHGATEWAY_URL (default http://localhost:9091)

param(
  [Parameter(Mandatory = $true)]
  [string] $ScriptPath,
  [string] $JobName = "k6",
  [string] $Instance = "last"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Join-Path $PSScriptRoot ".." | Resolve-Path).Path
$pushUrl = if ($env:PUSHGATEWAY_URL) { $env:PUSHGATEWAY_URL.TrimEnd("/") } else { "http://localhost:9091" }

function Sanitize-Label([string] $s) {
  if ([string]::IsNullOrEmpty($s)) { return "unknown" }
  $t = $s -replace '[^a-zA-Z0-9_]', '_'
  if ($t.Length -eq 0) { $t = "a" }
  if ($t[0] -notmatch '[a-zA-Z_]') { $t = "a" + $t }
  if ($t.Length -gt 200) { $t = $t.Substring(0, 200) }
  $t
}

function Get-MetricStat([object] $metrics, [string] $name, [string] $stat) {
  if (-not $metrics.ContainsKey($name)) { return $null }
  $m = $metrics[$name]
  if (-not $m -or -not $m.ContainsKey("values")) { return $null }
  $v = $m["values"]
  if (-not $v -or -not $v.ContainsKey($stat)) { return $null }
  return $v[$stat]
}

$outDir = Join-Path $repoRoot ".artifacts\k6"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$summaryPath = Join-Path $outDir ("k6-summary-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".json")

Write-Host "Running k6: $ScriptPath" -ForegroundColor Cyan
& k6 run --summary-export $summaryPath $ScriptPath

if (-not (Test-Path $summaryPath)) {
  throw "k6 summary file not found: $summaryPath"
}

$json = Get-Content -Raw -Path $summaryPath | ConvertFrom-Json
if (-not $json.metrics) { throw "k6 summary missing metrics." }

# Pull key metrics (k6 summary-export uses these common names).
$metrics = @{}
foreach ($p in $json.metrics.PSObject.Properties) { $metrics[$p.Name] = $p.Value }

$p95 = Get-MetricStat $metrics "http_req_duration" "p(95)"
$avg = Get-MetricStat $metrics "http_req_duration" "avg"
$max = Get-MetricStat $metrics "http_req_duration" "max"
$failRate = Get-MetricStat $metrics "http_req_failed" "rate"
$checksRate = Get-MetricStat $metrics "checks" "rate"
$reqs = Get-MetricStat $metrics "http_reqs" "count"
$rps = Get-MetricStat $metrics "http_reqs" "rate"

$ts = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$job = Sanitize-Label $JobName
$inst = Sanitize-Label $Instance

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("# TYPE myada_k6_run_last_unix gauge")
[void]$sb.AppendLine("myada_k6_run_last_unix $ts")

if ($null -ne $p95) { [void]$sb.AppendLine("# TYPE myada_k6_http_req_duration_p95_ms gauge"); [void]$sb.AppendLine("myada_k6_http_req_duration_p95_ms $p95") }
if ($null -ne $avg) { [void]$sb.AppendLine("# TYPE myada_k6_http_req_duration_avg_ms gauge"); [void]$sb.AppendLine("myada_k6_http_req_duration_avg_ms $avg") }
if ($null -ne $max) { [void]$sb.AppendLine("# TYPE myada_k6_http_req_duration_max_ms gauge"); [void]$sb.AppendLine("myada_k6_http_req_duration_max_ms $max") }
if ($null -ne $failRate) { [void]$sb.AppendLine("# TYPE myada_k6_http_req_failed_rate gauge"); [void]$sb.AppendLine("myada_k6_http_req_failed_rate $failRate") }
if ($null -ne $checksRate) { [void]$sb.AppendLine("# TYPE myada_k6_checks_rate gauge"); [void]$sb.AppendLine("myada_k6_checks_rate $checksRate") }
if ($null -ne $reqs) { [void]$sb.AppendLine("# TYPE myada_k6_http_reqs_total gauge"); [void]$sb.AppendLine("myada_k6_http_reqs_total $reqs") }
if ($null -ne $rps) { [void]$sb.AppendLine("# TYPE myada_k6_http_reqs_per_s gauge"); [void]$sb.AppendLine("myada_k6_http_reqs_per_s $rps") }

$putUrl = "$pushUrl/metrics/job/$job/instance/$inst"
Write-Host "Pushing metrics to $putUrl" -ForegroundColor Cyan

# Prometheus text format must use \n only (Windows default \r\n breaks Pushgateway parsing).
$pushText = $sb.ToString() -replace "`r`n", "`n" -replace "`r", "`n"
if (-not $pushText.EndsWith("`n")) { $pushText += "`n" }
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$pushBytes = $utf8NoBom.GetBytes($pushText)
Invoke-RestMethod -Uri $putUrl -Method Put -ContentType "text/plain; version=0.0.4; charset=utf-8" -Body $pushBytes

Write-Host "Done. Open Grafana: http://localhost:3001" -ForegroundColor Green

