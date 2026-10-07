using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
try{using var game=new RobotUiCheck();game.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
sealed class RobotUiCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame,lastUpdate=-1;WorldSetup world;Node node;BattleState battle;
    object Get(string name)=>typeof(Game1).GetField(name,Flags)!.GetValue(this)!;
    void Set(string name,object value)=>typeof(Game1).GetField(name,Flags)!.SetValue(this,value);
    void Call(string name,params object[] args)=>typeof(Game1).GetMethod(name,Flags)!.Invoke(this,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    void Click(int x,int y){Set("_pointer",new Point(x,y));Call("RobotInput",true,false);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        world=(WorldSetup)Get("_setup");world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);node=world.Nodes.Current(0);
        var font=(SpriteFont)Get("_font");Check("頭胴脚製造廃棄満杯工期分割野戦説得転倒継戦増援組立輸送失敗剣を振り上げた".All(font.Characters.Contains),"New robot/battle font coverage");
        world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);
        foreach(var parts in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})world.Robots.Nodes[node.Id].Add(world.Robots.Create(0,parts));
        world.Robots.Workshops[node.Id].Configure(RobotParts.Head,3);
        ((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);Call("OpenRobots");
    }
    protected override void Update(GameTime time)
    {
        if(lastUpdate==frame)return;lastUpdate=frame;
        try
        {
            if(frame==1)
            {
                Click(100,250);Click(250,250);Click(400,250);Click(100,637);
                Check(world.Robots.PendingEdits.Count==1,"Assembly queues before disposal completion");world.Robots.Advance((a,b)=>false,_=>0,1);
                Check(world.Robots.Nodes[node.Id].Count==1 && world.Robots.Nodes[node.Id].Robots[0].CanFight,"UI assembly");
                Click(440,575);Check(world.Robots.Workshops[node.Id].Product==RobotParts.Head,"Factory retained until node settings changed");
                Click(1760,322);Check(world.Robots.Transport.Plan(node.Id,0,RobotParts.Head).Sum(w=>w.Twentieths)==1,"5% transport editing");
                Click(1540,322);Check(world.Robots.Transport.Priority(node.Id).Count>0,"Receiving priority UI");
            }
            if(frame==2)
            {
                Click(100,250);Click(330,637);world.Robots.Advance((a,b)=>false,_=>0,2);Check(world.Robots.Nodes[node.Id].Count==2,"UI split");Call("OpenPocket");Set("_pointer",new Point(1040,250));Call("PocketInput",true,false);Set("_pointer",new Point(1040,637));Call("PocketInput",true,false);
                Check(world.Robots.Retinues[0].Count==1,"Retinue transfer");Check(Get("_screen").ToString()=="Pocket","Separate pocket screen");
                var id=world.Robots.Retinues[0].Robots[0].Id;
                Set("_pointer",new Point(100,250));Call("PocketInput",true,false);Set("_pointer",new Point(100,637));Call("PocketInput",true,false);
                Check(world.Robots.Retinues[0].Count==0 && world.Robots.Nodes[node.Id].Robots.Any(r=>r.Id==id),"Pocket deployment preserves identity");
                Set("_pointer",new Point(1040+149,250));Call("PocketInput",true,false);Set("_pointer",new Point(1040,637));Call("PocketInput",true,false);
                Check(world.Robots.Retinues[0].Count==1,"Pocket take back");
                var reserved=world.Robots.Nodes[node.Id].Robots[0];world.Robots.QueueSplit(node.Id,reserved.Id,RobotParts.Body);
                Set("_pointer",new Point(1040,250));Call("PocketInput",true,false);Set("_pointer",new Point(1040,637));Call("PocketInput",true,false);
                Check(world.Robots.Retinues[0].Count==1 && world.Robots.Nodes[node.Id].Robots.Any(r=>r.Id==reserved.Id),"Reserved robot stays deployed");world.Robots.PendingEdits.Clear();
                var fillers=new List<long>();while(world.Robots.Retinues[0].Count<12){var robot=world.Robots.Create(0,RobotParts.Head);fillers.Add(robot.Id);world.Robots.Retinues[0].Add(robot);}
                Call("PocketInput",true,false);Check(world.Robots.Retinues[0].Count==12 && world.Robots.Nodes[node.Id].Count==1,"Full pocket does not remove deployed robot");
                foreach(var filler in fillers)world.Robots.Retinues[0].Remove(filler);Call("OpenPocket");
            }
            if(frame==3)
            {
                Call("PocketInput",false,true);Check(Get("_screen").ToString()=="WorldStatus","Pocket Escape");Call("OpenMovement");
                Check((long)typeof(Game1).GetProperty("AvailableEscort",Flags)!.GetValue(this)! == 0,"Incomplete robots excluded from escort");Call("CloseMovement");
                battle=new BattleState(42,new(){PersuasionPercent=0}){Id=1,Kind=BattleKind.Node,TargetNode=node.Id,Defender=1};
                battle.AddArmy(1,node.Id,new[]{new Robot(10001,1,RobotParts.Complete),new Robot(10002,1,RobotParts.Complete)});
                battle.AddArmy(0,node.Id,new[]{new Robot(10003,0,RobotParts.Complete),new Robot(10004,0,RobotParts.Complete)},true);battle.BuildTerrain(BattleTerrain.Fort);battle.Deploy();
                for(int t=0;t<5;t++)BattleTurnResolver.Advance(battle);
                world.Campaign.RobotBattles.Scenes.Add(battle);Call("OpenBattle");Check(Get("_screen").ToString()=="Battle","Robot battle opening");
            }
            if(frame==4)
            {
                int turn=world.Population.Turn;Set("_pointer",new Point(1100,950));Call("RobotBattleInput",0d,true,false);Check(!(bool)Get("_robotPlaying"),"Pause");
                Set("_pointer",new Point(1600,950));Call("RobotBattleInput",0d,true,false);Check((double)Get("_battleAge")>0 && world.Population.Turn==turn,"Phase stepping does not advance world");
                for(int i=2;i<battle.Frames.Count;i++)
                {
                    Call("RobotBattleInput",0d,true,false);
                    Check((int)typeof(Game1).GetMethod("RobotFrameIndex",Flags)!.Invoke(this,new object[]{battle})! == i,"Each phase advances exactly once");
                }
                Set("_pointer",new Point(1300,950));
                for(int i=battle.Frames.Count-2;i>=0;i--)
                {
                    Call("RobotBattleInput",0d,true,false);
                    Check((int)typeof(Game1).GetMethod("RobotFrameIndex",Flags)!.Invoke(this,new object[]{battle})! == i,"Each phase reverses exactly once");
                }
                Call("RobotBattleInput",0d,false,true);
            }
            if(frame==5)
            {
                Call("RobotBattleInput",0d,false,true);Check(Get("_screen").ToString()=="WorldStatus","Robot battle returns to world");
                world.Campaign.RobotBattles.Scenes.Clear();var robot=world.Robots.Nodes[node.Id].Robots.Single();var route=world.Routes.Neighbors(node)[0];int target=world.Nodes.At(route.Target,route.Entry).Id;
                world.Robots.Transport.Resolve(world.Robots,new[]{new RobotShipment(node.Id,target,robot.Id,robot.Parts)},(a,b)=>true);Call("OpenBattle");Check(Get("_screen").ToString()=="Transport","Transport playback");Call("UpdateTransport",1d,false);
            }
            if(frame==6)
            {
                var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();
            }
            if(frame==7)
            {
                Call("UpdateTransport",2d,true);Check(Get("_screen").ToString()=="WorldStatus","Transport returns to world");Call("OpenRobots");
            }
            if(frame==8)
            {
                battle=new BattleState(7){Kind=BattleKind.Edge,TargetNode=node.Id};
                for(int owner=0;owner<4;owner++)battle.AddArmy(owner,node.Id,Enumerable.Range(0,3).Select(i=>new Robot(20000+owner*10+i,owner,RobotParts.Complete)),true);
                battle.BuildTerrain(BattleTerrain.Sea);battle.Deploy();battle.Capture(BattlePhase.Place);
                var captain=battle.Units.First(u=>u.Role==BattleRole.Captain);battle.Events.Add(new(0,BattlePhase.Place,captain.Id,"隊形成 / 剣を振り上げた"));
                world.Campaign.RobotBattles.Scenes.Clear();world.Campaign.RobotBattles.Scenes.Add(battle);world.Robots.Transport.Resolve(world.Robots,Array.Empty<RobotShipment>(),(a,b)=>true);Call("OpenBattle");
            }
            if(frame==9)
            {
                Check(BattlePieceArt.Pieces.Length==12 && BattlePieceArt.Pieces.Select(p=>string.Join(";",p)).Distinct().Count()==12,"Twelve distinct role/direction drawings");
                battle=new BattleState(8){Kind=BattleKind.Edge,TargetNode=node.Id};battle.Armies.Add(new(){Owner=0,Side=0});
                foreach(var role in Enum.GetValues<BattleRole>())foreach(var facing in Enum.GetValues<BattleFacing>())
                    battle.Units.Add(new(){Id=30000+BattlePieceArt.Index(role,facing),Owner=0,Role=role,Facing=facing,Position=new(2+(int)facing*2,1+(int)role*2)});
                battle.Capture(BattlePhase.Attack);battle.Capture(BattlePhase.Receive);
                battle.Attacks.Add(new(0,30000,null,new(1,8),new(8,8),BattleWeapon.Rifle));
                world.Campaign.RobotBattles.Scenes.Clear();world.Campaign.RobotBattles.Scenes.Add(battle);Call("OpenBattle");Set("_robotPlaying",false);
                Check(((SpriteFont)Get("_font")).Characters.Contains('冠'),"Piece legend font coverage");
            }
            if(frame==10)
            {
                battle=new BattleState(9,new(){PersuasionPercent=0}){Kind=BattleKind.Node,TargetNode=node.Id,Defender=0,Flag=new(8,8)};
                battle.Armies.Add(new(){Owner=0,Side=0});battle.Armies.Add(new(){Owner=1,Side=2});
                var starts=new[]{new Point(1,1),new Point(4,1),new Point(1,4),new Point(4,4)};
                var directions=new[]{BattleFacing.South,BattleFacing.East,BattleFacing.North,BattleFacing.West};
                for(int i=0;i<4;i++)
                {
                    var step=BattleState.Forward(directions[i]);
                    battle.Units.Add(new(){Id=40000+i*2,Owner=0,Position=starts[i],Facing=directions[i]});
                    battle.Units.Add(new(){Id=40001+i*2,Owner=1,Position=starts[i]+step,Facing=(BattleFacing)(((int)directions[i]+2)%4)});
                }
                foreach(var facing in Enum.GetValues<BattleFacing>())battle.Units.Add(new(){Id=41000+(int)facing,Owner=0,Role=BattleRole.Queen,Facing=facing,Position=new(1+(int)facing*2,7),Daggers=0,Rifles=0});
                BattleTurnResolver.Advance(battle,battle.Units.ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing,u.Role==BattleRole.Queen?BattleWeapon.None:BattleWeapon.Dagger)));
                Check(battle.Attacks.Count(a=>battle.Attacks.Any(other=>BattleAttackArt.Clashes(a,other)))==8,"Four directional frontal dagger clashes");
                world.Campaign.RobotBattles.Scenes.Clear();world.Campaign.RobotBattles.Scenes.Add(battle);Call("OpenBattle");Set("_robotPlaying",false);Set("_battleAge",.08*battle.Frames.FindIndex(f=>f.Phase==BattlePhase.Attack));
            }
            if(frame==11){battle.FlagStanding=false;battle.Capture(BattlePhase.Receive);Set("_battleAge",.08*(battle.Frames.Count-1));}
            if(frame==12)
            {
                var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=1920;graphics.PreferredBackBufferHeight=1080;graphics.ApplyChanges();
                world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);
                for(int i=0;i<12;i++)world.Robots.Nodes[node.Id].Add(world.Robots.Create(i%2,(RobotParts)(1+i%7)));
                ((WorldPreview)Get("_world")).ShowSetup(world,true);
                Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",Flags)!.FieldType,"WorldStatus"));Set("_statusGlobe",false);Set("_pointer",Point.Zero);
                var net=(CubeNet)Get("_net");net.Home(world);net.ShowRoutes=true;
                var panel=new Rectangle(1040,240,790,580);var at=net.NodePosition(world,node,panel);
                net.Drag(new Vector2(panel.Center.X,panel.Center.Y)-at);
            }
            if(frame==13)
            {
                var net=(CubeNet)Get("_net");var badge=net.RobotBadges.Single(b=>b.Node==node.Id);
                Check(!badge.Detailed && net.HitRobotBadge(badge.Bounds.Center)==node.Id,"Compact dozen pins and badge hover");
                var panel=new Rectangle(1040,240,790,580);var before=net.NodePosition(world,node,panel);
                net.ZoomAt(panel,new Point(panel.Left+50,panel.Top+50),120*30);
                Check(net.Zoom==64 && Vector2.Distance(before,net.NodePosition(world,node,panel))<1,"64x zoom preserves viewport center even with off-center pointer");
            }
            if(frame==14)
            {
                var net=(CubeNet)Get("_net");Check(net.RobotBadges.Single(b=>b.Node==node.Id).Detailed,"Zoom shows all twelve mixed-part slots");
                var panel=new Rectangle(1040,240,790,580);float zoom=net.Zoom;
                net.Drag(new Vector2(155,-120));net.SetCenter(world,(world.Cells[node.Cell].Face+1)%6);
                Set("_pointer",new Point(1420,935));
                Call("HandleInput",new Microsoft.Xna.Framework.Input.MouseState(1420,935,0,Microsoft.Xna.Framework.Input.ButtonState.Pressed,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released),new Microsoft.Xna.Framework.Input.KeyboardState(),new GameTime(),true);
                Check(net.Zoom==zoom && Vector2.Distance(net.NodePosition(world,node,panel),new Vector2(panel.Center.X,panel.Center.Y))<1,"Current-location button centers exact node and preserves zoom");
                net.ZoomAt(panel,net.NodePosition(world,node,panel).ToPoint(),-120*30);
            }
            if(frame==15)
            {
                var net=(CubeNet)Get("_net");Check(!net.RobotBadges.Single(b=>b.Node==node.Id).Detailed,"Zoom out returns to overview");
                Set("_statusGlobe",true);Call("FocusStatusGlobe");
                var globe=(WorldPreview)Get("_world");var area=(Rectangle)typeof(Game1).GetProperty("StatusGlobeArea",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
                foreach(int face in Enumerable.Range(0,6))
                {
                    var target=world.Nodes.All.First(n=>world.Cells[n.Cell].Face==face);
                    world.ConquerorLocations[0]=target.Cell;world.ConquerorPoints[0]=target.Center;Call("FocusStatusGlobe");
                    Check(globe.ProjectVisible(world.Routes.Position(target.Cell,target.Center),world.Cells[target.Cell].Normal,(float)Get("_yaw"),(float)Get("_pitch"),area,out var projected) && Vector2.Distance(projected,new Vector2(area.Center.X,area.Center.Y))<1,"Globe centers current node on every face");
                }
                world.ConquerorLocations[0]=node.Cell;world.ConquerorPoints[0]=node.Center;Call("FocusStatusGlobe");
            }
            if(frame==16)
            {
                Check(((CubeNet)Get("_net")).RobotBadges.Any(b=>b.Node==node.Id && !b.Detailed),"Globe robot pins");
                int turn=world.Population.Turn;
                Set("_pointer",new Point(150,1040));Call("MovementClick",new Point(150,1040),new Microsoft.Xna.Framework.Input.KeyboardState());
                Check(Get("_screen").ToString()=="Pocket" && world.Population.Turn==turn,"Main personal pocket shortcut");Call("PocketInput",false,true);
                Set("_pointer",new Point(350,1040));Call("MovementClick",new Point(350,1040),new Microsoft.Xna.Framework.Input.KeyboardState());
                Check(Get("_screen").ToString()=="Robots" && world.Population.Turn==turn,"Main robot transport shortcut opens settings without advancing world");Call("RobotInput",false,true);
                world.Campaign.RobotBattles.Scenes.Clear();
                for(int i=0;i<2;i++)
                {
                    var result=new BattleState(20+i){Kind=BattleKind.Node,TargetNode=node.Id,Defender=0,Finished=true,Winner=0};
                    result.Capture(BattlePhase.Choose);result.Capture(BattlePhase.Result);world.Campaign.RobotBattles.Scenes.Add(result);
                }
                Call("OpenBattle");Set("_battleAge",.08);Set("_pointer",Point.Zero);
            }
            if(frame==17)
            {
                Call("RobotBattleInput",4.9d,false,false);Check((int)Get("_battleWave")==0 && Math.Abs((double)Get("_robotResultAge")-4.9)<.0001,"Result waits five seconds");
                Set("_pointer",new Point(1100,1040));Call("RobotBattleInput",.2d,true,false);Check(!(bool)Get("_robotPlaying") && (int)Get("_battleWave")==0,"Result pause prevents expiry on the click");
            }
            if(frame==18)
            {
                int turn=world.Population.Turn;Call("RobotBattleInput",10d,false,false);Check((int)Get("_battleWave")==0 && Math.Abs((double)Get("_robotResultAge")-4.9)<.0001,"Paused result timer stays frozen");
                Call("RobotBattleInput",0d,true,false);Call("RobotBattleInput",.1d,false,false);
                Check((int)Get("_battleWave")==1 && (double)Get("_robotResultAge")==0 && world.Population.Turn==turn,"Resumed timer advances one scene and resets without a global turn");
            }
            if(frame==19)
            {
                int turn=world.Population.Turn;Call("RobotBattleInput",5.08d,false,false);
                Check(Get("_screen").ToString()=="WorldStatus" && world.Population.Turn==turn,"Last scene auto-returns to world without advancing its turn");
                Set("_statusGlobe",false);var net=(CubeNet)Get("_net");net.Home(world);
                var panel=new Rectangle(1040,240,790,580);net.Drag(new Vector2(panel.Center.X,panel.Center.Y)-net.NodePosition(world,node,panel));
                var store=world.Robots.Nodes[node.Id];foreach(var robot in store.Robots.ToArray())store.Remove(robot.Id);
                store.Add(world.Robots.Create(0,RobotParts.Head));
            }
            if(frame==20 || frame==21)
            {
                var net=(CubeNet)Get("_net");var badge=net.RobotBadges.Single(b=>b.Node==node.Id);
                var at=net.NodePosition(world,node,new Rectangle(1040,240,790,580));
                Check(badge.Bounds.Width==7 && badge.Bounds.Height==(frame==20?7:3),"Sparse pin bounds follow the visible parts rather than a dozen empty slots");
                Check(Math.Abs(badge.Bounds.Center.X-at.X)<=1 && Math.Abs(at.Y-badge.Bounds.Bottom-(node.IsHarbor?12:6))<=1,"Visible pin centered immediately above its node");
                if(frame==20){var store=world.Robots.Nodes[node.Id];store.Remove(store.Robots[0].Id);store.Add(world.Robots.Create(0,RobotParts.Legs));}
                else
                {
                    var store=world.Robots.Nodes[node.Id];while(store.Count<12)store.Add(world.Robots.Create(store.Count%2,(RobotParts)(1+store.Count%7)));
                    Set("_populationCell",node.Cell);Set("_populationNode",node.Id);
                    var oldBirth=node.Population.BirthPercent;var oldConversion=node.Population.Conversion.ToArray();
                    Call("NodeDialogClick",new Point(820,470),new Microsoft.Xna.Framework.Input.KeyboardState());
                    Check(node.Population.BirthPercent==oldBirth && node.Population.Conversion.SequenceEqual(oldConversion),"Removed population controls do not mutate settings");
                }
            }
            if(frame==22)
            {
                Check(world.Robots.Nodes[node.Id].Count==12,"Node inspection renders a full dozen");
                foreach(var robot in world.Robots.Nodes[node.Id].Robots.ToArray())world.Robots.Nodes[node.Id].Remove(robot.Id);
            }
            if(frame==23)
            {
                Check(world.Robots.Nodes[node.Id].Count==0,"Empty node inspection");
                Call("NodeDialogClick",new Point(830,190),new Microsoft.Xna.Framework.Input.KeyboardState());
                Check((int)Get("_populationCell")==-1,"Node robot panel closes");
                Set("_populationCell",node.Cell);Set("_populationNode",node.Id);
                Call("NodeDialogClick",new Point(150,910),new Microsoft.Xna.Framework.Input.KeyboardState());
                Check((bool)Get("_nodeFactoryOpen"),"Own node factory opens from tile");
                world.Campaign.RobotBattles.Encounters.Clear();
                Call("NodeDialogClick",new Point(700,370),new Microsoft.Xna.Framework.Input.KeyboardState());
                Call("NodeDialogClick",new Point(100,535),new Microsoft.Xna.Framework.Input.KeyboardState());
                Check(world.Robots.Workshops[node.Id].Product==RobotParts.Head,"Factory product plan applied: "+Get("_factoryPart")+" / "+world.Robots.Workshops[node.Id].Product+" / "+Get("_factoryMessage")+" / "+Get("_populationCell"));
                int period=world.Robots.Workshops[node.Id].ProductionPeriod;
                Call("NodeDialogClick",new Point(310,680),new Microsoft.Xna.Framework.Input.KeyboardState());
                Check(world.Robots.Workshops[node.Id].ProductionPeriod==period+1,"Factory period increases");
                Call("NodeDialogClick",new Point(500,535),new Microsoft.Xna.Framework.Input.KeyboardState());
                Check(world.Robots.Workshops[node.Id].Paused,"Factory pause");
            }
            if(frame==24)
            {
                Call("NodeDialogClick",new Point(830,190),new Microsoft.Xna.Framework.Input.KeyboardState());
                Check(!(bool)Get("_nodeFactoryOpen") && (int)Get("_populationCell")>=0,"Factory close returns to node dialog");
                int owner=node.Owner;node.Owner=1;world.Relations.SetSuperior(1,0);
                Call("NodeDialogClick",new Point(150,910),new Microsoft.Xna.Framework.Input.KeyboardState());Check((bool)Get("_nodeFactoryOpen"),"Subordinate factory access");
                Call("NodeDialogClick",new Point(830,190),new Microsoft.Xna.Framework.Input.KeyboardState());world.Relations.SetSuperior(1,-1);
                Call("NodeDialogClick",new Point(150,910),new Microsoft.Xna.Framework.Input.KeyboardState());Check(!(bool)Get("_nodeFactoryOpen"),"Enemy factory disabled");
                world.Relations.SetSuperior(0,1);Call("NodeDialogClick",new Point(150,910),new Microsoft.Xna.Framework.Input.KeyboardState());Check(!(bool)Get("_nodeFactoryOpen"),"Superior factory is not subordinate access");
                world.Relations.SetSuperior(0,-1);node.Owner=owner;
                Console.WriteLine("PASS: node/factory dialogs, product/period/pause, own/subordinate access, enemy/superior denial and robot/map/battle UI.");Exit();
            }
        }
        catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;Exit();}
    }
    protected override void Draw(GameTime time)
    {
        try
        {
            base.Draw(time);var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);
            using var texture=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);texture.SetData(pixels);
            using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"robot-ui-{frame}.png"));texture.SaveAsPng(output,texture.Width,texture.Height);frame++;
        }
        catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;Exit();}
    }
}
