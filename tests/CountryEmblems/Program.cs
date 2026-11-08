using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
Check(!WorldSetup.CountryEmblemNames.Any(n=>n is "星" or "月" or "太陽"),"Retired motifs remain selectable");
Check(WorldSetup.CountryEmblemNames.Length==16 && WorldSetup.CountryEmblemNames[13]=="チューリップ","Stable IDs and added rainbow");
var world=new WorldSetup();world.SetParticipantCounts(0,60);world.SetWorld(0);world.SetCast(1);world.SetPlacement(3);
foreach(int id in Enumerable.Range(0,16))world.SetCountryEmblem(0,id);
var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));Check(saved.CountryEmblem(0)==15,"Rainbow save roundtrip");
// Historical slots 1..3 now consistently resolve to the replacements in old saves.
foreach(int id in new[]{1,2,3}){world.SetCountryEmblem(0,id);var old=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));Check(old.CountryEmblem(0)==id,"Replacement IDs must not shift existing motifs");}
var seen=new HashSet<string>();for(int seed=0;seed<100;seed++){world.SetCast(seed);for(int i=0;i<60;i++)seen.Add(WorldSetup.CountryEmblemNames[world.CountryEmblem(i)]);}
Check(new[]{"猫","イルカ","四分音符","ダイヤモンド","虹"}.All(seen.Contains),"New motifs missing from automatic casts");
var topStripe=Enumerable.Range(0,10).Select(x=>WorldSetup.CountryPatternSecondary(1,x,0)).ToArray();
Check(!topStripe.SequenceEqual(Enumerable.Range(0,10).Select(x=>WorldSetup.CountryPatternSecondary(1,x,1))) &&
 !topStripe.SequenceEqual(Enumerable.Range(0,10).Select(y=>WorldSetup.CountryPatternSecondary(1,1,y))),"Necktie stripes must vary across both rows and columns");
using var game=new EmblemGame(args.Length>0?args[0]:AppContext.BaseDirectory);game.Run();
sealed class EmblemGame(string output):Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup world=null!;SpriteBatch batch=null!;UiPainter ui=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.SetParticipantCounts(1,19);world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.SelectTurnRuler(0);batch=(SpriteBatch)Get("_spriteBatch");ui=(UiPainter)Get("_ui");Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);}
 protected override void Update(GameTime time){}
 Color[] Raster(int emblem){using var target=new RenderTarget2D(GraphicsDevice,64,64);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(Color.Transparent);batch.Begin();CountryEmblemArt.Draw(ui,new(0,0,64,64),emblem,Color.White);batch.End();GraphicsDevice.SetRenderTarget(null);var pixels=new Color[4096];target.GetData(pixels);return pixels;}
 protected override void Draw(GameTime time){
  if(frame==0){
   var masks=new[]{1,2,3,5,6,14}.Select(Raster).Select(p=>string.Concat(p.Select(c=>c==Color.White?'1':'0'))).ToArray();Check(masks.Distinct().Count()==6,"New silhouettes, heart and two diamonds must differ");
   var heart=Raster(5);Check(heart[12*64+32]!=Color.White && heart[12*64+19]==Color.White && heart[12*64+45]==Color.White,"Heart needs two round lobes and a visible cleft");
   var rainbow=Raster(15);int arches=0;bool previous=false;for(int y=0;y<64;y++){bool white=rainbow[y*64+32]==Color.White;if(white && !previous)arches++;previous=white;}Check(arches==3,"Rainbow needs three separated arches");
   using var target=new RenderTarget2D(GraphicsDevice,1500,1080);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(18,35,46));batch.Begin();
   ui.Text("ワンポイントパターン",new(40,20),.8f,Color.White);ui.Text("64px / 32px / 16px",new(500,40),.5f,Color.White);
   ui.Text("無地",new(780,70),.4f,Color.White);ui.Text("斜め縞",new(1080,70),.4f,Color.White);
   int row=0;foreach(int id in new[]{1,2,3,5,6,14,15}){
    int y=100+row++*135;ui.Text(WorldSetup.CountryEmblemNames[id],new(40,y+30),.55f,Color.White);
    CountryEmblemArt.Draw(ui,new(370,y+12,64,64),id,new(255,218,99));CountryEmblemArt.Draw(ui,new(530,y+30,32,32),id,new(255,218,99));CountryEmblemArt.Draw(ui,new(620,y+40,16,16),id,new(255,218,99));
    world.SetCountryEmblem(0,id);world.SetCountryAppearance(0,7,2,0);world.SetCountryFlagShape(0,0);CountryFlagArt.Draw(ui,world,0,new(780,y,170,110));world.SetCountryAppearance(0,7,2,1);CountryFlagArt.Draw(ui,world,0,new(1080,y,170,110));
   }batch.End();GraphicsDevice.SetRenderTarget(null);using(var file=File.Create(Path.Combine(output,"new-emblems.png")))target.SaveAsPng(file,1500,1080);
   using var preview=new RenderTarget2D(GraphicsDevice,1000,270);GraphicsDevice.SetRenderTarget(preview);GraphicsDevice.Clear(new Color(18,35,46));batch.Begin();
   ui.Text("虹 / ３本のアーチ",new(35,15),.52f,Color.White);ui.Text("無地",new(340,15),.52f,Color.White);ui.Text("斜め縞",new(690,15),.52f,Color.White);
   CountryEmblemArt.Draw(ui,new(50,70,160,160),15,new(247,194,63));world.SetCountryEmblem(0,15);world.SetCountryAppearance(0,7,2,0);CountryFlagArt.Draw(ui,world,0,new(340,70,250,160));world.SetCountryAppearance(0,7,2,1);CountryFlagArt.Draw(ui,world,0,new(690,70,250,160));
   batch.End();GraphicsDevice.SetRenderTarget(null);using(var file=File.Create(Path.Combine(output,"rainbow-stripes.png")))preview.SaveAsPng(file,1000,270);
  }else{
   Call("OpenCountrySettings");Set("_pointer",new Point(1759,1003));Call("CountrySettingsInput",true,false);Check(world.CountryEmblem(0)==15,"Last rainbow button is visible and selectable");base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
   var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using var file=File.Create(Path.Combine(output,"new-emblems-settings.png"));image.SaveAsPng(file,image.Width,image.Height);
   Console.WriteLine("PASS: replacement IDs, rainbow save/cast, three separated arches, diagonal repeat, silhouettes, heart cleft and complete settings grid.");Exit();
  }frame++;
 }
}
