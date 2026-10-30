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
  // Nine direction codes, unlit above / passing-shot glow below.
  var ui=(UiPainter)Get("_ui");var viewport=GraphicsDevice.Viewport;
  using(var ports=new RenderTarget2D(GraphicsDevice,900,200)){
   GraphicsDevice.SetRenderTarget(ports);GraphicsDevice.Clear(new Color(24,43,51));batch.Begin(samplerState:SamplerState.PointClamp);
   for(int code=0;code<9;code++)for(int row=0;row<2;row++){
    var tile=new Rectangle(code*100,row*100,98,98);BattleObstacleArt.Draw(ui,tile,BattleObstacleStyle.Tree);
    var state=new BattleState(1);state.Walls.Add(Point.Zero);state.WallShotDirections[Point.Zero]=code;
    foreach(BattleFacing facing in Enum.GetValues<BattleFacing>())if(state.WallPassesShot(Point.Zero,facing)){
     var step=BattleState.Forward(facing);BattleObstacleArt.DrawShotMarker(ui,new Vector2(tile.Center.X+step.X*39,tile.Center.Y+step.Y*39),facing,row);
    }
   }
   batch.End();GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Viewport=viewport;
   var portPixels=new Color[900*200];ports.GetData(portPixels);
   for(int code=0;code<9;code++){
    bool changed=Enumerable.Range(0,100*100).Any(i=>portPixels[(i/100)*900+code*100+i%100]!=portPixels[(100+i/100)*900+code*100+i%100]);
    Check(changed==(code!=0),"Only directional wall markers glow");
   }
   using var file=File.Create(Path.Combine(AppContext.BaseDirectory,"wall-shot-directions.png"));ports.SaveAsPng(file,900,200);
  }
  using(var gates=new RenderTarget2D(GraphicsDevice,800,200)){
   GraphicsDevice.SetRenderTarget(gates);GraphicsDevice.Clear(new Color(24,43,51));batch.Begin();
   for(int facing=0;facing<4;facing++)for(int bent=0;bent<2;bent++)BattleFortificationArt.Draw(ui,new(facing*200+bent*100,0,98,98),new(Point.Zero,FortificationKind.Gate,0,(BattleFacing)facing,bent==0?2:1));
   for(int i=0;i<4;i++)BattleFortificationArt.Draw(ui,new(i*150,100,98,98),new(Point.Zero,(FortificationKind)i,0,BattleFacing.North));
   batch.End();GraphicsDevice.SetRenderTarget(null);GraphicsDevice.Viewport=viewport;
   var colors=new Color[800*200];gates.GetData(colors);
   Color[] Tile(int index)=>Enumerable.Range(0,100*100).Select(i=>colors[i/100*800+index*100+i%100]).ToArray();
   for(int i=0;i<8;i++)for(int j=0;j<i;j++)Check(!Tile(i).SequenceEqual(Tile(j)),"All four gate directions and both damage states have distinct artwork");
   using var file=File.Create(Path.Combine(AppContext.BaseDirectory,"fortification-art.png"));gates.SaveAsPng(file,800,200);
  }
  var w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  battle=new BattleState(1){TargetNode=w.Nodes.Current(0).Id,Defender=0,Kind=BattleKind.Edge};
  battle.Armies.Add(new(){Owner=0,Side=0});
  for(int i=0;i<4;i++){var u=new BattleUnit{Id=i+1,Owner=0,Position=new Point(3+i,4),Facing=(BattleFacing)i};battle.Units.Add(u);battle.Events.Add(new(1,BattlePhase.Consume,u.Id,"盾で防いだ"));}
  battle.Units[0].Shields=2;battle.Units[0].Daggers=1;battle.Units[0].Rifles=0;
  battle.Turn=1;battle.Capture(BattlePhase.Consume);
  Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Set("_robotPlaying",false);
 }
 protected override void Update(GameTime t){}
 void DaggerScene(){
  battle=new BattleState(43,new(){PersuasionPercent=0});battle.Armies.Add(new(){Owner=0,Side=0});battle.Armies.Add(new(){Owner=1,Side=2});
  var orders=new Dictionary<long,BattleAction>();
  for(int i=0;i<4;i++){
   var cell=new Point(i%2==0?2:6,i<2?2:6);var direction=BattleState.Forward((BattleFacing)i);
   var target=new BattleUnit{Id=100+i,Owner=0,Position=cell,Facing=BattleFacing.North,Shields=0};var attacker=new BattleUnit{Id=200+i,Owner=1,Position=cell+direction,Facing=(BattleFacing)((i+2)%4)};
   battle.Units.Add(target);battle.Units.Add(attacker);orders[target.Id]=new(Point.Zero,target.Facing);orders[attacker.Id]=new(Point.Zero,attacker.Facing,BattleWeapon.Dagger);
  }
  BattleTurnResolver.Advance(battle,orders);
  for(int i=0;i<4;i++){
   var recoil=BattleAttackArt.Recoil(battle,battle.Turn,100+i,.5f);var toward=BattleState.Forward((BattleFacing)i);
   if(recoil.X*toward.X+recoil.Y*toward.Y>=0 || Math.Abs(recoil.Length()-14)>.01)throw new Exception("Dagger recoil must move away from the incoming direction");
  }
  if(BattleAttackArt.Recoil(battle,battle.Turn,999,.5f)!=Vector2.Zero || BattleAttackArt.Recoil(battle,battle.Turn,100,0)!=Vector2.Zero || BattleAttackArt.Recoil(battle,battle.Turn,100,-1)!=Vector2.Zero)throw new Exception("No recoil before impact or for a missed target");
  Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Set("_robotPendingFocus",-1);
 }
 void FortScene(bool sea){
  var w=(WorldSetup)Get("_setup");battle=new BattleState(99){TargetNode=w.Nodes.Current(0).Id,Kind=BattleKind.Node,Defender=0};
  battle.BuildTerrain(sea?BattleTerrain.Sea:BattleTerrain.Fort);
  battle.AddArmy(0,battle.TargetNode,Enumerable.Range(0,3).Select(_=>w.Robots.Create(0,RobotParts.Complete)));
  battle.AddArmy(1,battle.TargetNode+1,Enumerable.Range(0,3).Select(_=>w.Robots.Create(1,RobotParts.Complete)));
  battle.Deploy();battle.BuildFortifications(12);battle.Capture(BattlePhase.Choose);
  Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Set("_robotRound",0);Set("_battleWave",0);Set("_robotPendingFocus",-1);Set("_battleAge",0d);
 }
 protected override void Draw(GameTime t){
  if(frame==1){battle.Finished=true;battle.Winner=0;Set("_robotPendingFocus",0);Set("_robotResultAge",4d);}
  if(frame==2)DaggerScene();
  if(frame==5 || frame==6)FortScene(frame==6);
  if(frame>=2 && frame<=4){var phase=frame==2?BattlePhase.Receive:frame==3?BattlePhase.Consume:BattlePhase.Fall;int index=battle.Frames.FindIndex(f=>f.Phase==phase);Set("_battleAge",index*.25);if(BattleAttackArt.ImpactProgress(battle,index,index+.5)<0)throw new Exception("Impact feedback spans receiving, consumption and fall");}
  GraphicsDevice.Clear(Color.Black);var batch=(SpriteBatch)Get("_spriteBatch");batch.Begin();
  typeof(Game1).GetMethod("DrawRobotBattle",F)!.Invoke(this,null);batch.End();
  if(frame>=2 && frame<=4)for(int i=0;i<4;i++)if(battle.Units.Single(u=>u.Id==100+i).Position!=new Point(i%2==0?2:6,i<2?2:6))throw new Exception("Visual hitback must not move the battle cell");
  var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);
  using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,frame==0?"shield.png":frame==1?"victory.png":frame>=5?$"fortification-{frame}.png":$"dagger-{frame}.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  if(++frame==7){Console.WriteLine("PASS: shields, victory and directional dagger impacts/recoil in all four directions across receive/consume/fall render without moving board cells.");Exit();}
 }
}
