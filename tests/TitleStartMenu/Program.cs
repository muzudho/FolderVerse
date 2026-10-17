using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new MenuGame();game.Run();
sealed class MenuGame:Game1{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 void Input(Point canvasPoint,ButtonState button=ButtonState.Released,params Keys[] keys){
  var canvas=(Rectangle)Call("CanvasBounds");var mouse=new MouseState(canvas.X+canvasPoint.X*canvas.Width/1920,canvas.Y+canvasPoint.Y*canvas.Height/1080,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
  Call("HandleInput",mouse,new KeyboardState(keys),new GameTime(),true);
 }
 void Click(int x,int y){Input(new(x,y));Input(new(x,y),ButtonState.Pressed);Input(new(x,y));}
 void Key(Keys key){Input(default,keys:new[]{key});Input(default);}
 string path=Path.Combine(AppContext.BaseDirectory,"menu-save-test","autosave.json.gz");int frame;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSavePath",path);Set("_autoSaveEnabled",true);if(File.Exists(path))File.Delete(path);Call("CheckAutoSave");
  Click(960,930);Check((bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title" && (int)Get("_titleMenuSelection")==2,"Start opens menu without save, New Game selected");
  Check(!(bool)Call("UpdateDemo",11d,true,false,false,false,11d),"Idle demo cannot steal open menu");
  Click(960,517);Check((bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title","Hidden Continue row is inert without save");
  Click(960,417);Check(Get("_screen").ToString()=="Title","Reserved How to Play row is inert");
  Key(Keys.Escape);Check(!(bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title","Escape dismisses menu instead of exiting app");
  Key(Keys.Enter);Check((bool)Get("_titleMenuOpen"),"Enter opens menu");Click(100,500);Check(!(bool)Get("_titleMenuOpen"),"Outside click dismisses menu");
  Click(960,930);Click(960,350);Check((bool)Get("_titleMenuOpen"),"Blank paper has no hidden close button");Key(Keys.Escape);
  Click(960,930);Click(960,617);Check(Get("_screen").ToString()=="PlayerCount","New Game opens existing player-count flow");
  var w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("AutoSaveDomestic");Call("ReturnToTitle");
  Click(960,930);Check((int)Get("_titleMenuSelection")==1,"Continue is initial selection with save");Key(Keys.Down);Check((int)Get("_titleMenuSelection")==2,"Arrow key selects New Game");Key(Keys.Up);Check((int)Get("_titleMenuSelection")==1,"Arrow key returns to Continue");Input(new(960,617));Input(new(960,617),keys:new[]{Keys.Up});Input(new(960,617));Check((int)Get("_titleMenuSelection")==1,"Stationary mouse cannot override keyboard selection");Key(Keys.Enter);Check(Get("_screen").ToString()=="WorldStatus","Keyboard Continue restores game");Call("ReturnToTitle");
  var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();
  Click(960,930);Click(960,517);Check(Get("_screen").ToString()=="WorldStatus","Continue hit testing works at 960x540");Call("ReturnToTitle");
  graphics.PreferredBackBufferWidth=1920;graphics.PreferredBackBufferHeight=1080;graphics.ApplyChanges();
  var savedBytes=File.ReadAllBytes(path);File.WriteAllText(path,"broken");Click(960,930);Click(960,517);Check((bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title","Resume failure retains usable paper menu");Click(960,617);Check(Get("_screen").ToString()=="PlayerCount" && File.ReadAllText(path)=="broken","New Game remains available without deleting unreadable save");File.WriteAllBytes(path,savedBytes);Set("_autoSaveError","");Set("_screen",Enum.Parse(Get("_screen").GetType(),"Title"));Call("OpenTitleStartMenu");
  Console.WriteLine("PASS: Start modal, reserved tutorial slot, save-dependent Continue, New Game, keyboard navigation, text-only paper, Escape/outside, demo pause and small-window hit testing.");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1)Set("_hasAutoSave",false);
  if(frame==2){Set("_hasAutoSave",true);var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
  if(frame==3)Set("_titleMenuOpen",false);
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  var canvas=(Rectangle)Call("CanvasBounds");int sx=canvas.X+780*canvas.Width/1920,sy=canvas.Y+940*canvas.Height/1080;var buttonPixel=pixels[sy*pp.BackBufferWidth+sx];
  Check(frame==3?buttonPixel.B>buttonPixel.R:buttonPixel.R>buttonPixel.B,"Start button is drawn only while the paper menu is closed");
  using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"start-menu-{frame}.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame++==3){Call("DeleteCompletedAutoSave");Exit();}
 }
}
