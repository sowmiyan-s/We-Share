$sdkRoot = "d:\PROJECTS\SHARE IT\android-sdk"
$zipPath = "$sdkRoot\tools.zip"
$tempPath = "$sdkRoot\temp"

if (-not (Test-Path $tempPath)) { New-Item -Path $tempPath -ItemType Directory }

Write-Host "Extracting tools..."
Expand-Archive -Path $zipPath -DestinationPath $tempPath -Force

# Create the specific structure sdkmanager expects
$cmdlineLatest = "$sdkRoot\cmdline-tools\latest"
if (-not (Test-Path $cmdlineLatest)) { New-Item -Path $cmdlineLatest -ItemType Directory -Force }

Write-Host "Moving tools to $cmdlineLatest"
Move-Item -Path "$tempPath\cmdline-tools\*" -Destination $cmdlineLatest -Force

# Cleanup temp
Remove-Item -Path $tempPath -Recurse -Force

$sdkManager = "$cmdlineLatest\bin\sdkmanager.bat"

# Set up environment variables for the session
$env:ANDROID_HOME = $sdkRoot
$env:ANDROID_SDK_ROOT = $sdkRoot

Write-Host "Accepting licenses..."
# Pass 'y' to accept all licenses
$yes = "y`ny`ny`ny`ny`ny`ny`n"
$yes | &$sdkManager --sdk_root=$sdkRoot --licenses

Write-Host "Installing platform 34 and build-tools 34.0.0..."
&$sdkManager --sdk_root=$sdkRoot "platforms;android-34" "build-tools;34.0.0" "platform-tools"

Write-Host "Android SDK Setup Complete."
