namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private Rectangle WarButton=>new(566,1006,200,64);
    private bool _warPlanning;
    // A coordinated attack from several sources to one destination is one reservation.
    private int WarReservationCount=>_setup.Campaign.PlannedOrders.Where(o=>CanPlanRuler(o.Ruler) && (o.Queen || o.Fighters>0) && EnemyTarget(o.TargetNode)).Select(o=>o.TargetNode).Distinct().Count();
    private string WarReservationLabel=>WarReservationCount==0?"開戦予約（なし）":$"開戦予約（{WarReservationCount}件）";
    private sealed class InvasionSource {public int Owner,Node,Count;public bool Queen,CanQueen,CanNode,Selected;public HashSet<long> Robots=new();}
    private readonly List<InvasionSource> _invasionSources=new();
    private int _invasionPage;
    private bool BatteryClick(Point p)
    {
        if(!MapViewBounds.Contains(p) || _movementOpen)return false;
        int ruler=_net.HitBattery(p);if(ruler<0)return false;
        if(ruler==_setup.PlayerSlot){OpenStrategyInspector();OpenPocket();}
        return true;
    }
    private void OpenWarPlanning()
    {
        _withdrawalEncounter=-1;CloseMovement();OpenMovementWithFit(false);_warPlanning=true;_net.AttackTargetMarkers=true;_net.RevealMovementTargets=true;_net.MovementTargets.Clear();
        foreach(var node in _setup.Nodes.All.Where(n=>CanPlanRuler(n.Owner)))
            foreach(var route in _setup.Routes.Neighbors(node))
            {
                int target=_setup.Nodes.At(route.Target,route.Entry).Id;
                if(_setup.Campaign.RobotBattles.CanAttackFrom(_setup,node.Owner,node.Id,target) && EnemyTarget(target))_net.MovementTargets.Add(target);
            }
        foreach(int ruler in Enumerable.Range(0,_setup.ActiveCount).Where(CanPlanRuler))
            foreach(var route in _setup.Routes.NodeChoices(ruler))
            {
                int target=_setup.Nodes.At(route.Target,route.Entry).Id;
                if(_setup.Campaign.RobotBattles.CanMarch(_setup,ruler,target) && EnemyTarget(target))_net.MovementTargets.Add(target);
            }
        _invasionSources.Clear();_invasionPage=0;_invasionPicker=-1;
    }
    private bool CanPlanRuler(int ruler)=>ruler>=0 && _setup.Relations.Controls(ruler) && _setup.Relations.Powered[ruler] && !_setup.Relations.Pending.Any(p=>p.Ruler==ruler);
    private bool EnemyTarget(int id)=>_setup.Nodes.All[id].Owner<0 || !_setup.Relations.Allied(_setup.PlayerSlot,_setup.Nodes.All[id].Owner);
    private void BuildInvasionSources(int target)
    {
        _invasionSources.Clear();_invasionPage=0;_invasionPicker=-1;
        foreach(var node in _setup.Nodes.All.Where(n=>CanPlanRuler(n.Owner) && _setup.Campaign.RobotBattles.CanAttackFrom(_setup,n.Owner,n.Id,target)))
            _invasionSources.Add(new(){Owner=node.Owner,Node=node.Id,CanNode=true});
        foreach(int ruler in Enumerable.Range(0,_setup.ActiveCount).Where(CanPlanRuler).Where(r=>_setup.Campaign.RobotBattles.CanMarch(_setup,r,target)))
        {
            int node=_setup.Nodes.Current(ruler).Id;
            _invasionSources.Add(new(){Owner=ruler,Node=node,Queen=true,CanQueen=true});
        }
        foreach(var row in _invasionSources)
        {
            var order=_setup.Campaign.PlannedOrders.FirstOrDefault(o=>o.Ruler==row.Owner && o.TargetNode==target && (o.Queen || o.PocketOnly)==row.Queen && (o.SourceNode==row.Node || o.Queen && _setup.Nodes.Current(o.Ruler)?.Id==row.Node));
            if(order!=null){row.Selected=order.Queen;var available=InvasionRobots(row);row.Robots.UnionWith((order.RobotIds??available.Take(row.Queen?available.Length:(int)order.Fighters).Select(r=>r.Id).ToArray()).Where(id=>available.Any(r=>r.Id==id)));row.Count=row.Robots.Count;}
        }
    }
    private static Rectangle InvasionRow(int index)=>new(84,300+index*72,760,66);
    private bool InvasionClick(Point p)
    {
        if(SelectedWithdrawal!=null)return WithdrawalClick(p) || new Rectangle(54,150,850,825).Contains(p);
        if(new Rectangle(84,850,110,42).Contains(p)){_invasionPage=Math.Max(0,_invasionPage-1);return true;}
        if(new Rectangle(210,850,110,42).Contains(p)){_invasionPage=Math.Min(Math.Max(0,(_invasionSources.Count-1)/7),_invasionPage+1);return true;}
        if(new Rectangle(570,850,274,42).Contains(p)){_selectedMoveNode=-1;_net.SelectedTarget=-1;_invasionSources.Clear();return true;}
        if(ConfirmMoveButton.Contains(p)){ReserveInvasion();return true;}
        for(int i=0;i<7;i++)
        {
            int index=_invasionPage*7+i;if(index>=_invasionSources.Count)break;
            var row=_invasionSources[index];var r=InvasionRow(i);
            if(new Rectangle(r.X+390,r.Y+7,350,50).Contains(p)){OpenInvasionPicker(index);return true;}
        }
        return false;
    }
    private void ReserveInvasion()
    {
        if(_selectedMoveNode<0)return;
        foreach(var row in _invasionSources)
        {
            _setup.Campaign.CancelReservation(row.Owner,row.Node,row.Queen,_selectedMoveNode);
            if(row.Robots.Count>0 || row.Queen && row.Selected)
                _setup.Campaign.Plan(new(row.Owner,_selectedMoveNode,row.Robots.Count,row.Node,row.Queen && row.Selected,row.Queen,row.Robots.OrderBy(id=>id).ToArray()));
        }
        CloseMovement();
    }
    private void EndPlannedTurn()
    {
        SaveDomesticCamera();CancelTransportModal();
        if(_movementOpen && _selectedMoveNode>=0)
        {if(_warPlanning)ReserveInvasion();else _setup.Campaign.Plan(new(_setup.PlayerSlot,_selectedMoveNode,0,_setup.Nodes.Current(_setup.PlayerSlot).Id,true,true));}
        if(ShowTutorialAttackGuideIfRequired())return;
        CloseMovement();if(_transportOverview)CloseTransportOverview();
        _setup.Campaign.Advance(_setup,-1,0);_world.ShowSetup(_setup,true);_populationCell=-1;OpenBattle();
    }
    private string InvasionTargetLabel=>_selectedMoveNode<0?"地図上で攻めることができる目標が光っています。　目標をクリックしてください。":"侵攻先："+_setup.Nodes.DisplayName(_setup.Nodes.All[_selectedMoveNode]);
    private void DrawInvasionPanel()
    {
        _ui.Box(new(54,150,850,825),new Color(27,49,62));
        _ui.Text("開戦・増援の予約",new(84,177),.8f,Cream);
        if(SelectedWithdrawal!=null){DrawWithdrawal();return;}
        var targetLines=InvasionTargetLabel.Split('　',StringSplitOptions.RemoveEmptyEntries);
        for(int line=0;line<targetLines.Length;line++)_ui.Text(targetLines[line],new(84,235+line*30),Math.Min(.6f,760/_font.MeasureString(targetLines[line]).X),Cream);
        for(int i=0;i<7;i++)
        {
            int index=_invasionPage*7+i;if(index>=_invasionSources.Count)break;
            var source=_invasionSources[index];var r=InvasionRow(i);_ui.Box(r,Muted);
            var label=$"#{source.Owner+1} "+(source.Queen?"電池："+_setup.ConquerorFullNames[source.Owner]:_setup.Nodes.DisplayName(_setup.Nodes.All[source.Node]).Split(" / ")[0]);
            _ui.Text(label,new(r.X+10,r.Y+20),Math.Min(.43f,365/_font.MeasureString(label).X),Cream);
            _ui.Button(new(r.X+390,r.Y+7,350,50),$"ロボット出撃（{source.Robots.Count}体）",source.Robots.Count>0 || source.Selected?Accent:Muted,.47f);
        }
        _ui.Button(new(84,850,110,42),"←",Muted,.5f);_ui.Button(new(210,850,110,42),"→",Muted,.5f);
        if(_selectedMoveNode>=0)_ui.Button(new(570,850,274,42),"目標を選び直す",Muted,.48f);
        _ui.Text("隣接する自国・手下から同時に侵攻 / 継戦中の敵へは増援",new(84,900),.35f,Cream);
        _ui.Button(ConfirmMoveButton,"開戦を予約",_selectedMoveNode>=0?Accent:Muted,.55f);
    }
}
