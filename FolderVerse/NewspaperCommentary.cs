namespace FolderVerse;
using System;
using System.Linq;

// Evidence, editorial interpretation and a conditional alternative are separate lines.
// These are observations of the available record, never a claim of counterfactual causality.
public static class NewspaperCommentary
{
    public static string[] Build(WorldSetup w,int country,int reader)
    {
        var previous=w.Campaign.PreviousNodeOwners;
        bool history=previous.Length==w.Nodes.All.Length;
        int lost=history?w.Nodes.All.Count(n=>previous[n.Id]==country&&n.Owner!=country):0;
        var battles=w.Campaign.RobotBattles.Scenes.Where(b=>b.Armies.Any(a=>a.Owner==country)).ToArray();
        int wins=battles.Count(b=>b.Finished&&b.Winner>=0&&w.Relations.Allied(country,b.Winner));
        if(wins>0&&lost>0)return new[]{
            $"確認：戦場で味方側が{wins}勝。一方、前回所有の{lost}地点が自国所有から外れました。",
            "講評：局地の勝利と、国全体の支配を保つことは別の成果です。",
            "仮説：次の補給路も確保できれば、勝利を広げられるかもしれません。"};
        var own=w.Nodes.All.Where(n=>n.Functional&&n.Owner==country).ToArray();
        var wall=history?own.FirstOrDefault(n=>previous[n.Id]==country&&n.Purpose==NodePurpose.Defense&&
            FacilityDevelopment.Grade(w,n)>=1&&!w.Campaign.RobotBattles.Scenes.Any(b=>b.TargetNode==n.Id)):null;
        if(wall!=null&&lost>0)return new[]{
            $"確認：地点{wall.Id+1}の城壁はグレード{FacilityDevelopment.Grade(w,wall)+1}。別の{lost}地点は自国所有外へ。",
            "講評：この城壁は今回の戦場記録に登場せず、守りの配分を点検する余地があります。",
            "仮説：必要な場所への補給も間に合えば、損失を抑えられるかもしれません。"};
        // Rival stocks and production failures are not disclosed through a commentary page.
        if(country!=reader)return Array.Empty<string>();
        bool Front(Node n)=>w.Campaign.RobotBattles.MarchTargets(w,n.Id).Any(t=>w.Nodes.All[t].Owner>=0&&!w.Relations.Allied(country,w.Nodes.All[t].Owner));
        int FreeTroops(Node n)=>w.Robots.Nodes[n.Id].Robots.Count(r=>r.Owner==country&&r.CanFight&&!r.Stationed&&!r.Disposal);
        var fronts=own.Where(Front).ToArray();
        var rear=own.Where(n=>!Front(n)).Select(n=>(Node:n,Count:FreeTroops(n))).OrderByDescending(x=>x.Count).FirstOrDefault();
        var thin=fronts.FirstOrDefault(n=>FreeTroops(n)==0);
        if(rear.Count>=3&&thin!=null)return new[]{
            $"確認：地点{rear.Node.Id+1}に役割札のない戦闘可能機{rear.Count}体。開戦対象のある地点{thin.Id+1}には0体。",
            "講評：在庫の量と、必要な場所へ届く戦力は同じではありません。",
            "仮説：経路・空き枠・趣向を確かめ、輸送や役割変更を検討できそうです。"};
        int waiting=own.Sum(n=>w.Robots.Workshops[n.Id].ProductionLines.Count(l=>!string.IsNullOrEmpty(l.IntakeFailure)));
        if(waiting>0)return new[]{
            $"確認：入荷先の問題が直近に記録された製造レーンは{waiting}本。",
            "講評：製造の計画だけでなく、届け先まで使えるかが成果を左右します。",
            "仮説：現在の経路・所有国・空き枠を確認すれば、再開の手が見つかるかもしれません。"};
        return Array.Empty<string>();
    }
}

