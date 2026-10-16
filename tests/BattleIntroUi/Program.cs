using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new IntroGame();game.Run();
sealed class IntroGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  var w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  var b=new BattleState(1){TargetNode=w.Nodes.Current(0).Id,Defender=0};b.Armies.Add(new(){Owner=0,Side=0});
  var mixed=new Robot(1,0,RobotParts.Complete) with {BodyOrigin=1};
  b.Units.Add(new(){Id=1,Owner=0,Position=new(4,4),Robot=mixed});b.Capture(BattlePhase.Choose);
  Set("_robotScenes",new[]{b});Set("_robotPlayback",new RobotBattlePlayback(new[]{b}));Call("FinishTransport");
  Check((double)Get("_battleIntroAge")==0,"Intro starts at battle entry");
  Call("RobotBattleInput",.7d,false,true);Check((double)Get("_battleAge")==0,"Intro prevents playback/skip");
  Check((float)Call("BattlePartOpacity",true)==1 && (float)Call("BattlePartOpacity",false)==1,"All parts first");
  Call("RobotBattleInput",1d,false,false);Check((float)Call("BattlePartOpacity",false)==0 && (float)Call("BattlePartOpacity",true)==1,"Foreign parts fade first");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  var batch=(SpriteBatch)Get("_spriteBatch");var captures=new List<Color[]>();
  foreach(double age in new[]{0d,1.7d,2.2d,2.7d}){
   Set("_battleIntroAge",age);GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawRobotBattle");batch.End();
   var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);captures.Add(pixels);
   using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"intro-{age:0.0}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  }
  Check(captures.Zip(captures.Skip(1)).All(pair=>!pair.First.SequenceEqual(pair.Second)),"All intro stages render differently");
  Call("RobotBattleInput",.1d,false,false);Check((double)Get("_battleAge")>0,"Playback begins after intro");
  Console.WriteLine("PASS: first entry, protected playback, foreign fade, crossfade and resumed playback.");Exit();
 }
}

