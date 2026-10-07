namespace FolderVerse;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// Explicit PRNG keeps saved seeds reproducible without relying on System.Random's implementation.
public sealed class SeedRandom
{
    private uint _state;
    public SeedRandom(int seed) { _state = unchecked((uint)seed) ^ 0x9E3779B9u; }
    public uint Next() { _state = unchecked(_state * 1664525u + 1013904223u); uint x = _state; x ^= x >> 16; x = unchecked(x * 0x7FEB352Du); x ^= x >> 15; return x; }
    public int Next(int maximum) => (int)(Next() % (uint)maximum);
    public SeedRandom Clone(){var copy=new SeedRandom(0);copy._state=_state;return copy;}
    public float Unit() => (Next() >> 8) / 16777216f;
    public void Shuffle<T>(T[] values) { for (int i = values.Length - 1; i > 0; i--) { int j = Next(i + 1); (values[i], values[j]) = (values[j], values[i]); } }
}

public sealed class SurfaceCell
{
    public int Id, Face, X, Y;
    public Vector3 Origin, U, V, Normal;
    public readonly List<int> Neighbors = new();
    public Vector3 Center => Origin + (U + V) * 0.5f;
    public Vector3[] Corners => new[] { Origin, Origin + U, Origin + U + V, Origin + V };
}

public sealed class WorldSetup
{
    public int WorldSeed { get; private set; }
    public int CastSeed { get; private set; }
    public int PlacementSeed { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }
    public SurfaceCell[] Cells { get; private set; } = Array.Empty<SurfaceCell>();
    public int[] Portraits { get; private set; } = Enumerable.Range(0,20).ToArray();
    public ConquerorLook[] Looks { get; private set; } = new ConquerorLook[20];
    public int[] ColorIndices { get; private set; } = Enumerable.Range(0,20).ToArray();
    public int[] Owners { get; private set; } = Array.Empty<int>();
    public int[] Capitals { get; private set; } = Array.Empty<int>();
    public int[] ConquerorLocations { get; private set; } = Array.Empty<int>();
    public Point[] ConquerorPoints {get;private set;}=Array.Empty<Point>();
    public TerrainRoutes Routes {get;}=new();
    public WorldNodes Nodes {get;}=new();
    public int[] TerritoryCounts { get; private set; } = new int[20];
    public string[] CityNames { get; private set; } = Array.Empty<string>();
    public string[] ConquerorNames { get; private set; } = Array.Empty<string>();
    public const string PoliticalSystem="独裁者";
    public int PlayerSlot { get; private set; } = -1;
    public PopulationSimulation Population {get;}=new();
    public ConquestCampaign Campaign {get;}=new();
    public RobotWorld Robots {get;}=new();
    public ConquerorRelations Relations {get;}=new();
    public void SelectPlayer(int slot)
    {
        if(slot<0 || slot>=ActiveCount || Owners.Length==0)throw new ArgumentOutOfRangeException(nameof(slot));
        PlayerSlot=slot;
        if(Relations.Party(this).Length<=1)Relations.Leader=slot;
    }
    public int ActiveCount => Math.Min(20, Cells.Length);
    public static readonly Color[] Colors = {
        new(239,88,97), new(66,171,235), new(247,194,63), new(76,195,139), new(178,117,239),
        new(245,139,63), new(238,121,186), new(53,200,205), new(166,207,64), new(121,153,249),
        new(220,177,135), new(188,68,96), new(34,119,173), new(174,137,26), new(32,132,103),
        new(126,71,171), new(181,97,43), new(176,76,139), new(35,138,144), new(217,228,240)
    };
    public Color OwnerColor(int slot) => slot<0?new Color(155,169,180):Colors[ColorIndices[slot]];
    public void SetWorld(int seed)
    {
        if(seed < 0) throw new ArgumentOutOfRangeException(nameof(seed));
        WorldSeed = seed;
        var random = new SeedRandom(seed);
        Width = random.Next(5) + 1; Height = random.Next(5) + 1; Depth = random.Next(5) + 1;
        Cells = CreateCells(Width, Height, Depth);
        CityNames=WorldNames.Cities(Cells,seed);
        ClearPlacement();
    }
    public void SetCast(int seed)
    {
        if(seed < 0) throw new ArgumentOutOfRangeException(nameof(seed));
        CastSeed=seed;
        var random=new SeedRandom(seed);
        // Slot + 1 is the permanent creation number, displayed clockwise from the
        // top-left portrait. Keep it independent of later territory/ranking changes.
        Portraits=new int[20]; Looks=new ConquerorLook[20];
        ColorIndices=Enumerable.Range(0,20).ToArray();random.Shuffle(ColorIndices);
        // Draw distinct completed portraits. A family can contain all six sisters.
        int familyBase=random.Next(ConquerorCatalog.BaseCount);
        int familySize=random.Next(5)==0 ? 2+random.Next(5) : 0;
        int[] variants=Enumerable.Range(0,6).ToArray();random.Shuffle(variants);
        // Three out of four casts guarantee different bases; the remaining casts allow sisters.
        bool distinctBases=random.Next(4)<3;
        int[] bases=Enumerable.Range(0,ConquerorCatalog.BaseCount).ToArray();random.Shuffle(bases);
        for(int slot=0;slot<20;slot++)
        {
            ConquerorLook look;
            do
            {
                int baseId=distinctBases?bases[slot]:slot<familySize ? familyBase :
                    slot>0 && random.Next(100)<18 ? Portraits[random.Next(slot)] : random.Next(60);
                if(Looks.Take(slot).Count(l=>l.BaseId==baseId)>=6)baseId=random.Next(60);
                look=new(baseId,slot<familySize?variants[slot]:random.Next(6));
            } while(Array.IndexOf(Looks,look,0,slot)>=0);
            Looks[slot]=look;Portraits[slot]=look.BaseId;
        }
        ConquerorNames=WorldNames.Conquerors(Looks,seed);
        ClearPlacement();
    }
    public void ClearPlacement() { PlayerSlot=-1; Owners = Array.Empty<int>(); Capitals = Array.Empty<int>(); ConquerorLocations=Array.Empty<int>(); TerritoryCounts = new int[20]; }
    public void SetPlacement(int seed)
    {
        if(seed < 0) throw new ArgumentOutOfRangeException(nameof(seed));
        PlayerSlot=-1;
        Relations.Reset();
        PlacementSeed = seed;
        var random = new SeedRandom(seed);
        int[] order = Enumerable.Range(0, Cells.Length).ToArray(); random.Shuffle(order);
        Owners = Enumerable.Repeat(-1, Cells.Length).ToArray(); Capitals = order.Take(ActiveCount).ToArray();
        ConquerorLocations=(int[])Capitals.Clone();
        TerritoryCounts = new int[20];
        var frontier = new List<(int Owner, int Cell)>();
        for(int owner = 0; owner < ActiveCount; owner++)
        { Owners[Capitals[owner]] = owner; TerritoryCounts[owner] = 1; foreach(int n in Cells[Capitals[owner]].Neighbors) frontier.Add((owner,n)); }
        while(frontier.Count > 0)
        {
            int index = random.Next(frontier.Count); var next = frontier[index];
            frontier[index] = frontier[^1]; frontier.RemoveAt(frontier.Count - 1);
            if(Owners[next.Cell] >= 0) continue;
            Owners[next.Cell] = next.Owner; TerritoryCounts[next.Owner]++;
            foreach(int n in Cells[next.Cell].Neighbors) if(Owners[n] < 0) frontier.Add((next.Owner,n));
        }
        Population.Initialize(this);
        Routes.Initialize(this);
        ConquerorPoints=ConquerorLocations.Select(Routes.Start).ToArray();
        Nodes.Initialize(this);
        Robots.Initialize(Nodes.All.Length,ActiveCount);
        foreach(var node in Nodes.All.Where(n=>n.Owner>=0))
        {
            Robots.Workshops[node.Id].Configure((RobotParts)(1+new SeedRandom(seed^node.Id).Next(7)),3);
            for(int i=0;i<3;i++)Robots.Nodes[node.Id].Add(Robots.Create(node.Owner,RobotParts.Complete,i==1?RobotRole.Captain:RobotRole.Soldier));
        }
        // Start at the Node centre.
        for(int ruler=0;ruler<ActiveCount;ruler++)ConquerorPoints[ruler]=Nodes.Current(ruler).Center;
        for(int ruler=0;ruler<ActiveCount;ruler++)Robots.Workshops[Nodes.Current(ruler).Id].ConfigureDisposal();
        Routes.RefreshDisplay();
        Campaign.Reset();
    }
    public static SurfaceCell[] CreateCells(int width, int height, int depth)
    {
        var cells = new List<SurfaceCell>(); var half = new Vector3(width,height,depth) * 0.5f;
        void Face(int face, Vector3 origin, Vector3 u, Vector3 v, Vector3 normal, int countU, int countV)
        {
            for(int y=0;y<countV;y++) for(int x=0;x<countU;x++) cells.Add(new SurfaceCell {
                Id=cells.Count, Face=face, X=x, Y=y, Origin=origin+u*x+v*y, U=u, V=v, Normal=normal });
        }
        Face(0,new(half.X,-half.Y,-half.Z),Vector3.Up,Vector3.Backward,Vector3.Right,height,depth);
        Face(1,new(-half.X,-half.Y,half.Z),Vector3.Up,Vector3.Forward,Vector3.Left,height,depth);
        Face(2,new(-half.X,half.Y,-half.Z),Vector3.Backward,Vector3.Right,Vector3.Up,depth,width);
        Face(3,new(-half.X,-half.Y,half.Z),Vector3.Forward,Vector3.Right,Vector3.Down,depth,width);
        Face(4,new(-half.X,-half.Y,half.Z),Vector3.Right,Vector3.Up,Vector3.Backward,width,height);
        Face(5,new(half.X,-half.Y,-half.Z),Vector3.Left,Vector3.Up,Vector3.Forward,width,height);
        var edges = new Dictionary<string, int>();
        string Vertex(Vector3 p) => $"{(int)MathF.Round(p.X*2)},{(int)MathF.Round(p.Y*2)},{(int)MathF.Round(p.Z*2)}";
        foreach(var cell in cells)
        {
            var corners=cell.Corners;
            for(int e=0;e<4;e++)
            {
                string a=Vertex(corners[e]), b=Vertex(corners[(e+1)%4]);
                string key=string.CompareOrdinal(a,b)<0 ? a+"|"+b : b+"|"+a;
                if(edges.TryGetValue(key,out int neighbor)) { cell.Neighbors.Add(neighbor); cells[neighbor].Neighbors.Add(cell.Id); }
                else edges.Add(key,cell.Id);
            }
        }
        return cells.ToArray();
    }
}
