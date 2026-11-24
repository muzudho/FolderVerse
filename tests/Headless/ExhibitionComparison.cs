using System.Reflection;
using System.Text.Json;
using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

sealed class ExhibitionComparison(string outputDirectory) : Game1
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int Turns = 2;
    private readonly WorldSetup[] expected = new WorldSetup[Turns];
    private WorldSetup world = null!;
    private int compared;
    private string previousScreen = "";
    private TimeSpan time;
    public bool Differences { get; private set; }

    private object Get(string name) => typeof(Game1).GetField(name, Private)!.GetValue(this)!;
    private void Set(string name, object value) => typeof(Game1).GetField(name, Private)!.SetValue(this, value);
    private void Call(string name, params object[] values) =>
        typeof(Game1).GetMethod(name, Private)!.Invoke(this, values);
    private string CurrentScreen => Get("_screen").ToString()!;

    protected override void LoadContent()
    {
        Content.RootDirectory = Path.Combine(AppContext.BaseDirectory, "Content");
        base.LoadContent();
        Set("_autoSaveEnabled", false);
        Set("_statisticsEnabled", false);
        Set("_conquestOverviewEnabled", false);
        var fortuneSeed = new Random(123).Next();
        Set("_random", new Random(123));
        JsonElement initial = default;

        world = (WorldSetup)Get("_setup");
        world.SetParticipantCounts(0, 4);
        world.SetWorld(42);
        world.SetCast(8);
        world.SetTerritoryRights(492471852);
        Call("BeginSelection");
        Set("_skipOtherLandTurns", true);
        Call("UpdateLandSetup", 0d, new MouseState(), true, false, false);
        Call("UpdateLandSetup", 5d, new MouseState(), true, false, true);
        world = (WorldSetup)Get("_setup"); // Fortune confirmation may replace the preview world.
        if (CurrentScreen != "WorldStatus") throw new InvalidOperationException("Exhibition setup did not reach WorldStatus.");
        var settings = world.InitialStock.Settings;
        if (settings.Uniform || settings.DistributionSeed != (fortuneSeed ^ 0x494E4954))
            throw new InvalidOperationException("Exhibition stock distribution cannot be represented by headless options.");
        Console.WriteLine($"Initial stock policy: robots={settings.RobotPercent} raw={settings.RawPercent} distribution={settings.DistributionSeed} uniform={settings.Uniform}");
        for (int turn = 1; turn <= Turns; turn++)
        {
            string directory = Path.Combine(outputDirectory, $"headless-{turn}");
            var log = new StringWriter();
            if (HeadlessRunner.Run(["--headless", "--seed", "42", "--cast-seed", "8",
                "--rights-seed", "492471852", "--fortune-seed", fortuneSeed.ToString(),
                "--players", "4", "--robot-scatter", settings.RobotPercent.ToString(),
                "--raw-stock", settings.RawPercent.ToString(), "--max-turns", turn.ToString(), "--output", directory], log) != 0)
                throw new InvalidOperationException("Headless run failed.");
            expected[turn - 1] = AutoSaveStore.Read<WorldSetup>(Path.Combine(directory, "world.fvs"));
            using var start = JsonDocument.Parse(log.ToString().Split('\n')[0]);
            initial = start.RootElement.Clone();
        }

        Compare("initial fortune seed", initial.GetProperty("fortuneSeed").GetInt32(), world.FortuneSeed);
        Compare("initial characters", initial.GetProperty("characters").Deserialize<int[]>(), world.Looks.Take(4).Select(l => l.BaseId));
        Compare("initial stocks", initial.GetProperty("initial"),
            world.Robots.Nodes.Select((s, n) => new { node = n, s.Count, s.TotalCapacity, s.WarehouseLimit,
                keepers = s.Robots.Count(r => r.WarehouseKeeper), rawUnits = s.MaplePackages.Sum(p => p.Units) }));
    }

    protected override void Update(GameTime unused)
    {
        var step = TimeSpan.FromSeconds(.25);
        time += step;
        Call("HandleInputCore", new MouseState(), new KeyboardState(), new GameTime(time, step), true);
        string screen = CurrentScreen;
        if (screen != previousScreen)
            Console.WriteLine($"Screen: {screen}, turn {world.Population.Turn}, phase {world.Campaign.Phase}");
        if (previousScreen == "Industry" && screen != previousScreen && !world.Industry.Pending)
        {
            WorldSetup headless = expected[compared++];
            string turn = $"turn {compared}";
            Compare($"{turn} number", headless.Population.Turn, world.Population.Turn);
            Compare($"{turn} node owners", headless.Nodes.All.Select(n => n.Owner), world.Nodes.All.Select(n => n.Owner));
            Compare($"{turn} robot stock", RobotStock(headless), RobotStock(world));
            Compare($"{turn} industry result", headless.Industry.Result, world.Industry.Result);
            Compare($"{turn} ranking", Ranking(headless), Ranking(world));
            Compare($"{turn} conquest", Won(headless), Won(world));
            AutoSaveStore.Write(Path.Combine(outputDirectory, $"exhibition-{compared}.fvs"), world);
            if (compared == Turns) Exit();
        }
        previousScreen = screen;
        if (time.TotalMinutes > 8) throw new InvalidOperationException($"Exhibition did not finish {Turns} turns. Last screen: {screen}.");
    }

    private static object RobotStock(WorldSetup setup) => setup.Robots.Nodes.Select(n =>
        n.Robots.OrderBy(r => r.Id).Select(r => new { r.Id, r.Parts }));

    private static object Ranking(WorldSetup setup) =>
        setup.Relations.ProvisionalRanking(setup).Select(r => new { r.Ruler, r.Cells, r.Rank });

    private static bool Won(WorldSetup setup) =>
        setup.Relations.ProvisionalRanking(setup).Any(r => r.Cells == setup.Cells.Length && setup.Relations.CanWinConquest(r.Ruler));

    private void Compare(string label, object headless, object exhibition)
    {
        bool same = JsonSerializer.Serialize(headless) == JsonSerializer.Serialize(exhibition);
        Differences |= !same;
        Console.WriteLine($"{(same ? "MATCH" : "DIFFER")} {label}");
    }
}
