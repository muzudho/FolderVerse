using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
sealed class TacticPreview:Game1
{
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var batch=(SpriteBatch)typeof(Game1).GetField("_spriteBatch",flags)!.GetValue(this)!;
        using var output=new RenderTarget2D(GraphicsDevice,1920,1080);
        GraphicsDevice.SetRenderTarget(output);GraphicsDevice.Clear(new Color(18,38,48));batch.Begin();
        var ui=(UiPainter)typeof(Game1).GetField("_ui",flags)!.GetValue(this)!;
        foreach(bool sea in new[]{false,true})for(int grade=0;grade<3;grade++){
            int x=80+grade*300,y=sea?420:80;
            ui.Text($"{(sea?"海":"陸")} / {4*(grade+1)}",new(x,y),.6f,Color.White);
            for(int i=0;i<4*(grade+1);i++)BattleFortificationArt.Draw(ui,new(x+i%4*55,y+50+i/4*60,52,52),new(new(i%4,i/4),FortificationKind.Wall,0,BattleFacing.North),1,sea);
        }
        typeof(Game1).GetMethod("DrawAmbushCardPanel",flags)!.Invoke(this,new object[]{new Rectangle(420,910,540,150),8L});batch.End();
        GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,"site-defense-tactics.png"));output.SaveAsPng(file,1920,1080);
        Console.WriteLine("PASS actual card and land/sea wall rendering");Exit();
    }
}
