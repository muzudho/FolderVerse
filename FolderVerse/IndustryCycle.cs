namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed record IndustrySector(int Turns,int Percent);
public sealed class IndustryCycle
{
    // Duplicate values keep the wheel irregular; total probability 100, expectation 4.
    public static readonly IndustrySector[] Sectors={new(1,1),new(1,20),new(13,10),new(0,2),new(7,15),new(2,7),new(40,1),new(0,2),new(3,12),new(1,22),new(4,8)};
    public IReadOnlyList<IndustrySector> Wheel {get;private set;}=MergeAdjacent(Sectors);
    public bool Jackpot=>SelectedSector>=0 && Result==Wheel.Max(s=>s.Turns);
    public static IndustrySector[] CreateWheel(int seed,int round)
    {
        var random=new SeedRandom(seed^round*13007^0x78219);
        for(int attempt=0;attempt<512;attempt++)
        {
            int a=3+random.Next(4),b=7+random.Next(4),c=11+random.Next(6),jackpot=30+random.Next(31);
            int wa=6+random.Next(11),wb=10+random.Next(11),wc=6+random.Next(9);
            int twos=400-jackpot-95-(a-1)*wa-(b-1)*wb-(c-1)*wc;
            int ones=95-wa-wb-wc-twos;
            if(twos<1 || ones<25)continue;
            int split=1+random.Next(ones-1);
            var sectors=new[]{new IndustrySector(0,2),new(0,2),new(1,split),new(1,ones-split),new(2,twos),new(a,wa),new(b,wb),new(c,wc),new(jackpot,1)};
            random.Shuffle(sectors);return MergeAdjacent(sectors);
        }
        return MergeAdjacent(Sectors);
    }
    public static IndustrySector[] MergeAdjacent(IEnumerable<IndustrySector> sectors)
    {
        var merged=new List<IndustrySector>();
        foreach(var sector in sectors)
            if(merged.Count>0 && merged[^1].Turns==sector.Turns)merged[^1]=merged[^1] with {Percent=merged[^1].Percent+sector.Percent};
            else merged.Add(sector);
        if(merged.Count>1 && merged[0].Turns==merged[^1].Turns)
        {merged[0]=merged[0] with {Percent=merged[0].Percent+merged[^1].Percent};merged.RemoveAt(merged.Count-1);}
        return merged.ToArray();
    }
    public bool Pending {get;private set;}
    public int Round {get;private set;}
    public int SelectedSector {get;private set;}=-1;
    public int Result=>SelectedSector<0?-1:Wheel[SelectedSector].Turns;
    public void Reset(){Pending=false;Round=0;SelectedSector=-1;Wheel=MergeAdjacent(Sectors);}
    public void Queue(int round){if(round<=Round)return;Round=round;Pending=true;SelectedSector=-1;}
    public static int SectorAt(int ticket)=>SectorAt(ticket,Sectors);
    public static int SectorAt(int ticket,IReadOnlyList<IndustrySector> wheel)
    {
        if(ticket<0 || ticket>=100)throw new ArgumentOutOfRangeException(nameof(ticket));
        int sum=0;for(int i=0;i<wheel.Count;i++){sum+=wheel[i].Percent;if(ticket<sum)return i;}
        throw new InvalidOperationException("Invalid industry wheel.");
    }
    public void Roll(int seed){if(!Pending || SelectedSector>=0)return;Wheel=CreateWheel(seed,Round);SelectedSector=SectorAt(new SeedRandom(seed^Round*7919^0x45123).Next(100),Wheel);}
    public bool Apply(WorldSetup world)
    {
        if(!Pending || SelectedSector<0)return false;
        for(int i=0;i<Result;i++)world.Robots.AdvanceIndustry(n=>world.Nodes.All[n].Owner,world.Campaign.RobotBattles.LockedNode);
        Pending=false;return true;
    }
}
