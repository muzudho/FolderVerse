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
 void Screen(string name)=>Check(Get("_screen").ToString()==name,"Expected "+name+", got "+Get("_screen"));
 void Input(bool enter=false,bool back=false,double elapsed=0)=>Call("OpeningInput",false,enter,back,elapsed,new KeyboardState());
 void Skip(){Set("_pointer",new Point(1675,55));Call("OpeningInput",true,false,false,0d,new KeyboardState());}
 void Click(Point point){var c=(Rectangle)Call("CanvasBounds");int x=c.X+(int)Math.Round(point.X*c.Width/1920d),y=c.Y+(int)Math.Round(point.Y*c.Height/1080d);Set("_previousMouse",new MouseState());Set("_previousKeyboard",new KeyboardState());foreach(var button in new[]{ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(x,y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);}
 WorldSetup Setup=>(WorldSetup)Get("_setup");
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Call("BeginOpening",false,null!);Screen("OpeningBook");}
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==1){Input(enter:true);Screen("PlayerCount");}
  if(frame==2){Set("_requestedPlayers",0);Input(enter:true);Screen("ComputerPlayerCount");Set("_requestedComputers",20);Input(enter:true);Screen("OpeningSpace");Check(Setup.ParticipantCount==20,"Participant settings lost");}
  if(frame==3){Input(enter:true);Screen("OpeningLottery");seed=Setup.WorldSeed;}
  if(frame==4){Input(enter:true);Screen("OpeningReview");for(int i=0;i<50;i++)Input(elapsed:1);Check(Setup.WorldSeed==seed,"Review rerolls the selected world");}
  if(frame==5){Input(back:true);Screen("OpeningLottery");Check(Setup.WorldSeed==seed,"Back loses selected world");Set("_openingAge",5.99d);Input(elapsed:.2);Check((double)Get("_openingAge")<1,"Passing planets do not advance");Check(((WorldPreview)Get("_departingOpeningPlanet")).Seed==seed && Setup.WorldSeed!=seed,"Outgoing planet must keep its original appearance");Input(enter:true);Screen("OpeningLottery");}
  if(frame==6){Set("_openingAge",2d);int selected=Setup.WorldSeed;Input(enter:true);Screen("OpeningReview");Check(Setup.WorldSeed==selected && Get("_departingOpeningPlanet")==null,"Selection must keep the incoming world and clear the outgoing preview");Input(enter:true);Screen("CastRolling");Check(!Setup.AllowDuplicateCharacters,"New game enables duplicate characters");Check(Setup.EquipmentMode==RobotEquipmentMode.Materials,"New game defaults to nine materials");Click(new Point(1650,99));Check(Setup.EquipmentMode==RobotEquipmentMode.Materials,"Cast rolling no longer changes equipment mode");}
  if(frame==6){Click(new Point(1650,45));Check(Setup.AllowDuplicateCharacters,"Toggle does not enable duplicate characters");Click(new Point(1650,45));Check(!Setup.AllowDuplicateCharacters && Setup.Looks.Select(l=>l.BaseId).Distinct().Count()==Setup.Looks.Length,"Turning off does not restore unique characters");seed=Setup.WorldSeed;Click(new Point(150,1040));Screen("OpeningReview");Check(Setup.WorldSeed==seed,"Cast back loses the chosen planet");}
  if(frame==7){Call("BeginOpening",false,null!);Skip();Screen("PlayerCount");Input(enter:true);Input(enter:true);Screen("OpeningLottery");Call("BeginOpening",false,(Action)(()=>callback=true));Input(enter:true);Screen("OpeningSpace");Input(enter:true);Check(callback,"Fixed campaign callback lost");Call("BeginOpening",true,null!);Skip();Screen("WorldStatus");Check(Setup.TutorialStep==1,"Tutorial fixed-world start lost");Call("BeginOpening",false,null!);Input(back:true);Screen("Title");}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
  if(frame<7){var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"opening-{frame}.png"));image.SaveAsPng(file,image.Width,image.Height);}
  if(frame++==7){Console.WriteLine("PASS: storybook, mandatory counts, space, moving planet selection/confirmation/back, narration skip, fixed tutorial/campaign, default unique characters and duplicate toggle.");Exit();}
 }
}
