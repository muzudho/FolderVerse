using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game = new CheckGame();
game.Run();
class CheckGame : Game1
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,Flags)!.GetValue(this)!;
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,Flags)!.Invoke(this,a)!;
 protected override void LoadContent()
 {
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");
  base.LoadContent();
  var mobs=(Texture2D[])Get("_cutInMobs");
  if(mobs.Length!=4)throw new Exception("Not all four mobs loaded");
  foreach(var t in mobs)
  {
   var pixels=new Color[t.Width*t.Height];t.GetData(pixels);
   if(pixels[0].A!=0 || pixels[^1].A!=0)throw new Exception("Backdrop remains");
   var center=pixels[(int)(t.Height*.45)*t.Width+t.Width/2];
   if(center.A!=255)throw new Exception("White clothing became transparent");
  }
  for(int i=0;i<100;i++){int old=(int)Get("_cutInMob");Call("ChooseCutInMob");if((int)Get("_cutInMob")==old)throw new Exception("Consecutive repeated mob");}
  Console.WriteLine("PASS: four sprites, transparent exterior, opaque white clothing, no immediate repeat");
 }
 protected override void Draw(GameTime time)
 {
  var screen=typeof(Game1).GetField("_screen",Flags)!;
  var batch=(SpriteBatch)Get("_spriteBatch");
  int n=0;
  foreach(var phase in new[]{"WorldStatus","Transport","Battle"})
  {
   GraphicsDevice.Clear(new Color(82,122,145));
   screen.SetValue(this,Enum.Parse(screen.FieldType,phase));
   batch.Begin();Call("DrawPhaseCutIn",100d+n*3);Call("DrawPhaseCutIn",100.6d+n*3);batch.End();
   var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);
   using var tex=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);tex.SetData(pixels);
   using var o=File.Create(Path.Combine(AppContext.BaseDirectory,phase+".png"));tex.SaveAsPng(o,tex.Width,tex.Height);
   n++;
  }
  Console.WriteLine("PASS: all three cut-ins rendered");Exit();
 }
}