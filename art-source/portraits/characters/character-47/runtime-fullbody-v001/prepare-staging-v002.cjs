const fs=require('fs'),path=require('path'),crypto=require('crypto');
const dir=__dirname,repo=path.resolve(dir,'../../../../..'),portrait=path.join(repo,'FolderVerse/Content/Images/Portraits');
const read=f=>JSON.parse(fs.readFileSync(f,'utf8').replace(/^\uFEFF/,''));
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const write=(f,x)=>fs.writeFileSync(f,(typeof x==='string'?x:JSON.stringify(x,null,2)+'\n').replace(/\r?\n/g,'\r\n'));
const previous=read(path.join(dir,'staging-v001/manifest.json')),crops=read(path.join(dir,'newspaper-crops-v006.json'));
const mgcb=fs.readFileSync(path.join(repo,'FolderVerse/Content/Content.mgcb'),'utf8'),project=fs.readFileSync(path.join(repo,'FolderVerse/FolderVerse.csproj'),'utf8');
const catalog=read(path.join(repo,'art-source/portraits/characters/catalog.json')),selected=read(path.join(portrait,'v3-standardized/selected-versions.json'));
const records=[];const targets=new Set();
for(const old of previous.Images){
 const r=structuredClone(old);
 if(r.Kind==='newspaper'){const crop=crops.Images.find(x=>x.Id===r.Id);r.Master=crop.Source;r.Crop={X:crop.X,Y:crop.Y,Width:crop.Width,Height:crop.Height};r.Tile='qa-newspaper-v006/'+r.Id+'-color.png';}
 r.Staged='staging-v002/'+r.Target;
 const master=path.join(dir,r.Master),tile=path.join(dir,r.Tile),runtime=path.join(portrait,r.Target),backup=path.join(dir,'backup',r.Target),out=path.join(dir,r.Staged);
 for(const f of [master,tile,runtime,backup])if(!fs.existsSync(f))throw Error('Missing '+f);
 if(targets.has(r.Target))throw Error('Duplicate target');targets.add(r.Target);
 const png=fs.readFileSync(tile),raw=fs.readFileSync(master);
 if(png.readUInt32BE(16)!==r.Width||png.readUInt32BE(20)!==r.Height)throw Error('Wrong tile dimensions');
 if(r.Crop.X<0||r.Crop.Y<0||r.Crop.X+r.Crop.Width>raw.readUInt32BE(16)||r.Crop.Y+r.Crop.Height>raw.readUInt32BE(20))throw Error('Crop outside master');
 if(Math.abs(r.Crop.Width/r.Crop.Height-r.Width/r.Height)>.002)throw Error('Aspect distortion');
 r.MasterSha256=hash(master);r.Sha256=hash(tile);r.RuntimeBeforeSha256=hash(runtime);r.BackupSha256=hash(backup);
 if(r.RuntimeBeforeSha256!==r.BackupSha256)throw Error('Runtime changed since backup: '+r.Target+'; do not overwrite');
 r.Distribution=r.Kind==='normal'?'MGCB Texture -> XNB':'PNG CopyToOutputDirectory PreserveNewest';
 if(r.Kind==='normal'&&!mgcb.includes('/build:Images/Portraits/'+r.Target))throw Error('MGCB registration missing');
 if(r.Kind==='newspaper'&&!project.includes('Content/Images/Portraits/newspaper/*.png'))throw Error('PNG copy rule missing');
 if(r.Kind==='normal'){const c=catalog.filter(x=>x.Current&&x.CharacterId===47&&x.Variant===r.Id),s=selected.filter(x=>x.CharacterId===47&&x.Variant===r.Id);if(c.length!==1||s.length!==1)throw Error('Ambiguous catalog selection');r.CurrentCatalogFile=c[0].File;r.CurrentSelectedFile=s[0].File;}
 fs.mkdirSync(path.dirname(out),{recursive:true});
 if(fs.existsSync(out)&&hash(out)!==r.Sha256)throw Error('Different staging output already exists: '+out);
 if(!fs.existsSync(out))fs.copyFileSync(tile,out);
 if(hash(out)!==r.Sha256)throw Error('Copy hash mismatch');records.push(r);
}
if(records.length!==20||records.filter(x=>x.Kind==='normal').length!==12)throw Error('Wrong set size');
const result={CharacterId:47,PreparedAt:'2026-10-19 22:33 JST',Status:'STAGED_NOT_ADOPTED',BodyQAPassed:false,RuntimeChanged:false,BuildVerified:false,Formal:'v005 candidate; exact anatomy ratio unmeasured',Pending:['Anatomical proportion QA and final candidate choice','Runtime/catalog/selection application','Build and distribution verification'],MetadataCounts:{CurrentCatalog:catalog.filter(x=>x.Current).length,Selected:selected.length},Images:records};
write(path.join(dir,'staging-v002/manifest.json'),result);
const escape=s=>s.replaceAll('&','&amp;').replaceAll('"','&quot;').replaceAll('<','&lt;');
let html='<!doctype html><meta charset="utf-8"><title>No47 適用候補 v002</title><style>body{background:#182436;color:white;font:16px sans-serif}section{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}img{max-width:100%}a{color:skyblue}.paper img{filter:grayscale(1)}</style><h1>No.47 ゲーム適用候補 v002</h1><p>通常６場面×２用途＋新聞８場面。ゲーム未変更。体格QA・最終選定・ビルドと配布検証は未完。</p><section>';
for(const r of records.filter(x=>!x.Target.startsWith('conqueror-creation/'))){html+='<article class="'+(r.Kind==='newspaper'?'paper':'')+'"><h2>'+r.Kind+' '+r.Id+'</h2><p>旧版 → 候補</p><img src="backup/'+escape(r.Target)+'"><img src="'+escape(r.Staged)+'"><p><a href="'+escape(r.Master)+'">全身原画</a></p></article>';}
write(path.join(dir,'staging-review-v002.html'),html+'</section>');
console.log('Validated 20 staged files; 20 unchanged runtime/backup pairs; 12 MGCB registrations; 8 PNG copy targets. Runtime unchanged.');
