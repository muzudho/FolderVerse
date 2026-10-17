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
    private sealed class InvasionSource {public int Owner,Node,Count;public bool Queen,CanQueen,CanNode,Selected;}
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
        _withdrawalEncounter=-1;CloseMovement();OpenMovementWithFit(false);_warPlanning=true;_net.RevealMovementTargets=true;_net.MovementTargets.Clear();
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
        _invasionSources.Clear();_invasionPage=0;
    }
    private bool CanPlanRuler(int ruler)=>ruler>=0 && _setup.Relations.Controls(ruler) && _setup.Relations.Powered[ruler] && !_setup.Relations.Pending.Any(p=>p.Ruler==ruler);
    private bool EnemyTarget(int id)=>_setup.Nodes.All[id].Owner<0 || !_setup.Relations.Allied(_setup.PlayerSlot,_setup.Nodes.All[id].Owner);
    private void BuildInvasionSources(int target)
    {
        _invasionSources.Clear();_invasionPage=0;
        foreach(var node in _setup.Nodes.All.Where(n=>CanPlanRuler(n.Owner) && _setup.Campaign.RobotBattles.CanAttackFrom(_setup,n.Owner,n.Id,target)))
            _invasionSources.Add(new(){Owner=node.Owner,Node=node.Id,CanNode=true});
        foreach(int ruler in Enumerable.Range(0,_setup.ActiveCount).Where(CanPlanRuler).Where(r=>_setup.Campaign.RobotBattles.CanMarch(_setup,r,target)))
        {
            int node=_setup.Nodes.Current(ruler).Id;
            _invasionSources.Add(new(){Owner=ruler,Node=node,Queen=true,CanQueen=true});
        }
        foreach(var row in _invasionSources)
        {
            var order=_setup.Campaign.PlannedOrders.FirstOrDefault(o=>o.Ruler==row.Owner && o.TargetNode==target && o.Queen==row.Queen && (o.SourceNode==row.Node || o.Queen && _setup.Nodes.Current(o.Ruler)?.Id==row.Node));
            if(order!=null){row.Selected=true;row.Count=row.Queen?0:(int)order.Fighters;}
        }
    }
    private static Rectangle InvasionRow(int index)=>new(84,300+index*72,760,66);
    private bool InvasionClick(Point p)
    {
        if(SelectedWithdrawal!=null)return WithdrawalClick(p) || new Rectangle(54,150,850,825).Contains(p);
        if(new Rectangle(84,850,110,42).Contains(p)){_invasionPage=Math.Max(0,_invasionPage-1);return true;}
        if(new Rectangle(210,850,110,42).Contains(p)){_invasionPage=Math.Min(Math.Max(0,(_invasionSources.Count-1)/7),_invasionPage+1);return true;}
        if(ConfirmMoveButton.Contains(p)){ReserveInvasion();return true;}
        for(int i=0;i<7;i++)
        {
            int index=_invasionPage*7+i;if(index>=_invasionSources.Count)break;
            var row=_invasionSources[index];var r=InvasionRow(i);
            if(new Rectangle(r.X+300,r.Y,170,r.Height).Contains(p) && row.CanQueen){row.Selected=!row.Selected;return true;}
            int max=row.Queen || !row.CanNode?0:_setup.Campaign.RobotBattles.AvailableFrom(_setup,row.Owner,row.Node);
            if(new Rectangle(r.X+490,r.Y,60,r.Height).Contains(p)){row.Count=Math.Max(0,row.Count-1);return true;}
            if(new Rectangle(r.X+680,r.Y,60,r.Height).Contains(p)){row.Count=Math.Min(max,row.Count+1);return true;}
        }
        return false;
    }
    private void ReserveInvasion()
    {
        if(_selectedMoveNode<0)return;
        foreach(var row in _invasionSources)
        {
            _setup.Campaign.CancelReservation(row.Owner,row.Node,row.Queen,_selectedMoveNode);
            if(row.Count>0 || row.Queen && row.Selected)
                _setup.Campaign.Plan(new(row.Owner,_selectedMoveNode,row.Count,row.Node,row.Queen,row.Queen));
        }
        CloseMovement();
    }
    private void EndPlannedTurn()
    {
        SaveDomesticCamera();CancelTransportModal();
        if(_movementOpen && _selectedMoveNode>=0)
        {if(_warPlanning)ReserveInvasion();else _setup.Campaign.Plan(new(_setup.PlayerSlot,_selectedMoveNode,0,_setup.Nodes.Current(_setup.PlayerSlot).Id,true,true));}
        CloseMovement();if(_transportOverview)CloseTransportOverview();
        _setup.Campaign.Advance(_setup,-1,0);_world.ShowSetup(_setup,true);_populationCell=-1;OpenBattle();
    }
    private string InvasionTargetLabel=>_selectedMoveNode<0?"明るい敵節点を選んでください":"侵攻先："+_setup.Nodes.DisplayName(_setup.Nodes.All[_selectedMoveNode]);
    private void DrawInvasionPanel()
    {
        _ui.Box(new(54,150,850,825),new Color(27,49,62));
        _ui.Text("開戦・増援の予約",new(84,177),.8f,Cream);
        if(SelectedWithdrawal!=null){DrawWithdrawal();return;}
        var targetLabel=InvasionTargetLabel;_ui.Text(targetLabel,new(84,245),Math.Min(.6f,760/_font.MeasureString(targetLabel).X),Cream);
        for(int i=0;i<7;i++)
        {
            int index=_invasionPage*7+i;if(index>=_invasionSources.Count)break;
            var source=_invasionSources[index];var r=InvasionRow(i);_ui.Box(r,Muted);
            var label=$"#{source.Owner+1} "+(source.Queen?"電池："+_setup.ConquerorFullNames[source.Owner]:_setup.Nodes.DisplayName(_setup.Nodes.All[source.Node]).Split(" / ")[0]);
            _ui.Text(label,new(r.X+10,r.Y+20),Math.Min(.43f,280/_font.MeasureString(label).X),Cream);
            _ui.Button(new(r.X+300,r.Y+7,170,50),source.Queen?(source.Selected?"征服者が出撃":"征服者は待機"):"格納庫が出撃",source.Selected?Accent:Muted,.4f);
            if(!source.Queen)_ui.Button(new(r.X+490,r.Y+7,60,50),"−",Muted,.6f);
            _ui.Center(source.Queen?$"直属 {_setup.Robots.Retinues[source.Owner].Robots.Count(r=>r.CanFight)}体":$"{source.Count}/{_setup.Campaign.RobotBattles.AvailableFrom(_setup,source.Owner,source.Node)}体",new(r.X+550,r.Y,130,66),.48f,Cream);
            if(!source.Queen)_ui.Button(new(r.X+680,r.Y+7,60,50),"＋",Muted,.6f);
        }
        _ui.Button(new(84,850,110,42),"←",Muted,.5f);_ui.Button(new(210,850,110,42),"→",Muted,.5f);
        _ui.Text("隣接する自国・手下から同時に侵攻 / 継戦中の敵へは増援",new(84,900),.35f,Cream);
        _ui.Button(ConfirmMoveButton,"開戦を予約",_selectedMoveNode>=0?Accent:Muted,.55f);
    }
}
