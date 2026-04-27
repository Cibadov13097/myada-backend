# One-off: verify Loki push JSON from ConvertTo-Json (no dotnet)
$ErrorActionPreference = "Stop"
$ns = "1700000000000000000"
$line = "short test"
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
$j = $lokiObj | ConvertTo-Json -Depth 20 -Compress
$j
try { $null = $j | ConvertFrom-Json; "ConvertFrom-Json: OK" } catch { "ConvertFrom-Json: FAIL $_" }
