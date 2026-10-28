using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
if(args.Length!=1){Console.Error.WriteLine("Usage: LocationResume <autosave.json.gz>");return;}
using var g=new Probe(Path.GetFullPath(args[0]));g.Run();
sealed class Probe:Game1{
 private readonly string savePath;
 public Probe(string path){savePath=path;}
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_conquestOverviewEnabled",false);Set("_autoSavePath",savePath);Set("_hasAutoSave",true);if(!(bool)Call("ResumeAutoSave"))throw new Exception("resume failed");var w=(WorldSetup)Get("_setup");Console.WriteLine($"turn={w.Population.Turn} node={w.Nodes.Current(w.PlayerSlot)?.Id} territory={w.TerritoryCounts[w.PlayerSlot]} powered={w.Relations.Powered[w.PlayerSlot]} view={Get("_statusGlobe")} marker={Call("WandererLocationVisible",new object[]{Vector2.Zero})}");Set("_tutorialAttackGuide",false);}
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){base.Draw(t);var p=GraphicsDevice.PresentationParameters;var data=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(data);using var tex=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);tex.SetData(data);using var f=File.Create(Path.Combine(AppContext.BaseDirectory,"resume-location.png"));tex.SaveAsPng(f,tex.Width,tex.Height);Exit();}
}
