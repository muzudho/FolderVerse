using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new VerifyEnding();game.Run();
sealed class VerifyEnding:Game1
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    WorldSetup w=null!;
    object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
    void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
    object Call(string n,params object[] values)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,values)!;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    string Screen=>Get("_screen").ToString()!;
    void Prepare(bool exhibition,bool demo=false){
        w.SetParticipantCounts(exhibition?0:1,exhibition?2:1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
        foreach(var node in w.Nodes.All)node.Owner=0;
        w.Industry.Queue(1);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_demoActive",demo);Set("_demoFinishStage",0);Set("_autoPlay",exhibition && !demo);
        Check((bool)Call("TryConquestEnding") && Screen=="Ending","Conquest enters ending before production");
    }
    protected override void LoadContent(){
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");
        Set("_autoSaveEnabled",false);Set("_autoSavePath",Path.Combine(AppContext.BaseDirectory,"ending-test-autosave.json.gz"));Set("_conquestOverviewEnabled",false);
        foreach(bool exhibition in new[]{false,true}){
            Prepare(exhibition);var same=w;int turn=w.Population.Turn,nodes=w.Nodes.All.Length;
            var batch=(SpriteBatch)Get("_spriteBatch");GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawEndingUi");batch.End();
            var colors=new Color[1920*1080];GraphicsDevice.GetBackBufferData(colors);using(var image=new Texture2D(GraphicsDevice,1920,1080)){image.SetData(colors);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,exhibition?"exhibition-ending.png":"human-ending.png"));image.SaveAsPng(file,1920,1080);}
            Call("UpdateEnding",9.99d,false);Check(Screen=="Ending","Result remains visible for its full duration");Call("UpdateEnding",.01d,false);
            Check(Screen=="Industry" && ReferenceEquals(same,Get("_setup")) && w.Nodes.All.Length==nodes && w.Population.Turn==turn && w.WorldConquestEndingShown,"Human/exhibition timeout resumes the same conquered world without resetting it");
            Check((bool)Get("_autoPlay")==exhibition,"Exhibition resumes automatic play while human play stays manual");
            Check(!(bool)Call("TryConquestEnding"),"Returning to game cannot repeatedly trigger the same ending");
            Prepare(exhibition);Set("_previousKeyboard",new KeyboardState());
            Call("HandleInputCore",new MouseState(),new KeyboardState(Keys.Enter),new GameTime(TimeSpan.FromSeconds(20),TimeSpan.FromSeconds(.1)),true);
            Check(Screen=="Industry","Enter resumes human/exhibition conquest endings");
        }
        Prepare(false,true);Call("UpdateDemo",10d,true,false,false,false,30d);Check(Screen=="Title" && !(bool)Get("_demoActive"),"Demo conquest returns to title and releases demo control");
        Prepare(false,true);Set("_endingBeforeIndustry",false);Set("_demoFinishStage",3);Set("_demoStepAge",8d);Call("UpdateDemo",.5d,true,false,false,false,40d);
        Check(Screen=="Title" && !(bool)Get("_demoActive"),"Timed demo ending also returns to title");
        Prepare(true);
        Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
        Set("_autoPlay",true);
        Check((bool)Call("AutoPauseInput",true,false),"Exhibition opens the pause menu");
        Set("_pointer",new Point(1500,260));
        Check((bool)Call("AutoPauseInput",true,false) && Screen=="Title","Pause menu exits to title");
        Check(!w.ComputerOnly && !(bool)Get("_autoPlay") && !(bool)Get("_autoPaused") &&
            !(bool)Get("_statusMenuOpen"),"Title clears exhibition and automatic menu state");
        Check(!(bool)Call("AutoPauseInput",true,false),"Title click is available to the start menu");
        Set("_previousKeyboard",new KeyboardState());
        Call("HandleInputCore",new MouseState(),new KeyboardState(Keys.Enter),new GameTime(),true);
        Check((bool)Get("_titleMenuOpen") && !(bool)Get("_statusMenuOpen"),"Enter opens the start menu");
        Console.WriteLine("PASS: ending resume, demo exit and exhibition pause-menu return to title.");Exit();
    }
    protected override void Update(GameTime time){}
}
