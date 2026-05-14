# Ultimate Build Script for We Share
# Generates a Zero-Dependency, Single-File, Self-Contained Windows Executable.
# NO .NET RUNTIME OR ADDITIONAL SOFTWARE REQUIRED ON THE TARGET DEVICE.

$ProjectDir = "src\WeShare.Desktop"
$PublishDir = "publish"
$ISCC = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "   WE SHARE - STANDALONE DEPLOYMENT BUILD" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

# Clean previous builds
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
mkdir $PublishDir

Write-Host "`nStep 1: Publishing Single-File Executable (Self-Contained)..." -ForegroundColor Yellow

# Publish command with all optimizations for zero-dependency portability
dotnet publish $ProjectDir `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $PublishDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nStep 1 Success! Standalone binary generated." -ForegroundColor Green
    
    if (Test-Path $ISCC) {
        Write-Host "`nStep 2: Building Professional Installer..." -ForegroundColor Yellow
        & $ISCC "installer.iss"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`nStep 2 Success! Installer built at 'setup\WeShare_Setup.exe'." -ForegroundColor Green
        } else {
            Write-Host "`nStep 2 Failed." -ForegroundColor Red
        }
    } else {
        Write-Host "`nInno Setup Compiler (ISCC.exe) not found. Skipping Step 2." -ForegroundColor Gray
        Write-Host "The standalone EXE in '$PublishDir' is still ready for use!" -ForegroundColor Green
    }
} else {
    Write-Host "`nPublishing failed. Please check the errors above." -ForegroundColor Red
}

Write-Host "`nDone!" -ForegroundColor Cyan
