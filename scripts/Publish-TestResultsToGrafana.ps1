# Publishes MyAda.sln test run results to Prometheus Pushgateway and Loki (for Grafana).
# Prerequisites: docker compose up -d pushgateway loki
#
# Usage:  powershell -File .\scripts\Publish-TestResultsToGrafana.ps1
#           (or: pwsh -File ...  if you installed PowerShell 7+)
# Env:   PUSHGATEWAY_URL (default http://localhost:9091)  LOKI_URL (default http://localhost:3100)

$ErrorActionPreference = "Stop"
$repoRoot = (Join-Path $PSScriptRoot ".." | Resolve-Path).Path
$pushUrl = if ($env:PUSHGATEWAY_URL) { $env:PUSHGATEWAY_URL.TrimEnd("/") } else { "http://localhost:9091" }
$lokiUrl = if ($env:LOKI_URL)        { $env:LOKI_URL.TrimEnd("/") }        else { "http://localhost:3100" }
$outDir  = Join-Path $repoRoot ".artifacts/test-results"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Write-Host "Running dotnet test - results: $outDir" -ForegroundColor Cyan
$trxFiles = [System.Collections.Generic.List[string]]::new()
$null = & dotnet test (Join-Path $repoRoot "MyAda.sln") @("--results-directory", $outDir, "--logger", "trx", "--nologo")
Get-ChildItem -Path $outDir -Filter "*.trx" -Recurse -File | ForEach-Object { $trxFiles.Add($_.FullName) }

$passed = 0; $failed = 0; $notExecuted = 0; $inconclusive = 0
$byAssembly = @{}
$failNames = [System.Collections.Generic.List[string]]::new()

function Ensure-Assembly([string] $key) {
  if (-not $byAssembly.ContainsKey($key)) {
    $byAssembly[$key] = @{ Passed = 0; Failed = 0; NotExecuted = 0; Inconclusive = 0 }
  }
}

function Sanitize-Label([string] $s) {
  if ([string]::IsNullOrEmpty($s)) { return "unknown" }
  $t = $s -replace '[^a-zA-Z0-9_]', '_'
  if ($t.Length -eq 0) { $t = "a" }
  if ($t[0] -notmatch '[a-zA-Z_]') { $t = "a" + $t }
  if ($t.Length -gt 200) { $t = $t.Substring(0, 200) }
  $t
}

function Trx-Ctr([object] $el, [string[]] $attrNames) {
  if (-not $el) { return 0 }
  foreach ($a in $attrNames) {
    if ([string]::IsNullOrEmpty($a)) { continue }
    if ($el -is [System.Xml.XmlElement]) {
      $v = $el.GetAttribute($a)
      if (-not [string]::IsNullOrWhiteSpace($v)) { return [int]$v }
    } elseif ($el -is [System.Xml.XmlNode] -and $el.Attributes) {
      $x = $el.Attributes | Where-Object { $_.Name -ceq $a } | Select-Object -First 1
      if ($x -and $x.Value) { return [int]$x.Value }
    }
  }
  0
}

function Add-FromFile([string] $file) {
  $base = [IO.Path]::GetFileNameWithoutExtension($file)
  $xml = [xml]([System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8))
  Ensure-Assembly $base
  $results = @(Select-Xml -Xml $xml -XPath '//*[local-name()="UnitTestResult"]' -ErrorAction SilentlyContinue)
  foreach ($hit in $results) {
    $n = $hit.Node
    $o = if ($n.outcome) { $n.outcome } else { $n.GetAttribute("outcome") }
    if ([string]::IsNullOrEmpty($o)) { continue }
    $testName = if ($n.testName) { $n.testName } else { $n.GetAttribute("testName") }

    switch ($o) {
      "Passed"       { $script:passed++;         $byAssembly[$base].Passed++ }
      "Failed"       { $script:failed++;         $byAssembly[$base].Failed++;  if ($testName) { $null = $script:failNames.Add($testName) } }
      "NotExecuted"  { $script:notExecuted++;   $byAssembly[$base].NotExecuted++ }
      "Inconclusive" { $script:inconclusive++;  $byAssembly[$base].Inconclusive++ }
    }
  }

  if ($results.Count -eq 0) {
    $sum = @(Select-Xml -Xml $xml -XPath '//*[local-name()="ResultSummary"]/*[local-name()="Counters"]' -ErrorAction SilentlyContinue)
    if ($sum.Count -gt 0) {
      $c0 = $sum[0].Node
      if ($c0) {
        $p = Trx-Ctr $c0 @("passed", "Passed")
        $f = Trx-Ctr $c0 @("failed", "Failed")
        $n = Trx-Ctr $c0 @("notexecuted", "notExecuted", "NotExecuted")
        $i = Trx-Ctr $c0 @("inconclusive", "Inconclusive")
        $script:passed += $p;       $byAssembly[$base].Passed = $p
        $script:failed += $f;       $byAssembly[$base].Failed = $f
        $script:notExecuted += $n;  $byAssembly[$base].NotExecuted = $n
        $script:inconclusive += $i; $byAssembly[$base].Inconclusive = $i
      }
    }
  }
}

foreach ($f in $trxFiles) { Add-FromFile $f }
if ($trxFiles.Count -eq 0) {
  Write-Warning "No .trx files found. Did tests run?"
}

$ts = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# TYPE myada_test_outcome_last gauge")
[void]$sb.AppendLine("myada_test_outcome_last{outcome=""Passed""} $passed")
[void]$sb.AppendLine("myada_test_outcome_last{outcome=""Failed""} $failed")
[void]$sb.AppendLine("myada_test_outcome_last{outcome=""NotExecuted""} $notExecuted")
[void]$sb.AppendLine("myada_test_outcome_last{outcome=""Inconclusive""} $inconclusive")
# Name on TYPE line must match the next metric (was causing Pushgateway 400)
[void]$sb.AppendLine("# TYPE myada_test_run_last_unix gauge")
[void]$sb.AppendLine("myada_test_run_last_unix $ts")
[void]$sb.AppendLine("# TYPE myada_test_by_assembly_outcome gauge")
foreach ($kv in $byAssembly.GetEnumerator()) {
  $a = Sanitize-Label $kv.Key
  $v = $kv.Value
  # -f uses {0},{1} for values; metric labels use {{ }} for literal braces in .NET String.Format
  if ($v.Passed -gt 0)         { [void]$sb.AppendLine(('myada_test_by_assembly_outcome{{assembly="{0}",outcome="Passed"}} {1}' -f $a, $v.Passed)) }
  if ($v.Failed -gt 0)         { [void]$sb.AppendLine(('myada_test_by_assembly_outcome{{assembly="{0}",outcome="Failed"}} {1}' -f $a, $v.Failed)) }
  if ($v.NotExecuted -gt 0)  { [void]$sb.AppendLine(('myada_test_by_assembly_outcome{{assembly="{0}",outcome="NotExecuted"}} {1}' -f $a, $v.NotExecuted)) }
  if ($v.Inconclusive -gt 0)   { [void]$sb.AppendLine(('myada_test_by_assembly_outcome{{assembly="{0}",outcome="Inconclusive"}} {1}' -f $a, $v.Inconclusive)) }
}
$putUrl = "$pushUrl/metrics/job/dotnet_test/instance/last"
Write-Host "Pushing metrics to $putUrl" -ForegroundColor Cyan
try {
  # Prometheus text format must use \n line endings only; AppendLine() uses \r\n on Windows and breaks Pushgateway (400: unknown type "gauge\r")
  $pushText = $sb.ToString() -replace "`r`n", "`n" -replace "`r", "`n"
  if (-not $pushText.EndsWith("`n")) { $pushText += "`n" }
  $utf8NoBom = New-Object System.Text.UTF8Encoding $false
  $pushBytes = $utf8NoBom.GetBytes($pushText)
  Invoke-RestMethod -Uri $putUrl -Method Put -ContentType "text/plain; version=0.0.4; charset=utf-8" -Body $pushBytes
} catch {
  Write-Warning "Pushgateway failed. Is it running? $($_.Exception.Message)"
  if ($_.ErrorDetails -and $_.ErrorDetails.Message) { Write-Warning $_.ErrorDetails.Message }
}

# Loki: nanosecond *Unix* time (since 1970) as string, not UtcNow.Ticks (since 0001 would overflow Ticks*100 in int64 and is wrong epoch for Loki)
$lokiPath = "$lokiUrl/loki/api/v1/push"
$unixEpoch = [DateTimeOffset] '1970-01-01T00:00:00Z'
$ns = [string](([DateTimeOffset]::UtcNow - $unixEpoch).Ticks * 100L)
$failText = if ($failNames.Count -gt 0) { " | failed_tests: " + ($failNames -join ' | ') } else { "" }
$maxLen = 240 * 1024
if ($failText.Length -gt $maxLen) { $failText = $failText.Substring(0, $maxLen) + "..." }
$line     = "trx=$($trxFiles.Count) P=$passed F=$failed N=$notExecuted I=$inconclusive unix=$ts" + $failText
# Strip UTF-8 BOM in summary if any (rare) — avoid odd parse errors
if ($line.Length -gt 0 -and [int][char]$line[0] -eq 0xFEFF) { $line = $line.TrimStart([char]0xFEFF) }
try {
  $nsRow  = [string[]]@($ns, $line)
  $valuesRows = [System.Collections.ArrayList]::new()
  $null = $valuesRows.Add($nsRow)
  $lokiObj = [ordered]@{
    streams = [System.Collections.ArrayList]@(
      [ordered]@{
        stream = [ordered]@{ job = "myada_test_results" }
        values = $valuesRows
      }
    )
  }
  $lokiBody = $lokiObj | ConvertTo-Json -Depth 20 -Compress
  if (-not ($lokiBody -match '^\s*\{')) { throw "ConvertTo-Json did not return an object" }
} catch {
  throw "Loki body build failed: $($_.Exception.Message)"
}
try {
  Write-Host "Pushing test run summary to Loki ($lokiPath)" -ForegroundColor Cyan
  $utf8NoBom2 = New-Object System.Text.UTF8Encoding $false
  $lokiBytes = $utf8NoBom2.GetBytes($lokiBody)
  Invoke-RestMethod -Uri $lokiPath -Method Post -ContentType "application/json; charset=utf-8" -Body $lokiBytes
} catch {
  Write-Warning "Loki push failed (is Loki up?). $($_.Exception.Message)"
  if ($_.ErrorDetails -and $_.ErrorDetails.Message) { Write-Warning $_.ErrorDetails.Message }
}

Write-Host "Done. Open Grafana: http://localhost:3001 -> dashboard MyADA - Logs and test results" -ForegroundColor Green
