$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskDir=$PSScriptRoot
$taskProgress=Get-Content -LiteralPath (Join-Path $taskDir 'generation-progress.json') -Raw | ConvertFrom-Json
$taskOut=Join-Path $taskDir 'qa-normal-v003'
[IO.Directory]::CreateDirectory($taskOut)|Out-Null
$taskSheet=[Drawing.Bitmap]::new(1024,1296)
$taskCanvas=[Drawing.Graphics]::FromImage($taskSheet)
$taskCanvas.Clear([Drawing.Color]::FromArgb(24,36,54))
$taskFont=[Drawing.Font]::new('Arial',12)
$taskLabels=@('Old runtime','Previous top crop','Scene crop v002','Circle diagnostic')
$taskCrops=(Get-Content -LiteralPath (Join-Path $taskDir 'normal-crops-v003.json') -Raw | ConvertFrom-Json).Images
for($taskI=0;$taskI -lt 6;$taskI++){
 $taskEntry=$taskProgress.Images | Where-Object {$_.Kind -eq 'normal' -and $_.Id -eq ($taskI+1)}
 $taskSrcName=if($taskI -eq 5){'character-47-normal-6-framing-v002-generated.png'}else{$taskEntry.Source}
 $taskSource=[Drawing.Bitmap]::FromFile((Join-Path $taskDir $taskSrcName))
 $taskOld=[Drawing.Bitmap]::FromFile((Join-Path $taskDir ('backup/v3-standardized/tiles/character-47-'+($taskI+1)+'.png')))
 for($taskMode=0;$taskMode -lt 4;$taskMode++){
  $taskTile=[Drawing.Bitmap]::new(256,192);$taskG=[Drawing.Graphics]::FromImage($taskTile);$taskG.Clear([Drawing.Color]::FromArgb(24,36,54));$taskG.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  if($taskMode -eq 0){$taskG.DrawImage($taskOld,0,0,256,192)}
  elseif($taskMode -eq 1){$taskPrev=[Drawing.Image]::FromFile((Join-Path $taskDir ('qa-normal-v001/normal-'+($taskI+1)+'-2.png')));$taskG.DrawImage($taskPrev,0,0,256,192);$taskPrev.Dispose()}
  else{
   if($taskMode -eq 3){$taskClip=[Drawing.Drawing2D.GraphicsPath]::new();$taskClip.AddEllipse(32,0,192,192);$taskG.SetClip($taskClip);$taskClip.Dispose()}
   $taskCrop=$taskCrops[$taskI]
   $taskG.DrawImage($taskSource,[Drawing.Rectangle]::new(0,0,256,192),[Drawing.Rectangle]::new($taskCrop.X,$taskCrop.Y,$taskCrop.Width,$taskCrop.Height),[Drawing.GraphicsUnit]::Pixel)
  }
  $taskG.Dispose();$taskName='normal-'+($taskI+1)+'-'+$taskMode+'.png';$taskTile.Save((Join-Path $taskOut $taskName));$taskCanvas.DrawImageUnscaled($taskTile,$taskMode*256,$taskI*216+24);$taskCanvas.DrawString((($taskI+1).ToString()+' '+$taskLabels[$taskMode]),$taskFont,[Drawing.Brushes]::White,$taskMode*256,$taskI*216);$taskTile.Dispose()
 }
 $taskSource.Dispose();$taskOld.Dispose()
}
$taskCanvas.Dispose();$taskSheet.Save((Join-Path $taskOut 'contact.png'));$taskSheet.Dispose();$taskFont.Dispose()
