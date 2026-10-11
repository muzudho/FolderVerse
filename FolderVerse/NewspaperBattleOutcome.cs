namespace FolderVerse;
using System;
using System.Linq;
public static class NewspaperBattleOutcome
{
    public static string Caption(BattleState battle,int owner,bool favorsArmy)
    {
        if(battle.Winner>=0 && battle.Allied(owner,battle.Winner))return "勝利！";
        var units=battle.Units.Concat(battle.Armies.SelectMany(a=>a.Waiting)).Where(u=>u.Owner==owner && u.Role!=BattleRole.Queen).DistinctBy(u=>u.Id).ToArray();
        int total=units.Length,lost=units.Count(u=>u.Fallen);
        if(total==0)return favorsArmy?"突然の転進":"退却……";
        if(lost*10>=total*9)return "消滅……";
        if(lost*2>=total)return "壊滅……";
        if(lost*10>=total*3)return "全滅……";
        return favorsArmy?"突然の転進":"退却……";
    }
    public static string Headline(BattleState battle,BattleFrame frame,string[] names,int patron,bool biased,bool resolved)
    {
        string Country(int owner)=>names[owner]+"国";
        if(resolved)
        {
            int subject=biased?patron:battle.Winner>=0?battle.Winner:battle.Armies.Min(a=>a.Owner);
            return Country(subject)+Caption(battle,subject,biased);
        }
        var departed=battle.Armies.Where(a=>frame.Units.Any(u=>u.Owner==a.Owner) && !frame.Units.Any(u=>u.Owner==a.Owner && !u.Fallen && !u.Retreated))
            .OrderBy(a=>biased && a.Owner==patron?0:1).ThenBy(a=>a.Owner).FirstOrDefault();
        if(departed!=null)
        {
            var units=frame.Units.Where(u=>u.Owner==departed.Owner && u.Role!=BattleRole.Queen).ToArray();
            int total=battle.Units.Concat(battle.Armies.SelectMany(a=>a.Waiting)).Where(u=>u.Owner==departed.Owner && u.Role!=BattleRole.Queen).DistinctBy(u=>u.Id).Count();
            int lost=units.Count(u=>u.Fallen);
            string caption=total>0 && lost*10>=total*9?"消滅……":total>0 && lost*2>=total?"壊滅……":total>0 && lost*10>=total*3?"全滅……":biased && departed.Owner==patron?"突然の転進":"退却……";
            return Country(departed.Owner)+caption;
        }
        if(frame.Turn<=(battle.Frames.FirstOrDefault()?.Turn??frame.Turn))
        {
            if(battle.Kind==BattleKind.Edge)return string.Join("、",battle.Armies.Select(a=>Country(a.Owner)))+"衝突！";
            var attacker=battle.Armies.FirstOrDefault(a=>a.Owner!=battle.Defender);
            if(attacker!=null)return Country(attacker.Owner)+"側から開戦！";
        }
        var counts=battle.Armies.Select(a=>new{a.Owner,Count=frame.Units.Count(u=>u.Owner==a.Owner && !u.Fallen && !u.Retreated)})
            .OrderByDescending(a=>a.Count).ThenBy(a=>a.Owner).ToArray();
        if(counts.Length>=2 && counts[0].Count>0 && (!biased || counts[0].Owner==patron))
        {
            if(counts[0].Count>2*counts.Skip(1).Sum(a=>a.Count))return Country(counts[0].Owner)+"勝勢！";
            if(counts[0].Count*10>=counts[1].Count*11 && counts[0].Count>counts[1].Count)return Country(counts[0].Owner)+"優勢！";
        }
        return "戦力拮抗！";
    }}
