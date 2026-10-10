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
    void Tick(double elapsed,bool click=false)=>typeof(Game1).GetMethod("UpdateDemo",Flags)!.Invoke(this,new object[]{elapsed,true,click,false,false});
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    protected override void LoadContent()
    {
        Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
        Tick(9);Check(!(bool)Get("_demoActive"),"Demo started too soon");Tick(0,true);Tick(9);Check(!(bool)Get("_demoActive"),"Click did not reset title timer");Tick(1);Check((bool)Get("_demoActive"),"Demo did not start at 10 seconds");
        bool transport=false,battle=false;
        for(int i=0;i<180;i++){Tick(1);transport|=Get("_screen").ToString()=="Transport";battle|=Get("_screen").ToString()=="Battle";}
        Check(transport,"Demo did not enter propagation");Check(battle,"Demo did not enter battle");
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
