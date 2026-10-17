using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new InputGame();game.Run();
sealed class InputGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 void Input(Point p,ButtonState button=ButtonState.Released,double dt=0,params Keys[] keys){
  var canvas=(Rectangle)Call("CanvasBounds");
  var mouse=new MouseState(canvas.X+p.X*canvas.Width/1920,canvas.Y+p.Y*canvas.Height/1080,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
  Call("HandleInput",mouse,new KeyboardState(keys),new GameTime(TimeSpan.Zero,TimeSpan.FromSeconds(dt)),true);
 }
 void Click(Point p){Input(p);Input(p,ButtonState.Pressed);Input(p);}
 void Open(int source){Call("ResetInspector");Set("_domesticSlide",0f);Click(((Rectangle)Prop("TransportOverviewButton")).Center);Input(new Point(0,0),dt:.4);Set("_populationNode",source);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  var setup=(WorldSetup)Get("_setup");setup.SetWorld(123);setup.SetCast(456);setup.SetPlacement(789);setup.SelectPlayer(0);
  var world=(WorldPreview)Get("_world");world.ShowSetup(setup,true);var net=(CubeNet)Get("_net");net.Home(setup);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_statusGlobe",false);
  int source=setup.Nodes.Current(0).Id;Open(source);Call("FocusCurrentNode");
  Click(new(200,280));Click(new(540,770));
  Check((bool)Get("_transportDestinationPicking"),"Real input enters destination mode");
  Click(new(740,810));Check(!(bool)Get("_transportDestinationPicking") && setup.Robots.Transport.Instructions(source,0).Count==0,"Cancel exits picker and removes unfinished new order");
  Click(new(540,770));Input(default,keys:new[]{Keys.Escape});Input(default);
  Check(!(bool)Get("_transportDestinationPicking") && setup.Robots.Transport.Instructions(source,0).Count==0,"Escape removes new draft");
  Click(new(540,770));var rule=setup.Robots.Transport.Instructions(source,0).Single();
  var adjacent=(int[])Call("TransportAdjacent");int target=adjacent.First(id=>((Rectangle)Prop("MapViewBounds")).Contains(net.NodePosition(setup,setup.Nodes.All[id],(Rectangle)Prop("NetPanel")).ToPoint()));
  Click(net.NodePosition(setup,setup.Nodes.All[target],(Rectangle)Prop("NetPanel")).ToPoint());
  Check(!(bool)Get("_transportDestinationPicking") && rule.Target==target,"Full input dispatch selects adjacent destination");
  Click(new(550,365));Input(default,keys:new[]{Keys.D7});Input(default,keys:new[]{Keys.Enter});Input(default);
  Check(rule.Limit==7 && (int)Get("_transportEditor")==0,"Full input dispatch edits quantity");
  foreach(var field in new[]{new Point(250,365),new Point(550,325),new Point(550,365),new Point(160,325)}){
   Open(source);Click(field);Check((bool)Get("_transportDestinationPicking") || (int)Get("_transportEditor")!=0 || (int)Get("_rateTarget")>=0,"Editor opens");
   Click(((Rectangle)Prop("InspectorClose")).Center);Input(default,dt:.5);
   Check(!(bool)Prop("InspectorVisible") && !(bool)Get("_transportDestinationPicking") && (int)Get("_transportEditor")==0 && (int)Get("_rateTarget")==-1,"Shared X closes from every transport modal");
   Check(setup.Robots.Transport.Instructions(source,0).Count==1,"Closing existing editor preserves order");
  }
  Open(source);Click(new(540,770));Click(((Rectangle)Prop("InspectorClose")).Center);Input(default,dt:.5);
  Check(setup.Robots.Transport.Instructions(source,0).Count==1,"Closing picker discards only pending draft");
  // Actual player handoff restores independent settings and both projection cameras.
  Call("ResetInspector");Set("_domesticSlide",0f);Set("_statusGlobe",false);
  net.ShowBatteries=net.ShowEquator=net.ShowRobots=net.ShowOtherCountries=net.ShowMapSymbols=net.ShowRoutes=false;net.ShowCountryColors=true;
  net.ScreenAngle=.37f;net.Drag(new(77,-43));net.ZoomAt((Rectangle)Prop("NetPanel"),new(1200,500),120);var first=net.CaptureView();
  world.GlobeZoom=3;world.GlobeAnchor=new(1,2,3);world.ManualOrientation=Quaternion.CreateFromYawPitchRoll(.4f,.2f,.1f);var orientation=world.ManualOrientation;
  Call("SaveDomesticCamera");setup.SelectPlayer(1);Call("RestoreDomesticCamera");
  Check(net.ShowBatteries && net.ShowEquator && net.ShowRobots && net.ShowOtherCountries && net.ShowMapSymbols && net.ShowRoutes && !net.ShowCountryColors && (bool)Get("_statusGlobe"),"First visit uses independent defaults");
  world.GlobeZoom=2;Call("SaveDomesticCamera");setup.SelectPlayer(0);Call("RestoreDomesticCamera");
  var restored=net.CaptureView();Check(restored.Pan==first.Pan && restored.Zoom==first.Zoom && restored.ScreenAngle==first.ScreenAngle && restored.Faces.SequenceEqual(first.Faces),"First player's net layout, zoom, angle and pan restored");
  Check(!net.ShowBatteries && !net.ShowEquator && !net.ShowRobots && !net.ShowOtherCountries && !net.ShowMapSymbols && !net.ShowRoutes && net.ShowCountryColors && world.CountryColors && world.ControlledCountryColors,"All display layers restored and globe synchronized");
  Check(!(bool)Get("_statusGlobe") && world.GlobeZoom==3 && world.GlobeAnchor==new Vector3(1,2,3) && world.ManualOrientation==orientation,"First player's globe camera restored");
  Call("OpenTransportOverview");Check(net.ShowRoutes,"Overview temporarily enables nodes");Call("SaveDomesticCamera");setup.SelectPlayer(1);Call("RestoreDomesticCamera");Check(world.GlobeZoom==2 && net.ShowBatteries,"Second player keeps own view");setup.SelectPlayer(0);Call("RestoreDomesticCamera");Check(!net.ShowRoutes,"Temporary node visibility is not saved as preference");
  // Exercise the actual end-turn handoff, rather than calling restore alone.
  setup.HumanPlayers.AddRange(new[]{0,1});setup.SelectTurnRuler(0);net.ShowBatteries=false;
  Call("EndPlannedTurn");Check(setup.PlayerSlot==1 && Get("_screen").ToString()=="WorldStatus" && net.ShowBatteries && world.GlobeZoom==2,"Turn submission hands off to the other player's stored view");
  setup.Campaign.Reset();setup.HumanPlayers.Clear();setup.Relations.SetSuperior(1,0);setup.Relations.Leader=0;setup.SelectPlayer(0);Call("RestoreDomesticCamera");Call("OpenStrategyInspector");Input(default,dt:.5);
  Click(new(650,118));Check(setup.PlayerSlot==1 && net.ShowBatteries,"Manual subordinate switch restores separate preferences");
  Click(new(200,118));Check(setup.PlayerSlot==0 && !net.ShowBatteries && world.GlobeZoom==3,"Returning from subordinate preserves original settings");
  var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();
  Open(source);Click(new(250,365));Click(new(740,810));Check(!(bool)Get("_transportDestinationPicking"),"Scaled-window cancel hit testing");Click(new(550,325));Click(((Rectangle)Prop("InspectorClose")).Center);Input(default,dt:.5);Check(!(bool)Prop("InspectorVisible"),"Scaled-window modal close hit testing");
  Call("BeginPlayerCount");setup.SelectPlayer(0);Call("RestoreDomesticCamera");Check(net.ShowBatteries && world.GlobeZoom==1,"New game clears old player preferences");
  Console.WriteLine("PASS: real input destination confirm/cancel/Escape, quantity keyboard, all modal close buttons, draft rollback, independent player cameras/layers, actual turn handoff and subordinate switching, temporary-layer isolation, small-window input and new-game reset.");Exit();
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){}
}
