namespace FolderVerse;
using System;
using System.Linq;

public sealed partial class PopulationSimulation
{
    public bool CanMigrateNode(WorldSetup world,int id,int direction)
    {
        var source=world.Nodes.All[id];if(!source.Population.Land)return false;
        var route=world.Routes.Find(source.Cell,source.Center,direction,true);if(route==null)return false;
        var target=world.Nodes.At(route.Target,route.Entry);
        return target!=null && target.Population.Land && source.Owner==target.Owner;
    }
    public void AdjustNodeConversion(WorldSetup world,int id,int role,decimal delta)
    {
        var p=world.Nodes.All[id].Population;p.Conversion[role]=Math.Clamp(p.Conversion[role]+delta,-100,100-Math.Max(0,p.Conversion[1-role]));
    }
    public void AdjustNodeMigration(WorldSetup world,int id,int role,int direction,decimal delta)
    {
        var p=world.Nodes.All[id].Population;decimal other=0;for(int d=0;d<4;d++)if(d!=direction)other+=p.Migration[role,d];
        p.Migration[role,direction]=Math.Clamp(p.Migration[role,direction]+delta,0,100-other);
    }
    private void AdvanceNodes(WorldSetup world)
    {
        var posts=world.Nodes.All;var staged=posts.Select(p=>(long[])p.Population.People.Clone()).ToArray();
        foreach(var post in posts)for(int role=0;role<2;role++)
        {
            var p=post.Population;decimal rate=p.Conversion[role];long count=(long)decimal.Floor((rate>=0?p.People[2]:p.People[role])*Math.Abs(rate)/100);
            staged[post.Id][role]+=rate>=0?count:-count;staged[post.Id][2]+=rate>=0?-count:count;
        }
        var result=staged.Select(a=>(long[])a.Clone()).ToArray();
        foreach(var post in posts)for(int role=0;role<3;role++)for(int d=0;d<4;d++)
        {
            if(post.Population.Migration[role,d]==0 || !CanMigrateNode(world,post.Id,d))continue;
            var route=world.Routes.Find(post.Cell,post.Center,d,true);var target=world.Nodes.At(route.Target,route.Entry);
            long count=(long)decimal.Floor(staged[post.Id][role]*post.Population.Migration[role,d]/100);
            result[post.Id][role]-=count;result[target.Id][role]+=count;
        }
        foreach(var post in posts)
        {
            var p=post.Population;decimal births=result[post.Id][1]*p.BirthPercent/100+p.BirthRemainder;
            long count=(long)decimal.Floor(births);p.BirthRemainder=births-count;result[post.Id][2]+=count;p.People=result[post.Id];
        }
        Turn++;world.Nodes.Recount();
    }
}
