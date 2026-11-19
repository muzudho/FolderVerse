using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new PlanetMenuGame();game.Run();
sealed class PlanetMenuGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 Rectangle Bounds(string n,params object[] a)=>(Rectangle)typeof(Game1).GetMethod(n,S)!.Invoke(null,a)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Input(bool click=false,bool enter=false,bool escape=false,Keys key=Keys.None){Set("_previousKeyboard",new KeyboardState());Set("_previousMouse",MouseAt((Point)Get("_pointer")));Call("TutorialMenuInput",click,enter,escape,key==Keys.None?new KeyboardState():new KeyboardState(key));}
 MouseState MouseAt(Point p){var c=(Rectangle)Call("CanvasBounds");return new(c.X+(int)Math.Round(p.X*c.Width/1920d),c.Y+(int)Math.Round(p.Y*c.Height/1080d),0,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);}
 void Click(Rectangle r){Set("_pointer",r.Center);Input(click:true);}
 void OpenMenu(){Set("_screen",Enum.Parse(Get("_screen").GetType(),"Title"));Call("OpenTitleStartMenu");Set("_pointer",Bounds("TitleMenuItem",0).Center);Call("TitleStartMenuInput",true,false,false,new KeyboardState(),true);}
 void SkipOpening(){Check(Get("_screen").ToString()=="OpeningBook","Tutorial starts with the storybook");Set("_pointer",new Point(1675,55));Call("OpeningInput",true,false,false,0d,new KeyboardState());}
 protected override void LoadContent(){
  Content.RootDirectory=Environment.GetEnvironmentVariable("FOLDERVERSE_TEST_CONTENT") ?? Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_tutorialAchievements",new TutorialAchievements(Path.Combine(AppContext.BaseDirectory,"menu-audit-achievements.json")));Set("_autoSaveEnabled",false);OpenMenu();
  Check((bool)Get("_tutorialMenuOpen") && (int)Get("_tutorialPlanetSelection")==0,"How to Play opens seven-planet menu with first selected");
  var sizes=((int X,int Y,int Z)[])typeof(Game1).GetField("TutorialPlanetSizes",S)!.GetValue(null)!;Check(sizes.SequenceEqual(new[]{(3,1,1),(1,1,1),(1,2,1),(2,1,2),(2,2,2),(3,2,2),(3,3,3)}),"All seven requested dimensions match previews");
  var textures=(RenderTarget2D[])Get("_tutorialPlanetTextures");Check(textures.Length==7,"Seven previews exist");foreach(var texture in textures){var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);Check(pixels.Count(p=>p.A>0)>100,"Every globe has rendered geometry");}
 }
 Rectangle GameBounds(string name)=>(Rectangle)typeof(Game1).GetProperty(name,F)!.GetValue(this)!;
 void GameClick(Point point){
  var release=MouseAt(point);
  var press=new MouseState(release.X,release.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
  foreach(var mouse in new[]{release,press,release})
   Call("HandleInput",mouse,new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.FromSeconds(.5)),true);
 }
 void ClickThroughPrimaplana(WorldSetup setup){
  Set("_tutorialAttackGuide",false);
  var net=(CubeNet)Get("_net");
  for(int turn=0;turn<12 && Get("_screen").ToString()!="Ending";turn++){
   GameClick(((Rectangle)Call("PrimaryActionBounds",2)).Center);
   var targets=net.MovementTargets.Where(id=>setup.Nodes.All[id].Owner>=0 && !setup.Relations.Allied(0,setup.Nodes.All[id].Owner)).ToArray();
   Check(targets.Length>0,"An unconquered town is reachable on turn "+turn);
   GameClick(net.NodePosition(setup,setup.Nodes.All[targets[0]],GameBounds("NetPanel")).ToPoint());
   Check(setup.Campaign.PlannedOrders.Any(order=>order.TargetNode==targets[0] && order.Fighters>0),"Selecting a target reserves armed robots");
   GameClick(GameBounds("InspectorClose").Center);
   Check(!(bool)typeof(Game1).GetProperty("InspectorVisible",F)!.GetValue(this)!, "The war inspector closed");
   GameClick(((Rectangle)Call("PrimaryActionBounds",1)).Center);
   for(int step=0;step<1000 && Get("_screen").ToString() is not ("WorldStatus" or "Ending");step++){
    switch(Get("_screen").ToString()){
     case "Transport":Call("FinishTransport");break;
     case "Battle":Call("RobotBattleInput",10d,false,true);break;
     case "BattleResearch":Call("UpdateBattleResearch",10d,true);break;
     default:throw new Exception("Unexpected Primaplana screen: "+Get("_screen"));
    }
   }
  }
  Check(Get("_screen").ToString()=="Ending" && (int)Get("_endingWinner")==0,"Repeated attacks conquer Primaplana");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   Input(key:Keys.Right);Check((int)Get("_tutorialPlanetSelection")==1,"Keyboard moves planet cursor");Input(key:Keys.Right);Input(enter:true);Check(Get("_screen").ToString()=="Title" && (bool)Get("_tutorialMenuOpen"),"Disabled planet cannot activate with Enter");
   for(int i=2;i<7;i++){Click(Bounds("TutorialPlanetCard",i));Check(Get("_screen").ToString()=="Title","Disabled planet "+i+" cannot launch");Set("_tutorialPlanetSelection",i);Input(enter:true);Check(Get("_screen").ToString()=="Title" && (bool)Get("_tutorialMenuOpen"),"Disabled planet "+i+" cannot activate with Enter");Console.WriteLine("PASS unfinished card mouse and Enter index="+i);}
  }
  if(frame==2){
   Click(Bounds("TutorialPlanetCard",1));SkipOpening();var setup=(WorldSetup)Get("_setup");Check(Get("_screen").ToString()=="WorldStatus" && setup.TutorialStep==1 && setup.TutorialMapBeforeWar && setup.PlayerSlot==0 && !(bool)Get("_tutorialMenuOpen"),"Clicking Petitstella directly starts integrated tutorial");
   Check(setup.Nodes.All.All(n=>setup.Robots.Nodes[n.Id].Count==6),"Direct launch retains beginner reinforcements");
  }
  if(frame==3){
   Call("ReturnToTitle");OpenMenu();Input(enter:true);var setup=(WorldSetup)Get("_setup");
   Check(Get("_screen").ToString()=="WorldStatus" && setup.IsPrimaplana && !(bool)Get("_statusGlobe"),"Enter starts Primaplana on its flat map");
   Check(setup.Cells.Length==3 && setup.Cells.All(cell=>cell.Face==2) && setup.Cells[0].Neighbors.SequenceEqual(new[]{1}) && setup.Cells[2].Neighbors.SequenceEqual(new[]{1}),"Plane has physical edges without wraparound");
   Check(setup.Robots.Retinues[0].Count>=10 && setup.Nodes.All.Where(node=>node.Owner!=0).All(node=>setup.Robots.Nodes[node.Id].Count==0),"Deployment starts with overwhelming strength");
  }
  if(frame==4){Set("_tutorialAttackGuide",false);Set("_tutorialChromeSlide",0f);}
  if(frame==5)ClickThroughPrimaplana((WorldSetup)Get("_setup"));
  if(frame==6){Call("ReturnToTitle");OpenMenu();Input(escape:true);Check(!(bool)Get("_tutorialMenuOpen") && (bool)Get("_titleMenuOpen"),"Escape returns to start menu");OpenMenu();}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"tutorial-planets-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==6){Console.WriteLine("PASS: seven rendered planets/dimensions, cursor navigation, five disabled planets, Petitstella launch, Primaplana topology, robots and world conquest and Escape back to start menu.");Exit();}
 }
}
