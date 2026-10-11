using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new Checks();game.Run();
class Checks:Game1{
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
int frame;
protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var w=(WorldSetup)Get("_setup");w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);((CubeNet)Get("_net")).Home(w);Set("_screen",Enum.Parse(typeof(Game1).GetNestedType("Screen",BindingFlags.NonPublic)!,"WorldStatus"));
foreach(int mode in new[]{0,1,2}){Call("RouteLayerClick",new Point(1500,100));Call("RouteLayerClick",new Point(1500,154+mode*52));var net=(CubeNet)Get("_net");if(net.ShowRoutes!=(mode==1) || net.ShowCountryColors!=(mode==2))throw new Exception("mode mismatch");}
var n=(CubeNet)Get("_net");var cell=w.Cells.First(c=>w.Nodes.InCell(c.Id).Count()>1);float h=.6f;int owner=w.Nodes.FullOwner(cell.Id);if(n.TerrainColor(w,cell.Id,h)!=WorldPreview.LandTerritoryColor(w.OwnerColor(owner),WorldTerrain.Kind(h)))throw new Exception("exclusive color");var node=w.Nodes.InCell(cell.Id).First();int old=node.Owner;node.Owner=(old+1)%w.ActiveCount;if(n.TerrainColor(w,cell.Id,h)!=WorldTerrain.ColorAt(h,cell.Normal))throw new Exception("partial cell tinted");node.Owner=old;
Set("_pointer",new Point(1200,400));var right=new MouseState(1200,400,0,ButtonState.Released,ButtonState.Released,ButtonState.Pressed,ButtonState.Released,ButtonState.Released);Set("_previousMouse",default(MouseState));Call("Rotate",right,false,true,new Rectangle(1000,200,800,600));if(!(bool)Get("_dragging"))throw new Exception("right drag not started");Set("_previousMouse",right);Set("_pointer",new Point(1220,420));float yaw=(float)Get("_yaw");Call("Rotate",right,false,true,new Rectangle(1000,200,800,600));if((float)Get("_yaw")==yaw)throw new Exception("rotation missing");Call("Rotate",default(MouseState),false,true,new Rectangle(1000,200,800,600));if((bool)Get("_dragging"))throw new Exception("rotation not stopped");Call("RouteLayerClick",new Point(1500,100));}
protected override void Update(GameTime t){}
protected override void Draw(GameTime t){base.Draw(t);if(frame++==1){Console.WriteLine("PASS: three layers, exclusive ownership colors, dropdown rendering and right drag");Exit();}}
}
