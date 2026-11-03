namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private double _tutorialCaptiveToastSeconds;
    private string _tutorialCaptiveToast="";
    private bool TutorialCaptiveAchievementsAvailable=>_setup.TutorialStep>0 && !_demoActive && !_autoPlay;
    private static readonly string[] TutorialCaptiveAchievementTitles={
        "捕まえた征服者の女の子を手下にした",
        "捕まえた征服者の女の子を見逃した",
        "捕まえた征服者の女の子（の持っている玩具）から電池を抜いた"
    };
    private static readonly string[][] TutorialCaptiveChoiceLines={
        new[]{"手下にすると、","相手の征服地点は全部","自分のものになりまーす。"},
        new[]{"見逃すと、相手はまだ","＜ゲーム＞を続けます。"},
        new[]{"持っている玩具の電池を抜くと、","相手は＜ゲーム＞から脱落しますが、","その征服地点は未所属になります。"}
    };
    private bool TutorialCaptiveChoiceNeedsExplanation(DefeatedChoice choice)=>TutorialCaptiveAchievementsAvailable && _tutorialAchievements?.Has(TutorialAchievements.CaptiveChoiceId(choice))!=true;
    private void AwardTutorialCaptiveChoice(DefeatedChoice choice)
    {
        if(!TutorialCaptiveAchievementsAvailable)return;
        try{
            if(_tutorialAchievements==null)LoadTutorialAchievements();
            if(_tutorialAchievements?.Award(TutorialAchievements.CaptiveChoiceId(choice))!=true)return;
            _tutorialCaptiveToast=TutorialCaptiveAchievementTitles[(int)choice];_tutorialCaptiveToastSeconds=3.5;
        }catch(Exception e){_tutorialAchievementError="実績の保存に失敗しました";RecordFailure(e,"tutorial_captive_achievement_write");}
    }
    private void DrawTutorialCaptiveChoiceNotes()
    {
        for(int i=0;i<3;i++){
            var button=DispositionButton(i);
            if(!TutorialCaptiveChoiceNeedsExplanation((DefeatedChoice)i))continue;
            var box=new Rectangle(button.X,946,button.Width,110);_ui.Box(box,new Color(27,49,62));
            var lines=TutorialCaptiveChoiceLines[i];
            for(int line=0;line<lines.Length;line++)_ui.Text(lines[line],new(button.X+12,956+line*29),Math.Min(.38f,(button.Width-24)/_font.MeasureString(lines[line]).X),Cream);
        }
    }
    private void DrawTutorialCaptiveAchievementToast()
    {
        float alpha=Math.Clamp((float)(_tutorialCaptiveToastSeconds/.35),0,1);
        var r=new Rectangle(250,85,1420,60);_ui.Box(r,new Color(255,214,112)*alpha);
        string text="実績解除："+_tutorialCaptiveToast;
        _ui.Center(text,r,Math.Min(.5f,(r.Width-32)/_font.MeasureString(text).X),new Color(35,38,46)*alpha);
    }
}
