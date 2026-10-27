Add-Type -AssemblyName System.Drawing

function Draw-PrintIcon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $size / 256.0

    # Background rounded rectangle (Dark Indigo / Royal Blue Gradient)
    $bgRect = New-Object System.Drawing.RectangleF(8 * $scale, 8 * $scale, 240 * $scale, 240 * $scale)
    $gradBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $bgRect,
        [System.Drawing.Color]::FromArgb(255, 30, 58, 138),  # Deep Blue #1E3A8A
        [System.Drawing.Color]::FromArgb(255, 14, 116, 144), # Cyan-Blue #0E7490
        45.0
    )
    
    # Path for rounded rectangle
    $radius = 36 * $scale
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $r = $bgRect
    $path.AddArc($r.X, $r.Y, $d, $d, 180, 90)
    $path.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $path.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $g.FillPath($gradBrush, $path)

    # Subtle inner border
    $borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 255, 255, 255), [Math]::Max(1, 2 * $scale))
    $g.DrawPath($borderPen, $path)

    # Wi-Fi waves on top
    $wavePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(240, 56, 189, 248), [Math]::Max(2, 6 * $scale)) # Cyan #38BDF8
    $wavePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $wavePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    
    # Wave 1 (outer)
    $g.DrawArc($wavePen, [float](76 * $scale), [float](28 * $scale), [float](104 * $scale), [float](60 * $scale), 200, 140)
    # Wave 2 (middle)
    $g.DrawArc($wavePen, [float](96 * $scale), [float](44 * $scale), [float](64 * $scale), [float](40 * $scale), 200, 140)
    # Dot (center)
    $dotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $g.FillEllipse($dotBrush, [float](122 * $scale), [float](68 * $scale), [float](12 * $scale), [float](12 * $scale))

    # Paper feeding from top
    $paperBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 241, 245, 249))
    $paperPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $paperRect = New-Object System.Drawing.RectangleF(78 * $scale, 92 * $scale, 100 * $scale, 40 * $scale)
    $g.FillRectangle($paperBrush, $paperRect)
    # Subtle paper lines
    $paperLinePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 203, 213, 225), [Math]::Max(1, 2 * $scale))
    $g.DrawLine($paperLinePen, [float](90 * $scale), [float](104 * $scale), [float](166 * $scale), [float](104 * $scale))
    $g.DrawLine($paperLinePen, [float](90 * $scale), [float](116 * $scale), [float](150 * $scale), [float](116 * $scale))

    # Printer Body (Main base)
    $printerBodyBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $printerBodyRect = New-Object System.Drawing.RectangleF(52 * $scale, 126 * $scale, 152 * $scale, 70 * $scale)
    $pRadius = 12 * $scale
    $pPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pd = $pRadius * 2
    $pr = $printerBodyRect
    $pPath.AddArc($pr.X, $pr.Y, $pd, $pd, 180, 90)
    $pPath.AddArc($pr.Right - $pd, $pr.Y, $pd, $pd, 270, 90)
    $pPath.AddArc($pr.Right - $pd, $pr.Bottom - $pd, $pd, $pd, 0, 90)
    $pPath.AddArc($pr.X, $pr.Bottom - $pd, $pd, $pd, 90, 90)
    $pPath.CloseFigure()
    $g.FillPath($printerBodyBrush, $pPath)

    # Printer Slot / Output Tray
    $slotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 30, 41, 59)) # Slate 800
    $g.FillRectangle($slotBrush, [float](68 * $scale), [float](154 * $scale), [float](120 * $scale), [float](16 * $scale))

    # Printed Document coming out of slot
    $outPaperBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 226, 232, 240))
    $g.FillRectangle($outPaperBrush, [float](78 * $scale), [float](162 * $scale), [float](100 * $scale), [float](42 * $scale))
    $g.DrawLine($paperLinePen, [float](88 * $scale), [float](176 * $scale), [float](168 * $scale), [float](176 * $scale))
    $g.DrawLine($paperLinePen, [float](88 * $scale), [float](188 * $scale), [float](148 * $scale), [float](188 * $scale))

    # Status LED
    $ledBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 34, 197, 94)) # Emerald Green
    $g.FillEllipse($ledBrush, [float](182 * $scale), [float](138 * $scale), [float](8 * $scale), [float](8 * $scale))

    # Clean up graphics
    $g.Dispose()
    $gradBrush.Dispose()
    $borderPen.Dispose()
    $wavePen.Dispose()
    $dotBrush.Dispose()
    $paperBrush.Dispose()
    $paperLinePen.Dispose()
    $printerBodyBrush.Dispose()
    $slotBrush.Dispose()
    $outPaperBrush.Dispose()
    $ledBrush.Dispose()

    return $bmp
}

function Save-MultiSizeIco([string]$outPath, [int[]]$sizes) {
    $images = @()
    foreach ($s in $sizes) {
        $bmp = Draw-PrintIcon $s
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $bytes = $ms.ToArray()
        $images += @{ Size = $s; Bytes = $bytes }
        $bmp.Dispose()
        $ms.Dispose()
    }

    $fs = [System.IO.File]::Create($outPath)
    $bw = New-Object System.IO.BinaryWriter($fs)

    # ICONDIR header
    $bw.Write([uint16]0) # Reserved
    $bw.Write([uint16]1) # Type 1 = Icon
    $bw.Write([uint16]$images.Count) # Count

    # Calculate offset
    $offset = 6 + ($images.Count * 16)

    # ICONDIRENTRY for each image
    foreach ($img in $images) {
        $w = if ($img.Size -ge 256) { 0 } else { [byte]$img.Size }
        $h = if ($img.Size -ge 256) { 0 } else { [byte]$img.Size }
        $bw.Write([byte]$w)
        $bw.Write([byte]$h)
        $bw.Write([byte]0) # Color count
        $bw.Write([byte]0) # Reserved
        $bw.Write([uint16]1) # Color planes
        $bw.Write([uint16]32) # Bit count
        $bw.Write([uint32]$img.Bytes.Length)
        $bw.Write([uint32]$offset)
        $offset += $img.Bytes.Length
    }

    # Write image data
    foreach ($img in $images) {
        $bw.Write($img.Bytes)
    }

    $bw.Flush()
    $bw.Close()
    $fs.Close()
    Write-Host "Created multi-size icon: $outPath"
}

# Generate App and Installer Icons
$icoPath = "src\KKR.PrintServer.App\Assets\app.ico"
Save-MultiSizeIco $icoPath @(256, 128, 64, 48, 32, 16)
Copy-Item $icoPath -Destination "installer\app.ico" -Force

# Generate Inno Setup Wizard Large Banner (164x314)
$wizBmp = New-Object System.Drawing.Bitmap(164, 314, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$gw = [System.Drawing.Graphics]::FromImage($wizBmp)
$gw.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gw.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

$wRect = New-Object System.Drawing.Rectangle(0, 0, 164, 314)
$wBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $wRect,
    [System.Drawing.Color]::FromArgb(255, 15, 23, 42),  # Deep Slate #0F172A
    [System.Drawing.Color]::FromArgb(255, 30, 58, 138), # Indigo Blue #1E3A8A
    90.0
)
$gw.FillRectangle($wBrush, $wRect)

# Draw central icon in banner
$iconBmp = Draw-PrintIcon 96
$gw.DrawImage($iconBmp, 34, 40, 96, 96)
$iconBmp.Dispose()

# Draw Title in banner
$titleFont = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$subFont = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Regular)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$cyanBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 56, 189, 248))

$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$gw.DrawString("KKR Wireless Print", $titleFont, $whiteBrush, 82, 150, $sf)
$gw.DrawString("Windows Print Server", $subFont, $cyanBrush, 82, 175, $sf)
$gw.DrawString("High-Speed Wireless`nPrinting for Mobile & PC", $subFont, $whiteBrush, 82, 215, $sf)

$gw.Dispose()
$wizBmp.Save("installer\wizard_large.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$wizBmp.Dispose()
Write-Host "Created installer\wizard_large.bmp"

# Generate Inno Setup Wizard Small Image (55x58)
$smBmp = New-Object System.Drawing.Bitmap(55, 58, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$gsm = [System.Drawing.Graphics]::FromImage($smBmp)
$gsm.Clear([System.Drawing.Color]::White)
$smIcon = Draw-PrintIcon 48
$gsm.DrawImage($smIcon, 3, 5, 48, 48)
$smIcon.Dispose()
$gsm.Dispose()
$smBmp.Save("installer\wizard_small.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$smBmp.Dispose()
Write-Host "Created installer\wizard_small.bmp"
