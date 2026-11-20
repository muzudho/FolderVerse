namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record TurnNewsArticle(int Country,string Kind,string Headline,string[] Lines,int Gained=0,int Lost=0,string Unit="地点",string[] Commentary=null);

// A newspaper reports observations; a conditional outlook never predicts a certain winner.
public static class TurnNewspaper
{
    public static TurnNewsArticle[] Build(WorldSetup w,int reader)
    {
        var articles=new List<TurnNewsArticle>();
        var ranking=w.Relations.ProvisionalRanking(w);
        foreach(int country in Enumerable.Range(0,w.ActiveCount).OrderBy(c=>c==reader?0:1).ThenBy(c=>c)){
            var own=w.Nodes.All.Where(n=>n.Functional && n.Owner==country).ToArray();
            var previous=w.Campaign.PreviousNodeOwners;
            bool history=previous.Length==w.Nodes.All.Length;
            int sectors=ranking.FirstOrDefault(r=>r.Ruler==country).Cells;
            int change=w.Campaign.SectorChange(w,country,sectors);
            if(change!=0)articles.Add(new(country,"昨日の出来事",change>0?"わが国側の征服地域が増加":"わが国側の征服地域が減少",new[]{
                $"前回 {sectors-change} 地域 → 現在 {sectors} 地域。",
                "世界征服状況と同じ集計です。配下を含む支配関係も反映します。",
                "増減は戦闘だけでなく、陣営の変化による場合もあります。"},Math.Max(0,change),Math.Max(0,-change),"地域"));
            var gained=history?own.Where(n=>previous[n.Id]!=country).ToArray():Array.Empty<Node>();
            var lost=history?w.Nodes.All.Where(n=>previous[n.Id]==country && n.Owner!=country).ToArray():Array.Empty<Node>();
            foreach(var battle in w.Campaign.RobotBattles.Scenes.Where(b=>b.Finished && b.Armies.Any(a=>a.Owner==country)).Take(3))
                articles.Add(new(country,"昨日の戦闘結果",NewspaperBattleOutcome.NationalHeadline(battle,country,true),new[]{"戦闘記録に基づくわが国の報道。",$"参戦した国：{string.Join("、",battle.Armies.Select(a=>$"第{a.Owner+1}国"))}","勝敗と、その後の補給・領土への影響は別に見届けよう。"}));
            if(gained.Length+lost.Length>0)
                articles.Add(new(country,"昨日の出来事","わが国の支配地点が変化",new[]{
                    $"獲得 {gained.Length} 地点 ／ 喪失 {lost.Length} 地点",
                    "獲得："+string.Join("、",gained.Take(4).Select(n=>w.Nodes.DisplayName(n))),
                    "喪失："+string.Join("、",lost.Take(4).Select(n=>w.Nodes.DisplayName(n))),
                    "地点の増減です。地域数や、戦闘だけの成果とは異なります。"},gained.Length,lost.Length));
            if(history && gained.Length+lost.Length>0){
                int before=Connections(w,country,previous),after=Connections(w,country,w.Nodes.All.Select(n=>n.Owner).ToArray());
                if(before!=after)articles.Add(new(country,"昨日の出来事",after>before?"わが国の連絡網が分かれた":"わが国の連絡網に変化",new[]{
                    $"自国の機能地点のまとまり：前回 {before} 組 → 現在 {after} 組。",
                    "他国の地点を経由せず、交通路でつながるまとまりを数えました。",
                    "同盟国経由の道や、実際の輸送予約・空き枠は別に確認が必要です。",
                    "戦闘だけの因果や、勝敗の決め手と断定する記事ではありません。"}));
            }
            // Offers are private to the reader's country, not omniscient news about rivals.
            int offers=w.Campaign.Trade.Offers.Count(o=>o.Turn<w.Population.Turn && (o.From.Ruler==country || o.To.Ruler==country));
            if(country==reader && offers>0)
                articles.Add(new(country,"今日の外交予定","交換協議の返事を待つ",new[]{$"わが国が関係する取引 {offers} 件を予定。","この後の外交フェーズで返事を行います。","相手や荷物の状況によって、不成立になることもあります。"}));
            if(own.Length==0)continue;
            int waiting=own.Sum(n=>w.Robots.Workshops[n.Id].ProductionLines.Count(l=>!string.IsNullOrEmpty(l.IntakeFailure)));
            if(waiting>0 && country==reader)
                articles.Add(new(country,"昨日の出来事","工場から入荷先の確認を求める声",new[]{$"入荷先の問題が記録された製造レーン {waiting} 本。","直近の記録です。現在の原材料・空き枠・経路も確認しましょう。","原料を送る、入荷先を変える、空きを作るなどの手があります。"}));
            int fronts=own.Count(n=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Any(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(country,w.Nodes.All[t].Owner)));
            if(country!=reader && gained.Length+lost.Length==0 && (fronts==0 || country%3!=w.Population.Turn%3))continue;
            articles.Add(new(country,"将来の予想・仮説",fronts>0?"もし補給を続けられれば、進出の機会":"もし次の進路を確保できれば",new[]{
                $"現在の機能地点 {own.Length} ／ 敵国への開戦対象を持つ地点 {fronts}。",
                fronts>0?"採取場・工場から前線への道を保てれば、物量を活かせるかもしれません。":"港や同盟国との接続を確かめ、次の進路を探す余地があります。",
                "急所を失えば、在庫が多くても届かなくなる可能性があります。",
                "これは条件付きの見通しです。勝利の保証や確率ではありません。"}));
        }
        // One national publisher chooses one lead story, rather than filling every section.
        int Priority(TurnNewsArticle a)=>a.Kind=="今日の外交予定"?100:
            a.Headline.Contains("連絡網")?90:a.Kind=="昨日の戦闘結果"?80:
            a.Unit=="地域"?75:a.Kind=="昨日の出来事"?70:10;
        return articles.GroupBy(a=>a.Country).Select(g=>g.OrderByDescending(Priority).First() with {Commentary=NewspaperCommentary.Build(w,g.Key,reader)}).ToArray();
    }
    public static int Connections(WorldSetup w,int country,int[] owners)
    {
        var seen=new HashSet<int>();int count=0;
        foreach(var start in w.Nodes.All.Where(n=>n.Functional && owners[n.Id]==country)){
            if(!seen.Add(start.Id))continue;
            count++;var queue=new Queue<int>();queue.Enqueue(start.Id);
            while(queue.TryDequeue(out int at))foreach(var route in w.Routes.Neighbors(w.Nodes.All[at])){
                int next=w.Nodes.At(route.Target,route.Entry).Id;
                if(owners[next]!=country || !seen.Add(next))continue;
                queue.Enqueue(next);
            }
        }
        return count;
    }
}
