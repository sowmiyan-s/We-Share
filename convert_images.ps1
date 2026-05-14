Add-Type -AssemblyName System.Drawing
function ConvertTo-Bmp($source, $dest) {
    if (Test-Path $source) {
        $img = [System.Drawing.Image]::FromFile((Resolve-Path $source))
        $img.Save((Join-Path (Get-Location) $dest), [System.Drawing.Imaging.ImageFormat]::Bmp)
        $img.Dispose()
        Write-Host "Converted $source to $dest"
    }
}

ConvertTo-Bmp "design_assets\installer_banner.png" "design_assets\installer_banner.bmp"
ConvertTo-Bmp "src\WeShare.UI\Assets\logo.png" "src\WeShare.UI\Assets\logo.bmp"
