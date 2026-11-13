using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

sealed class FunctionTransferUi:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,a,b;WorldSetup w=null!;
    object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
    object Call(string name,params object[] values)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,values)!;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);
        w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);
        foreach(var n in w.Nodes.All)n.Owner=0;
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
        var source=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>1);a=source.Id;var route=w.Routes.Neighbors(source)[0];b=w.Nodes.At(route.Target,route.Entry).Id;
        foreach(int node in new[]{a,b}){
            var factory=w.Robots.Workshops[node];factory.LimitLines(3);
            for(int seed=0;factory.ProductionLines.Length!=3;seed++)factory.GenerateLines(seed);
            foreach(var line in factory.ProductionLines)line.SelectPlan(3);factory.AutoAssemblyDisabled=true;
            for(int i=0;i<12;i++)w.Robots.Nodes[node].Add(w.Robots.Create(0,RobotParts.Complete));
        }
        ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Set("_statusGlobe",false);
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("OpenNodeInspector",a);Call("OpenNodeOffice");Call("UpdateDomesticSlide",1f);
    }
    private void CheckMapReservation()
    {
        Call("ResetInspector");Call("UpdateDomesticSlide",1f);
        var net=(CubeNet)Get("_net");net.ShowRoutes=false;
        var button=(Rectangle)Call("PrimaryActionBounds",6);
        Check((bool)Call("MapOverlayClick",button.Center,false) && (bool)Get("_functionTransferMap"),"Primary action opens reservation mode");
        Call("UpdateMapFooter",.25f);
        Check((Rectangle)Call("PrimaryActionBounds",6)==button &&
            Enumerable.Range(0,6).All(i=>((Rectangle)Call("PrimaryActionBounds",i)).Top>=1080),
            "Function transfer stays in place as the other actions slide offscreen");
        Call("BuildFunctionTransferMarkers");

        var markers=(System.Collections.IList)Get("_functionTransferMarkers");
        (int Source,int Target,Rectangle Bounds,bool Reserved)[] ReadMarkers()=>markers.Cast<object>().Select(m=>{
            var type=m.GetType();
            return ((int)type.GetProperty("Source")!.GetValue(m)!,(int)type.GetProperty("Target")!.GetValue(m)!,
                (Rectangle)type.GetProperty("Bounds")!.GetValue(m)!,(bool)type.GetProperty("Reserved")!.GetValue(m)!);
        }).ToArray();
        var candidates=ReadMarkers();
        var first=candidates.First(m=>candidates.Any(n=>n.Source==m.Target && n.Target==m.Source && !n.Bounds.Intersects(m.Bounds)));
        Check((bool)Call("MapOverlayClick",first.Bounds.Center,false),"Map arrow responds");
        var orders=w.Campaign.FunctionTransfers.Orders;
        Check(orders.Count==0 && Get("_transferChoice")!=null,"Arrow opens the modal before reserving");
        Check((bool)Call("MapOverlayClick",Point.Zero,true) && Get("_transferChoice")==null && orders.Count==0,"Escape cancels only the modal");
        Call("BuildFunctionTransferMarkers");
        Call("MapOverlayClick",first.Bounds.Center,false);
        Call("MapOverlayClick",new Point(960,575),false);
        Check(orders.Count==1 && orders[0].Source==first.Source && orders[0].Target==first.Target,"Arrow reserves the indicated direction");
        Check((bool)Get("_functionTransferMap"),"Reservation leaves the mode open");
        Call("BuildFunctionTransferMarkers");
        var updated=ReadMarkers();
        Check(!updated.Any(m=>!m.Reserved && (m.Source==first.Source || m.Source==first.Target)),"Reserved source and chain directions are removed");
        var second=updated.First(m=>!m.Reserved);
        w.Nodes.All[second.Source].Purpose=NodePurpose.Relay;
        Check((bool)Call("MapOverlayClick",second.Bounds.Center,false) && orders.Count==2 &&
            Get("_transferChoice")==null && !orders.Last().FactoryOnly,"Relay reserves a full transfer directly");
        w.Nodes.All[second.Source].Purpose=NodePurpose.General;
        Call("BuildFunctionTransferMarkers");
        var document=ReadMarkers().Single(m=>m.Reserved && m.Source==first.Source);
        Check((bool)Call("MapOverlayClick",document.Bounds.Center,false) && orders.Count==1 && orders[0].Source==second.Source,"Document cancels only its reservation");
        Set("_pointer",second.Bounds.Center);
    }
    private void CloseMapReservation()
    {
        var orders=w.Campaign.FunctionTransfers.Orders;
        w.Campaign.FunctionTransfers.Cancel(orders.Single().Source);
        var net=(CubeNet)Get("_net");
        var button=(Rectangle)Call("PrimaryActionBounds",6);
        var close=new Point(button.Right+30,button.Center.Y);
        Check((bool)Call("MapOverlayClick",close,false) && !(bool)Get("_functionTransferMap") && !net.ShowRoutes,
            "Close button exits the mode and restores route visibility");
        Call("UpdateMapFooter",.25f);
        Check(Enumerable.Range(0,6).All(i=>((Rectangle)Call("PrimaryActionBounds",i)).Bottom<=1080),
            "Other actions return after closing");
        Check((bool)Call("MapOverlayClick",button.Center,false) && (bool)Call("MapOverlayClick",Point.Zero,true) &&
            !(bool)Get("_functionTransferMap"),"Esc still closes the mode");
        Call("OpenNodeInspector",a);Call("OpenNodeOffice");Call("UpdateDomesticSlide",1f);
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time)
    {
        if(frame==0)CheckMapReservation();
        if(frame==1){
            CloseMapReservation();
            Check(!(bool)Call("FunctionTransferOfficeClick",new Point(200,860)),"Former office reservation button is inactive");
        }
        if(frame==2){
            w.Campaign.FunctionTransfers.Reserve(w,a,b);
            Call("ResetInspector");Call("OpenNodeInspector",a);
            Check((int)Get("_populationNode")==a,"Pending origin still opens its inspector");
            Call("OpenNodeFactory");Check((bool)Get("_nodeFactoryOpen"),"Pending origin still opens its factory");
            Call("OpenNodeAssembly");Check((bool)Get("_nodeAssemblyOpen") && ((long?[])Get("_hangarSlots")).Length==24,"Pending origin can edit the forecast hangar");
        }
        if(frame==3){Call("EndPlannedTurn");Check(w.Robots.Nodes[b].Count==24 && !w.Nodes.All[a].Functional,"Delivery executes the reservation");}
        if(frame==4){Call("FinishTransport");Call("OpenNodeInspector",b);Call("OpenNodeAssembly");Call("UpdateDomesticSlide",1f);}
        if(frame==5){
            Check((bool)Call("HangarPageClick",new Point(820,845)),"Hangar down arrow responds");
            Call("SyncHangarSlots");Check((int)Get("_hangarPage")==1 && ((long?[])Get("_hangarSlots")).Skip(12).Any(id=>id.HasValue),"Scrolling exposes merged stock");
        }
        if(frame==6){Call("OpenNodeFactory");Check((bool)Call("FactoryPageClick",new Point(820,940)),"Factory page selector responds");Check((int)Get("_factoryPage")==1,"Second factory page is reachable");}
        if(frame==7){
            w.Campaign.Advance(w,-1,0,enemies:false);Call("ResetInspector");Call("OpenDirectMap");Call("OpenDirectPair",b,FunctionalDeliveryRoutes.Targets(w,b).First());Call("UpdateDirectPairTransition",.35f);
            var nodes=(int[])Get("_directNodes");int side=Array.IndexOf(nodes,b);Call("DirectPageClick",new Point(500+side*1320,275));
            Check(((int[])Get("_directRow"))[side]==1,"Direct-delivery inspector scrolls to merged stock");
        }
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame*10),TimeSpan.Zero));
        var p=GraphicsDevice.PresentationParameters;var colors=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(colors);
        using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(colors);
        using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"function-transfer-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
        if(frame++==7){Console.WriteLine("PASS: map reservation, mode close and office button removal, delivery execution, source references and second pages of hangar, factory and delivery inspectors.");Exit();}
    }
}
