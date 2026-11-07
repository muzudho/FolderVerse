using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CheckLongUi();game.Run();
sealed class CheckLongUi:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(string n)=>(typeof(Game1).GetField(n,F)?.GetValue(this)??typeof(Game1).GetProperty(n,F)?.GetValue(this))!;
    void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
    Rectangle Rect(string n,params object[] args)=>(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args)!;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    void DrawShot(string name,Action draw){
        var batch=(SpriteBatch)Get("_spriteBatch");GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();draw();batch.End();
        var pixels=new Color[1920*1080];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,1920,1080);image.SetData(pixels);
        using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name+".png"));image.SaveAsPng(file,1920,1080);
    }
    protected override void LoadContent(){
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var w=(WorldSetup)Get("_setup");
        Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);foreach(var node in w.Nodes.All)node.Owner=0;
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_statusGlobe",false);w.SelectPlayer(0);((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
        var direct=Rect("PrimaryActionHomeBounds",3);var distant=Rect("PrimaryActionHomeBounds",7);var balance=Rect("PrimaryActionHomeBounds",4);
        Check(direct.Right<distant.Left && distant.Right<balance.Left,"Primary buttons are direct, long distance, then long-term balance");
        DrawShot("long-delivery-primary-buttons",()=>Call("DrawPrimaryActionButtonsCore"));
        Call("MapOverlayClick",distant.Center,false);Check((bool)Get("_longDeliveryMode") && (bool)Get("_directMap"),"The real primary button opens long-distance mode");
        var panel=(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!;
        DrawShot("long-delivery-pick-source",()=>{((CubeNet)Get("_net")).Draw((UiPainter)Get("_ui"),w,panel,-1,0);Call("DrawLongMarkers");});
        var markers=((IEnumerable<(Rectangle Bounds,int Node)>)Get("_longMarkers")).ToArray();Check(markers.Length>2,"Long-distance source markers are shown on the actual map");
        int source=markers[0].Node;Call("LongMapClick",markers[0].Bounds.Center);
        DrawShot("long-delivery-pick-target",()=>{((CubeNet)Get("_net")).Draw((UiPainter)Get("_ui"),w,panel,-1,0);Call("DrawLongMarkers");});
        markers=((IEnumerable<(Rectangle Bounds,int Node)>)Get("_longMarkers")).ToArray();var target=markers.OrderByDescending(m=>RobotLongDelivery.Path(w,source,m.Node,0).Length).First();
        Check(RobotLongDelivery.Path(w,source,target.Node,0).Length>2,"A non-adjacent target is selectable");Call("LongMapClick",target.Bounds.Center);
        Check((bool)Get("_directPairOpen"),"Selecting the target opens both hangars");
        var robot=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[source].Add(robot);Call("SyncLongSlots");
        Set("_pointer",Rect("DirectPart",0,0,0).Center);Call("LongPairInput",true,false);
        Set("_pointer",Rect("DirectCell",1,0).Center);Call("LongPairInput",true,false);
        var order=w.Robots.LongDelivery.Orders.Single();Check(order.Source==source && order.Target==target.Node && order.Parts==RobotParts.Head,"Actual part and destination cell clicks reserve the exact head");
        DrawShot("long-delivery-reservation",()=>Call("DrawLongPair"));
        Set("_pointer",Rect("LongOrderButton",0).Center);Call("LongPairInput",true,false);Check(w.Robots.LongDelivery.Orders.Count==0 && w.Robots.Nodes[source].Robots.Single().LongDeliveryNumber==null,"Reservation list can cancel the actual order");
        DrawShot("empty-home-lesson",()=>Call("DrawLessonCard",BattleLesson.EmptyHome,new Rectangle(550,140,820,780),true,1f));
        for(int seed=0;seed<20;seed++){w.SetWorld(seed);w.SetPlacement(3);if(w.Routes.Whirlpools.Length>0)break;}
        Check(w.Routes.Whirlpools.Length>0,"A generated world contains a sea whirlpool");
        w.SelectPlayer(0);((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
        DrawShot("nada-map",()=>((CubeNet)Get("_net")).Draw((UiPainter)Get("_ui"),w,panel,-1,0));
        Check(typeof(WorldPreview).GetField("_whirlpoolMesh",F)!.GetValue(Get("_world"))!=null,"Globe builds the raised whirlpool object mesh");
        var globe=(WorldPreview)Get("_world");Call("FocusGlobePosition",w.Routes.WhirlpoolCenter(w.Routes.Whirlpools[0]));globe.GlobeZoom=2;
        DrawShot("nada-globe",()=>{var batch=(SpriteBatch)Get("_spriteBatch");batch.End();globe.Draw((float)Get("_yaw"),(float)Get("_pitch"),0,false,true);batch.Begin();});
        DrawShot("nada-whirlpool-object",()=>SeaWhirlpoolArt.Draw((UiPainter)Get("_ui"),p=>new Vector2(960,540)+p*350));
        Console.WriteLine("PASS: primary action ordering, map source/non-adjacent destination selection, actual part reservation/cancel and empty-home lesson rendering.");Exit();
    }
    protected override void Update(GameTime gameTime){}
}
