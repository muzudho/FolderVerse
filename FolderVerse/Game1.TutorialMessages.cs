namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private bool _tutorialIntroShown,_tutorialMessagesOpen;
    private int _tutorialMessageSelection,_tutorialGuideMessage=1;
    private static readonly string[] TutorialMessageTitles={"内政開始の案内","開戦予約の案内","開戦の実績解除","ステップ１の完了","ステップ２の案内","ポケットの達成","移動の達成","ステップ２の完了","地星儀を展開図へ","展開図の回転","面のつながり","地星儀へ組立","展開図のスライド"};
    private static readonly string[][] TutorialMessageLines={
        new[]{"さあ、戦争の始め方を覚えましょう！","［開戦予約］ボタンを押してください！"},
        new[]{"待った～！　ステップ１では、戦争の始め方を覚えてみましょう！","まず［開戦予約］ボタンを押して出撃を予約してから、","［ターン終了］ボタンを押してください！"},
        new[]{"［開戦］を実績解除しました！"},
        new[]{"遊び方　ステップ１　は完了です。","右上の＜システムメニュー＞ボタンから、","［ここでゲームをエンディングにする］メニューを選んでくださーい！"},
        new[]{"ロボットをポケットに入れて移動しよう！","［わたしの国］から［ポケット］と［移動］を選んでね。","どちらが先でも大丈夫。移動は予約してターン終了で実行です！"},
        new[]{"やった！　ロボットをポケットに入れることができましたね！"},
        new[]{"わーい！　初めての移動ができましたね！"},
        new[]{"遊び方　ステップ２　は完了です。","右上の＜システムメニュー＞ボタンから、","［ここでゲームをエンディングにする］メニューを選んでくださーい！"},
        new[]{"［地星儀］（ちせいぎ）を［展開図］にしてみましょう！","画面右上の［展開図］ボタンを押してください！"},
        new[]{"［展開図］は回転できます！","地図の右上のロボットが描かれている円いウィンドウの周囲にある","３つのボタンを押してください！"},
        new[]{"［展開図］の面のつながりを確認してみましょう！","［展開図］の端っこにあるアルファベットをクリックしてください！"},
        new[]{"［地星儀］に戻しましょう！","画面の右上の［組立］ボタンをクリックしてください！"},
        new[]{"展開図の右上のロボットをマウスでドラッグすると、","展開図をスライドできます。やってみましょう！"}
    };
    private bool TutorialReviewAvailable=>_setup.TutorialStep is 1 or 2 or 3 && _screen!=Screen.Title && _screen!=Screen.PlayerCount;
    private int TutorialMessageBase=>_setup.TutorialStep switch{2=>4,3=>8,_=>0};
    private Rectangle TutorialReviewButton=>_screen==Screen.Battle?new(55,1046,400,30):new(1110,20,380,48);
    private int[] TutorialReviewMessages=>_setup.TutorialStep==3?new[]{8,9,12,10,11}:new[]{TutorialMessageBase,TutorialMessageBase+1,TutorialMessageBase+2,TutorialMessageBase+3};
    private static Rectangle TutorialMessageRow(int i)=>new(400,315+i*100,360,75);
    private static Rectangle TutorialMessageClose=>new(1150,800,360,60);
    private void UpdateTutorialStartGuide(double now){
        if(UpdateTutorialThreeGuide(now))return;
        if(UpdateTutorialTwoGuide(now))return;
        if(_setup.TutorialStep==1 && _setup.TutorialCompletionPending && !_setup.TutorialCompletionShown && IsStatusScreen && !_conquestOverview && !_autoPlay && !_demoActive &&
            _lastCutInPhase=="内政フェーズ" && now-_cutInStarted>=PhaseCutInSeconds){
            _setup.TutorialCompletionPending=false;_setup.TutorialCompletionShown=true;
            _tutorialGuideMessage=3;_tutorialAttackGuide=true;_tutorialGuideStarted=_animationTime;return;
        }
        if(_tutorialIntroShown || !TutorialAttackRequired || !IsStatusScreen || _autoPlay || _demoActive ||
            _lastCutInPhase!="内政フェーズ" || now-_cutInStarted<PhaseCutInSeconds)return;
        _tutorialIntroShown=true;_tutorialGuideMessage=0;_tutorialAttackGuide=true;_tutorialGuideStarted=_animationTime;
    }
    private bool TutorialMessageInput(bool click,bool escape){
        if(_tutorialMessagesOpen){
            if(escape || click && TutorialMessageClose.Contains(_pointer)){_tutorialMessagesOpen=false;return true;}
            if(click)for(int i=0;i<TutorialReviewMessages.Length;i++)if(TutorialMessageRow(i).Contains(_pointer))_tutorialMessageSelection=TutorialReviewMessages[i];
            return true;
        }
        if(TutorialReviewAvailable && _tutorialAchievementAge<0 && click && TutorialReviewButton.Contains(_pointer)){
            _tutorialMessagesOpen=true;_tutorialMessageSelection=TutorialMessageBase;return true;
        }
        return false;
    }
    private void DrawTutorialMessages(){
        if(!TutorialReviewAvailable)return;
        _ui.Button(TutorialReviewButton,"チュートリアルを見返す",Muted,.3f);
        if(!_tutorialMessagesOpen)return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.65f);_ui.Box(new(350,170,1220,760),new Color(25,42,56));
        _ui.Text("チュートリアルを見返す",new(400,215),.8f,Cream);
        for(int i=0;i<TutorialReviewMessages.Length;i++){int index=TutorialReviewMessages[i];_ui.Button(TutorialMessageRow(i),TutorialMessageTitles[index],index==_tutorialMessageSelection?Accent:Muted,.48f);}
        _ui.Box(new(790,300,730,450),Cream);
        var lines=TutorialMessageLines[_tutorialMessageSelection];
        for(int i=0;i<lines.Length;i++)_ui.Text(lines[i],new(825,365+i*85),Math.Min(.46f,655/_font.MeasureString(lines[i]).X),Muted);
        _ui.Button(TutorialMessageClose,"閉じる",Muted,.6f);
    }
}
