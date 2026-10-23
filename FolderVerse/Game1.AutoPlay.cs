namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private static readonly Rectangle AutoMenuItem=new(1320,294,538,52);
    private bool _autoPlay;
    private double _autoAge,_autoStepAge;
    private int _autoDomesticStage;
    private double _autoDomesticAge;
    private void StartAutoPlay()
    {
        CloseMovement();_statusMenuOpen=false;_populationCell=-1;
        _nodeAssemblyOpen=_nodeTransportOpen=_nodeDisposalOpen=_nodeFactoryOpen=false;
        ResetInspector();_autoDomesticStage=0;_autoDomesticAge=0;
        _autoPlay=true;_autoAge=_autoStepAge=0;_robotPlaying=true;
    }
    private bool UpdateAutoPlay(double elapsed,bool click,bool escape,double now)
    {
        if(!_autoPlay)return false;
        if(click || escape){_autoPlay=false;_autoStepAge=0;return true;}
        _autoAge+=elapsed;_autoStepAge+=elapsed;
        _net.UpdateAnimation((float)elapsed);UpdateMapTransition((float)elapsed);
        switch(_screen)
        {
            case Screen.Industry:UpdateIndustry(elapsed,false);break;
            case Screen.BattleResearch:UpdateBattleResearch(elapsed,false);break;
            case Screen.WorldStatus:
                UpdateAutoDomestic(elapsed);
                break;
            case Screen.Transport:UpdateTransport(elapsed,false,now);_autoStepAge=0;break;
            case Screen.Battle:
                if(_robotSummary)UpdateAutomaticSummary(elapsed,false);
                else{_robotPlaying=true;RobotBattleInput(elapsed,false,false);_autoStepAge=0;}
                break;
            case Screen.Disposition:
                if(_autoStepAge>=2){_pointer=DispositionButton(DemoDispositionChoice()).Center;DispositionInput(true,false);_autoStepAge=0;}
                break;
        }
        return true;
    }
    private void UpdateAutoDomestic(double elapsed)
    {
        _autoDomesticAge+=elapsed;
        if(_autoDomesticAge<2)return;
        _autoDomesticAge=0;
        switch(_autoDomesticStage++){
            case 0:
                var node=_setup.Nodes.All.Where(n=>n.Owner==_setup.PlayerSlot && !_setup.Campaign.RobotBattles.LockedNode(n.Id)).OrderByDescending(n=>_setup.Robots.AssemblySelection(n.Id,_setup.PlayerSlot,true)!=null).FirstOrDefault();
                if(node==null){_autoDomesticStage=5;break;}OpenNodeInspector(node.Id);break;
            case 1:OpenNodeTransport();break;
            case 2:OpenNodeAssembly();break;
            case 3:
                var ids=_setup.Robots.AssemblySelection(_populationNode,_setup.Nodes.All[_populationNode].Owner,true);
                if(ids!=null){_setup.Robots.Assemble(_populationNode,ids,_setup.Nodes.All[_populationNode].Owner);SyncHangarSlots();_hangarMessage="出撃できるロボットを組み立てました";}break;
            case 4:OpenNodeFactory();break;
            default:
                ResetInspector();_autoDomesticStage=0;_autoStepAge=0;
                _setup.Campaign.Advance(_setup,-1,0,automaticAll:true);_world.ShowSetup(_setup,true);OpenBattle();break;
        }
    }
    private void DrawAutoLabel(Matrix transform)
    {
        float time=(float)(_autoAge%9);if(time>=2.4f)return;
        float fade=Math.Clamp(Math.Min(time/.25f,(2.4f-time)/.4f),0,1);
        _spriteBatch.Begin(transformMatrix:transform);
        for(int i=0;i<4;i++){
            int jump=(int)(Math.Abs(MathF.Sin(time*5-i*.5f))*16);
            _counter.DrawText(_spriteBatch,"AUTO"[i].ToString(),new(860+i*50,510-jump,46,48),(i%2==0?new Color(255,187,214):new Color(185,230,231))*fade);
        }
        _spriteBatch.End();
    }
}
