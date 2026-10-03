namespace FolderVerse;

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class ScreenshotCapture
{
    public string LastSavedPath { get; private set; }
    public string Message { get; private set; } = "";
    public double MessageSeconds { get; private set; }
    private bool _pending;
    public void Request() { _pending=true; }
    public void Update(double seconds) { MessageSeconds=Math.Max(0,MessageSeconds-seconds); }
    public static string OutputDirectory()
    {
        // Debug runs save beside the solution; published games save beside the executable.
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null)
        {
            if(File.Exists(Path.Combine(directory.FullName,"FolderVerse.slnx")))return Path.Combine(directory.FullName,"Screenshots");
            directory=directory.Parent;
        }
        return Path.Combine(AppContext.BaseDirectory,"Screenshots");
    }
    public void CaptureAfterDraw(GraphicsDevice device)
    {
        if(!_pending)return;
        _pending=false;
        try
        {
            int width=device.PresentationParameters.BackBufferWidth,height=device.PresentationParameters.BackBufferHeight;
            var pixels=new Color[checked(width*height)];
            device.GetBackBufferData(pixels);
            using var texture=new Texture2D(device,width,height);texture.SetData(pixels);
            var folder=OutputDirectory();Directory.CreateDirectory(folder);
            var stamp=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).ToString("yyyyMMdd_HHmmss_fff");
            var path=Path.Combine(folder,"FolderVerse_"+stamp+"_"+Guid.NewGuid().ToString("N")[..6]+".png");
            using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write))texture.SaveAsPng(output,width,height);
            LastSavedPath=path;Message="Screenshot saved: "+Path.GetFileName(path);MessageSeconds=3;
        }
        catch(Exception error) when(error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { Message="Screenshot failed ("+error.GetType().Name+"). Check Screenshots folder.";MessageSeconds=6; }
    }
}