namespace FolderVerse;
using System;
using System.Linq;
public static class NewspaperBattleOutcome
{
    public static string NationalHeadline(BattleState battle, int country, bool resolved)
    {
        var army = battle.Armies.FirstOrDefault(a => a.Owner == country);
        bool allyInBattle = battle.Armies.Any(a => a.Owner != country && battle.Allied(country, a.Owner));
        string subject = army != null ? "わが軍" : allyInBattle ? "同盟軍" : "他国軍";
        var units = army == null ? Array.Empty<BattleUnit>() :
            battle.Units.Concat(army.Waiting).Where(u => u.Owner == country).ToArray();
        bool withdrew = army != null && units.Any(u => u.Retreated) &&
            units.All(u => u.Fallen || u.Retreated);

        if (!resolved) return subject + "の戦況 / 交戦中";
        if (battle.Winner < 0)
            return withdrew ? "わが軍が撤退 / 勝者なし" : subject + "の戦闘が終結 / 勝者なし";
        if (battle.Winner == country)
        {
            bool repelled = battle.Kind == BattleKind.Node && battle.Defender == country &&
                battle.FlagStanding && battle.Armies.Any(a => !battle.Allied(country, a.Owner));
            return repelled ? "わが軍、侵攻を撃退" : "わが軍が勝利";
        }
        if (battle.Allied(country, battle.Winner))
            return withdrew ? "わが軍は撤退、同盟軍が勝利" : "同盟軍が勝利";
        if (army == null) return allyInBattle ? "同盟軍が敗北" : "他国軍の戦闘が終結";
        return withdrew ? "わが軍が撤退 / 敵軍が勝利" : "わが軍が敗北";
    }

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
    public static bool Eliminated(BattleState battle,BattleFrame frame,int owner,bool resolved)
    {
        if(resolved && battle.Winner>=0 && battle.Allied(owner,battle.Winner))return false;
        var units=battle.Units.Concat(battle.Armies.SelectMany(a=>a.Waiting)).Where(u=>u.Owner==owner && u.Role!=BattleRole.Queen).DistinctBy(u=>u.Id).ToArray();
        int total=units.Length;
        int lost=resolved?units.Count(u=>u.Fallen):frame.Units.Count(u=>u.Owner==owner && u.Role!=BattleRole.Queen && u.Fallen);
        return total>0 && lost*10>=total*9;
    }    public static string Headline(BattleState battle,BattleFrame frame,string[] names,int patron,bool biased,bool resolved)
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
