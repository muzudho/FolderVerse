const {spawn}=require('node:child_process');
const fs=require('node:fs'),path=require('node:path'),{pathToFileURL}=require('node:url');
const out=path.resolve(process.argv[2]);fs.mkdirSync(out,{recursive:true});
const prototypePath=path.resolve(process.argv[3] ?? 'art-source/robots/dynamic-flag-prototype-v001/index.html');
const chrome=spawn('C:/Program Files/Google/Chrome/Application/chrome.exe',['--headless=new','--no-sandbox','--disable-gpu','--no-first-run','--disable-background-networking','--remote-debugging-pipe','--user-data-dir='+path.join(out,'test-profile'),'about:blank'],{windowsHide:true,stdio:['ignore','ignore','pipe','pipe','pipe']});
let session,seq=0,pending=new Map();const limit=setTimeout(()=>{chrome.kill();console.error('Browser check timed out');process.exit(1);},45000);
function call(method,params={}){return new Promise((resolve,reject)=>{const id=++seq;pending.set(id,{resolve,reject});chrome.stdio[3].write(JSON.stringify({id,method,params,...(session?{sessionId:session}:{})})+'\0');});}
async function evaluate(expression){const r=await call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw Error(JSON.stringify(r.exceptionDetails));return r.result.value;}
let buffer='';chrome.stdio[4].on('data',chunk=>{buffer+=chunk.toString();let split;while((split=buffer.indexOf('\0'))>=0){const text=buffer.slice(0,split);buffer=buffer.slice(split+1);if(!text)continue;const m=JSON.parse(text);if(m.id&&pending.has(m.id)){const p=pending.get(m.id);pending.delete(m.id);m.error?p.reject(Error(JSON.stringify(m.error))):p.resolve(m.result);}}});
(async()=>{try{const {targetId}=await call('Target.createTarget',{url:'about:blank'});session=(await call('Target.attachToTarget',{targetId,flatten:true})).sessionId;
await call('Emulation.setDeviceMetricsOverride',{width:1440,height:1420,deviceScaleFactor:1,mobile:false});await call('Page.navigate',{url:pathToFileURL(prototypePath).href});
await evaluate(`new Promise((resolve,reject)=>{let count=0;const t=setInterval(()=>{if(window.prototypeReady){clearInterval(t);resolve(true)}else if(++count>100){clearInterval(t);reject(Error('not ready'))}},100)})`);
if(process.argv.includes('--check-catalog')){
 const links=await evaluate(`Array.from(document.links,link=>link.href)`);
 const catalog=new URL('../index.html',pathToFileURL(prototypePath)).href;
 if(!links.includes(catalog))throw Error('Prototype is missing its catalog link');
 const pages=[
  [catalog,'ロボット素材カタログ',pathToFileURL(prototypePath).href],
  [new URL('../index.html',catalog).href,'FolderVerse NAS制作物一覧',catalog]
 ];
 for(const [url,title,link] of pages){
  await call('Page.navigate',{url});
  const found=await evaluate(`new Promise((resolve,reject)=>{let tries=0;const t=setInterval(()=>{
   if(location.href===${JSON.stringify(url)} && document.readyState==='complete'){
    clearInterval(t);resolve(document.title.includes(${JSON.stringify(title)}) &&
     Array.from(document.links,a=>a.href).includes(${JSON.stringify(link)}));
   }else if(++tries>=100){clearInterval(t);reject(Error('Catalog navigation timed out'))}
  },100)})`);
  if(!found)throw Error('Unexpected catalog page or missing return link: '+url);
 }
 await call('Page.navigate',{url:pathToFileURL(prototypePath).href});
 await evaluate(`new Promise((resolve,reject)=>{let count=0;const t=setInterval(()=>{if(window.prototypeReady){clearInterval(t);resolve(true)}else if(++count>100){clearInterval(t);reject(Error('not ready'))}},100)})`);
 console.log('PASS prototype, robot catalog and NAS artwork index links');
}
const result=await evaluate(`(()=>{const assert=(ok,msg)=>{if(!ok)throw Error(msg)};const cv=i=>document.querySelector('#head'+i);const image=i=>cv(i).toDataURL();const pixel=(i,x,y)=>Array.from(cv(i).getContext('2d').getImageData(x,y,1,1).data).join(',');const before=[image(0),image(1)],face=[pixel(0,250,280),pixel(0,160,335)];assert(before[0]!==before[1],'two nations differ');document.querySelector('#swap').click();assert(image(0)===before[1]&&image(1)===before[0],'swap redraws both heads');assert(pixel(0,250,280)===face[0]&&pixel(0,160,335)===face[1],'face/eye preserved');document.querySelector('#reset').click();assert(image(0)===before[0],'reset exact');const color=document.querySelector('[data-i="0"][data-field="primary"]');color.value='#3155c0';color.dispatchEvent(new Event('input',{bubbles:true}));assert(image(0)!==before[0],'color input redraw');assert(pixel(0,250,280)===face[0],'color keeps face');document.querySelector('#neutral').click();assert(image(0)===image(1),'one shared neutral base');document.querySelector('#reset').click();return {passed:['different flags','swap heads and emblems','face and eye invariant','reset','live color','shared neutral base']}})()`);
const shot=await call('Page.captureScreenshot',{format:'png',captureBeyondViewport:true});fs.writeFileSync(path.join(out,'preview.png'),Buffer.from(shot.data,'base64'));
await call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false});const mobile=await evaluate('document.documentElement.scrollWidth<=innerWidth');if(!mobile)throw Error('mobile overflow');result.passed.push('390px no horizontal overflow');fs.writeFileSync(path.join(out,'checks.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
session=undefined;await call('Browser.close');clearTimeout(limit);
}catch(e){console.error(e);clearTimeout(limit);chrome.kill();process.exitCode=1;}})();
