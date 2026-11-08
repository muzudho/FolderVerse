using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
for(int seed=0;seed<24;seed++){
 var w=new WorldSetup();w.PrepareCampaignDuel(seed,seed+10,seed+100,seed+1000,seed%60,seed%60);
 Check(w.Width==1 && w.Height==1 && w.Depth==1 && w.Cells.Length==6 && w.ActiveCount==2,"Campaign has exactly two countries on a 1x1x1 world");
 Check(w.PlayerCount==1 && w.ComputerPlayerCount==1 && w.HumanPlayers.SequenceEqual(new[]{0}) && w.ControllerOf(0)==0 && w.ControllerOf(1)==-1,"Human and CPU control are distinct");
 Check(w.Looks[0]==w.Looks[1] && w.Looks[0].BaseId==seed%60,"Both sides can use the identical character");
 foreach(int owner in new[]{0,1}){
  var cells=w.Cells.Where(c=>w.Owners[c.Id]==owner).Select(c=>c.Id).ToHashSet();Check(cells.Count==3 && w.TerritoryCounts[owner]==3 && w.TerritoryRights[owner]==3,"Each nation receives three sectors");
  var seen=new HashSet<int>{cells.First()};var q=new Queue<int>(seen);while(q.TryDequeue(out int id))foreach(int next in w.Cells[id].Neighbors)if(cells.Contains(next) && seen.Add(next))q.Enqueue(next);
  Check(seen.SetEquals(cells),"Both nations have connected territories");
 }
 Check(w.TutorialStep==0 && !w.LandPending && w.Nodes.All.Length==w.Robots.Nodes.Length && w.Nodes.All.All(n=>n.Owner>=0),"Normal terrain, nodes and robots initialize without tutorial mode");
 var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check(restored.Owners.SequenceEqual(w.Owners) && restored.Looks.SequenceEqual(w.Looks) && restored.ComputerPlayerCount==1,"Campaign uses the normal save format");
}
for(int seed=0;seed<24;seed++){
 var w=new WorldSetup();w.PrepareCampaignThreeKingdoms(seed,seed+10,seed+100,seed+1000,0,29,59);
 Check(w.Width==2 && w.Height==2 && w.Depth==2 && w.Cells.Length==24 && w.ActiveCount==3,"Three Kingdoms has three countries on a 2x2x2 world");
 Check(w.PlayerCount==1 && w.ComputerPlayerCount==2 && w.HumanPlayers.SequenceEqual(new[]{0}) && w.ControllerOf(1)==-1 && w.ControllerOf(2)==-1,"One human and two CPUs");
 Check(w.Looks.Select(l=>l.BaseId).SequenceEqual(new[]{0,29,59}),"All three chosen characters are retained");
 foreach(int ruler in Enumerable.Range(0,3)){
  var owned=w.Cells.Where(c=>w.Owners[c.Id]==ruler).ToArray();var ids=owned.Select(c=>c.Id).ToHashSet();
  Check(owned.Length==8 && owned.Select(c=>c.Face).Distinct().Count()==2 && w.TerritoryCounts[ruler]==8 && w.TerritoryRights[ruler]==8,"Each country owns exactly two whole faces and eight sectors");
  var seen=new HashSet<int>{ids.First()};var q=new Queue<int>(seen);while(q.TryDequeue(out int id))foreach(int next in w.Cells[id].Neighbors)if(ids.Contains(next) && seen.Add(next))q.Enqueue(next);
  Check(seen.SetEquals(ids),"Each pair of faces is connected across its shared edge");
 }
 var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check(restored.Owners.SequenceEqual(w.Owners) && restored.ComputerPlayerCount==2 && restored.Looks.SequenceEqual(w.Looks),"Three Kingdoms uses normal saves");
}
Console.WriteLine("PASS: 24 Three Kingdoms worlds, adjacent whole-face pairs, eight sectors each, human/CPU roles and save roundtrip.");
Console.WriteLine("PASS: 24 random campaigns, equal connected territories, identical characters, control, normal initialization and save roundtrip.");
using var game=new CampaignChecks();game.Run();
sealed class CampaignChecks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;
 readonly string save=Path.Combine(AppContext.BaseDirectory,"campaign-save-test","autosave.json.gz");
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 Rectangle Rect(string n,params object[] args){var m=typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic)!;return (Rectangle)m.Invoke(m.IsStatic?null:this,args)!;}
 void Click(Point point){var canvas=(Rectangle)Call("CanvasBounds");var physical=new Point(canvas.X+point.X*canvas.Width/1920,canvas.Y+point.Y*canvas.Height/1080);foreach(var button in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(physical.X,physical.Y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);}
 void Drag(Point from,Point to){var c=(Rectangle)Call("CanvasBounds");foreach(var state in new[]{(from,ButtonState.Released),(from,ButtonState.Pressed),(to,ButtonState.Pressed),(to,ButtonState.Released)}){var q=new Point(c.X+state.Item1.X*c.Width/1920,c.Y+state.Item1.Y*c.Height/1080);Call("HandleInput",new MouseState(q.X,q.Y,0,state.Item2,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);}}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSavePath",save);Set("_autoSaveEnabled",true);if(File.Exists(save))File.Delete(save);Call("OpenTitleStartMenu");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Click(Rect("TitleMenuItem",1).Center);Check((int)Get("_campaignMenuStage")==1,"Title campaign action opens the host introduction");Check(!(bool)Call("UpdateDemo",11d,true,false,false,false,11d),"Campaign menu blocks idle Demo");}
  if(frame==2){Click(new(1200,580));Check((int)Get("_campaignMenuStage")==2,"Scenario selection opens flat sixty-character grid");}
  if(frame==3){Click(new(1830,950));Click(Rect("CampaignCharacterCard",59).Center);Click(Rect("CampaignPlayerTab",1).Center);Click(Rect("CampaignCharacterCard",59).Center);Check(((int[])Get("_campaignCharacters")).SequenceEqual(new[]{59,59}),"Both sides can select the sixtieth character");}
  if(frame==4){var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();Click(Rect("CampaignRandomButton",0).Center);Check(((int[])Get("_campaignCharacters"))[0] is >=0 and <60,"Random chooses a valid face");Click(Rect("CampaignPlayerTab",0).Center);Click(Rect("CampaignCharacterCard",59).Center);Click(new(210,1035));Check((int)Get("_campaignMenuStage")==1,"Back returns to scenario menu without starting a world");Click(new(1200,580));}
  if(frame==5){Click(new(210,950));Check(Get("_screen").ToString()=="OpeningBook","Campaign starts with the storybook");Click(new(1675,55));var w=(WorldSetup)Get("_setup");Check(Get("_screen").ToString()=="WorldStatus" && w.ActiveCount==2 && w.Looks.All(l=>l.BaseId==59) && (bool)Get("_conquestOverview"),"Starting at 540p skips world building and enters normal conquest overview");Check(File.Exists(save),"Campaign start writes the standard domestic autosave");Call("ReturnToTitle");Call("OpenTitleStartMenu");Click(Rect("TitleMenuItem",2).Center);Check(Get("_screen").ToString()=="WorldStatus" && ((WorldSetup)Get("_setup")).Looks.All(l=>l.BaseId==59),"Continue restores the campaign and duplicate characters");Call("FinishConquestOverview");Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);}
  if(frame==6){Call("ReturnToTitle");Call("OpenTitleStartMenu");Call("OpenCampaignMenu");Click(new(1200,720));Check((int)Get("_campaignScenario")==2 && ((int[])Get("_campaignCharacters")).Length==3,"Scenario two uses the shared three-player selector");Click(Rect("CampaignPlayerTab",2).Center);Click(new(1830,950));Click(Rect("CampaignCharacterCard",59).Center);Check(((int[])Get("_campaignCharacters"))[2]==59,"Third participant can select character sixty");}
  if(frame==7){Set("_campaignCharacters",Enumerable.Repeat(1,60).ToArray());for(int i=0;i<9;i++)Click(new(890,950));Click(Rect("CampaignPlayerTab",59).Center);Click(Rect("CampaignCharacterCard",59).Center);Check((int)Get("_campaignCharacterTarget")==59 && ((int[])Get("_campaignCharacters"))[59]==59,"Shared selector handles all sixty participant slots by paging");}
  if(frame==8){Drag(new(960,500),new(1180,500));Check((int)Get("_campaignDivider")==1180,"Divider widens participant list");Click(Rect("CampaignPlayerTab",59).Center);Check((int)Get("_campaignCharacterTarget")==59,"Participant hit targets follow divider");}
  if(frame==9){Drag(new(1180,500),new(640,500));Check((int)Get("_campaignDivider")==640,"Divider widens character list");Set("_campaignCharacters",new[]{0,29,59});Set("_campaignPlayerPage",0);Set("_campaignCharacterTarget",0);Set("_campaignCharacterPage",0);}
  if(frame==10){Set("_campaignCharacters",new[]{0,29,59});Set("_campaignPlayerPage",0);Set("_campaignCharacterTarget",0);Click(new(210,950));Check(Get("_screen").ToString()=="OpeningBook","Campaign starts with the storybook");Click(new(1675,55));var w=(WorldSetup)Get("_setup");Check(w.ActiveCount==3 && w.Cells.Length==24 && w.ComputerPlayerCount==2 && w.Looks[2].BaseId==59,"Scenario two begins a playable world at 540p");}
  base.Draw(t);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"campaign-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==10){Console.WriteLine("PASS: both campaigns, shared paged selector, sixty participants, draggable divider, duplicate picks, 540p input and playable starts.");Exit();}
 }
}
