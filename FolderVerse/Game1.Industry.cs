namespace FolderVerse;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _industrySurprise,_industryPrayer,_industryRoom,_industryWall;
    private double _industryAge,_industryIdleAge;
    private const double IndustryIdleSeconds=5;
    private float _industryStartAngle,_industryStopAngle;
    private const double IndustrySpinSeconds=4.5;
    private const double IndustryRevealSeconds=.4;
    private const double IndustryLeadSeconds=1.5,IndustryTailSeconds=1.5;
    private double IndustryReadSeconds=>IndustryLeadSeconds+Math.Max(0,_setup.Industry.Wheel.Count-1)*IndustryRevealSeconds+IndustryTailSeconds;
    private int IndustryVisibleLabels=>_industryAge<IndustryLeadSeconds?0:Math.Min(_setup.Industry.Wheel.Count,1+(int)((_industryAge-IndustryLeadSeconds)/IndustryRevealSeconds+1e-8));
    private double IndustryStopSeconds=>IndustryReadSeconds+IndustrySpinSeconds;
    private static readonly Rectangle IndustryContinue=new(1230,935,590,80);
    private static readonly Rectangle IndustryWheelContinue=new(1645,943,224,68);
    private static readonly Rectangle IndustrySkip=new(1680,40,190,48);
    private void SkipIndustry()
    {
        if(_industryAge>=IndustryStopSeconds)return;
        _industryAge=IndustryStopSeconds;_industryIdleAge=0;
        UpdateIndustry(0,false);
    }
    private void BeginIndustry()
    {
        _manufacturingActive=false;
        StopBattleMusic();
        if(_setup.IsPrimaplana)ResolvePrimaplanaCaptives();
        if(TryConquestEnding())return;
        // Both introductory steps omit the roulette screen, retaining the normal industry processing.
        if(_setup.TutorialStep is 1 or 2 or 4 && _setup.Industry.Pending){
            _setup.Industry.Roll(_setup.PlacementSeed^_setup.FortuneSeed);_setup.Industry.Apply(_setup);_world.ShowSetup(_setup,true);
        }
        if(!_setup.Industry.Pending){FinishIndustry();return;}
        _setup.Industry.Roll(_setup.PlacementSeed^_setup.FortuneSeed);
        int before=0;for(int i=0;i<_setup.Industry.SelectedSector;i++)before+=_setup.Industry.Wheel[i].Percent;
        _industryStartAngle=-MathHelper.PiOver2;
        _industryStopAngle=_industryStartAngle+MathHelper.TwoPi*5-(before+_setup.Industry.Wheel[_setup.Industry.SelectedSector].Percent*.5f)/100*MathHelper.TwoPi;
        _industryAge=_industryIdleAge=0;_screen=Screen.Industry;
        _populationCell=_populationNode=-1;_reignView=false;
    }
    private void UpdateIndustry(double elapsed,bool proceed)
    {
        if(_manufacturingActive){UpdateManufacturing(elapsed,proceed);return;}
        double before=_industryAge;_industryAge+=elapsed;
        _industryIdleAge+=Math.Max(0,_industryAge-IndustryStopSeconds)-Math.Max(0,before-IndustryStopSeconds);
        if(_industryAge<IndustryStopSeconds)return;
        if(!proceed && _industryIdleAge+1e-8<IndustryIdleSeconds)return;
        BeginManufacturing();
    }
    private void FinishIndustry()
    {
        EnterDomesticAfterIndustry();
    }
    private void EnterDomesticAfterIndustry()
    {
        _conquestEntryFromIndustry=_screen==Screen.Industry;
        _screen=Screen.WorldStatus;_world.ShowSetup(_setup,true);RestoreDomesticCamera();OpenDisposition();
    }
    private Texture2D LoadIndustryMob(string name)
    {
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Setup",name+".png"));
        var texture=Texture2D.FromStream(GraphicsDevice,stream);var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
        RemoveMobBackdrop(pixels,texture.Width,texture.Height,new(-1,-1));texture.SetData(pixels);return texture;
    }
    private static Color[] IndustryColors(IReadOnlyList<IndustrySector> wheel)
    {
        var palette=new[]{new Color(161,207,193),new Color(226,145,117),new Color(184,199,142),new Color(245,221,167),new Color(153,191,208)};
        var colors=new Color[wheel.Count];int max=System.Linq.Enumerable.Max(wheel,s=>s.Turns);
        for(int i=0;i<wheel.Count;i++){
            var color=wheel[i].Turns==0?new Color(180,191,198):wheel[i].Turns==max?new Color(243,186,82):palette[i%palette.Length];
            bool Conflict(Color c)=>i>0 && colors[i-1]==c || i==wheel.Count-1 && i>0 && colors[0]==c;
            if(Conflict(color))foreach(var alternative in palette)if(!Conflict(alternative)){color=alternative;break;}
            colors[i]=color;
        }
        return colors;
    }
    private void DrawIndustry()
    {
        if(_manufacturingActive){DrawManufacturing();return;}
        bool stopped=_industryAge>=IndustryStopSeconds;int result=_setup.Industry.Result;
        if(_industryWall==null){using var wall=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Setup","industry-roulette-wall-v001.png"));_industryWall=Texture2D.FromStream(GraphicsDevice,wall);}
        PortraitHudLayout.DrawMainBackground(_spriteBatch,_industryWall);
        if(_industryRoom==null){using var room=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Setup","industry-roulette-room-v001.png"));_industryRoom=Texture2D.FromStream(GraphicsDevice,room);}
        PortraitHudLayout.DrawBackground(_spriteBatch,_industryRoom);
        PortraitHudLayout.DrawSplitter(_ui);
        _ui.Text("産業フェーズ",new(54,28),1.1f,Cream);
        if(!stopped && !ComputerExhibition)_ui.Button(IndustrySkip,"スキップ",Accent,.55f);
        var mob=_setupHost;
        if(stopped && result==0)mob=_industryPrayer??=LoadIndustryMob("prayer");
        else if(stopped && _setup.Industry.Jackpot)mob=_industrySurprise??=LoadIndustryMob("surprised");
        if(mob!=null)_spriteBatch.Draw(mob,PortraitHudLayout.Character,Color.White);
        string[] lines=!stopped?new[]{"さあ、産業の時間です！","あなたは何台のロボットを","製造できるかなー？"}:
            result==0?new[]{"工場はストップだー！","次こそ製造できますように！"}:
            _setup.Industry.Jackpot?new[]{"数年に一度の当たり年だあ！？","今だ、大チャーンス！"}:
            new[]{"さあ、工場を動かしましょう！"};
        var speech=new Rectangle(500,40,900,320);
        DrawSpeechBalloon(speech);
        var textArea=BalloonRenderer.TextArea(speech);
        float scale=Math.Min(.68f,textArea.Width/Math.Max(1,System.Linq.Enumerable.Max(lines,l=>_font.MeasureString(l).X)));
        PortraitHudLayout.CenterLines(_ui,_font,lines,textArea,scale,new Color(57,52,48));        float progress=(float)Math.Clamp((_industryAge-IndustryReadSeconds)/IndustrySpinSeconds,0,1);
        float angle=MathHelper.Lerp(_industryStartAngle,_industryStopAngle,1-MathF.Pow(1-progress,4));
        DrawAntiqueIndustryWheel(angle);
        if(stopped)DrawIndustryDuration(result,new(54,916,608,65),Cream,.65f,suffix:" 産業ターン");
        else _ui.Center(_industryAge<IndustryReadSeconds?"ルーレットを見てみよう":"ルーレット回転中",new(54,926,608,45),.62f,Cream);
        _ui.Text($"全体ターン {_setup.Population.Turn} / 産業ターン累計 {IndustryTime.Format(_setup.Robots.IndustryTurn).Replace("+"," ")}",new(54,1000),.43f,Cream);
        if(stopped){_ui.Button(IndustryWheelContinue,"製造レーン稼働へ",Accent,.52f);DrawPhaseTimer(new(565,1050,1255,12),_industryIdleAge,IndustryIdleSeconds);}
    }
}
