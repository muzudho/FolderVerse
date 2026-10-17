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
  var batch=(SpriteBatch)Get("_spriteBatch");var draw=typeof(Game1).GetMethod("DrawBattleSupplies",F)!;
  Color[] Supplies(BattleUnitView unit){
   var previous=GraphicsDevice.Viewport;using var target=new RenderTarget2D(GraphicsDevice,120,40);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(Color.Transparent);
   batch.Begin(samplerState:SamplerState.PointClamp);draw.Invoke(this,new object[]{unit,6,10,1f});batch.End();GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Viewport=previous;
   var pixels=new Color[120*40];target.GetData(pixels);return pixels;
  }
  void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  var empty=new BattleUnitView(1,0,BattleRole.Soldier,Point.Zero,BattleFacing.North,0,0,0,false,false);
  Check(Supplies(empty).All(p=>p.A==0),"Zero supplies hide icons, numbers and background");
  var shield=Supplies(empty with {Shields=2});var rifle=Supplies(empty with {Rifles=3});
  Check(shield.Any(p=>p.A>0) && rifle.Any(p=>p.A>0) && !shield.SequenceEqual(rifle),"Nonzero shield and rifle each retain their own icon");
  Check(!shield.Where((p,i)=>i%120>=35).Any(p=>p.A>0) && !rifle.Where((p,i)=>i%120>=35).Any(p=>p.A>0),"Zero slots leave neither number nor icon nor trailing background");
  var mixed=Supplies(empty with {Shields=2,Daggers=1});Check(!mixed.Where((p,i)=>i%120>=60).Any(p=>p.A>0),"Mixed counters compact remaining nonzero equipment");
  Check(Supplies(empty with {Shields=2,Fallen=true}).All(p=>p.A==0) && Supplies(empty with {Rifles=3,Retreated=true}).All(p=>p.A==0),"Fallen and retreated counters remain hidden");
  Console.WriteLine("PASS: zero equipment icons/numbers/background hidden, nonzero supplies retained and compacted.");
  var w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  battle=new BattleState(1){TargetNode=w.Nodes.Current(0).Id,Defender=0,Kind=BattleKind.Edge};
  battle.Armies.Add(new(){Owner=0,Side=0});
  for(int i=0;i<4;i++){var u=new BattleUnit{Id=i+1,Owner=0,Position=new Point(3+i,4),Facing=(BattleFacing)i};battle.Units.Add(u);battle.Events.Add(new(1,BattlePhase.Consume,u.Id,"盾で防いだ"));}
  battle.Units[0].Shields=2;battle.Units[0].Daggers=1;battle.Units[0].Rifles=0;
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