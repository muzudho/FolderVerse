using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

using var game=new FactoryNoRepeatCheck();game.Run();
sealed class FactoryNoRepeatCheck:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,node;WorldSetup w=null!;RobotWorkshop factory=null!;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Click(Point p){
        var canvas=(Rectangle)Call("CanvasBounds");
        foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})
            Call("HandleInput",new MouseState(canvas.X+p.X*canvas.Width/1920,canvas.Y+p.Y*canvas.Height/1080,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);
    }
    void Tick(int count){for(int i=0;i<count;i++)w.Robots.AdvanceIndustry(n=>n==node?0:-1);}
    protected override void LoadContent(){
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);
        w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);w.HumanPlayers.Add(0);
        node=w.Nodes.All.First(n=>n.Functional && n.Terrain!=TravelTerrain.Sea).Id;w.Nodes.All[node].Owner=0;
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);factory=w.Robots.Workshops[node];
        for(int seed=0;factory.ProductionLines.Length!=3;seed++)factory.GenerateLines(seed);
        factory.Automation=NodeAutomation.Manual;factory.AutoAssemblyDisabled=true;
        foreach(var line in factory.ProductionLines){line.Configure(RobotParts.Head,6);line.SetRest(1,1);}
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);
        ((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
        Call("OpenNodeInspector",node);Call("UpdateDomesticSlide",1f);Call("OpenNodeFactory");
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time){
        Set("_factoryLine",0);var toggle=(Rectangle)Call("FactoryNoRepeatButton");var rest=(Rectangle)Call("FactoryDropButton",1);
        Check(toggle.Left>rest.Right && toggle.Right<904,"Checkbox is right of rest dropdown and inside inspector");
        if(frame==1){Click(toggle.Center);Check(factory.DoNotRepeat && !factory.ProductionLines[1].DoNotRepeat,"Normal input toggles only selected lane");Tick(6);Check(factory.RestRemaining==3 && !factory.CycleFinished,"Production ends in rest, not immediate stop");}
        if(frame==2){Tick(3);Check(factory.CycleFinished && factory.LaneProgress==9,"Final rest reaches right endpoint");}
        if(frame==3){int before=w.Robots.ProductionIntakes.Count;Tick(6);Check(w.Robots.ProductionIntakes.Count==before+2,"Other two lanes keep manufacturing while first is stopped");var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
        if(frame==4){Click(toggle.Center);Check(!factory.DoNotRepeat && !factory.CycleFinished,"Scaled input unchecks and resumes lane");Tick(1);}
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame*10),TimeSpan.Zero));
        var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        if(frame==2){var canvas=(Rectangle)Call("CanvasBounds");int x=canvas.X+376*canvas.Width/1920,y=canvas.Y+344*canvas.Height/1080;Check(pixels[y*pp.BackBufferWidth+x]==new Color(45,57,63),"Finished fluorescent tube is dark at exact right endpoint");}
        using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);
        using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"factory-no-repeat-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
        if(frame++==4){Console.WriteLine("PASS: normal checkbox input, rest then stop, right-end dark tube pixels, independent lanes and 1080p/540p restart.");Exit();}
    }
}
