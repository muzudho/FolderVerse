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
Console.WriteLine("PASS: 24 random campaigns, equal connected territories, identical characters, control, normal initialization and save roundtrip.");
using var game=new CampaignChecks();game.Run();
sealed class CampaignChecks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;
 readonly string save=Path.Combine(AppContext.BaseDirectory,"campaign-save-test","autosave.json.gz");
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static Rectangle Rect(string n,params object[] args)=>(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args)!;
 void Click(Point point){var canvas=(Rectangle)Call("CanvasBounds");var physical=new Point(canvas.X+point.X*canvas.Width/1920,canvas.Y+point.Y*canvas.Height/1080);foreach(var button in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(physical.X,physical.Y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSavePath",save);Set("_autoSaveEnabled",true);if(File.Exists(save))File.Delete(save);Call("OpenTitleStartMenu");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Click(Rect("TitleMenuItem",1).Center);Check((int)Get("_campaignMenuStage")==1,"Title campaign action opens the host introduction");Check(!(bool)Call("UpdateDemo",11d,true,false,false,false,11d),"Campaign menu blocks idle Demo");}
  if(frame==2){Click(new(1200,580));Check((int)Get("_campaignMenuStage")==2,"Scenario selection opens flat sixty-character grid");}
  if(frame==3){Click(Rect("CampaignCharacterCard",59).Center);Click(Rect("CampaignPlayerTab",1).Center);Click(Rect("CampaignCharacterCard",59).Center);Check(((int[])Get("_campaignCharacters")).SequenceEqual(new[]{59,59}),"Both sides can select the sixtieth character");}
  if(frame==4){var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();Click(Rect("CampaignRandomButton",0).Center);Check(((int[])Get("_campaignCharacters"))[0] is >=0 and <60,"Random chooses a valid face");Click(Rect("CampaignPlayerTab",0).Center);Click(Rect("CampaignCharacterCard",59).Center);Click(new(210,1035));Check((int)Get("_campaignMenuStage")==1,"Back returns to scenario menu without starting a world");Click(new(1200,580));}
  if(frame==5){Click(new(210,950));var w=(WorldSetup)Get("_setup");Check(Get("_screen").ToString()=="WorldStatus" && w.ActiveCount==2 && w.Looks.All(l=>l.BaseId==59) && (bool)Get("_conquestOverview"),"Starting at 540p skips world building and enters normal conquest overview");Check(File.Exists(save),"Campaign start writes the standard domestic autosave");Call("ReturnToTitle");Call("OpenTitleStartMenu");Click(Rect("TitleMenuItem",2).Center);Check(Get("_screen").ToString()=="WorldStatus" && ((WorldSetup)Get("_setup")).Looks.All(l=>l.BaseId==59),"Continue restores the campaign and duplicate characters");Call("FinishConquestOverview");Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);}
  base.Draw(t);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"campaign-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==5){Console.WriteLine("PASS: title action, host, sixty-face grid, duplicate picks, random, 540p clicks and playable campaign start.");Exit();}
 }
}
