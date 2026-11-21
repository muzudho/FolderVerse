namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private WorldSetup _fortuneStockPreview;
    private int _fortuneStockSeed=-1,_fortuneDistributionSeed,_fortuneStockKind;
    private bool _fortunePieDragging,_fortuneUniform;
    private static readonly Vector2 FortunePieCenter=new(210,425);
    private static readonly Rectangle FortuneDistribution=new(75,630,520,244);
    private static readonly Rectangle FortuneRandomize=new(75,947,320,48),FortuneUniform=new(405,947,190,48);
    private static Rectangle FortuneStockKey(int i)=>new(350,320+i*54,245,48);
    private static readonly Rectangle FortuneRandomShare=new(75,565,520,34);
    private static readonly Color FortuneRobotColor=new(67,169,140),FortuneRawColor=new(218,169,77),FortuneEmptyColor=new(53,74,91),FortuneFixedColor=new(36,104,103);

    private void EnsureFortuneStockPreview()
    {
        if(_fortuneStockPreview!=null)return;
        if(_fortuneStockSeed<0)_fortuneStockSeed=_random.Next();
        _fortuneDistributionSeed=_fortuneStockSeed^0x494E4954;
        _fortuneStockPreview=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(_setup));
        _fortuneStockPreview.CompleteLand(_fortuneStockSeed,0,0,new(0,0,_fortuneDistributionSeed));
        double fixedPercent=100d*_fortuneStockPreview.InitialStock.FixedRobotUnits/Math.Max(1,_fortuneStockPreview.InitialStock.CapacityUnits);
        double additional=Math.Min(12.5,(100-fixedPercent)/2);
        _robotScatterPercent=(int)Math.Round(fixedPercent+additional);
        _rawStockPercent=Math.Min((int)Math.Round(additional),100-_robotScatterPercent);
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
            for(int kind=0;kind<2;kind++)if(FortuneHandle(kind).Contains(_pointer)){_fortuneStockKind=kind;_fortunePieDragging=true;break;}
            if(FortuneRandomShare.Contains(_pointer)){
                var stock=_fortuneStockPreview.InitialStock;
                int minimum=(int)Math.Ceiling(100d*stock.FixedRobotUnits/Math.Max(1,stock.CapacityUnits));
                int available=100-minimum,a=_random.Next(available+1),b=_random.Next(available+1);
                _robotScatterPercent=minimum+Math.Min(a,b);_rawStockPercent=Math.Abs(a-b);RebuildFortuneStock();
            }
            if(FortuneRandomize.Contains(_pointer)){_fortuneUniform=false;_fortuneDistributionSeed=_random.Next();RebuildFortuneStock();}
            if(FortuneUniform.Contains(_pointer)){_fortuneUniform=true;RebuildFortuneStock();}
        }
        if(_fortunePieDragging){
            var delta=_pointer.ToVector2()-FortunePieCenter;
            if(delta.Length()>12){
                double share=(Math.Atan2(delta.X,-delta.Y)+Math.PI*2)%(Math.PI*2)/Math.PI/2*100;
                var stock=_fortuneStockPreview.InitialStock;
                double robot=100d*stock.RobotUnits/Math.Max(1,stock.CapacityUnits);
                double boundary=_fortuneStockKind==0?robot:100d*(stock.RobotUnits+stock.RawUnits)/Math.Max(1,stock.CapacityUnits);
                if(boundary>75 && share<25)share=100;
                else if(boundary<25 && share>75)share=0;
                SetFortuneStockPercent((int)Math.Round(_fortuneStockKind==0?share:_fortuneStockKind==1?Math.Max(0,share-robot):100-share));
            }
        }
    }
    private Rectangle FortuneHandle(int kind)
    {
        var s=_fortuneStockPreview.InitialStock;float total=Math.Max(1,s.CapacityUnits);
        float angle=(kind==0?s.RobotUnits:s.RobotUnits+s.RawUnits)/total*MathHelper.TwoPi;
        var p=FortunePieCenter+new Vector2(MathF.Sin(angle),-MathF.Cos(angle))*(kind==0?88:126);
        return new((int)p.X-11,(int)p.Y-11,22,22);
    }
    private void DrawFortuneStockScenery(Rectangle globeArea)
    {
        if(!FortuneSettingsAvailable)return;
        EnsureFortuneStockPreview();var preview=_fortuneStockPreview;var stock=preview.InitialStock;
        foreach(var site in stock.Sites){
            var node=preview.Nodes.All[site.Node];var cell=preview.Cells[node.Cell];
            var position=preview.Routes.Position(node.Cell,node.Center);
            if(!_world.ProjectVisible(position,cell.Normal,_yaw,_pitch,globeArea,out var p))continue;
            var worker=preview.Robots.Nodes[site.Node].Robots.FirstOrDefault(r=>r.WarehouseKeeper || r.FactoryWorker);
            if(worker!=null)DrawRobotGlyph(new((int)p.X-10,(int)p.Y-30,20,30),worker);
        }
        float total=Math.Max(1,stock.CapacityUnits);
        int robots=(int)(90*(stock.RobotUnits-stock.FixedRobotUnits)/total),raw=(int)(90*stock.RawUnits/total);
        var samples=preview.Robots.Nodes.SelectMany(n=>n.Robots).Where(r=>!r.WarehouseKeeper && !r.FactoryWorker).ToArray();
        var packages=preview.Robots.Nodes.SelectMany(n=>n.MaplePackages).ToArray();
        for(int i=0;i<robots+raw;i++){
            float t=(float)((_landAge*.3+i*.071)%1),x=MathHelper.Lerp(650+i*173%1100,1080+i*47%310,t),y=MathHelper.Lerp(-100,460+i*31%200,t);
            var bounds=new Rectangle((int)x,(int)y,30,48);
            if(i<robots && samples.Length>0)DrawRobotGlyph(bounds,samples[i%samples.Length],opacity:1-t*.3f);
            else if(i>=robots && packages.Length>0)DrawMaplePackageArt(bounds,1,1-t*.3f,material:packages[(i-robots)%packages.Length].Material);
        }
    }
    private void DrawFortuneStock()
    {
        EnsureFortuneStockPreview();var stock=_fortuneStockPreview.InitialStock;float total=Math.Max(1,stock.CapacityUnits);
        float robot=stock.RobotUnits/total,raw=stock.RawUnits/total,fixedShare=stock.FixedRobotUnits/total;
        _ui.Text("格納庫の初期配置 / ポケットを除く",new(75,280),.5f,Cream);
        DrawSharedRatePie(FortunePieCenter,108,new[]{(fixedShare*360,FortuneFixedColor),((robot-fixedShare)*360,FortuneRobotColor),(raw*360,FortuneRawColor),((1-robot-raw)*360,FortuneEmptyColor)});
        var names=new[]{"固定ロボット","追加ロボット","原材料","空き"};var colors=new[]{FortuneFixedColor,FortuneRobotColor,FortuneRawColor,FortuneEmptyColor};var shares=new[]{fixedShare,robot-fixedShare,raw,1-robot-raw};
        for(int i=0;i<4;i++){
            var key=FortuneStockKey(i);_ui.Center($"{names[i]}  {shares[i]*100:0.#}%",key,.43f,Cream);
            _ui.Box(new(key.X+7,key.Y+9,7,key.Height-18),colors[i]);
        }
        for(int kind=0;kind<2;kind++){
            var color=kind==0?FortuneRobotColor:FortuneRawColor;float radius=kind==0?88:126;
            for(int segment=0;segment<96;segment++){
                float a=segment*MathHelper.TwoPi/96,b=(segment+1)*MathHelper.TwoPi/96;
                Line(FortunePieCenter+new Vector2(MathF.Sin(a),-MathF.Cos(a))*radius,FortunePieCenter+new Vector2(MathF.Sin(b),-MathF.Cos(b))*radius,color*.5f);
            }
            var handle=FortuneHandle(kind);_ui.Box(handle,Cream);var inner=handle;inner.Inflate(-3,-3);_ui.Box(inner,color);
            float percent=(kind==0?robot-fixedShare:raw)*100;
            _ui.Center($"{percent:0.#}%",new(handle.Center.X-35,handle.Bottom+21>545?handle.Y-20:handle.Bottom+3,70,18),.30f,Cream);
        }
        _ui.Text("２つの四角をドラッグして割合を変更",new(75,550),.30f,Cream);
        _ui.Button(FortuneRandomShare,"割合ランダム変更",Accent,.43f);
        _ui.Text($"全 {stock.CapacityUnits/3} 格納庫 / 1/3枠ずつ配分",new(75,606),.37f,Cream);
        // Scale to the largest site limit in this world; unavailable capacity stays transparent.
        var sites=stock.Sites.OrderBy(s=>_fortuneStockPreview.Robots.Nodes[s.Node].WarehouseLimit??s.CapacityUnits/3).ThenBy(s=>s.Node).ToArray();
        float maximum=Math.Max(1,sites.Select(s=>Math.Max(s.CapacityUnits/3f,_fortuneStockPreview.Robots.Nodes[s.Node].WarehouseLimit??s.CapacityUnits/3f)).DefaultIfEmpty(1).Max());
        int Y(float bays)=>FortuneDistribution.Bottom-(int)Math.Round(Math.Clamp(bays,0,maximum)/maximum*FortuneDistribution.Height);
        for(int i=0;i<sites.Length;i++){
            var site=sites[i];
            int x=FortuneDistribution.X+i*FortuneDistribution.Width/sites.Length;
            int right=FortuneDistribution.X+(i+1)*FortuneDistribution.Width/sites.Length;
            float capacity=site.CapacityUnits/3f;
            float limit=Math.Max(capacity,_fortuneStockPreview.Robots.Nodes[site.Node].WarehouseLimit??capacity);
            float fixedBays=site.FixedRobotUnits/3f,robots=(site.RobotUnits-site.FixedRobotUnits)/3f,rawBays=site.RawUnits/3f;
            void Band(float low,float high,Color color){int top=Y(high),bottom=Y(low);if(right>x && bottom>top)_ui.Box(new(x,top,right-x,bottom-top),color);}
            Band(0,fixedBays,FortuneFixedColor);
            Band(fixedBays,fixedBays+robots,FortuneRobotColor);
            Band(fixedBays+robots,fixedBays+robots+rawBays,FortuneRawColor);
            Band(fixedBays+robots+rawBays,capacity,FortuneEmptyColor);
            Band(capacity,limit,new Color(106,90,205));
            if(right>x){Line(new(x,Y(capacity)),new(right,Y(capacity)),Cream*.7f);Line(new(x,Y(limit)),new(right,Y(limit)),Cream);}
        }
        Line(new(FortuneDistribution.Left,FortuneDistribution.Top),new(FortuneDistribution.Right,FortuneDistribution.Top),Cream*.35f);
        _ui.Text($"{maximum:0}",new(28,FortuneDistribution.Top-8),.27f,Cream);_ui.Text("0",new(28,FortuneDistribution.Bottom-19),.27f,Cream);
        _ui.Text("地点の上限が少ない → 多い / 高さ：格納庫数",new(75,882),.29f,Cream);
        _ui.Text("下から：固定 / 追加ロボット / 原材料 / 空き",new(75,904),.26f,Cream);
        _ui.Text("スレートブルー：未開放 / 透明：地点の上限外",new(75,924),.26f,Cream);
        _ui.Button(FortuneRandomize,"分布ランダム変更",Accent,.43f);_ui.Button(FortuneUniform,"均等",_fortuneUniform?Accent:Muted,.43f);
        if(new Rectangle(60,270,550,785).Contains(_pointer))DrawRulebookHint("初期配置の設定","分からなかったら、そのまま確定して進んでください");
        void Line(Vector2 a,Vector2 b,Color color){var d=b-a;if(d.LengthSquared()>0)_ui.Tile((a+b)/2,new(d.Length(),2),MathF.Atan2(d.Y,d.X),color);}
    }
}
