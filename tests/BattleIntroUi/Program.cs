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
  Check((float)Call("BattlePartOpacity",true)==1 && (float)Call("BattlePartOpacity",false)==1,"All parts initially visible");
  Call("RobotBattleInput",.75d,false,true);Check((double)Get("_battleAge")==0,"Intro prevents playback/skip");
  Check((float)Call("BattlePartOpacity",true)==1 && (float)Call("BattlePartOpacity",false)==.5f,"Foreign parts half faded at 0.75 seconds");
  Call("RobotBattleInput",.75d,false,false);Check((float)Call("BattlePartOpacity",false)==0 && (float)Call("BattlePartOpacity",true)==1,"Foreign parts fade first");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  Set("_battleIntroAge",3.5d);Check((float)Call("BattlePartOpacity",true)==1,"Native parts remain for full two second hold");
  Set("_battleIntroAge",4.25d);Check((float)Call("BattlePartOpacity",true)==.5f,"Crossfade takes 1.5 seconds");
  Set("_battleIntroAge",5.75d);Check((float)typeof(Game1).GetProperty("BattleBoardDim",F)!.GetValue(this)! ==.5f,"Board reveal takes 1.5 seconds");
  var batch=(SpriteBatch)Get("_spriteBatch");
  Call("BeginPropagation");
  Vector2 initialBadge=Vector2.Zero;
  foreach(double age in new[]{0d,.25d,.5d,1d,2d}){
   Set("_transportAge",age);GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawPropagation",0,.6f+.4f*(float)(age/2),true);batch.End();
   var info=GraphicsDevice.PresentationParameters;var preview=new Color[info.BackBufferWidth*info.BackBufferHeight];GraphicsDevice.GetBackBufferData(preview);
   var area=(Rectangle)typeof(Game1).GetField("PropagationMapPanel",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
   var at=(Vector2)Call("PropagationBadgeCenter",0,area);
   if(age==0)initialBadge=at;
   if(age==.5){Check(Vector2.Distance(initialBadge,at)<.01f,"Badge fades before zooming");var color=preview[(int)(at.Y*info.BackBufferHeight/1080f)*info.BackBufferWidth+(int)(at.X*info.BackBufferWidth/1920f)];Check(Math.Abs(color.R-color.G)<25 && Math.Abs(color.G-color.B)<25,"Board is grey before zoom starts");}
   if(age==2)Check(Vector2.Distance(at,new Vector2(540,540))<1,"Zoom ends aligned with battle board");
   using var tex=new Texture2D(GraphicsDevice,info.BackBufferWidth,info.BackBufferHeight);tex.SetData(preview);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"preview-{age:0.00}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  }
  var captures=new List<Color[]>();
  foreach(double age in new[]{0d,1.5d,4.25d,5d,6.5d}){
   Set("_battleIntroAge",age);GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawRobotBattle");batch.End();
   var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);captures.Add(pixels);
   using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"intro-{age:0.0}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  }
  Check(captures.Zip(captures.Skip(1)).All(pair=>!pair.First.SequenceEqual(pair.Second)),"All intro stages render differently");
  Call("RobotBattleInput",.1d,false,false);Check((double)Get("_battleAge")>0,"Playback begins after intro");
  Console.WriteLine("PASS: first entry, protected playback, foreign fade, crossfade and resumed playback.");Exit();
 }
}

