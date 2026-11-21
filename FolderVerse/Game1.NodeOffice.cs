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
        string[] labels={NodeOfficeName,"ロボット製造工場",NodeDisposalName,"ロボット格納庫"};
        bool[] active={_nodeOfficeOpen,_nodeFactoryOpen,_nodeDisposalOpen,_nodeAssemblyOpen};
        bool[] enabled={true,CanOpenNodeFactory,InspectableNode(_populationNode),CanOpenHangar};
        for(int i=0;i<4;i++){
            var r=areas[i];var color=active[i] || enabled[i] && r.Contains(_pointer)?Accent:Muted;
            _ui.Box(r,color);_ui.Center(labels[i],r,Math.Min(.36f,(r.Width-12)/Math.Max(1,_font.MeasureString(labels[i]).X)),enabled[i]?UiPainter.ContrastInk(color):Cream*.4f);
            if(active[i])_ui.Box(new(r.X,r.Y,r.Width,4),Cream);
        }
    }
    private static readonly (NodePurpose? Purpose,string Label)[] NodePurposes={
        (NodePurpose.General,"自由用途型"),(NodePurpose.Harvest,"採取重点型"),(NodePurpose.Manufacturing,"製造重点型"),
        (NodePurpose.Relay,"輸送の中継地点"),(NodePurpose.Defense,"城壁"),(NodePurpose.Frontline,"詰所"),(null,"港"),(NodePurpose.FinalDisposal,"最終処分場")
    };
    private static Rectangle NodePurposeButton(int index)=>new(84+index*98,531,92,72);
    private string NodePurposeDisabledReason(Node node,NodePurpose? purpose)
    {
        if(purpose==NodePurpose.FinalDisposal)return node.HasFinalDisposal?"最終処分場の用途は変更できません":"この地点に最終処分場はありません";if(purpose==null)return node.IsHarbor?"港は陸と海を結ぶ地点です":"この地点は港ではありません";
        if(node.PermanentLandmark)return "ランドマークの用途は変更できません";if(!node.Functional)return "無機能地点では用途を変更できません";
        if(node.Purpose==NodePurpose.Relay)return _setup.RelayLogisticsEnabled?"管理人１体で７枠追加。中継同士の全施設移転で上限を足せます（最大60）。工場は受け入れません。":"中継地点では全施設移転のみできます";
        if(purpose==NodePurpose.Relay)return "工場だけを移転すると中継地点になります";
        if(purpose==NodePurpose.Harvest && !RobotWorld.RawMaterials.Any(m=>_setup.Robots.RawDeposit(node.Id,m)>0))
            return "この地域では何も採れません";
        if(!DomesticOperationsAllowed)return "用途を変更できるのは内政中です";
        if(!_setup.Relations.Controls(node.Owner))return "この地点の用途を変更する権限がありません";
        return null;
    }
    private bool NodePurposeClick(Point pointer)
    {
        if(!_nodeOfficeOpen)return false;
        var node=_setup.Nodes.All[_populationNode];
        for(int i=0;i<NodePurposes.Length;i++){
            if(!NodePurposeButton(i).Contains(pointer))continue;
            var purpose=NodePurposes[i].Purpose;
            if(purpose is NodePurpose value && NodePurposeDisabledReason(node,purpose)==null)node.Purpose=value;
            return true;
        }
        return false;
    }
    private void DrawNodePurposes(Node node)
    {
        for(int i=0;i<NodePurposes.Length;i++){
            var (purpose,label)=NodePurposes[i];
            bool enabled=NodePurposeDisabledReason(node,purpose)==null;
            bool selected=purpose==null?node.IsHarbor:!node.IsHarbor && node.Purpose==purpose;
            var area=NodePurposeButton(i);
            var color=enabled?(selected || area.Contains(_pointer)?Accent:Muted):Muted*.48f;
            _ui.Button(area,"",color,.3f);
            if(selected)_ui.Box(new(area.X,area.Y,area.Width,3),enabled?Cream:Cream*.55f);
            CubeNet.DrawPurposeIcon(_ui,purpose,new(area.Center.X,area.Y+31),node.Terrain==TravelTerrain.Sea,enabled || selected);
            _ui.Center(label,new(area.X,area.Bottom-24,area.Width,24),.28f,enabled?UiPainter.ContrastInk(color):Cream*.55f);
        }
    }
    private void DrawNodeOffice(){
        _ui.Box(new(54,150,850,825),new Color(27,49,62));DrawNodeTabs();
        var node=_setup.Nodes.All[_populationNode];var image=new Rectangle(303,282,352,231);
        _ui.Box(new(image.X-6,image.Y-6,image.Width+12,image.Height+12),_setup.OwnerColor(node.Owner));DrawSupervisor(image,node.Owner,_populationNode);
        DrawNodePurposes(node);
        _ui.Center(NodeOfficeName+" / 地点監督",new(84,644,780,60),.8f,Cream);
        string owner=node.Owner<0?"未征服":_setup.ConquerorNames[node.Owner]+"の配下";
        _ui.Center(owner,new(84,718,780,45),.6f,Cream);
        if(_setup.Robots.MapleSupplyEnabled && node.Functional && node.Purpose!=NodePurpose.Relay && _setup.Robots.MapleDeposit(node.Id)>0)
            _ui.Center($"楓の木 / 採取できる残り {_setup.Robots.MapleDeposit(node.Id)} 荷",new(84,754,780,24),.34f,Cream);
        var report=_setup.Campaign.SubordinateResults.LastOrDefault(r=>r.Node==node.Id && r.Controller==_setup.TurnPlayer);
        if(report!=null){
            _ui.Text($"手下の操作記録 / 全体ターン {report.Turn}",new(84,842),.34f,Cream);
            _ui.Text(report.Action,new(84,869),Math.Min(.32f,780/Math.Max(1,_font.MeasureString(report.Action).X)),Cream);
        }
        DrawFunctionTransferOffice();
        for(int i=0;i<NodePurposes.Length;i++){
            if(!NodePurposeButton(i).Contains(_pointer))continue;
            var (purpose,label)=NodePurposes[i];
            string[] hints={
                "メリットもデメリットも無いよ。",
                "原材料の採取が１０％速くなる代わりに、ロボット製造が１０％遅くなるよ。",
                "ロボット製造が１０％速くなる代わりに、原材料の採取が１０％遅くなるよ。",
                "格納庫が多くなるけど、工場の機能は永久に無くなるよ。工場だけ施設移転するとこれになるよ。",
                "守りやすい戦場になるよ。",
                "だいたい２地点先ぐらいまで進撃や迎撃、救援ができるようになるよ。",
                "陸と海の境目はこれだよ。メリットもデメリットも無いよ。","１全体ターンで格納庫に置いてあるスクラップを自動処分してくれるよ"
            };
            var reason=NodePurposeDisabledReason(node,purpose);
            var lines=new System.Collections.Generic.List<string>();
            void Wrap(string text){
                string line="";
                foreach(char c in text){
                    if(c=='\n'){lines.Add(line);line="";continue;}
                    if(_font.MeasureString(line+c).X*.38f>730){lines.Add(line);line="";}
                    line+=c;
                }
                if(line.Length>0)lines.Add(line);
            }
            Wrap(hints[i]);
            if(reason!=null && reason!="工場だけを移転すると中継地点になります" && reason!="港は陸と海を結ぶ地点です")Wrap(reason);
            var popup=new Rectangle(74,611,804,Math.Max(130,64+lines.Count*30));
            DrawRulebookPaper(popup);
            _ui.Text(label,new(popup.X+32,popup.Y+16),.44f,RulebookInk);
            for(int row=0;row<lines.Count;row++)_ui.Text(lines[row],new(popup.X+32,popup.Y+56+row*30),.38f,RulebookInk);
            break;
        }
    }
}
