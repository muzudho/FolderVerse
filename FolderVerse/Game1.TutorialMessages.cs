namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private bool _tutorialIntroShown,_tutorialMessagesOpen;
    private int _tutorialMessageSelection,_tutorialGuideMessage=1;
    private static readonly string[] TutorialMessageTitles={"内政開始の案内","開戦予約の案内","開戦の実績解除"};
    private static readonly string[][] TutorialMessageLines={
        new[]{"さあ、戦争の始め方を覚えましょう！","［開戦予約］ボタンを押してください！"},
        new[]{"待った～！　ステップ１では、戦争の始め方を覚えてみましょう！","まず［開戦予約］ボタンを押して出撃を予約してから、","［ターン終了］ボタンを押してください！"},
        new[]{"［開戦］を実績解除しました！"}
    };
    private bool TutorialReviewAvailable=>_setup.TutorialStep==1 && _screen!=Screen.Title && _screen!=Screen.PlayerCount;
    private Rectangle TutorialReviewButton=>_screen==Screen.Battle?new(55,1046,400,30):new(1445,20,195,48);
    private static Rectangle TutorialMessageRow(int i)=>new(400,315+i*100,360,75);
    private static Rectangle TutorialMessageClose=>new(1150,800,360,60);
    private void UpdateTutorialStartGuide(double now){
        if(_tutorialIntroShown || !TutorialAttackRequired || !IsStatusScreen || _autoPlay || _demoActive ||
            _lastCutInPhase!="内政フェーズ" || now-_cutInStarted<PhaseCutInSeconds)return;
        _tutorialIntroShown=true;_tutorialGuideMessage=0;_tutorialAttackGuide=true;_tutorialGuideStarted=_animationTime;
    }
    private bool TutorialMessageInput(bool click,bool escape){
        if(_tutorialMessagesOpen){
            if(escape || click && TutorialMessageClose.Contains(_pointer)){_tutorialMessagesOpen=false;return true;}
            if(click)for(int i=0;i<TutorialMessageTitles.Length;i++)if(TutorialMessageRow(i).Contains(_pointer))_tutorialMessageSelection=i;
            return true;
        }
        if(TutorialReviewAvailable && _tutorialAchievementAge<0 && click && TutorialReviewButton.Contains(_pointer)){
            _tutorialMessagesOpen=true;_tutorialMessageSelection=0;return true;
        }
        return false;
    }
    private void DrawTutorialMessages(){
        if(!TutorialReviewAvailable)return;
        _ui.Button(TutorialReviewButton,"チュートリアルを見返す",Muted,.3f);
        if(!_tutorialMessagesOpen)return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.65f);_ui.Box(new(350,170,1220,760),new Color(25,42,56));
        _ui.Text("チュートリアルを見返す",new(400,215),.8f,Cream);
        for(int i=0;i<TutorialMessageTitles.Length;i++)_ui.Button(TutorialMessageRow(i),TutorialMessageTitles[i],i==_tutorialMessageSelection?Accent:Muted,.48f);
        _ui.Box(new(790,300,730,450),Cream);
        var lines=TutorialMessageLines[_tutorialMessageSelection];
        for(int i=0;i<lines.Length;i++)_ui.Text(lines[i],new(825,365+i*85),Math.Min(.46f,655/_font.MeasureString(lines[i]).X),Muted);
        _ui.Button(TutorialMessageClose,"閉じる",Muted,.6f);
    }
}
