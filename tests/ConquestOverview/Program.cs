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
  world.HumanPlayers.Add(3);
  var p=new ConquestPresentation(world);var main=p.Cards.Where(c=>!c.Reminder).ToArray();
  Check(main.Select(c=>c.Ruler).SequenceEqual(world.Relations.ProvisionalRanking(world).OrderByDescending(e=>e.Rank).Select(e=>e.Ruler)),"Lowest rank first main introductions");
  int child=Array.FindIndex(p.Cards,c=>c.Ruler==3 && !c.Reminder);Check(p.Cards[child-2].Ruler==0 && p.Cards[child-1].Ruler==2 && p.Cards[child-1].Reminder,"Ancestors recalled root first");
  world.HumanPlayers.Remove(3);
  Check(ConquestPresentation.Detailed(world,0,6) && ConquestPresentation.Detailed(world,5,3) && !ConquestPresentation.Detailed(world,2,4),"Only human identities and top three keep detailed animation; controlled subordinates are compact");
  var compactCard=new ConquestPresentation.Card(3,6,3,false,ConquestPresentation.FaceTour(world,3));
  var compact=new ConquestTally(world,compactCard,true);
  int compactCount=world.Relations.ProvisionalRanking(world).Count(r=>!ConquestPresentation.Detailed(world,r.Ruler,r.Rank));
  Check(Math.Abs(compact.Duration*compactCount-5)<1e-9 && Math.Abs(ConquestTally.CompactSeconds(59)*59-5)<1e-9 && ConquestTally.CompactSeconds(0)==0,"Compact group shares five seconds, including 59 rulers and no compact rulers");
  Check(compact.CellsStart==0 && compact.EntryStart==0 && compact.ScoreStart==0 && compact.RankStart==0 && compact.FaceStart==0 && compact.Painted(world,0)==compact.Cells.Length,"Compact introduction reveals all results and painted land instantly");
  var compactPresentation=new ConquestPresentation(world);int compactIndex=Array.FindIndex(compactPresentation.Cards,c=>c.Ruler==3 && !c.Reminder);
  if(compactPresentation.Cards[compactIndex].Rank>3)Check(compactIndex==0 || !compactPresentation.Cards[compactIndex-1].Reminder,"Compact introduction has no extra ancestor reminders");
  Check(ConquestPresentation.OwnCells(world,0).All(c=>ConquestPresentation.ExactCellOwner(world,c)==0) && ConquestPresentation.OwnCells(world,0).Length==1,"No subordinate land included");
  foreach(var c in main){var tour=ConquestPresentation.FaceTour(world,c.Ruler);Check(tour.Distinct().Count()==tour.Length && tour.Length==1,"Only owned faces toured on dice world");var env=ConquestPresentation.Environment(world,c.Ruler);Check(env.X>=0 && env.X<=1 && env.Y>=0 && env.Y<=1 && env.Z>=0 && env.Z<=1,"Terrain weights bounded");}
  var tally=new ConquestTally(world,p.Cards.First(c=>c.Ruler==0 && !c.Reminder));
  Check(tally.Cells.Length==1 && tally.TotalCells==3,"Tally separates direct cells and full subtree total");
  Check(tally.SubordinateCells==2,"Subordinate breakdown includes descendants without counting their land twice");
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
  multi.HumanPlayers.Add(0);
  var emptyRanking=multi.Relations.ProvisionalRanking(multi);
  Check(emptyRanking.All(r=>!ConquestPresentation.Detailed(multi,r.Ruler,r.Rank)),"Zero-cell rulers including top three and human players use compact introductions");
  var emptyPresentation=new ConquestPresentation(multi);
  Check(emptyPresentation.Cards.All(c=>!c.Reminder),"Zero-cell introductions omit ancestor reminders");
  var emptyCompact=new ConquestTally(multi,emptyPresentation.Cards.First(c=>c.Ruler==0),true);
  Check(emptyCompact.CellsStart==0 && emptyCompact.RankStart==0 && Math.Abs(emptyCompact.Duration*multi.ActiveCount-5)<1e-9,"Zero-cell results reveal instantly and share the compact group's five seconds");
  int batteryless=multi.ActiveCount-1;multi.Relations.Powered[batteryless]=false;
  var batterylessRanking=multi.Relations.ProvisionalRanking(multi);
  Check(batterylessRanking[^1].Ruler==batteryless,"Batteryless ruler ranks below every powered zero-cell ruler despite highest entry number");
  var batterylessCard=new ConquestPresentation(multi).Cards.Single(c=>c.Ruler==batteryless && !c.Reminder);
  var batterylessTally=new ConquestTally(multi,batterylessCard,true);
  Check(!batterylessTally.Powered && batterylessTally.Score(0)==-100+batteryless+1 && batterylessTally.Score(0)==ConquerorRelations.RankingScore(0,batteryless+1,multi.ActiveCount,false),"Batteryless tally uses minus 100 plus Entry consistently with ranking");
  var rankedTiles=CharacterSelectionLayout.CreateOverview(new int[multi.ActiveCount],batterylessRanking.OrderBy(r=>r.Ruler).Select(r=>r.Rank).ToArray());
  Check(rankedTiles[batterylessRanking[0].Ruler].X==8 && rankedTiles[batterylessRanking[0].Ruler].Y==0,"Portrait placement respects battery penalty ranking");
  foreach(var counts in new[]{new[]{0},new int[6],new[]{0,1,0,1,0,1},new[]{1,1,1,1,1,1},Enumerable.Range(0,20).Select(i=>i==0?1000:i).ToArray()}){
   var tiles=CharacterSelectionLayout.CreateOverview(counts);Check(tiles.Length==counts.Length && tiles.All(t=>t.X>=8 && t.X+t.Width<=28 && t.Y+t.Height<=20 && t.Width>=2 && t.Height>=2),"Portrait layout avoids fixed left two calendar columns");
   foreach(var a in tiles)foreach(var b in tiles.Where(b=>b.Slot>a.Slot))Check(!a.Bounds.Intersects(b.Bounds),"Overview portraits never overlap");
   Check(tiles.Sum(t=>t.Width*t.Height)==400,"Rectangles fill every available calendar cell");
   foreach(var tile in tiles){var source=new Rectangle(0,0,256,192);var crop=PortraitRenderer.FitSource(source,tile.Bounds,new Vector2(.5f,.3f));
    Check(source.Contains(crop) && Math.Abs(crop.Width/(double)crop.Height-tile.Bounds.Width/(double)tile.Bounds.Height)<.04,"Portrait crop stays inside source and preserves aspect ratio");}
   var ranked=Enumerable.Range(0,counts.Length).OrderByDescending(i=>counts[i]).ThenByDescending(i=>i).ToArray();
   Check(tiles[ranked[0]].X==8 && tiles[ranked[0]].Y==0,"Highest rank takes top left portrait space");
   for(int i=1;i<ranked.Length;i++)Check(tiles[ranked[i-1]].Width*tiles[ranked[i-1]].Height>=tiles[ranked[i]].Width*tiles[ranked[i]].Height,"Lower ranks never receive larger tiles");
   var equal=CharacterSelectionLayout.CreateOverview(new int[counts.Length]);
   Check(equal[^1].X==8 && equal[^1].Y==0 && equal[^1].Width*equal[^1].Height>=equal[0].Width*equal[0].Height,"Zero-cell tie follows Entry ranking for placement and shrinking");
  }
  for(int population=1;population<=20;population++){
   var counts=Enumerable.Range(0,population).Select(i=>(i*37+population)%113).ToArray();var tiles=CharacterSelectionLayout.CreateOverview(counts);
   Check(tiles.Sum(t=>t.Width*t.Height)==400 && tiles.All(t=>t.Width>=2 && t.Height>=2),"All supported populations fill the calendar with readable minimum frames");
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
  if(frame==19){
   foreach(int ruler in Enumerable.Range(0,world.ActiveCount))((HashSet<int>)Get("_conquestRevealed")).Add(ruler);
   base.Draw(t);var full=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(full);
   using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(full);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"conquest-rectangles.png"));texture.SaveAsPng(output,texture.Width,texture.Height);
   var savedTally=Get("_conquestTally");var savedPresentation=Get("_conquestPresentation");int savedCard=(int)Get("_conquestCard");var savedTiles=Get("_conquestTiles");
   var nodeOwners=world.Nodes.All.Select(n=>n.Owner).ToArray();int savedSuperior=world.Relations.Superiors[2];
   foreach(var n in world.Nodes.All.Where(n=>n.Owner==0))n.Owner=-1;world.Relations.SetSuperior(2,-1);world.Relations.Powered[0]=false;
   var noBatteryPresentation=new ConquestPresentation(world);Set("_conquestPresentation",noBatteryPresentation);Set("_conquestCard",Array.FindIndex(noBatteryPresentation.Cards,c=>c.Ruler==0 && !c.Reminder));
   Set("_conquestTally",new ConquestTally(world,noBatteryPresentation.Cards[(int)Get("_conquestCard")],true));
   var noBatteryRanking=world.Relations.ProvisionalRanking(world).OrderBy(r=>r.Ruler).ToArray();Set("_conquestTiles",CharacterSelectionLayout.CreateOverview(noBatteryRanking.Select(r=>r.Cells).ToArray(),noBatteryRanking.Select(r=>r.Rank).ToArray()));
   base.Draw(t);GraphicsDevice.GetBackBufferData(full);texture.SetData(full);using var batterylessOutput=File.Create(Path.Combine(AppContext.BaseDirectory,"conquest-batteryless.png"));texture.SaveAsPng(batterylessOutput,texture.Width,texture.Height);
   for(int i=0;i<nodeOwners.Length;i++)world.Nodes.All[i].Owner=nodeOwners[i];world.Relations.SetSuperior(2,savedSuperior);world.Relations.Powered[0]=true;
   Set("_conquestTally",savedTally);Set("_conquestPresentation",savedPresentation);Set("_conquestCard",savedCard);Set("_conquestTiles",savedTiles);
  }
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
  Call("FinishConquestOverview");Set("_conquestOverviewTurn",-1);Call("BeginConquestOverview");Set("_conquestStage",4);
  var skipButton=(Rectangle)typeof(Game1).GetProperty("ConquestSkipButton",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
  var skipCanvas=(Rectangle)Call("CanvasBounds");
  int skipX=skipCanvas.X+skipButton.Center.X*skipCanvas.Width/1920,skipY=skipCanvas.Y+skipButton.Center.Y*skipCanvas.Height/1080;
  Call("HandleInput",new MouseState(skipX,skipY,0,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(11),TimeSpan.Zero),true);
  Check((int)Get("_conquestStage")==4,"Hovering skip button leaves presentation running");
  Call("HandleInput",new MouseState(skipX,skipY,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(11),TimeSpan.Zero),true);
  Check((int)Get("_conquestStage")==5 && (float)Get("_conquestExitOpacity")==1,"Mouse skip button follows the same closing transition as Enter at scaled resolution");
  Call("ReturnToTitle");Check(!(bool)Get("_conquestOverview") && (int)Get("_conquestOverviewTurn")==-1,"Title resets presentation and audio");
  Set("_titleMenuSelection",2);Call("TitleStartMenuInput",false,true,false,new KeyboardState(),true);
  Check(Get("_screen").ToString()=="PlayerCount","New game title action begins normal setup");
  world.SetWorld(123);world.SetCast(456);world.SetTerritoryRights(789);world.SelectPlayer(0);world.HumanPlayers.Add(0);
  foreach(int ruler in world.LandOrder())world.ClaimLand(ruler,world.Cells.Where(c=>world.Owners[c.Id]<0).Take(world.TerritoryRights[ruler]).Select(c=>c.Id).ToArray());
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"Fortune"));Set("_landAge",5d);
  Call("UpdateLandSetup",0d,new MouseState(),true,false,false);
  Check(world.TutorialStep==0 && (bool)Get("_conquestOverview"),"Normal new game enters conquest overview after fortune without tutorial mode");
  Call("FinishConquestOverview");
  Set("_demoActive",true);Set("_demoIntro",false);
  for(int turn=0;turn<2;turn++){
   world.Population.Advance(world);Call("RestoreDomesticCamera");
   Check((bool)Get("_conquestOverview"),"Demo starts overview on each new global turn");
   double stepAge=(double)Get("_demoStepAge");int ticks=0;var introduced=new HashSet<int>();
   while((bool)Get("_conquestOverview") && ticks++<10000){
    if((int)Get("_conquestStage")==4){var cards=((ConquestPresentation)Get("_conquestPresentation")).Cards;var card=cards[(int)Get("_conquestCard")];if(!card.Reminder)introduced.Add(card.Ruler);}
    Call("HandleInput",new MouseState(),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(ticks*.25),TimeSpan.FromSeconds(.25)),true);
   }
   Check(ticks<10000 && introduced.Count==world.ActiveCount,"Demo naturally finishes every ruler's introduction without skipping");
   Check((double)Get("_demoStepAge")==stepAge,"Scripted demo actions pause during overview");
   Call("RestoreDomesticCamera");Check(!(bool)Get("_conquestOverview"),"Demo does not repeat overview within the same global turn");
  }
  world.Population.Advance(world);Call("RestoreDomesticCamera");
  Call("HandleInput",new MouseState(),new KeyboardState(Keys.Escape),new GameTime(TimeSpan.Zero,TimeSpan.FromSeconds(.01)),true);
  Check((float)Get("_demoFade")==0,"Escape exits demo during overview");
  Call("UpdateDemo",.81d,true,false,false,false,0d);
  Check(!(bool)Get("_demoActive") && !(bool)Get("_conquestOverview") && Get("_screen").ToString()=="Title","Demo exit clears overview and returns to title");
  Console.WriteLine("PASS: normal new game conquest overview, monotonic outro fade, no globe reappearance behind curtain, preserved intro fade, cell paint and static addition formula, lower-rank-first introductions, area portraits, four-second ranking tree, ancestor recalls, exact owner cells, terrain mix, curtain timing, full playback, skip, and preserved game/view state.");Exit();
 }
}

