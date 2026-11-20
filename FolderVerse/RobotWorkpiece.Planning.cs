namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record WorkpieceResumeOrder(long Number,int Owner,int Node,long RobotId,RobotWorkshop Line);
public sealed partial class RobotPartPlanning
{
    public List<WorkpieceResumeOrder> WorkpieceResumes {get;}=new();
    public bool ReserveWorkpieceResume(WorldSetup world,int node,RobotWorkshop line,long id,int owner)
    {
        if(!world.Relations.Controls(owner) || world.Nodes.All[node].Owner!=owner || WorkpieceResumes.Any(o=>ReferenceEquals(o.Line,line)))return false;
        var robot=world.Robots.Nodes[node].Robots.FirstOrDefault(r=>r.Id==id);
        if(!world.Robots.CanResumeWorkpiece(world,node,line,robot))return false;
        WorkpieceResumes.Add(new(_next++,owner,node,id,line));Invalidate();return true;
    }
    public bool CancelWorkpieceResume(RobotWorkshop line,int owner)
    {
        int count=WorkpieceResumes.RemoveAll(o=>ReferenceEquals(o.Line,line) && o.Owner==owner);Invalidate();return count>0;
    }
    private void ResolveWorkpieceResumes(WorldSetup world)
    {
        foreach(var order in WorkpieceResumes.ToArray()){
            WorkpieceResumes.Remove(order);
            int node=Array.FindIndex(world.Robots.Workshops,w=>w.ProductionLines.Contains(order.Line));
            bool ok=node>=0 && world.Nodes.All[node].Owner==order.Owner && world.Robots.ResumeWorkpiece(world,node,order.Line,order.RobotId);
            LastResults.Add(new(order.Number,order.Owner,node>=0?node:order.Node,ok?"仕掛品の再開予約を実施":"仕掛品・作業員・所有国が変わったため再開予約を解除"));
        }
        Invalidate();
    }
}
