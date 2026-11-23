using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new FocusProbe();game.Run();
sealed class FocusProbe:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 protected override void LoadContent(){
  Window.Position=new(-3000,-3000);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("CONTENT_RUNTIME"));Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("CONTENT_RUNTIME")!,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Set("_demoActive",false);Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);
  var w=(WorldSetup)Get("_setup");w.SetParticipantCounts(0,2);w.SetWorld(42);w.SetCast(8);w.SetPlacement(3);w.SelectPlayer(0);
  foreach(string screen in new[]{"Newspaper","WorldStatus","Industry","Transport","Battle"}){
   Set("_screen",Enum.Parse(Get("_screen").GetType(),screen));Set("_autoPlay",true);Set("_battleIntroAge",0d);Set("_robotSummary",false);Set("_autoPaused",false);Set("_statusMenuOpen",false);
   var bounds=(Rectangle)Call("CanvasBounds");
   void Input(Point p,bool pressed,bool active=true,KeyboardState? keys=null){
    Call("HandleInput",new MouseState(p.X,p.Y,0,pressed?ButtonState.Pressed:ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),keys??new KeyboardState(),new GameTime(TimeSpan.FromSeconds(10),TimeSpan.Zero),active);
   }
   foreach(var p in new[]{new Point(bounds.Right+1551,bounds.Top-451),new Point(bounds.Right+577,bounds.Top+707),new Point(bounds.Left-1,bounds.Center.Y),new Point(bounds.Center.X,bounds.Bottom)}){
    Set("_previousMouse",new MouseState());Input(p,true);
    Check(!(bool)Get("_autoPaused")&&!(bool)Get("_statusMenuOpen"),screen+": external click does not pause or open menu "+p);
   }
   // A focus-return frame may still have the external click held down: it must not create a new click.
   Input(bounds.Center,true);Check(!(bool)Get("_autoPaused"),screen+": held button on focus return does not pause");
   Set("_previousMouse",new MouseState());Input(bounds.Center,true,false);Check(!(bool)Get("_autoPaused"),screen+": inactive click is ignored");
   Set("_previousMouse",new MouseState());Input(bounds.Center,true);Check((bool)Get("_autoPaused")&&(bool)Get("_statusMenuOpen"),screen+": deliberate in-game click still pauses");
   Call("ResumeAutoPause");Set("_previousKeyboard",new KeyboardState());Set("_previousMouse",new MouseState());Input(bounds.Center,false,true,new KeyboardState(Keys.Escape));Check((bool)Get("_autoPaused"),screen+": Escape still pauses");
  }
  Console.WriteLine("PASS exhibition focus regression");Exit();
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){}
}
