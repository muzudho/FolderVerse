using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

var faces=Enumerable.Range(0,6).Select(i=>
{
    var pose=Matrix.CreateFromQuaternion(WorldPreview.OpeningFaceTour(i/5d));
    return CubeNet.Normals.Single(normal=>Vector3.TransformNormal(normal,pose).Z>.999f);
}).ToArray();
if(faces.Distinct().Count()!=6)throw new Exception("Opening tour must present all six faces exactly once.");
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var fixture=new WorldSetup();fixture.SetParticipantCounts(0,60);fixture.SetWorld(1);
for(int seed=0;seed<100;seed++){fixture.SetCast(seed);Check(fixture.Looks.Select(l=>l.BaseId).Distinct().Count()==60,"Default cast repeats a character");}
fixture.AllowDuplicateCharacters=true;int repeated=0;
for(int seed=0;seed<100;seed++){fixture.SetCast(seed);if(fixture.Looks.Select(l=>l.BaseId).Distinct().Count()<60)repeated++;Check(fixture.Looks.Distinct().Count()==60,"Exact portraits repeat");}
Check(repeated>0,"Opt-in never permits families");
var saved=AutoSaveStore.Capture(fixture);
Check(AutoSaveStore.Restore<WorldSetup>(saved).AllowDuplicateCharacters,"Duplicate option lost on restore");
saved.Objects.Single(e=>e.Fields?.ContainsKey("AllowDuplicateCharacters")==true).Fields.Remove("AllowDuplicateCharacters");
var legacy=AutoSaveStore.Restore<WorldSetup>(saved);
Check(!legacy.AllowDuplicateCharacters && legacy.Looks.SequenceEqual(fixture.Looks),"Legacy save must default off without changing portraits");
using var game=new OpeningGame();game.Run();
sealed class OpeningGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame,seed;bool callback;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 double OpeningTime(string name)=>(double)typeof(Game1).GetField(name,F|BindingFlags.Static)!.GetRawConstantValue()!;
 void CheckOpeningSeats()
 {
  var canvas=(Rectangle)Call("CanvasBounds");
  var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
  var batch=(SpriteBatch)Get("_spriteBatch");
  var pp=GraphicsDevice.PresentationParameters;
  var previous=new Color[pp.BackBufferWidth*pp.BackBufferHeight];
  var pixels=new Color[previous.Length];
  for(int seats=0;seats<=5;seats++)
  {
   Set("_requestedPlayers",seats);
   GraphicsDevice.Clear(Color.Black);
   batch.Begin(transformMatrix:transform);
   Call("OpeningReadingTable");
   batch.End();
   GraphicsDevice.GetBackBufferData(pixels);
   if(seats>0)Check(previous.SequenceEqual(pixels)==(seats==5),"Seats must increase through four and then stop");
   (previous,pixels)=(pixels,previous);
  }
  Set("_requestedPlayers",1);
 }
 void Screen(string name)=>Check(Get("_screen").ToString()==name,"Expected "+name+", got "+Get("_screen"));
 void Input(bool enter=false,bool back=false,double elapsed=0)=>Call("OpeningInput",false,enter,back,elapsed,new KeyboardState());
 void FinishReveal(){for(int i=0;i<8;i++)Input(elapsed:.25);}
 void Skip(){Set("_pointer",new Point(1675,55));Call("OpeningInput",true,false,false,0d,new KeyboardState());}
 void Click(Point point){var c=(Rectangle)Call("CanvasBounds");int x=c.X+(int)Math.Round(point.X*c.Width/1920d),y=c.Y+(int)Math.Round(point.Y*c.Height/1080d);Set("_previousMouse",new MouseState());Set("_previousKeyboard",new KeyboardState());foreach(var button in new[]{ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(x,y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);}
 void CheckInspection()
 {
  int originalSeed=Setup.WorldSeed;
  var originalPose=(Quaternion)Get("_openingReviewPose");
  var c=(Rectangle)Call("CanvasBounds");
  Drag(new Point(920,485),ButtonState.Pressed);
  Drag(new Point(1090,545),ButtonState.Pressed);
  Drag(new Point(1090,545),ButtonState.Released);
  var rotated=(Quaternion)Get("_openingReviewPose");
  Check(Math.Abs(Quaternion.Dot(originalPose,rotated))<.99f,"Right drag must rotate the inspected planet");
  Capture("opening-inspection-globe");
  Click(new Point(1650,720));
  Check((float)Get("_mapTransitionAge")>=0,"Unfold must start the hinge animation");
  Input(enter:true,elapsed:1.1);
  Screen("OpeningReview");
  Capture("opening-inspection-unfolding");
  Input(elapsed:3);
  Check((bool)Get("_openingInspectionUnfolded") &&
    ((CubeNet)Get("_openingInspectionNet")).Faces.Select(f=>f.Face).Distinct().Count()==6,
    "Unfold must expose the six faces before land placement");
  Capture("opening-inspection-net");
  Click(new Point(1650,720));
  Input(elapsed:1.7);
  Capture("opening-inspection-folding");
  Input(elapsed:2.1);
  Check(!(bool)Get("_openingInspectionUnfolded") && rotated==(Quaternion)Get("_openingReviewPose") &&
    Setup.WorldSeed==originalSeed,"Assembly must keep the inspected star and viewing angle");
  void Drag(Point point,ButtonState right)
  {
   int x=c.X+point.X*c.Width/1920,y=c.Y+point.Y*c.Height/1080;
   Call("HandleInput",new MouseState(x,y,0,ButtonState.Released,ButtonState.Released,right,ButtonState.Released,ButtonState.Released),
       new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);
  }
 }
 void CheckDomesticMapTransition()
 {
  Set("_statusGlobe",true);
  Call("StartMapTransition");
  Check((float)Get("_mapTransitionAge")>=0,"Domestic unfold must start the hinge animation");
  Call("UpdateMapTransition",1.1f);
  Capture("domestic-unfolding");
  Call("UpdateMapTransition",3f);
  Check(!(bool)Get("_statusGlobe"),"Domestic unfold must switch to the net");
  Call("StartMapTransition");
  Call("UpdateMapTransition",1.1f);
  Capture("domestic-folding");
  Call("UpdateMapTransition",3f);
  Check((bool)Get("_statusGlobe"),"Domestic assembly must switch back to the globe");
 }
 WorldSetup Setup=>(WorldSetup)Get("_setup");
 void CheckWaterVillage()
 {
  var node=Setup.Nodes.All.First(n=>n.Terrain==TravelTerrain.Sea);
  var cell=Setup.Cells[node.Cell];
  var position=Setup.Routes.Position(node.Cell,node.Center);
  Vector2 Project(Vector3 point)
  {
   var delta=point-position;
   float x=Vector3.Dot(delta,cell.U),y=Vector3.Dot(delta,cell.V),z=Vector3.Dot(delta,cell.Normal);
   return new Vector2(180,138)+new Vector2(x+.5f*y,.48f*y-.25f*x-z)*105;
  }
  var draw=typeof(CubeNet).GetMethod("DrawSurfacePuck",F)!;
  var net=(CubeNet)Get("_net");var batch=(SpriteBatch)Get("_spriteBatch");
  var before=GraphicsDevice.GetRenderTargets();
  using var target=new RenderTarget2D(GraphicsDevice,360,240);
  var (units,host)=(node.FunctionUnits,node.FunctionHost);
  Color[] Render()
  {
   GraphicsDevice.SetRenderTarget(target);
   GraphicsDevice.Clear(new Color(33,107,130));
   batch.Begin();
   draw.Invoke(net,new object[]{Get("_ui"),Setup,node,cell,position,.78f,0f,
       (Func<Vector3,Vector2>)Project,(Func<Color,Color>)(c=>c)});
   batch.End();
   GraphicsDevice.SetRenderTargets(before);
   var pixels=new Color[360*240];target.GetData(pixels);return pixels;
  }
  try
  {
   node.FunctionUnits=0;node.FunctionHost=-1;
   var ordinary=Render();
   node.FunctionHost=Setup.Nodes.All.First(n=>n.Id!=node.Id && n.Functional).Id;
   var village=Render();
   Check(ordinary.Zip(village).Count(pair=>pair.First!=pair.Second)>1000,
      "A relocated sea site renders a floating settlement");
   using var image=new Texture2D(GraphicsDevice,360,240);
   image.SetData(village);
   using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"water-village.png"));
   image.SaveAsPng(output,360,240);
  }
  finally{(node.FunctionUnits,node.FunctionHost)=(units,host);}
 }
 void CheckRequestAnotherStory()
 {
  Set("_openingAge",OpeningTime("OpeningGlobeReady"));
  int declined=Setup.WorldSeed;
  var queue=(Queue<WorldSetup>)Get("_openingOrbitCandidates");
  int next=queue.ElementAt(1).WorldSeed;
  int round=(int)Get("_openingOrbitRound");
  Click(new Point(530,990));
  Screen("OpeningLottery");
  Check(Setup.WorldSeed==declined,"Declining must start the exit without immediately replacing the visible planet");
  Input(enter:true,elapsed:.25);
  Screen("OpeningLottery");
  int ticks=(int)Math.Ceiling((OpeningTime("OpeningPlanetInterval")-OpeningTime("OpeningGlobeEnd"))/.25);
  for(int i=1;i<ticks;i++)Input(elapsed:.25);
  Check(Setup.WorldSeed==next && (int)Get("_openingOrbitRound")==round+1,
      "Requesting another story advances exactly once after the exit and overview");
 }
 string PaletteFixture=>Path.Combine(AppContext.BaseDirectory,"test-terrain-palette.txt");
 protected override void LoadContent()
 {
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");
  base.LoadContent();
  Set("_terrainPalettePath",PaletteFixture);
  File.WriteAllText(PaletteFixture,"earth");
  Call("LoadTerrainPalette");
  var spin=typeof(Game1).GetMethod("OpeningOrbitSpin",F|BindingFlags.Static)!;
  var camera=typeof(Game1).GetMethod("OpeningOrbitCamera",F|BindingFlags.Static)!;
  var zoomInEnd=OpeningTime("OpeningZoomInEnd");var zoomOutStart=OpeningTime("OpeningZoomOutStart");
  var overview=OpeningTime("OpeningOverviewStart");
  foreach(var age in Enumerable.Range(0,101).Select(i=>i/100f*(float)overview))
  {
   var (pan,zoom)=((float Pan,float Zoom))camera.Invoke(null,new object[]{age})!;
   Check(zoom==0 || pan==1,"The camera must pan off the sun before it zooms in, and zoom out before returning");
  }
  Check(OpeningTime("OpeningGlobeEnd")-OpeningTime("OpeningGlobeReady")==4.5 &&
      overview-zoomOutStart==zoomInEnd,"The face tour holds for 4.5 seconds with symmetric camera travel");
  float Angle(float time,int slot=0)=>(float)spin.Invoke(null,new object[]{time,slot})!;
  Check(Angle(0)==Angle(.6f) && Angle(1.35f)==MathHelper.PiOver2 && Angle(1.35f)==Angle(1.9f),
      "Orbit models must pause between quarter turns");
  Check(Angle(.89f-.19f,1)>1.5f && Angle(.89f-.76f,4)==0 &&
      Math.Abs(Angle(.88f-.30f-.19f,1)-Angle(2.1f-.30f-.76f,4))<.001f,
      "Fast and slow planets use different stops and the same quarter-turn motion");
  var alignment=typeof(Game1).GetMethod("OpeningPolarAlignment",F|BindingFlags.Static)!;
  var axes=new[]{Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ};
  for(int axis=0;axis<axes.Length;axis++)
  {
   var pose=(Matrix)alignment.Invoke(null,new object[]{axis})!*Matrix.CreateRotationY(Angle(1.35f));
   Check(Vector3.Distance(Vector3.TransformNormal(axes[axis],pose),Vector3.Up)<.001f,
      "Every head system must spin around the up axis of the orbit model");
  }
  var longOrbit=typeof(Game1).GetMethod("HasLongOpeningOrbit",F|BindingFlags.Static)!;
  Check(Enumerable.Range(0,60).Where(i=>(bool)longOrbit.Invoke(null,new object[]{i})!).SequenceEqual(new[]{19,39,59}),
      "One long orbit must appear per twenty candidates");
  Call("BeginOpening",false,null!);Screen("OpeningBook");
  var palette=typeof(WorldTerrain).GetProperty("EarthPalette",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!;
  Check(!(bool)palette.GetValue(null)! && File.ReadAllText(PaletteFixture)=="earth",
      "A new game starts with planet colors without writing over the saved preference");
 }
 void CheckPalette()
 {
  int seed=Setup.WorldSeed;
  var colorAt=typeof(WorldTerrain).GetMethod("MapColorAt",F|BindingFlags.Static)!;
  Color Sea(int n)=>(Color)colorAt.Invoke(null,new object[]{.45f,Vector3.UnitZ,1,n})!;
  Check(Sea(0)!=Sea(1),"Planet palette must vary by seed");
  var modelTint=typeof(WorldTerrain).GetMethod("PlanetModelTint",F|BindingFlags.Static)!;
  Color land=(Color)colorAt.Invoke(null,new object[]{.63f,Vector3.UnitZ,1,seed})!;
  Check((Color)modelTint.Invoke(null,new object[]{seed})! == land,
      "The orbit model and the planet's grassland must share a color");
  Capture("opening-palette-planet");
  var planetPixels=GlobePixels();
  Click(new Point(1540,990));
  Check(File.ReadAllText(PaletteFixture)=="earth" && Sea(0)==Sea(1) && Setup.WorldSeed==seed,
     "The opening checkbox must apply one Earth palette without changing the candidate");
  Capture("opening-palette-earth");
  Check(GlobePixels().Zip(planetPixels).Count(pair=>pair.First!=pair.Second)>100,
      "The displayed globe must visibly change, independently of the checkbox");
  Set("_statusMenuOpen",true);
  Check((bool)Call("StatusMenuClick",new Point(1590,544)) && (bool)Get("_globeSettingsOpen"),
     "The system menu must open the globe settings");
  Click(new Point(960,675));
  Check(File.ReadAllText(PaletteFixture)=="planet" && Sea(0)!=Sea(1),
     "The system menu must update the same persistent preference");
  Click(new Point(960,750));
  Check(!(bool)Get("_globeSettingsOpen"),"The globe settings modal must close");
  Set("_statusMenuOpen",false);
  Color[] GlobePixels()
  {
   var p=GraphicsDevice.PresentationParameters;
   var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];
   GraphicsDevice.GetBackBufferData(pixels);
   var canvas=(Rectangle)Call("CanvasBounds");
   int left=canvas.X+canvas.Width*3/8,top=canvas.Y+canvas.Height/3;
   return Enumerable.Range(top,canvas.Height/4)
      .SelectMany(y=>pixels.Skip(y*p.BackBufferWidth+left).Take(canvas.Width/4)).ToArray();
  }
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==0)CheckOpeningSeats();
  if(frame==1){Input(enter:true);Screen("PlayerCount");}
  if(frame==2){Set("_requestedPlayers",0);Input(enter:true);Screen("ComputerPlayerCount");Set("_requestedComputers",20);Input(enter:true);Screen("OpeningSpace");Check(Setup.ParticipantCount==20,"Participant settings lost");}
  if(frame==3){Input(enter:true);Screen("OpeningSpace");Input(elapsed:.25);Input(elapsed:.25);Capture("opening-page");for(int i=0;i<3;i++)Input(elapsed:.25);Capture("opening-page-zoom");FinishReveal();Capture("opening-space");Input(enter:true);Screen("OpeningLottery");seed=Setup.WorldSeed;Check((double)Get("_openingAge")<0,"Galaxy approach must precede the first planet");Input(enter:true);Screen("OpeningLottery");for(int i=0;i<10;i++)Input(elapsed:.25);Capture("opening-galaxy");for(int i=0;i<12;i++){Input(elapsed:.25);if((double)Get("_openingAge")==-2.5)Capture("opening-galaxy-model-blend");}Check((double)Get("_openingAge")<0,"Orbit overview must precede the first planet");Capture("opening-orbits");while((double)Get("_openingAge")<0){Input(elapsed:.25);}Check((double)Get("_openingAge")==0,"The overview must end at the beginning of the regular selection interval");}
  if(frame==4){for(int i=0;i<8;i++){Input(elapsed:.25);if(i==5)Capture("opening-orbits-approach");if(i==6)Capture("opening-globe-handoff");}Check(Setup.WorldSeed==seed,"First planet changed on entry");CheckPalette();Input(enter:true);Screen("OpeningReview");CheckInspection();for(int i=0;i<50;i++)Input(elapsed:1);Check(Setup.WorldSeed==seed,"Review rerolls the selected world");}
  if(frame==5){Input(back:true);Screen("OpeningLottery");Check(Setup.WorldSeed==seed,"Back loses selected world");var queue=(Queue<WorldSetup>)Get("_openingOrbitCandidates");Check(queue.Count==12,"Eight orbit worlds and four future worlds must be queued");int nextSeed=queue.ElementAt(1).WorldSeed;Set("_openingAge",OpeningTime("OpeningOverviewStart")-.25d);Input(elapsed:.25);Capture("opening-orbits-hold");for(int i=0;i<7;i++)Input(elapsed:.25);Check(Setup.WorldSeed==seed,"The current candidate stays during the two-second overview");Input(enter:true);Screen("OpeningLottery");Input(elapsed:.25);Check(Setup.WorldSeed==nextSeed && ((WorldSetup[])Get("_passedOpeningOrbits"))[0].WorldSeed==seed,"Orbit candidates must advance in order and retain the passed world");Check((double)Get("_openingAge")<1,"Passing planets do not advance");Check((int)Get("_openingOrbitRound")==1,"Passing the first planet must replace one orbit");Check(((WorldPreview)Get("_world")).Seed==nextSeed,"The globe must update only when the overview ends");Input(enter:true);Screen("OpeningLottery");}
  if(frame==6){Set("_openingAge",2d);int selected=Setup.WorldSeed;Input(enter:true);Screen("OpeningReview");Check(Setup.WorldSeed==selected,"Selection must keep the focused world");Input(enter:true);Screen("CastRolling");Check(!Setup.AllowDuplicateCharacters,"New game enables duplicate characters");Check(Setup.EquipmentMode==RobotEquipmentMode.Materials,"New game defaults to nine materials");Click(new Point(1650,99));Check(Setup.EquipmentMode==RobotEquipmentMode.Materials,"Cast rolling no longer changes equipment mode");}
  if(frame==6){Click(new Point(1650,45));Check(Setup.AllowDuplicateCharacters,"Toggle does not enable duplicate characters");Click(new Point(1650,45));Check(!Setup.AllowDuplicateCharacters && Setup.Looks.Select(l=>l.BaseId).Distinct().Count()==Setup.Looks.Length,"Turning off does not restore unique characters");seed=Setup.WorldSeed;Click(new Point(150,1040));Screen("OpeningReview");Check(Setup.WorldSeed==seed,"Cast back loses the chosen planet");}
  if(frame==7){Call("BeginOpening",false,null!);Skip();Screen("PlayerCount");Input(enter:true);Input(enter:true);Screen("OpeningLottery");Check((double)Get("_openingAge")==0,"Skip must bypass the galaxy approach");Call("BeginOpening",false,(Action)(()=>callback=true));Input(enter:true);Screen("OpeningSpace");FinishReveal();Input(enter:true);Check(callback,"Fixed campaign callback lost");Call("BeginOpening",true,null!);Skip();Screen("WorldStatus");Check(Setup.TutorialStep==1,"Tutorial fixed-world start lost");CheckDomesticMapTransition();Set("_globeSettingsOpen",true);Capture("globe-settings");Set("_globeSettingsOpen",false);Call("BeginOpening",false,null!);Input(back:true);Screen("Title");}
  if(frame==5)CheckRequestAnotherStory();
  if(frame<7)Capture($"opening-{frame}");
  if(frame++==7){CheckWaterVillage();Console.WriteLine("PASS: storybook, mandatory counts, space, moving planet selection/confirmation/back, narration skip, fixed tutorial/campaign, default unique characters, duplicate toggle and relocated water village.");Exit();}
 }
 void Capture(string name){base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name+".png"));image.SaveAsPng(file,image.Width,image.Height);}
}
