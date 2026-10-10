namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private double _titleIdle,_demoAge,_demoStepAge;
    private bool _demoActive;
    private float _demoFade=-1;
    private Vector2 _demoCursor=new(960,540);
    private bool UpdateDemo(double elapsed,bool active,bool click,bool escape,bool enter)
    {
        if(_demoFade>=0)
        {
            _demoFade+=(float)elapsed;
            if(_demoFade>=.8f && _demoActive){_demoActive=false;ReturnToTitle();_titleIdle=0;IsMouseVisible=true;}
            if(_demoFade>=1.4f)_demoFade=-1;
            return true;
        }
        if(!_demoActive)
        {
            if(_screen!=Screen.Title){_titleIdle=0;return false;}
            if(click || enter || escape){_titleIdle=0;return false;}
            if(active)_titleIdle+=elapsed;
            if(_titleIdle<10)return false;
            _demoActive=true;_demoAge=_demoStepAge=0;IsMouseVisible=false;BeginRolling();
            return true;
        }
        if(click && new Rectangle(0,0,1920,1080).Contains(_pointer) || escape){_demoFade=0;return true;}
        if(!active)return true;
        _demoAge+=elapsed;_demoStepAge+=elapsed;_yaw+=(float)elapsed*.12f;
        var aim=_screen==Screen.WorldStatus?new Vector2(720,1040):_screen is Screen.Battle or Screen.Transport?new Vector2(1620,1040):new Vector2(1580,790);
        _demoCursor=Vector2.Lerp(_demoCursor,aim,1-MathF.Exp(-(float)elapsed*2));
        if(_screen==Screen.Transport){UpdateTransport(elapsed,false);_demoStepAge=0;return true;}
        if(_screen==Screen.Battle)
        {
            if(_robotSummary){if(_demoStepAge>3){RobotBattleInput(0,false,true);_demoStepAge=0;}}
            else{RobotBattleInput(elapsed,false,false);_demoStepAge=0;}
            return true;
        }
        if(_demoStepAge<3)return true;
        _demoStepAge=0;
        switch(_screen)
        {
            case Screen.Rolling:_screen=Screen.Review;break;
            case Screen.Review:BeginCast();break;
            case Screen.CastRolling:_screen=Screen.CastReview;break;
            case Screen.CastReview:BeginPlacement();break;
            case Screen.PlacementRolling:_screen=Screen.PlacementReview;break;
            case Screen.PlacementReview:BeginSelection();break;
            case Screen.PlayerSelect:_setup.SelectPlayer(0);_screen=Screen.PlayerReady;break;
            case Screen.PlayerReady:_net.Home(_setup);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;break;
            case Screen.WorldStatus:
                var home=_setup.Nodes.Current(_setup.PlayerSlot);
                var path=home==null?null:_setup.Routes.Neighbors(home).FirstOrDefault(r=>_setup.Campaign.RobotBattles.CanMarch(_setup,_setup.PlayerSlot,_setup.Nodes.At(r.Target,r.Entry).Id));
                int target=path==null?-1:_setup.Nodes.At(path.Target,path.Entry).Id;
                _setup.Campaign.Advance(_setup,target,target<0?0:_setup.Campaign.RobotBattles.Available(_setup,_setup.PlayerSlot));
                _world.ShowSetup(_setup,true);OpenBattle();break;
            case Screen.Disposition:DispositionInput(true,false);break;
            default:BeginRolling();break;
        }
        if(_screen==Screen.Disposition){_pointer=DispositionButton(0).Center;DispositionInput(true,false);}
        return true;
    }
    private void DrawDemoOverlay(Matrix transform)
    {
        if(!_demoActive && _demoFade<0)return;
        _spriteBatch.Begin(transformMatrix:transform);
        if(_demoActive)
        {
            _ui.Box(new(20,20,390,40),new Color(16,35,46,220));
            _ui.Text("DEMO / Click to return",new(32,25),.43f,Cream);
            int x=(int)_demoCursor.X,y=(int)_demoCursor.Y;
            // A small pink pointer with a heart, drawn independently of the system cursor.
            for(int row=0;row<24;row++)_ui.Box(new(x,y+row,Math.Max(2,row/2+2),1),new Color(255,193,210));
            _ui.Tile(new(x+11,y+24),new(5,16),-.4f,Color.White);
            _ui.Tile(new(x+29,y+9),new(11,11),MathHelper.PiOver4,new Color(242,132,163));
            _ui.Box(new(x+21,y,10,8),new Color(242,132,163));_ui.Box(new(x+29,y,10,8),new Color(242,132,163));
        }
        if(_demoFade>=0)
        {
            float opacity=_demoFade<.8f?_demoFade/.8f:1-(_demoFade-.8f)/.6f;
            _ui.Box(new(0,0,1920,1080),Color.Black*Math.Clamp(opacity,0,1));
        }
        _spriteBatch.End();
    }
}
