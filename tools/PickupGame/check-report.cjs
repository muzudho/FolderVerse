// Offline smoke check: executes report navigation against a small DOM stand-in.
// This checks data and JS behavior, not browser layout or font rendering.
const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const directory=process.argv[2],html=fs.readFileSync(path.join(directory,'index.html'),'utf8');
const data=JSON.parse(html.match(/<script id="data" type="application\/json">([\s\S]*?)<\/script>/)[1]);
const script=html.match(/<\/script><script>([\s\S]*?)<\/script>/)[1];
class Element{
 constructor(tag){this.tag=tag;this.children=[];this.style={};this.classList={toggle(){}}}
 append(...items){this.children.push(...items)} prepend(...items){this.children.unshift(...items)}
 replaceChildren(...items){this.children=[...items]} setAttribute(k,v){this[k]=v}
 cloneNode(){return Object.assign(new Element(this.tag),this)}
}
const elements=new Map();
const document={querySelector(selector){if(!elements.has(selector))elements.set(selector,new Element(selector));return elements.get(selector)},createElement:t=>new Element(t),createElementNS:(ns,t)=>new Element(t),createTextNode:text=>({text}),querySelectorAll:()=>[]};
document.querySelector('#data').textContent=JSON.stringify(data);
const context=vm.createContext({document,console,setInterval:()=>1,clearInterval:()=>{}});
vm.runInContext(script,context);
assert.equal(data.daily.length,data.daily.at(-1).turn+1);
assert.equal(data.snapshots[0].turn,0);
assert(data.daily.every((d,i)=>d.turn===i));
const final=data.snapshots.at(-1);
assert.equal(final.turn,data.daily.at(-1).turn);
assert(final.turn<=400);
for(let i=0;i<data.snapshots.length;i++){
 vm.runInContext(`show(${i})`,context);
 const s=data.snapshots[i];assert(s.turn%20===0||i===data.snapshots.length-1);
 for(const ext of ['png','json','fvs'])assert(fs.existsSync(path.join(directory,`turn-${String(s.turn).padStart(4,'0')}.${ext}`)));
 assert.equal(elements.get('#map').src,`turn-${String(s.turn).padStart(4,'0')}.png`);
 assert(elements.get('#statistics').textContent.includes(String(s.military)));
 assert.equal(elements.get('#nodes').children.length,s.nodes.length);
 assert.equal(s.military+s.civilian,s.nodes.length);
 assert.equal(elements.get('#ranking').children.length,s.ranking.filter(r=>r.Cells>0).length);
}
elements.get('#prev').onclick();assert.equal(+elements.get('#slider').value,data.snapshots.length-2);
elements.get('#slider').oninput({target:{value:'0'}});assert.equal(+elements.get('#slider').value,0);
elements.get('#next').onclick();assert.equal(+elements.get('#slider').value,1);
console.log(JSON.stringify({passed:true,snapshots:data.snapshots.length,days:data.daily.length,finalTurn:final.turn,initialMilitary:data.snapshots[0].military,finalMilitary:final.military,limitation:'DOM stand-in; PNG inspected separately; browser layout not automated'}));
