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
    private SoundEffect _paperKnock;
    private static readonly Rectangle PaperPrevious=new(120,940,290,65),PaperNext=new(430,940,290,65),PaperDone=new(1370,940,430,65);
    private bool TryBeginTurnNewspaper()
    {
        if(_conquestOverview || _screen is not (Screen.WorldStatus or Screen.Disposition) || !DomesticOperationsAllowed ||
            _setup.Industry.Pending || (_paperReadTurn==_setup.Population.Turn && _paperReadCountry==_setup.PlayerSlot) || _setup.TutorialStep>0)return false;
        _paperReadTurn=_setup.Population.Turn;_paperReadCountry=_setup.PlayerSlot;_paperReturnScreen=_screen;
        _turnPapers=TurnNewspaper.Build(_setup,_setup.PlayerSlot);_turnPaperIndex=0;_paperAge=_paperIdle=0;
        if(_turnPapers.Length==0)return false;
        _screen=Screen.Newspaper;return true;
    }
    private void TurnNewspaperInput(bool click,bool enter,bool escape,double elapsed)
    {
        double oldAge=_paperAge;_paperAge+=Math.Max(0,elapsed);_paperIdle+=Math.Max(0,elapsed);
        if(oldAge<.45 && _paperAge>=.45)PlayToyPost();
        if(click || enter || escape)_paperIdle=0;
        if(escape || enter || click && PaperDone.Contains(_pointer) || _paperIdle+1e-8>=PaperIdleSeconds){FinishTurnNewspaper();return;}
        if(!click)return;
        if(_paperAge<2){_paperAge=2;return;}
        int next=PaperPrevious.Contains(_pointer)?Math.Max(0,_turnPaperIndex-1):PaperNext.Contains(_pointer)?Math.Min(_turnPapers.Length-1,_turnPaperIndex+1):_turnPaperIndex;
        if(next!=_turnPaperIndex){bool same=_turnPapers[next].Country==_turnPapers[_turnPaperIndex].Country;_turnPaperIndex=next;_paperAge=same?2:0;}
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
        _ui.Box(new(0,0,1920,1080),new Color(35,46,53));
        ToyPaperBox(new(65,130,1790,900),new Color(97,64,45));
        for(int i=0;i<10;i++){
            _ui.Box(new(70,150+i*86,1780,2),new Color(117,79,54));
            for(int j=0;j<4;j++)_ui.Box(new(90+j*440,164+i*86,270,1),new Color(105,70,49));
        }
        var article=_turnPapers[_turnPaperIndex];
        _ui.Text("新聞フェーズ / 朝刊",new(80,40),.8f,Cream);
        _ui.Text($"{TurnNewspaper.DayLabel(_setup.Population.Turn)} / {_turnPaperIndex+1} 紙目・全 {_turnPapers.Length} 紙",new(990,58),.45f,Cream);
        DrawToyPost(article.Country,_paperAge);
        // A toy cup and saucer stay outside the readable paper.
        PaperOval(new(1625,212,185,56),new Color(42,31,25)*.4f);
        PaperOval(new(1620,200,185,56),new Color(191,208,190));
        PaperOval(new(1633,206,159,38),new Color(223,230,205));
        PaperOval(new(1755,162,54,65),paper);PaperOval(new(1768,174,26,38),new Color(97,64,45));
        ToyPaperBox(new(1650,157,116,81),paper);PaperOval(new(1650,146,116,30),new Color(242,224,185));
        PaperOval(new(1660,151,96,19),new Color(76,45,29));
        for(int i=0;i<3;i++)_ui.Box(new(1678+i*24,118-i*9,5,20),paper*.35f);
        double reveal=Math.Clamp((_paperAge-.7)/1.3,0,1);float t=MathHelper.SmoothStep(0,1,(float)reveal);
        var sheet=new Rectangle((int)MathHelper.Lerp(235,310,t),(int)MathHelper.Lerp(260,155,t),(int)MathHelper.Lerp(190,1290,t),(int)MathHelper.Lerp(100,740,t));
        if(_paperAge>=.7){
            _ui.Box(new(sheet.X+12,sheet.Y+14,sheet.Width,sheet.Height),Color.Black*.3f);ToyPaperBox(sheet,paper);
            _ui.Box(new(sheet.Center.X-1,sheet.Y+8,2,sheet.Height-16),new Color(215,194,155)*.35f);
            _ui.Box(new(sheet.X+8,sheet.Bottom-8,sheet.Width-16,2),new Color(196,176,139));
        }
        if(t>=.99f){
            DrawNationalNewspaperMasthead(new(475,174,960,100),article.Country+1);
            _ui.Box(new(350,285,1210,4),ink);
            NewsLine($"{TurnNewspaper.DayLabel(_setup.Population.Turn)} 朝刊 / 第{article.Country+1}国 / {_setup.ConquerorNames[article.Country]}国の視点 / {article.Kind}",350,305,1200,.44f,ink);
            NewsLine(article.Headline,350,370,1190,.85f,ink);
            for(int i=0;i<article.Lines.Length;i++)NewsLine(article.Lines[i],350,465+i*60,1190,.46f,ink);
            _ui.Box(new(350,735,1190,2),ink*.35f);
            if(article.Commentary?.Length>0){
                NewsLine("講評 / Commentary",370,752,1160,.45f,ink);
                for(int i=0;i<Math.Min(3,article.Commentary.Length);i++)NewsLine(article.Commentary[i],370,791+i*31,1160,.38f,ink);
            }else if(article.Gained+article.Lost>0){
                _ui.Box(new(380,785,Math.Min(430,article.Gained*28),24),new Color(72,145,111));
                _ui.Box(new(980,785,Math.Min(430,article.Lost*28),24),new Color(184,82,69));
                NewsLine($"獲得 {article.Gained} {article.Unit}",380,825,450,.48f,ink);NewsLine($"喪失 {article.Lost} {article.Unit}",980,825,450,.48f,ink);
            }else NewsLine("結果は記録、予定は変更あり、予想は『もしこうなら』。",370,790,1160,.44f,ink);
        }else _ui.Text("朝の配達 / クリックで新聞を広げる",new(580,820),.55f,Cream);
        // The reader stays fixed while the displayed newspaper's country changes.
        int reader=_paperReadCountry>=0&&_paperReadCountry<_setup.ActiveCount?_paperReadCountry:_setup.PlayerSlot;
        DrawRulerMedallion(reader,new Vector2(1755,580));
        _ui.Center("新聞を読んでいる人",new(1605,750,300,30),.42f,Cream);
        _ui.Center($"#{reader+1} / 内政のターン",new(1605,788,300,30),.4f,Cream);
        _ui.Button(PaperPrevious,"前の新聞",Muted,.5f);_ui.Button(PaperNext,"次の新聞",Accent,.5f);
        _ui.Button(PaperDone,_diplomacyPending?"新聞を置いて外交へ":"新聞を置いて内政へ",Accent,.46f);
        DrawPhaseTimer(new(120,1020,1680,10),_paperIdle,PaperIdleSeconds);
    }
    private void NewsLine(string text,int x,int y,int width,float scale,Color color)
        =>_ui.Text(text,new(x,y),Math.Min(scale,width/Math.Max(1,_font.MeasureString(text).X)),color);
    private void DrawToyPost(int country,double age)
    {
        // Four fictional toy designs belong to the four national masthead families.
        int kind=Array.IndexOf(new[]{"breagan","jostailu","leikfang","tegan"},_setup.NationalNewspaperFor(country+1).Id);
        var color=kind switch{0=>new Color(183,67,57),1=>new Color(54,116,143),2=>new Color(220,172,63),_=>new Color(78,140,104)};
        int x=105,y=320;
        _ui.Box(new(x+52,y+130,32,230),new Color(183,155,115));
        var box=kind switch{0=>new Rectangle(x,y,185,125),1=>new Rectangle(x+20,y-60,130,210),2=>new Rectangle(x,y+15,170,145),_=>new Rectangle(x,y-15,175,150)};
        _ui.Box(new(box.X+8,box.Y+10,box.Width,box.Height),Color.Black*.25f);
        if(kind is 0 or 1)PaperOval(new(box.X,box.Y-38,box.Width,78),Color.Lerp(color,Color.White,.16f));
        if(kind==3){for(int i=0;i<34;i++)_ui.Box(new(box.X+i,box.Y-34+i,box.Width-i*2,2),Color.Lerp(color,Color.Black,.2f));}
        ToyPaperBox(box,color);
        _ui.Box(new(box.X+14,box.Y+18,box.Width-28,14),new Color(47,48,46));
        _ui.Box(new(box.X+18,box.Bottom-32,box.Width-36,8),new Color(241,218,160));
        PaperOval(new(box.Right-27,box.Bottom-24,10,10),new Color(237,214,166));
        PaperOval(new(box.X+12,box.Bottom-24,10,10),new Color(237,214,166));
        _ui.Text($"{country+1}",new(box.X+26,box.Y+53),.7f,Cream);
        if(kind==0){_ui.Box(new(box.Right-12,box.Y-38,8,65),paperColor());_ui.Box(new(box.Right-4,box.Y-38,32,20),color);}
        if(age<.7){int px=(int)MathHelper.Lerp(440,box.X+24,(float)Math.Clamp(age/.45,0,1));ToyPaperBox(new(px,box.Y+9,95,25),new Color(250,236,202));_ui.Box(new(px+8,box.Y+17,50,3),color);}
        Color paperColor()=>new Color(241,218,160);
    }
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
