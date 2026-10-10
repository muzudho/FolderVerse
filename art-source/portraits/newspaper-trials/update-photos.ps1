param([switch]$ImportGame)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$items=@()
foreach($id in 1..60){foreach($kind in @('formal','paparazzi','friends','id')){
    $stem='character-{0:D2}-{1}-v001' -f $id,$kind
    $file="$stem.png"
    if($kind -eq 'formal' -and $id -in @(1,7,36)){$file='character-{0:D2}-newspaper-v002.png' -f $id}
    $source=Join-Path $PSScriptRoot $file
    if(!(Test-Path -LiteralPath $source)){continue}
    $image=[Drawing.Image]::FromFile($source)
    try{
        if($image.Width -ne 1200 -or $image.Height -ne 1600){throw "Unexpected size $file"}
        if($ImportGame){
            $runtime=Join-Path $PSScriptRoot '../../../FolderVerse/Content/Images/Portraits/newspaper'
            [IO.Directory]::CreateDirectory($runtime)|Out-Null
            $target=Join-Path $runtime ('character-{0:D2}-{1}.png' -f $id,$kind)
            $bitmap=[Drawing.Bitmap]::new(300,400)
            try{$g=[Drawing.Graphics]::FromImage($bitmap);try{$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.DrawImage($image,[Drawing.Rectangle]::new(0,0,300,400))}finally{$g.Dispose()};$bitmap.Save($target,[Drawing.Imaging.ImageFormat]::Png)}finally{$bitmap.Dispose()}
        }
    }finally{$image.Dispose()}
    $items+=[pscustomobject]@{CharacterId=$id;Kind=$kind;File=$file;Width=1200;Height=1600;Sha256=(Get-FileHash -LiteralPath $source).Hash;Reviewed=$true}
}}
$progress=[pscustomobject]@{Total=240;Completed=$items.Count;Photos=$items}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'all-photos-progress.json'),($progress|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
"Verified $($items.Count)/240 photos; game import=$ImportGame"
