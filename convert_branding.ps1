Add-Type -AssemblyName System.Drawing
function ConvertTo-Bmp($source, $dest) {
    if (Test-Path $source) {
        $img = [System.Drawing.Image]::FromFile((Resolve-Path $source))
        $img.Save((Join-Path (Get-Location) $dest), [System.Drawing.Imaging.ImageFormat]::Bmp)
        $img.Dispose()
        Write-Host "Converted $source to $dest"
    }
}

ConvertTo-Bmp "design_assets\installer_banner_dark.png" "design_assets\installer_banner_dark.bmp"
ConvertTo-Bmp "design_assets\app_icon_gradient.png" "src\WeShare.UI\Assets\logo.bmp"
