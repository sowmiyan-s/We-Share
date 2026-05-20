Add-Type -AssemblyName System.Drawing

function Convert-ToBmp {
    param([string]$source, [string]$dest, [int]$width, [int]$height)
    $img = New-Object System.Drawing.Bitmap($source)
    $bmp = New-Object System.Drawing.Bitmap($width, $height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    
    # Auto-detect background color from the top-left pixel
    $bgColor = $img.GetPixel(0, 0)
    $g.Clear($bgColor)
    
    # High quality settings
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    
    # Preserve aspect ratio
    $ratioX = $width / $img.Width
    $ratioY = $height / $img.Height
    $ratio = [System.Math]::Min($ratioX, $ratioY)
    
    $newWidth = [int]($img.Width * $ratio)
    $newHeight = [int]($img.Height * $ratio)
    
    $posX = [int](($width - $newWidth) / 2)
    $posY = [int](($height - $newHeight) / 2)
    
    $g.DrawImage($img, $posX, $posY, $newWidth, $newHeight)
    
    $bmp.Save($dest, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $g.Dispose()
    $bmp.Dispose()
    $img.Dispose()
}

# Paths
$DesignDir = "$PSScriptRoot\.."
$AssetsDir = "$PSScriptRoot\..\.."

$bannerSource = "$DesignDir\We Share.png"
$logoSource = "$AssetsDir\logo.png"

# Convert Banner (WizardImageFile - usually 164x314 or similar, but Stretch=yes helps)
Convert-ToBmp -source $bannerSource -dest "$DesignDir\installer_banner_light.bmp" -width 164 -height 314

# Convert Logo (WizardSmallImageFile - 55x55)
Convert-ToBmp -source $logoSource -dest "$AssetsDir\logo_light.bmp" -width 55 -height 55

Write-Host "Light theme assets generated successfully."
