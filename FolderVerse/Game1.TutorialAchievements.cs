namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private TutorialAchievements _tutorialAchievements;
    private bool _tutorialAttackAchieved;
    private bool _tutorialAttackPending;
    private double _tutorialAchievementAge=-1;
    private const double TutorialAchievementSeconds=3.5;
    private void QueueTutorialAttackAchievement(){
        _tutorialAchievementAge=-1;
        _tutorialAttackPending=TutorialAttackRequired && TutorialAchievements.Attacked(_setup);
    }
    private void RevealTutorialAttackAchievement(){
        if(!_tutorialAttackPending || !TutorialAchievements.AttackedScene(_setup,_robotScenes[_battleWave]))return;
        _tutorialAttackPending=false;CheckTutorialAttackAchievement();_tutorialAchievementAge=0;
    }
    private void UpdateTutorialAchievementCutIn(double elapsed){
        _tutorialAchievementAge+=Math.Max(0,elapsed);
        if(_tutorialAchievementAge>=TutorialAchievementSeconds)_tutorialAchievementAge=-1;
    }
    private void DrawTutorialAchievementCutIn(){
        float age=(float)_tutorialAchievementAge;
        float slide=1-MathHelper.SmoothStep(0,1,Math.Clamp(age/.35f,0,1));
        float alpha=Math.Clamp((float)(TutorialAchievementSeconds-age)/.4f,0,1);
        if(_tutorialHost!=null){float scale=Math.Min(520f/_tutorialHost.Width,740f/_tutorialHost.Height);_spriteBatch.Draw(_tutorialHost,new Rectangle(85-(int)(slide*500),300,(int)(_tutorialHost.Width*scale),(int)(_tutorialHost.Height*scale)),Color.White*alpha);}
        _ui.Box(new(640,395,1190,215),Cream*alpha);_ui.Tile(new(640,490),new(40,40),MathHelper.PiOver4,Cream*alpha);
        _ui.Center("［開戦］を実績解除しました！",new(690,435,1090,120),.85f,Muted*alpha);
    }
    private bool _tutorialAttackGuide;
    private float _tutorialGuideStarted;
    private string _tutorialAchievementError="";
    private void LoadTutorialAchievements(){
        if(!_autoSaveEnabled)return;
        try{_tutorialAchievements=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FolderVerse","achievements.json"));}
        catch(Exception e){_tutorialAchievementError="勲章の記録を読み込めませんでした";RecordFailure(e,"achievement_read");}
    }
    private void CheckTutorialAttackAchievement(){
        if(!TutorialAchievements.Attacked(_setup))return;
        _tutorialAttackAchieved=true;
        if(_setup.TutorialStep==1 && !_setup.TutorialCompletionShown)_setup.TutorialCompletionPending=true;
        if(!_autoSaveEnabled)return;
        try{if(_tutorialAchievements==null)LoadTutorialAchievements();_tutorialAchievements?.Award(TutorialAchievements.FirstAttack);}
        catch(Exception e){_tutorialAchievementError="勲章の保存に失敗しました";RecordFailure(e,"achievement_write");}
    }
    private bool TutorialAttackRequired=>_setup.TutorialStep==1 && !_tutorialAttackAchieved && _tutorialAchievements?.Has(TutorialAchievements.FirstAttack)!=true;
    private bool TutorialHasAttackReservation=>_setup.Campaign.PlannedOrders.Any(o=>o.Ruler==0 && (o.Queen || o.Fighters>0) &&
        _setup.Nodes.All[o.TargetNode].Owner>=0 && !_setup.Relations.Allied(0,_setup.Nodes.All[o.TargetNode].Owner));
    private bool ShowTutorialAttackGuideIfRequired(){
        if(!TutorialAttackRequired || TutorialHasAttackReservation)return false;
        _tutorialGuideMessage=1;_tutorialAttackGuide=true;_tutorialGuideStarted=_animationTime;return true;
    }
    private void DrawTutorialAttackGuide(){
        float slide=1-MathHelper.SmoothStep(0,1,Math.Clamp((_animationTime-_tutorialGuideStarted)/.35f,0,1));
        _ui.Box(new(0,0,1920,1080),Color.Black*.4f);
        if(_tutorialHost!=null){float scale=Math.Min(520f/_tutorialHost.Width,740f/_tutorialHost.Height);_spriteBatch.Draw(_tutorialHost,new Rectangle(85-(int)(slide*500),300,(int)(_tutorialHost.Width*scale),(int)(_tutorialHost.Height*scale)),Color.White);}
        _ui.Box(new(640,345,1190,350),Cream);_ui.Tile(new(640,490),new(40,40),MathHelper.PiOver4,Cream);
        var lines=TutorialMessageLines[_tutorialGuideMessage];
        for(int i=0;i<lines.Length;i++)_ui.Text(lines[i],new(690,395+i*70),Math.Min(.55f,1080/_font.MeasureString(lines[i]).X),Muted);
        if(_tutorialGuideMessage==1)_ui.Text("通常のゲームでは、開戦しなくてもターン終了できます。",new(690,610),.4f,Muted);
        _ui.Button(new(1360,730,400,60),"わかった",Accent,.6f);
    }
    private void DrawTutorialMedal(Vector2 center){
        _ui.Tile(center+new Vector2(-7,19),new(12,26),.2f,new Color(185,45,66));
        _ui.Tile(center+new Vector2(7,19),new(12,26),-.2f,new Color(185,45,66));
        for(int y=-19;y<=19;y++){int half=(int)MathF.Sqrt(19*19-y*y);_ui.Box(new((int)center.X-half,(int)center.Y+y,half*2+1,1),new Color(236,181,65));}
        _ui.Tile(center,new(21,21),MathHelper.PiOver4,new Color(255,235,151));
    }
    private void DrawTutorialGoal(){
        if(_setup.TutorialStep!=1 || (!IsStatusScreen && _screen!=Screen.Transport))return;
        string text=_tutorialAttackAchieved?"勲章獲得！ ステップ１：開戦":"ステップ１：開戦しましょう";
        _ui.Box(new(700,20,400,48),new Color(14,26,39,235));
        _ui.Center(text,new(710,20,330,48),.3f,_tutorialAttackAchieved?new Color(255,220,115):Cream);
        if(_tutorialAttackAchieved)DrawTutorialMedal(new(1070,40));
    }
}
