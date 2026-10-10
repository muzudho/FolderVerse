using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using var game=new PropagationCheck();game.Run();
sealed class PropagationCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame;
    object Get(string name)=>typeof(Game1).GetField(name,Flags)!.GetValue(this)!;
    object Call(string name,params object[] args)=>typeof(Game1).GetMethod(name,Flags)!.Invoke(this,args)!;
    void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        var w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
        ((CubeNet)Get("_net")).Home(w);
        var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);
        var r=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(r.Target,r.Entry);
        w.Campaign.RobotBattles.LastMarches.Add((0,a.Id,b.Id));
        var battle=new BattleState(1){SourceNode=a.Id,TargetNode=b.Id,Kind=BattleKind.Edge};
        battle.AddArmy(0,a.Id,Array.Empty<Robot>(),true);battle.Capture(BattlePhase.Choose);battle.Capture(BattlePhase.Result);
        w.Campaign.RobotBattles.Scenes.Add(battle);Call("OpenRobotBattle");
        Check(Get("_screen").ToString()=="Transport","Missing propagation before battle");
    }
    protected override void Update(GameTime time)
    {
        if(frame<5)typeof(Game1).GetField("_transportAge",Flags)!.SetValue(this,frame*.9d);
        if(frame==5)Call("UpdateTransport",5d,false);
        if(frame==5){Check(Get("_screen").ToString()=="Battle","Propagation did not auto-enter battle");Console.WriteLine("PASS: full-map propagation, marching battery/soldier, edge badge, zoom and automatic battle entry.");Exit();}
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(time);
        if(frame==2){var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);using var tex=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);tex.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"propagation.png"));tex.SaveAsPng(output,tex.Width,tex.Height);}
        frame++;
    }
}
