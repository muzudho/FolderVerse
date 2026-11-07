using FolderVerse;
using System.Reflection;
static class CapacityChecks
{
    static void Capacity(RobotStore s,int n)=>typeof(RobotStore).GetMethod("SetCapacity",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(s,new object[]{n});
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run(){
        foreach(var sample in new[]{(Size:1,Count:1,RestTurns:9,Reserve:0),(Size:2,Count:2,RestTurns:9,Reserve:1),(Size:12,Count:10,RestTurns:9,Reserve:2),(Size:60,Count:10,RestTurns:1,Reserve:10)}){
            var w=new RobotWorld();w.Initialize(1,1);Capacity(w.Nodes[0],sample.Size);
            for(int i=0;i<sample.Count;i++)w.Nodes[0].Add(w.Create(0,RobotParts.Complete));
            var f=w.Workshops[0];f.GenerateLines(123);f.Configure(RobotParts.Head,3);f.Automation=NodeAutomation.Auto;
            w.AdjustNode(0,0,(_,_)=>false);w.AdjustNode(0,0,(_,_)=>false);
            Check(f.ProductionLines.Length>0 && f.ProductionLines[0].RestTurns==sample.RestTurns,"Rest follows the actual storage capacity");
            Check(f.ReservedSlots==sample.Reserve,"Reserved empty slots scale with actual capacity");
        }
        foreach(bool reverse in new[]{false,true}){
            var w=new RobotWorld();w.Initialize(2,1);Capacity(w.Nodes[reverse?1:0],60);
            for(int n=0;n<2;n++)for(int i=0;i<(w.Nodes[n].TotalCapacity==60?10:5);i++)w.Nodes[n].Add(w.Create(0,RobotParts.Complete));
            w.Workshops[0].Automation=NodeAutomation.Auto;w.AdjustNode(0,0,(_,_)=>true,nodeOwner:_=>0);
            Check((w.Transport.Plan(0,0,RobotParts.Complete).Sum(p=>p.Degrees)>0)==reverse,"Transport balances fill ratios instead of raw robot counts");
        }
        Console.WriteLine("PASS: 1/2/12/60-slot factory policies and capacity-relative transport balancing.");
    }
}
