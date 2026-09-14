Add-Type -AssemblyName System.Drawing
$inPath = (Resolve-Path "docs\logo.png").Path
$outPath = (Resolve-Path "docs").Path + "\logo-dark.png"

$img = [System.Drawing.Bitmap]::FromFile($inPath)
$newBmp = New-Object System.Drawing.Bitmap $img.Width, $img.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb

# Indigo #533afd: R=83, G=58, B=253
for ($y = 0; $y -lt $img.Height; $y++) {
    for ($x = 0; $x -lt $img.Width; $x++) {
        $p = $img.GetPixel($x, $y)
        if ($p.A -gt 0) {
            $c = [System.Drawing.Color]::FromArgb($p.A, 83, 58, 253)
            $newBmp.SetPixel($x, $y, $c)
        }
    }
}

$img.Dispose()
$newBmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
$newBmp.Dispose()
Write-Host "Created $outPath successfully!"
