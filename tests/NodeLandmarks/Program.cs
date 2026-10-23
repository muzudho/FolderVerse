using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new Checks();game.Run();
sealed class Checks:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;CubeNet net=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Validate(WorldSetup world){
  var marks=NodeLandmarks.All(world);Check(marks.Count==world.Nodes.All.Length,"Every node has a landmark");
  foreach(var node in world.Nodes.All){var mark=marks[node.Id];Check(mark.Objects.Length>=2 && mark.Description.Length>0,"Combinations have usable descriptions");
   foreach(var route in world.Routes.Neighbors(node)){var other=world.Nodes.At(route.Target,route.Entry);Check(mark.Signature!=marks[other.Id].Signature,"Every adjacent pair has a different combination");}
   Check(Vector3.Distance(mark.Position,world.Routes.Position(node.Cell,node.Center))>0,"Landmark is beside the node");
   bool land=world.Routes.Land(node.Cell,node.Center);
   if(!land)Check(mark.Objects.All(k=>k is LandmarkKind.Reef or LandmarkKind.Buoy or LandmarkKind.Gulls or LandmarkKind.Wreck or LandmarkKind.Pier),"Sea nodes never receive inland buildings");
  }
 }
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var seen=new HashSet<LandmarkKind>();
  foreach(int seed in new[]{0,123,345}){
   var world=new WorldSetup();world.SetWorld(seed);world.SetCast(456);world.SetTerritoryRights(789);Validate(world);
   var initial=NodeLandmarks.All(world).Select(m=>(m.Position,m.Signature,m.Direction)).ToArray();
   foreach(int ruler in world.LandOrder())world.ClaimLand(ruler,Enumerable.Range(0,world.Cells.Length).Where(i=>world.Owners[i]<0).Take(world.TerritoryRights[ruler]).ToArray());
   world.CompleteLand(901);Validate(world);Check(initial.SequenceEqual(NodeLandmarks.All(world).Select(m=>(m.Position,m.Signature,m.Direction))),"Land-conquest landmarks persist into domestic phase");
   var loaded=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));Validate(loaded);Check(initial.SequenceEqual(NodeLandmarks.All(loaded).Select(m=>(m.Position,m.Signature,m.Direction))),"Landmarks regenerate identically from saves");
   foreach(var mark in NodeLandmarks.All(world))foreach(var kind in mark.Objects)seen.Add(kind);
  }
  Check(Enum.GetValues<LandmarkKind>().All(seen.Contains),"All requested landmark kinds can be generated");
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);net=(CubeNet)Get("_net");net.Home(w);net.ShowRobots=false;((WorldPreview)Get("_world")).ShowSetup(w,true);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_statusGlobe",false);
  net.FitAll((Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1);
  Console.WriteLine("PASS: all adjacent combinations differ across three worlds, all art types generate, land/sea restrictions, stable conquest-to-domestic placement and save/load.");
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==0){
   GraphicsDevice.Clear(new Color(18,35,46));var batch=(SpriteBatch)Get("_spriteBatch");var ui=(UiPainter)Get("_ui");batch.Begin();int i=0;
   foreach(var kind in Enum.GetValues<LandmarkKind>()){var at=new Vector2(150+i%6*290,160+i/6*300);LandmarkArt.Draw(ui,kind,at,43,1);ui.Center(NodeLandmarks.Name(kind),new((int)at.X-130,(int)at.Y+70,260,45),.65f,Color.White);i++;}batch.End();
  }else{
   if(frame==2){Set("_statusGlobe",true);Call("FocusStatusGlobe");}
   if(frame==3){w.SetTerritoryRights(789);Call("BeginLandConquest");}
   base.Draw(time);
  }
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var tex=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);tex.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"landmarks-{frame}.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame++==3){Console.WriteLine("PASS: landmark art gallery, net, globe and land-conquest rendering.");Exit();}
 }
}
