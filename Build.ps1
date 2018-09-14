param(
    [Parameter(Mandatory = $false)][switch] $RestorePackages,
    [Parameter(Mandatory = $false)][string] $Configuration = "Release",
    [Parameter(Mandatory = $false)][string] $OutputPath = ""
)

$ErrorActionPreference = "Stop"

$solutionPath = Split-Path $MyInvocation.MyCommand.Definition
$solutionFile = Join-Path $solutionPath "ApplePayJS.sln"
$sdkFile = Join-Path $solutionPath "global.json"

$dotnetVersion = (Get-Content $sdkFile | ConvertFrom-Json).sdk.version

if ($OutputPath -eq "") {
    $OutputPath = Join-Path "$(Convert-Path "$PSScriptRoot")" "artifacts"
}

if ($null -ne $env:CI) {
    $RestorePackages = $true
}

$installDotNetSdk = $false;

if (($null -eq (Get-Command "dotnet.exe" -ErrorAction SilentlyContinue)) -and ($null -eq (Get-Command "dotnet" -ErrorAction SilentlyContinue))) {
    Write-Host "The .NET Core SDK is not installed."
    $installDotNetSdk = $true
}
else {
    Try {
        $installedDotNetVersion = (dotnet --version 2>&1 | Out-String).Trim()
    }
    Catch {
        $installedDotNetVersion = "?"
    }

    if ($installedDotNetVersion -ne $dotnetVersion) {
        Write-Host "The required version of the .NET Core SDK is not installed. Expected $dotnetVersion."
        $installDotNetSdk = $true
    }
}

if ($installDotNetSdk -eq $true) {
    $env:DOTNET_INSTALL_DIR = Join-Path "$(Convert-Path "$PSScriptRoot")" ".dotnetcli"
    $sdkPath = Join-Path $env:DOTNET_INSTALL_DIR "sdk\$dotnetVersion"

    if (!(Test-Path $sdkPath)) {
        if (!(Test-Path $env:DOTNET_INSTALL_DIR)) {
            mkdir $env:DOTNET_INSTALL_DIR | Out-Null
        }
        $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.ps1"
        Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing
        & $installScript -Version "$dotnetVersion" -InstallDir "$env:DOTNET_INSTALL_DIR" -NoPath
    }

    $env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
    $dotnet = Join-Path "$env:DOTNET_INSTALL_DIR" "dotnet.exe"
}
else {
    $dotnet = "dotnet"
}

if ($RestorePackages -eq $true) {

    Write-Host "Restoring JavaScript packages..." -ForegroundColor Green

    $projectPath = Join-Path "$(Convert-Path "$PSScriptRoot")" "src"
    $projectPath = Join-Path $projectPath "ApplePayJs"

    Push-Location $projectPath
    & npm install
    & bower install
    Pop-Location
}

Write-Host "Publishing solution..." -ForegroundColor Green
& $dotnet publish $solutionFile --output $OutputPath --configuration $Configuration
