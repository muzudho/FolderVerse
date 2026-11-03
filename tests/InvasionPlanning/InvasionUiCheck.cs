using FolderVerse;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

sealed class InvasionUiCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame;WorldSetup world=null!;Node home=null!,target=null!;
    object Get(string n)=>typeof(Game1).GetField(n,Flags)!.GetValue(this)!;
    void Set(string n,object v)=>typeof(Game1).GetField(n,Flags)!.SetValue(this,v);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,Flags)!.Invoke(this,args)!;
    void Screen(string name)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),name));
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        world=(WorldSetup)Get("_setup");world.SetWorld(2);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
        world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);
        home=world.Nodes.Current(0);home.Owner=0;var route=world.Routes.Neighbors(home)[0];target=world.Nodes.At(route.Target,route.Entry);target.Owner=1;
        foreach(var store in new[]{world.Robots.Nodes[home.Id],world.Robots.Retinues[0]})for(int i=0;i<5;i++)store.Add(world.Robots.Create(0,RobotParts.Complete));
        var hangar=world.Robots.Nodes[home.Id];var samples=hangar.Robots.ToArray();
        foreach(var r in samples)hangar.Remove(r.Id);
        foreach(var r in samples.Select((r,i)=>i switch{
            1=>r with {DefenseOnly=true,DefenseFlag=new(0,0,0)},
            2=>r with {DamagedParts=RobotParts.Body,InterceptOnly=true},
            3=>r with {CheapParts=RobotParts.Complete,FactoryWorker=true},
            4=>r with {Disposal=true},_=>r}))hangar.Add(r);
        hangar.Add(world.Robots.Create(0,RobotParts.Complete) with {BodyOrigin=1});
        world.Campaign.Plan(new(0,target.Id,1,home.Id,false,false,new[]{samples[0].Id}));
        world.Campaign.Plan(new(0,home.Id,1,home.Id,false,true,new[]{world.Robots.Retinues[0].Robots[0].Id}));
        ((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);
        Set("_statusGlobe",false);Screen("WorldStatus");Call("OpenWarPlanning");Set("_domesticSlide",1f);
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time)
    {
        try
        {
            if(frame==0 && Environment.GetCommandLineArgs().Contains("--war-targets")){
                var net=(CubeNet)Get("_net");var globe=(WorldPreview)Get("_world");Call("PrepareDestinationMap");Call("UpdateNodeFocusCircle");
                if(!net.AttackTargetSpotlight || !globe.AttackTargetSpotlight || net.FocusBrightness!=.65f)throw new Exception("War spotlight begins before choosing a target at 65% brightness");
                foreach(var node in world.Nodes.All){
                    bool attackable=net.MovementTargets.Contains(node.Id);net.HoveredNode=node.Id;
                    if(net.EmphasizedNode(node.Id)!=attackable || net.DimNode(node.Id)==attackable)throw new Exception("Only attackable nodes can hover-emphasize and stay bright");
                    var white=net.NodeTint(node.Id,Color.White);if(attackable?white!=Color.White:Math.Abs(white.R-166)>1)throw new Exception("Non-target node tint must be 35% darker");
                    if(!net.NodeVisible(world,node))throw new Exception("Non-target nodes remain visible for context");
                }
                net.HoveredNode=home.Id;
            }
            if(frame==1){Call("SelectMoveNode",target.Id);var net=(CubeNet)Get("_net");var panel=(Rectangle)typeof(Game1).GetProperty("NetPanel",Flags)!.GetValue(this)!;net.SetCenter(world,world.Cells[home.Cell].Face);net.FitNodesAtSource(world,home,panel,8);var middle=(net.NodePosition(world,home,panel)+net.NodePosition(world,target,panel))/2;net.Drag(new Vector2(1430,550)-middle);}
            if(frame==1 && Environment.GetCommandLineArgs().Contains("--war-targets")){
                var net=(CubeNet)Get("_net");var globe=(WorldPreview)Get("_world");
                Call("PrepareDestinationMap");Call("UpdateNodeFocusCircle");
                var targets=net.MovementTargets.ToArray();
                if(targets.Length<2 || targets.Any(id=>!net.NodeVisible(world,world.Nodes.All[id])))throw new Exception("Every attackable target must remain visible after selection");
                if(net.NodeFocus!=target.Id || globe.NodeFocus!=target.Id || net.CountryFocus!=-1 || Math.Abs(net.FocusBrightness-.65f)>.001f || net.DimNode(target.Id))throw new Exception("Selected target must have a spotlight with 35% outside dimming");
                foreach(var source in world.Nodes.All.Where(n=>n.Owner>=0 && world.ControllerOf(n.Owner)==world.TurnPlayer && world.Campaign.RobotBattles.CanAttackFrom(world,n.Owner,n.Id,target.Id)))
                    if(!net.DimNode(source.Id))throw new Exception("Own sortie sources stay dim outside attackable target spotlight");
                foreach(int id in targets)if(net.DimNode(id))throw new Exception("All attackable destinations stay bright after selection");
                net.HoveredNode=home.Id;if(net.EmphasizedNode(home.Id))throw new Exception("Own node hover stays quiet after target selection");
                int next=targets.First(id=>id!=target.Id);
                var panel=(Rectangle)typeof(Game1).GetProperty("NetPanel",Flags)!.GetValue(this)!;
                net.Drag(new Vector2(1430,550)-net.NodePosition(world,world.Nodes.All[next],panel));
                var at=net.NodePosition(world,world.Nodes.All[next],panel).ToPoint();
                if(!(bool)Call("MovementClick",at,new Microsoft.Xna.Framework.Input.KeyboardState()) || (int)Get("_selectedMoveNode")!=next)throw new Exception("Clicking another map target must switch directly without reset");
                Call("PrepareDestinationMap");Call("UpdateNodeFocusCircle");
                if(net.NodeFocus!=next || globe.NodeFocus!=next || targets.Any(id=>!net.NodeVisible(world,world.Nodes.All[id])))throw new Exception("Spotlight must follow selection while keeping targets visible");
                if(!world.Campaign.PlannedOrders.Any(o=>o.TargetNode==target.Id))throw new Exception("Switching selection must preserve confirmed reservations");
            }
            if(frame==2 && !Environment.GetCommandLineArgs().Contains("--war-targets")){
                Call("OpenInvasionPicker",0);Set("_pointer",new Point(610,350));Call("InvasionPickerInput",true,false);
                var guard=world.Robots.Nodes[home.Id].Robots.Single(r=>r.DefenseOnly);var rows=(System.Collections.IList)Get("_invasionSources");
                if(!((HashSet<long>)Get("_invasionDraft")).Contains(guard.Id) || (string)Call("InvasionReservationLabel",rows[0]!,guard)!="守備")throw new Exception("Selected guard must still show its original reservation until confirmation");
            }
            if(frame==3 && !Environment.GetCommandLineArgs().Contains("--war-targets"))
            {
                Set("_invasionPicker",-1);
                var b=new BattleState(42,new(){PersuasionPercent=100}){Id=100,TargetNode=target.Id,Kind=BattleKind.Edge};b.Armies.Add(new(){Owner=0,Side=0});b.Armies.Add(new(){Owner=1,Side=2});
                b.Units.Add(new(){Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(3,4),Daggers=0,Rifles=0});b.Units.Add(new(){Id=999,Owner=1,Position=new(5,4),Robot=new(999,1,RobotParts.Complete){DamagedParts=RobotParts.Body}});
                b.Capture(BattlePhase.Persuade);typeof(BattleTurnResolver).GetMethod("Persuade",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{b});b.Capture(BattlePhase.Converted);
                Set("_robotScenes",new[]{b});Set("_robotPlayback",new RobotBattlePlayback(new[]{b}));Set("_robotPlaying",false);Set("_battleAge",0d);Screen("Battle");
            }
            if(frame==4)Set("_battleAge",.25d);
            if(frame==5){Screen("WorldStatus");Set("_tutorialAttackGuide",true);Set("_tutorialGuideStarted",0f);}
            if(frame==6){Screen("Title");Set("_tutorialAttackGuide",false);Set("_tutorialMenuOpen",true);}
            if(frame==7){Set("_tutorialMenuOpen",false);Screen("Battle");var b=((BattleState[])Get("_robotScenes"))[0];b.Kind=BattleKind.Node;b.Defender=1;b.Frames.Clear();b.FlagStanding=false;b.LastFlagOwner=0;b.FlagCaptureTurns=7;b.Capture(BattlePhase.Flag);b.FlagCaptureTurns=8;b.Capture(BattlePhase.Result);Set("_robotPlayback",new RobotBattlePlayback(new[]{b}));Set("_battleAge",0d);}
            if(frame==8){typeof(WorldSetup).GetProperty("TutorialStep")!.SetValue(world,1);Call("CloseMovement");Screen("WorldStatus");}
            if(frame==9)Set("_tutorialAttackAchieved",true);
            if(frame==10){var medals=new TutorialAchievements(Path.Combine(AppContext.BaseDirectory,"tutorial-ui-medals.json"));medals.Award(TutorialAchievements.FirstAttack);Set("_tutorialAchievements",medals);Screen("Title");Set("_tutorialMenuOpen",true);}
            if(frame==11){Set("_pointer",new Point(1100,1000));Call("TutorialMenuInput",true,false,false,new Microsoft.Xna.Framework.Input.KeyboardState());if(((TutorialAchievements)Get("_tutorialAchievements")).Has(TutorialAchievements.FirstAttack))throw new Exception("Clear medal button failed");}
            if(frame==12){Set("_tutorialMenuOpen",false);Screen("Battle");Set("_tutorialAchievementAge",1d);}
            if(frame==13){Call("RobotBattleInput",3d,false,false);if((double)Get("_tutorialAchievementAge")!=-1)throw new Exception("Achievement cut-in did not expire automatically");}
            if(frame==14){Screen("WorldStatus");Set("_tutorialAttackAchieved",false);Set("_tutorialAchievements",null!);Set("_tutorialIntroShown",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);Call("UpdateTutorialStartGuide",10d);}
            if(frame==15){Set("_tutorialAttackGuide",false);Set("_pointer",new Point(1500,40));Call("TutorialMessageInput",true,false);}
            if(frame==16){Set("_pointer",new Point(450,550));Call("TutorialMessageInput",true,false);}
            if(frame==17)Call("TutorialMessageInput",false,true);
            if(frame==18)
            {
                Screen("WorldStatus");Set("_tutorialAttackGuide",false);Set("_conquestOverview",false);
                Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);
                world.Campaign.RobotBattles.Rules=new(){PersuasionPercent=100};
                world.Robots.Nodes[home.Id].Add(world.Robots.Create(1,RobotParts.Complete) with {DamagedParts=RobotParts.Body,CheapParts=RobotParts.Legs});
                Call("PrepareHomePersuasion");Call("UpdateHomePersuasion",.5d,false,100d);Set("_domesticSlide",1f);
                if(!(bool)Get("_homePersuasionActive"))throw new Exception("Domestic persuasion did not open automatically");
            }
            if(frame==19)Set("_homePersuasionAge",2.5d);
            if(frame==20)Set("_homePersuasionAge",4.5d);
            if(frame==21){Set("_homePersuasion",((List<DomesticPersuasion>)Get("_homePersuasion")).Where(e=>e.Before.Owner==e.Ruler).ToList());Set("_homePersuasionAge",5d);}
            if(frame==22){Call("UpdateHomePersuasion",4d,false,100d);if((bool)Get("_homePersuasionActive"))throw new Exception("Domestic persuasion did not finish");}
            if(frame==2 && Environment.GetCommandLineArgs().Contains("--war-targets")){
                var net=(CubeNet)Get("_net");var globe=(WorldPreview)Get("_world");Set("_statusGlobe",true);Call("FocusStatusGlobe");Set("_selectedMoveNode",-1);net.SelectedTarget=-1;Call("PrepareDestinationMap");Call("UpdateNodeFocusCircle");
                if(!globe.AttackTargetSpotlight || !net.AttackTargetSpotlight || globe.FocusBrightness!=.65f || globe.FocusCircles.Count==0)throw new Exception("Globe uses the same target spotlight before selection");
                var area=(Rectangle)typeof(Game1).GetProperty("StatusGlobeArea",Flags)!.GetValue(this)!;
                if(!globe.ProjectVisible(world.Routes.Position(home.Cell,home.Center),world.Cells[home.Cell].Normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),area,out var at))throw new Exception("Own-hover globe fixture is visible");
                Set("_pointer",at.ToPoint());net.HoveredNode=home.Id;if(net.EmphasizedNode(home.Id) || !net.DimNode(home.Id))throw new Exception("Own globe node cannot be emphasized");
                using var render=new RenderTarget2D(GraphicsDevice,1920,1080);var previous=GraphicsDevice.GetRenderTargets();GraphicsDevice.SetRenderTarget(render);GraphicsDevice.Clear(Color.Transparent);
                var batch=(SpriteBatch)Get("_spriteBatch");batch.Begin();Call("DrawGlobeCellHover");batch.End();var hoverPixels=new Color[1920*1080];render.GetData(hoverPixels);GraphicsDevice.SetRenderTargets(previous);
                if(hoverPixels.Any(p=>p.A!=0))throw new Exception("Own globe hover draws no highlight brackets");
            }
            if(frame==3 && Environment.GetCommandLineArgs().Contains("--war-targets")){
                var net=(CubeNet)Get("_net");var globe=(WorldPreview)Get("_world");Call("CloseMovement");Call("UpdateNodeFocusCircle");net.HoveredNode=home.Id;
                if(net.AttackTargetSpotlight || globe.AttackTargetSpotlight || net.DimNode(home.Id) || !net.EmphasizedNode(home.Id))throw new Exception("Closing war planning restores ordinary own-node hover and brightness");
            }
            Set("_lastCutInPhase",frame>=3 && !Environment.GetCommandLineArgs().Contains("--war-targets")?"戦闘フェーズ":"内政フェーズ");Set("_cutInPhase","");Set("_animationTime",frame*.2f);
            base.Draw(time);
            var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);
            using var image=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);image.SetData(pixels);
            using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"invasion-ui-{frame}.png"));image.SaveAsPng(file,image.Width,image.Height);
            if(frame==3 && Environment.GetCommandLineArgs().Contains("--war-targets")){Console.WriteLine("PASS: all attackable targets visible, direct map target switching, spotlight at 65% outside brightness in net/globe, no own-hover emphasis, close restores hover, confirmed reservations preserved.");Exit();return;}
            if(++frame==23){File.Delete(Path.Combine(AppContext.BaseDirectory,"tutorial-ui-medals.json"));Console.WriteLine("PASS: rendered invasion, battle recoloring, domestic call, robot chatter, recruitment success and own-part success");Exit();}
        }
        catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;Exit();}
    }
}
