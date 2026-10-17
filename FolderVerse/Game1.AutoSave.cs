namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private sealed record SavedGame(WorldSetup World,Dictionary<int,DomesticViewState> Views);
    // Test hosts opt in with a temporary path; they never overwrite the real player's slot.
    private bool _autoSaveEnabled=Assembly.GetEntryAssembly()==typeof(Game1).Assembly;
    private string _autoSavePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FolderVerse","autosave.json.gz");
    private int _lastAutoSaveTurn=-1;
    private bool _hasAutoSave;
    private string _autoSaveError="";
    private static Rectangle NewGameWithSaveButton=>new(740,1000,440,60);
    private void CheckAutoSave(){_hasAutoSave=_autoSaveEnabled && File.Exists(_autoSavePath);}
    private void AutoSaveDomestic(){
        if(!_autoSaveEnabled || _demoActive || !IsStatusScreen || _setup.PlayerSlot<0 || _lastAutoSaveTurn==_setup.Population.Turn)return;
        try{
            SaveDomesticCamera();
            AutoSaveStore.Write(_autoSavePath,new SavedGame(_setup,new(_domesticViews)));
            _lastAutoSaveTurn=_setup.Population.Turn;_hasAutoSave=true;_autoSaveError="";
            _operations.Write("autosave_written",new{turn=_lastAutoSaveTurn,path=_autoSavePath});
        }catch(Exception error){_autoSaveError="オートセーブに失敗しました";RecordFailure(error,"autosave_write");}
    }
    private bool ResumeAutoSave(){
        if(!_hasAutoSave)return false;
        try{
            var saved=AutoSaveStore.Read<SavedGame>(_autoSavePath);var next=saved.World;
            if(next.PlayerSlot<0 || next.PlayerSlot>=next.ActiveCount || next.Robots.Nodes.Length!=next.Nodes.All.Length || next.LandPending || next.Industry.Pending)
                throw new InvalidDataException("Autosave is not at a domestic turn boundary.");
            next.Robots.Check();
            foreach(var battle in next.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles).Concat(next.Campaign.RobotBattles.Scenes).Distinct()){
                battle.Allied=next.Relations.Allied;battle.Check();
            }
            ResetInspector();_setup=next;_domesticViews.Clear();foreach(var view in saved.Views)_domesticViews.Add(view.Key,view.Value);
            _autoPlay=_demoActive=false;_statusMenuOpen=false;_robotPlaying=false;_robotPendingFocus=-1;_robotPendingSummary=false;
            _battleAge=0;_notifiedTurn=(-1,-1);_screen=Screen.WorldStatus;
            _world.ShowSetup(_setup,true);_lastAutoSaveTurn=_setup.Population.Turn;RestoreDomesticCamera();OpenDisposition();
            _autoSaveError="";_operations.Write("autosave_resumed",new{turn=_lastAutoSaveTurn});return true;
        }catch(Exception error){_screen=Screen.Title;_autoSaveError="再開できません。新しく始めるを選べます。";RecordFailure(error,"autosave_read");return false;}
    }
    private void DeleteCompletedAutoSave(){
        if(!_autoSaveEnabled || _demoActive)return;
        try{File.Delete(_autoSavePath);_hasAutoSave=false;_lastAutoSaveTurn=-1;_operations.Write("autosave_deleted",new{reason="ending_completed"});}
        catch(Exception error){RecordFailure(error,"autosave_delete");}
    }
    private void DrawResumeTitle(){
        if(!_hasAutoSave)return;
        _ui.Button(StartButton,"再開",DefaultAccent,.9f);
        _ui.Button(NewGameWithSaveButton,"新しく始める",Muted,.55f);
        if(_autoSaveError.Length>0)_ui.Text(_autoSaveError,new(580,820),.45f,Cream);
    }
}
