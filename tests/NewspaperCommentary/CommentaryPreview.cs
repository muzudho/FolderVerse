using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
sealed class CommentaryPreview:Game1
{
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
        void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
        var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);Set("_setup",w);
        Set("_turnPapers",new[]{new TurnNewsArticle(0,"昨日の出来事","わが国の支配地点が変化",new[]{"獲得 2 地点 ／ 喪失 3 地点","獲得：海岸の工場、北の港","喪失：南の工場、谷の採取場、東の中継地点","地点の増減です。地域数や、戦闘だけの成果とは異なります。"},2,3),new TurnNewsArticle(0,"講評","わが国の動きを振り返る",new[]{"確認：戦場で味方側が1勝。一方、前回所有の3地点が他国へ。","講評：局地の勝利と、国全体の支配を保つことは別の成果です。","仮説：次の補給路も確保できれば、勝利を広げられるかもしれません。"})});Set("_turnPaperIndex",1);Set("_paperAge",3d);
        var batch=(SpriteBatch)typeof(Game1).GetField("_spriteBatch",F)!.GetValue(this)!;
        foreach(var size in new[]{new Point(1920,1080),new Point(1366,768)}){
            using var target=new RenderTarget2D(GraphicsDevice,size.X,size.Y);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(Color.Black);
            batch.Begin(transformMatrix:Matrix.CreateScale(size.X/1920f,size.Y/1080f,1));typeof(Game1).GetMethod("DrawTurnNewspaper",F)!.Invoke(this,null);batch.End();GraphicsDevice.SetRenderTarget(null);
            using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"commentary-{size.X}.png"));target.SaveAsPng(file,size.X,size.Y);
        }
        Console.WriteLine("PASS commentary paper rendering at 1920 and 1366");Exit();
    }
}
