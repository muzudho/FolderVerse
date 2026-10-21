namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private Captive _dispositionScene;
    private DefeatedChoice _dispositionSceneChoice;
    private double _dispositionSceneAge;
    private void UpdateDispositionScene(double elapsed){if(_dispositionScene!=null){_dispositionSceneAge+=elapsed;if(_dispositionSceneAge>=8)FinishDispositionScene();}}
    private void FinishDispositionScene(){
        _dispositionScene=null;
        if(_setup.Relations.Pending.Count>0){_screen=Screen.Disposition;return;}
        _screen=_dispositionReturnList?Screen.InactiveList:Screen.WorldStatus;_dispositionReturnList=false;_lastAutoSaveTurn=-1;AutoSaveDomestic();
    }
    private void DispositionBubble(Rectangle box,string text){
        _ui.Box(new(box.X-3,box.Y-3,box.Width+6,box.Height+6),new(53,66,77));_ui.Box(box,new(255,245,224));
        _ui.Center(text,box,Math.Min(.52f,(box.Width-30)/_font.MeasureString(text).X),new(43,48,58));
        for(int i=0;i<22;i++)_ui.Box(new(box.Center.X+i/2,box.Bottom+i,22-i,1),new(255,245,224));
    }
    private void DrawDispositionScene(){
        bool removal=_dispositionSceneChoice==DefeatedChoice.RemoveBattery;
        _ui.Text(removal?"電池を引き抜く":"手下になりました",new(54,30),.9f,Cream);
        for(int side=0;side<2;side++){
            int ruler=side==0?_dispositionScene.Victor:_dispositionScene.Ruler,x=side==0?250:1120;
            var portrait=new Rectangle(x,320,500,500);_ui.Box(new(x-5,315,510,510),_setup.OwnerColor(ruler));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],portrait,!removal || side==0 || _dispositionSceneAge<1.2);
            _ui.Center(_setup.ConquerorNames[ruler],new(x,840,500,48),.6f,Cream);
            string line=removal?(side==0?"これでお前は＜ゲーム＞から脱落なのさ！":"やーん、抜かないでーっ"):
                side==0?"期待しているわ。世界征服を手伝ってね。":_setup.ConquerorNames[ruler]+"でーす。これから、よろしくお願いしまーす！";
            DispositionBubble(new(x-180,170,860,115),line);
        }
        if(removal){
            float t=MathHelper.SmoothStep(0,1,(float)Math.Clamp((_dispositionSceneAge-.4)/1.6,0,1));
            var at=Vector2.Lerp(new(1130,760),new(810,590),t);
            _ui.Box(new((int)at.X,(int)at.Y,110,55),new(44,53,62));_ui.Box(new((int)at.X+6,(int)at.Y+6,98,43),new(112,200,123));
            _ui.Box(new((int)at.X+110,(int)at.Y+16,12,24),new(240,216,147));
            _ui.Center("電池",new((int)at.X,(int)at.Y,110,55),.4f,new(25,50,40));
        }
        if(_dispositionSceneAge>=1.2)_ui.Center("クリック / Enter：次へ",new(600,970,720,50),.5f,Cream);
    }
}
