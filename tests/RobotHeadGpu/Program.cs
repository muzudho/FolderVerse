using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var probe=new HeadProbe(args[0],args[1],args.Length > 2 ? args[2] : "head");probe.Run();
sealed class HeadProbe:Game
{
 readonly string path,output,part;readonly GraphicsDeviceManager graphics;
 RobotHeadTexture heads=null!;WorldSetup world=new();SpriteBatch batch=null!;bool drawn;
 public HeadProbe(string path,string output,string part){this.path=path;this.output=output;this.part=part;graphics=new(this){PreferredBackBufferWidth=640,PreferredBackBufferHeight=360};IsFixedTimeStep=false;}
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 protected override void LoadContent(){
  world.SetParticipantCounts(1,1);world.SetWorld(0);
  var tiles=typeof(CountryEmblemArt).GetMethod("Tiles",BindingFlags.Static|BindingFlags.NonPublic)!;
  RobotHeadSurface.RectangleF? paint = part switch {
   "torso" => new(136f/1536,128f/1024,1248f/1536,800f/1024),
   "legs" => new(260f/1536,136f/1024,968f/1536,800f/1024),
   "side" or "back" => new(.06f,.15f,.86f,.71f),
   "torso-side" => new(424f/1536,136f/1024,696f/1536,792f/1024),
   "legs-side" => new(288f/1312,120f/1199,712f/1312,960f/1199),
   "torso-back" => new(128f/1536,128f/1024,1272f/1536,800f/1024),
   "legs-back" => new(240f/1536,104f/1024,1048f/1536,832f/1024),
   _ => null
  };
  RobotHeadSurface.RectangleF? sticker = part switch {
   "torso" => new(.42f,.35f,.18f,.23f),
   "legs" => new(.58f,.63f,.15f,.20f),
   "side" or "back" => new(.48f,.56f,.16f,.18f),
   "torso-side" => new(.32f,.52f,.11f,.17f),
   "legs-side" => new(.36f,.72f,.17f,.15f),
   "torso-back" => new(.42f,.40f,.16f,.20f),
   "legs-back" => new(.59f,.64f,.14f,.19f),
   _ => null
  };
  RobotHeadSurface.RectangleF? faceBounds = part == "side" ? new(.765f,.374f,.19f,.35f) : null;
  heads=new(GraphicsDevice,path,e=>(IReadOnlyList<Rectangle>)tiles.Invoke(null,new object[]{e})!,paint,sticker,faceBounds);batch=new(GraphicsDevice);
  world.SetCountryAppearance(0,6,12,1);world.SetCountryEmblem(0,1);
  var first=heads.Get(world,0);Check(ReferenceEquals(first,heads.Get(world,0)),"Identical flag should reuse texture");
  world.SetCountryAppearance(0,9,3,4);
  var alternate = heads.Get(world,0);
  Check(!ReferenceEquals(first,alternate),"Changed flag should invalidate key");
  if (part is "torso-back" or "legs-back")
  {
   const int size = RobotHeadTexture.Resolution;
   var before = new Color[size * size];
   var after = new Color[size * size];
   first.GetData(before);
   alternate.GetData(after);
   Check(before.Select(pixel => pixel.A).SequenceEqual(after.Select(pixel => pixel.A)),
    "Rear flag must preserve the silhouette alpha");
   Check(!before.SequenceEqual(after), "Rear paint must change with country");
   Check(after[0].A == 0 && (part != "legs-back" || after[128 * size + 128].A == 0),
    "Rear sprite must preserve transparent corners and the gap between legs");
  }
  if (part == "side")
  {
   var mirrored = heads.Get(world,0,mirrorSticker:true);
   Check(!ReferenceEquals(mirrored,alternate) && ReferenceEquals(mirrored,heads.Get(world,0,mirrorSticker:true)),
    "Opposite emblem orientation needs a separate reusable texture");
   Check(ReferenceEquals(alternate,heads.Get(world,0)), "Mirrored lookup replaced the normal texture");
   const int size = RobotHeadTexture.Resolution;
   var firstPixels = new Color[size * size];
   var alternatePixels = new Color[size * size];
   first.GetData(firstPixels);
   alternate.GetData(alternatePixels);
   foreach (int y in Enumerable.Range(109, 62))
   foreach (int x in Enumerable.Range(204, 25))
    Check(firstPixels[y * size + x] == alternatePixels[y * size + x],"Side face changed with country");
   Check(!firstPixels.SequenceEqual(alternatePixels),"Side paint did not change with country");
  }
  for(int i=0;i<24;i++){world.SetCountryAppearance(0,i%WorldSetup.Colors.Length,(i+1)%WorldSetup.Colors.Length,i%5);world.SetCountryFlagShape(0,i%4);heads.Get(world,0);}
  Check(heads.Count==RobotHeadTexture.CacheLimit && !first.IsDisposed,"Evicted textures must survive queued draws");
  heads.EndFrame();Check(first.IsDisposed,"Completed frame must release evicted textures");
  var clean = heads.Get(world,0);
  var worn = heads.Get(world,0,worn:true);
  Check(!ReferenceEquals(clean,worn) && ReferenceEquals(worn,heads.Get(world,0,worn:true)),
   "Wear must have a separate cached texture");
  var cleanPixels = new Color[RobotHeadTexture.Resolution * RobotHeadTexture.Resolution];
  var wornPixels = new Color[cleanPixels.Length];
  clean.GetData(cleanPixels);
  worn.GetData(wornPixels);
  Check(cleanPixels.Select(pixel=>pixel.A).SequenceEqual(wornPixels.Select(pixel=>pixel.A)),
   "Wear must preserve every alpha pixel");
  Check(!cleanPixels.SequenceEqual(wornPixels),"Wear must change the painted shell");
  if(part=="head"){
   int face=137*RobotHeadTexture.Resolution+100;
   Check(cleanPixels[face]==wornPixels[face],"Wear must preserve the head's face");
  }
 }
 protected override void Draw(GameTime time){
  if(drawn)return;drawn=true;GraphicsDevice.Clear(new Color(25,36,48));batch.Begin();
  Texture2D queued=null!;
  for(int i=0;i<24;i++){world.SetCountryAppearance(0,i%WorldSetup.Colors.Length,(i+1)%WorldSetup.Colors.Length,i%5);world.SetCountryFlagShape(0,i%4);var t=heads.Get(world,0);queued??=t;batch.Draw(t,new Rectangle(i,350,1,1),Color.White);}
  Check(!queued.IsDisposed,"Evicted texture was released before deferred batch flush");
  int height = part switch
  {
   "torso" or "legs" or "torso-side" or "torso-back" or "legs-back" => 200,
   "legs-side" => 274,
   _ => 300
  };
  for(int i=0;i<2;i++){world.SetCountryFlagShape(0,0);world.SetCountryAppearance(0,i==0?6:9,i==0?12:3,i==0?1:4);world.SetCountryEmblem(0,i==0?1:5);batch.Draw(heads.Get(world,0),new Rectangle(10+i*320,20,300,height),Color.White);}batch.End();
  heads.EndFrame();Check(queued.IsDisposed,"Evicted draw resources survived completed frame");
  var p=new Color[640*360];GraphicsDevice.GetBackBufferData(p);using(var image=new Texture2D(GraphicsDevice,640,360)){image.SetData(p);using var file=File.Create(output);image.SaveAsPng(file,640,360);}
  var last=heads.Get(world,0);heads.Dispose();heads.Dispose();Check(last.IsDisposed && heads.Count==0,"Dispose must release all textures and be idempotent");
  try{heads.Get(world,0);throw new Exception("Disposed cache accepted request");}catch(ObjectDisposedException){}
  Console.WriteLine("PASS GPU head upload, key changes, cache reuse/eviction, premultiplied rendering and disposal.");Exit();
 }
 protected override void UnloadContent(){heads?.Dispose();batch?.Dispose();base.UnloadContent();}
}
