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
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_conquestOverviewEnabled",false);
  var setup=(WorldSetup)Get("_setup");setup.SetWorld(123);setup.SetCast(456);setup.SetPlacement(789);setup.SelectPlayer(0);
  var world=(WorldPreview)Get("_world");world.ShowSetup(setup,true);var net=(CubeNet)Get("_net");net.Home(setup);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_statusGlobe",false);
  if(Environment.GetCommandLineArgs().Contains("--route-dropdown"))
  {
   setup.EnsureGuides().LastTurns[setup.PlayerSlot]=setup.Population.Turn;
   Set("_paperReadTurn",setup.Population.Turn);Set("_paperReadCountry",setup.PlayerSlot);
   const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
   Rectangle Option(int index)=>(Rectangle)typeof(Game1).GetMethod("TrafficLayerOption",Static)!.Invoke(null,new object[]{index})!;
   var toolbar=(Rectangle)typeof(Game1).GetMethod("RouteLayerButton",Static)!.Invoke(null,new object[]{3})!;
   Rectangle DisplayArea(string field)=>(Rectangle)typeof(Game1).GetField(field,Static)!.GetValue(null)!;
   var breaker=DisplayArea("SimpleDisplayButton");
   var filter=DisplayArea("DisplayFilterButton");
   foreach(bool globe in new[]{false,true}){
    Set("_statusGlobe",globe);net.ShowRoutes=false;
    bool otherCountries=net.ShowOtherCountries;
    Click(breaker.Center);
    Check((bool)Get("_simpleDisplayOpen"),"Breaker icon opens the breaker menu");
    Click(filter.Center);
    Check((bool)Get("_displayFilterOpen") && !(bool)Get("_simpleDisplayOpen") &&
        net.ShowOtherCountries==otherCountries,"Filter icon switches menus without toggling the filter");
    Click(DisplayArea("DisplayOtherCountriesRow").Center);
    Check(net.ShowOtherCountries!=otherCountries && (bool)Get("_displayFilterOpen") &&
        world.ControlledCountryColors==!net.ShowOtherCountries,"Other Countries toggles in the open menu and updates the globe");
    Click(DisplayArea("DisplayOtherCountriesRow").Center);
    Click(filter.Center);
    Check(net.ShowOtherCountries==otherCountries && !(bool)Get("_displayFilterOpen"),"Filter icon closes its menu");
    var toy=(Rectangle)Prop("OrientationToyArea");
    var handle=new Rectangle(toy.X+16,toy.Y+4,toy.Width-32,toy.Height-30);
    var overlap=Rectangle.Intersect(handle,Option(1));
    Check(!overlap.IsEmpty,"Node choice overlaps orientation drag handle");
    Click(toolbar.Center);
    Check((bool)Get("_trafficLayerListOpen"),$"Selection toolbar opens the dropdown (screen {Get("_screen")}, pointer {Get("_pointer")})");
    Input(overlap.Center,ButtonState.Pressed);
    Check(net.ShowRoutes && !(bool)Get("_orientationDragging") && !(bool)Get("_trafficLayerListOpen"),
        "Overlapping node choice selects nodes without starting an orientation drag");
    Input(overlap.Center);
    var axisOverlap=(from axis in Enumerable.Range(0,3) from sign in new[]{-1,1}
        let button=(Rectangle)Call("AxisButton",toy,axis,sign)
        select Rectangle.Intersect(button,Option(0))).FirstOrDefault(r=>!r.IsEmpty);
    Click(toolbar.Center);Click(axisOverlap.IsEmpty?Option(0).Center:axisOverlap.Center);
    Check(!net.ShowRoutes && !(bool)Prop("OrientationRotating"),"Region choice selects regions without rotating");
    Click(toolbar.Center);
    Input(toy.Center,ButtonState.Pressed);
    Check(!(bool)Get("_trafficLayerListOpen") && !(bool)Get("_orientationDragging"),
        "Click outside dismisses the menu without passing through to the toy");
    Input(toy.Center);
    Input(toy.Center,ButtonState.Pressed);
    Check((bool)Get("_orientationDragging"),"The toy is draggable again after dismissing the menu");
    Input(toy.Center);
    Click(toolbar.Center);Input(default,keys:new[]{Keys.Escape});Input(default);
    Check(!(bool)Get("_trafficLayerListOpen") && Get("_screen").ToString()=="WorldStatus",
        "Escape dismisses the menu without exiting the status screen");
   }
   var toggle=(Rectangle)typeof(Game1).GetField("MapViewButton",Static)!.GetValue(null)!;
   var batch=(SpriteBatch)Get("_spriteBatch");
   using var image=new RenderTarget2D(GraphicsDevice,1920,1080);
   GraphicsDevice.SetRenderTarget(image);
   batch.Begin();
   Call("DrawDisplayControlButton",breaker,0,false);
   Call("DrawDisplayControlButton",filter,1,true);
   batch.End();
   GraphicsDevice.SetRenderTarget(null);
   var displayIcons=(Rectangle[])Get("_displayControlIconSources");
   Check(displayIcons[0].Height>displayIcons[0].Width && displayIcons[1].Width>displayIcons[1].Height,
       "The rendered toolbar uses a tall breaker and a wide binocular silhouette");
   void CheckHint(Point hover,bool lower,bool toolbarHint)
   {
    Set("_pointer",hover);
    GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Transparent);
    batch.Begin();
    if(toolbarHint)Call("DrawDisplayControlMenus");
    else Call("DrawRulebookHint","他国","地図に表示する国を切り替えます。");
    Call("DrawPopupHints");
    batch.End();
    GraphicsDevice.SetRenderTarget(null);
    var sample=new Color[1];
    byte BorderAlpha(int y)
    {
     image.GetData(0,new Rectangle(510,y,1,1),sample,0,1);
     return sample[0].A;
    }
    Check((BorderAlpha(14)>0)==!lower && (BorderAlpha(922)>0)==lower,
        "The popup uses the edge opposite the hovered control");
   }
   CheckHint(breaker.Center,true,true);
   CheckHint(filter.Center,true,true);
   CheckHint(new Point(960,900),false,false);
   Set("_pointer",Point.Zero);
   Color[] ButtonPixels()
   {
    GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Transparent);
    batch.Begin();Call("DrawMapViewButton");batch.End();
    GraphicsDevice.SetRenderTarget(null);
    var pixels=new Color[toggle.Width*toggle.Height];
    image.GetData(0,toggle,pixels,0,pixels.Length);
    return pixels;
   }
   long Brightness(Color[] pixels,int half)
   {
    int center=toggle.Width*(1+half*2)/4;
    return Enumerable.Range(4,toggle.Height-8).Sum(y=>Enumerable.Range(center-30,60)
        .Sum(x=>{var color=pixels[y*toggle.Width+x];return (long)(color.R+color.G+color.B);}));
   }
   var globeSelected=ButtonPixels();
   Click(new Point(toggle.Left+30,toggle.Center.Y));Input(default,dt:4);
   Check(!(bool)Get("_statusGlobe"),"Map toggle still unfolds the globe");
   var netSelected=ButtonPixels();
   Check(Brightness(globeSelected,0)>Brightness(netSelected,0) &&
        Brightness(netSelected,1)>Brightness(globeSelected,1),"Both icons switch between enabled and highlighted colors in place");
   Click(new Point(toggle.Right-30,toggle.Center.Y));Input(default,dt:4);
   Check((bool)Get("_statusGlobe"),"The same toolbar slot switches back to the globe");
   Check(globeSelected.SequenceEqual(ButtonPixels()),"Switching back restores the original pair of colors");
   Console.WriteLine("PASS: breaker and filter icon rendering and menus, Other Countries, dropdown hit priority on globe and net, and both map-view toggle states");
   Exit();
   return;
  }
  int source=setup.Nodes.Current(0).Id;Open(source);Call("FocusCurrentNode");
  Click(new(540,770));
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
  int beforeDraft=setup.Robots.Transport.Instructions(source,0).Single().Degrees;Open(source);Click(new(540,770));Click(((Rectangle)Prop("InspectorClose")).Center);Input(default,dt:.5);
  Check(setup.Robots.Transport.Instructions(source,0).Single().Degrees==beforeDraft,"Canceling new draft restores existing rates and rest");
  Check(setup.Robots.Transport.Instructions(source,0).Count==1,"Closing picker discards only pending draft");
  // Replay the reported flow: Add, then immediately edit the visible row controls.
  Open(source);Click(new(604,765));Check((bool)Get("_transportDestinationPicking"),"Add starts destination picking");
  Click(new(590,442));Check((int)Get("_transportEditor")==1 && !(bool)Get("_transportDestinationPicking"),"New row parts remain clickable during destination picking");
  Input(default,keys:new[]{Keys.Escape});Input(default);
  Set("_transportEditNumber",2);Click(new(250,471));Click(new(550,471));Check((int)Get("_transportEditor")==2,"Quantity can replace destination picker");Input(default,keys:new[]{Keys.Escape});Input(default);
  Click(new(250,471));Click(new(160,431));Check((int)Get("_rateTarget")==2,"Adoption pie can replace destination picker");Input(default,keys:new[]{Keys.Escape});Input(default);
  Click(new(250,471));Click(new(269,435));Check(!(bool)Get("_transportDestinationPicking"),"Purpose control works during destination picking");
  Click(new(250,471));Click(new(839,438));Check(setup.Robots.Transport.Instructions(source,0).Count==1,"Delete control works during destination picking");
  // Exact coordinates from the report on a first, newly added row.
  foreach(var existing in setup.Robots.Transport.Instructions(source,0).ToArray())setup.Robots.Transport.DeleteInstruction(source,0,existing.Number);
  Click(new(604,765));Click(new(590,336));Check((int)Get("_transportEditor")==1,"Reported first-row click opens parts without choosing destination first");
  Click(((Rectangle)Prop("InspectorClose")).Center);Input(default,dt:.5);Check(setup.Robots.Transport.Instructions(source,0).Count==1,"Field edits retain the new instruction when inspector closes");
  // Other-country visibility is overridden only for actual war-planning targets.
  Call("ResetInspector");Set("_domesticSlide",0f);net.ShowOtherCountries=false;
  foreach(var owned in setup.Nodes.All.Where(n=>n.Owner==0)){
   var store=setup.Robots.Nodes[owned.Id];if(store.Count>=store.StorageLimit)store.Remove(store.Robots[0].Id);store.Add(setup.Robots.Create(0,RobotParts.Complete));
  }
  Call("OpenWarPlanning");Call("PrepareDestinationMap");
  int enemyTarget=net.MovementTargets.First(id=>setup.Nodes.All[id].Owner>0 && !setup.Relations.Controls(setup.Nodes.All[id].Owner));
  var enemyNode=setup.Nodes.All[enemyTarget];
  Check(!net.ShowOtherCountries && net.NodeVisible(setup,enemyNode),"Attackable foreign node visible with Other Countries off");
  foreach(var hidden in setup.Nodes.All.Where(n=>n.Owner>0 && !setup.Relations.Controls(n.Owner) && !net.MovementTargets.Contains(n.Id)))Check(!net.NodeVisible(setup,hidden),"Non-target foreign nodes remain hidden");
  net.SetCenter(setup,setup.Cells[enemyNode.Cell].Face);net.FitNodesAtSource(setup,enemyNode,(Rectangle)Prop("NetPanel"),1);
  var enemyAt=net.NodePosition(setup,enemyNode,(Rectangle)Prop("NetPanel")).ToPoint();
  Check(net.HitNode(setup,(Rectangle)Prop("NetPanel"),enemyAt,new[]{enemyTarget})==enemyTarget,"Net hit test includes revealed attack target");
  Call("MovementClick",enemyAt,new KeyboardState());Check((int)Get("_selectedMoveNode")==enemyTarget,"Revealed foreign target can be selected for invasion");
  Set("_statusGlobe",true);
  // Find a view orientation that exposes this target, then verify the globe hit path too.
  bool globeHit=false;foreach(float yaw in new[]{0f,MathHelper.PiOver2,MathHelper.Pi,-MathHelper.PiOver2})foreach(float pitch in new[]{0f,MathHelper.PiOver2,-MathHelper.PiOver2}){
   world.ManualOrientation=null;Set("_yawAngle",yaw);Set("_pitchAngle",pitch);world.GlobeAnchor=null;world.GlobeZoom=1;
   var cell=setup.Cells[enemyNode.Cell];if(world.ProjectVisible(setup.Routes.Position(enemyNode.Cell,enemyNode.Center),cell.Normal,yaw,pitch,(Rectangle)Prop("MapViewBounds"),out var at) && (int)Call("HitGlobeNode",at.ToPoint(),new[]{enemyTarget})==enemyTarget)globeHit=true;
  }
  Check(globeHit,"Globe hit test includes revealed attack target");
  Call("CloseMovement");Check(!net.NodeVisible(setup,enemyNode) && !net.ShowOtherCountries,"Closing invasion restores foreign node hiding without changing preference");Call("OpenMovement");Check(!net.NodeVisible(setup,enemyNode),"Normal movement does not reveal foreign attack targets");Call("ResetInspector");
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
  Click(((Rectangle)Prop("FactionPickerButton")).Center);var childRow=(Rectangle)Call("FactionPickerRow",1);Click(new(childRow.X+20,childRow.Center.Y));Check(setup.PlayerSlot==1 && net.ShowBatteries,"Manual subordinate switch restores separate preferences");
  Click(((Rectangle)Prop("FactionPickerButton")).Center);var parentRow=(Rectangle)Call("FactionPickerRow",0);Click(new(parentRow.X+20,parentRow.Center.Y));Check(setup.PlayerSlot==0 && !net.ShowBatteries && world.GlobeZoom==3,"Returning from subordinate preserves original settings");
  var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();
  Open(source);Click(new(250,365));Click(new(740,810));Check(!(bool)Get("_transportDestinationPicking"),"Scaled-window cancel hit testing");Click(new(550,325));Click(((Rectangle)Prop("InspectorClose")).Center);Input(default,dt:.5);Check(!(bool)Prop("InspectorVisible"),"Scaled-window modal close hit testing");
  Call("BeginPlayerCount");setup.SelectPlayer(0);Call("RestoreDomesticCamera");Check(net.ShowBatteries && world.GlobeZoom==1,"New game clears old player preferences");
  Console.WriteLine("PASS: foreign war targets visible/selectable in net and globe with Other Countries off, reset after planning, reported Add-to-field clicks, picker-to-parts/quantity/pie/purpose/delete transitions, real input destination confirm/cancel/Escape, quantity keyboard, all modal close buttons, draft rollback, independent player cameras/layers, actual turn handoff and subordinate switching, temporary-layer isolation, small-window input and new-game reset.");Exit();
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){}
}
