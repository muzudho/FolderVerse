namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private bool _tutorialMenuOpen;
    private Texture2D _tutorialHost;
    private static Rectangle TutorialClearMedals=>new(1000,975,720,52);
    private void LoadTutorialHost(){
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Setup","tutorial-library.png"));
        _tutorialHost=Texture2D.FromStream(GraphicsDevice,stream);
        var pixels=new Color[_tutorialHost.Width*_tutorialHost.Height];_tutorialHost.GetData(pixels);
        RemoveMobBackdrop(pixels,_tutorialHost.Width,_tutorialHost.Height,new(-1,-1));_tutorialHost.SetData(pixels);LoadTutorialPlanets();
    }
    private void TutorialMenuInput(bool click,bool enter,bool escape,KeyboardState keys){
        if(escape || click && BackButton.Contains(_pointer)){_tutorialMenuOpen=false;return;}
        if(click && TutorialClearMedals.Contains(_pointer)){
            try{if(_tutorialAchievements==null)LoadTutorialAchievements();if(_tutorialAchievements==null)throw new InvalidDataException("Achievement store unavailable.");_tutorialAchievements.Clear();_tutorialAttackAchieved=false;_tutorialAchievementError="";}
            catch(Exception e){_tutorialAchievementError="勲章の記録を消去できませんでした";RecordFailure(e,"achievement_clear");}return;
        }
        if(click || _pointer!=CanvasPoint(_previousMouse))for(int i=0;i<6;i++)if(TutorialPlanetCard(i).Contains(_pointer))_tutorialPlanetSelection=i;
        int move=0;
        if(keys.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))move=-1;
        if(keys.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))move=1;
        if(keys.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))move=-3;
        if(keys.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))move=3;
        _tutorialPlanetSelection=(_tutorialPlanetSelection+move+6)%6;
        if(_tutorialPlanetSelection==0 && (enter || click && TutorialPlanetCard(0).Contains(_pointer)))BeginPetitstellaTutorial();
    }
    private void BeginPetitstellaTutorial(){
        BeginTutorialStepOne();_setup.TutorialMapBeforeWar=true;_statusGlobe=true;
        foreach(var node in _setup.Nodes.All)for(int i=0;i<2;i++)_setup.Robots.Nodes[node.Id].Add(_setup.Robots.Create(0,RobotParts.Complete,RobotRole.Soldier));
        _world.ShowSetup(_setup,true);
        _lastAutoSaveTurn=-1;AutoSaveDomestic();
    }
    private void BeginTutorialStepOne(){
        _tutorialAttackAchieved=false;_tutorialAttackGuide=false;_tutorialAttackPending=false;_tutorialAchievementAge=-1;_tutorialIntroShown=false;_tutorialMessagesOpen=false;_tutorialLastMessage=-1;
        BeginPlayerCount();_autoPlay=_demoActive=false;_statusMenuOpen=false;
        _setup.PrepareTutorialStepOne(_random.Next(),_random.Next(),_random.Next());
        _setup.CompleteLand(_random.Next());_setup.SelectPlayer(0);_setup.HumanPlayers.Add(0);_setup.SelectTurnRuler(0);
        _world.ShowSetup(_setup,true);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;RestoreDomesticCamera();
    }
    private void DrawTutorialMenu()=>DrawTutorialPlanetMenu();
}
