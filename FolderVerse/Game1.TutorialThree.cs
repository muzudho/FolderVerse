namespace FolderVerse;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void BeginTutorialStepThree()
    {
        _tutorialAttackAchieved=false;_tutorialAttackGuide=false;_tutorialAttackPending=false;_tutorialAchievementAge=-1;_tutorialIntroShown=false;_tutorialMessagesOpen=false;
        BeginPlayerCount();_autoPlay=_demoActive=false;_statusMenuOpen=false;
        _setup.PrepareTutorialStepThree(_random.Next(),_random.Next(),_random.Next());
        _setup.SelectPlayer(0);_setup.HumanPlayers.Add(0);_setup.SelectTurnRuler(0);
        _world.ShowSetup(_setup,true);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;RestoreDomesticCamera();_statusGlobe=true;
    }
    private bool UpdateTutorialThreeGuide(double now)
    {
        if(_setup.TutorialStep!=3)return false;
        if(_tutorialAttackGuide || _tutorialMessagesOpen || _autoPlay || _demoActive || !IsStatusScreen || _conquestOverview || _homePersuasionActive ||
            MapTransitionActive || OrientationRotating || _net.IsAnimating || _lastCutInPhase!="内政フェーズ" || now-_cutInStarted<PhaseCutInSeconds)return true;
        int before=_setup.TutorialMapStage;
        if(before==0 && !_statusGlobe)_setup.TutorialMapStage=1;
        else if(before==1 && _setup.TutorialMapRotations==7)_setup.TutorialMapStage=2;
        else if(before==2 && _setup.TutorialMapEdgeChanged)_setup.TutorialMapStage=3;
        else if(before==3 && _statusGlobe)_setup.TutorialMapStage=4;
        int stage=_setup.TutorialMapStage;
        if(stage<4 && (_setup.TutorialMapNotices&(1<<stage))==0){
            _setup.TutorialMapNotices|=1<<stage;_tutorialGuideMessage=8+stage;_tutorialAttackGuide=true;_tutorialGuideStarted=_animationTime;
        }
        if(before!=stage){_lastAutoSaveTurn=-1;AutoSaveDomestic();}
        return true;
    }
    private void RecordTutorialMapRotation(int button)
    {if(_setup.TutorialStep==3 && _setup.TutorialMapStage==1 && !OrientationGlobe)_setup.TutorialMapRotations|=button;}
    private void RecordTutorialMapEdge()
    {if(_setup.TutorialStep==3 && _setup.TutorialMapStage==2)_setup.TutorialMapEdgeChanged=true;}
}
