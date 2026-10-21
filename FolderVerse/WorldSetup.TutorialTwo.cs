namespace FolderVerse;
using System;
using System.Linq;
public sealed partial class WorldSetup
{
    public void PrepareTutorialStepTwo(int worldSeed,int castSeed,int placementSeed)
    {
        SetPlayerCount(1);
        while(true){SetWorld(worldSeed);if(Width==1 && Height==2 && Depth==1)break;worldSeed=worldSeed==int.MaxValue?0:worldSeed+1;}
        _presetCastCount=7;SetCast(castSeed);
        for(int i=0;i<7;i++){Portraits[i]=i;Looks[i]=new(i,0);}
        ConquerorFullNames=WorldNames.Conquerors(Looks,castSeed);ConquerorNames=WorldNames.ShortNames(ConquerorFullNames);
        SetPlacement(placementSeed);
        Array.Fill(Owners,-1);
        var cells=Enumerable.Range(0,Cells.Length).ToArray();new SeedRandom(placementSeed).Shuffle(cells);
        for(int ruler=1;ruler<7;ruler++)Owners[cells[ruler-1]]=ruler;
        Capitals=Enumerable.Range(0,7).Select(r=>r==0?cells[0]:cells[r-1]).ToArray();
        ConquerorLocations=(int[])Capitals.Clone();
        Population.Initialize(this);Routes.Initialize(this);ConquerorPoints=ConquerorLocations.Select(Routes.Start).ToArray();Nodes.Initialize(this);
        Robots.Initialize(Nodes.All.Length,ActiveCount);
        for(int ruler=1;ruler<7;ruler++)
        {Robots.Training[ruler].AssemblyPolicy=2;Robots.Training[ruler].DeploymentPolicy=1;}
        foreach(var node in Nodes.All)
        {
            Robots.Workshops[node.Id].RemoveProductionLines();
            // The four unclaimed cells have no country, hence no national cheap robots.
            if(node.Owner>=0)for(int i=0;i<3;i++)Robots.Nodes[node.Id].Add(Robots.Create(node.Owner,RobotParts.Complete) with {CheapParts=RobotParts.Complete});
            Robots.Nodes[node.Id].Add(Robots.Create(0,RobotParts.Complete));
        }
        for(int ruler=0;ruler<7;ruler++)ConquerorPoints[ruler]=Nodes.Current(ruler).Center;
        Nodes.Recount();Routes.RefreshDisplay();Campaign.Reset();TutorialStep=2;LandPending=false;
    }
}
