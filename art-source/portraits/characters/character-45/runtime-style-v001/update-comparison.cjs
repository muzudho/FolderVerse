const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=__dirname;
if(fs.existsSync(path.join(root,"adoption.json")))throw Error("Already adopted: preserve adoption gallery; use finish-metadata.cjs for regeneration.");
const write=(name,text)=>fs.writeFileSync(path.join(root,name),text.replace(/\r?\n/g,'\r\n'),'utf8');
const images=[];
for(let n=1;n<=6;n++){
 const source=`character-45-normal-${n}-generated.png`;
 const p=path.join(root,source);if(!fs.existsSync(p))continue;
 const b=fs.readFileSync(p);
 const reference=n===1?'conqueror-balanced/character-45-1-balanced.png':`conqueror-balanced/remaining-variants/character-45-${n}-balanced.png`;
 images.push({Id:n,Source:source,Width:b.readUInt32BE(16),Height:b.readUInt32BE(20),Sha256:crypto.createHash('sha256').update(b).digest('hex'),Prompt:`character-45-normal-${n}-prompt.txt`,References:[reference,'characters/character-01/proportion-study/character-01-vitruvian-v017-trial.png'],Status:'generated-reviewed-not-adopted'});
}
write('generation-progress.json',JSON.stringify({CharacterId:45,Method:'built-in image_gen',Standard:'v017-approved-20261018',Images:images,Next:'newspaper-formal',GameAdopted:false},null,2)+'\n');
const options=images.map(x=>`<option value="${x.Id}">通常${x.Id}</option>`).join('');
const cards=images.map(x=>`<article><h2>通常${x.Id}</h2><img src="character-45-normal-${x.Id}.png" width="256" height="192"><p><a href="${x.Source}">原画</a> / <a href="${x.Prompt}">生成指示</a></p></article>`).join('');
write('index.html',`<!doctype html><html lang="ja"><meta charset="utf-8"><title>No.45 共通画風更新 制作中</title><style>body{background:#172438;color:#eef;font:17px/1.7 system-ui;max-width:1050px;margin:30px auto}a{color:#9df}button,input,select{font:inherit}.stage{position:relative;aspect-ratio:4/3;width:100%;background:#25374d}.stage img{position:absolute;inset:0;width:100%;height:100%;object-fit:contain}#after{opacity:.5}.tiles{display:flex;gap:20px;flex-wrap:wrap}</style><h1>No.45 共通画風更新 制作中</h1><p>通常６枚の候補画像。ゲームへの適用はまだです。旧ゲーム20配布先を保存済み。新聞８枚は未生成です。</p><select id="scene">${options}</select><button data-a="0">旧版</button><button data-a="50">50％</button><button data-a="100">新版</button><input id="alpha" type="range" min="0" max="100" value="50"><div class="stage"><img id="before" alt="旧ゲーム画像"><img id="after" alt="更新候補"></div><div class="tiles">${cards}</div><p><a href="production.md">制作記録</a> / <a href="generation-progress.json">進捗とハッシュ</a></p><script>const $=x=>document.getElementById(x);function update(){const n=$('scene').value;$('before').src='backup/v3-standardized/tiles/character-45-'+n+'.png';$('after').src='character-45-normal-'+n+'-generated.png';$('after').style.opacity=$('alpha').value/100;}document.querySelectorAll('[data-a]').forEach(b=>b.onclick=()=>{$('alpha').value=b.dataset.a;update()});$('scene').onchange=update;$('alpha').oninput=update;update();</script></html>\n`);
console.log('Updated comparison and candidate manifest:',images.length,'normal portraits; runtime unchanged.');
