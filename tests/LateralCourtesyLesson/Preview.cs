using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
sealed class LessonAndControlsPreview:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string m){if(!ok)throw new Exception(m);Console.WriteLine("PASS "+m);}
 protected override void LoadContent(){
  Window.Position=new(-3000,-3000);Content.RootDirectory=Environment.GetEnvironmentVariable("UI_FONT_ROOT")!;
  var font=Content.Load<SpriteFont>("UiFont");var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});var batch=new SpriteBatch(GraphicsDevice);
  Set("_font",font);Set("_pixel",pixel);Set("_spriteBatch",batch);Set("_ui",new UiPainter(batch,pixel,font));
  Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",F)!.FieldType,"WorldStatus"));Set("_statusGlobe",true);Set("_pointer",new Point(-10,-10));
  var w=(WorldSetup)Get("_setup");w.SetParticipantCounts(1,1);w.SetWorld(42);w.SetCast(8);w.SetTerritoryRights(2);var planner=new ComputerLandPlanner(w);foreach(int r in w.LandOrder())w.ClaimLand(r,planner.Select(r,null).Cells);w.CompleteLand(3,25,25);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);
  {
   // Match the actual Accent rather than assuming a fixed faction palette.
   var accent=(Color)typeof(Game1).GetProperty("Accent",F)!.GetValue(this)!;
   foreach(bool spinning in new[]{false,true}){
    Set("_globeSpinning",spinning);using var target=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(Color.Black);
    batch.Begin();Call("DrawMapNavigationButtons");Call("DrawStatusMenuCore");Call("DrawStratagemInterface");Call("DrawLessonCard",BattleLesson.LateralCourtesy,new Rectangle(80,100,660,700),true,1f);batch.End();
    var pixels=new Color[1920*1080];target.GetData(pixels);
    var nav=(Rectangle)Call("NetButton",1);Check(pixels[(nav.Y+7)*1920+nav.X+7]==accent,"rotation toggle uses faction color whether on or off");
    var menu=(Rectangle)typeof(Game1).GetField("StatusMenuButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Check(pixels[(menu.Y+7)*1920+menu.X+7]==accent,"system menu uses faction color");
    var manual=(Rectangle)Call("StratagemTrayBounds",new Rectangle(370,854,180,110));Check(pixels[(manual.Y+7)*1920+manual.X+7]==accent,"manual operation uses faction color");
    var disabled=(Rectangle)Call("NetButton",3);Check(pixels[(disabled.Y+7)*1920+disabled.X+7]!=accent,"no inspector target remains disabled");
    GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(Environment.GetEnvironmentVariable("UI_OUTPUT")!,"lesson19-controls-"+spinning+".png"));target.SaveAsPng(file,1920,1080);
   }
  }
  foreach(string key in new[]{"securePocket","securePocketCaution"})foreach(string line in GuideMessages.Page(key))Check(font.MeasureString(line).X*.48f<=850,"guide line fits balloon: "+line);
  Exit();
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){}
}
