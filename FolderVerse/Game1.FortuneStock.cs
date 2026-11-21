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
    private static readonly Rectangle FortuneDistribution=new(75,704,520,170);
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
            for(int i=1;i<4;i++)if(FortuneStockKey(i).Contains(_pointer))_fortuneStockKind=i-1;
            if(FortuneHandle(_fortuneStockKind).Contains(_pointer))_fortunePieDragging=true;
            if(FortuneRandomShare.Contains(_pointer)){
                var stock=_fortuneStockPreview.InitialStock;
                int minimum=(int)Math.Ceiling(100d*stock.FixedRobotUnits/Math.Max(1,stock.CapacityUnits));
                int available=100-minimum,a=_random.Next(available+1),b=_random.Next(available+1);
                _robotScatterPercent=minimum+Math.Min(a,b);_rawStockPercent=Math.Abs(a-b);RebuildFortuneStock();
            }
            for(int i=0;i<5;i++)if(new Rectangle(75+i*104,610,92,48).Contains(_pointer)){
                var stock=_fortuneStockPreview.InitialStock;
                int fixedPercent=(int)Math.Ceiling(100d*stock.FixedRobotUnits/Math.Max(1,stock.CapacityUnits));
                SetFortuneStockPercent(_fortuneStockKind==0?Math.Min(100,fixedPercent+i*25):i*25);
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
        var p=FortunePieCenter+new Vector2(MathF.Sin(angle),-MathF.Cos(angle))*108;
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
            var key=FortuneStockKey(i);_ui.Button(key,$"{names[i]}  {shares[i]*100:0.#}%",i>0 && _fortuneStockKind==i-1?Accent:Muted,.43f);
            _ui.Box(new(key.X+7,key.Y+9,7,key.Height-18),colors[i]);
        }
        var handle=FortuneHandle(_fortuneStockKind);_ui.Box(handle,Cream);_ui.Box(new(handle.X+4,handle.Y+4,14,14),Accent);
        _ui.Text("成分を選び、四角をドラッグ",new(75,540),.32f,Cream);
        _ui.Button(FortuneRandomShare,"割合ランダム変更",Accent,.43f);
        for(int i=0;i<5;i++){
            var b=new Rectangle(75+i*104,610,92,48);_ui.Button(b,$"{i*25}%",Muted,.38f);
            _spriteBatch.Draw(RatePieIcon(i*90,4),new Rectangle(b.X+2,b.Y+13,22,22),Color.White);
        }
        _ui.Text($"全 {stock.CapacityUnits/3} 格納庫 / 1/3枠ずつ配分",new(75,674),.37f,Cream);
        // Every site uses the same 0..60 scale; undeveloped capacity is not usable empty space.
        var sites=stock.Sites.OrderBy(s=>_fortuneStockPreview.Robots.Nodes[s.Node].WarehouseLimit??s.CapacityUnits/3).ThenBy(s=>s.Node).ToArray();
        int Y(float bays)=>FortuneDistribution.Bottom-(int)Math.Round(Math.Clamp(bays,0,60)/60*FortuneDistribution.Height);
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
            Band(capacity,limit,FortuneEmptyColor);
            int hatchTop=Y(limit),hatchBottom=Y(capacity),height=hatchBottom-hatchTop;
            for(int start=x-height;start<right;start+=8){
                int from=Math.Max(0,x-start),to=Math.Min(height,right-start);
                if(to>from)Line(new(start+from,hatchBottom-from),new(start+to,hatchBottom-to),Cream*.35f);
            }
            if(right>x){Line(new(x,Y(capacity)),new(right,Y(capacity)),Cream*.7f);Line(new(x,Y(limit)),new(right,Y(limit)),Cream);}
        }
        Line(new(FortuneDistribution.Left,FortuneDistribution.Top),new(FortuneDistribution.Right,FortuneDistribution.Top),Cream*.35f);
        _ui.Text("60",new(28,696),.27f,Cream);_ui.Text("0",new(28,855),.27f,Cream);
        _ui.Text("地点の上限が少ない → 多い / 高さ：格納庫数",new(75,882),.29f,Cream);
        _ui.Text("下から：固定 / 追加ロボット / 原材料 / 空き",new(75,904),.26f,Cream);
        _ui.Text("グレーの斜線：未開放 / 透明：地点の上限外",new(75,924),.26f,Cream);
        _ui.Button(FortuneRandomize,"分布ランダム変更",Accent,.43f);_ui.Button(FortuneUniform,"均等",_fortuneUniform?Accent:Muted,.43f);
        if(new Rectangle(60,270,550,785).Contains(_pointer))DrawRulebookHint("初期配置の設定","分からなかったら、そのまま確定して進んでください");
        void Line(Vector2 a,Vector2 b,Color color){var d=b-a;if(d.LengthSquared()>0)_ui.Tile((a+b)/2,new(d.Length(),2),MathF.Atan2(d.Y,d.X),color);}
    }
}
