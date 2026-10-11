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
protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.SetWorld(1);world.SetCast(2);world.SetPlacement(3);world.SelectPlayer(0);node=world.Nodes.Current(0);((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);Set("_populationCell",node.Cell);Set("_populationNode",node.Id);Set("_screen",Enum.Parse(typeof(Game1).GetNestedType("Screen",F)!,"WorldStatus"));Call("OpenNodeFactory");Call("NodeFactoryClick",new Point(100,370));Call("NodeFactoryClick",new Point(200,570));Call("NodeFactoryClick",new Point(260,660));Call("NodeFactoryClick",new Point(780,770));var factory=world.Robots.Workshops[node.Id];if(factory.SelectedPlan!=0 || factory.RestEvery!=3 || factory.RestTurns!=4 || factory.ReservedSlots!=12)throw new Exception("Factory selection failed");}
protected override void Update(GameTime t){}
protected override void Draw(GameTime t){base.Draw(t);if(frame++==1){Call("OpenNodeAssembly");var store=world.Robots.Nodes[node.Id];if(store.Count>0)store.Remove(store.Robots[0].Id);var foreign=world.Robots.Create(1,RobotParts.Complete);store.Add(foreign);Call("OpenNodeAssembly");if((bool)Call("CanMoveHangarRobot",foreign))throw new Exception("Foreign robot enabled");world.Relations.SetSuperior(1,0);if(!(bool)Call("CanMoveHangarRobot",foreign))throw new Exception("Subordinate robot disabled");world.Relations.SetSuperior(1,-1);Set("_pointer",new Point(160,320));}
if(frame==4){Console.WriteLine("PASS: factory plan/rest/capacity controls, foreign/subordinate robot permissions and screen rendering");Exit();}}
}

