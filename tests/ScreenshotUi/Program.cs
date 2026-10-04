using System.Reflection;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using var game=new ScreenshotCheck();game.Run();
sealed class ScreenshotCheck:Game1
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    int frame;string firstPath;Color[] baseline;
    object Get(string name)=>typeof(Game1).GetField(name,Flags)!.GetValue(this)!;
    void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    void Input(params Keys[] keys)=>typeof(Game1).GetMethod("HandleInput",Flags)!.Invoke(this,
        new object[]{new MouseState(),new KeyboardState(keys),new GameTime(TimeSpan.FromSeconds(frame*.1),TimeSpan.FromSeconds(.1)),true});
    protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();}
    protected override void Update(GameTime time)
    {
        if(frame==1 || frame==2)Input(Keys.LeftControl,Keys.P);
        else if(frame==7)Input(Keys.RightControl,Keys.P);
        else Input();
    }
    protected override void Draw(GameTime time)
    {
        base.Draw(new GameTime(TimeSpan.FromSeconds(frame*.1),TimeSpan.FromSeconds(.1)));
        var capture=(ScreenshotCapture)Get("_screenshots");
        var pixels=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(pixels);
        if(frame==0)baseline=pixels;
        if(frame==1)
        {
            firstPath=capture.LastSavedPath;Check(File.Exists(firstPath),"PNG not saved");
            using var stream=File.OpenRead(firstPath);using var saved=Texture2D.FromStream(GraphicsDevice,stream);
            var savedPixels=new Color[pixels.Length];saved.GetData(savedPixels);
            Check(savedPixels.SequenceEqual(baseline),"Saved PNG includes feedback or changed frame");
            Check(!pixels.SequenceEqual(savedPixels),"Visual feedback absent");
            Check((double)Get("_screenshotEffectStarted")==.1,"Feedback did not start on success");
            Check(Get("_shutterInstance")!=null,"Shutter sound not initialized");
        }
        if(frame==2)Check(capture.LastSavedPath==firstPath,"Held chord captured twice");
        if(frame==6)Check((double)Get("_screenshotEffectStarted")+.42<frame*.1,"Effect did not expire");
        if(frame==7)
        {
            Check(capture.LastSavedPath!=firstPath && File.Exists(capture.LastSavedPath),"Right Ctrl capture failed");
            Console.WriteLine("PASS: both Ctrl keys capture; held chord saves once; shared sound initializes; visual feedback starts after clean PNG capture and expires.");Exit();
        }
        frame++;
    }
}
