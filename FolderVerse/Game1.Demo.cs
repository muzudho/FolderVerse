namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private double _titleIdle,_demoAge,_demoStepAge;
    private bool _demoActive;
    private bool _demoIntro;
    private int _demoFinishStage;
    private float _demoSwing;
    private float _demoStarsAge=-1;
    private Vector2 _demoClickPoint;
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
            _titleIdle+=elapsed;
            if(_titleIdle<10)return false;
            _demoActive=true;_demoIntro=true;_demoAge=_demoStepAge=0;_demoSwing=0;
            _demoFinishStage=0;
            _demoCursor=new(2040,1140);IsMouseVisible=false;
            return true;
        }
        if(click && new Rectangle(0,0,1920,1080).Contains(_pointer) || escape){_demoFade=0;return true;}
        _demoAge+=elapsed;_demoStepAge+=elapsed;_yaw+=(float)elapsed*.12f;
        if(_demoStarsAge>=0){_demoStarsAge+=(float)elapsed;if(_demoStarsAge>.65f)_demoStarsAge=-1;}
        float previousSwing=_demoSwing;
        _demoSwing=Math.Max(0,_demoSwing-(float)elapsed);
        if(previousSwing>0 && _demoSwing==0)_demoStarsAge=0;
        if(_demoIntro)
        {
            float t=MathHelper.SmoothStep(0,1,(float)Math.Clamp(_demoAge/1.8,0,1));
            _demoCursor=Vector2.Lerp(new(2040,1140),new(StartButton.Center.X,StartButton.Center.Y),t);
            if(_demoAge>=1.8 && _demoAge-elapsed<1.8)StartDemoClick();
            if(_demoAge>=2.2){_demoIntro=false;_demoStepAge=0;BeginRolling();}
            return true;
        }
        if(_demoAge>20*60 && _screen==Screen.WorldStatus && _demoFinishStage==0)
        {_demoFinishStage=1;_demoStepAge=0;}
        if(_demoFinishStage>0)
        {
            var button=_demoFinishStage==1?StatusMenuButton:_demoFinishStage==2?EndingMenuItem:EndingReturn;
            _demoCursor=Vector2.Lerp(_demoCursor,new(button.Center.X,button.Center.Y),1-MathF.Exp(-(float)elapsed*4));
            if(_demoFinishStage==3)_endingAge+=elapsed;
            double wait=_demoFinishStage==3?8:1;
            if(_demoStepAge>=wait && _demoStepAge-elapsed<wait)
            {_demoCursor=new(button.Center.X,button.Center.Y);StartDemoClick();}
            if(_demoStepAge>=wait+.4)
            {
                _demoStepAge=0;
                if(_demoFinishStage<=2)
                {StatusMenuClick(button.Center);_demoFinishStage++;}
                else
                {_demoActive=false;_demoFinishStage=0;ReturnToTitle();_titleIdle=0;IsMouseVisible=true;}
            }
            return true;
        }
        var dispositionPoint=_screen==Screen.Disposition?DispositionButton(DemoDispositionChoice()).Center:Point.Zero;
        var aim=_screen==Screen.Disposition?new Vector2(dispositionPoint.X,dispositionPoint.Y):_screen==Screen.WorldStatus?new Vector2(720,1040):_screen is Screen.Battle or Screen.Transport?new Vector2(1840,850):new Vector2(1580,790);
        if(_demoSwing<=0)_demoCursor=Vector2.Lerp(_demoCursor,aim,1-MathF.Exp(-(float)elapsed*2));
        if(_screen==Screen.Transport){UpdateTransport(elapsed,false);_demoStepAge=0;return true;}
        if(_screen==Screen.Battle)
        {
            if(_robotSummary){if(_demoStepAge>3){RobotBattleInput(0,false,true);_demoStepAge=0;}}
            else{RobotBattleInput(elapsed,false,false);_demoStepAge=0;}
            return true;
        }
        if(_demoStepAge<3)return true;
        _demoStepAge=0;
        StartDemoClick();
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
            case Screen.Disposition:_pointer=dispositionPoint;DispositionInput(true,false);break;
            default:BeginRolling();break;
        }
        return true;
    }
    private int DemoDispositionChoice()=>_setup.Relations.Accepts(_setup,_setup.Relations.Pending[0].Ruler)?0:1;
    private void StartDemoClick(){_demoSwing=.4f;_demoClickPoint=_demoCursor;}
    private static Vector2 RotateDemoOffset(Vector2 offset,float angle)
        =>new(offset.X*MathF.Cos(angle)-offset.Y*MathF.Sin(angle),offset.X*MathF.Sin(angle)+offset.Y*MathF.Cos(angle));
    private void DrawDemoOverlay(Matrix transform)
    {
        if(!_demoActive && _demoFade<0)return;
        _spriteBatch.Begin(transformMatrix:transform);
        if(_demoActive)
        {
            _ui.Box(new(20,20,390,40),new Color(16,35,46,220));
            _ui.Text("DEMO / Click to return",new(32,25),.43f,Cream);
            const float restAngle=-.28f;
            float swing=_demoSwing>0?MathF.Sin((1-_demoSwing/.4f)*MathHelper.Pi)*.42f:0;
            float scale=116f/_demoLollipop.Height;
            // The grip remains fixed; the candy end lands on the intended click point.
            var origin=new Vector2(_demoLollipop.Width*.5f,_demoLollipop.Height*.94f);
            var tip=new Vector2(_demoLollipop.Width*.5f,_demoLollipop.Height*.045f);
            var contact=_demoSwing>0?_demoClickPoint:_demoCursor;
            var grip=contact-RotateDemoOffset((tip-origin)*scale,restAngle);
            _spriteBatch.Draw(_demoLollipop,grip,null,Color.White,restAngle+swing,origin,scale,SpriteEffects.None,0);
            if(_demoStarsAge>=0)
            for(int i=0;i<7;i++)
            {
                float angle=i*MathHelper.TwoPi/7;
                var center=_demoClickPoint+new Vector2(MathF.Cos(angle),MathF.Sin(angle))*(6+_demoStarsAge*65)+new Vector2(0,_demoStarsAge*_demoStarsAge*22);
                float radius=5*(1-_demoStarsAge/.65f);
                Color tint=(i%2==0?new Color(255,225,153):new Color(255,181,208))*(1-_demoStarsAge/.65f);
                for(int edge=0;edge<10;edge++)
                {
                    Vector2 PointAt(int vertex){float a=vertex*MathHelper.TwoPi/10-MathHelper.PiOver2;return center+new Vector2(MathF.Cos(a),MathF.Sin(a))*(vertex%2==0?radius:radius*.45f);}
                    var a=PointAt(edge);var b=PointAt(edge+1);var delta=b-a;
                    _ui.Tile((a+b)/2,new(delta.Length()+1,1.5f),MathF.Atan2(delta.Y,delta.X),tint);
                }
            }
        }
        if(_demoFade>=0)
        {
            float opacity=_demoFade<.8f?_demoFade/.8f:1-(_demoFade-.8f)/.6f;
            _ui.Box(new(0,0,1920,1080),Color.Black*Math.Clamp(opacity,0,1));
        }
        _spriteBatch.End();
    }
}
