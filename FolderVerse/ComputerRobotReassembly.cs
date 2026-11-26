namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

// Refit foreign soldiers through ordinary, cancellable part-swap reservations.
internal static class ComputerRobotReassembly
{
    internal static void Plan(WorldSetup world,bool automaticAll)
    {
        if(world.Campaign.Phase!=CampaignPhase.Domestic || !world.Campaign.UseRobotCombat || world.TutorialStep!=0)return;
        var stock=world.Robots;var manual=world.ManualRulers().ToHashSet();
        foreach(var node in world.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && world.Relations.CanAct(n.Owner) &&
            (automaticAll || !manual.Contains(n.Owner))).OrderBy(n=>n.Id)){
            int at=node.Id,owner=node.Owner;var shop=stock.Workshops[at];
            if(shop.Automation==NodeAutomation.Manual || shop.SupervisorLeaveRemaining>0 ||
                world.Campaign.RobotBattles.LockedNode(at) || ComputerStratagems.ProtectedSite(world,at) ||
                world.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==at || o.Target==at) ||
                world.Campaign.PlannedOrders.Any(o=>o.SourceNode==at || o.TargetNode==at) ||
                stock.PartPlanning.Orders.Any(o=>o.Source==at || o.Target==at) ||
                stock.PartPlanning.WorkpieceResumes.Any(o=>o.Node==at) || stock.PendingEdits.Any(o=>o.Node==at))continue;
            bool Free(Robot r)=>!r.Stationed && !r.Disposal && !r.IsWorkpiece && !r.IsDismantling &&
                !stock.AssemblyLocked(at,r) && !stock.ReservationBusy(at,r.Id) &&
                !world.Campaign.Trade.Offers.Any(o=>!o.Give.Raw && o.Give.Id==r.Id || !o.Want.Raw && o.Want.Id==r.Id);
            var robots=stock.Nodes[at].Robots.Where(Free).OrderBy(r=>r.Id).ToArray();
            var training=stock.Training[owner];
            var choices=new List<(Robot Donor,Robot Receiver,RobotParts Part,int Gain,int Equipment)>();
            foreach(var receiver in robots.Where(r=>r.Owner!=owner && r.Parts==RobotParts.Complete))
            foreach(var donor in robots.Where(r=>r.Owner==owner))
            foreach(var part in RobotMaterials.Parts){
                if(donor.Affiliation(part)!=owner || !donor.CanSeparate(part) || !receiver.CanSeparate(part))continue;
                var nextReceiver=receiver.Merge(donor.Slice(part),owner);
                var nextDonor=donor.Merge(receiver.Slice(part),owner);
                if(nextReceiver.Owner!=owner || !training.Eligible(nextReceiver))continue;
                bool wasReady=training.Eligible(donor),willBeReady=nextDonor.Owner==owner && training.Eligible(nextDonor);
                if(wasReady && !willBeReady)continue;
                int equipment=nextReceiver.Equipment(owner,world.EquipmentMode).Total+
                    (willBeReady?nextDonor.Equipment(owner,world.EquipmentMode).Total:0)-
                    (wasReady?donor.Equipment(owner,world.EquipmentMode).Total:0);
                if(equipment<0)continue;
                choices.Add((donor,receiver,part,1+(willBeReady?1:0)-(wasReady?1:0),equipment));
            }
            foreach(var choice in choices.OrderByDescending(c=>c.Gain).ThenByDescending(c=>c.Equipment)
                .ThenBy(c=>c.Receiver.Id).ThenBy(c=>c.Donor.Id).ThenBy(c=>(int)c.Part)){
                try{stock.PartPlanning.ReserveComputerReassembly(world,at,choice.Donor,choice.Receiver,choice.Part,owner);break;}
                catch(InvalidOperationException){continue;}
            }
        }
    }
}
