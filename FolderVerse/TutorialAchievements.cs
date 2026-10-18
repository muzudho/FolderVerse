namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
public sealed class TutorialAchievements
{
    public const string FirstAttack="tutorial-step-1-first-attack";
    private readonly string _path;
    public Dictionary<string,DateTimeOffset> Unlocked {get;private set;}=new();
    public TutorialAchievements(string path){_path=path;if(File.Exists(path))Unlocked=JsonSerializer.Deserialize<Dictionary<string,DateTimeOffset>>(File.ReadAllText(path))??new();}
    public bool Has(string id)=>Unlocked.ContainsKey(id);
    public bool Award(string id){
        if(Has(id))return false;
        var next=new Dictionary<string,DateTimeOffset>(Unlocked){{id,DateTimeOffset.UtcNow}};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path)));string pending=_path+".tmp";
        try{File.WriteAllText(pending,JsonSerializer.Serialize(next));if(File.Exists(_path))File.Replace(pending,_path,null);else File.Move(pending,_path);Unlocked=next;return true;}
        finally{if(File.Exists(pending))File.Delete(pending);}
    }
    public void Clear(){
        var next=Unlocked.Where(p=>!p.Key.StartsWith("tutorial-",StringComparison.Ordinal)).ToDictionary(p=>p.Key,p=>p.Value);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path)));string pending=_path+".tmp";
        try{File.WriteAllText(pending,JsonSerializer.Serialize(next));if(File.Exists(_path))File.Replace(pending,_path,null);else File.Move(pending,_path);Unlocked=next;}
        finally{if(File.Exists(pending))File.Delete(pending);}
    }
    public static bool Attacked(WorldSetup w)=>w.Campaign.RobotBattles.Scenes.Any(b=>AttackedScene(w,b));
    public static bool AttackedScene(WorldSetup w,BattleState b)=>w.TutorialStep==1 &&
        b.Armies.Any(a=>a.Owner==0) && b.Armies.Any(a=>!w.Relations.Allied(a.Owner,0)) &&
        w.Campaign.RobotBattles.LastMarches.Any(m=>m.Owner==0 &&
            (b.Kind==BattleKind.Node?b.TargetNode==m.Target && m.Source!=m.Target && b.Defender!=0:
             (b.SourceNode==m.Source && b.TargetNode==m.Target || b.SourceNode==m.Target && b.TargetNode==m.Source)));
}
