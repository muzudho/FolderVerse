using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

sealed class SubordinateUi:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,a,b,own,first;WorldSetup w=null!;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    Dictionary<int,HashSet<int>> Checks()=>(Dictionary<int,HashSet<int>>)Get("_nodeChecksByPlayer");
    void Ready(bool detailed,bool twoHumans=true,bool checkedB=true){
        Call("ResetInspector");Call("ClearAllNodeChecks");Set("_computerDomesticNodes",null!);Set("_subordinateNodes",null!);
        w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);
        w.HumanPlayers.Clear();w.HumanPlayers.Add(0);if(twoHumans)w.HumanPlayers.Add(2);w.Relations.SetSuperior(1,0);
        foreach(var n in w.Nodes.All)n.Owner=0;
        a=w.Nodes.All.First(n=>w.Routes.Land(n.Cell,n.Center)).Id;b=w.Nodes.All.First(n=>n.Id!=a).Id;own=w.Nodes.All.First(n=>n.Id!=a && n.Id!=b).Id;
        w.Nodes.All[a].Owner=w.Nodes.All[b].Owner=1;
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
        foreach(var shop in w.Robots.Workshops){shop.SelectPlan(3);shop.AutoAssemblyDisabled=true;}
        foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})w.Robots.Nodes[a].Add(w.Robots.Create(1,part));
        for(int r=0;r<w.ActiveCount;r++)if(w.ShowComputerOperations(r))w.ToggleComputerOperations(r);
        if(detailed)w.ToggleComputerOperations(1);
        Set("_autoPlay",false);Set("_demoActive",false);Set("_conquestOverview",false);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
        Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);Set("_tutorialAttackGuide",false);
        ((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
        Call("EnsureNodeCheckContext");Checks()[0]=checkedB?new(){b}:new();Checks()[2]=new(){own};
    }
    protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);}
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time){
        if(frame==0){
            Ready(false);Call("EndDomesticPhase",false);
            Check(((int[])Get("_subordinateNodes")).SequenceEqual(new[]{a}) && w.PlayerSlot==1,"Unchecked subordinate begins its own view even when operations display is off");
            Call("UpdateSubordinateDomestic",.3,false);
            Check((bool)Call("NodeChecked",a) && Checks()[0].Contains(b) && w.Campaign.SubordinateResults.Count==1,"Compact scene marks the operated point without removing existing checks");
            Check(w.Robots.Workshops[b].SelectedPlan==3 && w.Robots.Workshops[own].SelectedPlan==3,"Checked and directly owned points retain their settings");
        }
        if(frame==1){
            Call("UpdateSubordinateDomestic",.5,false);
            Check(Get("_subordinateNodes")==null && w.TurnPlayer==2 && w.Campaign.Phase==CampaignPhase.Domestic,"Supplement completes before switching to the next human's domestic turn");
            Check(!Checks().ContainsKey(0) && Checks()[2].Contains(own),"Only the submitted controller's checks are cleared");
        }
        if(frame==2){Ready(true,checkedB:false);Call("EndDomesticPhase",false);first=((int[])Get("_subordinateNodes"))[0];Call("UpdateSubordinateDomestic",.3,false);Check(!(bool)Call("NodeChecked",first),"Detailed view shows the operation before marking complete");}
        if(frame==3){Call("UpdateSubordinateDomestic",.7,false);Check((bool)Call("NodeChecked",first),"Detailed scene visibly marks its completed operation");}
        if(frame==4){
            Set("_previousKeyboard",new KeyboardState());Call("HandleInput",new MouseState(),new KeyboardState(Keys.Escape),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.FromSeconds(.05)),true);
            Check(Get("_subordinateNodes")==null && w.Campaign.SubordinateResults.Count==2 && w.TurnPlayer==2,"Physical Escape completes all remaining points exactly once and restores control");
        }
        if(frame==5){Ready(false);Checks()[0].Add(a);Call("EndDomesticPhase",false);Check(Get("_subordinateNodes")==null && w.TurnPlayer==2 && w.Campaign.SubordinateResults.Count==0,"All-checked subordinates need no scene or automatic edits");}
        if(frame==6){
            var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();
            Ready(false,twoHumans:false);Call("EndDomesticPhase",false);Call("UpdateSubordinateDomestic",.3,false);
        }
        if(frame==7){
            Call("UpdateSubordinateDomestic",0d,true);
            Check(Get("_subordinateNodes")==null && w.Campaign.Phase==CampaignPhase.Delivery && w.PlayerSlot==0,"Last controller's supplement proceeds to delivery with the original view restored");
            Check(w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Supplemented assembly actually completes during delivery");
        }
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame),TimeSpan.Zero));
        var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        using(var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"subordinate-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);}
        if(frame++==7){Console.WriteLine("PASS: display-off compact checks, detailed before/after, checked/own protection, physical Escape, multiplayer handoff, control restoration and full/half-size delivery completion.");Exit();}
    }
}
