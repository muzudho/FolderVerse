namespace FolderVerse;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _industrySurprise,_industryPrayer;
    private double _industryAge,_industryIdleAge;
    private const double IndustryIdleSeconds=5;
    private float _industryStartAngle,_industryStopAngle;
    private bool _industryApplied;
    private const double IndustrySpinSeconds=4.5;
    private const double IndustryRevealSeconds=.4;
    private const double IndustryLeadSeconds=1.5,IndustryTailSeconds=1.5;
    private double IndustryReadSeconds=>IndustryLeadSeconds+Math.Max(0,_setup.Industry.Wheel.Count-1)*IndustryRevealSeconds+IndustryTailSeconds;
    private int IndustryVisibleLabels=>_industryAge<IndustryLeadSeconds?0:Math.Min(_setup.Industry.Wheel.Count,1+(int)((_industryAge-IndustryLeadSeconds)/IndustryRevealSeconds+1e-8));
    private double IndustryStopSeconds=>IndustryReadSeconds+IndustrySpinSeconds;
    private static readonly Rectangle IndustryContinue=new(1230,935,590,80);
    private void BeginIndustry()
    {
        StopBattleMusic();
        // Both introductory steps omit the roulette screen, retaining the normal industry processing.
        if(_setup.TutorialStep is 1 or 2 && _setup.Industry.Pending){
            _setup.Industry.Roll(_setup.PlacementSeed^_setup.FortuneSeed);_setup.Industry.Apply(_setup);_industryApplied=true;_world.ShowSetup(_setup,true);
        }
        if(!_setup.Industry.Pending){_screen=Screen.WorldStatus;RestoreDomesticCamera();OpenDisposition();return;}
        _setup.Industry.Roll(_setup.PlacementSeed^_setup.FortuneSeed);
        int before=0;for(int i=0;i<_setup.Industry.SelectedSector;i++)before+=_setup.Industry.Wheel[i].Percent;
        _industryStartAngle=-MathHelper.PiOver2;
        _industryStopAngle=_industryStartAngle+MathHelper.TwoPi*5-(before+_setup.Industry.Wheel[_setup.Industry.SelectedSector].Percent*.5f)/100*MathHelper.TwoPi;
        _industryAge=_industryIdleAge=0;_industryApplied=false;_screen=Screen.Industry;
        _populationCell=_populationNode=-1;_reignView=false;
    }
    private void UpdateIndustry(double elapsed,bool proceed)
    {
        double before=_industryAge;_industryAge+=elapsed;
        _industryIdleAge+=Math.Max(0,_industryAge-IndustryStopSeconds)-Math.Max(0,before-IndustryStopSeconds);
        if(_industryAge<IndustryStopSeconds)return;
        if(!_industryApplied){_setup.Industry.Apply(_setup);_industryApplied=true;_world.ShowSetup(_setup,true);}
        if(!proceed && _industryIdleAge+1e-8<IndustryIdleSeconds)return;
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
        var palette=new[]{new Color(80,181,176),new Color(219,140,175),new Color(123,155,222),new Color(164,194,98)};
        var colors=new Color[wheel.Count];int max=System.Linq.Enumerable.Max(wheel,s=>s.Turns);
        for(int i=0;i<wheel.Count;i++){
            var color=wheel[i].Turns==0?new Color(126,139,158):wheel[i].Turns==max?new Color(248,194,58):palette[i%2];
            bool Conflict(Color c)=>i>0 && colors[i-1]==c || i==wheel.Count-1 && i>0 && colors[0]==c;
            if(Conflict(color))foreach(var alternative in palette)if(!Conflict(alternative)){color=alternative;break;}
            colors[i]=color;
        }
        return colors;
    }
    private void DrawIndustry()
    {
        bool stopped=_industryAge>=IndustryStopSeconds;int result=_setup.Industry.Result;
        _ui.Text("産業フェーズ",new(54,28),1.1f,Cream);
        var mob=_setupHost;
        if(stopped && result==0)mob=_industryPrayer??=LoadIndustryMob("prayer");
        else if(stopped && _setup.Industry.Jackpot)mob=_industrySurprise??=LoadIndustryMob("surprised");
        if(mob!=null)_spriteBatch.Draw(mob,new Rectangle(50,250,480,720),Color.White);
        string[] lines=!stopped?new[]{"さあ、産業の時間です！","あなたは何台のロボットを製造できるかなー？"}:
            result==0?new[]{"ロボット不足の征服者のみなさん、ざんねんでした～！","工場はストップだー！","次は製造できるように、祈ってくださーい！"}:
            _setup.Industry.Jackpot?new[]{"数年に一度の当たり年だあ！？","大穴当てたみなさん、今だ、大チャーンス！"}:
            new[]{"さあ、工場を動かしましょう！",$"{result}産業ターン進みました！"};
        _ui.Box(new(565,130,1290,205),Cream);_ui.Tile(new(558,270),new(36,36),MathHelper.PiOver4,Cream);
        for(int i=0;i<lines.Length;i++)_ui.Text(lines[i],new(610,160+i*55),.65f,Muted);
        var center=new Vector2(1170,625);const float radius=255;
        float progress=(float)Math.Clamp((_industryAge-IndustryReadSeconds)/IndustrySpinSeconds,0,1);
        float angle=MathHelper.Lerp(_industryStartAngle,_industryStopAngle,1-MathF.Pow(1-progress,4));
        float cumulative=0;
        var labels=new List<(string Text,Vector2 At)>();
        var colors=IndustryColors(_setup.Industry.Wheel);
        for(int i=0;i<_setup.Industry.Wheel.Count;i++)
        {
            var sector=_setup.Industry.Wheel[i];float start=angle+cumulative/100*MathHelper.TwoPi;
            var color=colors[i];
            if(stopped && i==_setup.Industry.SelectedSector)color=Color.Lerp(color,Color.White,.25f);
            int steps=sector.Percent*5;
            for(int k=0;k<=steps;k++)
            {
                float a=start+sector.Percent/100f*MathHelper.TwoPi*k/steps;
                _ui.Tile(center+new Vector2(MathF.Cos(a),MathF.Sin(a))*radius*.5f,new(radius,4),a,color);
            }
            float middle=start+sector.Percent/200f*MathHelper.TwoPi;
            var label=center+new Vector2(MathF.Cos(middle),MathF.Sin(middle))*(sector.Percent<=2?radius*.94f:radius*.7f);
            if(i<IndustryVisibleLabels)labels.Add((sector.Turns.ToString(),label));
            cumulative+=sector.Percent;
        }
        foreach(var label in labels)_ui.Center(label.Text,new((int)label.At.X-23,(int)label.At.Y-18,46,36),.65f,new Color(19,38,49));
        for(int y=0;y<28;y++)_ui.Box(new((int)center.X-28+y,(int)(center.Y-radius)-35+y,56-y*2,1),Cream);
        _ui.Center(stopped?$"{result} 産業ターン":_industryAge<IndustryReadSeconds?"ルーレットを見てみよう":"ルーレット回転中",new(800,890,740,45),.8f,Cream);
        _ui.Text($"全体ターン {_setup.Population.Turn} / 産業ターン累計 {_setup.Robots.IndustryTurn}",new(565,1000),.5f,Cream);
        if(stopped){_ui.Button(IndustryContinue,"次の内政へ",Accent,.7f);DrawPhaseTimer(new(565,1050,1255,12),_industryIdleAge,IndustryIdleSeconds);}
    }
}
