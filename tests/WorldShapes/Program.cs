using FolderVerse;

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
