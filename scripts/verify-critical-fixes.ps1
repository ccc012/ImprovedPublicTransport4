$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$failures = New-Object System.Collections.Generic.List[string]

function Assert-SourceMatch($relativePath, $pattern, $message) {
    $content = [System.IO.File]::ReadAllText((Join-Path $root $relativePath))
    if ($content -notmatch $pattern) {
        $failures.Add("$relativePath`: $message")
    }
}

function Assert-SourceNotMatch($relativePath, $pattern, $message) {
    $content = [System.IO.File]::ReadAllText((Join-Path $root $relativePath))
    if ($content -match $pattern) {
        $failures.Add("$relativePath`: $message")
    }
}

Assert-SourceMatch "CSLModsCommonShared\Utilities\JsonHelper.cs" "TypeNameHandling\s*=\s*TypeNameHandling\.None" "unsafe JSON type creation remains enabled"
Assert-SourceMatch "Data\SerializableDataExtension.cs" "EnsureAvailable\(data, index, 4\)" "primitive reads are not bounds checked"
Assert-SourceMatch "Data\SerializableDataExtension.cs" "new StringBuilder\(length\)" "ReadString still performs quadratic concatenation"
Assert-SourceMatch "HarmonyPatches\BuildingManagerPatches\GetDepotLevelsPatch.cs" "lineID\s*==\s*0\s*\|\|\s*lineID\s*>=\s*lines\.m_size" "lineID is indexed before validation"
Assert-SourceMatch "HarmonyPatches\DepotAIPatches\StartTransferPatch.cs" "lineID\s*==\s*0\s*\|\|\s*lineID\s*>=\s*lines\.m_size" "lineID is indexed before validation"
Assert-SourceMatch "UI\PreviewRenderer\PreviewRenderer.cs" "ReleaseRenderTexture\(" "render textures are replaced without release"
Assert-SourceMatch "Data\MovingAverage.cs" "lock \(this\._items\)\s*\{\s*if \(this\._items\.Count == 0\)" "queue count is still read outside its lock"
Assert-SourceMatch "HarmonyPatches\PublicTransportVehicleButtonPatches\OnMouseDownPatch.cs" "component is not UIButton button" "unsafe UIButton cast remains"
Assert-SourceMatch "Integration\ExpressBusServices\DepartureChecker.cs" "vehicleData\.Info\?\.m_class" "vehicle prefab null is not handled"
Assert-SourceMatch "UI\PanelExtenders\PanelExtenderLine.cs" "eventTextSubmitted -= OnColorTextSubmitted" "text event remains subscribed during destruction"
Assert-SourceMatch "HarmonyPatches\DepotAIPatches\DepotCapacityEnforcePatch.cs" "Interlocked\.CompareExchange\(ref _actionQueued, 1, 0\)" "cross-thread queue flag is not atomic"
Assert-SourceMatch "Data\CachedTransportLineData.cs" "lineID < data\.Length" "transport-line save records can exceed the fixed cache"
Assert-SourceMatch "Data\CachedTransportLineData.cs" "ReadInt32\(data1, ref index1\)" "transport-line deserialization bypasses validated readers"
Assert-SourceMatch "Data\CachedTransportLineData.cs" "IsKnownVersion\(str\)" "unknown four-character save versions are accepted"
Assert-SourceMatch "Data\CachedVehicleData.cs" "var cachedData = m_cachedVehicleData" "vehicle cache can be cleared while save iterates"
Assert-SourceMatch "Data\CachedNodeData.cs" "var cachedData = m_cachedNodeData" "node cache can be cleared while save iterates"
Assert-SourceMatch "BuildingExtension.cs" "OnDepotRemoved\?\.Invoke\(depots\.Key\.Service" "depot removals can emit duplicate class notifications"
Assert-SourceMatch "Data\PrefabData.cs" "Info\.m_trailers\.Length >= 2" "single-trailer Solaris prefab can index trailer 1"
Assert-SourceMatch "Data\VehiclePrefabs.cs" "_allPrefabData == null" "prefab lookup can run before registration"
Assert-SourceMatch "UI\VehicleSelectionRow.cs" "_info\?\.m_vehicleAI" "vehicle row dereferences a missing prefab AI"
Assert-SourceMatch "UI\VehicleSelection.cs" "CurrentLine != 0 && CurrentLine < lines\.m_size" "vehicle preview indexes an invalid line"
Assert-SourceMatch "UI\VehicleSelection.cs" "SelectedListVehicle[\s\S]*?CurrentLine != 0 && CurrentLine < lines\.m_size" "available-vehicle preview indexes an invalid line"
Assert-SourceMatch "Integration\MileageTaxiServices\Patch_TaxiAI_UnloadPassengers.cs" "taxiInstance\?\.m_transportInfo == null" "taxi fare calculation dereferences missing transport info"
Assert-SourceMatch "CSLModsCommonShared\Extension\ListExtensions.cs" "InvalidOperationException" "empty list access throws an opaque index error"
Assert-SourceMatch "CSLModsCommonShared\Manager\Domain.cs" "lock \(AllDomainsLock\)" "global domain registry is unsynchronized"
Assert-SourceMatch "CSLModsCommonShared\Manager\Domain.cs" "lock \(_managerLock\)" "manager registry is unsynchronized"
Assert-SourceMatch "CSLModsCommonShared\Manager\Domain.cs" "_managerLookup\.Remove\(type\)" "failed manager creation remains cached"
Assert-SourceMatch "CSLModsCommonShared\Manager\Domain.cs" "AllDomains\.Remove\(Name\)" "disposed domains remain globally rooted"
Assert-SourceMatch "CSLModsCommonShared\Manager\Domain.cs" "ReferenceEquals\(_defaultDomain, this\)[\s\S]*?_defaultDomain = null" "disposed default domain remains reusable"
Assert-SourceMatch "CSLModsCommonShared\Manager\UpdateManager.cs" "Snapshot\(_simulationInterfaces\)" "simulation interface enumeration is mutation-sensitive"
Assert-SourceMatch "ImprovedPublicTransportMod.cs" "public static volatile bool InGame" "game lifecycle state has no cross-thread visibility guarantee"
Assert-SourceMatch "UI\PublicTransportStopWorldInfoPanel.cs" "nodeCache != null && netNode != 0 && netNode < nodeCache\.Length\s*\? nodeCache\[netNode\]\.Unbunching" "stop panel indexes a missing node cache"
Assert-SourceMatch "HarmonyPatches\TransportLinePatches\SimulationStepPatch.cs" "m_currentFrameIndex\s*&\s*4095U\)\s*==\s*3840U" "weekly accounting runs for more than one frame"
Assert-SourceMatch "Data\CachedTransportLineData.cs" "QueueLocks\[lineID\]" "vehicle queues use replaceable queue instances as locks"
Assert-SourceMatch "Data\VehicleData.cs" "long income = \(long\)this\.IncomeThisWeek \+ \(long\)newPassengers \* ticketPrice" "passenger income can overflow before accumulation"
Assert-SourceMatch "HarmonyPatches\PublicTransportStopButtonPatches\OnMouseDownPatch.cs" "button\.objectUserData is not ushort objectUserData" "stop button assumes objectUserData is a ushort"
Assert-SourceMatch "UI\PreviewRenderer\PreviewRenderer.cs" "private Material _fallbackMaterial" "preview fallback material is allocated for every render"
Assert-SourceMatch "UI\PreviewRenderer\PreviewRenderer.cs" "Destroy\(_fallbackMaterial\)" "preview fallback material is never destroyed"
Assert-SourceMatch "CSLModsCommonShared\Utilities\CliExecutor.cs" "outputWaitHandle\.Close\(\)" "background CLI output wait handle is never disposed"
Assert-SourceMatch "CSLModsCommonShared\Utilities\CliExecutor.cs" "errorWaitHandle\.Close\(\)" "background CLI error wait handle is never disposed"
Assert-SourceMatch "HarmonyPatches\XYZVehicleAIPatches\LoadPassengersPatch.cs" "LoadPassengersPost\(State __state, bool __runOriginal\)" "post-accounting relies only on cross-invocation thread state"
Assert-SourceMatch "HarmonyPatches\XYZVehicleAIPatches\LoadPassengersPatch.cs" "if \(!__runOriginal\)" "skipped LoadPassengers calls are not identified from Harmony invocation state"
Assert-SourceNotMatch "HarmonyPatches\DepotAIPatches\DepotStatsDisplayPatch.cs" "_cacheBuildingId" "depot stats cache can publish an incomplete or stale entry"
Assert-SourceMatch "HarmonyPatches\TransportManagerPatches\CheckTransportLineVehiclesPatch.cs" "TryGetSelectedLineVehicle" "vanilla vehicle migration remains disabled for every line"
Assert-SourceMatch "HarmonyPatches\TransportManagerPatches\CheckTransportLineVehiclesPatch.cs" "CachedTransportLineData\.GetPrefabs\(lineID\) != null" "IPT-managed lines are not separated from vanilla-managed lines"
Assert-SourceMatch "Data\CachedTransportLineData.cs" "firstStop >= instance1\.m_nodes\.m_buffer\.Length" "first stop indexes the node buffer without an upper bound"
Assert-SourceMatch "CSLModsCommonShared\UI\ValueFields\TextDocument.cs" "var last = _redoStack\.Last\(\);\s*_redoStack\.RemoveAt" "Redo reads from the undo stack"
Assert-SourceNotMatch "UI\PanelExtenders\PanelExtenderVehicle.cs" "Vehicle\.Flags\.WaitingPath\)\s*!=\s*~" "WaitingPath is compared with an inverted all-flags mask"
Assert-SourceMatch "UI\PanelExtenders\PanelExtenderVehicle.cs" "\(data\.m_flags & Vehicle\.Flags\.WaitingPath\) != 0" "WaitingPath is not tested as a bit flag"
Assert-SourceMatch "UI\PreviewRenderer\PreviewRenderer.cs" "try[\s\S]*?RenderWithShader[\s\S]*?finally[\s\S]*?renderLight\.transform\.rotation = renderLightRotation[\s\S]*?SetCurrentMode\(currentMode, currentSubMode\)" "preview rendering does not transactionally restore global state"

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine("FAIL: $_") }
    exit 1
}

"Critical fix source checks passed."
