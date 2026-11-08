namespace FolderVerse;
using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

public partial class Game1
{
    internal bool FitWindowOnStartup {get;init;}

    protected override void Initialize()
    {
        if(FitWindowOnStartup && OperatingSystem.IsWindows())FitStartupWindow();
        base.Initialize();
    }

    private void FitStartupWindow()
    {
        int display=SDL_GetWindowDisplayIndex(Window.Handle);
        if(display<0 || SDL_GetDisplayUsableBounds(display,out var area)!=0)return;

        if(SDL_GetWindowBordersSize(Window.Handle,out int top,out int left,out int bottom,out int right)!=0)
        {
            // SDL may not know the native frame size until it shows the window.
            top=48;left=12;bottom=12;right=12;
        }
        int availableWidth=area.Width-left-right,availableHeight=area.Height-top-bottom;
        if(availableWidth<1 || availableHeight<1)return;
        double scale=Math.Min(1,Math.Min(availableWidth/1920d,availableHeight/1080d));
        int width=Math.Max(1,(int)(1920*scale)),height=Math.Max(1,(int)(1080*scale));
        _graphics.PreferredBackBufferWidth=width;
        _graphics.PreferredBackBufferHeight=height;
        _graphics.ApplyChanges();
        // SDL positions the client origin, so include the frame offset.
        Window.Position=new Point(area.X+left+(availableWidth-width)/2,area.Y+top+(availableHeight-height)/2);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SdlRect {public int X,Y,Width,Height;}
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)]
    private static extern int SDL_GetWindowDisplayIndex(IntPtr window);
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)]
    private static extern int SDL_GetDisplayUsableBounds(int display,out SdlRect bounds);
    [DllImport("SDL2",CallingConvention=CallingConvention.Cdecl)]
    private static extern int SDL_GetWindowBordersSize(IntPtr window,out int top,out int left,out int bottom,out int right);
}
