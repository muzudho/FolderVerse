param([int]$Index,[string]$Source,[string]$Prompt)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$planPath=Join-Path $PSScriptRoot 'plan.json'
$plan=Get-Content -LiteralPath $planPath -Raw -Encoding utf8|ConvertFrom-Json
$item=$plan.Items[$Index]
if($item.Completed){throw 'Pose already complete'}
$target=Join-Path $PSScriptRoot ($item.Stem+'.png')
if(Test-Path -LiteralPath $target){throw 'Do not overwrite existing pose'}
Copy-Item -LiteralPath $Source -Destination $target
[IO.File]::WriteAllText((Join-Path $PSScriptRoot ($item.Stem+'-prompt.txt')),$Prompt,[Text.UTF8Encoding]::new($false))
$im=[Drawing.Image]::FromFile($target)
try{$item|Add-Member Width $im.Width -Force;$item|Add-Member Height $im.Height -Force}finally{$im.Dispose()}
$item|Add-Member Sha256 (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -Force
$item|Add-Member Source $Source -Force
$item.Completed=$true
[IO.File]::WriteAllText($planPath,($plan|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
$spec=Join-Path $PSScriptRoot '../../../docs/【むずでょ個人用】/作品の世界観設定/モブ女素体_世界観設定.md'
$content=[IO.File]::ReadAllText($spec).Replace('* [ ] '+$item.Name,'* [x] '+$item.Name)
[IO.File]::WriteAllText($spec,$content,[Text.UTF8Encoding]::new($false))
$cards=foreach($p in $plan.Items){$name=[Net.WebUtility]::HtmlEncode($p.Name);if($p.Completed){"<article><h2>$name</h2><a href='$($p.Stem).png'><img loading='lazy' src='$($p.Stem).png'></a></article>"}else{"<article><h2>$name</h2><p>未制作</p></article>"}}
$count=@($plan.Items|Where-Object Completed).Count
$html="<!doctype html><html lang='ja'><meta charset='utf-8'><title>モブ女全身ポーズ</title><style>body{font-family:sans-serif;background:#eee;margin:24px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:16px}article{background:white;padding:16px}img{width:100%;height:440px;object-fit:contain}h2{font-size:16px}</style><h1>モブ女全身ポーズ $count/$($plan.Items.Count)</h1><p>白背景の原画。頭から足先までを確認。衣服マスク・ゲームへの追加取り込みは未実施。</p><main>"+($cards -join "`r`n")+'</main></html>'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'index.html'),$html,[Text.UTF8Encoding]::new($false))
"Saved $($item.Name); $count/$($plan.Items.Count)"
