using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new FolderChecks();game.Run();
sealed class FolderChecks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame,node;WorldSetup w=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 List<(int Kind,int Id,int Depth,int Owner,string Label)> Rows()=>(List<(int,int,int,int,string)>)Get("_inspectorFolderRows");
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);w.Relations.Reset();Check(w.ActiveCount>=3,"Three-ruler hierarchy fixture");w.Relations.SetSuperior(0,1);w.Relations.SetSuperior(1,2);
  node=w.Nodes.Current(0).Id;Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Call("OpenNodeInspector",node);Call("UpdateDomesticSlide",.4f);
  Call("OpenNodeAssembly");Check(!(bool)Call("NodeAutomationClick",new Point(200,940)) && (int)Get("_supervisorDialog")==0,"Supervisor controls are inert outside Office");
  Call("OpenNodeOffice");var icon=(Rectangle)Prop("InspectorFolderButton");Check((bool)Call("DomesticViewClick",icon.Center) && (bool)Get("_inspectorFoldersOpen"),"Office folder icon opens the tree");
  Check(Rows().Take(3).Select(r=>r.Id).SequenceEqual(new[]{2,1,0}) && Rows().Take(3).All(r=>r.Kind==0),"Boss chain includes all ancestors in top-down order");
  Check(Rows().Any(r=>r.Kind==1) && Rows().Any(r=>r.Kind==2 && r.Id==node),"Region and point supervisor folders are available");
  Check((bool)Call("IsCurrentInspectorFolder",2,node) && !(bool)Call("IsCurrentInspectorFolder",1,w.Nodes.All[node].Cell),"Only the active point folder is highlighted");
  int expandIndex=Rows().FindIndex(r=>r.Kind==1);var expandRow=Rows()[expandIndex];
  bool expanded=((HashSet<int>)Get("_expandedFolderRegions")).Contains(expandRow.Id);
  Set("_pointer",new Point(94+Math.Min(expandRow.Depth,10)*25,318+expandIndex*56));
  Call("InspectorFolderInput",new MouseState(),true,false,true);
  Check(((HashSet<int>)Get("_expandedFolderRegions")).Contains(expandRow.Id)!=expanded && (bool)Get("_inspectorFoldersOpen"),"Region branch expands/collapses without navigating");
  Call("InspectorFolderInput",new MouseState(),true,false,true);
  Set("_pointer",new Point(400,600));
  Call("InspectorFolderInput",new MouseState(400,600,-120,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),false,false,true);
  Check((int)Get("_folderScroll")==Math.Min(3,Math.Max(0,Rows().Count-10)),"Wheel scroll stays within tree bounds");Set("_folderScroll",0);
  Call("OpenInspectorFolderRow",0);Check((bool)Get("_reignView") && (int)Get("_strategyRuler")==2 && w.PlayerSlot==0 && (bool)Prop("StrategyReadOnly"),"Top boss opens the strategy inspector without changing turn or control");
  Call("OpenInspectorFolders");int own=Rows().FindIndex(r=>r.Kind==0 && r.Id==0);Call("OpenInspectorFolderRow",own);Check((bool)Get("_reignView") && !(bool)Prop("StrategyReadOnly") && w.PlayerSlot==0,"Current conqueror opens the same editable strategy inspector");
  Call("OpenInspectorFolders");int region=Rows().FindIndex(r=>r.Kind==1 && r.Id==w.Nodes.All[node].Cell);Check(region>=0,"Context region is in the tree");Call("OpenInspectorFolderRow",region);Check((int)Get("_regionCell")==w.Nodes.All[node].Cell,"Region folder opens regional supervisor");
  Check((bool)Call("IsCurrentInspectorFolder",1,w.Nodes.All[node].Cell) && !(bool)Call("IsCurrentInspectorFolder",2,node),"Region folder is highlighted instead of its representative point");
  Call("OpenInspectorFolders");int point=Rows().FindIndex(r=>r.Kind==2 && r.Id==node);Call("OpenInspectorFolderRow",point);Check((bool)Get("_nodeOfficeOpen") && (int)Get("_populationNode")==node,"Point folder opens point supervisor Office");
  var originalOwners=w.Nodes.All.Select(n=>n.Owner).ToArray();
  w.Relations.Reset();
  var net=(CubeNet)Get("_net");var world=(WorldPreview)Get("_world");
  int fullCell=w.Cells.First(c=>w.Nodes.InCell(c.Id).Any()).Id;
  int partialCell=w.Cells.First(c=>c.Id!=fullCell && w.Nodes.InCell(c.Id).Count()>1).Id;
  foreach(var n in w.Nodes.All)n.Owner=1;
  foreach(var n in w.Nodes.InCell(fullCell))n.Owner=2;
  var outpost=w.Nodes.InCell(partialCell).First();outpost.Owner=2;
  var secondOutpost=w.Nodes.All.First(n=>n.Cell!=fullCell && n.Cell!=partialCell && w.Nodes.InCell(n.Cell).Count()>1);secondOutpost.Owner=2;
  Call("OpenStrategyInspector");Set("_strategyRuler",2);Set("_statusGlobe",false);net.NodeFocus=-1;Call("UpdateNodeFocusCircle");
  Check(net.CountryFocus==2 && world.CountryFocus==2 && w.PlayerSlot==0,"Country lighting follows the inspected ruler without changing turn");
  Check(net.FocusCells.SetEquals(new[]{fullCell}) && world.FocusCells.SetEquals(net.FocusCells),"Only fully conquered cells remain bright");
  Check(net.FocusCircles.Count==2 && world.FocusCircles.Count==2 && net.FocusCircleNodes.Contains(outpost.Id),"Owned outpost outside conquered cells gets a spotlight");
  var color=new Color(200,160,120);var circle=net.FocusCircles[0];
  Check(net.FocusTerrain(color,new Vector2(-10000),fullCell)==color && net.FocusTerrain(color,circle.Center,partialCell)==color,"Country territory and outpost spotlight retain original brightness");
  Check(net.FocusTerrain(color,new Vector2(-10000),partialCell)==new Color(130,104,78),"Other terrain is dimmed by 35 percent");
  Check(net.NodeTint(outpost.Id,color)==color && net.DimNode(w.Nodes.All.First(n=>n.Owner==1 && !net.FocusCircleNodes.Contains(n.Id)).Id),"Country point markers follow the bright territory and circles");
  foreach(var n in w.Nodes.InCell(fullCell))n.Owner=1;
  Call("UpdateNodeFocusCircle");Check(net.FocusCells.Count==0 && net.FocusCircles.Count==2,"Country with only outposts retains their spotlights");
  outpost.Owner=secondOutpost.Owner=1;Call("UpdateNodeFocusCircle");
  Check(net.FocusCells.Count==0 && net.FocusCircles.Count==0 && net.FocusTerrain(color,Vector2.Zero)==new Color(130,104,78),"Landless country with no owned points dims all terrain without stale circles");
  Set("_strategyRuler",0);Call("UpdateNodeFocusCircle");Check(net.CountryFocus==0,"My country uses the active player's ownership");
  Call("OpenNodeInspector",node);Call("UpdateNodeFocusCircle");Check(net.CountryFocus==-1 && net.FocusCells.Count==0,"Switching inspectors clears country lighting");
  foreach(var n in w.Nodes.All)n.Owner=originalOwners[n.Id];
  w.Relations.SetSuperior(0,1);w.Relations.SetSuperior(1,2);
  Set("_statusGlobe",true);Call("OpenNodeOffice");
  Call("OpenInspectorFolders");Set("_pointer",new Point(1100,600));Call("InspectorFolderInput",new MouseState(),false,true,true);Check(!(bool)Get("_inspectorFoldersOpen"),"Escape closes the tree");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==0)Call("OpenInspectorFolders");
  if(frame==1){Call("OpenInspectorFolderRow",0);}
  if(frame==2)Call("OpenInspectorFolders");
  if(frame==3){Call("OpenNodeInspector",node);Call("OpenNodeAssembly");}
  if(frame==4){Call("OpenNodeOffice");Call("OpenInspectorFolders");var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using(var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"folders-{frame}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);}
  if(frame++==4){Console.WriteLine("PASS: Office-only supervisor controls, recursive boss folders, conqueror/region/point navigation, unchanged active ruler, Escape and full/half-size rendering.");Exit();}
 }
}
