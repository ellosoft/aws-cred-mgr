function Add-ToPathPermanently($Path) {
    $currentPath = [Environment]::GetEnvironmentVariable("Path", [System.EnvironmentVariableTarget]::User)
    if ($currentPath -notlike "*$Path*") {
        $newPath = $currentPath.TrimEnd(';') + ";$Path"
        [Environment]::SetEnvironmentVariable("Path", $newPath, [System.EnvironmentVariableTarget]::User)
        $env:Path = $newPath
    }
}

function Get-SystemArchitecture {
    switch ($env:PROCESSOR_ARCHITECTURE) {
        "AMD64" { return "x64" }
        "ARM64" { return "arm64" }
        "x86"   { return "x86" }
        default { throw "Unsupported architecture: $env:PROCESSOR_ARCHITECTURE" }
    }
}

function Get-LatestReleaseInfo($architecture) {
    $releaseInfo = Invoke-RestMethod -Uri "https://api.github.com/repos/ellosoft/aws-cred-mgr/releases/latest"
    $asset = $releaseInfo.assets | Where-Object { $_.name -like "*win-$architecture.exe" }

    if ($null -eq $asset) {
        throw "Could not find a $architecture executable in the latest release."
    }

    return @{
        DownloadUrl = $asset.browser_download_url
        ExpectedFileSize = $asset.size
    }
}

function Create-InstallationDirectory {
    $installDir = "$env:USERPROFILE\.aws_cred_mgr\bin"
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    return $installDir
}

function Download-File($url, $outputPath, $expectedSize) {
    $ProgressPreference = 'SilentlyContinue'
    try {
        Write-Host "Downloading aws-cred-mgr to $outputPath..."
        Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing -Headers @{"Cache-Control"="no-cache"} -ContentType 'application/octet-stream' -Verbose:$false

        # TODO: Replace this with a checksum verification
        if ((Get-Item $outputPath).Length -ne $expectedSize){
            throw "The file size of the downloaded file does not match the expected size."
        }

        Write-Host "Download completed successfully."
    }
    finally {
        $ProgressPreference = 'Continue'
    }
}

function Install-AwsCredMgr {
    $architecture = Get-SystemArchitecture
    $architecture = "x64" # TODO: Remove once the ARM64 release is available

    $releaseInfo = Get-LatestReleaseInfo $architecture
    $installDir = Create-InstallationDirectory
    $outputPath = Join-Path $installDir "aws-cred-mgr.exe"

    Download-File -url $releaseInfo.DownloadUrl -outputPath $outputPath -expectedSize $releaseInfo.ExpectedFileSize

    Add-ToPathPermanently $installDir

    Write-Host ""
    Write-Host "aws-cred-mgr has been successfully installed!" -ForegroundColor Green
    Write-Host "You can now use aws-cred-mgr by running 'aws-cred-mgr' in your terminal." -ForegroundColor Green
}

try {
    Install-AwsCredMgr
}
catch {
    Write-Host "Failed to install aws-cred-mgr: $_" -ForegroundColor Red
    Write-Host "Please try again or download the latest version from https://github.com/ellosoft/aws-cred-mgr/releases." -ForegroundColor Red
}
