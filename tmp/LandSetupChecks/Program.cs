using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
using var game=new CheckGame();game.Run();
class CheckGame:Game1{
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
int frame;
protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var w=(WorldSetup)Get("_setup");w.SetWorld(1);w.SetCast(2);Call("SetPlacementSeed",3);w.SelectPlayer(w.LandOrder()[0]);Call("BeginLandConquest"); Set("_landDrag",new Point(580,170));Set("_pointer",new Point(1859,949));Call("UpdateLandSetup",0d,default(Microsoft.Xna.Framework.Input.MouseState),true,false,false);var picked=(System.Collections.Generic.HashSet<int>)Get("_landSelection");if(picked.Count==0)throw new Exception("rectangle selected no cells");Set("_landDrag",new Point(580,170));Call("UpdateLandSetup",0d,default(Microsoft.Xna.Framework.Input.MouseState),true,false,false);if(picked.Count!=0)throw new Exception("rectangle toggle failed");Set("_demoActive",false);}
protected override void Update(GameTime t){if(frame>0 && frame<3)Call("UpdateLandSetup",2d,default(Microsoft.Xna.Framework.Input.MouseState),false,false,false);if(frame==3){var w=(WorldSetup)Get("_setup");foreach(int r in w.LandOrder().Where(r=>!w.Owners.Contains(r)))w.ClaimLand(r,w.Cells.Where(c=>w.Owners[c.Id]<0).Take(w.TerritoryRights[r]).Select(c=>c.Id).ToArray());Set("_screen",Enum.Parse(typeof(Game1).GetNestedType("Screen",BindingFlags.NonPublic)!,"Fortune"));Set("_landAge",2d);}if(frame==5)Call("UpdateLandSetup",5d,default(Microsoft.Xna.Framework.Input.MouseState),false,false,false);}
protected override void Draw(GameTime t){base.Draw(t);if(frame==0 || frame==3){var colors=new Microsoft.Xna.Framework.Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(colors);using var tex=new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);tex.SetData(colors);using var f=File.Create(Path.Combine(AppContext.BaseDirectory,$"land-{frame}.png"));tex.SaveAsPng(f,tex.Width,tex.Height);}if(frame++>=6){Console.WriteLine("PASS: land, fortune and domestic screens render");Exit();}}
}
