using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new EffectsGame();game.Run();
sealed class EffectsGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 int frame;BattleState battle=null!;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  var w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  battle=new BattleState(1){TargetNode=w.Nodes.Current(0).Id,Defender=0,Kind=BattleKind.Edge};
  battle.Armies.Add(new(){Owner=0,Side=0});
  for(int i=0;i<4;i++){var u=new BattleUnit{Id=i+1,Owner=0,Position=new Point(3+i,4),Facing=(BattleFacing)i};battle.Units.Add(u);battle.Events.Add(new(1,BattlePhase.Consume,u.Id,"盾で防いだ"));}
  battle.Turn=1;battle.Capture(BattlePhase.Consume);
  Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Set("_robotPlaying",false);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){battle.Finished=true;battle.Winner=0;Set("_robotPendingFocus",0);Set("_robotResultAge",4d);}
  GraphicsDevice.Clear(Color.Black);var batch=(SpriteBatch)Get("_spriteBatch");batch.Begin();
  typeof(Game1).GetMethod("DrawRobotBattle",F)!.Invoke(this,null);batch.End();
  var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);
  using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,frame==0?"shield.png":"victory.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  if(++frame==2){Console.WriteLine("PASS: shields in all four facings and victory during the fifth second render.");Exit();}
 }
}