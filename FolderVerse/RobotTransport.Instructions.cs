namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class TransportInstruction
{
    public int Number {get;internal set;}
    public int Target {get;internal set;}
    public int Degrees {get;internal set;}
    // Bit 1..7 selects an alternative parts combination, not a union of parts.
    public int Variants {get;internal set;}=1<<7;
    public bool Cheap {get;internal set;}
    public bool Disposal {get;internal set;}
    public int Limit {get;internal set;}=1;
    public int Sent {get;internal set;}
    public bool Complete=>Sent>=Limit;
}
public sealed partial class RobotTransport
{
    private readonly Dictionary<(int Node,int Owner),List<TransportInstruction>> _instructions=new();
    private readonly Queue<Dictionary<(int Source,int Target,bool Disposal),int>> _flowTurns=new();
    private readonly Dictionary<(int Source,int Target,bool Disposal),int> _pendingFlow=new();
    public bool HasInstructions(int node,int owner)=>_instructions.ContainsKey((node,owner));
    public IReadOnlyList<TransportInstruction> Instructions(int node,int owner)=>_instructions.TryGetValue((node,owner),out var list)?list.AsReadOnly():Array.Empty<TransportInstruction>();
    public void UseInstructions(int node,int owner){RobotWorld.Validate(owner,RobotParts.Head);if(node<0)throw new ArgumentOutOfRangeException(nameof(node));_instructions.TryAdd((node,owner),new());}
    public TransportInstruction AddInstruction(int node,int owner,int target)
    {
        UseInstructions(node,owner);var list=_instructions[(node,owner)];
        if(list.Count>=16)throw new InvalidOperationException("At most 16 instructions.");
        if(target<0 || target==node)throw new ArgumentException("Invalid destination.");
        int number=Enumerable.Range(1,16).First(n=>list.All(r=>r.Number!=n));
        int share=360/(list.Count+1);var rule=new TransportInstruction{Number=number,Target=target,Degrees=0};list.Add(rule);list.Sort((a,b)=>a.Number.CompareTo(b.Number));SetInstructionRate(node,owner,number,share);return rule;
    }
    public void DeleteInstruction(int node,int owner,int number){if(!_instructions.TryGetValue((node,owner),out var list))return;list.RemoveAll(r=>r.Number==number);NormalizeInstructions(list);}
    public void ConfigureInstruction(int node,int owner,int number,int target,int variants,bool cheap,bool disposal,int limit)
    {
        if(target<0 || target==node || (variants&~254)!=0 || variants==0 || limit<1 || limit>9999)throw new ArgumentException("Invalid instruction.");
        var rule=Instructions(node,owner).Single(r=>r.Number==number);
        bool changed=rule.Target!=target || rule.Variants!=variants || rule.Cheap!=cheap || rule.Disposal!=disposal;
        rule.Target=target;rule.Variants=variants;rule.Cheap=cheap;rule.Disposal=disposal;rule.Limit=limit;if(changed)rule.Sent=0;
    }
    public void RestartInstruction(int node,int owner,int number)=>Instructions(node,owner).Single(r=>r.Number==number).Sent=0;
    private static void NormalizeInstructions(List<TransportInstruction> rules)
    {
        if(rules.Count==0)return;int sum=rules.Sum(r=>r.Degrees);
        var values=rules.Select(r=>sum==0?360/rules.Count:(int)((long)r.Degrees*360/sum)).ToArray();
        int left=360-values.Sum();foreach(int i in Enumerable.Range(0,rules.Count).OrderByDescending(i=>sum==0?0:(long)rules[i].Degrees*360%sum).ThenBy(i=>i).Take(left))values[i]++;
        for(int i=0;i<rules.Count;i++)rules[i].Degrees=values[i];
    }
    public void SetInstructionRate(int node,int owner,int number,int degrees)
    {
        var rules=_instructions[(node,owner)];var selected=rules.Single(r=>r.Number==number);
        if(rules.Count==1){selected.Degrees=360;return;}
        degrees=Math.Clamp(degrees,0,360);var others=rules.Where(r=>r!=selected).ToArray();int sum=others.Sum(r=>r.Degrees),budget=360-degrees;
        var values=others.Select(r=>sum==0?budget/others.Length:(int)((long)r.Degrees*budget/sum)).ToArray();
        foreach(int i in Enumerable.Range(0,others.Length).OrderByDescending(i=>sum==0?0:(long)others[i].Degrees*budget%sum).ThenBy(i=>i).Take(budget-values.Sum()))values[i]++;
        selected.Degrees=degrees;for(int i=0;i<others.Length;i++)others[i].Degrees=values[i];
    }
    private sealed record InstructionChoice(RobotShipment Shipment,TransportInstruction Instruction);
    private Dictionary<long,Queue<InstructionChoice>> BuildInstructionChoices(RobotWorld world,Func<int,int,bool> connected,SeedRandom random,Func<int,bool> locked,Func<int,int,bool> canEnter)
    {
        var result=new Dictionary<long,Queue<InstructionChoice>>();
        for(int node=0;node<world.Nodes.Length;node++){
            if(locked?.Invoke(node)==true)continue;
            foreach(var robot in world.Nodes[node].Robots.OrderBy(r=>r.Id)){
                if(world.Workshops[node].DisposalTarget==robot.Id || world.PendingEdits.Any(e=>e.Ids.Contains(robot.Id)))continue;
                var options=new List<InstructionChoice>();
                foreach(var rule in Instructions(node,robot.Owner)){
                    if(rule.Complete || rule.Degrees==0 || rule.Target>=world.Nodes.Length || !connected(node,rule.Target) || locked?.Invoke(rule.Target)==true || !(canEnter?.Invoke(robot.Owner,rule.Target)??true) || robot.Disposal && !rule.Disposal)continue;
                    int mask=Enumerable.Range(1,7).Where(m=>(rule.Variants&(1<<m))!=0 && ((int)robot.Parts&m)==m && (!world.Workshops[node].AutoAssemblyDisabled || (RobotParts)m==robot.Parts) && (rule.Cheap?(robot.CheapParts&(RobotParts)m)==(RobotParts)m:(robot.CheapParts&(RobotParts)m)==0))
                        .OrderByDescending(m=>PartCount((RobotParts)m)).ThenByDescending(m=>m).FirstOrDefault();
                    if(mask!=0)options.Add(new(new(node,rule.Target,robot.Id,(RobotParts)mask,rule.Disposal),rule));
                }
                var queue=new Queue<InstructionChoice>();
                // Weighted sampling without replacement permits rerolling if the receiver cannot fit.
                while(options.Count>0){int roll=random.Next(options.Sum(o=>o.Instruction.Degrees)),sum=0;var chosen=options.First(o=>{sum+=o.Instruction.Degrees;return roll<sum;});queue.Enqueue(chosen);options.Remove(chosen);}
                if(queue.Count>0)result[robot.Id]=queue;
            }
        }
        return result;
    }
    private static int PartCount(RobotParts parts)=>((parts&RobotParts.Head)!=0?1:0)+((parts&RobotParts.Body)!=0?1:0)+((parts&RobotParts.Legs)!=0?1:0);
    private void RecordTransportFlow(RobotWorld world,IEnumerable<RobotShipment> shipments){foreach(var s in shipments){bool disposal=s.Disposal || world.Nodes[s.Source].Robots.Single(r=>r.Id==s.RobotId).Disposal;var key=(s.Source,s.Target,disposal);_pendingFlow[key]=_pendingFlow.GetValueOrDefault(key)+PartCount(s.Parts);}}
    private void FinishTransportTurn(){_flowTurns.Enqueue(new(_pendingFlow));_pendingFlow.Clear();while(_flowTurns.Count>5)_flowTurns.Dequeue();}
    public double AverageParts(int source,int target,bool disposal)=>_flowTurns.Sum(t=>t.GetValueOrDefault((source,target,disposal)))/5.0;
    public static int FlowBand(double mean)=>mean<=0?0:mean<=9?1:mean<=18?2:mean<=27?3:4;
    private void ResetInstructions(){_instructions.Clear();_flowTurns.Clear();_pendingFlow.Clear();}
}
