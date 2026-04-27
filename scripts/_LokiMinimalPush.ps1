$ErrorActionPreference = "Stop"
$u = "http://localhost:3100/loki/api/v1/push"
$unixEpoch = [DateTimeOffset] '1970-01-01T00:00:00Z'
$ns = [string](([DateTimeOffset]::UtcNow - $unixEpoch).Ticks * 100L)
$j = '{"streams":[{"stream":{"job":"myada_test_results"},"values":[["' + $ns + '","ok"]]}]}'
try {
  $b = (New-Object System.Text.UTF8Encoding $false).GetBytes($j)
  Invoke-RestMethod -Uri $u -Method Post -ContentType "application/json; charset=utf-8" -Body $b
  "OK: minimal Loki push"
} catch {
  "FAIL: $($_.Exception.Message)"
  if ($_.ErrorDetails) { $_.ErrorDetails.Message }
}
