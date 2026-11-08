namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private sealed record SavedGame(WorldSetup World,Dictionary<int,DomesticViewState> Views);
    // Test hosts opt in with a temporary path; they never overwrite the real player's slot.
    private bool _autoSaveEnabled=Assembly.GetEntryAssembly()==typeof(Game1).Assembly;
    private string _autoSavePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FolderVerse","autosave.json.gz");
    private int _lastAutoSaveTurn=-1;
    private bool _hasAutoSave;
    private string _autoSaveError="";
    private const double AutoSaveNoticeDuration=2,AutoSaveNoticeSlide=.25;
    private double _autoSaveNoticeSeconds;
    private void CheckAutoSave(){_hasAutoSave=_autoSaveEnabled && File.Exists(_autoSavePath);}
    private void AutoSaveDomestic(){
        if(!_autoSaveEnabled || _demoActive || !IsStatusScreen || DeliveryPlayback || _setup.PlayerSlot<0 || _lastAutoSaveTurn==_setup.Population.Turn)return;
        try{
            SaveDomesticCamera();
            AutoSaveStore.Write(_autoSavePath,new SavedGame(_setup,new(_domesticViews)));
            _lastAutoSaveTurn=_setup.Population.Turn;_hasAutoSave=true;_autoSaveError="";
            _autoSaveNoticeSeconds=AutoSaveNoticeDuration;
            _operations.Write("autosave_written",new{turn=_lastAutoSaveTurn,path=_autoSavePath});
        }catch(Exception error){_autoSaveError="オートセーブに失敗しました";RecordFailure(error,"autosave_write");}
    }
    private void DrawAutoSaveNotice(Rectangle canvas,Matrix transform)
    {
        if(_autoSaveNoticeSeconds<=0 || _screen==Screen.Title)return;
        float slide=(float)Math.Clamp(Math.Min(AutoSaveNoticeDuration-_autoSaveNoticeSeconds,_autoSaveNoticeSeconds)/AutoSaveNoticeSlide,0,1);
        int height=32;
        var row=new Rectangle(820,1080-(int)MathF.Round(height*MathHelper.SmoothStep(0,1,slide)),280,height);
        var previousClip=GraphicsDevice.ScissorRectangle;
        using var clip=new RasterizerState{ScissorTestEnable=true};
        GraphicsDevice.ScissorRectangle=canvas;
        _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
        _ui.Box(row,new Color(8,24,34,235));
        _ui.Center("セーブしました",row,.38f,Cream);
        _spriteBatch.End();
        GraphicsDevice.ScissorRectangle=previousClip;
    }
    private bool ResumeAutoSave(){
        if(!_hasAutoSave)return false;
        try{
            var saved=AutoSaveStore.Read<SavedGame>(_autoSavePath);var next=saved.World;
            if(next.PlayerSlot<0 || next.PlayerSlot>=next.ActiveCount || next.Robots.Nodes.Length!=next.Nodes.All.Length || next.LandPending || next.Industry.Pending || next.Campaign.Phase==CampaignPhase.Delivery)
                throw new InvalidDataException("Autosave is not in a planning phase.");
            next.Robots.Check();
            next.Robots.DirectDelivery.ReconcileDestinations(next);
            if(next.Salvage.LegacyPlacementPending)next.Salvage.Initialize(next);
            foreach(var battle in next.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles).Concat(next.Campaign.RobotBattles.Scenes).Distinct()){
                battle.Allied=next.Relations.Allied;battle.Check();
            }
            next.Robots.ConfigureFactories(next);ResetInspector();_setup=next;_tutorialAttackGuide=false;_tutorialLastMessage=-1;_tutorialAttackAchieved=next.TutorialMapBeforeWar?next.TutorialAttackCompleted:next.TutorialStep==1 && _tutorialAchievements?.Has(TutorialAchievements.FirstAttack)==true;_domesticViews.Clear();foreach(var view in saved.Views)_domesticViews.Add(view.Key,view.Value);
            if(_tutorialAttackAchieved && !next.TutorialMapBeforeWar && !next.TutorialCompletionShown)next.TutorialCompletionPending=true;
            if((next.TutorialStep==3 || next.TutorialMapBeforeWar && next.TutorialMapStage<6) && next.TutorialMapStage<=6)next.TutorialMapNotices&=~TutorialThreeNotice(next.TutorialMapStage,next.TutorialMapRotations);
            if(next.TutorialMapBeforeWar && next.TutorialMapStage>=6){_tutorialIntroShown=true;_tutorialLastMessage=!next.TutorialAttackCompleted && next.TutorialTurnEndNoticeShown?21:!next.TutorialAttackCompleted && next.TutorialSortieNoticeShown?20:!next.TutorialAttackCompleted && next.TutorialWarTargetNoticeShown?22:next.TutorialPostMoveStage>0?16+Math.Min(next.TutorialPostMoveStage,3):next.TutorialMoveNoticeShown?6:next.TutorialPocketNoticeShown?5:next.TutorialPocketIntroShown?4:16;}
            _autoPlay=_demoActive=false;_statusMenuOpen=false;_robotPlaying=false;_robotPendingFocus=-1;_robotPendingSummary=false;
            _battleAge=0;_notifiedTurn=(-1,-1);_screen=Screen.WorldStatus;RecordPetitstellaMedals();
            _world.ShowSetup(_setup,true);_lastAutoSaveTurn=_setup.Population.Turn;RestoreDomesticCamera();OpenDisposition();if(_setup.ComputerOnly)StartAutoPlay();
            _autoSaveError="";_operations.Write("autosave_resumed",new{turn=_lastAutoSaveTurn});return true;
        }catch(Exception error){_screen=Screen.Title;_autoSaveError="続きからを開始できません。最初からを選べます。";RecordFailure(error,"autosave_read");return false;}
    }
    private void DeleteCompletedAutoSave(){
        if(!_autoSaveEnabled || _demoActive)return;
        try{File.Delete(_autoSavePath);_hasAutoSave=false;_lastAutoSaveTurn=-1;_operations.Write("autosave_deleted",new{reason="ending_completed"});}
        catch(Exception error){RecordFailure(error,"autosave_delete");}
    }
}
