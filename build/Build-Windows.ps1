$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Set-Location $root
if(!(Get-Command dotnet -ErrorAction SilentlyContinue)){throw '.NET 10 SDK is required to build.'}
Remove-Item artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item artifacts\client -ItemType Directory -Force | Out-Null
$cfg=Get-Content src\Parsis.AutoTrader.App\appsettings.Production.json -Raw
if($env:TELEGRAM_API_ID){$cfg=$cfg.Replace('__TELEGRAM_API_ID__',$env:TELEGRAM_API_ID)}
if($env:TELEGRAM_API_HASH){$cfg=$cfg.Replace('__TELEGRAM_API_HASH__',$env:TELEGRAM_API_HASH)}
$cfg | Set-Content src\Parsis.AutoTrader.App\appsettings.Production.json -Encoding utf8

dotnet restore Parsis.AutoTrader.sln
dotnet test tests\Parsis.AutoTrader.Tests\Parsis.AutoTrader.Tests.csproj -c Release
dotnet publish src\Parsis.AutoTrader.App\Parsis.AutoTrader.App.csproj -c Release -r win-x64 --self-contained true -o artifacts\client
$iscc=(Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
if(!$iscc){$iscc='C:\Program Files (x86)\Inno Setup 6\ISCC.exe'}
if(!(Test-Path $iscc)){throw 'Inno Setup 6 not found.'}
& $iscc installer\ParsisAutoTrader.iss
Write-Host 'Build complete: installer\output'
