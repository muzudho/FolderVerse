namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private static readonly Rectangle WarButton=new(474,1018,110,52);
    private bool _warPlanning;
    private sealed class InvasionSource {public int Owner,Node,Count;public bool Queen,CanQueen,CanNode;}
    private readonly List<InvasionSource> _invasionSources=new();
    private int _invasionPage;
    private void OpenWarPlanning()
    {
        CloseMovement();OpenMovement();_warPlanning=true;_net.MovementTargets.Clear();
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
        _invasionSources.Clear();_invasionPage=0;if(_statusGlobe)FitDestinationView();
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
            var row=_invasionSources.FirstOrDefault(s=>s.Owner==ruler && s.Node==node);
            if(row==null){row=new(){Owner=ruler,Node=node,Queen=false};_invasionSources.Add(row);}
            row.CanQueen=true;
        }
        foreach(var row in _invasionSources)
        {
            var order=_setup.Campaign.PlannedOrders.FirstOrDefault(o=>o.Ruler==row.Owner && o.TargetNode==target && (o.SourceNode==row.Node || o.Queen && _setup.Nodes.Current(o.Ruler)?.Id==row.Node));
            if(order!=null){row.Queen=order.Queen;row.Count=(int)order.Fighters;}
        }
    }
    private static Rectangle InvasionRow(int index)=>new(84,300+index*72,760,66);
    private bool InvasionClick(Point p)
    {
        if(new Rectangle(84,850,110,42).Contains(p)){_invasionPage=Math.Max(0,_invasionPage-1);return true;}
        if(new Rectangle(210,850,110,42).Contains(p)){_invasionPage=Math.Min(Math.Max(0,(_invasionSources.Count-1)/7),_invasionPage+1);return true;}
        if(ConfirmMoveButton.Contains(p)){ReserveInvasion();return true;}
        for(int i=0;i<7;i++)
        {
            int index=_invasionPage*7+i;if(index>=_invasionSources.Count)break;
            var row=_invasionSources[index];var r=InvasionRow(i);
            if(new Rectangle(r.X+300,r.Y,170,r.Height).Contains(p) && row.CanQueen){row.Queen=!row.Queen;return true;}
            int max=row.Queen || !row.CanNode?0:_setup.Campaign.RobotBattles.AvailableFrom(_setup,row.Owner,row.Node);
            if(new Rectangle(r.X+490,r.Y,60,r.Height).Contains(p)){row.Count=Math.Max(0,row.Count-1);return true;}
            if(new Rectangle(r.X+680,r.Y,60,r.Height).Contains(p)){row.Count=Math.Min(max,row.Count+1);return true;}
        }
        return false;
    }
    private void ReserveInvasion()
    {
        if(_selectedMoveNode<0)return;
        foreach(var row in _invasionSources.Where(s=>s.Count>0 || s.Queen))
            _setup.Campaign.Plan(new(row.Owner,_selectedMoveNode,row.Count,row.Node,row.Queen));
        CloseMovement();
    }
    private void EndPlannedTurn()
    {
        if(_movementOpen && _selectedMoveNode>=0)
        {if(_warPlanning)ReserveInvasion();else _setup.Campaign.Plan(new(_setup.PlayerSlot,_selectedMoveNode,0,_setup.Nodes.Current(_setup.PlayerSlot).Id));}
        CloseMovement();if(_transportOverview)CloseTransportOverview();
        _setup.Campaign.Advance(_setup,-1,0);_world.ShowSetup(_setup,true);_populationCell=-1;OpenBattle();
    }
    private void DrawInvasionPanel()
    {
        _ui.Box(new(54,150,850,825),new Color(27,49,62));
        _ui.Text("開戦・増援の予約",new(84,177),.8f,Cream);_ui.Button(new(803,169,58,44),"×",Muted,.7f);
        _ui.Text(_selectedMoveNode<0?"明るい敵節点を選んでください":$"侵攻先：Node {_selectedMoveNode+1}",new(84,245),.6f,Cream);
        for(int i=0;i<7;i++)
        {
            int index=_invasionPage*7+i;if(index>=_invasionSources.Count)break;
            var source=_invasionSources[index];var r=InvasionRow(i);_ui.Box(r,Muted);
            _ui.Text($"#{source.Owner+1} Node {source.Node+1}",new(r.X+10,r.Y+20),.43f,Cream);
            _ui.Button(new(r.X+300,r.Y+7,170,50),source.Queen?"征服者が出撃":source.CanNode?"節点が攻撃":"征服者は待機",source.CanQueen?Accent:Muted,.4f);
            _ui.Button(new(r.X+490,r.Y+7,60,50),"−",Muted,.6f);
            _ui.Center(source.Queen?$"直属 {_setup.Robots.Retinues[source.Owner].Robots.Count(r=>r.CanFight)}体":$"{source.Count}/{_setup.Campaign.RobotBattles.AvailableFrom(_setup,source.Owner,source.Node)}体",new(r.X+550,r.Y,130,66),.48f,Cream);
            _ui.Button(new(r.X+680,r.Y+7,60,50),"＋",Muted,.6f);
        }
        _ui.Button(new(84,850,110,42),"←",Muted,.5f);_ui.Button(new(210,850,110,42),"→",Muted,.5f);
        _ui.Text("隣接する自国・手下から同時に侵攻 / 継戦中の敵へは増援",new(84,900),.35f,Cream);
        _ui.Button(ConfirmMoveButton,"開戦を予約",_selectedMoveNode>=0?Accent:Muted,.55f);
    }
}
