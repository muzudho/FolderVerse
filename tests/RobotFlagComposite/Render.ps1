param([Parameter(Mandatory=$true)][string]$Probe,[Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][string]$Output,[switch]$Paper,
      [ValidateSet('Head','Torso','Legs','Side','Back','TorsoSide','LegsSide','TorsoBack','LegsBack')][string]$PaperPart='Head',[switch]$Cardboard,
      [ValidateSet('Head','Torso','Legs','Side','Back','TorsoSide','LegsSide','TorsoBack','LegsBack')][string]$CardboardPart='Head',[switch]$Foam,[ValidateSet('Head','Torso','Legs','Side','Back')][string]$FoamPart='Head')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class RobotProbePng {
 public static void Decode(string source,string raw) {
  using(var image=new Bitmap(source)) using(var bitmap=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb)) {
   using(var g=Graphics.FromImage(bitmap))g.DrawImageUnscaled(image,0,0);
   var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   var bytes=new byte[bitmap.Width*bitmap.Height*4];
   for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,y*bitmap.Width*4,bitmap.Width*4);
   bitmap.UnlockBits(data);Swap(bytes);System.IO.File.WriteAllBytes(raw,bytes);
  }
 }
 static void Swap(byte[] b){for(int i=0;i<b.Length;i+=4){byte r=b[i];b[i]=b[i+2];b[i+2]=r;}}
 public static void Encode(string raw,string output,int width,int height) {
  var bytes=System.IO.File.ReadAllBytes(raw);Swap(bytes);
  using(var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
   var data=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
   for(int y=0;y<height;y++)Marshal.Copy(bytes,y*width*4,IntPtr.Add(data.Scan0,y*data.Stride),width*4);
   bitmap.UnlockBits(data);bitmap.Save(output,ImageFormat.Png);
  }
 }
}
'@
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$image=[Drawing.Bitmap]::new($Source);$width=$image.Width;$height=$image.Height;$image.Dispose()
$raw=Join-Path $Output 'neutral.rgba'
[RobotProbePng]::Decode($Source,$raw)
$mode=@()
if($Foam){$mode+=switch($FoamPart){'Torso'{'--foam-torso'} 'Legs'{'--foam-legs'} 'Side'{'--foam-side'} 'Back'{'--foam-back'} default{'--foam'}}}
elseif($Cardboard){
 $mode+=switch($CardboardPart){'Torso'{'--cardboard-torso'} 'Legs'{'--cardboard-legs'} 'Side'{'--cardboard-side'} 'Back'{'--cardboard-back'} 'TorsoSide'{'--cardboard-torso-side'} 'LegsSide'{'--cardboard-legs-side'} 'TorsoBack'{'--cardboard-torso-back'} 'LegsBack'{'--cardboard-legs-back'} default{'--cardboard'}}
}
elseif($Paper){
 $mode+=switch($PaperPart){'Torso'{'--paper-torso'} 'Legs'{'--paper-legs'} 'Side'{'--paper-side'} 'Back'{'--paper-back'} 'TorsoSide'{'--paper-torso-side'} 'LegsSide'{'--paper-legs-side'} 'TorsoBack'{'--paper-torso-back'} 'LegsBack'{'--paper-legs-back'} default{'--paper'}}
}
& dotnet $Probe $raw $width $height $Output @mode
if($LASTEXITCODE -ne 0){throw 'Composite rendering checks failed'}
foreach($i in 0,1){[RobotProbePng]::Encode((Join-Path $Output "head-$i.rgba"),(Join-Path $Output "head-$i.png"),$width,$height)}
