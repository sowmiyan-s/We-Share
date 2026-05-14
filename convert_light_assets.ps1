Add-Type -AssemblyName System.Drawing

function Convert-ToBmp {
    param([string]$source, [string]$dest, [int]$width, [int]$height)
    $img = [System.Drawing.Image]::FromFile($source)
    $bmp = New-Object System.Drawing.Bitmap($width, $height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::White)
    $g.DrawImage($img, 0, 0, $width, $height)
    $bmp.Save($dest, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $g.Dispose()
    $bmp.Dispose()
    $img.Dispose()
}

# Paths
$baseDir = "d:\PROJECTS\WE SHARE"
$bannerSource = "C:\Users\Asus\.gemini\antigravity\brain\087331c3-6772-49e8-bc25-88d53fb9d9a2\installer_banner_light_1778679001355.png"
$logoSource = "$baseDir\src\WeShare.UI\Assets\logo.png"

# Convert Banner (WizardImageFile - usually 164x314 or similar, but Stretch=yes helps)
Convert-ToBmp -source $bannerSource -dest "$baseDir\design_assets\installer_banner_light.bmp" -width 164 -height 314

# Convert Logo (WizardSmallImageFile - 55x55)
Convert-ToBmp -source $logoSource -dest "$baseDir\src\WeShare.UI\Assets\logo_light.bmp" -width 55 -height 55

Write-Host "Light theme assets generated successfully."
