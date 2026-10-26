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
    // Six individual alternatives: regular head/body/legs, cheap head/body/legs.
    public int? QualityParts {get;internal set;}
    public int PartChoices=>QualityParts ?? RobotTransport.IndividualChoices(Variants,Cheap);
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
    public int RestDegrees(int node,int owner)=>Math.Max(0,360-Instructions(node,owner).Sum(r=>r.Degrees));
    public void UseInstructions(int node,int owner){RobotWorld.Validate(owner,RobotParts.Head);if(node<0)throw new ArgumentOutOfRangeException(nameof(node));_instructions.TryAdd((node,owner),new());}
    // Player-facing instructions select individual parts; the legacy combination API remains available to AI.
    public static int SinglePartVariants(int alternatives)
    {
        int parts=0;for(int mask=1;mask<=7;mask++)if((alternatives&(1<<mask))!=0)parts|=mask;
        return ((parts&4)!=0?1<<4:0)|((parts&2)!=0?1<<2:0)|((parts&1)!=0?1<<1:0);
    }
    public void UseSinglePartInstructions(int node,int owner)
    {
        UseInstructions(node,owner);
        foreach(var rule in _instructions[(node,owner)]){
            if(rule.QualityParts.HasValue)continue;
            int variants=SinglePartVariants(rule.Variants);
            bool changed=variants!=rule.Variants;
            rule.Variants=variants;rule.QualityParts=IndividualChoices(variants,rule.Cheap);
            if(changed)rule.Sent=0;
        }
    }
    private static int ChoiceBit(int part,bool cheap)=>part switch{4=>1<<(cheap?3:0),2=>1<<(cheap?4:1),1=>1<<(cheap?5:2),6=>1<<(cheap?8:6),3=>1<<(cheap?9:7),_=>0};
    public static int IndividualChoices(int variants,bool cheap){
        int singles=SinglePartVariants(variants),choices=0;
        foreach(int part in new[]{4,2,1})if((singles&(1<<part))!=0)choices|=ChoiceBit(part,cheap);
        return choices;
    }
    public void ConfigurePartChoices(int node,int owner,int number,int target,int choices,bool disposal,int limit){
        if(choices<1 || choices>1023 || target<0 || target==node || limit<1 || limit>9999)throw new ArgumentException("Invalid individual parts instruction.");
        var rule=Instructions(node,owner).Single(r=>r.Number==number);
        bool changed=rule.Target!=target || rule.PartChoices!=choices || rule.Disposal!=disposal;
        rule.Target=target;rule.QualityParts=choices;rule.Disposal=disposal;rule.Limit=limit;
        rule.Cheap=(choices&199)==0;rule.Variants=0;
        foreach(int part in new[]{4,2,1,6,3})if((choices&(ChoiceBit(part,false)|ChoiceBit(part,true)))!=0)rule.Variants|=1<<part;
        if(changed)rule.Sent=0;
    }
    public TransportInstruction AddInstruction(int node,int owner,int target)
    {
        UseInstructions(node,owner);var list=_instructions[(node,owner)];
        if(list.Count>=16)throw new InvalidOperationException("At most 16 instructions.");
        if(target<0 || target==node)throw new ArgumentException("Invalid destination.");
        int number=Enumerable.Range(1,16).First(n=>list.All(r=>r.Number!=n));
        int share=360/(list.Count+1);var rule=new TransportInstruction{Number=number,Target=target,Degrees=0};list.Add(rule);list.Sort((a,b)=>a.Number.CompareTo(b.Number));SetInstructionRate(node,owner,number,share);return rule;
    }
    public void DeleteInstruction(int node,int owner,int number){if(!_instructions.TryGetValue((node,owner),out var list))return;list.RemoveAll(r=>r.Number==number);}
    public void ConfigureInstruction(int node,int owner,int number,int target,int variants,bool cheap,bool disposal,int limit)
    {
        if(target<0 || target==node || (variants&~254)!=0 || variants==0 || limit<1 || limit>9999)throw new ArgumentException("Invalid instruction.");
        var rule=Instructions(node,owner).Single(r=>r.Number==number);
        bool changed=rule.Target!=target || rule.Variants!=variants || rule.Cheap!=cheap || rule.Disposal!=disposal;
        rule.QualityParts=null;rule.Target=target;rule.Variants=variants;rule.Cheap=cheap;rule.Disposal=disposal;rule.Limit=limit;if(changed)rule.Sent=0;
    }
    public void RestartInstruction(int node,int owner,int number)=>Instructions(node,owner).Single(r=>r.Number==number).Sent=0;
    public void SetInstructionRate(int node,int owner,int number,int degrees)
    {
        var rules=_instructions[(node,owner)];var selected=rules.Single(r=>r.Number==number);
        degrees=Math.Clamp(degrees,0,360);var others=rules.Where(r=>r!=selected).ToArray();
        int sum=others.Sum(r=>r.Degrees),budget=360-degrees;
        // Decreases go to the fixed rest sector. Increases consume rest before shrinking other orders.
        if(sum>budget){
            var values=others.Select(r=>(int)((long)r.Degrees*budget/sum)).ToArray();
            foreach(int i in Enumerable.Range(0,others.Length).OrderByDescending(i=>(long)others[i].Degrees*budget%sum).ThenBy(i=>i).Take(budget-values.Sum()))values[i]++;
            for(int i=0;i<others.Length;i++)others[i].Degrees=values[i];
        }
        selected.Degrees=degrees;
    }
    private sealed record InstructionChoice(RobotShipment Shipment,TransportInstruction Instruction);
    private Dictionary<long,Queue<InstructionChoice>> BuildInstructionChoices(RobotWorld world,Func<int,int,bool> connected,SeedRandom random,Func<int,bool> locked,Func<int,int,bool> canEnter)
    {
        var result=new Dictionary<long,Queue<InstructionChoice>>();
        for(int node=0;node<world.Nodes.Length;node++){
            if(locked?.Invoke(node)==true)continue;
            foreach(var robot in world.Nodes[node].Robots.OrderBy(r=>r.Id)){
                if(!world.CanShip(node,robot))continue;
                var options=new List<InstructionChoice>();
                foreach(var rule in Instructions(node,robot.Owner)){
                    if(rule.Complete || rule.Degrees==0 || rule.Target>=world.Nodes.Length || !connected(node,rule.Target) || locked?.Invoke(rule.Target)==true || !(canEnter?.Invoke(robot.Owner,rule.Target)??true) || robot.Disposal && !rule.Disposal)continue;
                    int mask=Enumerable.Range(1,7).Where(m=>(rule.Variants&(1<<m))!=0 && robot.CanSeparate((RobotParts)m) && (!rule.QualityParts.HasValue || m is not (3 or 6) || robot.IntegratedParts==(RobotParts)m) && (!world.Workshops[node].AutoAssemblyDisabled || (RobotParts)m==robot.Parts) && (rule.QualityParts is int choices?(choices&ChoiceBit(m,(robot.CheapParts&(RobotParts)m)!=0))!=0:rule.Cheap?(robot.CheapParts&(RobotParts)m)==(RobotParts)m:(robot.CheapParts&(RobotParts)m)==0))
                        .OrderByDescending(m=>PartCount((RobotParts)m)).ThenByDescending(m=>m).FirstOrDefault();
                    if(mask!=0)options.Add(new(new(node,rule.Target,robot.Id,(RobotParts)mask,rule.Disposal),rule));
                }
                var queue=new Queue<InstructionChoice>();
                // Weighted sampling without replacement permits rerolling if the receiver cannot fit.
                while(options.Count>0){int rest=RestDegrees(node,robot.Owner);int roll=random.Next(rest+options.Sum(o=>o.Instruction.Degrees)),sum=0;if(roll<rest)break;roll-=rest;var chosen=options.First(o=>{sum+=o.Instruction.Degrees;return roll<sum;});queue.Enqueue(chosen);options.Remove(chosen);}
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
