using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CheckGame();game.Run();
class CheckGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
 void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
 object Call(string name,params object[] args)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,args)!;
 protected override void LoadContent()
 {
  var setup=(WorldSetup)Get("_setup");var net=(CubeNet)Get("_net");var world=new WorldPreview(GraphicsDevice);Set("_world",world);
  int checks=0;
  for(int seed=0;seed<10;seed++)
  {
   setup.SetWorld(seed);setup.SetCast(seed);setup.SetPlacement(seed);setup.SelectPlayer(0);world.ShowSetup(setup,true);
   foreach(var source in setup.Nodes.All.Take(10))foreach(bool globe in new[]{false,true})
   {
    Set("_movementOpen",false);Set("_populationCell",source.Cell);Set("_populationNode",source.Id);Set("_nodeTransportOpen",true);Set("_statusGlobe",globe);
    Call("FitDestinationView");var ids=(int[])Call("DestinationNodes");
    var area=(bool)Get("_statusGlobe")?(Rectangle)typeof(Game1).GetProperty("StatusGlobeArea",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(this)!:new Rectangle(1040,240,790,580);
    foreach(int id in ids.Append(source.Id))
    {
     object[] args={id,Vector2.Zero};bool visible=(bool)Call("OperationPosition",args);var at=(Vector2)args[1];
     if(!visible || !area.Contains(at.ToPoint()))throw new Exception($"Destination clipped seed={seed} source={source.Id} target={id}");
     if(id==source.Id && Vector2.Distance(at,new(area.Center.X,area.Center.Y))>1)throw new Exception("Source is not centered");
    }
    checks++;
   }
  }
  Console.WriteLine($"PASS: {checks} source/shape/view combinations; every destination visible, source centered");
  Set("_spriteBatch",new SpriteBatch(GraphicsDevice));
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t)
 {
  GraphicsDevice.Clear(new Color(238,225,199));var batch=(SpriteBatch)Get("_spriteBatch");batch.Begin();
  for(int edition=0;edition<7;edition++){Set("_newspaperEdition",edition);for(int owner=0;owner<20;owner++)Call("DrawNewspaperToy",owner,new Vector2(75+owner%10*170,70+(owner/10+edition*2)*68),54);}
  batch.End();var colors=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(colors);
  using var tex=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);tex.SetData(colors);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"scattered-toys.png"));tex.SaveAsPng(output,tex.Width,tex.Height);
  Console.WriteLine("PASS: seven editions of scattered markers rendered");Exit();
 }
}
