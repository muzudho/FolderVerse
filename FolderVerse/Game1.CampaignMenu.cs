namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _campaignMenuStage,_campaignCharacterTarget;
    private readonly int[] _campaignCharacters={0,1};
    private static readonly Rectangle CampaignDuelButton=new(720,540,1100,90),CampaignStartButton=new(50,920,330,70),CampaignBackButton=new(50,1010,330,52);
    private static Rectangle CampaignCharacterCard(int character)=>new(440+character%10*144,175+character/10*136,134,126);
    private static Rectangle CampaignPlayerTab(int player)=>new(50,175+player*345,330,52);
    private static Rectangle CampaignRandomButton(int player)=>new(50,445+player*345,330,48);
    private void OpenCampaignMenu(){_campaignMenuStage=1;_campaignCharacterTarget=0;_titleIdle=0;_campaignCharacters[0]=_random.Next(60);_campaignCharacters[1]=_random.Next(60);}
    private void CampaignMenuInput(bool click,bool enter,bool escape,KeyboardState keys)
    {
        if(escape || click && CampaignBackButton.Contains(_pointer)){_campaignMenuStage--;return;}
        if(_campaignMenuStage==1){if(enter || click && CampaignDuelButton.Contains(_pointer))_campaignMenuStage=2;return;}
        if(click){
            for(int player=0;player<2;player++){
                if(CampaignPlayerTab(player).Contains(_pointer)){_campaignCharacterTarget=player;return;}
                if(CampaignRandomButton(player).Contains(_pointer)){_campaignCharacters[player]=_random.Next(60);_campaignCharacterTarget=player;return;}
            }
            for(int i=0;i<60;i++)if(CampaignCharacterCard(i).Contains(_pointer)){_campaignCharacters[_campaignCharacterTarget]=i;return;}
        }
        if(enter || click && CampaignStartButton.Contains(_pointer))BeginCampaignDuel();
    }
    private void BeginCampaignDuel()
    {
        int human=_campaignCharacters[0],computer=_campaignCharacters[1];
        BeginPlayerCount();_autoPlay=_demoActive=false;_statusMenuOpen=false;
        _setup.PrepareCampaignDuel(_random.Next(),_random.Next(),_random.Next(),_random.Next(),human,computer);
        _world.ShowSetup(_setup,true);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;RestoreDomesticCamera();
    }
    private void DrawCampaignMenu()
    {
        _ui.Box(new(0,0,1920,1080),new Color(22,34,48));
        _ui.Text(_campaignMenuStage==1?"キャンペーンモード":"キャンペーン#1 地賽でタイマン",new(54,35),.85f,Cream);
        if(_campaignMenuStage==1){
            if(_setupHost!=null)_spriteBatch.Draw(_setupHost,new Rectangle(100,200,500,750),Color.White);
            _ui.Box(new(680,170,1170,270),Cream);_ui.Tile(new(680,310),new(45,45),MathHelper.PiOver4,Cream);
            _ui.Text("キャンペーンモードは、予め用意された世界で",new(730,210),.58f,Muted);
            _ui.Text("戦争をするモードです。",new(730,280),.58f,Muted);
            _ui.Text("それでは、以下のメニューから選んでください。",new(730,350),.58f,Muted);
            _ui.Button(CampaignDuelButton,"キャンペーン#1 地賽でタイマン",Accent,.65f);
            _ui.Text("あなた 対 コンピューター / １対１",new(750,675),.65f,Cream);
            _ui.Text("１×１×１の地星 / 連続する３ sectors 対３ sectors",new(750,745),.52f,Cream);
            _ui.Text("地形・地点の性能はランダム / 両者の征服者を選べます",new(750,815),.48f,Cream);
        }else{
            _ui.Text("左で担当を選び、右の60人から選択 / 同じキャラクターも選べます",new(54,115),.49f,Cream);
            for(int player=0;player<2;player++){
                _ui.Button(CampaignPlayerTab(player),player==0?"あなたを選ぶ":"コンピューターを選ぶ",_campaignCharacterTarget==player?Accent:Muted,.48f);
                var preview=new Rectangle(125,240+player*345,180,180);
                _portraitRenderer.Draw(_spriteBatch,new(_campaignCharacters[player],0),preview);
                _ui.Center($"No.{_campaignCharacters[player]+1:00}",new(50,420+player*345,330,24),.36f,Cream);
                _ui.Button(CampaignRandomButton(player),"ランダム",Muted,.50f);
            }
            for(int i=0;i<60;i++){
                var r=CampaignCharacterCard(i);bool human=_campaignCharacters[0]==i,computer=_campaignCharacters[1]==i;
                _ui.Box(r,human || computer?Accent:new Color(44,64,80));
                _portraitRenderer.Draw(_spriteBatch,new(i,0),new(r.X+4,r.Y+4,r.Width-8,96));
                _ui.Center($"{i+1:00}"+(human?" あなた":"")+(computer?" CPU":""),new(r.X+2,r.Bottom-24,r.Width-4,22),.27f,Cream);
            }
            _ui.Button(CampaignStartButton,"この２人で始める",Accent,.56f);
        }
        _ui.Button(CampaignBackButton,"戻る",Muted,.6f);
    }
}
