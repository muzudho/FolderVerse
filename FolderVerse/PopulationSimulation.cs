namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed class CellPopulation
{
    public bool Land;
    public long[] People=new long[3]; // Fighters, producers, unassigned.
    public decimal[] Conversion=new decimal[2];
    public decimal[,] Migration=new decimal[3,4];
    public decimal BirthPercent=0.00001m,BirthRemainder;
    public long Total=>People.Sum();
}

public sealed class PopulationSimulation
{
    public CellPopulation[] Cells {get;private set;}=Array.Empty<CellPopulation>();
    public int Turn {get;private set;}
    public void Initialize(WorldSetup world)
    {
        Turn=0;var random=new SeedRandom(world.WorldSeed^world.PlacementSeed^0x27491);
        var offset=WorldTerrain.Offset(world.WorldSeed);
        Cells=world.Cells.Select(c=>
        {
            var p=new CellPopulation{Land=WorldTerrain.CellKind(c,offset)!=TerrainKind.Sea};
            if(p.Land)
            {
                long total=Array.IndexOf(world.Capitals,c.Id)>=0?240000:60000+random.Next(120001);
                p.People[0]=total/24;p.People[1]=total*19/24;p.People[2]=total-p.People[0]-p.People[1];
            }
            return p;
        }).ToArray();
    }
    public int Neighbor(WorldSetup world,int cellId,int direction)
    {
        var cell=world.Cells[cellId];var n=cell.Normal;
        var up=Math.Abs(n.Y)>.5f?(n.Y>0?Vector3.Backward:Vector3.Forward):Vector3.Up;
        var right=Vector3.Cross(up,n);
        var axis=direction switch{0=>up,1=>right,2=>-up,_=>-right};
        // Match the shared edge in local north/east coordinates, including cube folds.
        var edge=cell.Corners.Select((a,i)=>new{A=a,B=cell.Corners[(i+1)%4]})
            .OrderByDescending(e=>Vector3.Dot((e.A+e.B)/2-cell.Center,axis)).First();
        return cell.Neighbors.First(id=>world.Cells[id].Corners.Contains(edge.A) && world.Cells[id].Corners.Contains(edge.B));
    }
    public bool CanMigrate(WorldSetup world,int cellId,int direction)
    {
        int target=Neighbor(world,cellId,direction);
        return Cells[cellId].Land && Cells[target].Land && world.Owners[cellId]==world.Owners[target] &&
            world.Routes.Find(cellId,world.Routes.Start(cellId),direction,true)!=null;
    }
    public void AdjustConversion(int cellId,int role,decimal delta)
    {
        var p=Cells[cellId];decimal maximum=100-Math.Max(0,p.Conversion[1-role]);
        p.Conversion[role]=Math.Clamp(p.Conversion[role]+delta,-100,maximum);
    }
    public void AdjustMigration(int cellId,int role,int direction,decimal delta)
    {
        var p=Cells[cellId];decimal other=0;
        for(int d=0;d<4;d++)if(d!=direction)other+=p.Migration[role,d];
        p.Migration[role,direction]=Math.Clamp(p.Migration[role,direction]+delta,0,100-other);
    }
    public void Advance(WorldSetup world)
    {
        var staged=Cells.Select(p=>(long[])p.People.Clone()).ToArray();
        for(int id=0;id<Cells.Length;id++)
        {
            var p=Cells[id];
            for(int role=0;role<2;role++)
            {
                decimal rate=p.Conversion[role];
                long count=(long)decimal.Floor((rate>=0?p.People[2]:p.People[role])*Math.Abs(rate)/100);
                staged[id][role]+=rate>=0?count:-count;
                staged[id][2]+=rate>=0?-count:count;
            }
        }
        var result=staged.Select(a=>(long[])a.Clone()).ToArray();
        for(int id=0;id<Cells.Length;id++)for(int role=0;role<3;role++)for(int d=0;d<4;d++)
        {
            if(!CanMigrate(world,id,d))continue;
            long count=(long)decimal.Floor(staged[id][role]*Cells[id].Migration[role,d]/100);
            result[id][role]-=count;result[Neighbor(world,id,d)][role]+=count;
        }
        for(int id=0;id<Cells.Length;id++)
        {
            var p=Cells[id];decimal births=result[id][1]*p.BirthPercent/100+p.BirthRemainder;
            long count=(long)decimal.Floor(births);p.BirthRemainder=births-count;
            result[id][2]+=count;p.People=result[id];
        }
        Turn++;
    }
}
