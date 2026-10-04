<#
.SYNOPSIS
    Builds OC2 Controller Icons and produces the release packages.

.DESCRIPTION
    1. Locates the Overcooked! 2 installation (needed only for its managed DLLs).
    2. Downloads BepInEx 5 (Windows x86 + macOS) into libs\ if it is not there yet.
    3. Compiles src\ with the C# compiler that ships with Windows (.NET Framework 4.x).
    4. Creates release\v<version>\ with one zip per platform plus the release notes.

.PARAMETER GameDir
    Overcooked! 2 folder. Auto-detected from Steam when omitted.

.PARAMETER Install
    Also copies the Windows package into the game folder (the game must be closed).

.EXAMPLE
    .\build.ps1
    .\build.ps1 -Install
    .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Overcooked! 2"
#>
param(
    [string]$GameDir,
    [switch]$Install
)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$Version         = "3.0.0"
$BepInExVersion  = "5.4.23.2"
$Csc             = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
$Out             = "build\OC2ControllerIcons.dll"

# ------------------------------------------------------------------ helpers

function Find-GameDir {
    $candidates = @()
    try {
        $steam = (Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction Stop).SteamPath
        if ($steam) {
            $candidates += Join-Path $steam "steamapps\common\Overcooked! 2"
            $vdf = Join-Path $steam "steamapps\libraryfolders.vdf"
            if (Test-Path $vdf) {
                foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                    $lib = $m.Groups[1].Value -replace '\\\\', '\'
                    $candidates += Join-Path $lib "steamapps\common\Overcooked! 2"
                }
            }
        }
    } catch { }
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c "Overcooked2_Data\Managed\Assembly-CSharp.dll")) { return $c }
    }
    return $null
}

function Get-BepInEx([string]$flavor) {
    $dir = "libs\BepInEx_$flavor"
    if (Test-Path "$dir\BepInEx\core\BepInEx.dll") { return $dir }
    $zip = "libs\BepInEx_${flavor}_$BepInExVersion.zip"
    $url = "https://github.com/BepInEx/BepInEx/releases/download/v$BepInExVersion/BepInEx_${flavor}_$BepInExVersion.zip"
    New-Item -ItemType Directory -Force "libs" | Out-Null
    Write-Host "Downloading BepInEx $BepInExVersion ($flavor)..." -ForegroundColor Cyan
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    Expand-Archive -Path $zip -DestinationPath $dir -Force
    Remove-Item $zip
    return $dir
}

# --------------------------------------------------------------------- build

if (-not $GameDir) { $GameDir = Find-GameDir }
if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir "Overcooked2_Data\Managed"))) {
    throw "Overcooked! 2 not found. Pass it explicitly: .\build.ps1 -GameDir `"<path to Overcooked! 2>`""
}
if (-not (Test-Path $Csc)) { throw "C# compiler not found at $Csc (.NET Framework 4.x is required)." }

$managed    = Join-Path $GameDir "Overcooked2_Data\Managed"
$bepWindows = Get-BepInEx "win_x86"      # Overcooked2.exe is a 32-bit executable
$bepMac     = Get-BepInEx "macos_x64"

New-Item -ItemType Directory -Force "build" | Out-Null

# The game runs on Mono 2.0 (.NET 3.5 profile): compile against ITS mscorlib, C# 5.
$refs = @(
    "mscorlib.dll", "System.dll", "System.Core.dll",
    "UnityEngine.dll", "UnityEngine.CoreModule.dll", "UnityEngine.UI.dll", "UnityEngine.UIModule.dll",
    "UnityEngine.TextRenderingModule.dll", "UnityEngine.ImageConversionModule.dll",
    "UnityEngine.AnimationModule.dll", "Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll"
) | ForEach-Object { "/r:" + (Join-Path $managed $_) }
$refs += "/r:$bepWindows\BepInEx\core\BepInEx.dll"
$refs += "/r:$bepWindows\BepInEx\core\0Harmony.dll"

$resources = Get-ChildItem "res\*.png" | ForEach-Object { "/resource:$($_.FullName),OC2ControllerIcons.res.$($_.Name)" }
$sources   = Get-ChildItem "src" -Recurse -Filter *.cs | ForEach-Object { $_.FullName }

$cscArgs = @("/nologo", "/target:library", "/optimize+", "/nostdlib+", "/noconfig",
             "/langversion:5", "/codepage:65001", "/out:$Out") + $refs + $resources + $sources

Write-Host "Compiling OC2 Controller Icons $Version..." -ForegroundColor Cyan
& $Csc @cscArgs
if ($LASTEXITCODE -ne 0) { Write-Host "Build FAILED" -ForegroundColor Red; exit 1 }
Write-Host "OK: $Out" -ForegroundColor Green

# ------------------------------------------------------------------ packages
# dist\windows : BepInEx x86 (Windows, and Linux / Steam Deck through Proton) + plugin
# dist\macos   : BepInEx macOS (libdoorstop + run_bepinex.sh) + plugin
# dist\plugin  : plugin only (for users who already have BepInEx 5)

$plugDir = "BepInEx\plugins\OC2ControllerIcons"
$dist    = "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
foreach ($p in @("windows", "macos", "plugin")) {
    New-Item -ItemType Directory -Force "$dist\$p\$plugDir" | Out-Null
    Copy-Item $Out "$dist\$p\$plugDir\" -Force
    Copy-Item "docs\README_EN.txt" "$dist\$p\OC2ControllerIcons_README.txt" -Force
    Copy-Item "LICENSE" "$dist\$p\$plugDir\LICENSE.txt" -Force
}
Copy-Item "$bepWindows\*" "$dist\windows\" -Recurse -Force
Copy-Item "$bepMac\*"     "$dist\macos\"   -Recurse -Force

# License notices for the bundled third-party components (BepInEx, Doorstop, HarmonyX...).
foreach ($p in @("windows", "macos")) {
    $lic = "$dist\$p\BepInEx\licenses"
    New-Item -ItemType Directory -Force $lic | Out-Null
    Copy-Item "docs\licenses\*" $lic -Force
    Copy-Item "LICENSE" "$lic\OC2ControllerIcons.LICENSE.txt" -Force
}

$release = "release\v$Version"
if (Test-Path $release) { Remove-Item $release -Recurse -Force }
New-Item -ItemType Directory -Force $release | Out-Null
$zips = [ordered]@{
    "windows" = "OC2ControllerIcons_v$Version-Windows_Linux.zip"
    "macos"   = "OC2ControllerIcons_v$Version-macOS.zip"
    "plugin"  = "OC2ControllerIcons_v$Version-PluginOnly.zip"
}
foreach ($k in $zips.Keys) {
    Compress-Archive -Path "$dist\$k\*" -DestinationPath "$release\$($zips[$k])"
}
Copy-Item "docs\NEXUSMODS_DESCRIPTION.txt" "$release\" -Force
Copy-Item "docs\README_EN.txt" "$release\INSTALL_AND_USAGE.txt" -Force
Write-Host "Release ready: $release" -ForegroundColor Green
Get-ChildItem $release | ForEach-Object { Write-Host ("  " + $_.Name) }

if ($Install) {
    Copy-Item "$dist\windows\*" $GameDir -Recurse -Force
    Write-Host "Installed into: $GameDir" -ForegroundColor Green
}
