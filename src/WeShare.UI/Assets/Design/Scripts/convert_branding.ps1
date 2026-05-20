Add-Type -AssemblyName System.Drawing
function ConvertTo-Bmp($source, $dest) {
    if (Test-Path $source) {
        $img = [System.Drawing.Image]::FromFile((Resolve-Path $source))
        $img.Save($dest, [System.Drawing.Imaging.ImageFormat]::Bmp)
        $img.Dispose()
        Write-Host "Converted $source to $dest"
    }
}

$DesignDir = "$PSScriptRoot\.."
$AssetsDir = "$PSScriptRoot\..\.."

ConvertTo-Bmp "$DesignDir\installer_banner_dark.png" "$DesignDir\installer_banner_dark.bmp"
ConvertTo-Bmp "$DesignDir\app_icon_gradient.png" "$AssetsDir\logo.bmp"
