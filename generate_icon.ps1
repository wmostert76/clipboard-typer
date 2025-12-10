Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)

# Enable high quality rendering
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# Draw Background (Dark Blue/Gray rounded square)
$rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(40, 44, 52)) # Dark theme background
$g.FillRectangle($brush, $rect)

# Draw Border
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(97, 175, 239), 10) # Blue border
$g.DrawRectangle($pen, $rect)

# Draw Text "CT"
$fontFamily = "Consolas"
$fontSize = 100
$font = New-Object System.Drawing.Font($fontFamily, $fontSize, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

$stringFormat = New-Object System.Drawing.StringFormat
$stringFormat.Alignment = [System.Drawing.StringAlignment]::Center
$stringFormat.LineAlignment = [System.Drawing.StringAlignment]::Center

$rectF = New-Object System.Drawing.RectangleF(0, 0, $size, $size)
$g.DrawString("CT", $font, $textBrush, $rectF, $stringFormat)

# Convert to Icon
$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)

# Save to file
$fs = New-Object System.IO.FileStream("app.ico", [System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()

# Cleanup
$icon.Dispose()
$textBrush.Dispose()
$font.Dispose()
$pen.Dispose()
$brush.Dispose()
$g.Dispose()
$bmp.Dispose()
Write-Host "Generated app.ico"
