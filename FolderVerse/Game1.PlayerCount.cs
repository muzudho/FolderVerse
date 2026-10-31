namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _requestedPlayers=1;
    private int _requestedComputers=19;
    private Texture2D _setupHost;
    private SoundEffect _turnChime;
    private (int Turn,int Ruler) _notifiedTurn=(-1,-1);
    private static readonly Rectangle PlayerCountConfirm=new(1050,900,700,90);
    private static Rectangle PlayerCountOption(int i)=>new(730+i%10*109,400+i/10*54,100,46);
    public static string EstimatedPlayTime(int players)
    {
        if(players<0 || players>GameLimits.MaxPlayers)throw new ArgumentOutOfRangeException(nameof(players));
        int minutes=players*20;
        return minutes<60?$"{minutes}分":minutes%60==0?$"{minutes/60}時間":$"{minutes/60}時間{minutes%60}分";
    }
    private static string MetaTime(int players)=>"＜"+string.Concat(EstimatedPlayTime(players).Select(c=>c>='0' && c<='9'?(char)('０'+c-'0'):c))+"＞";
    private void BeginPlayerCount(){_campaignMenuStage=0;FinishConquestOverview();_conquestOverviewTurn=-1;_tutorialMenuOpen=false;_titleMenuOpen=false;_lastAutoSaveTurn=-1;_domesticViews.Clear();_requestedPlayers=1;_requestedComputers=19;_notifiedTurn=(-1,-1);_screen=Screen.PlayerCount;}
    private void PlayerCountInput(bool click,bool enter,KeyboardState keys)
    {
        if(keys.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))_requestedPlayers=Math.Max(0,_requestedPlayers-1);
        if(keys.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))_requestedPlayers=Math.Min(GameLimits.MaxPlayers,_requestedPlayers+1);
        if(click)for(int i=0;i<=GameLimits.MaxPlayers;i++)if(PlayerCountOption(i).Contains(_pointer))_requestedPlayers=i;
        if(enter || click && PlayerCountConfirm.Contains(_pointer)){_requestedComputers=Math.Min(_requestedComputers,GameLimits.MaxPlayers-_requestedPlayers);_screen=Screen.ComputerPlayerCount;}
    }
    private void DrawPlayerCount()
    {
        _ui.Text("＜大きなお子様＞の人数確認フェーズ",new(54,28),1f,Cream);
        if(_setupHost!=null)_spriteBatch.Draw(_setupHost,new Rectangle(100,180,500,750),Color.White);
        _ui.Box(new(680,130,1180,230),Cream);
        _ui.Tile(new(675,240),new(45,45),MathHelper.PiOver4,Cream);
        const string greeting="さあ、＜大きなお子様＞のみなさん＜画面＞の前に集まってください。";
        _ui.Text(greeting,new(730,155),Math.Min(.65f,1080/_font.MeasureString(greeting).X),Muted);
        _ui.Text("準備はよろしいですか？",new(730,225),.65f,Muted);
        _ui.Text("このゲームをお遊びになる人数を選んでください。",new(730,300),.65f,Muted);
        for(int i=0;i<=GameLimits.MaxPlayers;i++)_ui.Button(PlayerCountOption(i),i.ToString(),_requestedPlayers==i?Accent:Muted,.65f);
        _ui.Center($"＜{_requestedPlayers}人＞ / 推定プレイ時間は{MetaTime(_requestedPlayers)}です。",new(690,800,1160,55),.7f,Cream);
        _ui.Button(PlayerCountConfirm,"よろしいですか？　次へ",Accent,.65f);
        _ui.Text("＜１人あたり20分＞の概算です。遊ぶ時間は自由です。",new(730,1005),.5f,Cream);
        _ui.Button(BackButton,"タイトルへ戻る",Muted,.6f);
    }
    private void ComputerPlayerCountInput(bool click,bool enter,KeyboardState keys)
    {
        int maximum=GameLimits.MaxPlayers-_requestedPlayers;
        if(keys.IsKeyDown(Keys.Left) && _previousKeyboard.IsKeyUp(Keys.Left))_requestedComputers=Math.Max(0,_requestedComputers-1);
        if(keys.IsKeyDown(Keys.Right) && _previousKeyboard.IsKeyUp(Keys.Right))_requestedComputers=Math.Min(maximum,_requestedComputers+1);
        if(click)for(int i=0;i<=maximum;i++)if(PlayerCountOption(i).Contains(_pointer))_requestedComputers=i;
        if((enter || click && PlayerCountConfirm.Contains(_pointer)) && _requestedPlayers+_requestedComputers>0)
        {_setup.SetParticipantCounts(_requestedPlayers,_requestedComputers);BeginRolling();}
    }
    private void DrawComputerPlayerCount()
    {
        _ui.Text("＜コンピュータープレイヤー＞の人数確認フェーズ",new(54,28),.85f,Cream);
        if(_setupHost!=null)_spriteBatch.Draw(_setupHost,new Rectangle(100,180,500,750),Color.White);
        _ui.Box(new(680,130,1180,230),Cream);
        _ui.Tile(new(675,240),new(45,45),MathHelper.PiOver4,Cream);
        _ui.Text("一緒に遊ぶ＜コンピュータープレイヤー＞の人数を",new(730,170),.65f,Muted);
        _ui.Text("選んでください。合計60人まで参加できます。",new(730,260),.65f,Muted);
        int maximum=GameLimits.MaxPlayers-_requestedPlayers;
        for(int i=0;i<=maximum;i++)_ui.Button(PlayerCountOption(i),i.ToString(),_requestedComputers==i?Accent:Muted,.65f);
        _ui.Center($"＜大きなお子様 {_requestedPlayers}人 ＋ コンピューター {_requestedComputers}人 / 合計 {_requestedPlayers+_requestedComputers}人＞",new(690,800,1160,55),.55f,Cream);
        bool valid=_requestedPlayers+_requestedComputers>0;
        _ui.Button(PlayerCountConfirm,valid?"よろしいですか？　この人数で遊ぶ":"合計１人以上を選んでください",valid?Accent:Muted,.6f);
        _ui.Text(_requestedPlayers==0?"＜大きなお子様＞が０人の場合は自動で進行します。":"コンピュータープレイヤーは自動で行動します。",new(730,1005),.5f,Cream);
        _ui.Button(BackButton,"人数を選び直す",Muted,.6f);
    }
    private void ConfirmHumanSelection()
    {
        _setup.HumanPlayers.Add(_setup.PlayerSlot);
        if(_setup.HumanPlayers.Count<_setup.PlayerCount){_screen=Screen.PlayerSelect;_hoveredSlot=_previewOwner=-1;return;}
        _setup.SelectTurnRuler(_setup.HumanPlayers[0]);BeginLandConquest();
    }
    private void LoadSetupHost()
    {
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Setup","host.png"));
        _setupHost=Texture2D.FromStream(GraphicsDevice,stream);
        var pixels=new Color[_setupHost.Width*_setupHost.Height];_setupHost.GetData(pixels);
        RemoveMobBackdrop(pixels,_setupHost.Width,_setupHost.Height,new(-1,-1));_setupHost.SetData(pixels);
    }
    private void NotifyPlayerTurn()
    {
        if(!IsStatusScreen || _setup.PlayerSlot<0)return;
        var turn=(_setup.Population.Turn,_setup.PlayerSlot);
        if(turn==_notifiedTurn)return;
        _notifiedTurn=turn;
        try
        {
            if(_turnChime==null)
            {
                const int rate=22050;var bytes=new byte[rate*2/2];
                for(int i=0;i<bytes.Length/2;i++)
                {
                    double t=i/(double)rate,local=t%.16;
                    double frequency=t<.16?523.25:t<.32?659.25:783.99;
                    short sample=(short)(Math.Sin(2*Math.PI*frequency*t)*7000*Math.Min(1,local/.008)*Math.Exp(-local*16));
                    bytes[i*2]=(byte)sample;bytes[i*2+1]=(byte)(sample>>8);
                }
                _turnChime=new SoundEffect(bytes,rate,AudioChannels.Mono);
            }
            if(!_demoActive && !_autoPlay)_turnChime.Play(.65f,0,0);
        }
        catch(NoAudioHardwareException error){RecordFailure(error,"turn_chime");}
    }
}
