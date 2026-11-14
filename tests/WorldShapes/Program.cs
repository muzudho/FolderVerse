using FolderVerse;
using Microsoft.Xna.Framework;

static void Check(bool value, string message) { if (!value) throw new Exception(message); }
const int samples = 2048;
var counts = new int[4];
var rodAxes = new HashSet<int>();
var slabAxes = new HashSet<int>();
bool longestRod = false, widestSlab = false;
var world = new WorldSetup();
for (int seed = 0; seed < samples; seed++)
{
    world.SetWorld(seed);
    int[] axes = { world.Width, world.Height, world.Depth };
    int kind = axes.Count(n => n > 1);
    counts[kind]++;
    Check(axes.All(n => n >= 1), "Nonpositive dimension");
    if (kind == 1)
    {
        Check(axes.Max() <= 9, "Rod exceeds 9");
        rodAxes.Add(Array.FindIndex(axes, n => n > 1));
        longestRod |= axes.Max() == 9;
    }
    if (kind == 2)
    {
        Check(axes.Max() <= 7, "Slab exceeds 7");
        slabAxes.Add(Array.IndexOf(axes, 1));
        widestSlab |= axes.Max() == 7;
    }
    if (kind == 3) Check(axes.All(n => n <= 5), "Box exceeds 5");
    int expectedCells = 2 * (axes[0] * axes[1] + axes[1] * axes[2] + axes[2] * axes[0]);
    Check(world.Cells.Length == expectedCells, "Wrong surface area");
    Check(world.ActiveCount == Math.Min(20, expectedCells), "Wrong active ruler count");
    foreach (var cell in world.Cells)
    {
        Check(cell.Neighbors.Count == 4 && cell.Neighbors.Distinct().Count() == 4, "Broken surface adjacency");
        Check(cell.Neighbors.All(n => world.Cells[n].Neighbors.Contains(cell.Id)), "Nonreciprocal seam");
    }
    if (seed < 32)
    {
        world.SetWorld(seed);
        Check(axes.SequenceEqual(new[] { world.Width, world.Height, world.Depth }), "Seed is not reproducible");
    }
}
int[] weights = { 1, 2, 4, 8 };
for (int kind = 0; kind < 4; kind++)
    Check(Math.Abs((double)counts[kind] / samples - weights[kind] / 15.0) < 0.04, "Shape frequency outside tolerance");
Check(rodAxes.Count == 3 && slabAxes.Count == 3, "Missing axis orientation");
Check(longestRod && widestSlab, "New maximum sizes not generated");
Console.WriteLine($"PASS: {samples} seeds; shape counts {string.Join(", ", counts)}; size limits, axis orientations, seed reproducibility, ruler counts and reciprocal surface seams.");

Check(WorldTerrain.HeadSystemForArea(200,300)==PlanetHeadSystem.Fish &&
      WorldTerrain.HeadSystemForArea(201,300)==PlanetHeadSystem.Fish,"At least two thirds sea qualifies as fish");
Check(WorldTerrain.HeadSystemForArea(100,300)==PlanetHeadSystem.Monkey &&
      WorldTerrain.HeadSystemForArea(199,300)==PlanetHeadSystem.Monkey,"From one third to below two thirds sea qualifies as monkey");
Check(WorldTerrain.HeadSystemForArea(99,300)==PlanetHeadSystem.Dog,"Below one third sea qualifies as dog");

var lottery=new PlanetLottery();
var lotteryRandom=new Random(20261114);
var lotteryWorld=new WorldSetup();
lotteryWorld.SetParticipantCounts(1,19);
for(int batch=0;batch<12;batch++)
{
    var drawn=new int[3];
    for(int draw=0;draw<9;draw++)
    {
        lottery.Draw(lotteryWorld,lotteryRandom);
        var terrain=lotteryWorld.Terrain;
        int total=lotteryWorld.Cells.Length*100;
        int sea=0;
        foreach(var cell in lotteryWorld.Cells)
            for(int y=0;y<10;y++)
                for(int x=0;x<10;x++)
                    if(WorldTerrain.Kind(terrain.Elevation(cell.Origin+cell.U*((x+.5f)/10)+cell.V*((y+.5f)/10)))==TerrainKind.Sea)
                        sea++;
        var system=WorldTerrain.HeadSystemForArea(sea,total);
        Check((int)system==lotteryWorld.PolarAxis,"Lottery terrain and polar axis disagree");
        if(batch==0)
        {
            lotteryWorld.Routes.Initialize(lotteryWorld);
            int routeSea=lotteryWorld.Cells.Sum(cell=>Enumerable.Range(0,100)
                .Count(i=>!lotteryWorld.Routes.Land(cell.Id,new Point(i%10,i/10))));
            Check(routeSea==sea,"Routes and the lottery must use the same sea tiles");
        }
        drawn[(int)system]++;
    }
    Check(drawn.All(count=>count==3),"Each completed deck has three stars per head system for the test seeds");
}
Check(lotteryWorld.TrySetWorld(123,.59,.61),"A narrow sea band should be reachable");
var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(lotteryWorld));
Check(restored.Terrain==lotteryWorld.Terrain && restored.PolarAxis==lotteryWorld.PolarAxis,"Sea adjustment was lost on save");
var oldSave=AutoSaveStore.Capture(lotteryWorld);
foreach(var entry in oldSave.Objects)entry.Fields?.Remove("_terrainElevationShift");
Check(AutoSaveStore.Restore<WorldSetup>(oldSave).Terrain.ElevationShift==0,"Legacy saves must use the original terrain");
Check(!lotteryWorld.TrySetWorld(456,.123456789,.123456789) && lotteryWorld.WorldSeed==123,
    "Impossible sea band must leave the current planet intact");
Check(lotteryWorld.TrySetWorld(123,.59,.61) && lotteryWorld.Terrain==restored.Terrain,
    "Seed and bounds must reproduce the same terrain");
lotteryWorld.SetWorld(123);
Check(lotteryWorld.Terrain.ElevationShift==0,"Unrestricted generation restores the original elevation");
Console.WriteLine("PASS: 12 shuffled decks, head-system boundaries, seed replay, impossible range and save round-trip.");

const int surveySize=10000, surveySeed=20261114;
var surveyRandom=new Random(surveySeed);
world.SetParticipantCounts(1,19); // Default selection on the new-game setup screen.
var heads=new int[3];
for(int i=0;i<surveySize;i++)
{
    world.SetWorld(surveyRandom.Next());
    heads[world.PolarAxis]++;
    if(heads[world.PolarAxis]>48)continue;

    Check(world.Geographic(world.North)==Vector3.Up,"North must map to geographic up");
    Check(world.Cells.Select(c=>WorldCoordinates.At(world,c)).Distinct().Count()==world.Cells.Length,
        $"Duplicate geographic coordinate for seed {world.WorldSeed}");
    foreach(var normal in CubeNet.Normals)
        Check(Vector3.Dot(world.MapUp(normal),normal)==0,"Map up must lie in the face plane");
}
foreach(var system in Enum.GetValues<PlanetHeadSystem>())
{
    int count=heads[(int)system];
    Check(count>0,$"No {system} worlds for coordinate coverage");
    Console.WriteLine($"{system}: {count}/{surveySize} ({count/(double)surveySize:P2}), survey RNG seed {surveySeed}");
}
