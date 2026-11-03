namespace FolderVerse;
public partial class Game1
{
    private bool UpdateTutorialWarGuide(double now)
    {
        if(!_setup.TutorialMapBeforeWar || _setup.TutorialMapStage<6 || _setup.TutorialAttackCompleted)return false;
        if(_tutorialAttackGuide || _tutorialMessagesOpen || _tutorialGuideClosing>=0 || _autoPlay || _demoActive || !IsStatusScreen || _conquestOverview || _homePersuasionActive ||
            _invasionPicker>=0 || _lastCutInPhase!="内政フェーズ" || now-_cutInStarted<PhaseCutInSeconds)return true;
        int message=-1;
        if(_movementOpen && _warPlanning && !_nodeInspectorClosing && !_setup.TutorialWarTargetNoticeShown){
            _setup.TutorialWarTargetNoticeShown=true;message=22;
        }else if(_movementOpen && _warPlanning && !_nodeInspectorClosing && _selectedMoveNode>=0 && !_setup.TutorialSortieNoticeShown){
            _setup.TutorialSortieNoticeShown=true;message=20;
        }else if(!InspectorVisible && _domesticSlide==0 && TutorialHasAttackReservation && !_setup.TutorialTurnEndNoticeShown){
            _setup.TutorialTurnEndNoticeShown=true;message=21;
        }
        if(message>=0){ShowTutorialGuide(message);_lastAutoSaveTurn=-1;AutoSaveDomestic();return true;}
        return false;
    }
}
