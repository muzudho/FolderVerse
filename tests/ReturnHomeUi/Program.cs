using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new ReturnGame();game.Run();
sealed class ReturnGame:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
    object Call(string name,params object[] values)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,values)!;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Click(Point at){Set("_pointer",at);Call("NodeAssemblyLocalInput",new MouseState(),true,false);}
    void Shot(string name){
        var batch=(SpriteBatch)Get("_spriteBatch");GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawNodeAssembly");batch.End();
        var colors=new Color[1920*1080];GraphicsDevice.GetBackBufferData(colors);
        using var texture=new Texture2D(GraphicsDevice,1920,1080);texture.SetData(colors);
        using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name+".png"));texture.SaveAsPng(file,1920,1080);
    }
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        var w=(WorldSetup)Get("_setup");w.SetParticipantCounts(1,3);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
        int home=w.Nodes.Current(0).Id;foreach(var n in w.Nodes.All)n.Owner=-1;w.Nodes.All[home].Owner=0;
        var robot=w.Robots.Create(1,RobotParts.Complete) with {HeadOrigin=1,BodyOrigin=1,LegsOrigin=2};
        w.Robots.Nodes[home].Add(robot);w.Robots.Nodes[home].Add(w.Robots.Create(0,RobotParts.Complete));
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverviewEnabled",false);Set("_autoSaveEnabled",false);
        Call("OpenNodeInspector",home);Set("_domesticSlide",1f);Call("OpenNodeAssembly");
        var button=(Rectangle)typeof(Game1).GetField("HangarReturnHomeButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
        Click(button.Center);Click(new Point(175,380));
        Check(w.Robots.Nodes[home].Robots.Single(r=>r.Id==robot.Id).ReturnHomeCommander==0,"Actual button and cell clicks create a return reservation");
        Check(Get("_hangarMessage").ToString()==RobotWorld.NoHomeCountry,"No-country reason is visible immediately");Shot("return-home-no-country");
        Click(new Point(175,380));Check(w.Robots.Nodes[home].Robots.Single(r=>r.Id==robot.Id).ReturnHomeCommander==null,"Second click cancels the pending reservation");
        Click(new Point(370,380));Check(w.Robots.Nodes[home].Robots.Single(r=>r.Owner==0).ReturnHomeCommander==null,"Domestic robot is not accidentally reserved");
        var route=w.Routes.Neighbors(w.Nodes.All[home]).First();int target=w.Nodes.At(route.Target,route.Entry).Id;w.Nodes.All[target].Owner=1;
        Click(new Point(175,380));Shot("return-home-reserved");
        typeof(ConquestCampaign).GetMethod("BeginDelivery",F)!.Invoke(w.Campaign,new object[]{w});
        Check(w.Robots.Nodes[home].Robots.All(r=>r.Id!=robot.Id) && w.Robots.Nodes[target].Robots.Any(r=>r.Id==robot.Id && r.Owner==1),"Real delivery phase executes return reservation");
        Check(w.Robots.Transport.LastTransfers.Any(s=>s.RobotId==robot.Id && s.Source==home && s.Target==target),"Return journey participates in delivery playback");
        Console.WriteLine("PASS: actual return button, exact robot selection, cancellation, domestic exclusion, visible missing-country reason and delivery integration.");Exit();
    }
    protected override void Update(GameTime time){}
}
