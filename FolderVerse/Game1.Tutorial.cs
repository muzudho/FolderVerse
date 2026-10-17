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
    private static Rectangle TutorialStep(int i)=>new(760,520+i*85,960,65);
    private void LoadTutorialHost(){
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Setup","tutorial-library.png"));
        _tutorialHost=Texture2D.FromStream(GraphicsDevice,stream);
        var pixels=new Color[_tutorialHost.Width*_tutorialHost.Height];_tutorialHost.GetData(pixels);
        RemoveMobBackdrop(pixels,_tutorialHost.Width,_tutorialHost.Height,new(-1,-1));_tutorialHost.SetData(pixels);
    }
    private void TutorialMenuInput(bool click,bool enter,bool escape,KeyboardState keys){
        if(escape || click && BackButton.Contains(_pointer)){_tutorialMenuOpen=false;return;}
        if(enter || click && TutorialStep(0).Contains(_pointer))BeginTutorialStepOne();
    }
    private void BeginTutorialStepOne(){
        BeginPlayerCount();_autoPlay=_demoActive=false;_statusMenuOpen=false;
        _setup.PrepareTutorialStepOne(_random.Next(),_random.Next(),_random.Next());
        _setup.CompleteLand(_random.Next());_setup.SelectPlayer(0);_setup.HumanPlayers.Add(0);_setup.SelectTurnRuler(0);
        _world.ShowSetup(_setup,true);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;RestoreDomesticCamera();
    }
    private void DrawTutorialMenu(){
        _ui.Box(new(0,0,1920,1080),new Color(22,34,48)*.94f);
        _ui.Text("遊び方",new(70,55),1f,Cream);
        if(_tutorialHost!=null){
            float scale=Math.Min(560f/_tutorialHost.Width,790f/_tutorialHost.Height);
            _spriteBatch.Draw(_tutorialHost,new Rectangle(90,190,(int)(_tutorialHost.Width*scale),(int)(_tutorialHost.Height*scale)),Color.White);
        }
        _ui.Box(new(680,170,1170,270),Cream);_ui.Tile(new(680,310),new(45,45),MathHelper.PiOver4,Cream);
        _ui.Text("このゲームの遊び方をチュートリアル形式で説明します。",new(730,205),.55f,Muted);
        _ui.Text("ステップ１から順番に進めてください。",new(730,280),.55f,Muted);
        _ui.Text("１つのステップの推定プレイ時間は２０分です。",new(730,355),.55f,Muted);
        for(int i=0;i<5;i++){
            if(i==0)_ui.Button(TutorialStep(i),"ステップ１",Accent,.65f);
            else _ui.Center($"ステップ{i+1}　（準備中）",TutorialStep(i),.6f,new Color(140,145,150));
        }
        _ui.Button(BackButton,"戻る",Muted,.6f);
    }
}
