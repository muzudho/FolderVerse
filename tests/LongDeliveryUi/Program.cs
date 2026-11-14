using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        DrawShot("long-delivery-pick-target",()=>{((CubeNet)Get("_net")).Draw((UiPainter)Get("_ui"),w,panel,-1,0);Call("DrawLongMarkers");});
        var markers=((IEnumerable<(Rectangle Bounds,int Node)>)Get("_longMarkers")).ToArray();Check(markers.Length>2,"Destination markers are shown");
        int target=markers[0].Node;Call("LongMapClick",markers[0].Bounds.Center);Set("_directPairSlide",1f);
        Check((bool)Get("_directPairOpen") && (bool)Get("_longChoosingSource"),"Destination opens right inspector before source selection");
        DrawShot("long-delivery-pick-source",()=>Call("DrawLongPair"));
        markers=((IEnumerable<(Rectangle Bounds,int Node)>)Get("_longMarkers")).ToArray();Check(markers.Length>0,"Source markers appear in the seamless central map");
        var sourceMarker=markers.OrderByDescending(m=>RobotLongDelivery.Path(w,m.Node,target,0).Length).First();int source=sourceMarker.Node;
        Check(RobotLongDelivery.Path(w,source,target,0).Length>2,"A distant source can be selected");
        Set("_pointer",sourceMarker.Bounds.Center);Call("LongPairInput",new MouseState(),true,false);
        Check(!(bool)Get("_longChoosingSource"),"Source opens the left inspector");
        var robot=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[source].Add(robot);Call("SyncLongSlots");
        var down=new MouseState(0,0,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
        Set("_pointer",Rect("DirectPart",0,0,0).Center);Call("LongPairInput",down,true,false);Set("_previousMouse",down);
        Set("_pointer",Rect("DirectCell",1,0).Center);Call("LongPairInput",new MouseState(),false,false);
        var order=w.Robots.LongDelivery.Orders.Single();Check(order.Source==source && order.Target==target && order.Parts==RobotParts.Head,"Dragging the head reserves the exact part and target slot");
        DrawShot("long-delivery-reservation",()=>Call("DrawLongPair"));
        Set("_pointer",new Point(900,980));Call("LongPairInput",down,true,false);
        Set("_pointer",Rect("LongOrderButton",0).Center);Call("LongPairInput",down,true,false);Check(w.Robots.LongDelivery.Orders.Count==0 && w.Robots.Nodes[source].Robots.Single().LongDeliveryNumber==null,"Reservation list cancels the actual order");
        Call("LongBack");Call("LongBack");Check((bool)Get("_longChoosingSource") && (int)Get("_longTarget")==target,"Back keeps destination and clears source");
        Call("LongBack");Check(!(bool)Get("_directPairOpen") && (int)Get("_longTarget")==-1,"Another back clears destination");
        DrawShot("empty-home-lesson",()=>Call("DrawLessonCard",BattleLesson.EmptyHome,new Rectangle(550,140,820,780),true,1f));
        for(int seed=0;seed<20;seed++){w.SetWorld(seed);w.SetPlacement(3);if(w.Routes.Whirlpools.Length>0)break;}
        Check(w.Routes.Whirlpools.Length>0,"A generated world contains a sea whirlpool");
        w.SelectPlayer(0);((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
        DrawShot("nada-map",()=>((CubeNet)Get("_net")).Draw((UiPainter)Get("_ui"),w,panel,-1,0));
        Check(typeof(WorldPreview).GetField("_whirlpoolMesh",F)!.GetValue(Get("_world"))!=null,"Globe builds the raised whirlpool object mesh");
        var globe=(WorldPreview)Get("_world");Call("FocusGlobePosition",w.Routes.WhirlpoolCenter(w.Routes.Whirlpools[0]));globe.GlobeZoom=2;
        DrawShot("nada-globe",()=>{var batch=(SpriteBatch)Get("_spriteBatch");batch.End();globe.Draw((float)Get("_yaw"),(float)Get("_pitch"),0,false,true);batch.Begin();});
        DrawShot("nada-whirlpool-object",()=>SeaWhirlpoolArt.Draw((UiPainter)Get("_ui"),p=>new Vector2(960,540)+p*350));
        w.EquipmentMode=RobotEquipmentMode.Materials;
        typeof(RobotWorld).GetMethod("InitializeMapleSupply",F)!.Invoke(w.Robots,new object[]{w});
        var maple=w.Nodes.All.First(n=>w.Robots.MapleDeposit(n.Id)>=4);
        var deposits=(int[])typeof(RobotWorld).GetField("_mapleDeposits",F)!.GetValue(w.Robots)!;
        var mapleSurface=w.Cells[maple.Cell];
        var mapleNet=(CubeNet)Get("_net");
        var mapleDraw=typeof(CubeNet).GetMethod("DrawMapleDeposit",F)!;
        foreach(bool isGlobe in new[]{false,true})
        foreach(int loads in new[]{0,1,2,3,4,12}){
            deposits[maple.Id]=loads;
            Vector2 Project(Vector3 p){
                var d=p-mapleSurface.Center;
                return new Vector2(960,540)+new Vector2(Vector3.Dot(d,mapleSurface.U),
                    Vector3.Dot(d,mapleSurface.V)-Vector3.Dot(d,mapleSurface.Normal)*.75f)*400;
            }
            DrawShot($"maple-{isGlobe}-{loads}",()=>mapleDraw.Invoke(mapleNet,new object[]{Get("_ui"),w,maple,(Func<Vector3,Vector2>)Project,isGlobe}));
        }
        w.EquipmentMode=RobotEquipmentMode.Legacy;
        var iceCell=w.Cells.OrderByDescending(c=>Enumerable.Range(0,100).Count(i=>w.Routes.PackIceAt(c.Id,new(i%10,i/10)))).First();
        var iceTexture=(Texture2D)typeof(CubeNet).GetMethod("IceTexture",F)!.Invoke(Get("_net"),new object[]{Get("_ui"),w,iceCell.Id})!;
        DrawShot("polar-ice-floes",()=>((UiPainter)Get("_ui")).Batch.Draw(iceTexture,new Rectangle(600,180,720,720),Color.White));
        var polar=w.Cells.First(c=>w.IsPolarFace(c.Face));Call("FocusGlobePosition",polar.Center);globe.GlobeZoom=1.2f;
        DrawShot("polar-ice-globe",()=>{var batch=(SpriteBatch)Get("_spriteBatch");batch.End();globe.Draw((float)Get("_yaw"),(float)Get("_pitch"),0,false,true);batch.Begin();});
        w.SetParticipantCounts(1,59);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
        foreach(var n in w.Nodes.All){n.Owner=n.Id%60;for(int i=0;i<4;i++)w.Robots.Nodes[n.Id].Add(w.Robots.Create(n.Owner,RobotParts.Complete));}
        for(int i=0;i<60;i++){w.SetCountryAppearance(i,i%WorldSetup.Colors.Length,(i+1)%WorldSetup.Colors.Length,1);w.SetCountryEmblem(i,1+i%13);}
        globe.ShowSetup(w,true);var net=(CubeNet)Get("_net");net.ShowRobots=true;net.ShowOtherCountries=true;
        for(int i=0;i<3;i++)globe.DrawRobotPins(w,net,0,0,panel);
        long allocation=GC.GetAllocatedBytesForCurrentThread();var timer=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<10;i++)globe.DrawRobotPins(w,net,0,0,panel);timer.Stop();
        Console.WriteLine($"Robot pins: {timer.Elapsed.TotalMilliseconds/10:F1} ms/frame, {(GC.GetAllocatedBytesForCurrentThread()-allocation)/10/1024} KiB/frame, {w.Robots.Nodes.Sum(n=>n.Count)} robots.");
        Call("CloseDirectDelivery");Set("_statusGlobe",true);net.ShowRoutes=true;Set("_populationCell",-1);Set("_populationNode",-1);Set("_statusCell",-1);
        var captionNode=w.Nodes.All.First(n=>n.Owner==0);var surface=w.Cells[captionNode.Cell];var position=w.Routes.Position(captionNode.Cell,captionNode.Center);Call("FocusGlobePosition",position);
        var area=(Rectangle)Get("StatusGlobeArea");globe.ProjectVisible(position,surface.Normal,(float)Get("_yaw"),(float)Get("_pitch"),area,out var before);
        var caption=new Point(1160,840);globe.GlobePan+=new Vector2((caption.X-before.X)/area.Width,(caption.Y-before.Y)/area.Height);
        Check((int)Call("HitGlobeNode",caption,null!)==captionNode.Id,"A location can be hit beneath the current-location caption");
        MouseState Mouse(ButtonState left)=>new(caption.X,caption.Y,0,left,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
        Set("_previousMouse",Mouse(ButtonState.Released));Call("HandleInput",Mouse(ButtonState.Pressed),new KeyboardState(),new GameTime(),true);Call("HandleInput",Mouse(ButtonState.Released),new KeyboardState(),new GameTime(),true);
        Check((int)Get("_populationNode")==captionNode.Id,"Press/release through the bottom-right text opens the actual captionNode inspector");
        Console.WriteLine("PASS: globe caption is click-through.");
        Console.WriteLine("PASS: primary action ordering, map source/non-adjacent destination selection, actual part reservation/cancel and empty-home lesson rendering.");Exit();
    }
    protected override void Update(GameTime gameTime){}
}
