using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new SettingsGame();game.Run();
sealed class SettingsGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;
 int frame;WorldPreview world=null!;CubeNet net=null!;Vector2 pan;float zoom;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 Rectangle Prop(string n)=>(Rectangle)typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 Rectangle Bounds(string n)=>(Rectangle)typeof(Game1).GetField(n,S)!.GetValue(null)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Click(Point p){Set("_pointer",p);Call("OperationSettingsInput",true,false);}
 void Advance(float t)=>Call("UpdateOperationSettings",t);
 void Select(int i){Click(Bounds("MapDragButton").Center);Check((bool)Get("_mapDragListOpen"),"Dropdown opens inside settings");Click(((Rectangle)typeof(Game1).GetMethod("MapDragOption",S)!.Invoke(null,new object[]{i})!).Center);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Call("BeginTutorialStepThree");typeof(WorldSetup).GetProperty("TutorialStep")!.SetValue(Get("_setup"),0);
  Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_homePersuasionTurn",((WorldSetup)Get("_setup")).Population.Turn);
  world=(WorldPreview)Get("_world");net=(CubeNet)Get("_net");world.GlobePan=new(.12f,.08f);world.GlobeZoom=1.3f;pan=world.GlobePan;zoom=world.GlobeZoom;
  var button=Prop("OperationSettingsButton");var toy=Prop("OrientationToyArea");Check(button.Left>=toy.Right && button.Top>toy.Bottom && button.Right<=1920,"Mouse button sits below-right of robot within canvas");
  Check(!(bool)Call("MapDragClick",Bounds("MapDragButton").Center),"Hidden dropdown cannot receive clicks");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Click(Prop("OperationSettingsButton").Center);Check((bool)Get("_operationSettingsOpen"),"Mouse button opens settings");Advance(.15f);Check((float)Get("_operationSettingsSlide")>0 && (float)Get("_operationSettingsSlide")<1,"Panel animates into view");}
  if(frame==2){Advance(.15f);Check((float)Get("_operationSettingsSlide")==1,"Panel finishes entering");Select(1);Check((bool)Get("_globeDragSlide"),"Globe slide selected");Set("_statusGlobe",false);Select(0);Check((bool)Get("_netDragRotate") && (bool)Get("_globeDragSlide"),"Net and globe drag preferences remain independent");Set("_statusGlobe",true);Click(Bounds("MapDragButton").Center);}
  if(frame==3){
   var canvas=(Rectangle)Call("CanvasBounds");var toolbar=Bounds("StatusMenuButton").Center;var mouse=new MouseState(canvas.X+(int)(toolbar.X*canvas.Width/1920f),canvas.Y+(int)(toolbar.Y*canvas.Height/1080f),0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
   Set("_previousMouse",new MouseState());Call("HandleInputCore",mouse,new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);Check(!(bool)Get("_statusMenuOpen"),"Settings consumes clicks over background menu");
   Set("_pointer",Point.Zero);Call("OperationSettingsInput",false,true);Check(!(bool)Get("_operationSettingsOpen") && !(bool)Get("_mapDragListOpen"),"Escape closes panel and dropdown");Advance(.15f);
  }
  if(frame==4){Advance(.15f);Check((float)Get("_operationSettingsSlide")==0,"Panel exits fully");Click(Prop("OperationSettingsButton").Center);Advance(.3f);Click(Bounds("OperationSettingsClose").Center);Check(!(bool)Get("_operationSettingsOpen"),"Close button closes settings");Advance(.3f);Check(world.GlobePan==pan && world.GlobeZoom==zoom,"Open, settings edits and close preserve globe camera");}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"operation-settings-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==4){Console.WriteLine("PASS: mouse button placement, sliding inspector, globe/net settings, hidden-input blocking, background-input blocking, Escape/close and camera preservation.");Exit();}
 }
}
