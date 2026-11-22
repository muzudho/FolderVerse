using FolderVerse;
using System.Reflection;
using System.Text.Json;
using System.IO.Compression;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
object Call(object obj,string name,params object[] a)=>obj.GetType().GetMethod(name,F)!.Invoke(obj,a)!;
RobotWorld Fixture(){var r=new RobotWorld();r.Initialize(1,1);Call(r.Nodes[0],"SetCapacity",9);for(int i=0;i<8;i++)r.Nodes[0].Add(r.Create(0,RobotParts.Complete));typeof(RobotWorld).GetField("_mapleDeposits",F)!.SetValue(r,new[]{10});return r;}
var robots=Fixture();Check((bool)Call(robots,"CanHarvestMaple",0,1,RawMaterial.Maple),"eighth of nine slots does not withhold last slot");
Check((bool)Call(robots,"HarvestMaple",0,1,RawMaterial.Maple)&&robots.Nodes[0].Count==9,"harvest can fill ninth slot");
robots=Fixture();robots.Nodes[0].Add(robots.Create(0,RobotParts.Complete));Check(!(bool)Call(robots,"CanHarvestMaple",0,1,RawMaterial.Maple),"physically full hangar still waits");
robots=Fixture();Call(robots.Nodes[0],"ReserveStorage",-100L);Check(!(bool)Call(robots,"CanHarvestMaple",0,1,RawMaterial.Maple),"real return reservation still protects last slot");Call(robots.Nodes[0],"ReleaseReturn",-100L);Check((bool)Call(robots,"CanHarvestMaple",0,1,RawMaterial.Maple),"released return reservation makes slot usable");
robots=Fixture();robots.ProductionIntakes.Add(new(0,null,robots.Create(0,RobotParts.Head),robots.Workshops[0]));Check(!(bool)Call(robots,"CanHarvestMaple",0,1,RawMaterial.Maple),"real production intake still protects last slot");
robots=Fixture();robots.Workshops[0].GenerateLines(123);robots.Workshops[0].Automation=NodeAutomation.Auto;robots.AdjustNode(0,0,(_,_)=>false);Check(robots.Nodes[0].StorageLimit==9&&(bool)Call(robots,"CanHarvestMaple",0,1,RawMaterial.Maple),"automation does not recreate empty buffer");
var doc=AutoSaveStore.Capture(robots);Check(doc.Version==AutoSaveStore.CurrentVersion&&doc.Version==3,"new saves declare format version three");Check(doc.Objects.All(e=>e.Fields==null||!e.Fields.ContainsKey("<ReservedSlots>k__BackingField")),"new save contains no empty buffer setting");
doc.Version=2;int id=doc.Objects.Count;doc.Objects.Add(new(){Type=typeof(int).AssemblyQualifiedName!,Scalar="9"});foreach(var entry in doc.Objects.Where(e=>e.Type==typeof(RobotWorkshop).AssemblyQualifiedName))entry.Fields!["<ReservedSlots>k__BackingField"]=id;
var old=AutoSaveStore.Restore<RobotWorld>(doc);Check(old.Nodes[0].StorageLimit==9&&(bool)Call(old,"CanHarvestMaple",0,1,RawMaterial.Maple),"version two buffer setting is ignored without losing usable capacity");
foreach(int version in new[]{0,-1,AutoSaveStore.CurrentVersion+1}){var unsupported=AutoSaveStore.Capture(robots);unsupported.Version=version;bool rejected=false;try{AutoSaveStore.Restore<RobotWorld>(unsupported);}catch(AutoSaveStore.IncompatibleSaveException e){rejected=e.Message=="このセーブファイルは読め込めなくなりました";}Check(rejected,"unsupported version is rejected with requested message "+version);}
var missing=JsonSerializer.Deserialize<AutoSaveStore.Document>("{\"Root\":0,\"Objects\":[]}")!;Check(missing.Version==0,"missing version never pretends to be current");
string output=args[0];Directory.CreateDirectory(output);string path=Path.Combine(output,"current-save.json.gz");AutoSaveStore.Write(path,robots);Check(AutoSaveStore.Read<RobotWorld>(path).Nodes[0].Count==8,"version three gzip save roundtrip");
var future=AutoSaveStore.Capture(robots);future.Version=AutoSaveStore.CurrentVersion+1;string futurePath=Path.Combine(output,"future-save.json.gz");using(var zip=new GZipStream(File.Create(futurePath),CompressionLevel.Fastest))JsonSerializer.Serialize(zip,future);
using var game=new BufferUi(output,futurePath);game.Run();
sealed class BufferUi(string output,string futurePath):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;SpriteBatch batch=null!;WorldSetup w=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 protected override void LoadContent(){
 Window.Position=new(-3000,-3000);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));w=new(){EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.Relations.Reset();w.SelectPlayer(0);w.HumanPlayers.Add(0);w.Relations.Leader=0;foreach(var n in w.Nodes.All)n.Owner=0;
 Set("_world",new WorldPreview(GraphicsDevice));Set("_setup",w);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));int node=w.Nodes.All.First(n=>n.Functional).Id;Call("OpenNodeInspector",node);Call("OpenNodeFactory");Set("_factoryLine",0);
 batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");var font=Content.Load<SpriteFont>("UiFont");Set("_font",font);Set("_ui",new UiPainter(batch,pixel,font));Set("_portraitRenderer",new PortraitRenderer(Content));
 Call("NodeFactoryClick",new Point(475,900));if((int)Get("_factoryDropDown")!=-1)throw new Exception("Removed setting still opens dropdown");Console.WriteLine("PASS removed setting has no clickable dropdown");
 }
 void CheckDaylight(){
 var globe=(WorldPreview)Get("_world");globe.ShowSetup(w,false);globe.DaylightWorld=w;globe.DawnMeridian=.5f;globe.SelectionSetup=w;globe.NodeFocus=0;globe.FocusCircleArea=new(0,0,1920,1080);
 var type=typeof(WorldPreview);object Read(string n)=>type.GetField(n,F)!.GetValue(globe)!;void Apply()=>type.GetMethod("ApplyDaylight",F)!.Invoke(globe,null);
 Apply();var source=(VertexPositionColorNormal[])Read("_daySource");var night=((VertexPositionColorNormal[])Read("_dayWorking")).ToArray();int chosen=-1;Vector2 at=default;
 for(int i=0;i<source.Length;i++)if(night[i].Color!=source[i].Color&&globe.ProjectVisible(source[i].Position,source[i].Normal,0,0,globe.FocusCircleArea,out at)){chosen=i;break;}
 if(chosen<0)throw new Exception("No visible night sample");globe.FocusCircles.Add(new(at,40));Apply();var lit=(VertexPositionColorNormal[])Read("_dayWorking");if(lit[chosen].Color!=source[chosen].Color)throw new Exception("Night dims spotlight");
 globe.FocusCircles.Clear();Apply();if(lit[chosen].Color!=night[chosen].Color)throw new Exception("Removed spotlight leaves daylight behind");Console.WriteLine("PASS night spotlight bypass and unchanged-time cache invalidation");
 }
 void CheckHudMask(){
 var net=(CubeNet)Get("_recoveryNet");net.FocusBrightness=.22f;net.FocusCircles.Clear();net.FocusCircles.Add(new(new(1000,600),60));net.FocusCircles.Add(new(new(1040,600),60));
 using var target=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(Color.Red);batch.Begin();Call("DrawRecoveryNetMask");batch.End();GraphicsDevice.SetRenderTarget(null);var pixels=new Color[1920*1080];target.GetData(pixels);
 if(pixels[600*1920+1000]!=Color.Red||pixels[600*1920+1020]!=Color.Red||pixels[600*1920+1150]==Color.Red)throw new Exception("HUD circle union mask broken");Console.WriteLine("PASS HUD mask preserves circle interiors and overlap, dims exterior");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
 using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawNodeFactory");batch.End();GraphicsDevice.SetRenderTarget(null);using(var file=File.Create(Path.Combine(output,"factory-without-empty-buffer.png")))image.SaveAsPng(file,1920,1080);
 CheckDaylight();CheckHudMask();
 Set("_hasAutoSave",true);Set("_autoSavePath",futurePath);byte[] before=File.ReadAllBytes(futurePath);if((bool)Call("ResumeAutoSave")||(string)Get("_autoSaveError")!="このセーブファイルは読め込めなくなりました"||!before.SequenceEqual(File.ReadAllBytes(futurePath)))throw new Exception("Incompatible save UI must show requested message without changing file");Console.WriteLine("PASS resume shows incompatible message and preserves original save");Exit();
 }
}
