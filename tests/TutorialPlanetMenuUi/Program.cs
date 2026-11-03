using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new PlanetMenuGame();game.Run();
sealed class PlanetMenuGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 Rectangle Bounds(string n,params object[] a)=>(Rectangle)typeof(Game1).GetMethod(n,S)!.Invoke(null,a)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Input(bool click=false,bool enter=false,bool escape=false,Keys key=Keys.None){Set("_previousKeyboard",new KeyboardState());Set("_previousMouse",MouseAt((Point)Get("_pointer")));Call("TutorialMenuInput",click,enter,escape,key==Keys.None?new KeyboardState():new KeyboardState(key));}
 MouseState MouseAt(Point p){var c=(Rectangle)Call("CanvasBounds");return new(c.X+(int)Math.Round(p.X*c.Width/1920d),c.Y+(int)Math.Round(p.Y*c.Height/1080d),0,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);}
 void Click(Rectangle r){Set("_pointer",r.Center);Input(click:true);}
 void OpenMenu(){Set("_screen",Enum.Parse(Get("_screen").GetType(),"Title"));Call("OpenTitleStartMenu");Set("_pointer",Bounds("TitleMenuItem",0).Center);Call("TitleStartMenuInput",true,false,false,new KeyboardState(),true);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);OpenMenu();
  Check((bool)Get("_tutorialMenuOpen") && !(bool)Get("_tutorialStepDialogOpen") && (int)Get("_tutorialPlanetSelection")==0,"How to Play opens six-planet menu with first selected");
  var sizes=((int X,int Y,int Z)[])typeof(Game1).GetField("TutorialPlanetSizes",S)!.GetValue(null)!;Check(sizes.SequenceEqual(new[]{(1,1,1),(1,2,1),(2,1,2),(2,2,2),(3,2,2),(3,3,3)}),"All six requested dimensions match previews");
  var textures=(RenderTarget2D[])Get("_tutorialPlanetTextures");Check(textures.Length==6,"Six previews exist");foreach(var texture in textures){var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);Check(pixels.Count(p=>p.A>0)>100,"Every globe has rendered geometry");}
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   Input(key:Keys.Right);Check((int)Get("_tutorialPlanetSelection")==1,"Keyboard moves planet cursor");Input(enter:true);Check(!(bool)Get("_tutorialStepDialogOpen"),"Disabled planet cannot activate with Enter");
   for(int i=1;i<6;i++){Click(Bounds("TutorialPlanetCard",i));Check(!(bool)Get("_tutorialStepDialogOpen") && Get("_screen").ToString()=="Title","Disabled planet "+i+" cannot launch");}
  }
  if(frame==2){Click(Bounds("TutorialPlanetCard",0));Check((bool)Get("_tutorialStepDialogOpen"),"Petitstella opens dialog");Input(key:Keys.Down);Check((int)Get("_tutorialStepSelection")==1,"Dialog supports keyboard selection");Input(escape:true);Check(!(bool)Get("_tutorialStepDialogOpen") && (bool)Get("_tutorialMenuOpen"),"Escape returns from dialog to planets");Input(enter:true);}
  if(frame==3){
   var close=(Rectangle)typeof(Game1).GetProperty("TutorialStepDialogClose",S)!.GetValue(null)!;Click(close);Check(!(bool)Get("_tutorialStepDialogOpen"),"Dialog close returns to planets");
   for(int step=0;step<3;step++){
    Click(Bounds("TutorialPlanetCard",0));Click(Bounds("TutorialStep",step));var setup=(WorldSetup)Get("_setup");Check(setup.TutorialStep==step+1 && Get("_screen").ToString()=="WorldStatus" && setup.PlayerSlot==0,"Existing step "+(step+1)+" launches unchanged");Call("ReturnToTitle");OpenMenu();
   }
   Input(escape:true);Check(!(bool)Get("_tutorialMenuOpen") && (bool)Get("_titleMenuOpen"),"Escape returns to start menu");OpenMenu();
  }
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"tutorial-planets-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==3){Console.WriteLine("PASS: six rendered planets/dimensions, cursor navigation, five disabled planets, temporary dialog, Escape/close and all three existing tutorial entries.");Exit();}
 }
}
