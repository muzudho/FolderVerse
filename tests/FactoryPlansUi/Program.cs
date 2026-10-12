using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
using var game=new Checks();game.Run();
class Checks:Game1{
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
int frame;WorldSetup world=null!;Node node=null!;
protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var simulation=new RobotWorld();simulation.Initialize(1,1);var workshop=simulation.Workshops[0];var counts=new HashSet<int>();for(int seed=0;seed<100;seed++){workshop.GenerateLines(seed);counts.Add(workshop.ProductionLines.Length);}if(counts.Count!=4)throw new Exception("Missing line counts");for(int seed=0;workshop.ProductionLines.Length!=3;seed++)workshop.GenerateLines(seed);foreach(var line in workshop.ProductionLines)line.Configure(RobotParts.Head,1);workshop.SetReservedSlots(10);typeof(RobotWorkshop).GetMethod("CompleteProduction",F)!.Invoke(workshop,new object[]{simulation,0,0});if(simulation.Nodes[0].Count!=2 || !workshop.ProductionLines[2].Waiting)throw new Exception("Shared capacity failed");world=(WorldSetup)Get("_setup");world.SetWorld(1);world.SetCast(2);world.SetPlacement(3);world.SelectPlayer(0);node=world.Nodes.Current(0);((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);Set("_populationCell",node.Cell);Set("_populationNode",node.Id);Set("_screen",Enum.Parse(typeof(Game1).GetNestedType("Screen",F)!,"WorldStatus"));((ScreenshotCapture)Get("_screenshots")).Request();var factory=world.Robots.Workshops[node.Id];for(int seed=0;factory.ProductionLines.Length!=3;seed++)factory.GenerateLines(seed);Call("OpenNodeFactory");Call("NodeFactoryClick",new Point(182+((factory.Manufacturing?factory.ProductionPeriod:1)-1)*72,348));Call("NodeFactoryClick",new Point(110,550));Call("NodeFactoryClick",new Point(580,378));Call("NodeFactoryClick",new Point(580,162));Call("NodeFactoryClick",new Point(760,378));Call("NodeFactoryClick",new Point(760,192));Call("NodeFactoryClick",new Point(450,828));Call("NodeFactoryClick",new Point(450,791));if(factory.SelectedPlan!=0 || factory.RestEvery!=3 || factory.RestTurns!=4 || factory.ReservedSlots!=12)throw new Exception("Factory selection failed");}
protected override void Update(GameTime t){}
protected override void Draw(GameTime t){base.Draw(t);if(frame++==1){var saved=((ScreenshotCapture)Get("_screenshots")).LastSavedPath;if(!File.Exists(saved) || Path.GetFileName(Path.GetDirectoryName(saved))!=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).ToString("yyyy-MM"))throw new Exception("Screenshot month folder failed");Call("OpenNodeAssembly");var store=world.Robots.Nodes[node.Id];if(store.Count>0)store.Remove(store.Robots[0].Id);var foreign=world.Robots.Create(1,RobotParts.Complete);store.Add(foreign);Call("OpenNodeAssembly");if((bool)Call("CanMoveHangarRobot",foreign))throw new Exception("Foreign robot enabled");world.Relations.SetSuperior(1,0);if(!(bool)Call("CanMoveHangarRobot",foreign))throw new Exception("Subordinate robot disabled");world.Relations.SetSuperior(1,-1);Set("_pointer",new Point(160,320));}
if(frame==4){Console.WriteLine("PASS: factory plan/rest/capacity controls, foreign/subordinate robot permissions and screen rendering");Exit();}}
}



