using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
sealed class CrossfirePreview:Game1
{
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
        var batch=(SpriteBatch)typeof(Game1).GetField("_spriteBatch",F)!.GetValue(this)!;
        foreach(var size in new[]{new Point(1920,1080),new Point(1366,768)}){
            using var target=new RenderTarget2D(GraphicsDevice,size.X,size.Y);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(18,38,48));
            batch.Begin(transformMatrix:Matrix.CreateScale(size.X/1920f,size.Y/1080f,1));
            typeof(Game1).GetMethod("DrawLessonCard",F)!.Invoke(this,new object[]{BattleLesson.CrossfireSafety,new Rectangle(80,80,660,800),true,1f});
            typeof(Game1).GetMethod("DrawAmbushCardPanel",F)!.Invoke(this,new object[]{new Rectangle(1000,600,700,150),1L,true});
            batch.End();GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"crossfire-{size.X}.png"));target.SaveAsPng(file,size.X,size.Y);
        }
        Console.WriteLine("PASS tactic and lesson18 rendering at 1920 and 1366");Exit();
    }
}
