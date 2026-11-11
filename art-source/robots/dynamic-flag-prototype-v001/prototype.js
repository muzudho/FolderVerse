'use strict';
const INITIAL=[{name:'Ａ国',primary:'#008b87',secondary:'#f15e45',pattern:1,emblem:1},{name:'Ｂ国',primary:'#694ea8',secondary:'#f2c84b',pattern:4,emblem:5}];
let countries=structuredClone(INITIAL),basePixels;
const W=640,doc=document;
const clamp=(x,a=0,b=1)=>Math.max(a,Math.min(b,x));
const rgb=hex=>[1,3,5].map(i=>parseInt(hex.slice(i,i+2),16));
function secondary(pattern,x,y){
  if(pattern===1)return ((x+y)%5+5)%5<2;
  x=(Math.floor(x)%10+10)%10;y=(Math.floor(y)%10+10)%10;
  return (Math.floor(x/2)+Math.floor(y/2))%2===0;
}
function polygon(x,y,points){let inside=false;for(let i=0,j=points.length-1;i<points.length;j=i++){
  const a=points[i],b=points[j];if((a[1]>y)!==(b[1]>y)&&x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0])inside=!inside;
}return inside;}
const heart=Array.from({length:96},(_,i)=>{const a=i*Math.PI*2/96,s=Math.sin(a);return [.86*s*s*s,-(13*Math.cos(a)-5*Math.cos(2*a)-2*Math.cos(3*a)-Math.cos(4*a)+2.5)/18];});
function emblem(id,x,y){
  if(id===5)return polygon(x,y,heart);
  const head=x*x/(.72*.72)+(y+.02)*(y+.02)/(.6*.6)<=1||polygon(x,y,[[-.68,-.12],[-.72,-.88],[-.17,-.52]])||polygon(x,y,[[.17,-.52],[.72,-.88],[.68,-.12]]);
  return head&&!([-.27,.27].some(cx=>(x-cx)**2+(y+.13)**2<=.085**2));
}
function emblemTile(c,size=100){const cv=doc.createElement('canvas');cv.width=cv.height=size;const cx=cv.getContext('2d');cx.fillStyle=c.secondary;
 for(let y=0;y<32;y++)for(let x=0;x<32;x++)if(emblem(c.emblem,(x+.5-16)/16,(y+.5-16)/16))cx.fillRect(x*size/32,y*size/32,Math.ceil(size/32),Math.ceil(size/32));return cv;}
function flag(canvas,c){const cx=canvas.getContext('2d'),a=rgb(c.primary),b=rgb(c.secondary);const data=cx.createImageData(360,240);
 for(let y=0;y<240;y++)for(let x=0;x<360;x++){const p=(y*360+x)*4,col=secondary(c.pattern,(x+.5)*10/360,(y+.5)*10/240)?b:a;data.data.set([...col,255],p);}cx.putImageData(data,0,0);
 const mark=emblemTile(c,138),dark=doc.createElement('canvas');dark.width=dark.height=138;const dc=dark.getContext('2d');dc.drawImage(mark,0,0);dc.globalCompositeOperation='source-in';dc.fillStyle='#0c171f';dc.fillRect(0,0,138,138);cx.save();cx.shadowColor='#0c171f';cx.shadowBlur=0;for(const [dx,dy] of [[-5,0],[5,0],[0,-5],[0,5],[-4,-4],[4,4],[-4,4],[4,-4]])cx.drawImage(dark,111+dx,51+dy);cx.restore();cx.drawImage(mark,111,51);cx.strokeStyle='#0c171f';cx.lineWidth=6;cx.strokeRect(0,0,360,240);
}
function drawHead(canvas,c){const cx=canvas.getContext('2d');if(!basePixels)return;const pixels=new Uint8ClampedArray(basePixels.data),a=rgb(c.primary),b=rgb(c.secondary),isNeutral=doc.querySelector('#neutral').checked;
 if(!isNeutral)for(let y=0;y<W;y++)for(let x=0;x<W;x++){
   const p=(y*W+x)*4;if(pixels[p+3]<2)continue;
   const u=x/W,v=y/W,r=pixels[p],g=pixels[p+1],bl=pixels[p+2],mean=(r+g+bl)/3;
   const faceInterior=u>.20&&u<.57&&v>.45&&v<.65;
   // Neutral shell is recolored; the warm cream face and its dark features are protected.
   let mask=1-clamp((Math.max(r,g,bl)-Math.min(r,g,bl)-3)/10);
   if(faceInterior||mean<28)mask=0;
   if(mask<=0)continue;
   // Compress pattern towards the receding side to suggest a rounded head, without a 3-D mesh.
   const surfaceX=(u-.06)*10+Math.max(0,u-.68)**2*22;
   const surfaceY=(v-.15)*10+.32*Math.sin((u-.1)*Math.PI);
   const ink=secondary(c.pattern,surfaceX,surfaceY)?b:a;
   const shade=clamp(mean/156,.1,1.08),shine=clamp((mean-188)/67)*.95;
   for(let k=0;k<3;k++)pixels[p+k]=pixels[p+k]*(1-mask)+(ink[k]*shade*(1-shine)+255*shine)*mask;
 }
 cx.clearRect(0,0,W,W);cx.putImageData(new ImageData(pixels,W,W),0,0);
 if(!isNeutral){const mark=emblemTile(c,80),outline=doc.createElement('canvas');outline.width=outline.height=100;const oc=outline.getContext('2d');oc.drawImage(mark,10,10);oc.globalCompositeOperation='source-in';oc.fillStyle='#fff';oc.fillRect(0,0,100,100);cx.save();cx.translate(W*.735,W*.665);cx.transform(.9,-.08,.04,1,0,0);for(const [x,y]of[[-3,0],[3,0],[0,-3],[0,3]])cx.drawImage(outline,-50+x,-50+y);cx.drawImage(mark,-40,-40);cx.restore();}
}
function render(){countries.forEach((c,i)=>{flag(doc.querySelector(`#flag${i}`),c);drawHead(doc.querySelector(`#head${i}`),c);});}
function mount(){doc.querySelector('#cards').innerHTML=countries.map((c,i)=>`<article><h2>${c.name}の国旗と頭</h2><canvas class="flag" id="flag${i}" width="360" height="240" aria-label="${c.name}の国旗"></canvas><canvas class="head" id="head${i}" width="640" height="640" aria-label="${c.name}の模様を反映したポリプロピレンの頭"></canvas><div class="controls"><label>基本色<input type="color" data-i="${i}" data-field="primary" value="${c.primary}"></label><label>２色目<input type="color" data-i="${i}" data-field="secondary" value="${c.secondary}"></label><label>模様<select data-i="${i}" data-field="pattern"><option value="1" ${c.pattern===1?'selected':''}>斜めストライプ</option><option value="4" ${c.pattern===4?'selected':''}>チェッカー</option></select></label><label>紋章<select data-i="${i}" data-field="emblem"><option value="1" ${c.emblem===1?'selected':''}>猫</option><option value="5" ${c.emblem===5?'selected':''}>ハート</option></select></label></div><p class="small">共通原画１枚から合成・国旗データだけ変更</p></article>`).join('');render();}
doc.querySelector('#cards').addEventListener('input',e=>{const {i,field}=e.target.dataset;if(i===undefined)return;countries[i][field]=['pattern','emblem'].includes(field)?Number(e.target.value):e.target.value;render();});
doc.querySelector('#swap').onclick=()=>{const a=countries[0],b=countries[1];countries=[{...b,name:'Ａ国'},{...a,name:'Ｂ国'}];mount();};
doc.querySelector('#reset').onclick=()=>{countries=structuredClone(INITIAL);doc.querySelector('#neutral').checked=false;mount();};
doc.querySelector('#neutral').onchange=render;
mount();const base=new Image();base.onload=()=>{try{const cv=doc.createElement('canvas');cv.width=cv.height=W;const cx=cv.getContext('2d',{willReadFrequently:true});cx.drawImage(base,0,0,W,W);basePixels=cx.getImageData(0,0,W,W);render();doc.querySelector('#status').textContent='表示完了。同じ頭のまま、色・模様・紋章を変更できます。';window.prototypeReady=true;}catch(e){doc.querySelector('#error').textContent='描画できませんでした：'+e.message;}};base.onerror=()=>doc.querySelector('#error').textContent='共通画像を読み込めませんでした。';base.src=window.HEAD_BASE;
// Expose pure design helpers for regression checks; no game save or rules are mutated.
window.flagPrototype={secondary,emblem,INITIAL};
