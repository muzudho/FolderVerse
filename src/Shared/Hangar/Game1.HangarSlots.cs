namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static RobotParts HangarPart(int row)=>row==0?RobotParts.Head:row==1?RobotParts.Body:RobotParts.Legs;

    private static void SyncHangarOccupants(long?[] slots,RobotStore store,Func<int,bool> reserved)
    {
        var ids=store.Robots.Select(r=>r.Id).ToHashSet();
        for(int i=0;i<slots.Length;i++)if(slots[i] is long id && !ids.Contains(id))slots[i]=null;
        foreach(long id in ids.Where(id=>!slots.Contains(id))){
            int slot=Array.FindIndex(slots,item=>!item.HasValue);
            for(int i=slot;i>=0 && i<slots.Length;i++)if(!slots[i].HasValue && !reserved(i)){slot=i;break;}
            if(slot>=0)slots[slot]=id;
        }
    }
    private static Dictionary<int,MaplePackage> HangarMaterials(long?[] slots,RobotStore store,IEnumerable<MaplePackage> packages,Func<int,bool> reserved=null,
        IReadOnlyDictionary<long,int> positions=null, IReadOnlyDictionary<int,MaplePackage> occupied=null)
    {
        var robots=store.Robots.ToDictionary(r=>r.Id);
        var order=Enumerable.Range(0,slots.Length).Where(slot=>reserved?.Invoke(slot)!=true)
            .OrderBy(slot=>slots[slot].HasValue?0:1).ToArray();
        var used=new bool[slots.Length*3];
        foreach(int slot in order)
            if(slots[slot] is long id && robots.TryGetValue(id,out var robot))
                for(int row=0;row<3;row++)used[slot*3+row]=(robot.Parts&HangarPart(row))!=0;
        var result=new Dictionary<int,MaplePackage>();
        if(occupied!=null)
            foreach(var entry in occupied)
                for(int i=0;i<entry.Value.Sections;i++)used[entry.Key+i]=true;
        foreach(var package in packages.OrderByDescending(p=>p.Sections).ThenByDescending(p=>positions?.ContainsKey(p.Id)==true)){
            int sections=package.Sections;
            int key=order.SelectMany(slot=>Enumerable.Range(0,4-sections).Reverse().Select(row=>slot*3+row))
                .FirstOrDefault(start=>Enumerable.Range(start,sections).All(index=>!used[index]),-1);
            if(positions?.TryGetValue(package.Id,out int preferred)==true && preferred>=0 &&
                order.Contains(preferred/3) && preferred%3+sections<=3 &&
                Enumerable.Range(preferred,sections).All(index=>!used[index]))key=preferred;
            if(key<0)continue;
            result.Add(key,package);
            for(int i=0;i<sections;i++)used[key+i]=true;
        }
        return result;
    }

    private static bool MaterialOccupies(Dictionary<int,MaplePackage> cargo,int slot,int row)=>
        cargo.Any(p=>p.Key/3==slot && row>=p.Key%3 && row<p.Key%3+p.Value.Sections);

}
