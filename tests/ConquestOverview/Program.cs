using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new OverviewGame();game.Run();
sealed class OverviewGame:Game1{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 int frame;WorldSetup world=null!;int[] owners=null!;bool batteries;object originalWorld=null!;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  world=(WorldSetup)Get("_setup");world.PrepareTutorialStepOne(123,456,789);world.CompleteLand(90);world.SelectPlayer(0);world.HumanPlayers.Add(0);
  world.Relations.SetSuperior(2,0);world.Relations.SetSuperior(3,2);
  var p=new ConquestPresentation(world);var main=p.Cards.Where(c=>!c.Reminder).ToArray();
  Check(main.Select(c=>c.Ruler).SequenceEqual(world.Relations.RankedForest(world).Select(e=>e.Ruler)),"Breadth first ranked main introductions");
  int child=Array.FindIndex(p.Cards,c=>c.Ruler==3 && !c.Reminder);Check(p.Cards[child-2].Ruler==0 && p.Cards[child-1].Ruler==2 && p.Cards[child-1].Reminder,"Ancestors recalled root first");
  Check(ConquestPresentation.OwnCells(world,0).All(c=>ConquestPresentation.ExactCellOwner(world,c)==0) && ConquestPresentation.OwnCells(world,0).Length==1,"No subordinate land included");
  foreach(var c in main){var tour=ConquestPresentation.FaceTour(world,c.Ruler);Check(tour.Distinct().Count()==tour.Length && tour.Length==1,"Only owned faces toured on dice world");var env=ConquestPresentation.Environment(world,c.Ruler);Check(env.X>=0 && env.X<=1 && env.Y>=0 && env.Y<=1 && env.Z>=0 && env.Z<=1,"Terrain weights bounded");}
  var multi=new WorldSetup();multi.SetWorld(123);multi.SetCast(456);multi.SetPlacement(789);
  foreach(var n in multi.Nodes.All)n.Owner=0;
  var tourAll=ConquestPresentation.FaceTour(multi,0);Check(tourAll.Length==6,"All six owned faces included once");
  double length=0;int prev=4;foreach(int f in tourAll){length+=Math.Acos(Math.Clamp(Vector3.Dot(multi.Cells.First(c=>c.Face==prev).Normal,multi.Cells.First(c=>c.Face==f).Normal),-1,1));prev=f;}
  Check(Math.Abs(length-5*Math.PI/2)<.0001,"Six-face tour uses shortest five quarter turns from starting face");
  owners=(int[])world.Owners.Clone();originalWorld=Get("_world");var net=(CubeNet)Get("_net");net.ShowBatteries=false;batteries=net.ShowBatteries;
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("RestoreDomesticCamera");batteries=net.ShowBatteries;
  Check((bool)Get("_conquestOverview"),"First domestic boundary starts presentation");
  Call("UpdateConquestOverview",.25d,false);Check((int)Get("_conquestStage")==0,"Curtain closes before hold");
  for(int i=0;i<3;i++)Call("UpdateConquestOverview",.25d,false);Check((int)Get("_conquestStage")==1,"Curtain hold stage");
  for(int i=0;i<4;i++)Call("UpdateConquestOverview",.25d,false);Check((int)Get("_conquestStage")==1,"Hold lasts 1.5 seconds");
  for(int i=0;i<20 && (int)Get("_conquestStage")!=4;i++)Call("UpdateConquestOverview",.25d,false);
  Check((int)Get("_conquestStage")==4,"Opening and 1.5 second fade reach cards");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==0)for(int i=0;i<7;i++)Call("UpdateConquestOverview",.25d,false);
  if(frame==1){Set("_conquestStage",1);Set("_conquestAge",.5d);}
  if(frame==2){Set("_conquestStage",4);Set("_conquestAge",0d);}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using(var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"conquest-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);}
  if(frame++<2)return;
  int guard=0;while((bool)Get("_conquestOverview") && guard++<2500)Call("UpdateConquestOverview",.25d,false);
  Check(guard<2500,"All introductions and closing transition finish");Check(owners.SequenceEqual(world.Owners) && ReferenceEquals(originalWorld,Get("_world")) && ((CubeNet)Get("_net")).ShowBatteries==batteries,"Presentation preserves game and player view");
  Call("RestoreDomesticCamera");Check(!(bool)Get("_conquestOverview"),"Same turn handoff does not repeat presentation");
  world.Population.Advance(world);Call("RestoreDomesticCamera");Check((bool)Get("_conquestOverview"),"New global turn starts presentation again");Call("UpdateConquestOverview",.1d,true);Check((int)Get("_conquestStage")==5,"Enter/Escape skip follows closing transition");
  Call("FinishConquestOverview");Call("BeginConquestOverview");
  // New turn was already introduced; explicitly exercise the actual input interception.
  Set("_conquestOverviewTurn",-1);Call("BeginConquestOverview");
  Call("HandleInput",new MouseState(),new KeyboardState(Keys.Enter),new GameTime(TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(.01)),true);
  Check((int)Get("_conquestStage")==5,"Real Enter input skips presentation before domestic input");
  Call("ReturnToTitle");Check(!(bool)Get("_conquestOverview") && (int)Get("_conquestOverviewTurn")==-1,"Title resets presentation and audio");Console.WriteLine("PASS: BFS ranking, ancestor recalls, exact owner cells, terrain mix, curtain timing, full playback, skip, and preserved game/view state.");Exit();
 }
}

