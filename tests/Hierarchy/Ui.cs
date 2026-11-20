using FolderVerse;using System.Reflection;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;
using var game=new Preview();game.Run();
sealed class Preview:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;SpriteBatch batch=null!;int frame;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 protected override void LoadContent(){
 var runtime=Environment.GetEnvironmentVariable("HIERARCHY_RUNTIME")!;AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",runtime);Content.RootDirectory=Path.Combine(runtime,"Content");
 var root=AutoSaveStore.Read<object>(Environment.GetEnvironmentVariable("HIERARCHY_SAVE")!);var w=(WorldSetup)root.GetType().GetProperty("World")!.GetValue(root)!;Set("_setup",w);
 batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});var font=Content.Load<SpriteFont>("UiFont");
 Set("_spriteBatch",batch);Set("_pixel",pixel);Set("_font",font);Set("_ui",new UiPainter(batch,pixel,font));Set("_portraitRenderer",new PortraitRenderer(Content));
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){GraphicsDevice.Clear(new Color(16,33,44));batch.Begin();typeof(Game1).GetMethod("DrawHierarchyUi",F)!.Invoke(this,null);batch.End();
 var pp=GraphicsDevice.PresentationParameters;var data=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(data);using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(data);using(var f=File.Create(Path.Combine(Environment.GetEnvironmentVariable("HIERARCHY_OUTPUT")!,"hierarchy.png")))tex.SaveAsPng(f,tex.Width,tex.Height);if(frame++==1)Exit();}
}
