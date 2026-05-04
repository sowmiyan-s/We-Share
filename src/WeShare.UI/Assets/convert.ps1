$pngPath = 'd:\PROJECTS\SHARE IT\src\ShareIt.UI\Assets\logo.png'
$icoPath = 'd:\PROJECTS\SHARE IT\src\ShareIt.UI\Assets\logo.ico'
$pngBytes = [System.IO.File]::ReadAllBytes($pngPath)
$size = $pngBytes.Length
$icoHeader = [byte[]]@(0,0, 1,0, 1,0, 0,0, 0,0, 1,0, 32,0, ($size -band 0xFF), (($size -shr 8) -band 0xFF), (($size -shr 16) -band 0xFF), (($size -shr 24) -band 0xFF), 22,0,0,0)
$icoBytes = New-Object byte[] ($icoHeader.Length + $pngBytes.Length)
[Array]::Copy($icoHeader, 0, $icoBytes, 0, $icoHeader.Length)
[Array]::Copy($pngBytes, 0, $icoBytes, $icoHeader.Length, $pngBytes.Length)
[System.IO.File]::WriteAllBytes($icoPath, $icoBytes)
