using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new RulesGame(args[0]);game.Run();
sealed class RulesGame(string output):Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 SpriteBatch batch=null!;WorldSetup w=null!;WorldPreview globe=null!;CubeNet net=null!;int frame,node;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 protected override void LoadContent(){
  Window.Position=new(-3000,-3000);Directory.CreateDirectory(output);
  AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));
  Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");base.LoadContent();
  Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);
  w=(WorldSetup)Get("_setup");w.SetParticipantCounts(2,2);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.HumanPlayers.Add(0);w.Relations.Reset();w.Relations.Leader=0;
  foreach(var n in w.Nodes.All)n.Owner=0;node=w.Nodes.All.First(n=>n.Functional).Id;
  globe=(WorldPreview)Get("_world");globe.ShowSetup(w,false);globe.SelectionSetup=w;globe.DaylightWorld=w;globe.DawnMeridian=.5f;
  net=(CubeNet)Get("_net");net.Home(w);net.ShowRobots=false;net.ShowBatteries=false;
  batch=(SpriteBatch)Get("_spriteBatch");Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_animationTime",10f);
  Set("_domesticSlide",0f);CheckAppearance();CheckTerrain();CheckRing();CheckClipping();
 }
 void CheckAppearance(){
  var map=new CubeNet{RestrictNodeActions=true};map.ActionTargetNodes.Add(node);var normal=new Color(180,120,60);var palette=MapNodeColors.FromEnabled(normal);
  map.HoveredNode=node;Check(map.NodeTint(node,normal)==palette.EnabledColor&&map.NodeNeedsRing(node),"hover uses enabled color plus HUD ring");
  int other=node+1;map.HoveredNode=other;map.AttentionNodes.Add(other);Check(map.NodeTint(other,normal)==palette.DisabledColor&&!map.NodeNeedsRing(other),"disabled object has no ring or attention pulse");
  map.AttentionNodes.Add(node);map.AppearanceTime=.4f;var bright=map.NodeTint(node,normal);map.AppearanceTime=1.2f;var low=map.NodeTint(node,normal);Check(bright==palette.HighlightColor&&low==palette.EnabledColor,"tutorial cycles enabled to highlight without disabling input");
  map.SelectedNodes.Add(node);Check(map.NodeTint(node,normal)==palette.HighlightColor,"selection overrides tutorial pulse");
  map.ActionTargetNodes.Clear();map.PrepareTerrainHighlights(w);Check(map.SelectedNodes.Count==0&&!map.NodeSelected(node),"invalid selection clears rather than returning later");
 }
 void CheckTerrain(){
  object Read(string n)=>typeof(WorldPreview).GetField(n,F)!.GetValue(globe)!;
  void Apply()=>typeof(WorldPreview).GetMethod("ApplyDaylight",F)!.Invoke(globe,null);
  Apply();var baseline=((VertexPositionColorNormal[])Read("_dayWorking")).ToArray();
  globe.NodeFocus=node;globe.CountryFocus=0;globe.RestrictNodeActions=true;Apply();var focused=(VertexPositionColorNormal[])Read("_dayWorking");
  Check(baseline.Select(v=>v.Color).SequenceEqual(focused.Select(v=>v.Color)),"focus never darkens terrain or cancels night");
  net.NodeFocus=node;net.InspectedNode=node;net.PrepareTerrainHighlights(w);Check(net.HighlightTiles.Count==1,"selected object highlights exactly one ground tile");
  globe.HighlightTiles.UnionWith(net.HighlightTiles);Apply();var source=(VertexPositionColorNormal[])Read("_daySource");var tiles=(MapTerrainTile[])Read("_dayTileMap");var phases=(float[])Read("_dayPhases");int changed=0;
  for(int i=0;i<source.Length;i++){
   bool selected=globe.HighlightTiles.Contains(tiles[i]);float hour=WorldDayCycle.Wrap(globe.DawnMeridian-phases[i]+8/24f)*24;
   var expected=WorldDayCycle.Tint(MapTerrainColors.ForTile(source[i].Color,selected),hour);
   CheckEqual(focused[i].Color,expected,"terrain palette is applied before day/night");
   if(focused[i].Color!=baseline[i].Color){if(!selected)throw new Exception("Selection changed unrelated tile");changed++;}
  }
  Check(changed>0,"globe highlights the same tile selected by net without unlit overlay");
  globe.HighlightTiles.Clear();Apply();Check(baseline.Select(v=>v.Color).SequenceEqual(focused.Select(v=>v.Color)),"deselect restores terrain even with unchanged time");
  Check(MapTerrainColors.Enabled(Color.White).R<255&&MapTerrainColors.ForTile(Color.White,true).R>MapTerrainColors.Enabled(Color.White).R,"snow has reserved highlight headroom");
  net.NodeFocus=net.InspectedNode=-1;globe.NodeFocus=-1;globe.CountryFocus=-1;globe.RestrictNodeActions=false;
 }
 static void CheckEqual(Color a,Color b,string message){if(a!=b)throw new Exception(message+" "+a+" != "+b);}
 void CheckRing(){
  var ui=(UiPainter)Get("_ui");Color[] Draw(float time){using var image=new RenderTarget2D(GraphicsDevice,320,240);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(new Color(16,35,47));batch.Begin();MapHud.DrawRing(ui,new(160,120),60,time);batch.End();GraphicsDevice.SetRenderTarget(null);var pixels=new Color[320*240];image.GetData(pixels);return pixels;}
  var a=Draw(0);var b=Draw(1);Check(a[120*320+160]==new Color(16,35,47)&&a[0]==new Color(16,35,47),"HUD ring has transparent center and no spotlight disk");Check(!a.SequenceEqual(b),"HUD ring animates on screen without scene lighting");
 }
 void CheckClipping(){
  using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Magenta);var clip=new Rectangle(500,200,800,500);
  globe.Draw(0,0,10,false,true,deferAtmosphere:true,clip:clip);typeof(WorldPreview).GetMethod("DrawAtmosphere",F)!.Invoke(globe,new object[]{10f,1f,1f});GraphicsDevice.SetRenderTarget(null);
  var pixels=new Color[1920*1080];image.GetData(pixels);int inside=0;
  for(int y=0;y<1080;y++)for(int x=0;x<1920;x++)if(pixels[y*1920+x]!=Color.Magenta){if(!clip.Contains(x,y))throw new Exception("Terrain/atmosphere escaped clip");inside++;}
  Check(inside>0,"terrain and atmosphere stay inside the same clip on every edge");
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(new Color(16,35,47));batch.Begin();Call("DrawStatusUi");batch.End();GraphicsDevice.SetRenderTarget(null);
  string name=frame==0?"domestic-full-space":frame==1?"domestic-inspector-space":"domestic-net-hud";using(var file=File.Create(Path.Combine(output,name+".png")))image.SaveAsPng(file,1920,1080);
  if(frame==0){
   var art=(Texture2D)Get("_guideActionArt");var icon=new Color[art.Width*art.Height];art.GetData(icon);
   Check(icon[0].A==0&&icon.Any(c=>c.A>200)&&icon.All(c=>c.R==c.G&&c.G==c.B),"adopted guide icon is transparent neutral art ready for nation tint");
  }
  var pixels=new Color[1920*1080];image.GetData(pixels);Check(pixels[380*1920]!=new Color(16,35,47)&&pixels[380*1920+1919]!=new Color(16,35,47),"space reaches both screen edges "+name);
  if(frame==0){Call("OpenNodeInspector",node);Set("_domesticSlide",1f);Call("FocusInspectorLocation");}else if(frame==1){Set("_statusGlobe",false);Set("_pointer",net.AnimatedNodePosition(w,w.Nodes.All[node],new Rectangle(40,142,1840,820)).ToPoint());}else{Console.WriteLine("PASS map UI rules and domestic space rendering");Exit();}
  frame++;
 }
}
