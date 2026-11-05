namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private bool _tutorialIntroShown,_tutorialMessagesOpen;
    private int _tutorialMessageSelection,_tutorialGuideMessage=1,_tutorialLastMessage=-1;
    private static readonly string[] TutorialMessageTitles={"内政開始の案内","開戦予約の案内","開戦の実績解除","ステップ１の完了","ステップ２の案内","ポケットの達成","移動の達成","ステップ２の完了","地星儀を展開図へ","展開図の回転","面のつながり","地星儀へ組立","展開図のスライド","地星儀の回転","地星儀のスライド","ステップ３の完了","開戦予約の解放","戦力の余裕と世界征服","中断と続きから","セーブ枠の案内","出撃ロボットの選択","予約後のターン終了","攻め込む地点の選択"};
    private static readonly string[][] TutorialMessageLines={
        new[]{"さあ、戦争の始め方を覚えましょう！","内政フェーズで［開戦予約］を押してください！","予約後に［内政終了・伝播へ］を押すと、配送、出兵の順に進みます！"},
        new[]{"まずは開戦予約をしてみましょう！","画面左下の［開戦予約］ボタンから、出撃するロボットを選んでくださーい！"},
        new[]{"［開戦］を実績解除しました！"},
        new[]{"遊び方　ステップ１　は完了です。","右上の＜システムメニュー＞ボタンから、","［ここでゲームをエンディングにする］メニューを選んでくださーい！"},
        new[]{"ロボットをポケットに入れて移動しよう！","画面左下の［わたしの国］ボタンから［ポケット］と［移動］を選んでね。","どちらが先でも大丈夫。移動は予約してターン終了で実行です！"},
        new[]{"やった！　ロボットをポケットに入れることができましたね！"},
        new[]{"わーい！　初めての移動ができましたね！"},
        new[]{"遊び方　ステップ２　は完了です。","右上の＜システムメニュー＞ボタンから、","［ここでゲームをエンディングにする］メニューを選んでくださーい！"},
        new[]{"それではチュートリアルを始めまーす！","［地星儀］（ちせいぎ）を［展開図］にしてみましょう！","画面右上の［展開図］ボタンを押してください！"},
        new[]{"［展開図］は回転できます！","地図の右上のロボットが描かれている円いウィンドウの周囲にある","３つのボタンを押してください！"},
        new[]{"［展開図］の面のつながりを確認してみましょう！","［展開図］の端っこにあるアルファベットをクリックしてください！"},
        new[]{"［地星儀］に戻しましょう！","画面の右上の［組立］ボタンをクリックしてください！"},
        new[]{"展開図の右上のロボットをマウスでドラッグすると、","展開図をスライドできます。やってみましょう！"},
        new[]{"地星儀（ちせいぎ）も回転できます！","右上の方向ロボットのウィンドウの周りにある７つのボタン、","＋－と 0,0,0 を押してください。"},
        new[]{"地星儀（ちせいぎ）もスライドできます！","右上の円いウィンドウの中にいるロボットを","マウスでドラッグしてください。"},
        new[]{"これで遊び方　ステップ３は終了です。","右上の＜システムメニュー＞ボタンから、","［ゲームを止めてタイトル画面に戻る］メニューをクリックしてください。"},
        new[]{"地図の操作を覚えることができましたね！","内政フェーズの［開戦予約］ボタンが使えるようになりました！","直送便の到着後を見込んで、出撃するロボットを予約できます！"},
        new[]{"戦力には余裕があります！　開戦だけで押しきっちゃってくださーい！","覚えた操作で、プチステラを世界征服しましょう！"},
        new[]{"休みたくなったら、右上の＜システムメニュー＞から、","［ゲームを止めてタイトル画面に戻る］を選んでね！","スタートメニューの［続きから］で再開できます！"},
        new[]{"内政フェーズの開始時に、自動でセーブされます！","セーブデータのスロットは１つだけです。","新しいゲームを進めると、前のセーブは上書きされます！"},
        new[]{"戦力は楽勝なほど余裕がありまーす！","深く考えず出撃できるロボットを全部クリックしてみましょう！","予約ができたら、開戦予約のウィンドウの［×］で閉じてください！"},
        new[]{"開戦予約ができましたね！","画面左下の大きな［軍事終了・ターン終了］ボタンを押して、開戦しましょう！"},
        new[]{"地星儀では、側面や裏側に攻め込める地点が隠れていることがあります。","回転してみましょう！","攻め込む地点をクリックして選んでください！"}
    };
    private void ShowTutorialGuide(int message){
        _tutorialGuideMessage=_tutorialLastMessage=message;_tutorialAttackGuide=true;_tutorialGuideStarted=_animationTime;
    }
    private bool TutorialReviewAvailable=>_setup.TutorialStep is 1 or 2 or 3 && _screen!=Screen.Title && _screen!=Screen.PlayerCount && _screen!=Screen.ComputerPlayerCount;
    private int TutorialMessageBase=>_setup.TutorialMapBeforeWar?8:_setup.TutorialStep switch{2=>4,3=>8,_=>0};
    private Rectangle TutorialReviewButton=>_screen==Screen.Battle?new(55,1046,400,30):new(1110,20,380,48);
    private int[] TutorialReviewMessages=>_setup.TutorialMapBeforeWar?new[]{8,9,12,10,11,13,14,16,1,22,20,21,2,4,5,6,17,18,19}:_setup.TutorialStep==3?new[]{8,9,12,10,11,13,14,15}:new[]{TutorialMessageBase,TutorialMessageBase+1,TutorialMessageBase+2,TutorialMessageBase+3};
    private Rectangle TutorialMessageRow(int i)=>new(400,315+i*(_setup.TutorialMapBeforeWar?Math.Min(40,560/TutorialReviewMessages.Length):_setup.TutorialStep==3?60:100),360,_setup.TutorialMapBeforeWar?Math.Min(34,560/TutorialReviewMessages.Length-6):_setup.TutorialStep==3?48:75);
    private static Rectangle TutorialMessageClose=>new(1150,800,360,60);
    private void UpdateTutorialStartGuide(double now){
        RecordPetitstellaMedals();
        if(UpdateTutorialThreeGuide(now))return;
        if(UpdateTutorialWarGuide(now))return;
        if(UpdateTutorialTwoGuide(now))return;
        if(_setup.TutorialStep==1 && _setup.TutorialCompletionPending && !_setup.TutorialCompletionShown && IsStatusScreen && !_conquestOverview && !_autoPlay && !_demoActive &&
            _lastCutInPhase=="内政フェーズ" && now-_cutInStarted>=PhaseCutInSeconds){
            _setup.TutorialCompletionPending=false;_setup.TutorialCompletionShown=true;
            ShowTutorialGuide(3);return;
        }
        if(_tutorialIntroShown || !TutorialAttackRequired || !IsStatusScreen || _autoPlay || _demoActive ||
            _lastCutInPhase!="内政フェーズ" || now-_cutInStarted<PhaseCutInSeconds)return;
        _tutorialIntroShown=true;ShowTutorialGuide(0);
    }
    private bool TutorialMessageInput(bool click,bool escape){
        if(_tutorialMessagesOpen){
            if(escape || click && TutorialMessageClose.Contains(_pointer)){_tutorialMessagesOpen=false;return true;}
            if(click)for(int i=0;i<TutorialReviewMessages.Length;i++)if(TutorialMessageRow(i).Contains(_pointer))_tutorialMessageSelection=TutorialReviewMessages[i];
            return true;
        }
        if(TutorialReviewAvailable && _tutorialAchievementAge<0 && click && TutorialReviewButton.Contains(_pointer)){
            _tutorialMessagesOpen=true;_tutorialMessageSelection=Array.IndexOf(TutorialReviewMessages,_tutorialLastMessage)>=0?_tutorialLastMessage:TutorialMessageBase;return true;
        }
        return false;
    }
    private void DrawTutorialReviewButton()=>DrawTutorialChrome(()=>{
        var r=TutorialReviewButton;
        bool hover=r.Contains(_pointer);
        _ui.Button(r,"",new Color(205,143,42));
        var top=new Color(255,230,151);var bottom=new Color(224,157,51);
        if(hover){top=Color.Lerp(top,Color.White,.2f);bottom=Color.Lerp(bottom,Color.White,.2f);}
        for(int y=2;y<r.Height-2;y++)_ui.Box(new(r.X+2,r.Y+y,r.Width-4,1),Color.Lerp(top,bottom,(y-2f)/(r.Height-5)));
        const string label="チュートリアルを見返す";
        float scale=Math.Min(.48f,(r.Width-24)/_ui.Measure(label,1).X);
        _ui.Center(label,r,scale,new Color(35,38,46));
    },TutorialReviewButton.Center.Y<540);
    private void DrawTutorialMessages(){
        if(!TutorialReviewAvailable)return;
        DrawTutorialReviewButton();
        if(!_tutorialMessagesOpen)return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.65f);_ui.Box(new(350,170,1220,760),new Color(25,42,56));
        _ui.Text("チュートリアルを見返す",new(400,215),.8f,Cream);
        for(int i=0;i<TutorialReviewMessages.Length;i++){
            int index=TutorialReviewMessages[i];var row=TutorialMessageRow(i);bool selected=index==_tutorialMessageSelection;
            if(selected){var border=row;border.Inflate(3,3);_ui.Box(border,new Color(255,214,112));}
            _ui.Button(row,TutorialMessageTitles[index],selected?Accent:Muted,_setup.TutorialMapBeforeWar?.4f:.48f);
        }
        _ui.Box(new(790,300,730,450),Cream);
        var lines=TutorialMessageLines[_tutorialMessageSelection];
        for(int i=0;i<lines.Length;i++)_ui.Text(lines[i],new(825,365+i*85),Math.Min(.46f,655/_font.MeasureString(lines[i]).X),Muted);
        _ui.Button(TutorialMessageClose,"閉じる",Muted,.6f);
    }
}
