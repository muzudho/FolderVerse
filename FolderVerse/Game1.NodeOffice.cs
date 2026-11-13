namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _nodeOfficeOpen;
    private string NodeOfficeName=>_populationNode>=0 && _setup.Routes.Land(_setup.Nodes.All[_populationNode].Cell,_setup.Nodes.All[_populationNode].Center)?"地点庁舎":"地点監督船";
    private int _lastNodeTab;
    private static readonly Rectangle NodeOfficeTile=new(84,210,190,40);
    private void OpenNodeOffice(){_nodeOfficeOpen=true;_nodeAssemblyOpen=_nodeFactoryOpen=_nodeDisposalOpen=_nodeTransportOpen=false;_hangarDrag=null;_factoryDropDown=_factoryPlanLine=-1;_lastNodeTab=0;}
    private void RestoreNodeTab(){
        int tab=_lastNodeTab;OpenNodeOffice();
        if(tab==1)OpenNodeFactory();else if(tab==2)OpenNodeDisposal();else if(tab==3)OpenNodeAssembly();
        _lastNodeTab=tab;
    }
    private void DrawNodeTabs(){
        var areas=new[]{NodeOfficeTile,NodeFactoryTile,NodeDisposalTile,NodeAssemblyTile};
        string[] labels={NodeOfficeName,"ロボット製造工場","ロボット廃棄計画","ロボット格納庫"};
        bool[] active={_nodeOfficeOpen,_nodeFactoryOpen,_nodeDisposalOpen,_nodeAssemblyOpen};
        bool[] enabled={true,CanOpenNodeFactory,CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner),CanOpenHangar};
        for(int i=0;i<4;i++){
            var r=areas[i];var color=active[i] || enabled[i] && r.Contains(_pointer)?Accent:Muted;
            _ui.Box(r,color);_ui.Center(labels[i],r,.36f,enabled[i]?UiPainter.ContrastInk(color):Cream*.4f);
            if(active[i])_ui.Box(new(r.X,r.Y,r.Width,4),Cream);
        }
    }
    private static readonly (NodePurpose Purpose,string Label)[] NodePurposes={
        (NodePurpose.General,"自由用途型"),(NodePurpose.Harvest,"採取重点型"),(NodePurpose.Manufacturing,"製造重点型"),
        (NodePurpose.Relay,"輸送の中継地点"),(NodePurpose.Defense,"防衛特化拠点"),(NodePurpose.Frontline,"攻撃前線")
    };
    private static Rectangle NodePurposeButton(int index)=>new(84+index%3*261,539+index/3*35,255,32);
    private bool NodePurposeClick(Point pointer)
    {
        if(!_nodeOfficeOpen)return false;
        var node=_setup.Nodes.All[_populationNode];
        for(int i=0;i<NodePurposes.Length;i++){
            if(!NodePurposeButton(i).Contains(pointer))continue;
            var purpose=NodePurposes[i].Purpose;
            if(DomesticOperationsAllowed && _setup.Relations.Controls(node.Owner) && node.Functional &&
                node.Purpose!=NodePurpose.Relay && purpose!=NodePurpose.Relay)node.Purpose=purpose;
            return true;
        }
        return false;
    }
    private void DrawNodePurposes(Node node)
    {
        for(int i=0;i<NodePurposes.Length;i++){
            var (purpose,label)=NodePurposes[i];
            bool enabled=DomesticOperationsAllowed && _setup.Relations.Controls(node.Owner) && node.Functional &&
                node.Purpose!=NodePurpose.Relay && purpose!=NodePurpose.Relay;
            var area=NodePurposeButton(i);
            _ui.Button(area,node.Purpose==purpose?"● "+label:label,node.Purpose==purpose?Accent:enabled?Muted:Muted*.55f,.37f);
        }
        string hint=node.Purpose switch {
            NodePurpose.Harvest=>"採取 −10％ / 製造 ＋10％",
            NodePurpose.Manufacturing=>"製造 −10％ / 採取 ＋10％",
            NodePurpose.Relay=>"工場移転済み / 格納庫を使用可能 / 全機能移転のみ",
            NodePurpose.Defense=>"未予約の完全体に守備専用予約",
            NodePurpose.Frontline=>"未予約の完全体に迎撃予約",
            _=>"用途計画 / 中継地点は工場だけを移転すると設定されます"
        };
        _ui.Center(hint,new(84,610,784,26),.34f,Cream);
    }
    private void DrawNodeOffice(){
        _ui.Box(new(54,150,850,825),new Color(27,49,62));DrawNodeTabs();
        var node=_setup.Nodes.All[_populationNode];var image=new Rectangle(303,282,352,231);
        _ui.Box(new(image.X-6,image.Y-6,image.Width+12,image.Height+12),_setup.OwnerColor(node.Owner));DrawSupervisor(image,node.Owner,_populationNode);
        DrawNodePurposes(node);
        _ui.Center(NodeOfficeName+" / 地点監督",new(84,644,780,60),.8f,Cream);
        string owner=node.Owner<0?"未征服":_setup.ConquerorNames[node.Owner]+"の配下";
        _ui.Center(owner,new(84,718,780,45),.6f,Cream);
        if(_setup.Robots.MapleSupplyEnabled && node.Functional && _setup.Robots.MapleDeposit(node.Id)>0)
            _ui.Center($"楓の木 / 採取できる残り {_setup.Robots.MapleDeposit(node.Id)} 荷",new(84,754,780,24),.34f,Cream);
        _ui.Button(new(84,778,784,48),"放棄品置き場 / "+_setup.Salvage.Items.Count(i=>i.Node==node.Id)+" 件",Muted,.46f);
        var report=_setup.Campaign.SubordinateResults.LastOrDefault(r=>r.Node==node.Id && r.Controller==_setup.TurnPlayer);
        if(report!=null){
            _ui.Text($"手下の操作記録 / 全体ターン {report.Turn}",new(84,842),.34f,Cream);
            _ui.Text(report.Action,new(84,869),Math.Min(.32f,780/Math.Max(1,_font.MeasureString(report.Action).X)),Cream);
        }
        DrawFunctionTransferOffice();
    }
}
