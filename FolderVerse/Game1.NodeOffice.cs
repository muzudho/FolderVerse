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
        bool[] enabled={true,NodeFactoriesAllowed && _setup.Robots.Workshops[_populationNode].ProductionLines.Length>0 && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner),CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner),CanOpenHangar};
        for(int i=0;i<4;i++){
            var r=areas[i];var color=active[i] || enabled[i] && r.Contains(_pointer)?Accent:Muted;
            _ui.Box(r,color);_ui.Center(labels[i],r,.36f,enabled[i]?UiPainter.ContrastInk(color):Cream*.4f);
            if(active[i])_ui.Box(new(r.X,r.Y,r.Width,4),Cream);
        }
    }
    private void DrawNodeOffice(){
        _ui.Box(new(54,150,850,825),new Color(27,49,62));DrawNodeTabs();
        var node=_setup.Nodes.All[_populationNode];var image=new Rectangle(229,294,500,326);
        _ui.Box(new(image.X-6,image.Y-6,image.Width+12,image.Height+12),_setup.OwnerColor(node.Owner));DrawSupervisor(image,node.Owner,_populationNode);
        _ui.Center(NodeOfficeName+" / 地点監督",new(84,644,780,60),.8f,Cream);
        string owner=node.Owner<0?"未征服":_setup.ConquerorNames[node.Owner]+"の配下";
        _ui.Center(owner,new(84,718,780,45),.6f,Cream);
        _ui.Button(new(84,778,784,48),"放棄品置き場 / "+_setup.Salvage.Items.Count(i=>i.Node==node.Id)+" 件",Muted,.46f);
    }
}
