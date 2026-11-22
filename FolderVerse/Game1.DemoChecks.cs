namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private int _demoWarSource;
    private bool _demoWarReserved;
    private bool DemoControlClick(Point target,double elapsed)
    {
        if(_demoSwing<=0)_demoCursor=Vector2.Lerp(_demoCursor,target.ToVector2(),1-MathF.Exp(-(float)elapsed*4));
        _demoDomesticAge+=elapsed;if(_demoDomesticAge<1.8)return false;
        _demoDomesticAge=0;_demoCursor=target.ToVector2();_pointer=target;_ui.Pointer=target;StartDemoClick();return true;
    }
    private bool UpdateDemoWarChecks(double elapsed)
    {
        Point target=Point.Zero;
        if(_demoDomesticStage==30){_demoWarReserved=false;target=WarButton.Center;}
        if(_demoDomesticStage==31){
            foreach(int id in _net.AttackHighlightTargets){
                if(!OperationPosition(id,out var at))continue;var p=at.ToPoint();
                if((_statusGlobe?HitGlobeNode(p,_net.MovementTargets):_net.HitNode(_setup,NetPanel,p,_net.MovementTargets))==id){target=p;break;}
            }
            if(target==Point.Zero){
                if(_net.AttackHighlightTargets.Count==0){CloseNodeInspector();_demoDomesticStage=17;}
                else FocusDemoNode(_setup.Nodes.All[_net.AttackHighlightTargets.OrderBy(id=>id).First()]);
                _demoDomesticAge=0;return true;
            }
        }
        if(_demoDomesticStage==32){
            _demoWarSource=Enumerable.Range(0,_invasionSources.Count).FirstOrDefault(i=>InvasionRobots(_invasionSources[i]).Length>0,-1);
            if(_demoWarSource<0)_demoWarSource=_invasionSources.FindIndex(r=>r.Queen);
            if(_demoWarSource<0){_demoDomesticStage=36;_demoDomesticAge=0;return true;}
            _invasionPage=InvasionPageForSource(_demoWarSource);
            var row=_invasionSources[_demoWarSource];
            var bounds=InvasionVisibleRows().Single(layout=>layout.Index==_demoWarSource).Bounds;
            var robots=InvasionRobots(row);var previews=InvasionPreviewRobots(row);
            int slot=robots.Length>0?Array.FindIndex(previews,robot=>robot.Id==robots[0].Id):-1;
            if(robots.Length>0 && slot<0){_demoDomesticStage=36;_demoDomesticAge=0;return true;}
            target=InspectorScreenBounds(slot>=0?InvasionCardAt(bounds,slot):InvasionQueenAt(bounds)).Center;
        }
        if(_demoDomesticStage is 33 or 34){_demoDomesticStage=35;_demoDomesticAge=0;return true;}
        if(_demoDomesticStage==35){
            EnsureNodeCheckContext();bool marked=_warChecksByPlayer.TryGetValue(NodeCheckPlayer,out var checks) && checks.Contains(_selectedMoveNode);
            if(!_demoWarReserved || !NodeCheckButtonVisible || marked){_demoDomesticStage=36;_demoDomesticAge=0;return true;}
            target=NodeCheckButton.Center;
        }
        if(_demoDomesticStage==36)target=InspectorClose.Center;
        if(!DemoControlClick(target,elapsed))return true;
        switch(_demoDomesticStage){
            case 30:MapOverlayClick(target,false);break;
            case 31:MovementClick(target,new KeyboardState());if(_selectedMoveNode<0)return true;break;
            case 32:
                InvasionClick(new Point(target.X-NodeInspectorOffset,target.Y));
                _demoWarReserved=_setup.Campaign.PlannedOrders.Any(o=>!o.Peaceful && o.TargetNode==_selectedMoveNode && CanPlanRuler(o.Ruler) && (o.Queen || o.Fighters>0));break;
            case 35:DomesticViewClick(target);break;
            case 36:DomesticViewClick(target);_demoDomesticStage=_demoWarReserved?19:17;return true;
        }
        _demoDomesticStage++;return true;
    }
}
