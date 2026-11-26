param([Parameter(Mandatory)][string]$Directory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$utf8=[Text.UTF8Encoding]::new($false)
$snapshots=@(Get-ChildItem -LiteralPath $Directory -Filter 'turn-*.json' | Sort-Object Name | ForEach-Object {Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json -AsHashtable})
if(!$snapshots.Count){throw 'No snapshots'}
$daily=@(Get-Content -LiteralPath (Join-Path $Directory 'daily.jsonl') | ForEach-Object {ConvertFrom-Json -InputObject $_ -AsHashtable})
$font=[Drawing.Font]::new('Yu Gothic',13)
$small=[Drawing.Font]::new('Yu Gothic',10)
$title=[Drawing.Font]::new('Yu Gothic',24,[Drawing.FontStyle]::Bold)
function Paint-Text($graphics,$text,$x,$y,$fontValue,$color){
    $brush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($color))
    $graphics.DrawString([string]$text,$fontValue,$brush,[single]$x,[single]$y)
    $brush.Dispose()
}
function Fill-Box($graphics,$x,$y,$width,$height,$color){
    $brush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($color))
    $graphics.FillRectangle($brush,[single]$x,[single]$y,[single]$width,[single]$height);$brush.Dispose()
}
function Line($graphics,$x,$y,$xx,$yy,$color,$width=1){
    $pen=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml($color),[single]$width)
    $graphics.DrawLine($pen,[single]$x,[single]$y,[single]$xx,[single]$yy);$pen.Dispose()
}
$faceNames=@('+X 面','−X 面','+Y 面（上）','−Y 面（下）','+Z 面','−Z 面')
$terrainColors=@{Sea='#244456';Land='#3c514c';Highland='#535269';Mountain='#535269';Ice='#718792';Coast='#6b6451';Hill='#4c5652';Grassland='#3c514c'}
foreach($s in $snapshots){
    $bitmap=[Drawing.Bitmap]::new(1600,1120)
    $g=[Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([Drawing.ColorTranslator]::FromHtml('#101f2b'))
    Paint-Text $g ("全体ターン {0}　｜　軍用 {1} 地点 ／ 民間開放 {2} 地点" -f $s.turn,$s.military,$s.civilian) 28 17 $title '#f4e6c5'
    Paint-Text $g ("SEED {0} ・ 六面を個別表示（面をまたぐ辺は省略）／ 地域内は元の10×10地形" -f $s.seed) 30 65 $font '#a7bfca'
    $positions=@{};$nodes=@{};$countries=@{}
    foreach($c in $s.countries){$countries[[int]$c.id]=$c}
    foreach($n in $s.nodes){$nodes[[int]$n.Id]=$n}
    for($face=0;$face -lt 6;$face++){
        $cells=@($s.cells | Where-Object {$_['Face'] -eq $face})
        $left=25+($face%3)*525;$top=110+[math]::Floor($face/3)*440
        Fill-Box $g $left $top 510 425 '#182c3b'
        Paint-Text $g $faceNames[$face] ($left+12) ($top+7) $font '#f4e6c5'
        $cols=1+($cells.X|Measure-Object -Maximum).Maximum;$rows=1+($cells.Y|Measure-Object -Maximum).Maximum
        $unit=[math]::Min(478/$cols,365/$rows)
        $ox=$left+16+(478-$unit*$cols)/2;$oy=$top+43+(365-$unit*$rows)/2
        foreach($c in $cells){
            $cx=$ox+$c.X*$unit;$cy=$oy+$c.Y*$unit
            for($i=0;$i -lt 100;$i++){
                $color=$terrainColors[[string]$c.terrain[$i]];if(!$color){$color='#455851'}
                Fill-Box $g ($cx+($i%10)*$unit/10) ($cy+[math]::Floor($i/10)*$unit/10) ($unit/10+.3) ($unit/10+.3) $color
            }
            Line $g $cx $cy ($cx+$unit) $cy '#223b48'
            Line $g $cx $cy $cx ($cy+$unit) '#223b48'
            foreach($n in @($s.nodes|Where-Object {$_['Cell'] -eq $c.Id})){
                $positions[[int]$n.Id]=@(($cx+($n.x+.5)*$unit/10),($cy+($n.y+.5)*$unit/10),$face)
            }
        }
    }
    foreach($e in $s.edges){
        $a=$positions[[int]$e.source];$b=$positions[[int]$e.target]
        if(!$a -or !$b -or $a[2] -ne $b[2]){continue}
        $na=$nodes[[int]$e.source];$nb=$nodes[[int]$e.target]
        $color=if($na.root -ge 0 -and $nb.root -ge 0 -and $na.root -ne $nb.root){'#b86a62'}else{'#6d8185'}
        Line $g $a[0] $a[1] $b[0] $b[1] $color 1.2
    }
    foreach($n in $s.nodes){
        $p=$positions[[int]$n.Id];if(!$p){continue}
        if(!$n.Functional){
            Line $g ($p[0]-3) ($p[1]-3) ($p[0]+3) ($p[1]+3) '#97a7ac' 1.6
            Line $g ($p[0]-3) ($p[1]+3) ($p[0]+3) ($p[1]-3) '#97a7ac' 1.6
            continue
        }
        $color=if($n.root -ge 0){$countries[[int]$n.root].color}else{'#a1a7ab'}
        $brush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($color))
        $pen=[Drawing.Pen]::new([Drawing.Color]::White,1.1)
        $radius=if($n.IsHarbor -or $n.HasFinalDisposal){6}else{4.5}
        $g.FillEllipse($brush,[single]($p[0]-$radius),[single]($p[1]-$radius),[single]($radius*2),[single]($radius*2))
        $g.DrawEllipse($pen,[single]($p[0]-$radius),[single]($p[1]-$radius),[single]($radius*2),[single]($radius*2))
        if($n.purpose -eq 'Defense'){Line $g ($p[0]-7) ($p[1]-7) ($p[0]+7) ($p[1]-7) '#ffffff' 2.5}
        if($n.purpose -eq 'Relay'){Line $g ($p[0]-6) ($p[1]+7) ($p[0]+6) ($p[1]+7) '#7be1e1' 2}
        $brush.Dispose();$pen.Dispose()
    }
    Paint-Text $g '● 軍用地点：根のボスの色　 × 民間開放地点　 ━ 上線：城壁 ／ 下線：中継　 大きな丸：港・最終処分場' 30 1000 $font '#d2e0e4'
    $ranked=@($s.ranking|Where-Object {$_['Cells'] -gt 0}|Select-Object -First 8)
    $x=30
    foreach($r in $ranked){$c=$countries[[int]$r.Ruler];Fill-Box $g $x 1040 15 15 $c.color;Paint-Text $g ("#{0}：{1}地域" -f (1+$r.Ruler),$r.Cells) ($x+20) 1036 $small '#e6eff0';$x+=190}
    Paint-Text $g '解析用の図。ゲーム画面のスクリーンショットではありません。各面の向きはローカル座標です。' 30 1080 $small '#a7bfca'
    $path=Join-Path $Directory ('turn-{0:D4}.png' -f $s.turn)
    $bitmap.Save($path,[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bitmap.Dispose()
}
$font.Dispose();$small.Dispose();$title.Dispose()
$payload=@{snapshots=$snapshots;daily=$daily;metadata=(Get-Content (Join-Path $Directory 'metadata.json') -Raw|ConvertFrom-Json -AsHashtable)}|ConvertTo-Json -Depth 25 -Compress
$payload=$payload.Replace('<','\u003c')
$template=@'
<!doctype html><html lang="ja"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>今日のピックアップ・ゲーム｜FolderVerse</title>
<style>
:root{color-scheme:dark;font-family:system-ui,"Yu Gothic",sans-serif;background:#101f2b;color:#e6eff0}body{max-width:1500px;margin:auto;padding:28px}h1{color:#f4e6c5;margin-bottom:8px}p{line-height:1.7}small,.muted{color:#a7bfca}button,a,input{accent-color:#4eafa8}button{background:#254352;color:inherit;border:1px solid #58818e;border-radius:8px;padding:10px 18px;cursor:pointer}button:hover,button.active{background:#397975}a{color:#99ded5}.toolbar{position:sticky;top:0;background:#101f2bf2;padding:14px 0;z-index:2;display:flex;align-items:center;gap:12px;flex-wrap:wrap}input{flex:1;min-width:150px}.grid{display:grid;grid-template-columns:minmax(0,3fr) minmax(260px,1fr);gap:22px}.card{background:#182c3b;padding:18px;border-radius:12px;margin:16px 0}#map{width:100%;border-radius:10px}table{width:100%;border-collapse:collapse;font-size:14px}td,th{padding:8px 4px;border-bottom:1px solid #304957;text-align:right}td:nth-child(2){text-align:left}.swatch{display:inline-block;width:12px;height:12px;margin-right:6px}.thumbnails{display:flex;overflow:auto;gap:12px;padding-bottom:12px}.thumb{min-width:200px}.thumb img{width:180px;display:block}svg{width:100%;height:auto}.notice{border-left:4px solid #d3ae62;padding-left:16px}#statistics{font-size:20px}.gallery img{width:100%}@media(max-width:900px){.grid{display:block}body{padding:12px}}@media print{.toolbar,.thumbnails{display:none}.grid{display:block}.card{break-inside:avoid}}
</style>
<h1>今日のピックアップ・ゲーム</h1><p id="subtitle"></p>
<p class="notice">診断CPU候補の試行です。ゲーム本体へ採用済みの結果ではありません。20全体ターンごとと初期・決着を保存しています。地図は実データから作った解析用六面図です。面をまたぐ交通路は省略し、戦闘・移動の全イベントを描いてはいません。</p>
<div class="toolbar"><button id="prev">◀ 前</button><button id="play">▶ 再生</button><input id="slider" type="range" min="0" value="0"><button id="next">次 ▶</button><strong id="turn"></strong></div>
<div class="grid"><main><a id="imageLink" target="_blank"><img id="map" alt="全体ターンごとの六面地図"></a><div id="statistics" class="card"></div></main><aside><div class="card"><h2>陣営の順位</h2><p class="muted">地域数はゲーム本来の陣営順位判定。色は根のボス。番号はエントリーナンバー。</p><table><thead><tr><th>順位</th><th>征服者</th><th>地域</th></tr></thead><tbody id="ranking"></tbody></table></div><div class="card"><h2>記録を開く</h2><p><a id="save">この時点のセーブ</a></p><p><a id="json">この時点の生データ</a></p><p><a href="metadata.json">SEED・DLLハッシュ</a> ／ <a href="daily.jsonl">日次集計</a></p></div></aside></div>
<h2>20ターンずつ見返す</h2><div id="thumbnails" class="thumbnails"></div>
<div class="card"><h2>軍用地点の減少</h2><p>全地点のうち軍用地点として残っている数。領土の所有変更と、施設移転による地点の減少を区別して見てください。</p><svg id="chart" viewBox="0 0 1100 240" role="img" aria-label="全体ターンと軍用地点数の折れ線グラフ"></svg><p id="chartSummary"></p></div>
<details class="card"><summary>選んだ時点の地点一覧（直接所有者・陣営・用途・格納庫）</summary><p>「空き」は表示しません。在庫件数と格納枠占有は異なる場合があるためです。</p><table><thead><tr><th>地点ID</th><th>直接所有／根ボス</th><th>状態・用途</th><th>開放容量</th><th>在庫件数</th><th>上限</th></tr></thead><tbody id="nodes"></tbody></table></details>
<details class="card"><summary>保存した全画像を並べる</summary><div id="gallery" class="gallery"></div></details>
<p class="muted">年月日：日本時間。全体ターンは内部の累計値を表示。初期は0。各記録は軍事解決後、未決着なら産業処理後。勝利時は追加産業処理を行いません。400打切り時は収束未判定で、敗北や征服400ターンとは扱いません。このHTMLはオフラインで開けます。</p>
<script id="data" type="application/json">__DATA__</script><script>
const D=JSON.parse(document.querySelector('#data').textContent),S=D.snapshots,$=s=>document.querySelector(s);let index=0,timer=null;
const elem=(tag,text)=>{const e=document.createElement(tag);if(text!==undefined)e.textContent=text;return e};
$('#subtitle').textContent=`${D.metadata.createdJst} JST 作成 ／ SEED ${S[0].seed} ／ ${S[0].cells.length}地域 ／ ${S[0].mode} ／ 原材料 ${S[0].raw?'有効':'無効'}`;
$('#slider').max=S.length-1;
function show(i){index=Math.max(0,Math.min(S.length-1,i));let s=S[index],stem=`turn-${String(s.turn).padStart(4,'0')}`;$('#slider').value=index;$('#turn').textContent=`全体ターン ${s.turn}（${index+1}/${S.length}）`;$('#map').src=stem+'.png';$('#imageLink').href=stem+'.png';$('#save').href=stem+'.fvs';$('#json').href=stem+'.json';$('#statistics').textContent=`軍用 ${s.military} 地点　／　民間開放 ${s.civilian} 地点　／　初期から軍用 ${s.military-S[0].military} 地点`;
let countries=new Map(s.countries.map(c=>[c.id,c]));$('#ranking').replaceChildren();for(let r of s.ranking.filter(r=>r.Cells>0)){let c=countries.get(r.Ruler),tr=elem('tr');tr.append(elem('td',r.Rank));let td=elem('td'),span=elem('span');span.className='swatch';span.style.background=c.color;td.append(span,document.createTextNode(`#${c.entry} ${c.name}`));tr.append(td,elem('td',r.Cells));$('#ranking').append(tr)}
$('#nodes').replaceChildren();for(let n of s.nodes){let tr=elem('tr');tr.append(elem('td',n.Id),elem('td',n.Owner<0?'未所属':`#${n.Owner+1} ／ #${n.root+1}`),elem('td',n.Functional?(n.IsHarbor?'港':n.HasFinalDisposal?'最終処分場':({General:'自由用途',Harvest:'採取',Manufacturing:'製造',Relay:'中継',Defense:'城壁',Frontline:'詰所'}[n.purpose]||n.purpose)):'民間開放'),elem('td',n.capacity),elem('td',n.stock),elem('td',n.limit));$('#nodes').append(tr)}document.querySelectorAll('.thumb').forEach((b,j)=>b.classList.toggle('active',j===index))}
S.forEach((s,i)=>{let b=elem('button',`全体ターン ${s.turn}`),im=elem('img');im.src=`turn-${String(s.turn).padStart(4,'0')}.png`;im.alt=`全体ターン${s.turn}`;b.prepend(im);b.className='thumb';b.onclick=()=>show(i);$('#thumbnails').append(b);let fig=elem('figure'),caption=elem('figcaption',`全体ターン ${s.turn} ／ 軍用${s.military}`);fig.append(im.cloneNode(),caption);$('#gallery').append(fig)});
$('#prev').onclick=()=>show(index-1);$('#next').onclick=()=>show(index+1);$('#slider').oninput=e=>show(+e.target.value);function stop(){clearInterval(timer);timer=null;$('#play').textContent='▶ 再生'}$('#play').onclick=()=>{if(timer){stop();return}if(index===S.length-1)show(0);$('#play').textContent='■ 停止';timer=setInterval(()=>{if(index===S.length-1){stop();return}show(index+1)},1800)};
const svg=$('#chart'),NS='http://www.w3.org/2000/svg',add=(tag,attrs,text)=>{let e=document.createElementNS(NS,tag);for(let[k,v]of Object.entries(attrs))e.setAttribute(k,v);if(text!==undefined)e.textContent=text;svg.append(e)};
const end=D.daily.at(-1).turn,max=Math.max(...D.daily.map(x=>x.military)),x=t=>65+1000*t/Math.max(1,end),y=v=>200-170*v/max;
for(let i=0;i<=4;i++){let v=Math.round(max*i/4);add('line',{x1:65,y1:y(v),x2:1065,y2:y(v),stroke:'#304957'});add('text',{x:10,y:y(v)+5,fill:'#b7ccd4','font-size':15},v)}
add('polyline',{points:D.daily.map(d=>`${x(d.turn)},${y(d.military)}`).join(' '),fill:'none',stroke:'#69c6b3','stroke-width':3});for(let t=0;t<=end;t+=20)add('text',{x:x(t),y:225,fill:'#b7ccd4','font-size':13,'text-anchor':'middle'},t);
$('#chartSummary').textContent=`初期 ${S[0].military} → 最終 ${S.at(-1).military} 軍用地点（${S.at(-1).turn}全体ターン）。地域数 ${S[0].cells.length} は地点数とは別です。`;
show(0);
</script></html>
'@
[IO.File]::WriteAllText((Join-Path $Directory 'index.html'),$template.Replace('__DATA__',$payload),$utf8)
$manifest=@(Get-ChildItem -LiteralPath $Directory -File | Where-Object {$_.Extension -in @('.png','.fvs','.json','.html')} | ForEach-Object {@{name=$_.Name;length=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
[IO.File]::WriteAllText((Join-Path $Directory 'manifest.json'),($manifest|ConvertTo-Json),$utf8)
Write-Output ("Rendered {0} snapshots to {1}" -f $snapshots.Count,$Directory)
