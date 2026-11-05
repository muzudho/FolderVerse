using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

sealed class PlannedHangarsUi:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,a,b;WorldSetup w=null!;Robot head=null!,rest=null!;RobotWorkshop line=null!;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    void Input(Point p,ButtonState state){var c=(Rectangle)Call("CanvasBounds");Call("HandleInput",new MouseState(c.X+p.X*c.Width/1920,c.Y+p.Y*c.Height/1080,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);}
    void Click(Point p){Input(p,ButtonState.Released);Input(p,ButtonState.Pressed);Input(p,ButtonState.Released);}
    void Open(int node){Call("OpenNodeInspector",node);Call("UpdateDomesticSlide",1f);Call("OpenNodeAssembly");}
    void Assemble(){
        Call("SyncHangarSlots");var slots=(long?[])Get("_hangarSlots");int source=Array.IndexOf(slots,(long?)head.Id),target=Array.IndexOf(slots,(long?)rest.Id);
        var from=(Rectangle)typeof(Game1).GetMethod("HangarPartBounds",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{source,0})!;
        var to=(Rectangle)typeof(Game1).GetMethod("HangarCell",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{target})!;
        Input(from.Center,ButtonState.Released);Input(from.Center,ButtonState.Pressed);Input(to.Center,ButtonState.Pressed);Input(to.Center,ButtonState.Released);
    }
    protected override void LoadContent(){
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);
        w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);w.HumanPlayers.Add(0);
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);foreach(var shop in w.Robots.Workshops)shop.AutoAssemblyDisabled=true;
        var first=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);a=first.Id;var route=w.Routes.Neighbors(first)[0];b=w.Nodes.At(route.Target,route.Entry).Id;first.Owner=w.Nodes.All[b].Owner=0;
        head=w.Robots.Create(0,RobotParts.Head);rest=w.Robots.Create(0,RobotParts.Body|RobotParts.Legs);w.Robots.Nodes[a].Add(head);w.Robots.Nodes[b].Add(rest);
        w.Campaign.FunctionTransfers.Reserve(w,a,b);
        ((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);Open(b);
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time){
        if(frame==1){Assemble();Check(w.Robots.PartPlanning.Orders.Count==1 && w.Robots.Nodes[a].Count==1 && w.Robots.Nodes[b].Robots.Single().Parts!=RobotParts.Complete,"Normal drag reserves without changing real stock");}
        if(frame==2){Click(new(470,275));Check(w.Robots.PartPlanning.Orders.Count==0,"Undo button removes operation");Assemble();w.Campaign.FunctionTransfers.Cancel(a);Check(w.Robots.PartPlanning.Preview(w).Nodes[b].Robots.Single().Parts==RobotParts.Complete,"Cancelled function transfer keeps valid adjacent assembly forecast");}
        if(frame==3){Call("EndPlannedTurn");Check(w.Robots.Nodes[a].Count==0 && w.Robots.Nodes[b].Robots.Single().Parts==RobotParts.Complete,"Real delivery performs the fallback");}
        if(frame==4){
            w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,enemies:false,allPlanned:true);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
            line=w.Robots.Workshops[a];line.Configure(RobotParts.Complete,1);line.SetDoNotRepeat(true);Open(a);Call("OpenNodeFactory");
        }
        if(frame==5){Click(new(120,435));Check(Get("_intakeLine")!=null,"Factory intake button opens destination picker");}
        if(frame==6){
            var choices=(int[])typeof(Game1).GetProperty("IntakeChoices",F)!.GetValue(this)!;int index=Array.IndexOf(choices,b);Check(index>=0,"Adjacent destination is offered");Click(new(900,322+index*62));Check(line.IntakeTarget==b,"Normal input selects adjacent intake destination");
            w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);Check(w.Robots.ProductionIntakes.Count==1 && w.Robots.Nodes[b].Count==1,"Manufactured output is an intake booking");Open(b);
        }
        if(frame==7){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
        if(frame==8){
            Call("OpenDirectPair",b,a);Call("UpdateDirectPairTransition",.35f);
            var slots=(long?[][])Get("_directSlots");long intake=w.Robots.ProductionIntakes.Single().Product.Id;int slot=Array.IndexOf(slots[0],(long?)intake);
            Check(slot>=0,"Direct-delivery inspector offers manufactured intake prototype");
            var from=(Rectangle)typeof(Game1).GetMethod("DirectPart",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{0,slot,0})!;
            var to=(Rectangle)typeof(Game1).GetMethod("DirectCell",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{1,0})!;
            Input(from.Center,ButtonState.Released);Input(from.Center,ButtonState.Pressed);Input(to.Center,ButtonState.Pressed);Input(to.Center,ButtonState.Released);
            Check(w.Robots.PartPlanning.Orders.Count==1 && w.Robots.Nodes[a].Count==0 && w.Robots.ProductionIntakes.Single().Product.Parts==RobotParts.Complete,"Cross-inspector drag is a reservation and does not mutate pending product");
        }
        if(frame==9){Click(new(960,84));Check(w.Robots.PartPlanning.Orders.Count==0 && w.Robots.PartPlanning.Preview(w).Nodes[b].Count==2,"Scaled direct-inspector undo restores planned intake");}
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame*10),TimeSpan.Zero));
        var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);
        using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"planned-hangars-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
        if(frame++==9){Console.WriteLine("PASS: real projected-hangar drag/undo, transfer cancellation, fallback execution, intake destinations, cross-inspector reservations and 1080p/540p arrival preview.");Exit();}
    }
}
