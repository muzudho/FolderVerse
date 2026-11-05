using FolderVerse;

static class NoRepeatChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static RobotWorld World(int every=3,int rest=2)
    {
        var w=new RobotWorld();w.Initialize(2,1);
        foreach(var line in w.Workshops){line.Configure(RobotParts.Head,2);line.AutoAssemblyDisabled=true;}
        w.Workshops[0].SetRest(every,rest);w.Workshops[0].SetDoNotRepeat(true);return w;
    }
    static void Tick(RobotWorld w,int count){for(int i=0;i<count;i++)w.AdvanceIndustry(n=>0);}
    public static void Run()
    {
        var w=World();var line=w.Workshops[0];
        Tick(w,6);Check(w.Nodes[0].Count==3 && line.RestRemaining==6 && !line.CycleFinished,"All configured repeats finish before rest");
        Tick(w,5);Check(!line.CycleFinished && line.RestRemaining==1,"No early stop during final rest tick");
        line.Paused=true;Tick(w,2);Check(line.RestRemaining==1 && !line.CycleFinished,"Pause preserves remaining rest");line.Paused=false;
        Tick(w,1);Check(line.CycleFinished && line.CycleProgress==line.CycleTurns && line.LaneProgress==8,"Rest completion stops exactly at lane's right end");
        int count=w.Nodes[0].Count,other=w.Nodes[1].Count;Tick(w,6);
        Check(w.Nodes[0].Count==count && w.Nodes[1].Count>other,"Only the configured lane stops permanently");
        var restored=AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(w));Tick(restored,2);
        Check(restored.Workshops[0].DoNotRepeat && restored.Workshops[0].CycleFinished && restored.Nodes[0].Count==count,"Completed stop survives save and resume");
        line.SetDoNotRepeat(false);Tick(w,2);Check(!line.CycleFinished && w.Nodes[0].Count==count+1,"Unchecking restarts manufacturing");
        var legacy=AutoSaveStore.Capture(restored);
        foreach(var item in legacy.Objects){item.Fields?.Remove("<DoNotRepeat>k__BackingField");item.Fields?.Remove("<CycleFinished>k__BackingField");}
        var old=AutoSaveStore.Restore<RobotWorld>(legacy);Check(!old.Workshops[0].DoNotRepeat && !old.Workshops[0].CycleFinished,"Old saves retain normal repeating behavior");
        w=World(1,1);line=w.Workshops[0];line.SetReservedSlots(12);Tick(w,4);
        Check(line.Waiting && !line.CycleFinished && w.Nodes[0].Count==0,"Full hangar does not falsely complete a one-cycle run");
        line.SetReservedSlots(0);Tick(w,1);Check(w.Nodes[0].Count==1 && line.RestRemaining==3,"Freed capacity starts final rest after actual shipment");
        Tick(w,3);Check(line.CycleFinished,"Deferred shipment still stops after rest");
        w=World(1,1);line=w.Workshops[0];Tick(w,3);
        var mid=AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(w));Tick(mid,2);Check(mid.Workshops[0].CycleFinished && mid.Nodes[0].Count==1,"Mid-rest save resumes the final two ticks only");
        line.Configure(RobotParts.Body,2);Check(line.DoNotRepeat && !line.CycleFinished,"New product rearms one-cycle run");
        Tick(w,2);Check(line.CycleFinished && line.LaneProgress==2,"Without a rest stretch, one-shot production stops at production end");
        Console.WriteLine("PASS: no-repeat count/rest timing, capacity wait, paused rest, lane independence, restart, product changes and current/legacy save state.");
    }
}
