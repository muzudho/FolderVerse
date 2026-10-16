using FolderVerse;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using var game=new CheckGame();game.Run();
sealed class CheckGame:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        var w=(WorldSetup)Get("_setup");w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
        var net=(CubeNet)Get("_propagationNet");net.SetCenter(w,0);((CubeNet)Get("_net")).SetCenter(w,0);
        var route=w.Nodes.All.SelectMany(n=>w.Routes.Neighbors(n).Select(r=>(From:n,To:w.Nodes.At(r.Target,r.Entry))))
            .First(p=>!net.FacesJoined(w.Cells[p.From.Cell].Face,w.Cells[p.To.Cell].Face));
        w.Campaign.RobotBattles.LastMarches.Add((0,route.From.Id,route.To.Id));Call("BeginPropagation");
        Check((bool)Get("_propagationMovingFaces"),"Cross-face propagation needs attachment");
        Call("UpdatePropagationMovement",.85d);Check((bool)Get("_propagationCentering"),"Centering stage follows attachment");
        var from=(Vector2)Get("_propagationFromPan");var to=(Vector2)Get("_propagationToPan");
        float fromScale=(float)Get("_propagationFromScale"),toScale=(float)Get("_propagationToScale");
        Check(Vector2.Distance(from,to)>.01f || Math.Abs(fromScale-toScale)>.01f,"Fixture actually requires camera correction");
        Check(Vector2.Distance(net.Pan,from)<.001f,"Attachment end does not instantly center");
        Call("UpdatePropagationMovement",.3d);Check(Vector2.Distance(net.Pan,Vector2.Lerp(from,to,.5f))<.001f,"Halfway centering interpolates pan");
        Check(Math.Abs(net.Fit(new Rectangle(40,100,1840,860)).Scale-(fromScale+toScale)/2)<.001f,"Halfway camera scale stays continuous too");
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time)
    {
        var net=(CubeNet)Get("_propagationNet");var batch=(SpriteBatch)Get("_spriteBatch");var before=net.Pan;
        GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawPropagation",-1,0f,true);batch.End();
        Check(Vector2.Distance(net.Pan,before)<.001f,"Draw preserves interpolated camera");
        Call("UpdatePropagationMovement",.3d);Check(!(bool)Get("_propagationCentering"),"Centering finishes on schedule");
        Call("UpdatePropagationMovement",20d);Check((bool)typeof(Game1).GetProperty("PropagationComplete",F)!.GetValue(this)!,"Large elapsed frame finishes attachment, centering and travel");
        Console.WriteLine("PASS: attachment then smooth centering, midpoint, drawing continuity and low-FPS propagation completion.");Exit();
    }
}
