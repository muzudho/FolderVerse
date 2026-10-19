$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskDir=$PSScriptRoot
$taskProgress=Get-Content -LiteralPath (Join-Path $taskDir 'generation-progress.json') -Raw | ConvertFrom-Json
$taskOut=Join-Path $taskDir 'qa-normal-v001'
[IO.Directory]::CreateDirectory($taskOut)|Out-Null
$taskSheet=[Drawing.Bitmap]::new(1024,1296)
$taskCanvas=[Drawing.Graphics]::FromImage($taskSheet)
$taskCanvas.Clear([Drawing.Color]::FromArgb(24,36,54))
$taskFont=[Drawing.Font]::new('Arial',12)
$taskLabels=@('Old runtime','Whole master fit','Top crop candidate','Circle diagnostic')
for($taskI=0;$taskI -lt 6;$taskI++){
 $taskEntry=$taskProgress.Images | Where-Object {$_.Kind -eq 'normal' -and $_.Id -eq ($taskI+1)}
 $taskSource=[Drawing.Bitmap]::FromFile((Join-Path $taskDir $taskEntry.Source))
 $taskOld=[Drawing.Bitmap]::FromFile((Join-Path $taskDir ('backup/v3-standardized/tiles/character-47-'+($taskI+1)+'.png')))
 for($taskMode=0;$taskMode -lt 4;$taskMode++){
  $taskTile=[Drawing.Bitmap]::new(256,192);$taskG=[Drawing.Graphics]::FromImage($taskTile);$taskG.Clear([Drawing.Color]::FromArgb(24,36,54));$taskG.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  if($taskMode -eq 0){$taskG.DrawImage($taskOld,0,0,256,192)}
  elseif($taskMode -eq 1){$taskScale=[Math]::Min(256/$taskSource.Width,192/$taskSource.Height);$taskW=[int]($taskSource.Width*$taskScale);$taskH=[int]($taskSource.Height*$taskScale);$taskG.DrawImage($taskSource,[int]((256-$taskW)/2),0,$taskW,$taskH)}
  else{
   if($taskMode -eq 3){$taskClip=[Drawing.Drawing2D.GraphicsPath]::new();$taskClip.AddEllipse(32,0,192,192);$taskG.SetClip($taskClip);$taskClip.Dispose()}
   $taskCropH=[single]($taskSource.Width*3/4)
   $taskG.DrawImage($taskSource,[Drawing.Rectangle]::new(0,0,256,192),[Drawing.Rectangle]::new(0,0,$taskSource.Width,[int]$taskCropH),[Drawing.GraphicsUnit]::Pixel)
  }
  $taskG.Dispose();$taskName='normal-'+($taskI+1)+'-'+$taskMode+'.png';$taskTile.Save((Join-Path $taskOut $taskName));$taskCanvas.DrawImageUnscaled($taskTile,$taskMode*256,$taskI*216+24);$taskCanvas.DrawString((($taskI+1).ToString()+' '+$taskLabels[$taskMode]),$taskFont,[Drawing.Brushes]::White,$taskMode*256,$taskI*216);$taskTile.Dispose()
 }
 $taskSource.Dispose();$taskOld.Dispose()
}
$taskCanvas.Dispose();$taskSheet.Save((Join-Path $taskOut 'contact.png'));$taskSheet.Dispose();$taskFont.Dispose()
