using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

sealed class AutoPauseUi:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame;WorldSetup w=null!;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F|BindingFlags.Static)!.Invoke(this,args)!;
    Rectangle Bounds(string n)=>(Rectangle)typeof(Game1).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    void Input(double elapsed,Point? p=null,Keys? key=null){var c=(Rectangle)Call("CanvasBounds");var at=p??new Point(10,10);Call("HandleInput",new MouseState(c.X+at.X*c.Width/1920,c.Y+at.Y*c.Height/1080,0,p.HasValue?ButtonState.Pressed:ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),key.HasValue?new KeyboardState(key.Value):new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100+frame),TimeSpan.FromSeconds(elapsed)),true);}
    void Click(Point p){Input(0);Input(0,p);Input(0);}
    void Ready(){
        Set("_autoPaused",false);Set("_autoPlay",false);Set("_countrySettingsOpen",false);Set("_computerDomesticNodes",null!);Set("_computerMarches",null!);Set("_conquestOverview",false);
        Call("ResetInspector");w.SetParticipantCounts(0,20);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);
        ((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);
    }
    protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);}
    protected override void Update(GameTime time){}
    protected override void Draw(GameTime time){
        if(frame==0){Ready();Input(.5);Check((bool)Get("_autoPlay") && w.ActiveCount==20 && !w.IsHumanPlayer(0) && w.ManualRulers().Length==0,"0/20 automatically starts with no implicit human in slot zero");Input(2.1);Check((int)Get("_autoDomesticStage")==1,"The first computer operates without a user's turn-end click");Set("_autoAge",.6d);}
        if(frame==1){Click(new(10,10));Check((bool)Get("_autoPaused") && (bool)Get("_autoPlay"),"Click opens a paused menu instead of disabling AUTO");var age=Get("_autoAge");var stage=Get("_autoDomesticStage");Input(30);Check(Get("_autoAge").Equals(age) && Get("_autoDomesticStage").Equals(stage),"Paused menu freezes automatic clocks and domestic actions");}
        if(frame==2){Click(Bounds("CountrySettingsMenuItem").Center);Check((bool)Get("_countrySettingsOpen"),"Paused system menu opens country settings");Click(((Rectangle)Call("CountrySettingsCard",3)).Center);Click(Bounds("CountryHumanButton").Center);Input(30);Check(w.PlayerCount==1 && w.ComputerPlayerCount==19 && w.IsHumanPlayer(3) && (bool)Get("_autoPaused"),"A human can be added while the game stays paused");}
        if(frame==3){Click(Bounds("CountrySettingsClose").Center);Check((bool)Get("_autoPaused") && !(bool)Get("_countrySettingsOpen"),"Closing settings returns to paused menu");Click(Bounds("AutoPauseClose").Center);Check(!(bool)Get("_autoPaused") && !(bool)Get("_autoPlay") && w.PlayerSlot==3 && w.ControllerOf(3)==3,"X returns control to the newly selected human");}
        if(frame==4){
            Ready();w.Industry.Queue(1);Call("BeginIndustry");Input(0);Click(new(10,10));Check((bool)Get("_autoPaused") && Get("_screen").ToString()=="Industry","Industry can be paused without changing the underlying phase");
            Click(Bounds("CountrySettingsMenuItem").Center);Input(30);Check((bool)Get("_countrySettingsOpen"),"Country settings works while the saved game screen is not WorldStatus");
        }
        if(frame==5){
            Click(Bounds("CountrySettingsClose").Center);Click(Bounds("AutoPauseClose").Center);Check(!(bool)Get("_autoPaused") && (bool)Get("_autoPlay") && Get("_screen").ToString()=="Industry","X resumes zero-human industry in place");
            Ready();Input(0);var home=w.Nodes.Current(0);Set("_computerDomesticNodes",new[]{home});Set("_computerDomesticIndex",0);Set("_computerDomesticAge",.8d);Set("_computerViewRuler",0);Set("_computerViewLeader",0);
            Click(new(10,10));Input(30);Check((double)Get("_computerDomesticAge")==.8,"Computer-operation playback is frozen before its updater can consume the click");
        }
        if(frame==6){
            Click(Bounds("AutoPauseClose").Center);Input(.1);Check((double)Get("_computerDomesticAge")>.8,"Computer playback resumes at its saved position");
            Ready();Set("_conquestOverviewEnabled",true);Call("BeginConquestOverview");Input(.2);Click(new(10,10));var age=Get("_conquestAge");Input(30);Check((bool)Get("_autoPaused") && Get("_conquestAge").Equals(age),"Conquest overview also pauses before its update and drawing");Set("_conquestOverviewEnabled",false);
        }
        if(frame==7){
            Ready();w.SetHumanPlayer(0,true);Call("StartAutoPlay");Click(new(10,10));Click(Bounds("AutoPauseClose").Center);Check((bool)Get("_autoPlay") && !(bool)Get("_autoPaused"),"Existing human's optional AUTO also resumes with X");
            Input(0,key:Keys.Escape);Check((bool)Get("_autoPaused"),"Escape opens pause menu");Input(0);Input(0,key:Keys.Escape);Check(!(bool)Get("_autoPaused"),"Escape can resume the main pause menu");
        }
        if(frame==8){
            Ready();w.SetTerritoryRights(456);Call("BeginSelection");Check(Get("_screen").ToString()=="LandConquest" && !w.IsHumanPlayer(0),"Zero humans skip human selection and start automatic land allocation");
            var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();
            Click(Bounds("LandSkipOthers").Center);
            Check((bool)Get("_skipOtherLandTurns") && !(bool)Get("_autoPaused") && !(bool)Get("_autoPlay") &&
                Get("_screen").ToString()=="Fortune","Land skip works in a zero-human game before AUTO starts");
        }
        if(frame==9){
            Click(new(10,10));
            Check(!(bool)Get("_autoPaused") && !(bool)Get("_autoPlay"),"Fortune clicks do not pause AUTO");
            Click(Bounds("FortuneConfirm").Center);
            Check((bool)Get("_autoPlay") && Get("_screen").ToString()=="WorldStatus","Confirming Fortune starts AUTO after world construction");
            Click(new(10,10));Check((bool)Get("_autoPaused"),"AUTO can be paused after world construction");
        }
        if(frame==10){
            Ready();Input(0);var home=w.Nodes.All.First(n=>n.Owner==1);Call("BeginComputerView",1);Set("_computerDomesticNodes",new[]{home});Set("_computerDomesticIndex",0);Set("_computerDomesticAge",.5d);
            Click(new(10,10));Click(Bounds("CountrySettingsMenuItem").Center);Click(((Rectangle)Call("CountrySettingsCard",3)).Center);Click(Bounds("CountryHumanButton").Center);Click(Bounds("CountrySettingsClose").Center);Click(Bounds("AutoPauseClose").Center);
            Call("UpdateComputerDomestic",0d,true);Check(w.PlayerSlot==3 && !(bool)Get("_autoPlay") && w.Campaign.Phase==CampaignPhase.Delivery,"Pending CPU presentation restores the newly added human rather than its previous spectator");
        }
        if(frame==11){Ready();Input(0);Click(new(10,10));Click(Bounds("QuitMenuItem").Center);Check(Get("_screen").ToString()=="Title" && !(bool)Get("_autoPaused"),"Paused menu can stop the exhibition and return to title");}
        if(frame==12){
            Ready();
            foreach(var node in w.Nodes.All.Where(n=>n.Owner==0))node.FunctionUnits=0;
            Input(2.1);
            Check((int)Get("_populationNode")==-1,"AUTO does not open an inspector for a nonfunctional point");
            Input(2.1);
            Check(w.Campaign.Phase==CampaignPhase.Delivery,"AUTO proceeds to delivery when no point can be inspected");
        }
        if(frame==13){
            Ready();Input(2.1);
            Check((int)Get("_populationNode")>=0,"AUTO opens a functional point");
            Input(2.1);Call("ResetInspector");Input(2.1);
            Check(w.Campaign.Phase==CampaignPhase.Delivery,"AUTO proceeds to delivery if the inspector is cleared before opening the hangar");
        }
        base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame),TimeSpan.Zero));
        var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
        using(var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"auto-pause-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);}
        if(frame++==13){Console.WriteLine("PASS: AUTO pause, takeover, resume, and delivery with no valid inspector.");Exit();}
    }
}
