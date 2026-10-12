namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private static readonly Rectangle AutoMenuItem=new(1320,294,538,52);
    private bool _autoPlay;
    private double _autoAge,_autoStepAge;
    private void StartAutoPlay()
    {
        CloseMovement();_statusMenuOpen=false;_populationCell=-1;
        _nodeAssemblyOpen=_nodeTransportOpen=_nodeDisposalOpen=_nodeFactoryOpen=false;
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
            case Screen.WorldStatus:
                if(_autoStepAge<2)return true;
                _autoStepAge=0;
                _setup.Campaign.Advance(_setup,-1,0,automaticAll:true);
                _world.ShowSetup(_setup,true);OpenBattle();
                break;
            case Screen.Transport:UpdateTransport(elapsed,false,now);_autoStepAge=0;break;
            case Screen.Battle:
                if(_robotSummary){if(_autoStepAge>=3){RobotBattleInput(0,false,true);_autoStepAge=0;}}
                else{RobotBattleInput(elapsed,false,false);_autoStepAge=0;}
                break;
            case Screen.Disposition:
                if(_autoStepAge>=2){_pointer=DispositionButton(DemoDispositionChoice()).Center;DispositionInput(true,false);_autoStepAge=0;}
                break;
        }
        return true;
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