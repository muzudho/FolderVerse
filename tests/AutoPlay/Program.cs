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
// Newspaper timing and its transition need loaded graphics and a valid world; see BattleLessonsUi.
