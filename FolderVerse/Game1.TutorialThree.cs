namespace FolderVerse;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void BeginTutorialStepThree()
    {
        _tutorialAttackAchieved=false;_tutorialAttackGuide=false;_tutorialAttackPending=false;_tutorialAchievementAge=-1;_tutorialIntroShown=false;_tutorialMessagesOpen=false;_tutorialLastMessage=-1;
        BeginPlayerCount();_autoPlay=_demoActive=false;_statusMenuOpen=false;
        _setup.PrepareTutorialStepThree(_random.Next(),_random.Next(),_random.Next());
        _setup.SelectPlayer(0);_setup.HumanPlayers.Add(0);_setup.SelectTurnRuler(0);
        _world.ShowSetup(_setup,true);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;RestoreDomesticCamera();_statusGlobe=true;
    }
    private bool TutorialMapCourse=>_setup.TutorialStep==3 || _setup.TutorialMapBeforeWar;
    private bool UpdateTutorialThreeGuide(double now)
    {
        if(!TutorialMapCourse || _setup.TutorialMapBeforeWar && _setup.TutorialMapStage>=6)return false;
        if(_tutorialAttackGuide || _tutorialMessagesOpen || _autoPlay || _demoActive || !IsStatusScreen || _conquestOverview || _homePersuasionActive ||
            MapTransitionActive || OrientationRotating || _orientationDragging || _net.IsAnimating || _lastCutInPhase!="内政フェーズ" || now-_cutInStarted<PhaseCutInSeconds)return true;
        int before=_setup.TutorialMapStage;
        if(before==0 && !_statusGlobe)_setup.TutorialMapStage=1;
        else if(before==1 && (_setup.TutorialMapRotations&7)==7)_setup.TutorialMapStage=2;
        else if(before==2 && (_setup.TutorialMapRotations&8)!=0 && _setup.TutorialMapEdgeChanged)_setup.TutorialMapStage=3;
        else if(before==3 && _statusGlobe)_setup.TutorialMapStage=4;
        else if(before==4 && _statusGlobe && (_setup.TutorialMapRotations&TutorialGlobeRotationMask)==TutorialGlobeRotationMask)_setup.TutorialMapStage=5;
        else if(before==5 && _statusGlobe && (_setup.TutorialMapRotations&TutorialGlobeSlide)!=0){_setup.TutorialMapStage=6;if(!_setup.TutorialMapBeforeWar)_setup.TutorialCompletionShown=true;}
        int stage=_setup.TutorialMapStage;
        bool slide=stage==2 && (_setup.TutorialMapRotations&8)==0;
        int notice=TutorialThreeNotice(stage,_setup.TutorialMapRotations);
        if(stage<=6 && (_setup.TutorialMapNotices&notice)==0){
            _setup.TutorialMapNotices|=notice;ShowTutorialGuide(_setup.TutorialMapBeforeWar && stage==6?16:slide?12:stage>=4?stage+9:8+stage);
            if(_setup.TutorialMapBeforeWar && stage==6)_tutorialIntroShown=true;
        }
        if(before!=stage){_lastAutoSaveTurn=-1;AutoSaveDomestic();}
        return true;
    }
    // Keep the original net bits (0..3); globe buttons use bits 4..10 and slide uses bit 11.
    private const int TutorialGlobeRotationMask=127<<4,TutorialGlobeSlide=1<<11;
    private static int TutorialThreeNotice(int stage,int rotations)=>stage==2 && (rotations&8)==0?16:1<<(stage>=4?stage+1:stage);
    private void RecordTutorialGlobeRotation(int button)
    {if(TutorialMapCourse && _setup.TutorialMapStage==4 && OrientationGlobe && !_tutorialAttackGuide)_setup.TutorialMapRotations|=button;}
    private void RecordTutorialMapRotation(int button)
    {if(TutorialMapCourse && _setup.TutorialMapStage==1 && !OrientationGlobe)_setup.TutorialMapRotations|=button;}
    private void RecordTutorialMapEdge()
    {if(TutorialMapCourse && _setup.TutorialMapStage==2 && (_setup.TutorialMapRotations&8)!=0)_setup.TutorialMapEdgeChanged=true;}
}
