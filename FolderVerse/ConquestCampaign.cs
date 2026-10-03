namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record March(int Ruler,int Direction,long Fighters,int ArrivalOutpost=-1);
public sealed class ConquestCampaign
{
    public string Report {get;private set;}="";
    public void Reset()=>Report="";
    public void Advance(WorldSetup world,int direction,long fighters,bool enemies=true,int arrivalOutpost=-1)
    {
        var orders=new List<March>();
        if(direction>=0)
        {
            if(world.Routes.ForMarch(world.PlayerSlot,direction,arrivalOutpost)==null){Report="経路がつながっていないため移動できない";return;}
            orders.Add(new(world.PlayerSlot,direction,fighters,arrivalOutpost));
        }
        if(enemies)
        {
            var random=new SeedRandom(world.PlacementSeed^world.Population.Turn^0x57421);
            for(int ruler=0;ruler<world.ActiveCount;ruler++)
            {
                if(ruler==world.PlayerSlot)continue;
                int source=world.ConquerorLocations[ruler];var home=world.Outposts.Current(ruler);
                long army=home!=null && home.Owner==ruler?home.Population.People[0]/2:0;
                var move=Enumerable.Range(0,4).SelectMany(d=>world.Routes.ArrivalChoices(ruler,d).Select(path=>(Direction:d,Path:path))).OrderBy(choice=>
                {
                    var path=choice.Path;var target=world.Outposts.At(path.Target,path.Entry);
                    return world.TerritoryCounts[ruler]==0?random.Next(100):
                        home==null || home.Owner!=ruler?(target.Owner==ruler?0:100):
                        target.Owner!=ruler && target.Population.People[0]<army?0:50+random.Next(50);
                }).FirstOrDefault();
                if(move.Path!=null)orders.Add(new(ruler,move.Direction,army,world.Outposts.At(move.Path.Target,move.Path.Entry).Id));
            }
        }
        Resolve(world,orders);
    }
    public void Resolve(WorldSetup world,IReadOnlyList<March> orders)
    {
        if(orders.Select(o=>o.Ruler).Distinct().Count()!=orders.Count)throw new ArgumentException("One march per ruler per turn.");
        foreach(var order in orders)if(order.Ruler<0 || order.Ruler>=world.ActiveCount || order.Direction<0 || order.Direction>3 || order.Fighters<0)throw new ArgumentOutOfRangeException(nameof(orders));
        var routes=orders.ToDictionary(o=>o.Ruler,o=>world.Routes.ForMarch(o.Ruler,o.Direction,o.ArrivalOutpost));
        if(routes.Values.Any(r=>r==null))throw new InvalidOperationException("March has no reachable terrain route.");
        var arrivals=new Dictionary<int,Dictionary<int,long>>();
        var previousFull=world.Cells.Select(c=>world.Outposts.FullOwner(c.Id)).ToArray();
        var sources=orders.ToDictionary(o=>o.Ruler,o=>world.Outposts.Current(o.Ruler)?.Id??-1);
        var retreats=new List<(int Ruler,long Fighters)>();
        foreach(var order in orders)
        {
            var source=world.Outposts.Current(order.Ruler);
            long available=source!=null && source.Owner==order.Ruler?source.Population.People[0]:0;
            long army=Math.Min(order.Fighters,available);
            if(source!=null)source.Population.People[0]-=army;
            int target=routes[order.Ruler].Target;
            world.ConquerorLocations[order.Ruler]=target;
            world.ConquerorPoints[order.Ruler]=routes[order.Ruler].Entry;
            int post=world.Outposts.At(target,routes[order.Ruler].Entry).Id;
            if(!arrivals.TryGetValue(post,out var forces))arrivals[post]=forces=new();
            forces[order.Ruler]=army;
        }
        world.Population.Advance(world);
        var messages=new List<string>();
        int battles=0,conquests=0;
        foreach(var entry in arrivals.OrderBy(e=>e.Key))
        {
            var post=world.Outposts.All[entry.Key];int cell=post.Cell,defender=post.Owner;var forces=entry.Value;
            forces[defender]=forces.GetValueOrDefault(defender)+post.Population.People[0];
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
            post.Population.People[0]=survivors.GetValueOrDefault(winner);
            if(winner!=defender)
            {
                post.Owner=winner;
                conquests++;
                Array.Clear(post.Population.Conversion);Array.Clear(post.Population.Migration);
                messages.Add(WorldCoordinates.Label(world,cell)+"を征服");
            }
            else if(battle)messages.Add(WorldCoordinates.Label(world,cell)+"で戦闘 / 防衛側が維持");
        }
        foreach(var retreat in retreats)
        {
            int source=sources[retreat.Ruler];
            if(source<0 || world.Outposts.All[source].Owner!=retreat.Ruler)source=Array.FindIndex(world.Outposts.All,p=>p.Owner==retreat.Ruler);
            if(source>=0)world.Outposts.All[source].Population.People[0]+=retreat.Fighters;
        }
        world.Outposts.Recount();
        string result=orders.Any(o=>o.Ruler==world.PlayerSlot)?
            world.TerritoryCounts[world.PlayerSlot]==0?"あなたは放浪者":world.Outposts.Current(world.PlayerSlot)?.Owner==world.PlayerSlot?"あなたの現在地は自国拠点":"あなたの現在地は他国拠点":"あなたは待機";
        int cellsWon=world.Cells.Count(c=>world.Outposts.FullOwner(c.Id)>=0 && world.Outposts.FullOwner(c.Id)!=previousFull[c.Id]);
        Report=$"{result} / 戦闘 {battles} 件 / 拠点確保 {conquests} / セル征服 {cellsWon}";
    }
}
