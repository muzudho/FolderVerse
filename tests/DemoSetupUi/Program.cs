using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
using var game=new Checks();game.Run();
sealed class Checks:Game1{
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Static;WorldSetup w=null!;int frame;double now;
object Get(string n){var f=typeof(Game1).GetField(n,F);return f!=null?f.GetValue(this)!:typeof(Game1).GetProperty(n,F)!.GetValue(this)!;}
void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
static void Check(bool b,string m){if(!b)throw new Exception(m);}
void StepDemo(){now+=1d/18;Call("UpdateDemo",1d/18,true,false,false,false,now);}
protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");}
void NativeClick(Point point){var canvas=(Rectangle)Call("CanvasBounds");var p=new Point(canvas.X+point.X*canvas.Width/1920,canvas.Y+point.Y*canvas.Height/1080);foreach(var button in new[]{Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Pressed,Microsoft.Xna.Framework.Input.ButtonState.Released})Call("HandleInput",new Microsoft.Xna.Framework.Input.MouseState(p.X,p.Y,0,button,0,0,0,0),new Microsoft.Xna.Framework.Input.KeyboardState(),new GameTime(),true);}
void VerifyNativeSelection(){Set("_demoActive",false);Call("ResetInspector");w.SetParticipantCounts(1,19);w.SetWorld(123);w.SetCast(456);w.SetTerritoryRights(789);Call("BeginSelection");var tile=((CharacterTile[])Get("_selectionTiles")).First(t=>t.Slot==3);NativeClick(tile.Bounds.Center);Check(w.PlayerSlot==3 && Get("_screen").ToString()=="PlayerReady","Physical native click still selects the same face through shared selection input");NativeClick(((Rectangle)Get("StatusProceed")).Center);Check(w.HumanPlayers.SequenceEqual(new[]{3}) && Get("_screen").ToString()=="LandConquest","Physical native confirmation still follows normal flow");}
protected override void Update(GameTime t){}
protected override void Draw(GameTime t){int scenario=frame/7,step=frame%7;
if(step==0){Call("ResetInspector");w.SetParticipantCounts(scenario%2==0?1:2,19);w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);((WorldPreview)Get("_world")).ShowSetup(w,true);Set("_screen",Enum.Parse(Get("_screen").GetType(),"PlacementRolling"));Set("_demoActive",true);Set("_demoIntro",false);Set("_demoAge",0d);Set("_demoStepAge",0d);Set("_demoFinishStage",0);Set("_demoSwing",0f);Set("_conquestOverview",false);var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=scenario>=2?960:1920;g.PreferredBackBufferHeight=scenario>=2?540:1080;g.ApplyChanges();}
else if(Get("_screen").ToString()!="LandConquest"){
 string screen=Get("_screen").ToString()!;int selected=-1;Rectangle expected;
 if(screen=="PlayerSelect"){var tile=((CharacterTile[])Get("_selectionTiles")).Where(t=>t.Slot>=0 && !w.HumanPlayers.Contains(t.Slot)).OrderBy(t=>t.Slot).First();selected=tile.Slot;expected=tile.Bounds;}else expected=(Rectangle)Get(screen=="PlayerReady"?"StatusProceed":"CastAction");
 int beforeCount=w.HumanPlayers.Count,ticks=0;while(Get("_screen").ToString()==screen && ticks++<100)StepDemo();
 var click=((Vector2)Get("_demoClickPoint")).ToPoint();Check(ticks<=56,"Demo advances after natural three-second wait");Check(expected.Contains(click),"Demo click is inside actual control/tile for "+screen);
 if(screen=="PlayerSelect"){Check((int)Call("HitSelection",click)==selected && w.PlayerSlot==selected,"Actual selected character matches hit face");Check(!w.HumanPlayers.Contains(selected),"Demo never chooses an already assigned human character");Check(Get("_screen").ToString()=="PlayerReady","Character selection opens confirmation");}
 if(screen=="PlayerReady"){Check(w.HumanPlayers.Count==beforeCount+1 && w.HumanPlayers.Distinct().Count()==w.HumanPlayers.Count,"Confirmation adds one distinct human ruler");Check(Get("_screen").ToString()==(w.HumanPlayers.Count<w.PlayerCount?"PlayerSelect":"LandConquest"),"Confirmation follows normal next-human/land flow");}
 if(screen=="PlacementReview")Check(Get("_screen").ToString()=="PlayerSelect","Territory confirmation opens selection");
 Console.WriteLine($"scenario={scenario} before={screen} click={click} expected={expected} selected={selected} humans={w.HumanPlayers.Count} after={Get("_screen")}");
}
if(step==6)Check(Get("_screen").ToString()=="LandConquest","One/two human setup reaches land conquest");
base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var data=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(data);using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(data);using var f=File.Create(Path.Combine(AppContext.BaseDirectory,$"demo-setup-{frame}.png"));tex.SaveAsPng(f,tex.Width,tex.Height);
if(frame++==27){VerifyNativeSelection();Console.WriteLine("PASS: natural18FPS placement stop/confirm, actual face hit, confirmation and distinct one/two-human setup at1080p/540p.");Exit();}
}
}
