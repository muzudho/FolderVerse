namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _manufacturingActive;
    private int _manufacturingNode=-1,_manufacturingReceived;
    private double _manufacturingAge,_manufacturingTickAge,_manufacturingDoneAge;
    private const double ManufacturingTickSeconds=.75;
    private HashSet<long> _manufacturingInitial=new();
    private ProductionIntake[] _manufacturingArrivals=Array.Empty<ProductionIntake>();

    private static int ProductParts(Robot robot)=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Count(p=>(robot.Parts&p)!=0);
    private int PickManufacturingNode()
    {
        var robots=_setup.Robots;
        var initial=robots.ProductionIntakes.Select(i=>i.Product.Id).Concat(robots.Nodes.SelectMany(s=>s.Robots).Select(r=>r.Id)).ToHashSet();
        var preview=AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(robots));
        preview.ConfigureFactories(_setup);
        for(int i=_setup.Industry.AppliedUnits;i<_setup.Industry.Result;i++)
            preview.AdvanceIndustry(n=>_setup.Nodes.All[n].Owner,_setup.Campaign.RobotBattles.LockedNode);
        var products=preview.ProductionIntakes.Where(i=>!initial.Contains(i.Product.Id)).Select(i=>(Node:i.Target??i.Source,Product:i.Product))
            .Concat(preview.Nodes.SelectMany((s,n)=>s.Robots.Where(r=>!initial.Contains(r.Id)).Select(r=>(Node:n,Product:r))));
        return products.GroupBy(p=>p.Node).OrderByDescending(g=>g.Sum(p=>ProductParts(p.Product))).ThenBy(g=>g.Key)
            .Select(g=>g.Key).FirstOrDefault(_setup.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && !_setup.Campaign.RobotBattles.LockedNode(n.Id))
                .OrderByDescending(n=>robots.Workshops[n.Id].ProductionLines.Count(l=>l.Manufacturing)).Select(n=>n.Id).FirstOrDefault(-1));
    }

    private void BeginManufacturing()
    {
        _manufacturingNode=PickManufacturingNode();
        _manufacturingActive=true;
        _manufacturingAge=_manufacturingTickAge=_manufacturingDoneAge=0;
        _manufacturingReceived=0;
        _manufacturingArrivals=Array.Empty<ProductionIntake>();
        _manufacturingInitial=ManufacturingStock().Select(r=>r.Id).ToHashSet();
    }

    private Robot[] ManufacturingStock()=>_manufacturingNode<0?Array.Empty<Robot>():
        _setup.Robots.Nodes[_manufacturingNode].Robots.Concat(_setup.Robots.ProductionIntakes
            .Where(i=>(i.Target??i.Source)==_manufacturingNode).Select(i=>i.Product)).DistinctBy(r=>r.Id).ToArray();

    private void UpdateManufacturing(double elapsed,bool proceed)
    {
        _manufacturingAge+=elapsed;
        if(_setup.Industry.Pending)
        {
            _manufacturingTickAge+=elapsed;
            if(_manufacturingTickAge>=ManufacturingTickSeconds)
            {
                _manufacturingTickAge=0;
                var before=_setup.Robots.ProductionIntakes.Select(i=>i.Product.Id).ToHashSet();
                _setup.Industry.Advance(_setup,IndustryTime.UnitsPerTurn);
                _manufacturingArrivals=_setup.Robots.ProductionIntakes.Where(i=>(i.Target??i.Source)==_manufacturingNode && !before.Contains(i.Product.Id)).ToArray();
                _manufacturingReceived=ManufacturingStock().Where(r=>!_manufacturingInitial.Contains(r.Id)).Sum(ProductParts);
            }
            return;
        }
        _manufacturingTickAge+=elapsed;
        _manufacturingDoneAge+=elapsed;
        if(proceed || _manufacturingDoneAge>=3)FinishIndustry();
    }

    private void DrawManufacturing()
    {
        _ui.Text("製造レーン稼働フェーズ",new(54,28),.9f,Cream);
        _ui.Center($"今回の産業ターン　{IndustryTime.Format(_setup.Industry.AppliedUnits)} / {IndustryTime.Format(_setup.Industry.Result)}",
            new(950,35,900,50),.6f,Cream);
        var left=new Rectangle(54,145,850,790);var right=new Rectangle(990,145,870,790);
        _ui.Box(left,new Color(24,45,59));_ui.Box(right,new Color(24,45,59));
        _ui.Center("ロボット製造工場",new(left.X,160,left.Width,48),.65f,Cream);
        _ui.Center("格納庫 / 入荷予約",new(right.X,160,right.Width,48),.65f,Cream);
        if(_manufacturingNode<0){_ui.Center("稼働できる工場がありません",new(54,420,1806,60),.6f,Cream);return;}
        var node=_setup.Nodes.All[_manufacturingNode];
        string destination=_setup.Nodes.DisplayName(node);
        _ui.Center(destination+" / 今回の納品が最も多い地点",new(54,92,1806,40),.48f,Cream);
        var lines=_setup.Robots.Workshops.SelectMany((w,n)=>w.ProductionLines
            .Where(l=>(l.IntakeTarget??n)==_manufacturingNode && _setup.Nodes.All[n].Functional)
            .Select(l=>(Source:n,Line:l))).ToArray();
        int pages=Math.Max(1,(lines.Length+3)/4);
        int page=(int)(_manufacturingAge/3)%pages;
        var latest=_manufacturingArrivals.LastOrDefault();
        if(latest!=null){int index=Array.FindIndex(lines,l=>ReferenceEquals(l.Line,latest.Line));if(index>=0)page=index/4;}
        foreach(var item in lines.Skip(page*4).Take(4).Select((value,index)=>(value,index)))
        {
            var (source,line)=item.value;int y=245+item.index*160;
            int owner=_setup.Nodes.All[source].Owner;
            _ui.Text(_setup.Nodes.DisplayName(_setup.Nodes.All[source]),new(80,y),.33f,Cream);
            DrawRobotGlyph(new(90,y+28,65,100),line.CurrentProductionPlan.Preview(Math.Max(0,owner)),whiteOutline:true);
            var bar=new Rectangle(184,y+50,645,34);
            _ui.Box(bar,Muted);
            float progress=line.RestRemaining>0?(float)(IndustryTime.Whole(line.RestTurns)-line.RestRemaining)/Math.Max(1,IndustryTime.Whole(line.RestTurns))
                :(float)line.ProductionAge/Math.Max(1,line.ProductionPeriod);
            var ink=line.Waiting?new Color(223,106,93):line.RestRemaining>0?new Color(234,203,98):Accent;
            _ui.Box(new(bar.X,bar.Y,(int)(bar.Width*progress),bar.Height),ink);
            string status=line.CycleFinished?"製造終了":!line.Manufacturing?"製造しない":line.Paused?"一時停止":
                line.RestRemaining>0?$"休暇 あと {IndustryTime.Format(line.RestRemaining)}":
                line.Waiting?"格納枠の空きを待っています":$"工期 {IndustryTime.Format(line.ProductionAge)} / {IndustryTime.Format(line.ProductionPeriod)}";
            _ui.Text(status,new(184,y+100),.4f,Cream);
        }
        _ui.Center($"製造レーン {page+1}/{pages}",new(54,895,850,30),.32f,Cream);
        var stock=ManufacturingStock();
        int stockPages=Math.Max(1,(stock.Length+11)/12);
        int stockPage=latest!=null?Math.Max(0,Array.FindIndex(stock,r=>r.Id==latest.Product.Id))/12:(int)(_manufacturingAge/3)%stockPages;
        for(int slot=0;slot<12;slot++)
        {
            var box=new Rectangle(1015+slot%4*207,235+slot/4*215,192,195);
            _ui.Box(box,new Color(12,29,42));
            var robot=stock.ElementAtOrDefault(stockPage*12+slot);
            if(robot==null)continue;
            bool fresh=!_manufacturingInitial.Contains(robot.Id);
            DrawRobotGlyph(new(box.X+55,box.Y+12,82,125),robot,whiteOutline:true);
            _ui.Center(fresh?"今回の入荷予約":_setup.Robots.ProductionIntakes.Any(i=>i.Product.Id==robot.Id)?"入荷予約":"格納済",
                new(box.X,box.Bottom-42,box.Width,30),.32f,fresh?Accent:Cream);
        }
        _ui.Center($"今回 { _manufacturingReceived} パーツ / 格納・入荷予約 {stock.Length} / {_setup.Robots.Nodes[node.Id].TotalCapacity} 枠　{stockPage+1}/{stockPages}",
            new(990,895,870,30),.34f,Cream);
        float flight=(float)Math.Clamp(_manufacturingTickAge/ManufacturingTickSeconds,0,1);
        if(flight<.95f)
        {
            foreach(var arrival in _manufacturingArrivals.TakeLast(4))
            {
                int lane=Array.FindIndex(lines,l=>ReferenceEquals(l.Line,arrival.Line));
                int slot=Array.FindIndex(stock,r=>r.Id==arrival.Product.Id);
                if(lane<0 || lane/4!=page || slot<0 || slot/12!=stockPage)continue;
                var from=new Vector2(820,305+lane%4*160);
                var to=new Vector2(1111+slot%4*207,315+slot%12/4*215);
                var at=Vector2.Lerp(from,to,MathHelper.SmoothStep(0,1,flight));
                DrawRobotGlyph(new((int)at.X-38,(int)at.Y-55,76,110),arrival.Product,whiteOutline:true);
            }
        }
        _ui.Center("できたパーツは入荷予約へ。次の配送で格納庫に入ります。",new(54,950,1120,60),.43f,Cream);
        if(!_setup.Industry.Pending && !ComputerExhibition)_ui.Button(IndustryContinue,"次の内政へ",Accent,.65f);
    }
}
