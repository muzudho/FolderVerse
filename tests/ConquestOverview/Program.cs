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
 double exitStart,exitMiddle;int frame;WorldSetup world=null!;int[] owners=null!;bool batteries;object originalWorld=null!;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  world=(WorldSetup)Get("_setup");world.PrepareTutorialStepOne(123,456,789);world.CompleteLand(90);world.SelectPlayer(0);world.HumanPlayers.Add(0);
  world.Relations.SetSuperior(2,0);world.Relations.SetSuperior(3,2);
  var p=new ConquestPresentation(world);var main=p.Cards.Where(c=>!c.Reminder).ToArray();
  Check(main.Select(c=>c.Ruler).SequenceEqual(world.Relations.RankedForest(world).Select(e=>e.Ruler)),"Breadth first ranked main introductions");
  int child=Array.FindIndex(p.Cards,c=>c.Ruler==3 && !c.Reminder);Check(p.Cards[child-2].Ruler==0 && p.Cards[child-1].Ruler==2 && p.Cards[child-1].Reminder,"Ancestors recalled root first");
  Check(ConquestPresentation.OwnCells(world,0).All(c=>ConquestPresentation.ExactCellOwner(world,c)==0) && ConquestPresentation.OwnCells(world,0).Length==1,"No subordinate land included");
  foreach(var c in main){var tour=ConquestPresentation.FaceTour(world,c.Ruler);Check(tour.Distinct().Count()==tour.Length && tour.Length==1,"Only owned faces toured on dice world");var env=ConquestPresentation.Environment(world,c.Ruler);Check(env.X>=0 && env.X<=1 && env.Y>=0 && env.Y<=1 && env.Z>=0 && env.Z<=1,"Terrain weights bounded");}
  var tally=new ConquestTally(world,p.Cards.First(c=>c.Ruler==0 && !c.Reminder));
  Check(tally.Cells.Length==1 && tally.TotalCells==3,"Tally separates direct cells and full subtree total");
  Check(tally.Painted(world,1.17)==0 && tally.Painted(world,1.18)==1 && tally.Counted(world,tally.CountEnd)==3,"One painted cell increments counter, then descendants contribute");
  Check(Math.Abs(tally.ConvertStart-tally.CountEnd-1.5)<1e-8,"Entry portrait holds for 1.5 seconds before conversion");
  for(int i=0;i<=tally.TotalCells;i++)Check(tally.RemainingCells(tally.ConvertStart+i*.10)==tally.TotalCells-i && tally.Score(tally.ConvertStart+i*.10)==i*100,"Each consumed cell contributes exactly 100 points");
  Check(tally.RemainingEntry(tally.EntryConvertEnd)==0 && tally.Score(tally.EntryConvertEnd)==299 && Math.Abs(tally.Duration-tally.RankStart-4)<1e-8,"Entry subtracts one and ranking stays four seconds");
  var entryThree=new ConquestTally(world,p.Cards.First(c=>c.Ruler==2 && !c.Reminder));
  for(int i=0;i<=3;i++)Check(entryThree.RemainingEntry(entryThree.CellConvertEnd+i*.06)==3-i && entryThree.Score(entryThree.CellConvertEnd+i*.06)==entryThree.TotalCells*100-i,"Each Entry unit subtracts exactly one point");
  var multi=new WorldSetup();multi.SetWorld(123);multi.SetCast(456);multi.SetPlacement(789);
  foreach(var n in multi.Nodes.All)n.Owner=0;
  var tourAll=ConquestPresentation.FaceTour(multi,0);Check(tourAll.Length==6,"All six owned faces included once");
  double length=0;int prev=4;foreach(int f in tourAll){length+=Math.Acos(Math.Clamp(Vector3.Dot(multi.Cells.First(c=>c.Face==prev).Normal,multi.Cells.First(c=>c.Face==f).Normal),-1,1));prev=f;}
  Check(Math.Abs(length-5*Math.PI/2)<.0001,"Six-face tour uses shortest five quarter turns from starting face");
  var paintedTour=new ConquestTally(multi,new ConquestPresentation(multi).Cards.First(c=>c.Ruler==0 && !c.Reminder));
  for(double age=0;age<paintedTour.TourSeconds;age+=.01)Check(paintedTour.Painted(multi,age)<=paintedTour.Cells.Length,"Multi-face reveal remains bounded");
  Check(paintedTour.Painted(multi,paintedTour.TourSeconds)==paintedTour.Cells.Length,"All faces finish painting");
  foreach(var n in multi.Nodes.All)n.Owner=-1;
  var emptyTally=new ConquestTally(multi,new ConquestPresentation(multi).Cards.First(c=>c.Ruler==0 && !c.Reminder));
  Check(emptyTally.TotalCells==0 && emptyTally.Score(emptyTally.EntryConvertEnd)==-1,"Zero-cell entry produces negative score without skipping registration penalty");
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
  var tally=(ConquestTally)Get("_conquestTally");
  Set("_conquestStage",frame switch{1=>1,6 or 7 or 8=>5,9 or 10=>6,11 or 12=>3,_=>4});
  Set("_conquestAge",frame switch{0=>1.18,1=>.5,2=>tally.CountEnd+.75,3=>tally.ConvertStart+.1,4 or 5=>tally.RankStart+2,6=>0d,7=>.75d,8=>1.49999d,9=>0d,10=>.3d,11=>0d,12=>1.49d,_=>0d});
  if(frame!=1 && frame<6)Call("UpdateConquestOverview",0d,false);
  if(frame==5){var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using(var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"conquest-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);}
  var canvas=(Rectangle)Call("CanvasBounds");double brightness=0;
  for(int y=250;y<850;y+=10)for(int x=800;x<1400;x+=10){int px=canvas.X+x*canvas.Width/1920,py=canvas.Y+y*canvas.Height/1080;var c=pixels[py*pp.BackBufferWidth+px];brightness+=c.R+c.G+c.B;}
  if(frame==6){exitStart=brightness;Check(exitStart>0,"Outro begins from the globe already on screen");}
  if(frame==7){exitMiddle=brightness;Check(exitMiddle>0 && exitMiddle<exitStart*.65,"Outro darkens instead of fading back in");}
  if(frame==8)Check(brightness<exitStart*.02,"Globe disappears at end of fade-out");
  if(frame is 9 or 10)Check(brightness==0,"Globe remains absent while closing curtain starts and moves");
  if(frame==11)Check(brightness==0,"Intro still starts completely black");
  if(frame==12)Check(brightness>exitMiddle,"Intro still fades in correctly");
  if(frame++<12)return;
  Call("FinishConquestOverview");Set("_conquestOverviewTurn",-1);Call("BeginConquestOverview");
  int guard=0;while((bool)Get("_conquestOverview") && guard++<2500){
   Call("UpdateConquestOverview",.25d,false);
   if((int)Get("_conquestStage")==4){var card=((ConquestPresentation)Get("_conquestPresentation")).Cards[(int)Get("_conquestCard")];
    if(!card.Reminder){var globe=(WorldPreview)Get("_conquestGlobe");var timeline=(ConquestTally)Get("_conquestTally");int visible=timeline.Painted(world,(double)Get("_conquestAge"));
     Check(globe.PresentationCells.SetEquals(timeline.Cells.Take(visible)),"Rendered paint mask matches live counter and excludes subordinate cells");}}
  }
  Check(guard<2500,"All introductions and closing transition finish");Check(owners.SequenceEqual(world.Owners) && ReferenceEquals(originalWorld,Get("_world")) && ((CubeNet)Get("_net")).ShowBatteries==batteries,"Presentation preserves game and player view");
  Call("RestoreDomesticCamera");Check(!(bool)Get("_conquestOverview"),"Same turn handoff does not repeat presentation");
  world.Population.Advance(world);Call("RestoreDomesticCamera");Check((bool)Get("_conquestOverview"),"New global turn starts presentation again");Call("UpdateConquestOverview",.1d,true);Check((int)Get("_conquestStage")==5,"Enter/Escape skip follows closing transition");
  Call("FinishConquestOverview");Call("BeginConquestOverview");
  // New turn was already introduced; explicitly exercise the actual input interception.
  Set("_conquestOverviewTurn",-1);Call("BeginConquestOverview");
  Call("HandleInput",new MouseState(),new KeyboardState(Keys.Enter),new GameTime(TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(.01)),true);
  Check((int)Get("_conquestStage")==5,"Real Enter input skips presentation before domestic input");
  Check((float)Get("_conquestExitOpacity")==0 && Get("_conquestMusic")==null,"Skip before intro never reveals a globe or starts new music");
  Call("FinishConquestOverview");Set("_conquestOverviewTurn",-1);Call("BeginConquestOverview");Set("_conquestStage",3);Set("_conquestAge",.6d);Call("UpdateConquestOverview",0d,true);
  Check(Math.Abs((float)Get("_conquestExitOpacity")-.4f)<.0001,"Skip during fade-in preserves current opacity without a bright flash");
  Call("ReturnToTitle");Check(!(bool)Get("_conquestOverview") && (int)Get("_conquestOverviewTurn")==-1,"Title resets presentation and audio");Console.WriteLine("PASS: monotonic outro fade, no globe reappearance behind curtain, preserved intro fade, cell-to-score timeline, entry countdown, four-second ranking tree, BFS ranking, ancestor recalls, exact owner cells, terrain mix, curtain timing, full playback, skip, and preserved game/view state.");Exit();
 }
}

