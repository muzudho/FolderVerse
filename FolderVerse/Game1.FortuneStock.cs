namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private WorldSetup _fortuneStockPreview;
    private int _fortuneStockSeed=-1,_fortuneDistributionSeed,_fortuneStockKind;
    private bool _fortunePieDragging,_fortuneUniform;
    private static readonly Vector2 FortunePieCenter=new(210,425);
    private static readonly Rectangle FortuneDistribution=new(75,704,520,170);
    private static readonly Rectangle FortuneRandomize=new(75,925,320,48),FortuneUniform=new(405,925,190,48);
    private static Rectangle FortuneStockKey(int i)=>new(350,357+i*65,245,56);
    private static readonly Color FortuneRobotColor=new(67,169,140),FortuneRawColor=new(218,169,77),FortuneEmptyColor=new(53,74,91),FortuneFixedColor=new(36,104,103);

    private void EnsureFortuneStockPreview()
    {
        if(_fortuneStockPreview!=null)return;
        if(_fortuneStockSeed<0)_fortuneStockSeed=_random.Next();
        _fortuneDistributionSeed=_fortuneStockSeed^0x494E4954;
        _fortuneStockPreview=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(_setup));
        _fortuneStockPreview.CompleteLand(_fortuneStockSeed,0,0,new(0,0,_fortuneDistributionSeed));
        int minimum=(int)Math.Floor(100d*_fortuneStockPreview.InitialStock.FixedRobotUnits/Math.Max(1,_fortuneStockPreview.InitialStock.CapacityUnits));
        _robotScatterPercent=Math.Max(_robotScatterPercent,minimum);
        _rawStockPercent=Math.Min(_rawStockPercent,100-_robotScatterPercent);
        RebuildFortuneStock();
    }
    private void RebuildFortuneStock()=>_fortuneStockPreview.RebuildInitialStock(new(_robotScatterPercent,_rawStockPercent,_fortuneDistributionSeed,_fortuneUniform));
    private void SetFortuneStockPercent(int percent)
    {
        var stock=_fortuneStockPreview.InitialStock;
        int minimum=(int)Math.Floor(100d*stock.FixedRobotUnits/Math.Max(1,stock.CapacityUnits));
        int robot=_robotScatterPercent,raw=_rawStockPercent;
        if(_fortuneStockKind==0){robot=Math.Clamp(percent,minimum,100);raw=Math.Min(raw,100-robot);}
        else if(_fortuneStockKind==1){raw=Math.Clamp(percent,0,100-minimum);robot=Math.Clamp(robot,minimum,100-raw);}
        else {int empty=Math.Clamp(percent,0,100-minimum);robot=Math.Clamp(robot,minimum,100-empty);raw=100-empty-robot;}
        if(robot==_robotScatterPercent && raw==_rawStockPercent)return;
        _robotScatterPercent=robot;_rawStockPercent=raw;RebuildFortuneStock();
    }
    private void FortuneStockInput(MouseState mouse,bool active,bool click)
    {
        EnsureFortuneStockPreview();
        if(!active || mouse.LeftButton==ButtonState.Released)_fortunePieDragging=false;
        if(active && click){
            for(int i=0;i<3;i++)if(FortuneStockKey(i).Contains(_pointer))_fortuneStockKind=i;
            if(Vector2.Distance(_pointer.ToVector2(),FortunePieCenter)<=126)_fortunePieDragging=true;
            for(int i=0;i<5;i++)if(new Rectangle(75+i*104,610,92,48).Contains(_pointer))SetFortuneStockPercent(i*25);
            if(FortuneRandomize.Contains(_pointer)){_fortuneUniform=false;_fortuneDistributionSeed=_random.Next();RebuildFortuneStock();}
            if(FortuneUniform.Contains(_pointer)){_fortuneUniform=true;RebuildFortuneStock();}
        }
        if(_fortunePieDragging){
            var delta=_pointer.ToVector2()-FortunePieCenter;
            if(delta.Length()>12)SetFortuneStockPercent((int)Math.Round((Math.Atan2(delta.X,-delta.Y)+Math.PI*2)%(Math.PI*2)/Math.PI/2*100));
        }
    }
    private void DrawFortuneStock()
    {
        EnsureFortuneStockPreview();var stock=_fortuneStockPreview.InitialStock;float total=Math.Max(1,stock.CapacityUnits);
        float robot=stock.RobotUnits/total,raw=stock.RawUnits/total,fixedShare=stock.FixedRobotUnits/total;
        _ui.Text("格納庫の初期配置 / ポケットを除く",new(75,280),.5f,Cream);
        DrawSharedRatePie(FortunePieCenter,108,new[]{(fixedShare*360,FortuneFixedColor),((robot-fixedShare)*360,FortuneRobotColor),(raw*360,FortuneRawColor),((1-robot-raw)*360,FortuneEmptyColor)});
        var names=new[]{"ロボット","原材料","空き"};var colors=new[]{FortuneRobotColor,FortuneRawColor,FortuneEmptyColor};var shares=new[]{robot,raw,1-robot-raw};
        for(int i=0;i<3;i++){
            var key=FortuneStockKey(i);_ui.Button(key,$"{names[i]}  {shares[i]*100:0.#}%",_fortuneStockKind==i?Accent:Muted,.43f);
            _ui.Box(new(key.X+7,key.Y+9,7,key.Height-18),colors[i]);
        }
        _ui.Box(new(75,553,10,12),FortuneFixedColor);
        _ui.Text($"固定ロボット {fixedShare*100:0.#}% / 管理人・作業員",new(92,550),.37f,Cream);
        _ui.Text("種類を選び、円をドラッグして割合を変更",new(75,576),.34f,Cream);
        for(int i=0;i<5;i++){
            var b=new Rectangle(75+i*104,610,92,48);_ui.Button(b,$"{i*25}%",Muted,.38f);
            _spriteBatch.Draw(RatePieIcon(i*90,4),new Rectangle(b.X+2,b.Y+13,22,22),Color.White);
        }
        _ui.Text($"全 {stock.CapacityUnits/3} 格納庫 / 1/3枠ずつ配分",new(75,674),.37f,Cream);
        _ui.Box(FortuneDistribution,FortuneEmptyColor);
        float left=FortuneDistribution.X;Vector2? previousTop=null,previousRaw=null;
        foreach(var site in stock.Sites){
            float width=FortuneDistribution.Width*site.CapacityUnits/total;
            float upper=(site.RobotUnits+site.RawUnits)/(float)Math.Max(1,site.CapacityUnits),lower=site.RawUnits/(float)Math.Max(1,site.CapacityUnits);
            int x=(int)Math.Round(left),right=(int)Math.Round(left+width),bottom=FortuneDistribution.Bottom;
            int topY=bottom-(int)Math.Round(upper*FortuneDistribution.Height),rawY=bottom-(int)Math.Round(lower*FortuneDistribution.Height);
            _ui.Box(new(x,rawY,Math.Max(0,right-x),bottom-rawY),FortuneRawColor);
            _ui.Box(new(x,topY,Math.Max(0,right-x),rawY-topY),FortuneRobotColor);
            var a=new Vector2(left+width/2,topY);var b=new Vector2(left+width/2,rawY);
            if(previousTop is {} pt)Line(pt,a,Cream);if(previousRaw is {} pr)Line(pr,b,new Color(255,211,127));
            previousTop=a;previousRaw=b;left+=width;
        }
        _ui.Text("100%",new(18,696),.27f,Cream);_ui.Text("0%",new(28,855),.27f,Cream);
        _ui.Text("格納庫が少ない地点 → 多い地点",new(75,882),.33f,Cream);
        _ui.Text("線の下：原材料 / 線の間：ロボット / 上：空き",new(75,904),.29f,Cream);
        _ui.Button(FortuneRandomize,"分布ランダム変更",Accent,.43f);_ui.Button(FortuneUniform,"均等",_fortuneUniform?Accent:Muted,.43f);
        void Line(Vector2 a,Vector2 b,Color color){var d=b-a;if(d.LengthSquared()>0)_ui.Tile((a+b)/2,new(d.Length(),2),MathF.Atan2(d.Y,d.X),color);}
    }
}
