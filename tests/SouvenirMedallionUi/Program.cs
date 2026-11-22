using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CoinCheck(args[0]);game.Run();
sealed class CoinCheck(string output):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;SpriteBatch batch=null!;int frame;readonly double[] ages={0,.36,.85,1.2,1.5};
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 protected override void LoadContent(){
  Window.Position=new(-3000,-3000);Directory.CreateDirectory(output);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Set("_demoActive",false);Set("_autoPlay",false);
  w=(WorldSetup)Get("_setup");w.SetParticipantCounts(4,4);w.SetWorld(2);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);w.HumanPlayers.Add(0);batch=(SpriteBatch)Get("_spriteBatch");
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);Set("_coinClock",10d);Call("QueueDomesticMedallion");
  var back=SouvenirMedallion.Pose(0);var edge=SouvenirMedallion.Pose(.85f);var front=SouvenirMedallion.Pose(1.5f);Check(back.Reverse&&back.ScaleX==1&&!front.Reverse&&front.ScaleX>.99f&&edge.ScaleX<.02f,"entry turns reverse through edge to portrait face");Check(SouvenirMedallion.Pose(.36f).Glint>.9f,"glint coincides with turn start");Check(SouvenirMedallion.ChimePcm().Any(b=>b!=0)&&SouvenirMedallion.ChimePcm().Length>20000,"sparkle chime has audible PCM waveform");
  Check(Enumerable.Range(1,4).Select(SouvenirMedallion.Metal).Distinct().Count()==4&&SouvenirMedallion.Metal(10)==SouvenirMedallion.Metal(4),"ranks map to gold silver copper and plated fallback");
  var end=(Rectangle)typeof(Game1).GetMethod("PrimaryActionHomeBounds",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{1})!;var normal=(Rectangle)typeof(Game1).GetMethod("PrimaryActionHomeBounds",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{0})!;Check(end.Width==normal.Width*2&&end.Right<normal.Left,"turn-end button is double width without overlap");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  Set("_coinClock",10+ages[frame]);using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(new Color(16,35,47));batch.Begin();Call("DrawDomesticMedallion",0,new Vector2(176,916));for(int i=0;i<4;i++)Call("DrawRulerMedallion",i,new Vector2(400+i*350,400));batch.End();GraphicsDevice.SetRenderTarget(null);
  using(var file=File.Create(Path.Combine(output,"coin-entry-"+frame+".png")))image.SaveAsPng(file,1920,1080);
  if(frame==0){Check(!(bool)Get("_coinEntryPending")&&(double)Get("_coinEntryStarted")==10,"entry starts when domestic medal becomes visible");var f=(Texture2D)Get("_coinFront");var b=(Texture2D)Get("_coinBack");Color[] Read(Texture2D texture){var data=new Color[texture.Width*texture.Height];texture.GetData(data);return data;}var fp=Read(f);var bp=Read(b);Check(fp[f.Width/2+f.Height/2*f.Width].A==0&&bp[b.Width/2+b.Height/2*b.Width].A>200&&fp[0].A==0&&bp[0].A==0,"front aperture and reverse coin have correct transparency");}
  if(frame==1)Check((bool)Get("_coinEntrySound")&&(Get("_coinChime")!=null||(bool)Get("_coinAudioUnavailable")),"chime is triggered once or gracefully handles missing audio hardware");
  if(++frame==ages.Length){Call("QueueDomesticMedallion");Check(!(bool)Get("_coinEntryPending"),"reopening domestic camera does not repeat the same turn intro");Call("ResetDomesticMedallionEntry");Call("QueueDomesticMedallion");Check((bool)Get("_coinEntryPending"),"new game can replay the intro even with matching seeds and turn");Console.WriteLine("PASS engraved commemorative medal and reverse-to-front entry");Exit();}
 }
}
