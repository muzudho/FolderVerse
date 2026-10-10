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
        for(int i=0;i<4;i++)w.Looks[i]=new ConquerorLook(new[]{1,2,3,6}[i],0);
        ((CubeNet)Get("_net")).Home(w);
        var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);
        var r=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(r.Target,r.Entry);
        w.Campaign.RobotBattles.LastMarches.Add((0,a.Id,b.Id));
        var battle=new BattleState(1){SourceNode=a.Id,TargetNode=b.Id,Kind=BattleKind.Edge};
        for(int owner=0;owner<4;owner++)
        {
            battle.AddArmy(owner,a.Id,Array.Empty<Robot>(),true);
            battle.Units.Add(new(){Id=owner+1,Owner=owner,Role=owner==0?BattleRole.Queen:BattleRole.Soldier,Position=new Point(2+owner,2+owner)});
        }
        battle.Capture(BattlePhase.Choose);battle.Capture(BattlePhase.Result);
        w.Campaign.RobotBattles.Scenes.Add(battle);Call("OpenRobotBattle");
        Check(Get("_screen").ToString()=="Transport","Missing propagation before battle");
    }
    protected override void Update(GameTime time)
    {
        if(frame<5)typeof(Game1).GetField("_transportAge",Flags)!.SetValue(this,frame==4?4.99d:frame*.9d);
        if(frame==5)Call("UpdateTransport",5d,false);
        if(frame==5){Check(Get("_screen").ToString()=="Battle","Propagation did not auto-enter battle");}
        if(frame==23)
        {
            typeof(Game1).GetField("_robotPlaying",Flags)!.SetValue(this,false);
            typeof(Game1).GetField("_newspaperClock",Flags)!.SetValue(this,0d);
            typeof(Game1).GetField("_newspaperEdition",Flags)!.SetValue(this,0);
            Call("RobotBattleInput",10d,false,false);Check((int)Get("_newspaperEdition")==1,"Automatic newspaper switch failed");
            typeof(Game1).GetField("_pointer",Flags)!.SetValue(this,new Point(1200,760));
            Call("RobotBattleInput",0d,true,false);Check((int)Get("_newspaperEdition")==2,"Manual newspaper switch failed");
            var renderer=Get("_newspaperPortraits");
            var textures=(Dictionary<string,Texture2D>)renderer.GetType().GetField("_photos",Flags)!.GetValue(renderer)!;
            Check(textures.Count>0,"No newspaper photo loaded");
            foreach(var texture in textures.Values){var colors=new Color[texture.Width*texture.Height];texture.GetData(colors);Check(colors.All(c=>c.R==c.G && c.G==c.B),"Newspaper photo is not monochrome");}
            Console.WriteLine("PASS: propagation, 2/3/4 columns, six fixed publishers, monochrome pixels, missing photos, automatic/manual newspaper switching.");Exit();
        }
    }
    protected override void Draw(GameTime time)
    {
        if(frame>=5 && frame<23)
        {
            typeof(Game1).GetField("_newspaperEdition",Flags)!.SetValue(this,(frame-5)%NewspaperProfiles.All.Length);
            if(frame is 11 or 17){var scenes=(BattleState[])Get("_robotScenes");scenes[0].Armies.RemoveAt(scenes[0].Armies.Count-1);}
        }
        base.Draw(time);
        if(frame==4)
        {
            var at=(Vector2)Call("PropagationBadgeCenter",0,new Rectangle(40,100,1840,860));
            Check(Vector2.Distance(at,new Vector2(540,540))<.1f,"Zoom badge does not align with battle center");
            var net=(CubeNet)Get("_propagationNet");var panel=new Rectangle(40,100,1840,860);float zoomed=net.Fit(panel).Scale;net.FitAll(panel);
            Check(Math.Abs(zoomed/net.Fit(panel).Scale-1000f/30)<.01f,"Zoom badge does not match 1000-pixel battle size");
        }
        if(frame==2 || frame>=5 && frame<23){var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);using var tex=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);tex.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,frame>=5?$"newspaper-{4-(frame-5)/NewspaperProfiles.All.Length}-{(frame-5)%NewspaperProfiles.All.Length}.png":"propagation.png"));tex.SaveAsPng(output,tex.Width,tex.Height);}
        frame++;
    }
}
