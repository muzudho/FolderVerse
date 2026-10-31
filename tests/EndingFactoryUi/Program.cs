using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new VerifyGame();game.Run();
sealed class VerifyGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;int frame,node;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 Rectangle Button(string n)=>(Rectangle)typeof(Game1).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
 static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 string Screen=>Get("_screen").ToString()!;
 void Input(Point at,ButtonState b,double seconds=0){var c=(Rectangle)Call("CanvasBounds");var p=new Point(c.X+at.X*c.Width/1920,c.Y+at.Y*c.Height/1080);Call("HandleInput",new MouseState(p.X,p.Y,0,b,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(20),TimeSpan.FromSeconds(seconds)),true);}
 void Click(Point p){Input(p,ButtonState.Released);Input(p,ButtonState.Pressed);Input(p,ButtonState.Released);}
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.PrepareCampaignDuel(123,456,789,12,0,1);node=w.Nodes.All.First(n=>n.Owner==0 && w.Robots.Workshops[n.Id].ProductionLines.Length>0).Id;Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Call("OpenNodeInspector",node);Call("UpdateDomesticSlide",1f);Call("OpenNodeFactory");Input(Point.Zero,ButtonState.Released);}
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame is 1 or 3 or 5){int field=frame==3?1:frame==5?2:0;Set("_factoryLine",0);Click(((Rectangle)Call("FactoryDropButton",field)).Center);Check((int)Get("_factoryDropDown")==field,"Physical click opens factory field "+field);}
  if(frame is 2 or 4 or 6){int field=frame==4?1:frame==6?2:0;Click(((Rectangle)Call("FactoryDropOption",field,3)).Center);Check((int)Get("_factoryDropDown")==-1,"Choice closes dropdown");var f=w.Robots.Workshops[node];Check((field==0?f.ProductionLines[0].RestEvery:field==1?f.ProductionLines[0].RestTurns:f.ReservedSlots)==(field==2?3:4),"Choice updates field "+field);}
  if(frame==7){var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();Click(((Rectangle)Call("FactoryDropButton",1)).Center);Check((int)Get("_factoryDropDown")==1,"540p dropdown opens");}
  if(frame==8){Click(((Rectangle)Call("FactoryDropOption",1,8)).Center);Check(w.Robots.Workshops[node].ProductionLines[0].RestTurns==9,"540p choice updates休9");w.Industry.Queue(1);Call("BeginIndustry");Check(Screen=="Industry" && !w.WorldConquestEndingShown,"Incomplete world enters industry");w.Industry.Queue(2);foreach(var n in w.Nodes.All)n.Owner=0;w.Nodes.All[^1].Owner=-1;Check(!(bool)Call("TryConquestEnding"),"Unclaimed node prevents full conquest");w.Nodes.All[^1].Owner=1;Check(!(bool)Call("TryConquestEnding"),"Independent enemy prevents full conquest");w.Relations.SetSuperior(1,0);Call("BeginIndustry");Check(Screen=="Ending" && w.Industry.Pending && w.Industry.SelectedSector==-1 && w.WorldConquestEndingShown,"Conquest intercepts industry without applying it");Call("UpdateEndingMusic",true);Check(Get("_endingMusic")!=null,"Ending starts BGM");}
  if(frame==9){Input(Point.Zero,ButtonState.Released,4);Check(Screen=="Ending" && (double)Get("_endingAge")==4,"Timer advances and game remains resumable");}
  if(frame==10){Click(Button("EndingResume").Center);Check(Screen=="Industry" && w.Industry.Pending,"Resume returns to pending industry");Check(Get("_endingMusic")==null,"Resume stops ending music");Call("BeginIndustry");Check(Screen=="Industry","Ending is shown once");var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check(restored.WorldConquestEndingShown,"Conquest acknowledgement survives save roundtrip");Call("StartEnding");Click(Button("EndingResume").Center);Check(Screen=="WorldStatus","Manual ending resumes domestic play");Call("StartEnding");}
  if(frame==11){Input(Point.Zero,ButtonState.Released,9.9);Check(Screen=="Ending","Timer remains before deadline");}
  if(frame==12){Input(Button("EndingResume").Center,ButtonState.Pressed,.1);Check(Screen=="Title","Resume at deadline times out to title");w.PrepareCampaignDuel(123,456,789,12,0,1);Check(!w.WorldConquestEndingShown,"New campaign resets conquest acknowledgement");Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("StartEnding");Set("_demoActive",true);Call("UpdateDemo",10d,true,false,false,false,30d);Check(Screen=="Title" && !(bool)Get("_demoActive"),"Demo ending times out and releases demo control");w.PrepareCampaignDuel(123,456,789,12,0,1);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("StartEnding");Click(Button("EndingReturn").Center);Check(Screen=="Title","Title button works immediately");MusicChecks();}
  base.Draw(t);
  if(frame is 1 or 3 or 5 or 7 or 8 or 9 or 11){var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"ending-factory-{frame}.png"));image.SaveAsPng(file,image.Width,image.Height);}
  if(frame++==12){Console.WriteLine("PASS: actual factory dropdown input/rendering at1080p/540p; conquest-before-industry, resume, once-only saved flag, deadline/title and BGM lifecycle; deterministic finite PCM.");Exit();}
 }
 static void MusicChecks(){var all=new EndingMusicScore().Render(EndingMusicScore.Rate*11);var chunked=new EndingMusicScore();var parts=new List<byte>();for(int i=0;i<110;i++)parts.AddRange(chunked.Render(2205));Check(all.SequenceEqual(parts),"PCM is independent of render chunk size");Check(all.Skip(EndingMusicScore.Rate*20).All(b=>b==0),"Score is silent after10 seconds");Check(all.Take(EndingMusicScore.Rate*14).Any(b=>b!=0),"Music contains audio before cadence");}
}


