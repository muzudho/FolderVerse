using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new CheckGame();game.Run();
sealed class CheckGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;int node,other,frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 bool Checked(int n)=>(bool)Call("NodeChecked",n);
 void Hover(Point p){Set("_pointer",p);}
 void Click(Point point){
  var canvas=(Rectangle)Call("CanvasBounds");var p=new Point(canvas.X+point.X*canvas.Width/1920,canvas.Y+point.Y*canvas.Height/1080);
  foreach(var b in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(p.X,p.Y,0,b,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);
 }
 void Open(int n){Call("OpenNodeInspector",n);Call("UpdateDomesticSlide",1f);}
 Vector2 Position(int n){var args=new object[]{n,Vector2.Zero};Check((bool)Call("OperationPosition",args),"Marked node projects into view");return (Vector2)args[1];}
 void FitNet(){
  Set("_statusGlobe",false);var net=(CubeNet)Get("_net");var panel=(Rectangle)Prop("NetPanel");net.SetCenter(w,w.Cells[w.Nodes.All[node].Cell].Face);net.FitNodesAtSource(w,w.Nodes.All[node],panel,4);net.Drag(new Vector2(1430,400)-net.NodePosition(w,w.Nodes.All[node],panel));
 }
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.PrepareCampaignDuel(123,456,789,12,0,1);
  node=w.Nodes.Current(0).Id;other=w.Routes.Neighbors(w.Nodes.All[node]).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).First(n=>n!=node);
  Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Open(node);FitNet();
  string Literal(string name)=>(string)typeof(Game1).GetField(name,BindingFlags.Static|BindingFlags.NonPublic)!.GetRawConstantValue()!;
  Check(Literal("NodeCheckButtonHint")=="地図ビューにチェックマークを付けることができます。ターン終了時に消えます。好きなようにお使いください。" && Literal("NodeCheckMarkerHint")=="チェックマークはあなたが付けました。","Both requested tooltip messages match exactly");
  Check(!Checked(node),"New turn begins unchecked");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Click(((Rectangle)Prop("NodeCheckButton")).Center);Check(Checked(node) && !(bool)Get("_nodeInspectorClosing") && (int)Get("_populationNode")==node,"Check toggles on without closing or changing the inspector");}
  if(frame==2){Click(((Rectangle)Prop("NodeCheckButton")).Center);Check(!Checked(node),"Second click toggles off");Click(((Rectangle)Prop("NodeCheckButton")).Center);Open(other);Click(((Rectangle)Prop("NodeCheckButton")).Center);Check(Checked(node) && Checked(other),"Marks are independent and survive changing inspected nodes");}
  if(frame==3){Call("CloseNodeInspector");Call("UpdateDomesticSlide",1f);Check(Checked(node),"Closing inspector preserves manual mark");Open(node);FitNet();Hover(Position(node).ToPoint());}
  if(frame==4){
   w.HumanPlayers.Add(1);w.SelectTurnRuler(1);Check(!Checked(node),"Another human does not see the first human's notes");Open(node);Click(((Rectangle)Prop("NodeCheckButton")).Center);Check(Checked(node),"Another human has an independent mark on the same node");
   w.SelectTurnRuler(0);w.HumanPlayers.Remove(1);Check(Checked(node) && Checked(other),"Original human's marks are retained");
   w.Relations.SetSuperior(1,0);w.SelectControlledRuler(1);Check(Checked(node),"Controlled rulers share the same human's notes");w.SelectControlledRuler(0);w.Relations.SetSuperior(1,-1);Open(node);FitNet();
  }
  if(frame==5){Set("_statusGlobe",true);Call("FocusGlobePosition",w.Routes.Position(w.Nodes.All[node].Cell,w.Nodes.All[node].Center));var at=Position(node);var panel=(Rectangle)Prop("StatusGlobeArea");((WorldPreview)Get("_world")).GlobePan+=new Vector2((1430-at.X)/panel.Width,(400-at.Y)/panel.Height);Hover(Position(node).ToPoint());}
  if(frame==6){var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();Click(((Rectangle)Prop("NodeCheckButton")).Center);Check(!Checked(node),"Half-size physical click toggles off");Click(((Rectangle)Prop("NodeCheckButton")).Center);Check(Checked(node) && !(bool)Get("_nodeInspectorClosing"),"Half-size check remains non-closing");}
  if(frame==7){w.Nodes.All[other].Owner=0;Set("_statusGlobe",false);Call("OpenDirectMap");Call("OpenDirectPair",node,other);Call("UpdateDirectPairTransition",.35f);}
  if(frame==8){var hits=(List<(Rectangle Bounds,int Node)>)Get("_nodeCheckHits");Check(hits.Any(h=>h.Node==node) && hits.Any(h=>h.Node==other),"Both endpoint checks are visible in the direct-delivery map");Hover(hits.First(h=>h.Node==node).Bounds.Center);}
  if(frame==9){Call("DirectPairInput",new MouseState(),false,true);Call("UpdateDirectPairTransition",.35f);Call("CloseDirectDelivery");Check(Checked(node),"Returning from direct delivery preserves notes");Call("EndPlannedTurn");Check(!((Dictionary<int,HashSet<int>>)Get("_nodeChecksByPlayer")).ContainsKey(0),"Submitting the human turn removes its marks immediately");Call("BeginPlayerCount");Check(((Dictionary<int,HashSet<int>>)Get("_nodeChecksByPlayer")).Count==0,"New game clears transient annotations");}
  base.Draw(t);
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"node-checks-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame is 1 or 3 or 5){var hits=(List<(Rectangle Bounds,int Node)>)Get("_nodeCheckHits");Check(hits.Any(h=>h.Node==node),"Check is drawn on the selected node in net/globe view");}
  if(frame++==9){Console.WriteLine("PASS: non-closing independent toggles, exact hints, inspector persistence, human isolation, faction sharing, net/globe/direct maps, 1080p/540p input and turn/new-game expiry.");Exit();}
 }
}
