namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

public partial class Game1
{
    private TurnNewsArticle[] _turnPapers=Array.Empty<TurnNewsArticle>();
    private int _turnPaperIndex,_paperReadTurn=-1,_paperReadCountry=-1;
    private Screen _paperReturnScreen=Screen.WorldStatus;
    private double _paperAge,_paperIdle;
    private const double PaperIdleSeconds=8;
    private const double PaperReadingStart=8.3;
    private SoundEffect _paperKnock;
    private static readonly Rectangle PaperPrevious=new(690,990,220,52),PaperNext=new(925,990,220,52),PaperDone=new(1415,990,440,52);
    private bool TryBeginTurnNewspaper()
    {
        if(_conquestOverview || _screen is not (Screen.WorldStatus or Screen.Disposition) || !DomesticOperationsAllowed ||
            _setup.Industry.Pending || (_paperReadTurn==_setup.Population.Turn && _paperReadCountry==_setup.PlayerSlot) || _setup.TutorialStep>0)return false;
        _paperReadTurn=_setup.Population.Turn;_paperReadCountry=_setup.PlayerSlot;_paperReturnScreen=_screen;
        _turnPapers=TurnNewspaper.Build(_setup,_setup.PlayerSlot);_turnPaperIndex=0;_paperAge=_paperIdle=0;
        if(_turnPapers.Length>0){SelectPaperCountry(_turnPapers[0].Country);BeginPaperScenes();}
        if(_turnPapers.Length==0)return false;
        _screen=Screen.Newspaper;return true;
    }
    private void TurnNewspaperInput(bool click,bool enter,bool escape,double elapsed)
    {
        double oldAge=_paperAge;_paperAge+=Math.Max(0,elapsed);_paperIdle+=Math.Max(0,_paperAge-PaperReadingStart)-Math.Max(0,oldAge-PaperReadingStart);
        if(oldAge<.45 && _paperAge>=.45)PlayToyPost();
        UpdatePaperTileSound();
        if(click || enter || escape)_paperIdle=0;
        if(escape || enter || click && PaperDone.Contains(_pointer) || _paperIdle+1e-8>=PaperIdleSeconds){FinishTurnNewspaper();return;}
        if(!click)return;
        if(_paperAge<PaperReadingStart){_paperAge=PaperReadingStart;return;}
        var faces=CurrentPaperFaces;
        for(int i=0;i<faces.Length;i++)if(PaperFacePreview(i).Contains(_pointer)){
            _turnPaperIndex=faces[i];return;
        }
        var countries=_turnPapers.Select(a=>a.Country).Distinct().ToArray();
        int current=Array.IndexOf(countries,_turnPapers[_turnPaperIndex].Country);
        int next=PaperPrevious.Contains(_pointer)?Math.Max(0,current-1):PaperNext.Contains(_pointer)?Math.Min(countries.Length-1,current+1):current;
        if(next!=current){SelectPaperCountry(countries[next]);_paperAge=PaperReadingStart;}

    }
    private int[] CurrentPaperFaces=>Enumerable.Range(0,_turnPapers.Length).Where(i=>_turnPapers[i].Country==_turnPapers[_turnPaperIndex].Country).ToArray();
    private static Rectangle PaperFacePreview(int index)=>new(700+index%6*198,870+index/6*52,188,46);
    private void SelectPaperCountry(int country)
    {
        var faces=Enumerable.Range(0,_turnPapers.Length).Where(i=>_turnPapers[i].Country==country).ToArray();
        _turnPaperIndex=faces[Random.Shared.Next(faces.Length)];
    }
    private void FinishTurnNewspaper()
    {
        _screen=_paperReturnScreen;
        // Diplomacy stays pending until every paper has been put down.
        if(_diplomacyPending){_diplomacyPending=false;if(BeginDiplomacy())return;}
    }
    private void DrawTurnNewspaper()
    {
        var ink=new Color(57,52,48);var paper=new Color(250,236,202);
        DrawPaperScenes();
        var article=_turnPapers[_turnPaperIndex];var faces=CurrentPaperFaces;
        var countries=_turnPapers.Select(a=>a.Country).Distinct().ToArray();
        int face=Array.IndexOf(faces,_turnPaperIndex)+1;
        _ui.Box(new(20,22,590,58),new Color(23,42,48)*.75f);
        _ui.Text("新聞フェーズ / 朝刊",new(36,30),.65f,Cream);
        _ui.Box(new(690,18,1170,48),new Color(23,42,48)*.75f);
        NewsLine($"{TurnNewspaper.DayLabel(_setup.Population.Turn)} / {Array.IndexOf(countries,article.Country)+1} 紙目・全 {countries.Length} 紙 / {face}面",715,28,1120,.43f,Cream);
        float t=MathHelper.SmoothStep(0,1,(float)Math.Clamp((_paperAge-(PaperReadingStart-.7))/.7,0,1));
        var sheet=new Rectangle((int)MathHelper.Lerp(1100,700,t),(int)MathHelper.Lerp(500,300,t),(int)MathHelper.Lerp(260,1150,t),(int)MathHelper.Lerp(120,545,t));
        if(t>0){
            _ui.Box(new(sheet.X+12,sheet.Y+14,sheet.Width,sheet.Height),Color.Black*.3f);
            for(int layer=Math.Min(3,faces.Length-1);layer>0;layer--)ToyPaperBox(new(sheet.X+layer*5,sheet.Y+layer*5,sheet.Width,sheet.Height),Color.Lerp(paper,ink,.08f*layer));
            ToyPaperBox(sheet,paper);
            _ui.Box(new(sheet.Center.X-1,sheet.Y+8,2,sheet.Height-16),new Color(215,194,155)*.35f);
        }
        if(t>=.99f){
            DrawNationalNewspaperMasthead(new(850,310,850,78),article.Country+1);
            _ui.Box(new(730,395,1090,3),ink);
            NewsLine($"{TurnNewspaper.DayLabel(_setup.Population.Turn)} 朝刊 / 第{article.Country+1}国 / {_setup.ConquerorNames[article.Country]}国の視点 / {face}面：{article.Kind}",730,410,1090,.39f,ink);
            NewsLine(article.Headline,730,459,1090,.73f,ink);
            float lineStep=Math.Min(51,250f/Math.Max(1,article.Lines.Length));
            for(int i=0;i<article.Lines.Length;i++)NewsLine(article.Lines[i],730,528+(int)(i*lineStep),1090,.43f,ink);
            if(article.Gained+article.Lost>0){
                _ui.Box(new(750,778,Math.Min(300,article.Gained*28),10),new Color(72,145,111));
                _ui.Box(new(1320,778,Math.Min(300,article.Lost*28),10),new Color(184,82,69));
                NewsLine($"獲得 {article.Gained} {article.Unit}",750,800,450,.36f,ink);NewsLine($"喪失 {article.Lost} {article.Unit}",1320,800,450,.36f,ink);
            }
            for(int i=0;i<faces.Length;i++){
                var preview=PaperFacePreview(i);bool selected=faces[i]==_turnPaperIndex;
                ToyPaperBox(preview,selected?new Color(220,193,135):new Color(239,221,183));
                NewsLine($"{i+1}面 / {_turnPapers[faces[i]].Kind}",preview.X+8,preview.Y+5,preview.Width-16,.31f,ink);
                NewsLine(_turnPapers[faces[i]].Headline,preview.X+8,preview.Y+25,preview.Width-16,.25f,ink);
            }
            _ui.Button(PaperPrevious,"前の新聞",Muted,.46f);_ui.Button(PaperNext,"次の新聞",Accent,.46f);
        }else{
            _ui.Box(new(780,900,970,50),new Color(23,42,48)*.75f);
            _ui.Center("朝の配達 / クリックで新聞を広げる",new(790,905,950,40),.45f,Cream);
        }
        // The reader is independent of the national paper currently being browsed.
        int reader=_paperReadCountry>=0&&_paperReadCountry<_setup.ActiveCount?_paperReadCountry:_setup.PlayerSlot;
        DrawRulerMedallion(reader,new Vector2(170,865));
        _ui.Box(new(325,905,285,88),new Color(23,42,48)*.8f);
        _ui.Center("新聞を読んでいる人",new(330,914,275,28),.38f,Cream);
        _ui.Center($"#{reader+1} / 内政のターン",new(330,953,275,28),.37f,Cream);
        _ui.Button(PaperDone,_diplomacyPending?"新聞を置いて外交へ":"新聞を置いて内政へ",Accent,.43f);
        DrawPhaseTimer(new(690,1055,1170,10),_paperIdle,PaperIdleSeconds);
    }
    private void NewsLine(string text,int x,int y,int width,float scale,Color color)
        =>_ui.Text(text,new(x,y),Math.Min(scale,width/Math.Max(1,_font.MeasureString(text).X)),color);
    private void ToyPaperBox(Rectangle r,Color color)
    {
        _ui.Box(r,Color.Lerp(color,Color.Black,.25f));
        _ui.Box(new(r.X+4,r.Y+4,Math.Max(1,r.Width-8),Math.Max(1,r.Height-8)),color);
        _ui.Box(new(r.X+5,r.Y+4,Math.Max(1,r.Width-10),3),Color.Lerp(color,Color.White,.35f));
    }
    private void PaperOval(Rectangle r,Color color)
    {
        for(int y=0;y<r.Height;y+=2){float t=(y+.5f)/r.Height*2-1;int half=(int)(r.Width*.5f*MathF.Sqrt(Math.Max(0,1-t*t)));if(half>0)_ui.Box(new(r.Center.X-half,r.Y+y,half*2,2),color);}
    }
    private void PlayToyPost()
    {
        if(SeGain<=0)return;
        try{
            if(_paperKnock==null){
                const int rate=22050,count=rate/4;var bytes=new byte[count*2];
                for(int i=0;i<count;i++){double t=i/(double)rate;double tail=Math.Max(0,t-.065);double v=Math.Sin(t*950)*Math.Exp(-t*65)+.6*Math.Sin(tail*620)*Math.Exp(-tail*38)*(t>.065?1:0);short s=(short)(v*4500);bytes[i*2]=(byte)s;bytes[i*2+1]=(byte)(s>>8);}
                _paperKnock=new SoundEffect(bytes,rate,AudioChannels.Mono);
            }
            _paperKnock.Play(.4f*SeGain,0,0);
        }catch(NoAudioHardwareException error){RecordFailure(error,"toy_post_audio");}
    }
}
