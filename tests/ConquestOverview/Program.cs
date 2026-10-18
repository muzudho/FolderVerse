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
  Check(main.Select(c=>c.Ruler).SequenceEqual(world.Relations.ProvisionalRanking(world).OrderByDescending(e=>e.Rank).Select(e=>e.Ruler)),"Lowest rank first main introductions");
  int child=Array.FindIndex(p.Cards,c=>c.Ruler==3 && !c.Reminder);Check(p.Cards[child-2].Ruler==0 && p.Cards[child-1].Ruler==2 && p.Cards[child-1].Reminder,"Ancestors recalled root first");
  Check(ConquestPresentation.OwnCells(world,0).All(c=>ConquestPresentation.ExactCellOwner(world,c)==0) && ConquestPresentation.OwnCells(world,0).Length==1,"No subordinate land included");
  foreach(var c in main){var tour=ConquestPresentation.FaceTour(world,c.Ruler);Check(tour.Distinct().Count()==tour.Length && tour.Length==1,"Only owned faces toured on dice world");var env=ConquestPresentation.Environment(world,c.Ruler);Check(env.X>=0 && env.X<=1 && env.Y>=0 && env.Y<=1 && env.Z>=0 && env.Z<=1,"Terrain weights bounded");}
  var tally=new ConquestTally(world,p.Cards.First(c=>c.Ruler==0 && !c.Reminder));
  Check(tally.Cells.Length==1 && tally.TotalCells==3,"Tally separates direct cells and full subtree total");
  Check(tally.Painted(world,1.17)==0 && tally.Painted(world,1.18)==1 && tally.Counted(world,tally.CountEnd)==3,"One painted cell increments counter, then descendants contribute");
  Check(tally.CellsStart<tally.EntryStart && tally.EntryStart<tally.ScoreStart && tally.ScoreStart<tally.RankStart && tally.RankStart<tally.FaceStart,"Cells, entry, points, rank and portrait reveal in order");
  Check(tally.Score(tally.ConvertStart)==301 && Math.Abs(tally.Duration-tally.FaceStart-4)<1e-8,"Static arithmetic score and four-second rank");
  Check(ConquerorRelations.RankingScore(10,9,6)==1009,"Ten cells plus Entry nine is 1009");
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
  Check(emptyTally.TotalCells==0 && emptyTally.Score(emptyTally.ConvertStart)==1,"Zero cells retain positive entry points");
  foreach(var counts in new[]{new[]{0},new[]{1,1,1,1,1,1},Enumerable.Range(0,20).Select(i=>i==0?1000:i).ToArray()}){
   var tiles=CharacterSelectionLayout.CreateOverview(counts);Check(tiles.Length==counts.Length && tiles.All(t=>t.X>=8 && t.X+t.Side<=28 && t.Y+t.Side<=20 && t.Side>=1),"Portrait layout avoids fixed left two calendar columns");
   foreach(var a in tiles)foreach(var b in tiles.Where(b=>b.Slot>a.Slot))Check(!a.Bounds.Intersects(b.Bounds),"Overview portraits never overlap");
  }
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
  if(frame==14){var presentation=(ConquestPresentation)Get("_conquestPresentation");Set("_conquestCard",Array.FindIndex(presentation.Cards,c=>c.Ruler==0&&!c.Reminder));Call("SetConquestCard");((HashSet<int>)Get("_conquestRevealed")).Clear();tally=(ConquestTally)Get("_conquestTally");}
  Set("_conquestStage",frame switch{1=>1,6 or 7 or 8=>5,9 or 10=>6,11 or 12=>3,13=>8,_=>4});
  Set("_conquestAge",frame switch{0=>1.18,1=>.5,2=>tally.CountEnd+.75,3=>tally.ConvertStart+.1,4 or 5=>tally.RankStart+2,6=>0d,7=>.75d,8=>1.49999d,9=>0d,10=>.3d,11=>0d,12=>1.49d,13=>0d,14=>0d,15=>tally.CellsStart+.1,16=>tally.EntryStart+.1,17=>tally.ScoreStart+.1,18=>tally.RankStart+.1,19=>tally.FaceStart+.1,_=>0d});
  if(frame!=1 && frame<6 || frame>=14)Call("UpdateConquestOverview",0d,false);
  if(frame>=14){Check(((HashSet<int>)Get("_conquestRevealed")).Contains(0)==(frame==19),"Portrait remains hidden until after provisional rank");}
  if(frame==5){var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using(var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"conquest-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);}
  var canvas=(Rectangle)Call("CanvasBounds");double brightness=0;
  for(int y=230;y<380;y+=5)for(int x=220;x<410;x+=5){int px=canvas.X+x*canvas.Width/1920,py=canvas.Y+y*canvas.Height/1080;var c=pixels[py*pp.BackBufferWidth+px];if(!(c.R>c.G*3 && c.R>c.B*2))brightness+=c.R+c.G+c.B;}
  if(frame==6){exitStart=brightness;Check(exitStart>0,"Outro begins from the globe already on screen");}
  if(frame==7){exitMiddle=brightness;Check(exitMiddle>0 && exitMiddle<exitStart*.65,"Outro darkens instead of fading back in");}
  if(frame==8)Check(brightness<exitStart*.02,"Globe disappears at end of fade-out");
  if(frame is 9 or 10)Check(brightness==0,"Globe remains absent while closing curtain starts and moves");
  if(frame==11)Check(brightness==0,"Intro still starts completely black");
  if(frame==12)Check(brightness>exitMiddle,"Intro still fades in correctly");
  if(frame is 1 or 13)Check(pixels.All(c=>c.R>c.G*3 && c.R>c.B*2),"Closed curtain covers every HUD/header/footer pixel on entry and return to domestic");
  if(frame++<19)return;
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
  Call("ReturnToTitle");Check(!(bool)Get("_conquestOverview") && (int)Get("_conquestOverviewTurn")==-1,"Title resets presentation and audio");Console.WriteLine("PASS: monotonic outro fade, no globe reappearance behind curtain, preserved intro fade, cell paint and static addition formula, lower-rank-first introductions, area portraits, four-second ranking tree, ancestor recalls, exact owner cells, terrain mix, curtain timing, full playback, skip, and preserved game/view state.");Exit();
 }
}

