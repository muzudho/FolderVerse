using System;
using System.Linq;
using FolderVerse;
using Microsoft.Xna.Framework;
var world=new WorldSetup();world.SetWorld(10);world.SetCast(1);world.SetPlacement(1);world.SelectPlayer(0);
int checkedPaths=0,maxSteps=0;
for(int center=0;center<6;center++)for(int rotation=0;rotation<4;rotation++)for(int a=0;a<6;a++)for(int b=a+1;b<6;b++)
{
 if(Vector3.Dot(CubeNet.Normals[a],CubeNet.Normals[b])!=0)continue;
 var net=new CubeNet();net.SetCenter(world,center);for(int r=0;r<rotation;r++)net.Turn(world,1);
 var path=net.JoinPath(a,b);maxSteps=Math.Max(maxSteps,path.Length);
 foreach(var step in path){if(!net.AnimateAttachment(step.Face,step.Side))throw new Exception("Invalid hinge");net.UpdateAnimation(1);}
 if(!net.FacesJoined(a,b))throw new Exception("Unjoined result");checkedPaths++;
}
Console.WriteLine($"PASS: {checkedPaths} adjacent-face routes across all centers and rotations, maximum {maxSteps} hinge moves");using(var game=new Game1())
{
 const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
 object Field(string name)=>typeof(Game1).GetField(name,flags)!.GetValue(game)!;
 void Call(string name,params object[] args)=>typeof(Game1).GetMethod(name,flags)!.Invoke(game,args);
 var setup=(WorldSetup)Field("_setup");setup.SetWorld(10);setup.SetCast(1);setup.SetPlacement(1);setup.SelectPlayer(0);
 var nodes=Enumerable.Range(0,6).Select(face=>setup.Nodes.All.First(n=>setup.Cells[n.Cell].Face==face).Id).ToArray();
 for(int a=0;a<6;a++)for(int b=a+1;b<6;b++)if(Vector3.Dot(CubeNet.Normals[a],CubeNet.Normals[b])==0)setup.Campaign.RobotBattles.LastMarches.Add((a,nodes[a],nodes[b]));
 Call("BeginPropagation");var net=(CubeNet)Field("_propagationNet");
 var travel=(System.Collections.Generic.Dictionary<(int Source,int Target),float>)Field("_propagationTravel");
 int ticks=0;while(travel.Values.Any(t=>t<1) && ticks++<1000)
 {
  var before=travel.ToDictionary(p=>p.Key,p=>p.Value);Call("UpdatePropagationMovement",1.0/18);
  foreach(var p in travel)if(p.Value>before[p.Key] && !net.FacesJoined(setup.Cells[setup.Nodes.All[p.Key.Source].Cell].Face,setup.Cells[setup.Nodes.All[p.Key.Target].Cell].Face))throw new Exception("Pin crossed disconnected faces");
 }
 if(travel.Values.Any(t=>t<1))throw new Exception("Propagation failed to finish");
 Console.WriteLine($"PASS: all {travel.Count} routes finish at 18 FPS in {ticks} frames; no movement across disconnected faces");
}