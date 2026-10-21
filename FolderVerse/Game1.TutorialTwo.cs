namespace FolderVerse;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void BeginTutorialStepTwo()
    {
        _tutorialAttackAchieved=false;_tutorialAttackGuide=false;_tutorialAttackPending=false;_tutorialAchievementAge=-1;_tutorialIntroShown=false;_tutorialMessagesOpen=false;
        BeginPlayerCount();_autoPlay=_demoActive=false;_statusMenuOpen=false;
        _setup.PrepareTutorialStepTwo(_random.Next(),_random.Next(),_random.Next());
        _setup.SelectPlayer(0);_setup.HumanPlayers.Add(0);_setup.SelectTurnRuler(0);
        _world.ShowSetup(_setup,true);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;RestoreDomesticCamera();
    }
    private bool UpdateTutorialTwoGuide(double now)
    {
        if(_setup.TutorialStep!=2)return false;
        if(_tutorialAttackGuide || _tutorialMessagesOpen || _autoPlay || _demoActive || !IsStatusScreen || _conquestOverview || _lastCutInPhase!="内政フェーズ" || now-_cutInStarted<PhaseCutInSeconds)return true;
        int message=-1;
        if(!_tutorialIntroShown){_tutorialIntroShown=true;message=4;}
        else if(_setup.TutorialPocketed && !_setup.TutorialPocketNoticeShown){_setup.TutorialPocketNoticeShown=true;message=5;}
        else if(_setup.TutorialMoved && !_setup.TutorialMoveNoticeShown){_setup.TutorialMoveNoticeShown=true;message=6;}
        else if(_setup.TutorialPocketNoticeShown && _setup.TutorialMoveNoticeShown && !_setup.TutorialCompletionShown){_setup.TutorialCompletionShown=true;message=7;}
        if(message>=0){_tutorialGuideMessage=message;_tutorialAttackGuide=true;_tutorialGuideStarted=_animationTime;}
        return true;
    }
}
