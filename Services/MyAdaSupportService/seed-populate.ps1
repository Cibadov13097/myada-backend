param(
    [string]$BaseUrl = "http://localhost:5007"
)

$ErrorActionPreference = "Stop"

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [object]$Body = $null
    )

    $uri = "$BaseUrl$Path"
    try {
        if ($null -eq $Body) {
            return Invoke-RestMethod -Uri $uri -Method $Method -ContentType "application/json"
        }

        $json = $Body | ConvertTo-Json -Depth 10
        return Invoke-RestMethod -Uri $uri -Method $Method -Body $json -ContentType "application/json"
    }
    catch {
        Write-Host "API call failed: $Method $Path" -ForegroundColor Red
        throw
    }
}

function Get-Result {
    param([object]$Response)
    if ($null -eq $Response) { return $null }
    if ($null -ne $Response.result) { return $Response.result }
    return $Response
}

function Get-RandomItem {
    param([array]$Items)
    return $Items | Get-Random
}

function Get-CountSafe {
    param(
        [string]$Path,
        [string]$Label
    )

    try {
        $resp = Invoke-Api -Method "GET" -Path $Path
        $result = Get-Result $resp
        $count = @($result).Count
        Write-Host "$Label -> $count"
        return $count
    }
    catch {
        Write-Host "$Label -> FAILED ($($_.Exception.Message))" -ForegroundColor Yellow
        return -1
    }
}

Write-Host "== MyAdaSupportService Seed Script ==" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"

$reporterIds = 3001..3010
$itStaffIds = 9001..9005
$fmStaffIds = 9101..9105
$allStaffIds = @($itStaffIds + $fmStaffIds)
$dispatcherId = 8001

$buildings = @("A", "B", "C")
$roomsByBuilding = @{
    "A" = @(101, 102, 103, 203)
    "B" = @(101, 102, 105)
    "C" = @(101, 201, 202)
}
$campusAreas = @(
    "Main Gate near parking area",
    "Library open study zone",
    "Sports field lighting pole 3",
    "Student affairs front desk",
    "Cafeteria outdoor seating",
    "Admin block corridor level 2"
)

$itCategories = @(
    "Wi-Fi connectivity issue",
    "Projector or smart board issue",
    "Computer or laptop issue",
    "Printer or scanner issue",
    "Account access or password reset",
    "Email or Teams issue",
    "Network port or LAN issue",
    "Software installation request"
)

$fmCategories = @(
    "Air conditioner issue",
    "Electrical outlet or lighting issue",
    "Plumbing leak or blockage",
    "Furniture damage",
    "Door or lock issue",
    "Cleaning request",
    "Pest control request",
    "Water dispenser issue"
)

# 1) Create categories (at least 8 IT + 8 FM)
$categorySuffix = (Get-Date -Format "yyyyMMddHHmmss")
$createdItCategoryIds = @()
$createdFmCategoryIds = @()

Write-Host "`n[1/6] Creating categories..."
foreach ($name in $itCategories) {
    $payload = @{
        dto = @{
            module = 0
            name = "$name [$categorySuffix]"
        }
    }
    $resp = Invoke-Api -Method "POST" -Path "/api/Categories" -Body $payload
    $result = Get-Result $resp
    $createdItCategoryIds += [int]$result.id
    Start-Sleep -Milliseconds 70
}

foreach ($name in $fmCategories) {
    $payload = @{
        dto = @{
            module = 1
            name = "$name [$categorySuffix]"
        }
    }
    $resp = Invoke-Api -Method "POST" -Path "/api/Categories" -Body $payload
    $result = Get-Result $resp
    $createdFmCategoryIds += [int]$result.id
    Start-Sleep -Milliseconds 70
}

Write-Host "Created IT categories: $($createdItCategoryIds.Count)"
Write-Host "Created FM categories: $($createdFmCategoryIds.Count)"

# 2) Set staff statuses (Online/Offline/OnBreak coverage)
Write-Host "`n[2/6] Setting staff statuses..."
$statusPlan = @{
    9001 = "Online"; 9002 = "Online"; 9003 = "OnBreak"; 9004 = "Offline"; 9005 = "OnBreak";
    9101 = "Online"; 9102 = "Offline"; 9103 = "OnBreak"; 9104 = "Online"; 9105 = "Offline"
}

$statusCounts = @{ Online = 0; Offline = 0; OnBreak = 0 }
foreach ($staffId in $allStaffIds) {
    $status = $statusPlan[$staffId]
    $payload = @{ status = $status }
    Invoke-Api -Method "PUT" -Path "/api/SupportStaffStatuses/member/$staffId" -Body $payload | Out-Null
    $statusCounts[$status]++
    Start-Sleep -Milliseconds 50
}
Write-Host "Staff statuses set -> Online:$($statusCounts.Online), Offline:$($statusCounts.Offline), OnBreak:$($statusCounts.OnBreak)"

# 3) Create 60 requests (30 IT, 30 FM)
Write-Host "`n[3/6] Creating support requests..."
$allRequests = New-Object System.Collections.Generic.List[object]
$criticalTarget = 12
$buildingTarget = 42

for ($i = 0; $i -lt 60; $i++) {
    $isIt = $i -lt 30
    $area = if ($isIt) { "it" } else { "fm" }
    $memberId = Get-RandomItem -Items $reporterIds
    $categoryId = if ($isIt) { Get-RandomItem -Items $createdItCategoryIds } else { Get-RandomItem -Items $createdFmCategoryIds }

    $isBuilding = $i -lt $buildingTarget
    $isCritical = $i -lt $criticalTarget

    if ($isBuilding) {
        $building = Get-RandomItem -Items $buildings
        $useRoom = ($i % 3 -ne 0) # majority room, some nonRoom

        if ($useRoom) {
            $room = Get-RandomItem -Items $roomsByBuilding[$building]
            $payload = @{
                area = $area
                categoryId = $categoryId
                locationType = "building"
                buildingId = switch ($building) { "A" { 1 } "B" { 2 } "C" { 3 } }
                placeType = "room"
                roomId = $room
                areaDetails = "Building $building room $room"
                description = if ($isIt) { "IT issue #$($i + 1) in Building $building Room $room" } else { "FM issue #$($i + 1) in Building $building Room $room" }
                urgency = if ($isCritical) { "critical" } else { "standard" }
                attachmentUrls = @()
            }
        }
        else {
            $payload = @{
                area = $area
                categoryId = $categoryId
                locationType = "building"
                buildingId = switch ($building) { "A" { 1 } "B" { 2 } "C" { 3 } }
                placeType = "nonRoom"
                roomId = $null
                areaDetails = "Building $building lobby/common area"
                description = if ($isIt) { "IT issue #$($i + 1) at Building $building common area" } else { "FM issue #$($i + 1) at Building $building common area" }
                urgency = if ($isCritical) { "critical" } else { "standard" }
                attachmentUrls = @()
            }
        }
    }
    else {
        $campus = Get-RandomItem -Items $campusAreas
        $payload = @{
            area = $area
            categoryId = $categoryId
            locationType = "campus"
            buildingId = $null
            placeType = "nonRoom"
            roomId = $null
            areaDetails = $campus
            description = if ($isIt) { "IT issue #$($i + 1) at campus: $campus" } else { "FM issue #$($i + 1) at campus: $campus" }
            urgency = if ($isCritical) { "critical" } else { "standard" }
            attachmentUrls = @()
        }
    }

    $resp = Invoke-Api -Method "POST" -Path "/api/SupportRequests/member/$memberId" -Body $payload
    $created = Get-Result $resp

    $obj = [PSCustomObject]@{
        requestId = [int]$created.id
        memberId = [int]$memberId
        area = $area
    }
    $allRequests.Add($obj)
    Start-Sleep -Milliseconds 80
}

Write-Host "Created requests total: $($allRequests.Count) (IT:30, FM:30)"
Write-Host "Created request IDs: $((($allRequests | Select-Object -ExpandProperty requestId) -join ', '))"

# 4) Assign + lifecycle transitions to reach exact status distribution
Write-Host "`n[4/6] Applying assignments and lifecycle..."
$assignedPool = $allRequests | Select-Object -First 45
$progressPool = $assignedPool | Select-Object -First 30
$completedPool = $progressPool | Select-Object -First 15
$cancelledPool = $allRequests | Select-Object -Skip 55 | Select-Object -First 5

$assignedCount = 0
$inProgressCount = 0
$completedCount = 0
$cancelledCount = 0

foreach ($r in $assignedPool) {
    $staffId = if ($r.area -eq "it") { Get-RandomItem -Items $itStaffIds } else { Get-RandomItem -Items $fmStaffIds }
    $assignPayload = @{
        staffId = $staffId
        dispatcherInstructions = "Please handle request $($r.requestId) and update timeline."
    }
    Invoke-Api -Method "PUT" -Path "/api/SupportRequests/$($r.requestId)/assign/dispatcher/$dispatcherId" -Body $assignPayload | Out-Null
    $r | Add-Member -NotePropertyName staffId -NotePropertyValue $staffId -Force
    $assignedCount++
    Start-Sleep -Milliseconds 70
}

foreach ($r in $progressPool) {
    Invoke-Api -Method "PUT" -Path "/api/SupportRequests/$($r.requestId)/start/staff/$($r.staffId)" | Out-Null
    Start-Sleep -Milliseconds 60
    Invoke-Api -Method "PUT" -Path "/api/SupportRequests/$($r.requestId)/in-progress/staff/$($r.staffId)" | Out-Null
    $inProgressCount++
    Start-Sleep -Milliseconds 60
}

foreach ($r in $completedPool) {
    Invoke-Api -Method "PUT" -Path "/api/SupportRequests/$($r.requestId)/complete/staff/$($r.staffId)" | Out-Null
    $completedCount++
    Start-Sleep -Milliseconds 60
}

foreach ($r in $cancelledPool) {
    $cancelPayload = @{
        reason = "Reporter no longer needs support for request $($r.requestId)."
    }
    Invoke-Api -Method "PUT" -Path "/api/SupportRequests/$($r.requestId)/cancel/member/$($r.memberId)" -Body $cancelPayload | Out-Null
    $cancelledCount++
    Start-Sleep -Milliseconds 60
}

Write-Host "Assigned: $assignedCount"
Write-Host "Moved to InProgress flow: $inProgressCount"
Write-Host "Completed: $completedCount"
Write-Host "Cancelled: $cancelledCount"
Write-Host "Expected final status distribution (for this run): New=10, Assigned=15, InProgress=15, Completed=15, Cancelled=5"

# 5) Add attachments to >=25 requests (1-3 each)
Write-Host "`n[5/6] Adding attachments..."
$requestsForAttachments = $allRequests | Select-Object -First 25
$totalAttachments = 0

foreach ($r in $requestsForAttachments) {
    $attachCount = Get-Random -Minimum 1 -Maximum 4
    for ($j = 1; $j -le $attachCount; $j++) {
        $attachmentPayload = @{
            requestId = $r.requestId
            url = "https://cdn.example.com/support/$($r.requestId)/evidence-$j.jpg"
        }
        Invoke-Api -Method "POST" -Path "/api/Attachments" -Body $attachmentPayload | Out-Null
        $totalAttachments++
        Start-Sleep -Milliseconds 40
    }
}

Write-Host "Attachments created: $totalAttachments across $($requestsForAttachments.Count) requests"

# 6) Validation calls requested by prompt
Write-Host "`n[6/6] Validation counts (requested filters)..."

Get-CountSafe -Path "/api/SupportRequests?status=open&service=IT" -Label "GET /api/SupportRequests?status=open&service=IT" | Out-Null
Get-CountSafe -Path "/api/SupportRequests?status=open&service=FM" -Label "GET /api/SupportRequests?status=open&service=FM" | Out-Null
Get-CountSafe -Path "/api/SupportRequests?status=completed" -Label "GET /api/SupportRequests?status=completed" | Out-Null
Get-CountSafe -Path "/api/SupportRequests?status=cancelled" -Label "GET /api/SupportRequests?status=cancelled" | Out-Null

Write-Host "`nSeed completed successfully." -ForegroundColor Green
