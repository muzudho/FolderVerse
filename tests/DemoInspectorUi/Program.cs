using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new DemoInspectorChecks();game.Run();
sealed class DemoInspectorChecks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;int node;WorldSetup world=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);world.SelectPlayer(0);
  ((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);
  Call("ResetInspector");Check((int)Get("_inspectedNode")==-1,"Fixture starts with no inspected supervisor");
  Set("_demoDomesticStage",0);Set("_demoDomesticAge",0d);Call("UpdateDemoDomestic",1.8d);Call("UpdateDomesticSlide",1f);
  node=(int)Get("_populationNode");Check(node>=0 && (int)Get("_inspectedNode")==node && (bool)Get("_nodeOfficeOpen"),"Demo opens the office with synchronized node and supervisor IDs");
  var font=(SpriteFont)Get("_font");Check("伏太峡渓渡谷".All(font.Characters.Contains),"Compiled font includes every previously missing static glyph");Check(font.DefaultCharacter=='?' && !font.Characters.Contains('\uE000'),"Unknown external text has an explicit fallback");Check(font.MeasureString("external \uE000").X>0,"Unknown dynamic glyph cannot throw during measurement");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Set("_inspectedNode",-1);} // Office must render its explicit node, independent of region supervisor state.
  if(frame==2){Set("_inspectedNode",node);Set("_demoDomesticStage",11);Set("_demoDomesticAge",0d);Call("UpdateDemoDomestic",1.8d);Check((int)Get("_populationNode")==-1 && (int)Get("_inspectedNode")==-1 && !(bool)Get("_nodeOfficeOpen"),"Demo closes inspectors through the shared reset path");}
  if(frame==3){Call("OpenNodeInspector",node);Call("OpenNodeAssembly");Set("_hangarMessage","external \uE000");}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"demo-office-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame++==3){Console.WriteLine("PASS: Demo office opening/closing, explicit supervisor subject, unknown dynamic text measurement and rendering.");Exit();}
 }
}
