Add-Type -AssemblyName System.Drawing

$W = 1024; $H = 500
$bmp = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Diagonal teal gradient background matching the icon's palette
$colorTopLeft = [System.Drawing.Color]::FromArgb(255, 12, 90, 74)
$colorBottomRight = [System.Drawing.Color]::FromArgb(255, 5, 46, 40)
$rect = New-Object System.Drawing.Rectangle 0, 0, $W, $H
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $colorTopLeft, $colorBottomRight, 45)
$g.FillRectangle($brush, $rect)

# Faint decorative ring echoing the icon's circle-of-dots motif, bleeding off the right edge
$ringPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(40, 255, 255, 255), 3)
$g.DrawEllipse($ringPen, 700, -180, 620, 620)
$dotBrushFaint = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(55, 255, 255, 255))
$ringDots = @(
  @(1010, 130), @(1290, 130), @(1150, -50), @(1150, 310), @(870, 130)
)
foreach ($d in $ringDots) {
  $g.FillEllipse($dotBrushFaint, $d[0]-14, $d[1]-14, 28, 28)
}

# App icon, left side, vertically centered
$iconSize = 320
$iconPath = "D:\workspace\AI\POC\Dourak\icons\app-icon-512.png"
$icon = [System.Drawing.Image]::FromFile($iconPath)
$iconX = 70
$iconY = [int](($H - $iconSize) / 2)
$g.DrawImage($icon, $iconX, $iconY, $iconSize, $iconSize)
$icon.Dispose()

# Wordmark + tagline, right of the icon
$textX = $iconX + $iconSize + 55
$fontFamily = "Segoe UI"
$titleFont = New-Object System.Drawing.Font($fontFamily, 76, [System.Drawing.FontStyle]::Bold)
$titleBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$g.DrawString("Dourak", $titleFont, $titleBrush, $textX, 150)

$goldPen = [System.Drawing.Color]::FromArgb(255, 244, 180, 60)
$ruleBrush = New-Object System.Drawing.SolidBrush($goldPen)
$g.FillRectangle($ruleBrush, $textX, 250, 90, 6)

$arabicFont = New-Object System.Drawing.Font($fontFamily, 36, [System.Drawing.FontStyle]::Bold)
$arabicBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(240, 255, 255, 255))
$rtlFormat = New-Object System.Drawing.StringFormat
$rtlFormat.FormatFlags = [System.Drawing.StringFormatFlags]::DirectionRightToLeft
$arabicText = [char]::ConvertFromUtf32(0x0627) + [char]::ConvertFromUtf32(0x062C) + [char]::ConvertFromUtf32(0x0639) + [char]::ConvertFromUtf32(0x0644) + [char]::ConvertFromUtf32(0x0020) + [char]::ConvertFromUtf32(0x062C) + [char]::ConvertFromUtf32(0x0645) + [char]::ConvertFromUtf32(0x0639) + [char]::ConvertFromUtf32(0x064A) + [char]::ConvertFromUtf32(0x0627) + [char]::ConvertFromUtf32(0x062A) + [char]::ConvertFromUtf32(0x0643) + [char]::ConvertFromUtf32(0x0020) + [char]::ConvertFromUtf32(0x0645) + [char]::ConvertFromUtf32(0x0646) + [char]::ConvertFromUtf32(0x0638) + [char]::ConvertFromUtf32(0x0645) + [char]::ConvertFromUtf32(0x0629)
$arabicWidth = 480
$g.DrawString($arabicText, $arabicFont, $arabicBrush, [System.Drawing.RectangleF]::new($textX, 280, $arabicWidth, 60), $rtlFormat)

$taglineFont = New-Object System.Drawing.Font($fontFamily, 20, [System.Drawing.FontStyle]::Italic)
$taglineBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(150, 255, 255, 255))
$g.DrawString("Savings circles, organized", $taglineFont, $taglineBrush, $textX, 350)

$g.Flush()
$outPath = "D:\workspace\AI\POC\Dourak\icons\feature-graphic-1024x500.png"
$bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$bmp.Dispose()
"Saved: $outPath"
