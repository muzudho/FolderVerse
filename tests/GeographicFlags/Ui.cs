using FolderVerse;using System.Reflection;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;
using var game=new Preview();game.Run();
sealed class Preview:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;SpriteBatch batch=null!;UiPainter painter=null!;WorldSetup world=null!;int frame;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 protected override void LoadContent(){
 var runtime=Environment.GetEnvironmentVariable("HIERARCHY_RUNTIME")!;AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",runtime);Content.RootDirectory=Path.Combine(runtime,"Content");
 var root=AutoSaveStore.Read<object>(Environment.GetEnvironmentVariable("HIERARCHY_SAVE")!);var w=(WorldSetup)root.GetType().GetProperty("World")!.GetValue(root)!;Set("_setup",w);world=w;w.Relations.Reset();w.Relations.SetSuperior(1,0);w.Relations.SetSuperior(2,1);
 batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});var font=Content.Load<SpriteFont>("UiFont");
 Set("_spriteBatch",batch);Set("_pixel",pixel);Set("_font",font);painter=new UiPainter(batch,pixel,font);Set("_ui",painter);Set("_portraitRenderer",new PortraitRenderer(Content));
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){GraphicsDevice.Clear(new Color(16,33,44));batch.Begin();for(int a=0;a<4;a++)for(int b=0;b<4;b++){world.SetCountryFlagShape(0,a);world.SetCountryFlagShape(2,b);CountryFlagArt.DrawGeographic(painter,world,2,new Rectangle(80+b*430,80+a*230,330,190));CountryFlagArt.Marker(painter,world,2,new Vector2(440+b*430,230+a*230),36,geographic:true);}batch.End();
 var pp=GraphicsDevice.PresentationParameters;var data=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(data);using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(data);using(var f=File.Create(Path.Combine(Environment.GetEnvironmentVariable("HIERARCHY_OUTPUT")!,"flags.png")))tex.SaveAsPng(f,tex.Width,tex.Height);if(frame++==1)Exit();}
}

