namespace FolderVerse;
using System;
using System.Linq;

public sealed record IndustrySector(int Turns,int Percent);
public sealed class IndustryCycle
{
    // Duplicate values keep the wheel irregular; total probability 100, expectation 4.
    public static readonly IndustrySector[] Sectors={new(0,1),new(1,20),new(13,10),new(0,2),new(7,15),new(2,8),new(40,1),new(0,2),new(3,12),new(1,21),new(4,8)};
    public bool Pending {get;private set;}
    public int Round {get;private set;}
    public int SelectedSector {get;private set;}=-1;
    public int Result=>SelectedSector<0?-1:Sectors[SelectedSector].Turns;
    public void Reset(){Pending=false;Round=0;SelectedSector=-1;}
    public void Queue(int round){if(round<=Round)return;Round=round;Pending=true;SelectedSector=-1;}
    public static int SectorAt(int ticket)
    {
        if(ticket<0 || ticket>=100)throw new ArgumentOutOfRangeException(nameof(ticket));
        int sum=0;for(int i=0;i<Sectors.Length;i++){sum+=Sectors[i].Percent;if(ticket<sum)return i;}
        throw new InvalidOperationException("Invalid industry wheel.");
    }
    public void Roll(int seed){if(!Pending || SelectedSector>=0)return;SelectedSector=SectorAt(new SeedRandom(seed^Round*7919^0x45123).Next(100));}
    public bool Apply(WorldSetup world)
    {
        if(!Pending || SelectedSector<0)return false;
        for(int i=0;i<Result;i++)world.Robots.AdvanceIndustry(n=>world.Nodes.All[n].Owner,world.Campaign.RobotBattles.LockedNode);
        Pending=false;return true;
    }
}
