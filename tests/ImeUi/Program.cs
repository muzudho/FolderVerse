using System.Runtime.InteropServices;
using System.Text.Json;
using FolderVerse;
using Microsoft.Xna.Framework;

if(!OperatingSystem.IsWindows()){Console.WriteLine("SKIP: Windows IME integration check.");return;}
string original=Environment.GetEnvironmentVariable("SDL_IME_SHOW_UI");
try
{
    Environment.SetEnvironmentVariable("SDL_IME_SHOW_UI","0");
    string log;
    using(var game=new ImeProbe())
    {
        log=game.OperationLogPath;
        if(Environment.GetEnvironmentVariable("SDL_IME_SHOW_UI")!="1")throw new Exception("Native IME hint not configured before game construction.");
        game.Run();
    }
    var stages=File.ReadLines(log).Select(line=>JsonDocument.Parse(line)).Where(d=>d.RootElement.GetProperty("kind").GetString()=="ime_state")
        .Select(d=>d.RootElement.GetProperty("data").GetProperty("stage").GetString()).ToArray();
    foreach(var stage in new[]{"before_game","game_created","exiting","after_game_disposed"})
        if(!stages.Contains(stage))throw new Exception("Missing IME lifetime snapshot: "+stage);
    Console.WriteLine("PASS: SDL native IME hint, normal startup/disposal and IME snapshots before initialization through after disposal.");
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
finally{Environment.SetEnvironmentVariable("SDL_IME_SHOW_UI",original);}

sealed class ImeProbe:Game1
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetHint([MarshalAs(UnmanagedType.LPUTF8Str)] string name);
    protected override void LoadContent()
    {
        var path=Path.Combine(AppContext.BaseDirectory,"runtimes",RuntimeInformation.ProcessArchitecture==Architecture.Arm64?"win-arm64":"win-x64","native","SDL2.dll");
        var library=NativeLibrary.Load(path);
        try
        {
            var getHint=Marshal.GetDelegateForFunctionPointer<GetHint>(NativeLibrary.GetExport(library,"SDL_GetHint"));
            if(Marshal.PtrToStringUTF8(getHint("SDL_IME_SHOW_UI"))!="1")throw new Exception("Initialized SDL did not read the native IME UI hint.");
        }
        finally{NativeLibrary.Free(library);}
    }
    protected override void Update(GameTime time)=>Exit();
    protected override void Draw(GameTime time)=>GraphicsDevice.Clear(Color.Black);
}
