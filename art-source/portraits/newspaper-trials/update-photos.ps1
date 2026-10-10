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
$labels=@{formal='式典・記念撮影';paparazzi='パパラッチ';friends='家族・友達';id='証明写真'}
$html='<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>征服者の顔写真帳</title><style>body{background:#d8cdb8;color:#332c25;font-family:Georgia,"Yu Mincho",serif;margin:0}main{max-width:1440px;margin:24px auto;padding:28px;background:#f3e9d6}header{text-align:center;border-block:4px double #564837;padding:16px}button{padding:12px;background:#fff6e5;border:1px solid #65523c;cursor:pointer}section{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:18px}img{width:100%;aspect-ratio:3/4;display:block}body.mono img{filter:grayscale(1)}h2{border-bottom:2px solid #65523c;padding-bottom:12px;margin-top:36px}h3{font-size:18px}.pending{aspect-ratio:3/4;display:grid;place-items:center;background:#e9ddc7;color:#8b7a60}@media(max-width:800px){section{grid-template-columns:repeat(2,minmax(0,1fr))}main{margin:8px;padding:16px}}</style><main><header><h1>征服者の顔写真帳</h1><p>全60人 × 4種類 / 1200 × 1600 pixels / 完成 '+$items.Count+' / 240 枚</p><p>クリックで原寸表示。撮影場面は試作の設定案。</p><button onclick="document.body.classList.toggle(''mono'')">カラー／モノクロ表示</button></header>'
foreach($group in ($items|Group-Object CharacterId)){
    $html+='<h2>No.'+$group.Name+'</h2><section>'
    foreach($kind in @('formal','paparazzi','friends','id')){
        $entry=$group.Group|Where-Object Kind -eq $kind|Select-Object -First 1
        $html+='<article><h3>'+$labels[$kind]+'</h3>'
        if($entry){$html+='<a href="'+$entry.File+'"><img loading="lazy" src="'+$entry.File+'" alt="No.'+$group.Name+' '+$labels[$kind]+'"></a>'}
        else{$html+='<div class="pending">制作待ち</div>'}
        $html+='</article>'
    }
    $html+='</section>'
}
$html+='</main></html>'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'index.html'),$html,[Text.UTF8Encoding]::new($false))
"Verified $($items.Count)/240 photos; game import=$ImportGame"
