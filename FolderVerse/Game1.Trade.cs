namespace FolderVerse;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _tradePlannerOpen;
    private readonly int[] _tradePages=new int[4];
    private TradeEndpoint? _tradeFrom,_tradeTo;
    private TradeCargo? _tradeGive,_tradeWant;
    private string _tradeMessage="";
    private readonly List<string> _tradeReplies=new();
    private int _tradeReplyPage;
    private const int TradeRows=11;
    private static readonly Rectangle TradeBack=new(1550,36,305,58),TradeSubmit=new(1430,950,420,70);
    private static readonly Rectangle TradeCancel=new(54,950,440,70),DiplomacyReject=new(1000,950,350,70);
    private static Rectangle TradeRow(int column,int row)=>new(54+column*455,218+row*55,434,49);
    private static Rectangle TradePager(int column,bool next)=>new(54+column*455+(next?336:0),840,98,44);
    private TradePlan Trade=>_setup.Campaign.Trade;

    private void OpenTradePlanner()
    {
        ResetInspector();
        _tradePlannerOpen=true;
        _tradeFrom=_tradeTo=null;
        _tradeGive=_tradeWant=null;
        Array.Clear(_tradePages);
        _tradeMessage="";
    }

    private TradeEndpoint[] TradeSources()
    {
        int ruler=_setup.PlayerSlot;
        var nodes=_setup.Nodes.All.Where(node=>node.Owner==ruler)
            .Select(node=>new TradeEndpoint(ruler,node.Id,false));
        var current=_setup.Nodes.Current(ruler);
        if(current!=null)nodes=nodes.Prepend(new(ruler,current.Id,true));
        return nodes.Where(source=>Trade.Cargo(_setup,source).Any() && Trade.Partners(_setup,source).Any()).ToArray();
    }

    private string TradeEndpointLabel(TradeEndpoint end)=>
        _setup.ConquerorNames[end.Ruler]+" / "+(end.Pocket?"ポケット":_setup.Nodes.DisplayName(_setup.Nodes.All[end.Node]));

    private string TradeCargoLabel(TradeEndpoint endpoint,TradeCargo cargo)
    {
        var robot=Trade.Robot(_setup,endpoint,cargo);
        if(robot!=null)return $"ID {cargo.Id} / "+SalvagePartLabel(robot);
        var package=Trade.Package(_setup,endpoint,cargo);
        return package==null?$"ID {cargo.Id}":$"{RobotWorld.RawName(package.Material)} {package.Units}/{MaplePackage.Capacity}";
    }

    private void TradePlanner(bool draw,bool click=false)
    {
        var sources=TradeSources();
        var partners=_tradeFrom is {} from?Trade.Partners(_setup,from).ToArray():Array.Empty<TradeEndpoint>();
        var give=_tradeFrom is {} left?Trade.Cargo(_setup,left).ToArray():Array.Empty<TradeCargo>();
        var want=_tradeTo is {} right?Trade.Cargo(_setup,right).ToArray():Array.Empty<TradeCargo>();
        var mine=Trade.Offers.Where(offer=>offer.From.Ruler==_setup.PlayerSlot).ToArray();
        bool ready=_tradeFrom.HasValue && _tradeTo.HasValue && _tradeGive.HasValue && _tradeWant.HasValue;
        if(draw)
        {
            _ui.Box(new(0,0,1920,1080),new Color(16,35,47));
            _ui.Text("取引",new(54,34),.94f,Cream);
            _ui.Text("荷を１つずつ選びます。返事は次の全体ターンの外交フェーズで届きます。",new(54,115),.47f,Cream);
            _ui.Button(TradeBack,"閉じる",Muted,.49f);
            _ui.Button(TradeSubmit,"取引を申し込む",ready?Accent:Muted,.51f);
            if(mine.Length>0)_ui.Button(TradeCancel,"最後の取引予約を取り消す",Muted,.42f);
            SalvageText($"取引予約 {mine.Length} 件   {_tradeMessage}",new(54,894,1796,42),.44f);
        }
        if(click && TradeBack.Contains(_pointer)){_tradePlannerOpen=false;return;}
        if(click && TradeCancel.Contains(_pointer) && mine.Length>0)
        {
            Trade.Offers.Remove(mine[^1]);
            _tradeMessage="取引予約を取り消しました";
            _lastAutoSaveTurn=-1;AutoSaveDomestic();return;
        }
        if(click && ready && TradeSubmit.Contains(_pointer))
        {
            bool booked=Trade.Book(_setup,_tradeFrom.Value,_tradeTo.Value,_tradeGive.Value,_tradeWant.Value);
            _tradeMessage=booked?"取引を予約しました":"その荷は予約できません";
            if(booked){_tradeGive=_tradeWant=null;_lastAutoSaveTurn=-1;AutoSaveDomestic();}
            return;
        }
        List(0,"こちらの地点・ポケット",sources,_tradeFrom,TradeEndpointLabel,value=>
        {
            _tradeFrom=value;_tradeTo=null;_tradeGive=_tradeWant=null;
            Array.Clear(_tradePages,1,3);
        });
        List(1,"交渉相手",partners,_tradeTo,TradeEndpointLabel,value=>
        {
            _tradeTo=value;_tradeWant=null;_tradePages[3]=0;
        });
        List(2,"こちらが渡す荷",give,_tradeGive,value=>TradeCargoLabel(_tradeFrom.Value,value),value=>_tradeGive=value);
        List(3,"相手からもらう荷",want,_tradeWant,value=>TradeCargoLabel(_tradeTo.Value,value),value=>_tradeWant=value);

        void List<T>(int column,string title,T[] entries,T? selected,Func<T,string> label,Action<T> choose) where T:struct
        {
            int pages=Math.Max(1,(entries.Length+TradeRows-1)/TradeRows);
            int page=_tradePages[column]=Math.Min(_tradePages[column],pages-1);
            if(draw)
            {
                var heading=TradeRow(column,0);heading.Y=165;
                SalvageText(title,heading,.52f);
                if(entries.Length==0)SalvageText("選べる項目がありません",TradeRow(column,0),.38f);
                if(pages>1)
                {
                    _ui.Button(TradePager(column,false),"←",Muted,.45f);
                    _ui.Button(TradePager(column,true),"→",Muted,.45f);
                    SalvageText($"{page+1} / {pages}",new(154+455*column,840,234,44),.4f);
                }
            }
            if(click && TradePager(column,false).Contains(_pointer))_tradePages[column]=(page+pages-1)%pages;
            if(click && TradePager(column,true).Contains(_pointer))_tradePages[column]=(page+1)%pages;
            for(int i=0;i<TradeRows && page*TradeRows+i<entries.Length;i++)
            {
                var entry=entries[page*TradeRows+i];
                var row=TradeRow(column,i);
                if(draw)
                {
                    _ui.Box(row,selected.HasValue && EqualityComparer<T>.Default.Equals(entry,selected.Value)?new Color(44,101,112):Muted);
                    SalvageText(label(entry),row,.39f);
                }
                if(click && row.Contains(_pointer))choose(entry);
            }
        }
    }

    private TradeOffer DueTrade=>Trade.Offers.FirstOrDefault(offer=>offer.Turn<_setup.Population.Turn);

    private bool BeginDiplomacy()
    {
        if(DueTrade==null || _setup.Industry.Pending)return false;
        _tradeReplies.Clear();_tradeReplyPage=0;
        _screen=Screen.Diplomacy;
        ResolveAutomaticTradeReplies();
        if(_autoPlay || _demoActive){_tradeReplies.Clear();return false;}
        return true;
    }

    private void ResolveAutomaticTradeReplies()
    {
        while(DueTrade is {} offer)
        {
            bool possible=Trade.Possible(_setup,offer);
            if(possible && !_autoPlay && !_demoActive && _setup.ControllerOf(offer.To.Ruler)>=0 &&
                _setup.ControllerOf(offer.From.Ruler)!=_setup.ControllerOf(offer.To.Ruler))break;
            RecordTradeReply(offer,possible && (_setup.ControllerOf(offer.From.Ruler)>=0 &&
                _setup.ControllerOf(offer.From.Ruler)==_setup.ControllerOf(offer.To.Ruler) || Trade.ComputerAccepts(_setup,offer)));
        }
    }

    private void RecordTradeReply(TradeOffer offer,bool accept)
    {
        string detail=TradeEndpointLabel(offer.From)+" → "+TradeEndpointLabel(offer.To)+" / "+
            TradeCargoLabel(offer.From,offer.Give)+" / "+TradeCargoLabel(offer.To,offer.Want);
        string reply=Trade.Answer(_setup,offer,accept);
        _tradeReplies.Add(detail+" / "+reply);
    }

    private void DiplomacyInput(bool click,bool enter)
    {
        if(DueTrade is {} offer)
        {
            if(!click || (!TradeSubmit.Contains(_pointer) && !DiplomacyReject.Contains(_pointer)))return;
            RecordTradeReply(offer,TradeSubmit.Contains(_pointer));
            ResolveAutomaticTradeReplies();
            return;
        }
        if(click && TradePager(0,false).Contains(_pointer))_tradeReplyPage=Math.Max(0,_tradeReplyPage-1);
        if(click && TradePager(0,true).Contains(_pointer))_tradeReplyPage=Math.Min(_tradeReplyPage+1,Math.Max(0,(_tradeReplies.Count-1)/TradeRows));
        if(!enter && !(click && TradeSubmit.Contains(_pointer)))return;
        _tradeReplies.Clear();
        EnterDomesticAfterIndustry();
    }

    private void DrawDiplomacy()
    {
        _ui.Box(new(0,0,1920,1080),new Color(16,35,47));
        _ui.Text("外交フェーズ",new(54,34),.94f,Cream);
        if(DueTrade is {} offer)
        {
            SalvageText("交渉相手  "+TradeEndpointLabel(offer.To),new(54,158,1796,70),.63f);
            SalvageText("申し込み元  "+TradeEndpointLabel(offer.From),new(54,251,1796,70),.56f);
            SalvageText("相手からもらう荷  "+TradeCargoLabel(offer.From,offer.Give),new(54,399,1796,82),.57f);
            SalvageText("こちらが渡す荷  "+TradeCargoLabel(offer.To,offer.Want),new(54,510,1796,82),.57f);
            _ui.Button(DiplomacyReject,"辞退する",Muted,.52f);
            _ui.Button(TradeSubmit,"取引に応じる",Accent,.52f);
            return;
        }
        var replies=_tradeReplies.Skip(_tradeReplyPage*TradeRows).Take(TradeRows).ToArray();
        for(int i=0;i<replies.Length;i++)
            SalvageText(replies[i],new(54,166+i*58,1796,54),.47f);
        if(_tradeReplies.Count>TradeRows)
        {
            _ui.Button(TradePager(0,false),"←",Muted,.45f);
            _ui.Button(TradePager(0,true),"→",Muted,.45f);
        }
        _ui.Button(TradeSubmit,"内政フェーズへ",Accent,.52f);
    }
}
