using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new ZoomChecks();game.Run();
sealed class ZoomChecks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldPreview world=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 void Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a);
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  var w=(WorldSetup)Get("_setup");w.SetWorld(1);w.SetCast(2);w.SetTerritoryRights(3);w.SelectPlayer(0);
  foreach(var c in w.Cells)w.Owners[c.Id]=0;
  Set("_conquestOverview",false);Call("BeginLandConquest");world=(WorldPreview)Get("_world");
  var bounds=new Rectangle(580,170,1280,780);var bg=new Color(7,9,11);
  using var target=new RenderTarget2D(GraphicsDevice,1920,1080,false,SurfaceFormat.Color,DepthFormat.Depth24);
  var previous=GraphicsDevice.GetRenderTargets();var viewport=GraphicsDevice.Viewport;
  foreach(float zoom in new[]{.5f,1f,4f,8f}){
   world.GlobeZoom=zoom;
   Color[] Render(bool expanded){
    GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(bg);
    GraphicsDevice.Viewport=expanded?new Viewport(0,0,1920,1080):new Viewport(bounds);
    world.Draw(0,0,0,false,true,expanded?bounds:null);
    GraphicsDevice.SetRenderTargets(previous);GraphicsDevice.Viewport=viewport;
    var pixels=new Color[1920*1080];target.GetData(pixels);return pixels;
   }
   var clipped=Render(false);var full=Render(true);int changed=0,compared=0,extension=0;
   for(int y=0;y<1080;y++)for(int x=0;x<1920;x++){
    int i=y*1920+x;
    if(bounds.Contains(x,y)){compared++;if(clipped[i]!=full[i])changed++;}
    else if(full[i]!=bg)extension++;
   }
   Check(changed<compared*.02,"Expanded viewport preserves projection and colors inside original frame");
   if(zoom>=4)Check(extension>1000,"Zoomed land colors extend beyond the old rectangular viewport");
  }
  object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
  Check((bool)Prop("OrientationControlsVisible"),"Land conquest enables orientation controls");
  var toy=(Rectangle)Prop("OrientationToyArea");
  foreach(int axis in new[]{0,1,2})foreach(int sign in new[]{-1,1}){
   var from=Quaternion.CreateFromRotationMatrix(world.ViewOrientation((float)Get("_yawAngle"),(float)Get("_pitchAngle")));
   var button=(Rectangle)typeof(Game1).GetMethod("AxisButton",F)!.Invoke(this,new object[]{toy,axis,sign})!;
   Set("_pointer",button.Center);
   Call("UpdateLandSetup",0d,new MouseState(),true,true,false);
   Check((bool)Prop("OrientationRotating"),"Land input starts axis rotation");
   Call("UpdateLandSetup",.2d,new MouseState(),true,false,false);
   Call("UpdateLandSetup",.2d,new MouseState(),true,false,false);
   var to=world.ManualOrientation!.Value;float angle=2*MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(from,to)),0,1));
   Check(MathF.Abs(angle-MathHelper.ToRadians(30))<.001,"Each land control rotates exactly 30 degrees");
  }
  Set("_pointer",((Rectangle)Prop("OrientationResetButton")).Center);
  Call("UpdateLandSetup",0d,new MouseState(),true,true,false);
  Check(world.ManualOrientation==null && (float)Get("_yawAngle")==0 && (float)Get("_pitchAngle")==0,"Land reset returns to default orientation");
  Check(((System.Collections.ICollection)Get("_landOrder")).Count>0 && ((System.Collections.IEnumerable)Get("_landSelection")).Cast<int>().Count()==0,"Rotation controls do not select land");
  w.SetTerritoryRights(3);w.SelectPlayer(0);Call("BeginLandConquest");
  var others=w.LandOrder().Where(r=>r!=0).ToArray();
  Set("_landOrder",new[]{others[0],0}.Concat(others.Skip(1)).ToArray());
  var skip=(Rectangle)typeof(Game1).GetField("LandSkipOthers",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Set("_pointer",skip.Center);Call("UpdateLandSetup",0d,new MouseState(),true,true,false);
  Check((bool)Get("_skipOtherLandTurns") && (int)Get("_landTurn")==1,"Skip button immediately stops at human turn");
  Check(w.Owners.Count(o=>o==others[0])==w.TerritoryRights[others[0]] && !w.Owners.Contains(0),"Skipping claims exact CPU rights and leaves human choice untouched");
  var selection=(HashSet<int>)Get("_landSelection");selection.UnionWith(w.Cells.Where(c=>w.Owners[c.Id]<0).Take(w.TerritoryRights[0]).Select(c=>c.Id));Call("ConfirmLand");
  Call("UpdateLandSetup",0d,new MouseState(),false,false,false);
  Check(Get("_screen").ToString()=="Fortune" && w.Owners.All(o=>o>=0),"Skip completes remaining CPU turns and advances to fortune");
  w.SetTerritoryRights(3);w.SelectPlayer(0);w.HumanPlayers.Clear();w.HumanPlayers.Add(0);w.HumanPlayers.Add(others[1]);Call("BeginLandConquest");
  Set("_landOrder",new[]{others[0],others[1],0}.Concat(others.Skip(2)).ToArray());
  Set("_pointer",skip.Center);Call("UpdateLandSetup",0d,new MouseState(),true,true,false);
  Check((int)Get("_landTurn")==1 && !w.Owners.Contains(others[1]),"Multiplayer skip preserves the next human turn");
  w.HumanPlayers.Clear();w.SetTerritoryRights(3);w.SelectPlayer(0);Call("BeginLandConquest");
  Check(!(bool)Get("_skipOtherLandTurns"),"New land phase resets skip preference");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  world.GlobeZoom=frame==0?1:8;
  if(frame==2){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using(var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){tex.SetData(pixels);using var f=File.Create(Path.Combine(AppContext.BaseDirectory,$"land-zoom-{frame}.png"));tex.SaveAsPng(f,tex.Width,tex.Height);}
  if(frame++==2){Console.WriteLine("PASS: zoom .5, 1, 4 and 8 preserve framing and extend country fill past old bounds; full/half-size UI rendered.");Exit();}
 }
}
