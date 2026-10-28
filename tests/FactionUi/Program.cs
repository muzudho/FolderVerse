using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new FactionGame();game.Run();
sealed class FactionGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
 object Prop(string name)=>typeof(Game1).GetProperty(name,F)!.GetValue(this)!;
 void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
 object Call(string name,params object[] args)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,args)!;
 void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 WorldSetup world=null!;int frame;Vector2 fixedCenter;int target;
 void Click(Point point){Set("_pointer",point);Call("FactionPickerInput",true,false);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");
  world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.HumanPlayers.AddRange(new[]{0,1});world.SelectTurnRuler(0);
  world.Relations.SetSuperior(2,0);world.Relations.SetSuperior(3,2);world.Relations.SetSuperior(1,0);world.Relations.SetSuperior(4,1);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);
  Call("RestoreDomesticCamera");fixedCenter=(Vector2)Prop("PlayerMedallionCenter");
  var tree=((int Ruler,int Depth)[])Call("FactionTree");Check(tree.Single(e=>e.Ruler==3).Depth==2,"Nested subordinate keeps tree depth");
  var route=world.Routes.NodeChoices(0).First();target=world.Nodes.At(route.Target,route.Entry).Id;
  Set("_movementOpen",true);Set("_warPlanning",false);Set("_selectedMoveNode",target);
  Check((bool)Call("SelectOperationRuler",3) && world.PlayerSlot==3 && world.TurnPlayer==0,"Select subordinate without changing turn owner");
  Check((Vector2)Prop("PlayerMedallionCenter")==fixedCenter && (Vector2)Prop("OperationMedallionCenter")!=fixedCenter,"Player medal stays fixed while operation medal appears");
  Check(world.Campaign.PlannedOrders.Any(o=>o.Ruler==0 && o.TargetNode==target),"Switching preserves pending movement as a reservation");
  Check(!(bool)Call("SelectOperationRuler",1) && world.PlayerSlot==3,"Cannot operate another human vassal");
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==1){Click(((Rectangle)Prop("FactionPickerButton")).Center);Check((bool)Get("_factionPickerOpen"),"Arrow opens hierarchy picker");}
  if(frame==2){Click(((Rectangle)Call("FactionPickerRow",0)).Center);Check(world.PlayerSlot==0 && !(bool)Get("_factionPickerOpen"),"Root selection closes tree");Check((Vector2)Prop("OperationMedallionCenter")==fixedCenter,"Self selection uses one medal");}
  if(frame==3){
   Check(((System.Collections.IDictionary)Get("_medallionRings")).Count==2,"Two nation rings are cached instead of recreated every frame");
   Click(((Rectangle)Prop("FactionPickerButton")).Center);
   var tree=((int Ruler,int Depth)[])Call("FactionTree");int index=Array.FindIndex(tree,e=>e.Ruler==1);
   Check(index>=0 && index<8,"Human vassal visible in hierarchy");Click(((Rectangle)Call("FactionPickerRow",index)).Center);Check(world.PlayerSlot==0,"Other human's row stays disabled");Call("FactionPickerInput",false,true);
  }
  if(frame==4){
   for(int r=5;r<world.ActiveCount;r++)world.Relations.SetSuperior(r,0);
   Click(((Rectangle)Prop("FactionPickerButton")).Center);var panel=(Rectangle)Prop("FactionPickerPanel");
   Check(!panel.Intersects(new Rectangle((int)fixedCenter.X-148,(int)fixedCenter.Y-148,296,296)),"Hierarchy menu never covers the fixed player medal");
   Click(new Point(panel.Right-62,panel.Bottom-36));Check((int)Get("_factionPickerPage")==1,"Hierarchy pages support larger factions");
  }
  if(frame==5){var tree=((int Ruler,int Depth)[])Call("FactionTree");Click(((Rectangle)Call("FactionPickerRow",0)).Center);Check(world.PlayerSlot==tree[8].Ruler && world.TurnPlayer==0,"Paged hierarchy row selects the correct subordinate");}
  base.Draw(time);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"faction-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  if(++frame==6){Console.WriteLine("PASS: fixed player medal, nested operation medal, hierarchy picker, draft preservation, one-medal self selection and human control boundaries.");Exit();}
 }
}
