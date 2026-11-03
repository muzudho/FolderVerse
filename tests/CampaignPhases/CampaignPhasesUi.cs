using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

sealed class CampaignPhasesUi:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,a,b,turn;WorldSetup w=null!;
    object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
    object Call(string name,params object[] values)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,values)!;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);
        w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);
        for(int i=1;i<w.ActiveCount;i++)w.Relations.Powered[i]=false;
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
        foreach(var workshop in w.Robots.Workshops)workshop.AutoAssemblyDisabled=true;
        var source=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>1);a=source.Id;
        var neighbors=w.Routes.Neighbors(source).Select(r=>w.Nodes.At(r.Target,r.Entry)).ToArray();b=neighbors[0].Id;
        source.Owner=neighbors[0].Owner=neighbors[1].Owner=0;
        w.ConquerorLocations[0]=source.Cell;w.ConquerorPoints[0]=source.Center;
        var cargo=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[a].Add(cargo);
        w.Robots.DirectDelivery.ReserveParts(w,a,b,cargo.Id,cargo.Parts,null,0);
        var failed=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[a].Add(failed);
        w.Robots.DirectDelivery.Reserve(w,a,neighbors[1].Id,failed.Id,RobotParts.Head,null,0);
        neighbors[1].Owner=1;
        ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_lastCutInPhase","");turn=w.Population.Turn;
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time)
    {
        if(frame==1){
            Call("EndPlannedTurn");
            Check(Get("_screen").ToString()=="Transport" && w.Campaign.Phase==CampaignPhase.Delivery,"Domestic end opens delivery playback");
            Check(w.Robots.Nodes[b].Robots.Single().Parts==RobotParts.Complete && w.Population.Turn==turn,"Delivery completes before military without consuming global time");
            Check(w.Robots.DirectDelivery.Failures.Count==1,"Hostile destination leaves a failed shipping label");
        }
        if(frame==2){
            Set("_lastCutInPhase","配送フェーズ");Set("_cutInStarted",0d);
            for(int i=0;i<50 && w.Campaign.Phase==CampaignPhase.Delivery;i++)Call("UpdateTransport",10d,false,100d);
            Check(w.Campaign.Phase==CampaignPhase.Military && Get("_screen").ToString()=="WorldStatus","Delivery animation returns to military map");
            Check((bool)Call("PhasePrimaryActionAllowed",2) && !(bool)Call("PhasePrimaryActionAllowed",3),"Military map enables war and disables shipping");
            Call("OpenNodeInspector",a);Call("OpenNodeAssembly");Call("UpdateDomesticSlide",1f);
            var stock=w.Robots.Nodes[a].Robots.ToArray();Set("_pointer",new Point(90,870));
            Call("NodeAssemblyLocalInput",new MouseState(),true,false);
            Check(stock.SequenceEqual(w.Robots.Nodes[a].Robots),"Military assembly click preserves inventory");
        }
        if(frame==3){Call("ResetInspector");Call("OpenWarPlanning");Check((bool)Get("_movementOpen"),"Military war button opens attack planner");}
        if(frame==4){
            Call("EndPlannedTurn");
            Check(w.Population.Turn==turn+1 && w.Campaign.Phase==CampaignPhase.Domestic && w.Industry.Pending,"Military end advances one turn and queues industry");
            Check(w.Robots.Nodes[b].Count==1,"Military completion preserves delivered stock");
        }
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame*10),TimeSpan.Zero));
        var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);
        using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"campaign-phase-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
        if(frame++==4){Console.WriteLine("PASS: rendered domestic, delivery and military flow, failed shipping label, military inventory protection and turn completion.");Exit();}
    }
}
