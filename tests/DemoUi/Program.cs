using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using var game=new DemoCheck();game.Run();
sealed class DemoCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    bool checkedDemo;
    bool drawn;
    object Get(string n)=>typeof(Game1).GetField(n,Flags)!.GetValue(this)!;
    void Tick(double elapsed,bool click=false,bool active=true)=>typeof(Game1).GetMethod("UpdateDemo",Flags)!.Invoke(this,new object[]{elapsed,active,click,false,false});
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        Tick(9);Check(!(bool)Get("_demoActive"),"Demo started too soon");Tick(0,true);Tick(9);Check(!(bool)Get("_demoActive"),"Click did not reset title timer");Tick(1);Check((bool)Get("_demoActive"),"Demo did not start at 10 seconds");
        Check(Get("_screen").ToString()=="Title","Intro should remain on title");
        Tick(1.8);Check((float)Get("_demoSwing")>0,"START click swing missing");Tick(.4);Check(Get("_screen").ToString()=="Rolling","START intro did not launch demo");
        bool transport=false,battle=false;
        for(int i=0;i<180;i++){Tick(1);transport|=Get("_screen").ToString()=="Transport";battle|=Get("_screen").ToString()=="Battle";}
        Check(transport,"Demo did not enter propagation");Check(battle,"Demo did not enter battle");
        double before=(double)Get("_demoAge");Tick(1,active:false);Check((double)Get("_demoAge")>before,"Inactive demo stopped");
        var screenField=typeof(Game1).GetField("_screen",Flags)!;
        var world=(WorldSetup)Get("_setup");
        world.Relations.Pending.Clear();
        foreach(int personality in new[]{3,9,11,13})
        {
            var keyword=ConquerorCatalog.Keywords.First(k=>k.Personality==personality);
            world.Looks[1]=new(keyword.BaseId,keyword.VariantId);
            world.Relations.Released[1]=false;world.Relations.SetSuperior(1,-1);
            world.Relations.Pending.Add(new(1,0,world.Nodes.Current(1).Id));
            screenField.SetValue(this,Enum.Parse(screenField.FieldType,"Disposition"));
            typeof(Game1).GetField("_demoStepAge",Flags)!.SetValue(this,0d);
            for(int frame=0;frame<55;frame++)Tick(1d/18,active:false);
            Check(world.Relations.Pending.Count==0,"Demo repeatedly selected a refused recruitment");
            Check(world.Relations.Accepts(world,1)?world.Relations.Superiors[1]==0:world.Relations.Released[1],"Demo disposition choice incorrect");
        }
        var phaseSeconds=(double)typeof(Game1).GetField("RobotFrameSeconds",BindingFlags.Static|BindingFlags.NonPublic)!.GetRawConstantValue()!;
        var sampled=Enumerable.Range(0,27).Select(i=>(int)(i/18d/phaseSeconds)).ToArray();
        Check(Enumerable.Range(0,6).All(phase=>sampled.Count(i=>i==phase)>=4),"18 FPS skips combat phases");
        screenField.SetValue(this,Enum.Parse(screenField.FieldType,"WorldStatus"));
        typeof(Game1).GetField("_demoAge",Flags)!.SetValue(this,1200d);
        Tick(.1,active:false);Check((int)Get("_demoFinishStage")==1,"20-minute ending route not started");
        Tick(1);Tick(.4);Check((bool)Get("_statusMenuOpen"),"Demo did not click menu");
        Tick(1);Tick(.4);Check(Get("_screen").ToString()=="Ending","Demo did not click ending menu");
        Tick(8);Tick(.4);Check(Get("_screen").ToString()=="Title" && !(bool)Get("_demoActive"),"Ending return button did not end demo");
        Tick(10,active:false);Check((bool)Get("_demoActive"),"Inactive title did not restart demo loop");
        typeof(Game1).GetField("_pointer",Flags)!.SetValue(this,new Point(960,540));Tick(0,true);Check((float)Get("_demoFade")==0,"Click did not begin fade");Tick(.81);Check(Get("_screen").ToString()=="Title" && !(bool)Get("_demoActive"),"Demo did not return to title");Tick(.6);Check((float)Get("_demoFade")<0,"Fade did not finish");Tick(9);Check(!(bool)Get("_demoActive"),"Idle timer did not reset after demo");
        Console.WriteLine("PASS: 10-second idle, click reset, automatic world/cast/placement/propagation/battle, demo click fade and title return.");Tick(1);checkedDemo=true;
    }
    protected override void Update(GameTime time){if(checkedDemo && drawn)Exit();}
    protected override void Draw(GameTime time)
    {
        base.Draw(time);var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);
        using var tex=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);tex.SetData(pixels);
        using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"demo.png"));tex.SaveAsPng(output,tex.Width,tex.Height);drawn=true;
    }
}
