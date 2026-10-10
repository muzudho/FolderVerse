$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$m=Get-Content (Join-Path $PSScriptRoot 'remaining-progress.json') -Raw | ConvertFrom-Json
$catalog=Get-Content (Join-Path $PSScriptRoot '../characters/catalog.json') -Raw | ConvertFrom-Json
$keys=@{}
$cards=foreach($p in ($m.Portraits|Sort-Object CharacterId,Variant)){
    $key="$($p.CharacterId)-$($p.Variant)"
    if($keys.ContainsKey($key)){throw "Duplicate $key"}; $keys[$key]=$true
    $path=Join-Path $PSScriptRoot $p.File
    if((Get-FileHash -LiteralPath $path).Hash -ne $p.Sha256){throw "SHA256 mismatch $key"}
    $im=[Drawing.Image]::FromFile($path)
    try {if($im.Width*3 -ne $im.Height*4){throw "Not 4:3: $key"}} finally {$im.Dispose()}
    $original=@($catalog|Where-Object {$_.Current -and $_.CharacterId -eq $p.CharacterId -and $_.Variant -eq $p.Variant})
    if($original.Count -ne 1){throw "Invalid catalog entry $key"}
    $note=[Net.WebUtility]::HtmlEncode($p.Note)
    "<article><h2>No.$($p.CharacterId) / 差分$($p.Variant)</h2><div class='pair'><figure><img loading='lazy' src='../characters/$($original[0].File)'><figcaption>元画像</figcaption></figure><figure><a href='$($p.File)'><img loading='lazy' src='$($p.File)'></a><figcaption>調整版</figcaption></figure></div><p>$note</p></article>"
}
if($m.Completed -ne $m.Portraits.Count){throw 'Progress count mismatch'}
$html='<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>差分画像の調整進捗</title><style>body{background:#111827;color:#eef2ff;font-family:system-ui;margin:24px}h1{font-size:24px}article{background:#1e293b;padding:16px;margin:20px 0;border-radius:12px}h2{font-size:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;aspect-ratio:4/3;object-fit:contain;border-radius:6px}figcaption{margin-top:6px;color:#cbd5e1}p{color:#94a3b8}@media(max-width:700px){.pair{grid-template-columns:1fr}}</style><h1>征服者作成用：差分調整 '+$m.Completed+'/300枚</h1><p>元画像と内政フェーズ用画像は保存。No.7：室内はラフな服、屋外は緑と金のマジックローブ。</p>'+($cards -join "`r`n")+'</html>'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'remaining-gallery.html'),$html,[Text.UTF8Encoding]::new($false))
"Verified $($m.Completed)/$($m.Total) images, SHA256, 4:3, unique keys; gallery updated."
