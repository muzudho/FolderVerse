namespace FolderVerse;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class OperationLog:IDisposable
{
    private StreamWriter _writer;
    private readonly Stopwatch _clock=Stopwatch.StartNew();
    private readonly string _session=Guid.NewGuid().ToString("N");
    private long _sequence;
    public string FilePath {get;private set;}
    public string Error {get;private set;}
    public long ElapsedMilliseconds=>_clock.ElapsedMilliseconds;
    private static readonly JsonSerializerOptions Options=new(){Encoder=JavaScriptEncoder.UnsafeRelaxedJsonEscaping,NumberHandling=JsonNumberHandling.AllowNamedFloatingPointLiterals};
    internal static string StateKey(object state)=>JsonSerializer.Serialize(state,Options);
    public OperationLog(string directory=null)
    {
        string primary=directory??Path.Combine(Path.GetDirectoryName(ScreenshotCapture.OutputDirectory()),"Logs");
        if(!Open(primary) && directory==null)
            Open(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FolderVerse","Logs"));
        Write("session_start",new{processId=Environment.ProcessId,version=typeof(Game1).Assembly.GetName().Version?.ToString(),
            buildTimeUtc=File.GetLastWriteTimeUtc(typeof(Game1).Assembly.Location).ToString("O")});
    }
    private bool Open(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            string name=$"operations_{DateTimeOffset.Now:yyyyMMdd_HHmmss_fff}_{_session[..8]}.jsonl";
            FilePath=Path.GetFullPath(Path.Combine(directory,name));
            _writer=new(new FileStream(FilePath,FileMode.CreateNew,FileAccess.Write,FileShare.ReadWrite),new UTF8Encoding(false)){AutoFlush=true};
            Error=null;return true;
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or NotSupportedException)
        {Error=e.Message;Debug.WriteLine("Operation log: "+Error);return false;}
    }
    public void Write(string kind,object data)
    {
        if(_writer==null)return;
        try
        {
            _writer.WriteLine(JsonSerializer.Serialize(new{schemaVersion=1,sessionId=_session,sequence=++_sequence,
                time=DateTimeOffset.Now.ToString("O"),elapsedMs=_clock.ElapsedMilliseconds,kind,data},Options));
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            Error=e.Message;Debug.WriteLine("Operation log: "+Error);var failed=_writer;_writer=null;
            try{failed?.Dispose();}catch(Exception closeError) when(closeError is IOException or UnauthorizedAccessException or ObjectDisposedException){Debug.WriteLine(closeError.Message);}
        }
    }
    public void Dispose()
    {
        Write("session_end",new{});
        try{_writer?.Dispose();}
        catch(Exception e) when(e is IOException or UnauthorizedAccessException){Error=e.Message;Debug.WriteLine("Operation log: "+Error);}
        _writer=null;
    }
}
