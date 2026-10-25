namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _nodeOfficeOpen;
    private int _lastNodeTab;
    private static readonly Rectangle NodeOfficeTile=new(84,169,190,60);
    private void OpenNodeOffice(){_nodeOfficeOpen=true;_nodeAssemblyOpen=_nodeFactoryOpen=_nodeDisposalOpen=_nodeTransportOpen=false;_hangarDrag=null;_factoryDropDown=_factoryPlanLine=-1;_lastNodeTab=0;}
    private void RestoreNodeTab(){
        int tab=_lastNodeTab;OpenNodeOffice();
        if(tab==1)OpenNodeFactory();else if(tab==2)OpenNodeDisposal();else if(tab==3)OpenNodeAssembly();
        _lastNodeTab=tab;
    }
    private void DrawNodeTabs(){
        var areas=new[]{NodeOfficeTile,NodeFactoryTile,NodeDisposalTile,NodeAssemblyTile};
        string[] labels={"役所","ロボット製造工場","ロボット廃棄計画","ロボット格納庫"};
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
        _ui.Center("役所 / 地点監督",new(84,644,780,60),.8f,Cream);
        string owner=node.Owner<0?"未征服":_setup.ConquerorNames[node.Owner]+"の配下";
        _ui.Center(owner,new(84,718,780,45),.6f,Cream);
        string name=_setup.Nodes.DisplayName(node);
        _ui.Center(name,new(84,778,780,55),Math.Min(.6f,760/Math.Max(1,_font.MeasureString(name).X)),Cream);
    }
}
