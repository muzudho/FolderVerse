using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
sealed class CountrySettingsUi:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 void Click(int x,int y){var c=(Rectangle)Call("CanvasBounds");foreach(var s in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(c.X+x*c.Width/1920,c.Y+y*c.Height/1080,0,s,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);}
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetParticipantCounts(1,59);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);}
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==0){Click(1680,43);Click(1550,376);Check((bool)Get("_countrySettingsOpen"),"System menu opens settings through real input");}
  if(frame==1){Click(765,910);Check((int)Get("_countrySettingsRuler")==58,"Bottom row can select country 59");Click(1200,335);Click(1535,476);Click(1535,596);Click(1430,757);Check(w.IsHumanPlayer(58) && w.ColorIndices[58]==6 && w.CountrySecondaryColor(58)==6 && w.CountryPattern(58)==1,"Real controls change country 59");Click(1200,596);}
  if(frame==2){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  if(frame==3){Click(1200,820);Check(w.CountryPattern(58)==3,"Scaled input selects grid");}
  if(frame==4){Click(1780,110);Check(!(bool)Get("_countrySettingsOpen"),"Close dismisses modal");}
  if(frame==5){Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-100d);foreach(var n in w.Nodes.All)n.Owner=58;Array.Fill(w.Owners,58);var net=(CubeNet)Get("_net");net.ShowCountryColors=true;net.ShowRobots=net.ShowBatteries=net.ShowMapSymbols=net.ShowEquator=net.ShowRoutes=false;Set("_statusGlobe",false);var world=(WorldPreview)Get("_world");world.CountryColors=true;world.ShowSetup(w,true);}
  if(frame==6)Set("_statusGlobe",true);
  if(frame==7){Set("_screen",Enum.Parse(Get("_screen").GetType(),"CastReview"));((WorldPreview)Get("_world")).ShowSetup(w,false);}
  if(frame==8){Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("OpenCountrySettings");Click(180,225);Click(1580,335);Click(765,910);Click(1580,335);Click(1780,110);Check(w.ComputerOnly && (bool)Get("_autoPlay"),"Closing after removing the last human starts automatic play");}
  if(frame==9){Click(10,10);Check(!(bool)Get("_autoPlay"),"Automatic play can be interrupted");Call("OpenCountrySettings");Click(180,225);Click(1200,335);Click(1780,110);Check(w.PlayerSlot==0 && !w.ComputerOnly && !(bool)Get("_autoPlay"),"Switching back selects a human controller");}
  base.Draw(time);
  if(frame is 1 or 3 or 5 or 6 or 7){var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"country-settings-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);}
  if(frame++==9){Console.WriteLine("PASS: system menu, 60-country selector, human/color/pattern inputs, automatic/manual transitions, net/globe/cast previews and real rendering at 1080p/540p.");Exit();}
 }
}
