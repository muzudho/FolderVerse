namespace FolderVerse;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;

// Read-only snapshots. Never toggle another application's IME to guess at a repair.
public static class WindowsImeDiagnostics
{
    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public uint Size,Flags;
        public IntPtr Active,Focus,Capture,MenuOwner,MoveSize,Caret;
        public int Left,Top,Right,Bottom;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint thread,ref GuiThreadInfo info);
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint thread);
    [DllImport("imm32.dll")] private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr window);
    [DllImport("user32.dll",EntryPoint="SendMessageTimeoutW",SetLastError=true)]
    private static extern IntPtr SendMessageTimeout(IntPtr window,uint message,UIntPtr parameter,IntPtr value,uint flags,uint timeout,out UIntPtr result);
    public static object ReadForeground()
    {
        if(!OperatingSystem.IsWindows())return new{supported=false};
        return ReadWindow(GetForegroundWindow());
    }
    public static object[] ReadVisualStudio()
    {
        if(!OperatingSystem.IsWindows())return Array.Empty<object>();
        var states=new List<object>();
        foreach(var process in Process.GetProcessesByName("devenv"))
        {
            using(process)
            {
                try{states.Add(new{processId=process.Id,state=ReadWindow(process.MainWindowHandle)});}
                catch(InvalidOperationException){/* Visual Studio exited while reading its window. */}
            }
        }
        return states.ToArray();
    }
    private static object ReadWindow(IntPtr window)
    {
        if(window==IntPtr.Zero)return new{supported=true,foreground=false};
        uint thread=GetWindowThreadProcessId(window,out uint process);
        var info=new GuiThreadInfo{Size=(uint)Marshal.SizeOf<GuiThreadInfo>()};
        var focus=GetGUIThreadInfo(thread,ref info) && info.Focus!=IntPtr.Zero?info.Focus:window;
        var ime=ImmGetDefaultIMEWnd(focus);
        long? Read(uint command)
        {
            if(ime==IntPtr.Zero)return null;
            // Bound the wait if the foreground application is paused in a debugger.
            return SendMessageTimeout(ime,0x0283,new UIntPtr(command),IntPtr.Zero,0x0002,50,out var result)!=IntPtr.Zero?(long?)result.ToUInt64():null;
        }
        return new{supported=true,foreground=window==GetForegroundWindow(),processId=process,threadId=thread,window=window.ToInt64(),focus=focus.ToInt64(),
            keyboardLayout=GetKeyboardLayout(thread).ToInt64().ToString("X"),imeWindow=ime.ToInt64(),
            reportedOpen=Read(0x0005),conversionMode=Read(0x0001),sentenceMode=Read(0x0003)};
    }
}
