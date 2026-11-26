const fs=require('fs'),z=require('zlib'),assert=require('assert');
const read=p=>JSON.parse(z.gunzipSync(fs.readFileSync(p)));
const a=read(process.argv[2]),b=read(process.argv[3]),seen=new Set(),differences=[];
function walk(i,j,path){
 if(i<0||j<0){if(i!==j)differences.push({path,a:i,b:j});return}
 const key=i+','+j;if(seen.has(key))return;seen.add(key);
 const x=a.Objects[i],y=b.Objects[j];assert(x&&y);
 if(x.Type!==y.Type||x.Scalar!==y.Scalar){differences.push({path,a:x.Scalar,b:y.Scalar});return}
 assert.deepEqual(x.Shape,y.Shape);
 if(x.Items){assert.equal(x.Items.length,y.Items.length);x.Items.forEach((id,n)=>walk(id,y.Items[n],path+'['+n+']'))}
 if(x.Fields){assert.deepEqual(Object.keys(x.Fields),Object.keys(y.Fields));for(const field in x.Fields)walk(x.Fields[field],y.Fields[field],path+'.'+field)}
}
assert.equal(a.Version,b.Version);assert.equal(a.Objects.length,b.Objects.length);walk(a.Root,b.Root,'world');
const signature='world.<Robots>k__BackingField.<PartPlanning>k__BackingField._signature';
assert(differences.every(d=>d.path===signature),'Unexpected world state difference');
console.log(JSON.stringify({passed:true,objects:a.Objects.length,differences,explanation:'Only permitted difference: process-dependent HashCode preview cache signature; no game-domain scalar differences observed. Binary save hashes are not identical.'}));
