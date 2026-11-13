namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// The block tree retains exactly the stretches that can connect two functional nodes.
internal sealed class ScenicRoutes
{
    private readonly record struct Leg(int Cell, RouteStep Route, int From, int To);
    private readonly record struct Segment(Vector3 From, Vector3 To, float Start, float Length, bool Sea);
    private sealed record SeaLeg(int From,int To,float Length,RouteStep[] Routes);
    private readonly record struct SeaVisit(SeaLeg Leg,bool Reverse,float Start);
    private sealed record SeaTour(SeaVisit[] Visits,float Duration,float Phase);
    private const float YachtSpeed=.08f, YachtPause=1.2f;
    internal readonly record struct Vehicle(Vector3 At, Vector3 Forward, bool Sea);
    private readonly Dictionary<RouteStep, (Segment[] Segments, float Length, float Phase)> _paths = new();
    private readonly Dictionary<RouteStep, List<(SeaLeg Leg, SeaTour Tour)>> _seaTours = new();
    private readonly RouteStep[][] _display;
    private readonly bool[] _functional;

    internal bool Matches(WorldSetup world) => ReferenceEquals(_display, world.Routes.DisplayPathSnapshot) &&
        _functional.Length == world.Nodes.All.Length && world.Nodes.All.All(n => _functional[n.Id] == n.Functional);

    internal ScenicRoutes(WorldSetup world)
    {
        _display = world.Routes.DisplayPathSnapshot;
        _functional = world.Nodes.All.Select(n => n.Functional).ToArray();
        var legs = new List<Leg>();
        for (int cell = 0; cell < _display.Length; cell++)
            foreach (var route in _display[cell])
            {
                if (route.Path.Length == 0) continue;
                var from = world.Nodes.At(cell, route.Path[0]);
                var to = world.Nodes.At(route.Target, route.Entry);
                if (from != null && to != null && from.Id != to.Id)
                    legs.Add(new(cell, route, from.Id, to.Id));
            }
        var pairs = legs.GroupBy(l => (A: Math.Min(l.From,l.To), B: Math.Max(l.From,l.To))).ToArray();
        var through = ThroughRoutes(_functional, pairs.Select(p => p.Key).ToArray());
        var terrainOffset = WorldTerrain.Offset(world.WorldSeed);
        var seaLegs = new List<SeaLeg>();
        for (int i = 0; i < pairs.Length; i++)
        {
            if (through[i]) continue;
            // Join reciprocal curves only when their endpoints meet at the same portal.
            foreach (var portal in pairs[i].GroupBy(l => l.Route.Portal?.Position))
            {
                var sections = new List<(RouteStep Route, Segment[] Segments)>();
                float length = 0;
                bool join = portal.Key.HasValue && portal.Count() == 2 &&
                    portal.Select(l => l.From).Distinct().Count() == 2;
                var ordered=portal.OrderBy(l=>l.From).ToArray();
                foreach (var leg in ordered)
                {
                    var curve = world.Routes.Curve(leg.Cell,leg.Route);
                    if (join && sections.Count == 1) curve = curve.Reverse().ToArray();
                    var segments = new List<Segment>();
                    for (int j = 1; j < curve.Length; j++)
                    {
                        var a = curve[j-1]; var b = curve[j];
                        float distance = Vector3.Distance(a,b);
                        if (distance < .00001f) continue;
                        bool sea = WorldTerrain.Elevation((a+b)/2,terrainOffset) < .52f;
                        segments.Add(new(a,b,length,distance,sea));
                        length += distance;
                    }
                    sections.Add((leg.Route, segments.ToArray()));
                }
                float phase = ((pairs[i].Key.A * 17 + pairs[i].Key.B * 31) % 29) * .41f;
                foreach (var section in sections)
                    _paths.Add(section.Route,(section.Segments,length,phase));
                if(length>0 && (join || !portal.Key.HasValue && ordered.Length==1) &&
                    sections.All(s=>s.Segments.All(segment=>segment.Sea)))
                    seaLegs.Add(new(ordered[0].From,ordered[0].To,length,sections.Select(s=>s.Route).ToArray()));
            }
        }
        BuildSeaTours(seaLegs);
    }

    private void BuildSeaTours(List<SeaLeg> legs)
    {
        var links=new Dictionary<int,List<SeaLeg>>();
        foreach(var leg in legs)
            foreach(int endpoint in new[]{leg.From,leg.To})
            {
                if(!links.TryGetValue(endpoint,out var adjacent))links.Add(endpoint,adjacent=new());
                adjacent.Add(leg);
            }
        foreach(var (node,adjacent) in links)
            adjacent.Sort((a,b)=>(a.From==node?a.To:a.From).CompareTo(b.From==node?b.To:b.From));
        var assigned=new HashSet<SeaLeg>();
        foreach(var first in legs)
        {
            if(assigned.Contains(first))continue;
            var visits=new List<SeaVisit>();
            float duration=0;
            var leg=first;
            bool reverse=false;
            // Rotate to the next exit on arrival. Only a dead end sends the yacht back immediately.
            do
            {
                visits.Add(new(leg,reverse,duration));
                duration+=leg.Length/YachtSpeed+YachtPause;
                int at=reverse?leg.From:leg.To;
                var exits=links[at];
                leg=exits[(exits.IndexOf(leg)+1)%exits.Count];
                reverse=leg.To==at;
            }while(!ReferenceEquals(leg,first) || reverse);
            var tour=new SeaTour(visits.ToArray(),duration,first.From%29*.41f);
            foreach(var visited in visits.Select(visit=>visit.Leg).Distinct())
            {
                assigned.Add(visited);
                foreach(var route in visited.Routes)
                {
                    if(!_seaTours.TryGetValue(route,out var tours))_seaTours.Add(route,tours=new());
                    tours.Add((visited,tour));
                }
            }
        }
    }

    // Tarjan's edge stack decomposes cycles as well as bridges. Pruning the
    // resulting block tree also removes a loop attached to a single active stop.
    internal static bool[] ThroughRoutes(bool[] functional, (int A, int B)[] edges)
    {
        var links = functional.Select(_ => new List<(int Node,int Edge)>()).ToArray();
        for (int i = 0; i < edges.Length; i++)
        {
            var (a,b) = edges[i];
            links[a].Add((b,i)); links[b].Add((a,i));
        }
        var discovered = new int[links.Length];
        var pending = new Stack<int>();
        var blocks = new List<int[]>();
        int time = 0;
        int Visit(int at, int parent)
        {
            int low = discovered[at] = ++time;
            foreach (var (next,edge) in links[at])
            {
                if (edge == parent || discovered[next] > discovered[at]) continue;
                pending.Push(edge);
                if (discovered[next] != 0)
                {
                    low = Math.Min(low,discovered[next]);
                    continue;
                }
                int child = Visit(next,edge);
                low = Math.Min(low,child);
                if (child < discovered[at]) continue;
                var block = new List<int>();
                int popped;
                do { popped = pending.Pop(); block.Add(popped); } while (popped != edge);
                blocks.Add(block.ToArray());
            }
            return low;
        }
        for (int i = 0; i < links.Length; i++)
            if (discovered[i] == 0) Visit(i,-1);

        int nodes = links.Length;
        var tree = Enumerable.Range(0,nodes+blocks.Count).Select(_ => new List<int>()).ToArray();
        for (int i = 0; i < blocks.Count; i++)
            foreach (int node in blocks[i].SelectMany(e => new[]{edges[e].A,edges[e].B}).Distinct())
            {
                tree[node].Add(nodes+i); tree[nodes+i].Add(node);
            }
        var degree = tree.Select(branch => branch.Count).ToArray();
        bool Terminal(int i) => i < nodes && functional[i];
        var leaves = new Queue<int>(Enumerable.Range(0,tree.Length).Where(i => degree[i] <= 1 && !Terminal(i)));
        var removed = new bool[tree.Length];
        while (leaves.TryDequeue(out int at))
        {
            if (removed[at]) continue;
            removed[at] = true;
            foreach (int next in tree[at])
                if (!removed[next] && --degree[next] == 1 && !Terminal(next)) leaves.Enqueue(next);
        }
        var through = new bool[edges.Length];
        for (int i = 0; i < blocks.Count; i++)
            if (!removed[nodes+i])
                foreach (int edge in blocks[i]) through[edge] = true;
        return through;
    }

    internal bool TryDraw(RouteStep route, float time, Action<Vector3,Vector3,bool> dash, out Vehicle? vehicle)
    {
        vehicle = null;
        if (!_paths.TryGetValue(route,out var path)) return false;
        const float spacing = .16f, dashLength = .09f, speed = .13f, pause = .65f;
        float duration = path.Length / speed;
        float period = 2 * (duration + pause);
        float phase = period>0?(time+path.Phase) % period:0;
        bool reverse = phase >= duration+pause;
        float offset = Math.Clamp((phase-(reverse ? duration+pause : 0))*speed,0,path.Length);
        float distance = reverse ? path.Length-offset : offset;
        bool showVehicle=true;
        if(_seaTours.TryGetValue(route,out var tours))
        {
            showVehicle=false;
            foreach(var sea in tours)
            {
                float at=(time+sea.Tour.Phase)%sea.Tour.Duration;
                int index=Array.FindLastIndex(sea.Tour.Visits,visit=>visit.Start<=at);
                var visit=sea.Tour.Visits[Math.Max(0,index)];
                if(!ReferenceEquals(sea.Leg,visit.Leg))continue;
                showVehicle=true;
                reverse=visit.Reverse;
                offset=Math.Clamp((at-visit.Start)*YachtSpeed,0,path.Length);
                distance=reverse?path.Length-offset:offset;
                break;
            }
        }
        foreach (var segment in path.Segments)
        {
            float end = segment.Start+segment.Length;
            Vector3 At(float at) => Vector3.Lerp(segment.From,segment.To,(at-segment.Start)/segment.Length);
            for (float start = MathF.Floor(segment.Start/spacing)*spacing; start < end; start += spacing)
            {
                float a = Math.Max(segment.Start,start), b = Math.Min(end,start+dashLength);
                if (b > a) dash(At(a),At(b),segment.Sea);
            }
            if (showVehicle && distance >= segment.Start && (distance < end || end == path.Length && distance <= end))
                vehicle = new(At(distance),Vector3.Normalize(segment.To-segment.From)*(reverse ? -1 : 1),segment.Sea);
        }
        return true;
    }
}
