namespace FolderVerse;

using System;
using System.Collections.Generic;
using System.Linq;

public enum LandPreference
{
    Connected, Warm, Logistics, Enclave, Sea, Competitive,
    Challenge, AvoidPowers, Resources, Mountains, Island, Polar
}

public sealed record LandSelection(int[] Cells, int AvailableCount, bool FirstChoiceTaken, int FirstChoice);

public sealed class ComputerLandPlanner
{
    private static readonly LandPreference[] Preferences =
    {
        LandPreference.Connected, LandPreference.Warm, LandPreference.Logistics, LandPreference.Enclave, LandPreference.Sea, LandPreference.Competitive,
        LandPreference.Connected, LandPreference.Challenge, LandPreference.AvoidPowers, LandPreference.Sea, LandPreference.Logistics, LandPreference.Resources,
        LandPreference.Mountains, LandPreference.Island, LandPreference.Competitive, LandPreference.Resources, LandPreference.Logistics, LandPreference.Enclave,
        LandPreference.Challenge, LandPreference.Island, LandPreference.Polar, LandPreference.Resources, LandPreference.Connected, LandPreference.Challenge,
        LandPreference.Enclave, LandPreference.Logistics, LandPreference.Warm, LandPreference.Competitive, LandPreference.Mountains, LandPreference.AvoidPowers,
        LandPreference.Competitive, LandPreference.Polar, LandPreference.Warm, LandPreference.Resources, LandPreference.Challenge, LandPreference.Logistics,
        LandPreference.Sea, LandPreference.AvoidPowers, LandPreference.Island, LandPreference.Challenge, LandPreference.Polar, LandPreference.AvoidPowers,
        LandPreference.Connected, LandPreference.Logistics, LandPreference.Competitive, LandPreference.Warm, LandPreference.Polar, LandPreference.Island,
        LandPreference.Mountains, LandPreference.Sea, LandPreference.Warm, LandPreference.Polar, LandPreference.Resources, LandPreference.Enclave,
        LandPreference.Connected, LandPreference.Polar, LandPreference.Enclave, LandPreference.Mountains, LandPreference.Sea, LandPreference.Logistics
    };

    private readonly WorldSetup _world;
    private readonly Geography[] _geography;
    private readonly record struct Geography(int Land, int Mountains, int Harbors, int Resources, double Latitude, int Entrances, int Ice);

    public ComputerLandPlanner(WorldSetup world)
    {
        _world = world;
        var maple = RobotWorld.GenerateMapleDeposits(world);
        var terrain = world.Terrain;
        _geography = world.Cells.Select(cell =>
        {
            var nodes = world.Nodes.InCell(cell.Id).ToArray();
            int resources = nodes.Sum(node => maple[node.Id])
                + CubeNet.ResourceSites(world, cell, terrain).Where(site => RobotWorld.SiteNode(world, cell, site.Position) != null)
                    .Sum(site => RobotWorld.SiteLoads(site.Kind, site.Size));
            return new Geography(nodes.Where(node => node.Population.Land).Sum(node => node.Points.Length),
                nodes.Where(node => node.Terrain == TravelTerrain.Mountain).Sum(node => node.Points.Length),
                nodes.Count(node => node.IsHarbor), resources, WorldTerrain.RelativeLatitude(world, cell.Center),
                world.Routes.DisplayPaths(cell.Id).Select(route => route.Target).Distinct().Count(),
                Enumerable.Range(0, 100).Count(i => world.Routes.PackIceAt(cell.Id, new(i % 10, i / 10))));
        }).ToArray();
    }

    public static LandPreference PreferenceFor(int character) => Preferences[character];

    public static string Label(LandPreference preference) => preference switch
    {
        LandPreference.Connected => "領土つなぎ",
        LandPreference.Warm => "暖地好き",
        LandPreference.Logistics => "兵站家",
        LandPreference.Enclave => "飛び地好き",
        LandPreference.Sea => "海好き",
        LandPreference.Competitive => "勝負師",
        LandPreference.Challenge => "難所好き",
        LandPreference.AvoidPowers => "大国避け",
        LandPreference.Resources => "資源一番乗り",
        LandPreference.Mountains => "山好き",
        LandPreference.Island => "島守り",
        _ => "極好き"
    };

    public static string Remark(int character, bool firstChoiceTaken)
    {
        if (character + 1 is 6 or 14 or 15 or 28 or 30 or 42 or 45 or 48 or 52)
            return "";
        if (character + 1 is 3 or 12 or 20 or 23 or 56 && !firstChoiceTaken)
            return "";
        return PreferenceFor(character) switch
        {
            LandPreference.Connected => "味方の土地をつなげておこう。",
            LandPreference.Warm => "暖かいところにしよう。",
            LandPreference.Logistics => "産地と工場を近くに置きたいね。",
            LandPreference.Enclave => "別の方面にも足場を作りたいな。",
            LandPreference.Sea => "海が見えるところがいいわ。",
            LandPreference.Competitive => "守りを固めて、次に進める場所ね。",
            LandPreference.Challenge => "みんなが避ける土地から始めようか。",
            LandPreference.AvoidPowers => "大きな国とは距離を置こう。",
            LandPreference.Resources when firstChoiceTaken => "先を越されたか。次の産地を探そう。",
            LandPreference.Resources => "まずは原材料を押さえよう。",
            LandPreference.Mountains => "山のそばに旗を立てたいな。",
            LandPreference.Island => "入口の少ないところを押さえよう。",
            _ => "流氷の道を探そう。"
        };
    }

    public LandSelection Select(int ruler, LandPreference? preferenceOverride = null)
    {
        var preference = preferenceOverride ?? PreferenceFor(_world.Looks[ruler].BaseId);
        var random = new SeedRandom(_world.PlacementSeed ^ ruler * 7919);
        var available = _world.Cells.Where(cell => _world.Owners[cell.Id] < 0).Select(cell => cell.Id).ToHashSet();
        int availableCount = available.Count;
        int rights = _world.TerritoryRights[ruler];
        var chosen = new HashSet<int>();
        var order = new List<int>();
        var ties = _world.Cells.Select(_ => random.Next()).ToArray();
        var scored = _world.Cells.Select(cell => Score(cell, preference, ruler)).ToArray();
        int firstChoice = _world.Cells.OrderByDescending(cell => scored[cell.Id]).ThenBy(cell => ties[cell.Id]).First().Id;
        bool firstChoiceTaken = !available.Contains(firstChoice);

        while (order.Count < rights && available.Count > 0)
        {
            var frontier = chosen.SelectMany(id => _world.Cells[id].Neighbors)
                .Where(available.Contains).ToHashSet();
            // Make room for one intentional foothold while keeping the main territory connected where possible.
            bool foothold = preference == LandPreference.Enclave && rights >= 4 && order.Count == rights - 1;
            var remote = foothold ? available.Where(id => !frontier.Contains(id) && (_geography[id].Harbors > 0 || _geography[id].Resources > 0)).ToArray() : Array.Empty<int>();
            IEnumerable<int> pool = remote.Length > 0 ? remote : frontier.Count > 0 ? frontier : available;
            int next = pool.OrderByDescending(id => scored[id]).ThenBy(id => ties[id]).First();
            available.Remove(next);
            chosen.Add(next);
            order.Add(next);
        }

        return new LandSelection(order.ToArray(), availableCount, firstChoiceTaken, firstChoice);
    }

    private double Score(SurfaceCell cell, LandPreference preference, int ruler)
    {
        var g = _geography[cell.Id];
        int nearbyResources = cell.Neighbors.Max(id => _geography[id].Resources);
        int nearbyPower = cell.Neighbors.Select(id => _world.Owners[id])
            .Where(owner => owner >= 0 && owner != ruler).Distinct().Sum(owner => _world.TerritoryRights[owner]);
        return preference switch
        {
            LandPreference.Connected => cell.Neighbors.Count(id => _world.Owners[id] < 0),
            LandPreference.Warm => -g.Latitude + g.Land * .001,
            LandPreference.Logistics => g.Resources + nearbyResources * .5 + g.Harbors * 4 + g.Entrances,
            LandPreference.Enclave => g.Harbors * 8 + g.Resources,
            LandPreference.Sea => -g.Land + g.Harbors * .5,
            LandPreference.Competitive => g.Resources * .5 + g.Harbors * 4 + g.Land * .06 - Math.Abs(g.Entrances - 2) * 3 - nearbyPower * .2,
            LandPreference.Challenge => nearbyPower * .2 + g.Entrances * 2 - g.Resources * .5 - g.Land * .05,
            LandPreference.AvoidPowers => -nearbyPower * 20 + g.Resources,
            LandPreference.Resources => g.Resources + nearbyResources * .01,
            LandPreference.Mountains => g.Mountains + g.Land * .01,
            LandPreference.Island => g.Land * .05 - Math.Abs(g.Harbors - 1) * 6 - Math.Abs(g.Entrances - 1) * 3,
            _ => g.Ice + g.Latitude
        };
    }
}
