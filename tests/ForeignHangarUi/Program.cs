using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new HangarGame();game.Run();
sealed class HangarGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;int node,frame;RobotStore store=null!;long mixedId;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 bool Prop(string n)=>(bool)typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 Rectangle Rect(string n,params object[] a)=>(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,a)!;
 static void Check(bool value,string text){if(!value)throw new Exception(text);}
 void Open(){Call("OpenNodeInspector",node);Set("_domesticSlide",1f);}
 void Drag(int source,int target)
 {
  var start=Rect("HangarPartBounds",source,0).Center;var end=Rect("HangarCell",target).Center;
  Set("_pointer",start);Call("NodeAssemblyLocalInput",new MouseState(start.X,start.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),true,false);
  Set("_previousMouse",new MouseState(start.X,start.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released));Set("_pointer",end);
  Call("NodeAssemblyLocalInput",new MouseState(end.X,end.Y,0,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),false,false);
 }
 protected override void LoadContent()
 {
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.PrepareTutorialStepTwo(123,456,789);w.SelectPlayer(0);w.HumanPlayers.Add(0);node=w.Nodes.Current(0).Id;store=w.Robots.Nodes[node];
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverviewEnabled",false);((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
  foreach(bool cheap in new[]{false,true})foreach(bool damaged in new[]{false,true})
  {
   foreach(var robot in store.Robots.ToArray())store.Remove(robot.Id);
   var own=w.Robots.Create(0,RobotParts.Complete) with {CheapParts=cheap?RobotParts.Head:0,DamagedParts=damaged?RobotParts.Head:0};
   var foreign=w.Robots.Create(1,RobotParts.Body|RobotParts.Legs);store.Add(own);store.Add(foreign);Open();
   Check(Prop("CanOpenHangar") && (bool)Call("CanMoveHangarPart",own,RobotParts.Head),"Own regular/cheap/damaged part is movable at current foreign node");
   Check(!(bool)Call("CanMoveHangarPart",foreign,RobotParts.Body),"Foreign parts cannot be grabbed");
   Check((bool)Call("HangarPlaceEnabled",foreign,RobotParts.Head) && !(bool)Call("HangarPlaceEnabled",foreign,RobotParts.Body),"Enable display agrees with vacant and foreign-occupied parts");
   Check((bool)Call("HangarPlaceEnabled",own,RobotParts.Head),"Own damaged/cheap parts remain visibly enabled");
   Drag(0,1);var result=store.Robots.Single(r=>r.Id==foreign.Id);
   Check(result.Parts==RobotParts.Complete && result.Origin(RobotParts.Head)==0 && result.IsCheap(RobotParts.Head)==cheap && result.IsDamaged(RobotParts.Head)==damaged,"Move into vacant foreign robot part preserves origin, quality and damage");
   // Another country's head is occupied: the whole transaction must be rejected.
   var other=w.Robots.Create(1,RobotParts.Head);store.Add(other);Open();var before=store.Robots.ToArray();Drag(1,2);Check(before.SequenceEqual(store.Robots),"Cannot swap own part for another country's part");
   // Own parts can swap, including damaged cheap parts, without repairing either.
   store.Remove(other.Id);var ownHead=w.Robots.Create(0,RobotParts.Head);store.Add(ownHead);Open();Drag(1,2);
   var received=store.Robots.Single(r=>r.Id==ownHead.Id);Check(received.IsCheap(RobotParts.Head)==cheap && received.IsDamaged(RobotParts.Head)==damaged,"Own-to-own swap keeps cheap and damaged flags");
   Open();Drag(2,3);Check(store.Robots.Any(r=>r.Parts==RobotParts.Head && r.IsCheap(RobotParts.Head)==cheap && r.IsDamaged(RobotParts.Head)==damaged),"Move to empty slot keeps all part properties");
  }
  int elsewhere=w.Nodes.All.First(n=>n.Id!=node && n.Owner>0).Id;Call("OpenNodeInspector",elsewhere);Check(!Prop("CanOpenHangar"),"Foreign nodes without own queen remain read-only");
  Open();int owner=w.Nodes.All[node].Owner;w.Relations.Powered[0]=false;Check(!Prop("CanOpenHangar"),"No manipulation without battery");w.Relations.Powered[0]=true;
  foreach(var robot in store.Robots.ToArray())store.Remove(robot.Id);
  store.Add(w.Robots.Create(1,RobotParts.Complete));
  var mixed=w.Robots.Create(1,RobotParts.Complete) with {HeadOrigin=0,BodyOrigin=1,LegsOrigin=1,CheapParts=RobotParts.Head,DamagedParts=RobotParts.Head};store.Add(mixed);mixedId=mixed.Id;
  store.Add(w.Robots.Create(0,RobotParts.Head));store.Add(w.Robots.Create(1,RobotParts.Body));store.Add(w.Robots.Create(0,RobotParts.Complete));Open();
  Set("_pointer",new Point(1500,950));Set("_animationTime",10f);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t)
 {
  if(frame==1){Set("_hangarDrag",(long?)mixedId);Set("_hangarPart",RobotParts.Head);Set("_pointer",Rect("HangarCell",3).Center);Check(!(bool)Call("HangarPlaceEnabled",store.Robots[0],RobotParts.Head),"Foreign occupied drop remains disabled during drag");Check((bool)Call("HangarPlaceEnabled",store.Robots[3],RobotParts.Head),"Vacant part enabled during drag");Check(!(bool)Call("HangarPlaceEnabled",store.Robots[4],RobotParts.Body),"Nonmatching part disabled during head drag");}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);image.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"foreign-hangar-{frame}.png"));image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==1){Console.WriteLine("PASS: own-part drags, vacant slots, own swaps, blocked foreign swaps, regular/cheap/damaged combinations, no repair exploit, queen/battery restrictions and enabled/disabled source/drop display.");Exit();}
 }
}
