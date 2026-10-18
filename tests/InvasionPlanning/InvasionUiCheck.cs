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
        ((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);
        Set("_statusGlobe",false);Screen("WorldStatus");Call("OpenWarPlanning");Set("_domesticSlide",1f);
    }
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time)
    {
        try
        {
            if(frame==1){Call("SelectMoveNode",target.Id);var net=(CubeNet)Get("_net");var panel=(Rectangle)typeof(Game1).GetProperty("NetPanel",Flags)!.GetValue(this)!;net.SetCenter(world,world.Cells[home.Cell].Face);net.FitNodesAtSource(world,home,panel,8);var middle=(net.NodePosition(world,home,panel)+net.NodePosition(world,target,panel))/2;net.Drag(new Vector2(1430,550)-middle);}
            if(frame==2){Call("OpenInvasionPicker",0);Set("_pointer",new Point(400,350));Call("InvasionPickerInput",true,false);}
            if(frame==3)
            {
                Set("_invasionPicker",-1);
                var b=new BattleState(42,new(){PersuasionPercent=100}){Id=100,TargetNode=target.Id,Kind=BattleKind.Edge};b.Armies.Add(new(){Owner=0,Side=0});b.Armies.Add(new(){Owner=1,Side=2});
                b.Units.Add(new(){Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(3,4),Daggers=0,Rifles=0});b.Units.Add(new(){Id=999,Owner=1,Position=new(5,4)});
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
            Set("_lastCutInPhase",frame>=3?"戦闘フェーズ":"内政フェーズ");Set("_cutInPhase","");Set("_animationTime",frame*.2f);
            base.Draw(time);
            var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);
            using var image=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);image.SetData(pixels);
            using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"invasion-ui-{frame}.png"));image.SaveAsPng(file,image.Width,image.Height);
            if(++frame==18){File.Delete(Path.Combine(AppContext.BaseDirectory,"tutorial-ui-medals.json"));Console.WriteLine("PASS: rendered target selection, pulsing target, robot modal, persuasion range and conversion success, tutorial guide, medal clear button, flag countdown and timed achievement cut-in");Exit();}
        }
        catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;Exit();}
    }
}
