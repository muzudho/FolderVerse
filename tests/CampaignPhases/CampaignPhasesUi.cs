using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using System.Collections;

sealed class CampaignPhasesUi:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,a,b,c,turn;WorldSetup w=null!;
    object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
    object Call(string name,params object[] values)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,values)!;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    void Click(Point p)
    {
        var canvas=(Rectangle)Call("CanvasBounds");
        foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})
            Call("HandleInput",new MouseState(canvas.X+p.X*canvas.Width/1920,canvas.Y+p.Y*canvas.Height/1080,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);
    }
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);
        w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);w.HumanPlayers.Add(0);
        for(int i=1;i<w.ActiveCount;i++)w.Relations.Powered[i]=false;
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
        w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
        foreach(var workshop in w.Robots.Workshops)workshop.AutoAssemblyDisabled=true;
        var source=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);a=source.Id;
        var route=w.Routes.Neighbors(source)[0];var recipient=w.Nodes.At(route.Target,route.Entry);b=recipient.Id;
        c=w.Routes.Neighbors(recipient).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).First(n=>n!=a);
        source.Owner=recipient.Owner=0;w.Nodes.All[c].Owner=1;
        w.ConquerorLocations[0]=source.Cell;w.ConquerorPoints[0]=source.Center;
        var cargo=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[a].Add(cargo);
        w.Robots.DirectDelivery.ReserveParts(w,a,b,cargo.Id,cargo.Parts,null,0);
        ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);turn=w.Population.Turn;
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time)
    {
        if(frame==0){
            var war=(Rectangle)typeof(Game1).GetProperty("WarButton",F)!.GetValue(this)!;Click(war.Center);Call("UpdateDomesticSlide",1f);
            Check((bool)Get("_warPlanning"),"Normal war button opens planner during domestic");
            Call("SelectMoveNode",c);var rows=(IList)Get("_invasionSources");
            int row=Enumerable.Range(0,rows.Count).First(i=>(int)rows[i]!.GetType().GetField("Node")!.GetValue(rows[i])! == b && !(bool)rows[i]!.GetType().GetField("Queen")!.GetValue(rows[i])!);
            Call("OpenInvasionPicker",row);
            Check(w.Campaign.DeliverySortieRobots(w,0,b).Single().Id<0,"Picker offers future arrival, not existing stock");
        }
        if(frame==1){
            Click(new(440,390));Click(new(1400,820));
            Check(w.Campaign.PlannedOrders.Single().DeliveryMembers!.Single().DeliveryNumbers.Length==3,"Real picker input reserves arrival dependencies");
        }
        if(frame==2){
            Call("EndPlannedTurn");
            Check(Get("_screen").ToString()=="Transport" && w.Campaign.Phase==CampaignPhase.Delivery && w.Population.Turn==turn,"One domestic submit starts delivery");
            Check(w.Robots.Nodes[b].Robots.Single().Parts==RobotParts.Complete,"Delivery assembles selected arrival");
        }
        if(frame==3){
            Set("_lastCutInPhase","配送フェーズ");Set("_cutInStarted",0d);
            for(int i=0;i<100 && w.Campaign.Phase==CampaignPhase.Delivery;i++)Call("UpdateTransport",10d,false,100d);
            Check(w.Campaign.Phase==CampaignPhase.Domestic && w.Population.Turn==turn+1 && Get("_screen").ToString()=="Transport","Delivery proceeds to sortie without second human turn");
            Check(w.Campaign.RobotBattles.LastMarches.Any(m=>m.Source==b && m.Target==c),"Forecast army actually departs");
        }
        if(frame==4){
            Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);
            w.Campaign.LastCancelledWars.Add(new(turn,0,c,"直送便 #1 が未到着（空き枠待機） / 開戦予定全体を中止"));
            Call("ResetInspector");Call("OpenWarPlanning");Call("UpdateDomesticSlide",1f);Click(new(730,198));
            Check((bool)Get("_warCancellationsOpen"),"History button opens through normal input");
        }
        if(frame==5){var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
        if(frame==6){Click(new(1510,850));Check(!(bool)Get("_warCancellationsOpen"),"Scaled normal input closes history");}
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame*10),TimeSpan.Zero));
        var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);
        using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"campaign-phase-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
        if(frame++==6){Console.WriteLine("PASS: real arrival picker input, single-turn delivery then sortie, previous-turn history input and 1080p/540p rendering.");Exit();}
    }
}
