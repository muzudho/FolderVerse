namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
public sealed class TutorialAchievements
{
    // Planet medals are separate from shared action achievements such as captive choices.
    public const string PetitstellaLessons="tutorial-planet-petitstella-lessons";
    public const string PetitstellaConquest="tutorial-planet-petitstella-conquest";
    public static bool PetitstellaLessonsCompleted(WorldSetup w)=>w.TutorialStep==1 && w.TutorialMapBeforeWar && w.TutorialMapStage>=6 && w.TutorialAttackCompleted && w.TutorialPocketed && w.TutorialMoved;
    public static bool PetitstellaWorldConquered(WorldSetup w)=>w.TutorialStep==1 && w.TutorialMapBeforeWar && w.Cells.Length>0 && w.Relations.Powered[0] && w.Relations.ProvisionalRanking(w).Any(r=>r.Ruler==0 && r.Cells==w.Cells.Length);
    public const string FirstAttack="tutorial-step-1-first-attack";
    public const string CaptiveRecruited="tutorial-captive-recruited";
    public const string CaptiveReleased="tutorial-captive-released";
    public const string CaptiveBatteryRemoved="tutorial-captive-battery-removed";
    public static string CaptiveChoiceId(DefeatedChoice choice)=>choice switch{
        DefeatedChoice.Recruit=>CaptiveRecruited,DefeatedChoice.Release=>CaptiveReleased,DefeatedChoice.RemoveBattery=>CaptiveBatteryRemoved,_=>throw new ArgumentOutOfRangeException(nameof(choice))
    };
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
        // The battle itself proves hostile contact; post-battle alliances must not erase it.
        b.Armies.Any(a=>a.Owner==0) && b.Armies.Any(a=>a.Owner!=0) &&
        w.Campaign.RobotBattles.LastMarches.Any(m=>m.Owner==0 &&
            (b.Kind==BattleKind.Node?b.TargetNode==m.Target && b.Defender!=0:
             (b.SourceNode==m.Source && b.TargetNode==m.Target || b.SourceNode==m.Target && b.TargetNode==m.Source)));
}
