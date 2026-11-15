namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _campaignMenuStage,_campaignCharacterTarget;
    private int[] _campaignCharacters={0,1};
    private int _campaignScenario=1,_campaignPlayerPage,_campaignCharacterPage;
    private int _campaignDivider=960;
    private bool _campaignDividerDragging;
    private int CampaignCharacterColumns=>Math.Max(4,(1880-_campaignDivider-30)/148);
    private int CampaignCharacterPageSize=>CampaignCharacterColumns*5;
    private int CampaignPlayerWidth=>(_campaignDivider-80)/2;
    private static readonly Rectangle CampaignDuelButton=new(720,540,1100,90),CampaignThreeKingdomsButton=new(720,680,1100,90),CampaignStartButton=new(50,920,330,70),CampaignBackButton=new(50,1010,330,52);
    private Rectangle CampaignCharacterCard(int character){int slot=character%CampaignCharacterPageSize;int width=(1880-_campaignDivider-30)/CampaignCharacterColumns;return new(_campaignDivider+30+slot%CampaignCharacterColumns*width,175+slot/CampaignCharacterColumns*140,width-10,130);}
    private Rectangle CampaignPlayerTab(int player)=>new(50+player%6%2*(CampaignPlayerWidth+15),175+player%6/2*235,CampaignPlayerWidth,46);
    private Rectangle CampaignRandomButton(int player){var tab=CampaignPlayerTab(player);int left=Math.Min(190,tab.Width/2);return new(tab.X+left,tab.Y+116,tab.Width-left-12,44);}
    private Rectangle CampaignPlayersPrevious=>new(400,930,70,48);
    private Rectangle CampaignPlayersNext=>new(_campaignDivider-100,930,70,48);
    private Rectangle CampaignCharactersPrevious=>new(_campaignDivider+30,930,70,48);
    private static readonly Rectangle CampaignCharactersNext=new(1800,930,70,48);
    private bool UpdateCampaignDivider(MouseState mouse,bool active,bool click){
        if(_screen!=Screen.Title || !_titleMenuOpen || _campaignMenuStage!=2 || !active){_campaignDividerDragging=false;return false;}
        if(click && new Rectangle(_campaignDivider-10,165,20,720).Contains(_pointer))_campaignDividerDragging=true;
        if(!_campaignDividerDragging)return false;
        if(mouse.LeftButton!=ButtonState.Pressed){_campaignDividerDragging=false;return true;}
        int first=_campaignCharacterPage*CampaignCharacterPageSize;_campaignDivider=Math.Clamp(_pointer.X,640,1180);_campaignCharacterPage=first/CampaignCharacterPageSize;return true;
    }
    private string CampaignScenarioName=>_campaignScenario==2?"キャンペーン#2 三国誌":"キャンペーン#1 地賽でタイマン";
    private void SelectCampaignScenario(int scenario){
        _campaignScenario=scenario;int count=scenario==2?3:2;
        if(_campaignCharacters.Length!=count){var old=_campaignCharacters;_campaignCharacters=Enumerable.Range(0,count).Select(i=>i<old.Length?old[i]:_random.Next(60)).ToArray();}
        _campaignCharacterTarget=_campaignPlayerPage=_campaignCharacterPage=0;_campaignMenuStage=2;
    }
    private void OpenCampaignMenu(){_campaignMenuStage=1;_campaignCharacterTarget=_campaignPlayerPage=_campaignCharacterPage=0;_titleIdle=0;for(int i=0;i<_campaignCharacters.Length;i++)_campaignCharacters[i]=_random.Next(60);}
    private void CampaignMenuInput(bool click,bool enter,bool escape,KeyboardState keys)
    {
        if(escape || click && CampaignBackButton.Contains(_pointer)){_campaignMenuStage--;return;}
        if(_campaignMenuStage==1){if(click && CampaignThreeKingdomsButton.Contains(_pointer))SelectCampaignScenario(2);else if(enter || click && CampaignDuelButton.Contains(_pointer))SelectCampaignScenario(1);return;}
        if(click){
            if(CampaignPlayersPrevious.Contains(_pointer)){_campaignPlayerPage=Math.Max(0,_campaignPlayerPage-1);return;}
            if(CampaignPlayersNext.Contains(_pointer)){_campaignPlayerPage=Math.Min((_campaignCharacters.Length-1)/6,_campaignPlayerPage+1);return;}
            if(CampaignCharactersPrevious.Contains(_pointer)){_campaignCharacterPage=Math.Max(0,_campaignCharacterPage-1);return;}
            if(CampaignCharactersNext.Contains(_pointer)){_campaignCharacterPage=Math.Min((ConquerorCatalog.BaseCount-1)/CampaignCharacterPageSize,_campaignCharacterPage+1);return;}
            for(int player=_campaignPlayerPage*6;player<Math.Min(_campaignCharacters.Length,(_campaignPlayerPage+1)*6);player++){
                if(CampaignPlayerTab(player).Contains(_pointer)){_campaignCharacterTarget=player;return;}
                if(CampaignRandomButton(player).Contains(_pointer)){_campaignCharacters[player]=_random.Next(60);_campaignCharacterTarget=player;return;}
            }
            for(int i=_campaignCharacterPage*CampaignCharacterPageSize;i<Math.Min(ConquerorCatalog.BaseCount,(_campaignCharacterPage+1)*CampaignCharacterPageSize);i++)if(CampaignCharacterCard(i).Contains(_pointer)){_campaignCharacters[_campaignCharacterTarget]=i;return;}
        }
        if(enter || click && CampaignStartButton.Contains(_pointer))BeginOpening(false,BeginCampaignDuel);
    }
    private void BeginCampaignDuel()
    {
        int human=_campaignCharacters[0],computer=_campaignCharacters[1];
        BeginPlayerCount();_autoPlay=_demoActive=false;_statusMenuOpen=false;
        if(_campaignScenario==2)_setup.PrepareCampaignThreeKingdoms(_random.Next(),_random.Next(),_random.Next(),_random.Next(),_campaignCharacters);
        else _setup.PrepareCampaignDuel(_random.Next(),_random.Next(),_random.Next(),_random.Next(),human,computer);
        _world.ShowSetup(_setup,true);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;RestoreDomesticCamera();
    }
    private void DrawCampaignMenu()
    {
        _ui.Box(new(0,0,1920,1080),new Color(22,34,48));
        _ui.Text(_campaignMenuStage==1?"キャンペーンモード":CampaignScenarioName,new(54,35),.85f,Cream);
        if(_campaignMenuStage==1){
            if(_setupHost!=null)_spriteBatch.Draw(_setupHost,new Rectangle(100,200,500,750),Color.White);
            _ui.Box(new(680,170,1170,270),Cream);_ui.Tile(new(680,310),new(45,45),MathHelper.PiOver4,Cream);
            _ui.Text("キャンペーンモードは、予め用意された世界で",new(730,210),.58f,Muted);
            _ui.Text("戦争をするモードです。",new(730,280),.58f,Muted);
            _ui.Text("それでは、以下のメニューから選んでください。",new(730,350),.58f,Muted);
            _ui.Button(CampaignDuelButton,"キャンペーン#1 地賽でタイマン",Accent,.65f);
            _ui.Text("１人 対 CPU１人 / １×１×１ / 各３ sectors",new(750,640),.48f,Cream);
            _ui.Button(CampaignThreeKingdomsButton,"キャンペーン#2 三国誌",Accent,.65f);
            _ui.Text("１人 対 CPU２人 / ２×２×２ / 各８ sectors",new(750,780),.48f,Cream);
            _ui.Text("各国は隣り合う２面 / 地形・地点の性能はランダム",new(750,830),.46f,Cream);
        }else{
            _ui.Text("左：参加者を選択 / 右：キャラクターを選択 / 同じキャラクターも選べます",new(54,115),.49f,Cream);
            for(int player=_campaignPlayerPage*6;player<Math.Min(_campaignCharacters.Length,(_campaignPlayerPage+1)*6);player++){
                var tab=CampaignPlayerTab(player);_ui.Box(new(tab.X,tab.Y,tab.Width,220),new Color(44,64,80));
                _ui.Button(tab,player==0?"あなた":$"コンピューター {player}",_campaignCharacterTarget==player?Accent:Muted,.48f);
                _portraitRenderer.Draw(_spriteBatch,new(_campaignCharacters[player],0),new(tab.X+12,tab.Y+57,Math.Min(145,tab.Width/2-20),Math.Min(145,tab.Width/2-20)));
                _ui.Center($"No.{_campaignCharacters[player]+1:00}",new(CampaignRandomButton(player).X,tab.Y+68,CampaignRandomButton(player).Width,30),.45f,Cream);
                _ui.Button(CampaignRandomButton(player),"ランダム",Muted,.48f);
            }
            for(int i=_campaignCharacterPage*CampaignCharacterPageSize;i<Math.Min(ConquerorCatalog.BaseCount,(_campaignCharacterPage+1)*CampaignCharacterPageSize);i++){
                var r=CampaignCharacterCard(i);bool selected=_campaignCharacters[_campaignCharacterTarget]==i;
                int uses=_campaignCharacters.Count(c=>c==i);
                _ui.Box(r,selected?Accent:new Color(44,64,80));
                _portraitRenderer.Draw(_spriteBatch,new(i,0),new(r.X+4,r.Y+4,r.Width-8,100));
                _ui.Center($"{i+1:00}"+(uses>0?$" 選択{uses}人":""),new(r.X+2,r.Bottom-24,r.Width-4,22),.29f,Cream);
            }
            _ui.Button(CampaignPlayersPrevious,"←",Muted,.5f);_ui.Button(CampaignPlayersNext,"→",Muted,.5f);
            _ui.Center($"参加者 {_campaignPlayerPage+1}/{(_campaignCharacters.Length+5)/6}",new(400,890,_campaignDivider-430,30),.43f,Cream);
            _ui.Button(CampaignCharactersPrevious,"←",Muted,.5f);_ui.Button(CampaignCharactersNext,"→",Muted,.5f);
            _ui.Center($"キャラクター {_campaignCharacterPage+1}/{(ConquerorCatalog.BaseCount+CampaignCharacterPageSize-1)/CampaignCharacterPageSize}",new(_campaignDivider+100,930,1700-_campaignDivider,48),.43f,Cream);
            _ui.Button(CampaignStartButton,$"この{_campaignCharacters.Length}人で始める",Accent,.56f);
            _ui.Box(new(_campaignDivider-3,165,6,720),Muted);
            for(int i=0;i<3;i++)_ui.Box(new(_campaignDivider-7,505+i*10,14,3),Cream);
            _ui.Text("中央の仕切りをドラッグして左右の幅を調整",new(990,1005),.4f,Cream);

        }
        _ui.Button(CampaignBackButton,"戻る",Muted,.6f);
    }
}
