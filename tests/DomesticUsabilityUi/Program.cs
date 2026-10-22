using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new UsabilityGame();game.Run();
sealed class UsabilityGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup world=null!;CubeNet net=null!;int frame,node;Robot own=null!,foreign=null!;long transferred;
 Color[] ownHover=null!,foreignBaseline=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 Rectangle Rect(string n,params object[] a)=>(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,a)!;
 void Screen(string n)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),n));
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Pointer(Point p){Set("_pointer",p);}
 void Click(Point p){Pointer(p);Call("PocketInput",true,false);}
 void Persuasion(bool manual){
  Screen("WorldStatus");Set("_homePersuasion",new List<DomesticPersuasion>{new(0,node,foreign,foreign,false)});Set("_homePersuasionActive",false);Set("_homePersuasionAge",0d);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);
  Check((bool)Call("UpdateHomePersuasion",.1d,false,10d) && (bool)Get("_nodeAssemblyOpen"),"Persuasion opens the hangar first");Set("_domesticSlide",1f);
  Call("UpdateHomePersuasion",manual?5d:8d,manual,20d);
  Check(!(bool)Get("_homePersuasionActive") && (int)Get("_populationCell")==-1 && !(bool)Get("_nodeAssemblyOpen") && (float)Get("_domesticSlide")==0 && Get("_screen").ToString()=="WorldStatus","Manual/automatic persuasion completion returns to map immediately");
 }
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.PrepareTutorialStepTwo(123,456,789);world.SelectPlayer(0);world.HumanPlayers.Add(0);node=world.Nodes.Current(0).Id;
  foreach(var r in world.Robots.Nodes[node].Robots.ToArray())world.Robots.Nodes[node].Remove(r.Id);
  own=world.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,BodyOrigin=1,LegsOrigin=1};foreign=world.Robots.Create(1,RobotParts.Complete) with {HeadOrigin=0,BodyOrigin=0,LegsOrigin=0};world.Robots.Nodes[node].Add(own);world.Robots.Nodes[node].Add(foreign);
  world.Robots.Retinues[0].Add(world.Robots.Create(0,RobotParts.Head|RobotParts.Body));Set("_conquestOverviewEnabled",false);Set("_tutorialAttackGuide",false);Set("_tutorialIntroShown",true);Persuasion(true);Persuasion(false);
  net=(CubeNet)Get("_net");net.Home(world);((WorldPreview)Get("_world")).ShowSetup(world,true);net.ShowBatteries=false;net.ShowOtherCountries=false;
  Call("OpenPocket");Check((bool)Call("PocketRobotSelectable",own,true) && !(bool)Call("PocketRobotSelectable",foreign,true),"Selection uses owner rather than manufacture colors");
  Check(!(bool)Call("PocketTransferEnabled",true) && !(bool)Call("PocketTransferEnabled",false),"Transfer is disabled until selection");
  Click(Rect("PocketCard",1,true).Center);Check(Get("_pocketRobot")==null,"Foreign card cannot be selected even with own-country paint");
  Click(Rect("PocketCard",0,true).Center);Check((long)Get("_pocketRobot")==own.Id && (bool)Call("PocketTransferEnabled",true) && !(bool)Call("PocketTransferEnabled",false),"Only the selected source direction enables transfer");
  world.Robots.PendingEdits.Add(new(node,new[]{own.Id},null));Check(!(bool)Call("PocketRobotSelectable",own,true) && !(bool)Call("PocketTransferEnabled",true),"Reserved robot is not highlighted as movable");world.Robots.PendingEdits.Clear();
  transferred=own.Id;Click(Rect("PocketTransferButton",true).Center);Check(world.Robots.Retinues[0].Robots.Any(r=>r.Id==transferred && r.Owner==0 && r.Origin(RobotParts.Head)==1),"Pocket transfer preserves owner, identity and original paint");
  Call("OpenPocket");
  Console.WriteLine("PASS: persuasion closes both ways, owner/origin mismatch, foreign refusal, selection/directional enabling, reserved robot refusal and identity-preserving transfer.");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==0)Pointer(Rect("PocketCard",0,false).Center);
  if(frame==1){Click(Rect("PocketCard",1,false).Center);Pointer(Rect("PocketTransferButton",false).Center);}
  if(frame==2)Pointer(Rect("PocketCard",0,true).Center);
  if(frame==3){Click(Rect("PocketTransferButton",false).Center);Check(world.Robots.Nodes[node].Robots.Any(r=>r.Id==transferred),"Deployment from the pocket returns to current foreign node");Pointer(Rect("PocketCard",1,true).Center);}
  if(frame==4 || frame==5){Call("PocketInput",false,true);Screen("WorldStatus");Call("ResetInspector");Set("_domesticSlide",0f);Set("_statusGlobe",frame==4);if(frame==4)Call("FocusStatusGlobe");else{net.Home(world);net.FitNodesAtSource(world,world.Nodes.All[node],(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1f);}Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);net.ShowBatteries=false;net.ShowOtherCountries=false;}
  if(frame==6){Set("_statusGlobe",true);Call("FocusStatusGlobe");Set("_yawAngle",(float)Get("_yawAngle")+MathHelper.Pi);}
  base.Draw(t);
  if(frame>=4){var args=new object[]{Vector2.Zero};Check((bool)Call("WandererLocationVisible",args)==(frame<6),"Wanderer marker respects globe visibility and works with battery/other countries hidden");world.Relations.Powered[0]=false;Check(!(bool)Call("WandererLocationVisible",new object[]{Vector2.Zero}),"No marker for battery removed conqueror");world.Relations.Powered[0]=true;}
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"usability-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  Color[] CardPixels(Rectangle box)=>Enumerable.Range(box.Top,box.Height).SelectMany(y=>Enumerable.Range(box.Left,box.Width).Select(x=>pixels[y*p.BackBufferWidth+x])).ToArray();
  if(frame==0)ownHover=CardPixels(Rect("PocketCard",0,false));
  if(frame==1){Check(!ownHover.SequenceEqual(CardPixels(Rect("PocketCard",0,false))),"Own card changes visibly under hover");foreignBaseline=CardPixels(Rect("PocketCard",0,true));}
  if(frame==2)Check(foreignBaseline.SequenceEqual(CardPixels(Rect("PocketCard",0,true))),"Foreign card does not falsely highlight as clickable");
  if(frame++==6){Console.WriteLine("PASS: pixel-checked own/foreign hover, source/action states, distinct ownership, globe/net wanderer markers and offscreen hint rendered.");Exit();}
 }
}
