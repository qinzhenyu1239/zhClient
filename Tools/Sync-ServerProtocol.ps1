param([string]$ServerRoot = (Join-Path $PSScriptRoot '../../New_Server'))
$ErrorActionPreference = 'Stop'
$server = (Resolve-Path $ServerRoot).Path
$assets = Join-Path $PSScriptRoot '../Assets'
$destination = Join-Path $assets 'Game/Scripts/Messages'
$source = Join-Path $server 'GameServer/src/Game/Datas/Messages'
if (!(Test-Path $source)) { throw "Server messages not found: $source" }

function Write-ProtocolFile([string]$path, [string]$content) {
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($path), $content, [Text.UTF8Encoding]::new($false))
    if (!(Test-Path "$path.meta")) {
        $guid = [Guid]::NewGuid().ToString('N')
        [IO.File]::WriteAllText([IO.Path]::GetFullPath("$path.meta"), "fileFormatVersion: 2`nguid: $guid`n")
    }
}

# This project uses attributed C# contracts as its protocol source (there is no ET Proto2CS).
# Keep the old Unity script GUIDs while retiring their superseded type definitions.
$legacy = 'AccountInfo','PlayerInfo','ReqGuestLogin','ReqUserLogin','ReqSelectPlayer',
          'ReqPullingPlayerData','ResGuestLogin','ResSelectPlayer','ResPullingPlayerData'
foreach ($name in $legacy) {
    $path = Join-Path $destination "$name.cs"
    if (Test-Path $path) { Write-ProtocolFile $path '// Protocol definition moved to the synchronized server message files.' }
}
foreach ($file in (Get-ChildItem $source -Filter '*.cs')) {
    $content = [IO.File]::ReadAllText($file.FullName)
    # These unused server imports otherwise introduce server-only dependencies into Unity.
    $content = $content -replace '(?m)^using (Newtonsoft.Json|NLog.Config);\r?\n', ''
    Write-ProtocolFile (Join-Path $destination $file.Name) ("// Synchronized by Tools/Sync-ServerProtocol.ps1. Edit New_Server contracts first.`n" + $content)
}
Write-ProtocolFile (Join-Path $destination 'ServerConstants.cs') ([IO.File]::ReadAllText((Join-Path $server 'GameServer/src/Game/Constants.cs')))
Write-ProtocolFile (Join-Path $assets 'Framework/Scripts/Serializer/ProtocolPacketLayout.cs') ([IO.File]::ReadAllText((Join-Path $server 'GameServer/src/Framework/Core/serializer/ProtocolPacketLayout.cs')))
Write-Output 'Server contracts, command constants and packet layout synchronized.'
