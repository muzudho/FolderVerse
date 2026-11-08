using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
if(args.Length!=3)throw new ArgumentException("Content directory, old-save copy, expected assembly version required.");
object saved=AutoSaveStore.Read<object>(args[1]);var world=saved is WorldSetup w?w:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;world.Robots.Check();
Console.WriteLine($"PASS: copied legacy save turn={world.Population.Turn}, world={world.WorldSeed}, cast={world.CastSeed}, placement={world.PlacementSeed}.");
if(typeof(Game1).Assembly.GetName().Version!.ToString()!=args[2])throw new Exception("Unexpected release assembly version");
using var game=new ReleaseGame(Path.GetFullPath(args[0]));game.Run();
sealed class ReleaseGame(string content):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;
 void Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a);
 protected override void LoadContent(){Content.RootDirectory=content;base.LoadContent();}
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==1)Call("BeginOpening",false,null!);if(frame==2)Call("BeginOpeningLottery");
  base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame),TimeSpan.Zero));
  var p=GraphicsDevice.PresentationParameters;var colours=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(colours);
  if(colours.Distinct().Count()<30)throw new Exception("Release frame is blank");
  using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(colours);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"release-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==2){Console.WriteLine("PASS: published assembly/content render title, storybook and planet lottery; real autosave disabled by test host.");Exit();}
 }
}
