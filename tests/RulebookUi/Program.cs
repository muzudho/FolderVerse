using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new PaperChecks();game.Run();
sealed class PaperChecks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  Set("_conquestOverview",false);Call("OpenTitleStartMenu");Set("_hasAutoSave",true);
  Call("TitleStartMenuInput",false,false,false,new KeyboardState(Keys.Up),true);
  Check((bool)Get("_titleMenuOpen"),"Keyboard navigation retains the menu");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Set("_hasAutoSave",false);var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  base.Draw(t);
  var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  Check(pixels.Count(c=>c==new Color(255,246,221))>1000,"Enabled cream buttons are visible at both window sizes");
  if(frame==0)Check(pixels.Count(c=>c==new Color(29,82,82))>5000,"Keyboard selection fills its whole button in teal");
  else Check(pixels.Count(c=>c==new Color(232,218,195))>1000,"Unavailable Continue uses a flat muted fill");
  using(var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){
   tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"paper-menu-{frame}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  }
  if(frame++==1){
   Set("_pointer",new Point(960,617));Call("TitleStartMenuInput",true,false,false,new KeyboardState(),true);
   Check(Get("_screen").ToString()=="Title","Unavailable Continue remains inert");
   Set("_pointer",new Point(960,717));Call("TitleStartMenuInput",true,false,false,new KeyboardState(),true);
   Check(Get("_screen").ToString()=="OpeningBook","New game remains clickable");
   Console.WriteLine("PASS: menu buttons at full and half size, keyboard selection, unavailable Continue and New Game.");Exit();
  }
 }
}
