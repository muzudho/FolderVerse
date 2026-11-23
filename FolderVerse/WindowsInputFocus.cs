namespace FolderVerse;
using System;
using System.Runtime.InteropServices;

// SDL's active state can lag behind a click that activates another application.
internal static class WindowsInputFocus
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    public static bool IsForeground {
        get {
            if(!OperatingSystem.IsWindows())return true;
            var window=GetForegroundWindow();if(window==IntPtr.Zero)return false;
            GetWindowThreadProcessId(window,out uint process);
            return process==(uint)Environment.ProcessId;
        }
    }
}
