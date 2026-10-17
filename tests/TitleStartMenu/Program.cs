using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new MenuGame();game.Run();
sealed class MenuGame:Game1{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 void Input(Point canvasPoint,ButtonState button=ButtonState.Released,params Keys[] keys){
  var canvas=(Rectangle)Call("CanvasBounds");var mouse=new MouseState(canvas.X+canvasPoint.X*canvas.Width/1920,canvas.Y+canvasPoint.Y*canvas.Height/1080,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
  Call("HandleInput",mouse,new KeyboardState(keys),new GameTime(),true);
 }
 void Click(int x,int y){Input(new(x,y));Input(new(x,y),ButtonState.Pressed);Input(new(x,y));}
 void Key(Keys key){Input(default,keys:new[]{key});Input(default);}
 string path=Path.Combine(AppContext.BaseDirectory,"menu-save-test","autosave.json.gz");int frame;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_conquestOverviewEnabled",false);Set("_autoSavePath",path);Set("_autoSaveEnabled",true);if(File.Exists(path))File.Delete(path);Call("CheckAutoSave");
  Click(960,930);Check((bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title" && (int)Get("_titleMenuSelection")==2,"Start opens menu without save, New Game selected");
  Check(!(bool)Call("UpdateDemo",11d,true,false,false,false,11d),"Idle demo cannot steal open menu");
  Click(960,517);Check((bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title","Hidden Continue row is inert without save");
  Click(960,417);Check((bool)Get("_tutorialMenuOpen"),"How to Play opens tutorial guide");
  Check(!(bool)Call("UpdateDemo",11d,true,false,false,false,11d),"Tutorial guide pauses demo");
  Click(1100,640);Check((bool)Get("_tutorialMenuOpen"),"Unimplemented steps are disabled");
  Click(1100,550);var tutorial=(WorldSetup)Get("_setup");
  Check(Get("_screen").ToString()=="WorldStatus" && tutorial.PlayerCount==1 && tutorial.HumanPlayers.SequenceEqual(new[]{0}) && tutorial.PlayerSlot==0,"Step one reaches domestic as No.1");
  Check(tutorial.Width==1 && tutorial.Height==1 && tutorial.Depth==1 && tutorial.ActiveCount==6 && tutorial.Portraits.Take(6).SequenceEqual(Enumerable.Range(0,6)) && tutorial.Looks.Take(6).All(l=>l.VariantId==0),"Dice world and original No.1 through No.6 cast");
  Check(!tutorial.LandPending && tutorial.Owners.Distinct().Count()==6 && tutorial.Robots.Nodes.Length==tutorial.Nodes.All.Length && File.Exists(path),"Normal randomized land, economy, robots and autosave initialized");
  Check(tutorial.TutorialStep==1 && tutorial.Nodes.All.All(n=>tutorial.Robots.Nodes[n.Id].Count==4 && tutorial.Robots.Nodes[n.Id].Robots.All(r=>r.Role==RobotRole.Soldier && r.Parts==RobotParts.Complete && r.CanFight && r.Owner==n.Owner)),"Every tutorial hangar starts with four complete soldiers");
  for(int seed=0;seed<10;seed++){
   var sample=new WorldSetup();sample.PrepareTutorialStepOne(seed,456,789);sample.CompleteLand(seed+100);sample.SelectPlayer(0);
   Check(WorldTerrain.HasSeaAndLand(sample.Cells,sample.WorldSeed),"Tutorial rerolls until it has both sea and land cells");
   Check(sample.Nodes.All.All(n=>sample.Robots.Nodes[n.Id].Count==4 && sample.Campaign.RobotBattles.AvailableFrom(sample,n.Owner,n.Id)==4),"Random terrain keeps four immediately deployable soldiers at every node");
   Check(sample.Nodes.All.Where(n=>n.Owner==0).Any(n=>sample.Routes.Neighbors(n).Any(r=>sample.Nodes.At(r.Target,r.Entry).Owner!=0)),"Random tutorial terrain always has a first-turn attack route");
  }
  int allSeaSeed=-1,allLandSeed=-1;var cube=WorldSetup.CreateCells(1,1,1);
  for(int seed=0;seed<10000 && (allSeaSeed<0 || allLandSeed<0);seed++){
   if(new SeedRandom(seed).Next(15)!=0)continue;
   bool sea=false,land=false;var offset=WorldTerrain.Offset(seed);
   foreach(var cell in cube)for(int y=0;y<10;y++)for(int x=0;x<10;x++){
    if(WorldTerrain.Kind(WorldTerrain.Elevation(cell.Origin+cell.U*((x+.5f)/10)+cell.V*((y+.5f)/10),offset))==TerrainKind.Sea)sea=true;else land=true;
   }
   if(!land)allSeaSeed=seed;if(!sea)allLandSeed=seed;
  }
  Check(allSeaSeed>=0 && allLandSeed>=0,"Normal random worlds can contain all-sea and all-land dice planets");
  foreach(int seed in new[]{allSeaSeed,allLandSeed}){
   var sample=new WorldSetup();sample.SetWorld(seed);Check(!WorldTerrain.HasSeaAndLand(sample.Cells,sample.WorldSeed),"Extreme terrain fixture is unchanged in normal play");
   sample.PrepareTutorialStepOne(seed,456,789);Check(sample.WorldSeed!=seed && WorldTerrain.HasSeaAndLand(sample.Cells,sample.WorldSeed),"Tutorial rejects both extreme starting worlds");
  }
  Check(tutorial.Robots.Workshops.All(w=>w.ProductionLines.Length==0),"Tutorial has no manufacturing lines for humans or AI");
  var label=typeof(Game1).GetProperty("WarReservationLabel",F)!;
  Check((string)label.GetValue(this)! =="開戦予約（なし）","No invasion reservation label");
  Call("OpenWarPlanning");var targets=((CubeNet)Get("_net")).MovementTargets;
  Check(tutorial.Population.Turn==0 && targets.Count>0,"War planning has reachable enemy targets before the first turn ends");
  int invasionTarget=targets.First();Set("_selectedMoveNode",invasionTarget);Call("BuildInvasionSources",invasionTarget);
  var targetLabel=(string)typeof(Game1).GetProperty("InvasionTargetLabel",F)!.GetValue(this)!;
  Check(targetLabel=="侵攻先："+tutorial.Nodes.DisplayName(tutorial.Nodes.All[invasionTarget]) && !targetLabel.Contains("Node "),"Invasion target uses human-readable node name without numeric ID");
  Call("InvasionClick",new Point(794,332));Call("ReserveInvasion");
  Check(tutorial.Campaign.PlannedOrders.Count==1 && tutorial.Campaign.PlannedOrders[0].Fighters==1 && (string)label.GetValue(this)! =="開戦予約（1件）","First-turn plus button reserves a soldier and updates label");
  var order=tutorial.Campaign.PlannedOrders[0];tutorial.Campaign.Plan(order);Check((string)label.GetValue(this)! =="開戦予約（1件）","Replacing same reservation does not double count");
  int subordinate=tutorial.Nodes.All[invasionTarget].Owner%5+1;tutorial.Relations.SetSuperior(subordinate,0);int subordinateSource=tutorial.Nodes.Current(subordinate).Id;
  tutorial.Campaign.Plan(new(subordinate,invasionTarget,1,subordinateSource,false));Check((string)label.GetValue(this)! =="開戦予約（1件）","Coordinated sources attacking one destination count once");
  int secondTarget=tutorial.Nodes.All.First(n=>n.Owner!=0 && n.Owner!=subordinate && n.Id!=invasionTarget).Id;
  tutorial.Campaign.Plan(new(subordinate,secondTarget,1,subordinateSource,false));Check((string)label.GetValue(this)! =="開戦予約（2件）","Different enemy destinations count as separate reservations");
  tutorial.Campaign.Plan(new(subordinate,order.SourceNode,0,subordinateSource,true));Check((string)label.GetValue(this)! =="開戦予約（1件）","Friendly movement does not count as invasion");
  tutorial.Campaign.CancelSource(subordinate,subordinateSource);
  tutorial.Campaign.CancelSource(order.Ruler,order.SourceNode);Check((string)label.GetValue(this)! =="開戦予約（なし）","Cancel returns label to none");
  tutorial.Robots.Check();Call("ReturnToTitle");Check((bool)Call("ResumeAutoSave"),"Tutorial save resumes");
  tutorial=(WorldSetup)Get("_setup");Check(tutorial.TutorialStep==1 && tutorial.Robots.Nodes.All(n=>n.Count==4),"Resume preserves step restrictions and initial stock");
  tutorial.Population.Advance(tutorial);tutorial.Industry.Queue(tutorial.Population.Turn);int beforeIndustry=tutorial.Robots.IndustryTurn;
  Call("BeginIndustry");Check(Get("_screen").ToString()=="WorldStatus" && !tutorial.Industry.Pending && tutorial.Population.Turn==1,"Step one skips industrial screen and reaches a stable domestic boundary without extra global turns");
  Check(tutorial.Robots.IndustryTurn==beforeIndustry+tutorial.Industry.Result,"Silent industrial processing retains disposal and assembly timing");
  var nameMethod=typeof(Game1).GetMethod("MedallionName",BindingFlags.NonPublic|BindingFlags.Static)!;
  Check((string)nameMethod.Invoke(null,new object[]{"エミリア・アルメ"})! =="エミリア アルメ" && (string)nameMethod.Invoke(null,new object[]{"エミリア　 アルメ"})! =="エミリア アルメ","Medallion keeps full name with one separating space");
  var font=(SpriteFont)Get("_font");
  var layout=((string Glyph,Vector2 At,float Rotation,Vector2 Origin)[])Call("MedallionNameLayout",Vector2.Zero,"エミリア・アルメ");
  Check(layout.All(l=>Math.Abs(l.At.Length()-132)<.001),"Medallion engraving radius moves outward by two pixels");
  for(int i=1;i<layout.Length;i++){
   float bounds=(font.MeasureString(layout[i-1].Glyph).X+font.MeasureString(layout[i].Glyph).X)*(.36f*1.2f)/(2*132);
   Check(Math.Abs(layout[i].Rotation-layout[i-1].Rotation-bounds-MathHelper.ToRadians(5))<.0001,"Every character has exactly five degrees of additional spacing");
  }
  float firstEdge=layout[0].Rotation-font.MeasureString(layout[0].Glyph).X*(.36f*1.2f)/(2*132),lastEdge=layout[^1].Rotation+font.MeasureString(layout[^1].Glyph).X*(.36f*1.2f)/(2*132);
  Check(Math.Abs(firstEdge+lastEdge)<.0001,"Curved full name is centered at twelve o'clock");
  float band=(float)typeof(Game1).GetField("MedallionBandWidth",BindingFlags.NonPublic|BindingFlags.Static)!.GetRawConstantValue()!;
  Check(Math.Abs(band-26.4f)<.0001,"Engraving band is 1.2 times its previous width");
  var engraving=(Texture2D)Call("MedallionLetterTexture","ミ",new Color(239,88,97));var engraved=new Color[engraving.Width*engraving.Height];engraving.GetData(engraved);
  var originalBounds=font.GetGlyphs()['ミ'].BoundsInTexture;var original=new Color[originalBounds.Width*originalBounds.Height];font.Texture.GetData(0,originalBounds,original,0,original.Length);
  Check(engraved.Count(c=>c.A>0)>original.Count(c=>c.A>0),"Extra-bold engraving thickens the actual glyph silhouette");
  Check(engraved.Any(c=>c.A>245 && c.R+c.G+c.B<150) && engraved.Any(c=>c.A>245 && c.R+c.G+c.B>600),"Deboss glyph contains a dark inset wall and a bright opposite inner edge");
  Check(ReferenceEquals(engraving,Call("MedallionLetterTexture","ミ",new Color(239,88,97))),"Deboss textures are cached rather than rebuilt per frame");
  int tutorialNode=tutorial.Nodes.Current(0).Id;Call("OpenNodeInspector",tutorialNode);
  // Even a legacy/injected production line cannot bypass the tutorial entry restriction.
  for(int seed=0;tutorial.Robots.Workshops[tutorialNode].ProductionLines.Length==0;seed++)tutorial.Robots.Workshops[tutorialNode].GenerateLines(seed);
  Call("OpenNodeFactory");Check(!(bool)Get("_nodeFactoryOpen"),"Direct factory entry blocked in tutorial");
  Call("UpdateDomesticSlide",1f);Call("NodeDialogClick",new Point(150,195),new KeyboardState());Check(!(bool)Get("_nodeFactoryOpen"),"Hidden factory tile cannot be clicked");
  Set("_nodeFactoryOpen",true);Call("NodeFactoryClick",new Point(200,300));Check(!(bool)Get("_nodeFactoryOpen"),"Stale modal cannot enter tutorial factory");
  Call("ReturnToTitle");File.Delete(path);Call("CheckAutoSave");Click(960,930);
  Click(960,417);Key(Keys.Escape);Check(!(bool)Get("_tutorialMenuOpen") && (bool)Get("_titleMenuOpen"),"Guide Escape returns to start menu");
  Key(Keys.Escape);Check(!(bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title","Escape dismisses menu instead of exiting app");
  Key(Keys.Enter);Check((bool)Get("_titleMenuOpen"),"Enter opens menu");Click(100,500);Check(!(bool)Get("_titleMenuOpen"),"Outside click dismisses menu");
  Click(960,930);Click(960,350);Check((bool)Get("_titleMenuOpen"),"Blank paper has no hidden close button");Key(Keys.Escape);
  Click(960,930);Click(960,617);Check(Get("_screen").ToString()=="PlayerCount","New Game opens existing player-count flow");
  var w=(WorldSetup)Get("_setup");w.SetWorld(123);Check(w.TutorialStep==0,"Normal world clears tutorial restrictions");w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);w.Industry.Queue(1);Call("BeginIndustry");Check(Get("_screen").ToString()=="Industry" && w.Industry.Pending,"Normal play retains industrial roulette");w.Industry.Reset();Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("AutoSaveDomestic");Call("ReturnToTitle");
  Click(960,930);Check((int)Get("_titleMenuSelection")==1,"Continue is initial selection with save");Key(Keys.Down);Check((int)Get("_titleMenuSelection")==2,"Arrow key selects New Game");Key(Keys.Up);Check((int)Get("_titleMenuSelection")==1,"Arrow key returns to Continue");Input(new(960,617));Input(new(960,617),keys:new[]{Keys.Up});Input(new(960,617));Check((int)Get("_titleMenuSelection")==1,"Stationary mouse cannot override keyboard selection");Key(Keys.Enter);Check(Get("_screen").ToString()=="WorldStatus","Keyboard Continue restores game");Call("ReturnToTitle");
  var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();
  Click(960,930);Click(960,517);Check(Get("_screen").ToString()=="WorldStatus","Continue hit testing works at 960x540");Call("ReturnToTitle");
  graphics.PreferredBackBufferWidth=1920;graphics.PreferredBackBufferHeight=1080;graphics.ApplyChanges();
  var savedBytes=File.ReadAllBytes(path);File.WriteAllText(path,"broken");Click(960,930);Click(960,517);Check((bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title","Resume failure retains usable paper menu");Click(960,617);Check(Get("_screen").ToString()=="PlayerCount" && File.ReadAllText(path)=="broken","New Game remains available without deleting unreadable save");File.WriteAllBytes(path,savedBytes);Set("_autoSaveError","");Set("_screen",Enum.Parse(Get("_screen").GetType(),"Title"));Call("OpenTitleStartMenu");
  Console.WriteLine("PASS: Start modal, tutorial guide, four-soldier preset, saved factory restrictions, skipped tutorial industry, extra-bold deboss medallion names and war reservation counts, save-dependent Continue, New Game, keyboard navigation, text-only paper, Escape/outside, demo pause and small-window hit testing.");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==5){Call("BeginTutorialStepOne");Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);}
  if(frame==6){var w=(WorldSetup)Get("_setup");var pair=w.Nodes.All.Where(n=>n.Owner==0).SelectMany(n=>w.Routes.Neighbors(n).Select(r=>(Source:n.Id,Target:w.Nodes.At(r.Target,r.Entry).Id))).First(p=>w.Nodes.All[p.Target].Owner!=0);w.Campaign.Plan(new(0,pair.Target,1,pair.Source,false));}
  if(frame==7){var w=(WorldSetup)Get("_setup");int n=w.Nodes.Current(0).Id;Call("OpenNodeInspector",n);Call("UpdateDomesticSlide",1f);}

  if(frame==1)Set("_hasAutoSave",false);
  if(frame==2){Set("_hasAutoSave",true);var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
  if(frame==3)Set("_titleMenuOpen",false);
  if(frame==4){Set("_titleMenuOpen",true);Set("_tutorialMenuOpen",true);}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  var canvas=(Rectangle)Call("CanvasBounds");int sx=canvas.X+780*canvas.Width/1920,sy=canvas.Y+940*canvas.Height/1080;var buttonPixel=pixels[sy*pp.BackBufferWidth+sx];
  if(frame<4)Check(frame==3?buttonPixel.B>buttonPixel.R:buttonPixel.R>buttonPixel.B,"Start button is drawn only while the paper menu is closed");
  using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"start-menu-{frame}.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame++==7){Call("DeleteCompletedAutoSave");Exit();}
 }
}
