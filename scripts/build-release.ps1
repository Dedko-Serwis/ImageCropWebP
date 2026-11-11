param(
    [switch]$PublishGitHub
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'ImageCropWebP.csproj'
$dist = Join-Path $root 'dist'
$publish = Join-Path $dist 'publish'
[xml]$xml = Get-Content -LiteralPath $project -Raw
$version = [string]$xml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Ustaw <Version>X.Y.Z</Version> w ImageCropWebP.csproj.' }
$setup = Join-Path $dist "ImageCropWebP-$version-Setup.exe"
$iss = Join-Path $root 'installer\ImageCropWebP.iss'
if (-not (Test-Path $iss)) { throw "Brak $iss" }
New-Item -ItemType Directory -Force -Path $dist | Out-Null
if (Test-Path $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
& dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false "-p:Version=$version" -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish nie powiodlo sie.' }
$isccCandidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 7\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $iscc) { $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue; if ($cmd) { $iscc = $cmd.Source } }
if (-not $iscc) { throw 'Nie znaleziono Inno Setup 6/7 (ISCC.exe).' }
& $iscc "/DMyAppVersion=$version" "/DSourceDir=$publish" $iss
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $setup)) { throw 'Budowa instalatora nie powiodla sie.' }
Get-FileHash -LiteralPath $setup -Algorithm SHA256 | Format-List
if ($PublishGitHub) {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $gh) { throw 'Zainstaluj GitHub CLI (gh) i wykonaj gh auth login.' }
    & gh auth status
    if ($LASTEXITCODE -ne 0) { throw 'Brak autoryzacji gh.' }
    & git -C $root diff --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Zapisz zmiany w git przed wydaniem.' }
    & git -C $root diff --cached --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Zapisz zmiany staged w git przed wydaniem.' }
    $untracked = & git -C $root ls-files --others --exclude-standard
    if ($untracked) { throw 'Repozytorium zawiera niezatwierdzone pliki.' }
    $branch = (& git -C $root branch --show-current).Trim()
    if ($branch -ne 'master') { throw 'Publikuj tylko z master.' }
    & git -C $root fetch origin master --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Nie udalo sie pobrac origin/master.' }
    $head = (& git -C $root rev-parse HEAD).Trim()
    $remote = (& git -C $root rev-parse origin/master).Trim()
    if ($head -ne $remote) { throw 'HEAD rozni sie od origin/master.' }
    & gh release view "v$version" --repo Dedko-Serwis/ImageCropWebP *> $null
    if ($LASTEXITCODE -eq 0) { throw "Release v$version juz istnieje." }
    & gh release create "v$version" $setup --repo Dedko-Serwis/ImageCropWebP --target $head --title "ImageCropWebP $version" --generate-notes
    if ($LASTEXITCODE -ne 0) { throw 'Publikacja GitHub Release nie powiodla sie.' }
}
