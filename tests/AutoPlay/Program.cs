using FolderVerse;
var world=new WorldSetup();world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);
var home=world.Nodes.All.First(n=>world.Routes.Neighbors(n).Length>0);
home.Owner=0;world.ConquerorLocations[0]=home.Cell;world.ConquerorPoints[0]=home.Center;
for(int i=0;i<3;i++)world.Robots.Nodes[home.Id].Add(world.Robots.Create(0,RobotParts.Complete));
int turn=world.Population.Turn;
world.Campaign.Advance(world,-1,0,automaticAll:true);
if(world.Population.Turn!=turn+1)throw new Exception("AUTO failed to commit one global turn");
if(!world.Campaign.RobotBattles.LastMarches.Any(m=>m.Owner==0))throw new Exception("AUTO did not plan player actions");
world.Campaign.RobotBattles.Check(world);
Console.WriteLine("PASS: AUTO plans human-player actions and commits one global turn");
using(var game=new Game1()){
 const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 var type=typeof(Game1);
 var tick=type.GetMethod("UpdateAutomaticSummary",flags)!;
 tick.Invoke(game,new object[]{5d,true});
 if((int)type.GetField("_summaryReadPapers",flags)!.GetValue(game)!=0)throw new Exception("Paper advanced before reading time");
 tick.Invoke(game,new object[]{1d,true});
 tick.Invoke(game,new object[]{6d,true});
 if((int)type.GetField("_summaryReadPapers",flags)!.GetValue(game)!=2 || (int)type.GetField("_newspaperEdition",flags)!.GetValue(game)!=2)throw new Exception("DEMO did not show three different publishers");
 tick.Invoke(game,new object[]{6d,true});
 if((int)type.GetField("_summaryReadPapers",flags)!.GetValue(game)!=3)throw new Exception("DEMO did not finish reading");
 type.GetField("_summaryReadPapers",flags)!.SetValue(game,0);
 type.GetField("_summaryReadAge",flags)!.SetValue(game,0d);
 tick.Invoke(game,new object[]{6d,false});
 if((int)type.GetField("_summaryReadPapers",flags)!.GetValue(game)!=1)throw new Exception("AUTO must read one publisher");
 Console.WriteLine("PASS: summary reading clock advances through three DEMO publishers");
}
