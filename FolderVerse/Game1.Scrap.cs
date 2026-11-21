namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _scrapPage;
    private static readonly Rectangle ScrapNext=new(660,735,190,36);
    private void DrawScrapArt(Rectangle r,float opacity=1)
    {
        int w=Math.Max(4,r.Width*3/5),h=Math.Max(4,Math.Min(r.Height/2,w/2));
        int x=r.Center.X-w/2,y=r.Center.Y-h/2;
        _ui.Box(new(x,y,w,h),new Color(92,113,128)*opacity);
        _ui.Box(new(x-3,y,w+6,Math.Max(2,h/6)),new Color(214,165,71)*opacity);
        for(int i=0;i<5;i++)_ui.Box(new(x+w*i/6,y-h/4*(1+i%2),Math.Max(2,w/7),Math.Max(2,h/2)),new Color(158+i*13,167+i*10,173+i*9)*opacity);
    }
    private bool ScrapDisposalClick(Point p)
    {
        var stock=_setup.Robots;int node=_populationNode;
        if(new Rectangle(84,735,270,36).Contains(p)){stock.Discarding.Orders.RemoveAll(o=>o.Node==node);return true;}
        var packages=stock.Nodes[node].MaplePackages.Where(p=>p.Material==RawMaterial.Scrap).ToArray();
        int pages=Math.Max(1,(packages.Length+7)/8);_scrapPage=Math.Clamp(_scrapPage,0,pages-1);
        if(ScrapNext.Contains(p)){_scrapPage=(_scrapPage+1)%pages;return true;}
        for(int i=0;i<8 && _scrapPage*8+i<packages.Length;i++)if(new Rectangle(84+i%4*190,552+i/4*72,178,60).Contains(p)){
            try{stock.Discarding.Reserve(_setup,node,_setup.Nodes.All[node].Owner,packages[_scrapPage*8+i].Id);_nodeDisposalMessage="廃棄を予約しました";}
            catch(InvalidOperationException e){_nodeDisposalMessage=e.Message;}return true;
        }
        return false;
    }
    private void DrawScrapDisposal()
    {
        var n=_setup.Nodes.All[_populationNode];var stock=_setup.Robots;var shop=stock.Workshops[n.Id];
        _ui.Box(new(54,150,850,825),new(27,49,62));DrawNodeTabs();
        _ui.Text(n.HasFinalDisposal?"最終処分場":"解体・廃棄",new(84,282),.64f,Cream);
        _ui.Text(n.HasFinalDisposal?"１全体ターンで格納庫に置いてあるスクラップを自動処分してくれるよ":"カゴへの廃棄は、１全体ターンにつき１格納庫分まで",new(84,324),.33f,Cream);
        int y=366;
        foreach(var line in shop.DisposalFacilities.Where(l=>l.DismantleOnly).Take(4)){
            string status=line.DisposalTarget is long id?$"ID {id} / {IndustryTime.Format(line.DisposalAge)} / {IndustryTime.Format(line.DisposalPeriod)}":"解体する荷を待っています";
            _ui.Text("解体レーン / "+status,new(84,y),.38f,Cream);y+=35;
        }
        if(y==366)_ui.Text("格納庫で解体作業員を予約してください",new(84,y),.42f,Cream);
        _ui.Text("スクラップ / クリックでカゴへ廃棄予約",new(84,518),.4f,Cream);
        var packages=stock.Nodes[n.Id].MaplePackages.Where(p=>p.Material==RawMaterial.Scrap).ToArray();
        int pages=Math.Max(1,(packages.Length+7)/8);_scrapPage=Math.Clamp(_scrapPage,0,pages-1);
        for(int i=0;i<8 && _scrapPage*8+i<packages.Length;i++){
            var p=packages[_scrapPage*8+i];var box=new Rectangle(84+i%4*190,552+i/4*72,178,60);
            _ui.Button(box,$"{p.Units}/9 格納庫"+(stock.Discarding.Reserved(p.Id)?" / 予約":""),stock.Discarding.Reserved(p.Id)?Accent:Muted,.34f);
        }
        _ui.Button(new(84,735,270,36),"この地点の廃棄予約を取消",Muted,.36f);
        _ui.Button(ScrapNext,$"次 / {_scrapPage+1}/{pages}",Muted,.36f);
        _ui.Text($"廃棄予約 {stock.Discarding.Orders.Where(o=>o.Node==n.Id).Sum(o=>o.Ninths)}/9 格納庫",new(84,784),.4f,Cream);
        _ui.Text(_nodeDisposalMessage,new(84,824),.35f,Cream);
        int orderY=862;
        foreach(var order in stock.Discarding.Orders.Where(o=>o.Node==n.Id && !o.Raw)){
            _ui.Text($"カゴへ / ID {order.Id} / {PartsLabel(order.Parts)}",new(84,orderY),.34f,Cream);orderY+=28;
        }
        DrawHangarAction(NodeSalvageButton,10,DomesticOperationsAllowed);
        _ui.Button(NodeRecoveryPlanButton,"回収隊の計画",Accent,.34f);
    }
}
