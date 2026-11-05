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
            _invasionPage=_demoWarSource/7;var button=InvasionSortieButton(_demoWarSource%7).Center;target=new(button.X+NodeInspectorOffset,button.Y);
        }
        if(_demoDomesticStage is 33 or 34){
            if(_invasionPicker<0){_demoDomesticStage=32;_demoDomesticAge=0;return true;}
            var robots=InvasionRobots(_invasionSources[_invasionPicker]);
            if(_demoDomesticStage==33 && robots.Length==0 && _invasionQueenDraft)_demoDomesticStage=34;
            target=_demoDomesticStage==34?InvasionPickerDone.Center:robots.Length>0?InvasionRobotCard(0).Center:InvasionQueenToggle.Center;
        }
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
            case 32:InvasionClick(new(target.X-NodeInspectorOffset,target.Y));if(_invasionPicker<0)return true;break;
            case 33:InvasionPickerInput(true,false);break;
            case 34:
                InvasionPickerInput(true,false);
                _demoWarReserved=_setup.Campaign.PlannedOrders.Any(o=>!o.Peaceful && o.TargetNode==_selectedMoveNode && CanPlanRuler(o.Ruler) && (o.Queen || o.Fighters>0));break;
            case 35:DomesticViewClick(target);break;
            case 36:DomesticViewClick(target);_demoDomesticStage=_demoWarReserved?19:17;return true;
        }
        _demoDomesticStage++;return true;
    }
}
