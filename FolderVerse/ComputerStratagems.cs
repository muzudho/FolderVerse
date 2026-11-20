namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record ComputerStratagemDecision(int Turn,int Ruler,int Candidates,int Reserved,int Committed,string Reason,
    Dictionary<string,int> ByKind,Dictionary<string,int> Rejected,StratagemKind? Kind=null,int Target=-1,int[] Sites=null,string Detail="");

public static class ComputerStratagems
{
    public static bool ProtectedSite(WorldSetup w,int node)=>w.Campaign.StratagemDecisions.Any(d=>d.Turn==w.Population.Turn&&d.Committed>0&&d.Sites?.Contains(node)==true);
    public static void Plan(WorldSetup w,bool automaticAll=false)
    {
        if(!w.Campaign.UseRobotCombat||w.Campaign.Phase!=CampaignPhase.Domestic)return;
        var log=w.Campaign.StratagemDecisions;
        if(log.Any(d=>d.Turn!=w.Population.Turn))log.Clear();
        var plans=w.Campaign.Stratagems;var battle=w.Campaign.RobotBattles;
        foreach(int ruler in Enumerable.Range(0,w.ActiveCount).Where(w.Relations.CanAct).Select(w.Relations.Root).Distinct().Order()){
            var prior=log.FirstOrDefault(d=>d.Ruler==ruler);
            if(prior!=null){if(automaticAll&&prior.Reason=="manual_control")log.Remove(prior);else continue;}
            var party=StratagemPlans.ComputerParty(w,ruler,automaticAll);
            var kinds=new Dictionary<string,int>();var rejected=new Dictionary<string,int>();
            void Reject(string reason)=>rejected[reason]=rejected.GetValueOrDefault(reason)+1;
            void Report(int count,int reserved,int committed,string reason,StratagemMatch chosen=null,string detail="")=>log.Add(new(w.Population.Turn,ruler,count,reserved,committed,reason,kinds,rejected,chosen?.Kind,chosen?.Target??-1,chosen?.Sites.Select(s=>s.Node).ToArray(),detail));
            if(party.Length==0){Report(0,0,0,"manual_control");continue;}
            if(plans.Pending.Any(p=>p.Controller==ruler)){Report(0,0,0,"pending_bundle");continue;}
            var matches=plans.FindComputer(w,ruler,automaticAll);
            foreach(var group in matches.GroupBy(m=>m.Kind))kinds[group.Key.ToString()]=group.Count();
            if(matches.Length==0){Report(0,0,0,"no_candidates");continue;}
            var policy=w.RulerComputerPolicy(ruler);var viable=new List<(StratagemMatch Match,long Score)>();
            foreach(var match in matches){
                var sites=match.Sites.Select(s=>s.Node).ToHashSet();
                if(sites.Any(n=>w.Robots.Workshops[n].Automation==NodeAutomation.Manual)){Reject("manual_site");continue;}
                if(w.Robots.PartPlanning.Orders.Any(o=>sites.Contains(o.Source)||sites.Contains(o.Target))||
                    w.Robots.DirectDelivery.Orders.Any(o=>sites.Contains(o.Source)||sites.Contains(o.Target))||
                    w.Robots.LongDelivery.Orders.Any(o=>sites.Contains(o.Source)||sites.Contains(o.Target)||sites.Contains(o.Node))||
                    w.Robots.PendingEdits.Any(o=>sites.Contains(o.Node))){Reject("existing_operation");continue;}
                int defense=battle.DefendingRobots(w,ruler,match.Target);
                if(match.Marches.Length>0){
                    int force=match.Marches.Sum(m=>(int)m.Fighters),required=defense+Math.Max(1,(defense+2)/3);
                    if(force<required){Reject("insufficient_force");continue;}
                    if(match.Marches.Any(m=>w.Campaign.AttackMemory.Recent(w,m.Ruler,m.SourceNode,m.TargetNode,w.Population.Turn+1) is {} failed&&m.Fighters<=failed.Force)){
                        Reject("recent_failed_attack");continue;
                    }
                }
                int preference=match.Kind==StratagemKind.WallRelief
                    ?policy.KeepGuard?(policy.AttackPriority==ComputerAttackPriority.Capture?20:100):10
                    :policy.AttackPriority==ComputerAttackPriority.Capture?100:80;
                int captives=Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.Powered[o]&&!w.Relations.Allied(ruler,o)&&w.Nodes.Current(o)?.Id==match.Target);
                var target=w.Nodes.All[match.Target];
                long score=preference*10000L+policy.TargetScore(captives,defense,true)+
                    (match.Kind==StratagemKind.ConcentratedAttack?100:0)+(w.Robots.Nodes[target.Id].WarehouseLimit??w.Robots.Nodes[target.Id].TotalCapacity)-match.Troops.Length;
                viable.Add((match,score));
            }
            if(viable.Count==0){Report(matches.Length,0,0,"all_rejected");continue;}
            var chosen=viable.OrderByDescending(x=>x.Score).ThenBy(x=>x.Match.Key,StringComparer.Ordinal).First().Match;
            if(viable.Count>1)rejected["lower_priority"]=viable.Count-1;
            if(!plans.ReserveComputer(w,ruler,chosen,automaticAll,out string reason)){Reject("reservation_changed");Report(matches.Length,0,0,"reservation_rejected",chosen,reason);continue;}
            var id=plans.Pending.Single(p=>p.Controller==ruler&&p.Computer).Id;
            if(!plans.CommitComputer(w,ruler,automaticAll,out reason)){plans.Cancel(id,ruler);Report(matches.Length,1,0,"commit_rejected",chosen,reason);continue;}
            Report(matches.Length,1,1,"committed",chosen);
        }
    }
}
