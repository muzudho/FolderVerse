namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record March(int Ruler,int Direction,long Fighters);
public sealed class ConquestCampaign
{
    public string Report {get;private set;}="";
    public void Reset()=>Report="";
    public void Advance(WorldSetup world,int direction,long fighters,bool enemies=true)
    {
        var orders=new List<March>();
        if(direction>=0)
        {
            if(world.Routes.ForRuler(world.PlayerSlot,direction)==null){Report="経路がつながっていないため移動できない";return;}
            orders.Add(new(world.PlayerSlot,direction,fighters));
        }
        if(enemies)
        {
            var random=new SeedRandom(world.PlacementSeed^world.Population.Turn^0x57421);
            for(int ruler=0;ruler<world.ActiveCount;ruler++)
            {
                if(ruler==world.PlayerSlot)continue;
                int source=world.ConquerorLocations[ruler];
                long army=world.Owners[source]==ruler && world.TerritoryCounts[ruler]>0?world.Population.Cells[source].People[0]/2:0;
                int move=Enumerable.Range(0,4).Where(d=>world.Routes.ForRuler(ruler,d)!=null).OrderBy(d=>
                {
                    int target=world.Population.Neighbor(world,source,d);
                    return world.TerritoryCounts[ruler]==0?random.Next(100):
                        world.Owners[source]!=ruler?(world.Owners[target]==ruler?0:100):
                        world.Owners[target]!=ruler && world.Population.Cells[target].People[0]<army?0:50+random.Next(50);
                }).DefaultIfEmpty(-1).First();
                if(move>=0)orders.Add(new(ruler,move,army));
            }
        }
        Resolve(world,orders);
    }
    public void Resolve(WorldSetup world,IReadOnlyList<March> orders)
    {
        if(orders.Select(o=>o.Ruler).Distinct().Count()!=orders.Count)throw new ArgumentException("One march per ruler per turn.");
        foreach(var order in orders)if(order.Ruler<0 || order.Ruler>=world.ActiveCount || order.Direction<0 || order.Direction>3 || order.Fighters<0)throw new ArgumentOutOfRangeException(nameof(orders));
        var routes=orders.ToDictionary(o=>o.Ruler,o=>world.Routes.ForRuler(o.Ruler,o.Direction));
        if(routes.Values.Any(r=>r==null))throw new InvalidOperationException("March has no reachable terrain route.");
        var arrivals=new Dictionary<int,Dictionary<int,long>>();
        var sources=orders.ToDictionary(o=>o.Ruler,o=>world.ConquerorLocations[o.Ruler]);
        var retreats=new List<(int Ruler,long Fighters)>();
        foreach(var order in orders)
        {
            int source=world.ConquerorLocations[order.Ruler];
            long available=world.Owners[source]==order.Ruler && world.TerritoryCounts[order.Ruler]>0?world.Population.Cells[source].People[0]:0;
            long army=Math.Min(order.Fighters,available);
            world.Population.Cells[source].People[0]-=army;
            int target=world.Population.Neighbor(world,source,order.Direction);
            world.ConquerorLocations[order.Ruler]=target;
            world.ConquerorPoints[order.Ruler]=routes[order.Ruler].Entry;
            if(!arrivals.TryGetValue(target,out var forces))arrivals[target]=forces=new();
            forces[order.Ruler]=army;
        }
        world.Population.Advance(world);
        var messages=new List<string>();
        int battles=0,conquests=0;
        foreach(var entry in arrivals.OrderBy(e=>e.Key))
        {
            int cell=entry.Key,defender=world.Owners[cell];var forces=entry.Value;
            forces[defender]=forces.GetValueOrDefault(defender)+world.Population.Cells[cell].People[0];
            var attackers=forces.Where(f=>f.Key!=defender && f.Value>0).OrderBy(f=>f.Key).ToArray();
            long attack=attackers.Sum(f=>f.Value),defense=forces[defender],loss=Math.Min(attack,defense);
            var casualties=attackers.ToDictionary(f=>f.Key,f=>attack==0?0:(long)decimal.Floor((decimal)loss*f.Value/attack));
            long remainder=loss-casualties.Values.Sum();
            foreach(var force in attackers.OrderByDescending(f=>(decimal)loss*f.Value/attack-casualties[f.Key]).ThenBy(f=>f.Key).Take((int)remainder))casualties[force.Key]++;
            var survivors=attackers.ToDictionary(f=>f.Key,f=>f.Value-casualties[f.Key]);
            survivors[defender]=Math.Max(0,defense-attack);
            long largest=survivors.Count==0?0:survivors.Values.Max();
            int winner=largest>0?survivors.Where(f=>f.Value==largest).OrderBy(f=>f.Key).First().Key:defender;
            foreach(var survivor in survivors.Where(f=>f.Key!=winner && f.Value>0))retreats.Add((survivor.Key,survivor.Value));
            bool battle=forces.Any(f=>f.Key!=defender && f.Value>0);
            if(battle)battles++;
            world.Population.Cells[cell].People[0]=survivors.GetValueOrDefault(winner);
            if(winner!=defender)
            {
                world.Owners[cell]=winner;
                conquests++;
                Array.Clear(world.Population.Cells[cell].Conversion);Array.Clear(world.Population.Cells[cell].Migration);
                messages.Add(WorldCoordinates.Label(world,cell)+"を征服");
            }
            else if(battle)messages.Add(WorldCoordinates.Label(world,cell)+"で戦闘 / 防衛側が維持");
        }
        Array.Clear(world.TerritoryCounts);
        foreach(var retreat in retreats)
        {
            int source=sources[retreat.Ruler];
            if(world.Owners[source]!=retreat.Ruler)source=Array.FindIndex(world.Owners,o=>o==retreat.Ruler);
            if(source>=0)world.Population.Cells[source].People[0]+=retreat.Fighters;
        }
        foreach(int owner in world.Owners)world.TerritoryCounts[owner]++;
        for(int ruler=0;ruler<world.ActiveCount;ruler++)
            if(world.TerritoryCounts[ruler]>0 && world.Owners[world.Capitals[ruler]]!=ruler)
                world.Capitals[ruler]=Enumerable.Range(0,world.Cells.Length).Where(c=>world.Owners[c]==ruler).OrderByDescending(c=>world.Population.Cells[c].Total).ThenBy(c=>c).First();
        string result=orders.Any(o=>o.Ruler==world.PlayerSlot)?
            world.TerritoryCounts[world.PlayerSlot]==0?"あなたは放浪者":world.Owners[world.ConquerorLocations[world.PlayerSlot]]==world.PlayerSlot?"あなたの現在地は自国":"あなたの現在地は他国":"あなたは待機";
        Report=$"{result} / 戦闘 {battles} 件 / 征服 {conquests} セル";
    }
}
