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

Check(WorldTerrain.HeadSystemForArea(75,100)==PlanetHeadSystem.Fish,"75% sea must qualify");
Check(WorldTerrain.HeadSystemForArea(25,100)==PlanetHeadSystem.Dog,"75% land must qualify");
Check(WorldTerrain.HeadSystemForArea(74,100)==PlanetHeadSystem.Monkey &&
      WorldTerrain.HeadSystemForArea(26,100)==PlanetHeadSystem.Monkey,"Intermediate shares must qualify as monkey");

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
