$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskDir=$PSScriptRoot
$taskOut=Join-Path $taskDir 'qa-newspaper-v002'
[IO.Directory]::CreateDirectory($taskOut)|Out-Null
$taskJobs=(Get-Content (Join-Path $taskDir 'newspaper-crops-v002.json') -Raw | ConvertFrom-Json).Images
$taskSheet=[Drawing.Bitmap]::new(1200,848);$taskCanvas=[Drawing.Graphics]::FromImage($taskSheet);$taskCanvas.Clear([Drawing.Color]::FromArgb(24,36,54));$taskFont=[Drawing.Font]::new('Arial',12)
$taskI=0
foreach($taskJob in $taskJobs){
 $taskSource=[Drawing.Bitmap]::FromFile((Join-Path $taskDir $taskJob.Source));$taskTile=[Drawing.Bitmap]::new(300,400);$taskG=[Drawing.Graphics]::FromImage($taskTile);$taskG.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $taskG.DrawImage($taskSource,[Drawing.Rectangle]::new(0,0,300,400),[Drawing.Rectangle]::new($taskJob.X,$taskJob.Y,$taskJob.Width,$taskJob.Height),[Drawing.GraphicsUnit]::Pixel);$taskG.Dispose();$taskSource.Dispose();$taskTile.Save((Join-Path $taskOut ($taskJob.Id+'-color.png')))
 for($taskY=0;$taskY -lt 400;$taskY++){for($taskX=0;$taskX -lt 300;$taskX++){$taskC=$taskTile.GetPixel($taskX,$taskY);$taskGray=[int](0.299*$taskC.R+0.587*$taskC.G+0.114*$taskC.B);$taskTile.SetPixel($taskX,$taskY,[Drawing.Color]::FromArgb(255,$taskGray,$taskGray,$taskGray))}}
 $taskTile.Save((Join-Path $taskOut ($taskJob.Id+'-gray.png')));$taskPX=($taskI%4)*300;$taskPY=[int][Math]::Floor($taskI/4)*424;$taskCanvas.DrawImageUnscaled($taskTile,$taskPX,$taskPY+24);$taskCanvas.DrawString($taskJob.Id,$taskFont,[Drawing.Brushes]::White,$taskPX,$taskPY);$taskTile.Dispose();$taskI++
}
$taskCanvas.Dispose();$taskSheet.Save((Join-Path $taskOut 'contact-gray.png'));$taskSheet.Dispose();$taskFont.Dispose()
