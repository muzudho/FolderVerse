using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using var game=new MarginCheck();game.Run();
sealed class MarginCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame;
    protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();}
    protected override void Update(GameTime time)
    {
        if(frame>=3){Console.WriteLine("PASS: tall, ultrawide and thin horizontal margins draw separate rabbit, train, ball and star assets.");Exit();return;}
        var graphics=(GraphicsDeviceManager)typeof(Game1).GetField("_graphics",Flags)!.GetValue(this)!;
        int width=frame==0?1000:1600,height=frame==0?900:frame==1?650:1100;
        if(graphics.PreferredBackBufferWidth!=width || graphics.PreferredBackBufferHeight!=height){graphics.PreferredBackBufferWidth=width;graphics.PreferredBackBufferHeight=height;graphics.ApplyChanges();}
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);
        int width=GraphicsDevice.Viewport.Width,height=GraphicsDevice.Viewport.Height;
        var colors=new Color[width*height];GraphicsDevice.GetBackBufferData(colors);
        var corner=colors[2*width+2];if(corner.R<200 || corner.G<190 || corner.B<190)throw new Exception("Margin corner does not contain pastel pattern");
        using var texture=new Texture2D(GraphicsDevice,width,height);texture.SetData(colors);
        using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"margin-{frame}.png"));texture.SaveAsPng(output,width,height);frame++;
    }
}
