$DesignDir = "$PSScriptRoot\.."
$AssetsDir = "$PSScriptRoot\..\.."

$pngPath = "$DesignDir\app_icon_gradient.png"
$icoPath = "$AssetsDir\logo.ico"
$logoPngPath = "$AssetsDir\logo.png"

# Copy PNG to logo.png
Copy-Item $pngPath $logoPngPath -Force
Write-Host "Updated logo.png"

# Create ICO from PNG (Simple PNG-in-ICO format)
$pngBytes = [System.IO.File]::ReadAllBytes($pngPath)
$icoHeader = [byte[]](0, 0, 1, 0, 1, 0) # Reserved, Type (1), Count (1)
$icoDirEntry = [byte[]](0, 0, 0, 0, 1, 0, 32, 0) # Width (0=256), Height (0=256), Colors (0), Reserved, Planes (1), BPP (32)

$size = $pngBytes.Length
$sizeBytes = [System.BitConverter]::GetBytes($size)
$offset = 22 # 6 (header) + 16 (dir entry)
$offsetBytes = [System.BitConverter]::GetBytes($offset)

$icoFile = $icoHeader + $icoDirEntry + $sizeBytes + $offsetBytes + $pngBytes
[System.IO.File]::WriteAllBytes($icoPath, $icoFile)

# Update BMP for installer UI
Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile($pngPath)
$img.Save("$AssetsDir\logo.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$img.Dispose()
Write-Host "Updated logo.bmp"

