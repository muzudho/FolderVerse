namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public static class NewspaperSelection
{
    public static int Score(BattleState battle,int edition,int player)
    {
        var units=battle.Units.Concat(battle.Armies.SelectMany(a=>a.Waiting)).DistinctBy(u=>u.Id).ToArray();
        var counts=units.GroupBy(u=>u.Owner).ToDictionary(g=>g.Key,g=>g.Count());
        int quarter=Math.Max(1,(battle.NewspaperRanks.Count+3)/4);
        bool top(int owner)=>battle.NewspaperRanks.TryGetValue(owner,out int rank) && rank<=quarter;
        bool low(int owner)=>battle.NewspaperRanks.TryGetValue(owner,out int rank) && rank>battle.NewspaperRanks.Count-quarter;
        int score=(!battle.Finished?15:0)+Math.Min(20,units.Length/3)+Math.Min(30,Math.Max(0,battle.Armies.Count-2)*10);
        if(battle.Armies.Any(a=>top(a.Owner)))score+=25;
        if(units.Any(u=>u.Role==BattleRole.Queen))score+=40;
        if(battle.Winner>=0 && low(battle.Winner) && battle.Armies.Any(a=>a.Owner!=battle.Winner && top(a.Owner)))score+=100;
        if(battle.Winner>=0 && counts.Any(c=>c.Key!=battle.Winner && c.Value>counts.GetValueOrDefault(battle.Winner)))score+=30;
        // Publishers emphasize different aspects without changing the underlying result.
        if(edition==0 && battle.Armies.Any(a=>a.Owner==player))score+=35;
        if(edition==2 && battle.Winner>=0 && low(battle.Winner))score+=25;
        if(edition==3)score+=Math.Min(30,units.Count(u=>u.Fallen));
        if(edition==5 && units.Any(u=>u.Role==BattleRole.Queen))score+=25;
        return score;
    }
    public static int[] Select(BattleState[] battles,int edition,int player,int limit=6)
    {
        var remaining=Enumerable.Range(0,battles.Length).ToList();var selected=new List<int>();var covered=new HashSet<int>();
        while(remaining.Count>0 && selected.Count<limit){
            int next=remaining.OrderByDescending(i=>Score(battles[i],edition,player)+(battles[i].Armies.Any(a=>!covered.Contains(a.Owner))?12:0)).ThenBy(i=>i).First();
            selected.Add(next);remaining.Remove(next);foreach(var army in battles[next].Armies)covered.Add(army.Owner);
        }
        return selected.ToArray();
    }
}
