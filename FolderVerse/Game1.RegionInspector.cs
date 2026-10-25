namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool? _routesBeforeRegionInspector;
    private int RegionMapFocusCell=>_regionCell>=0 && !_nodeInspectorClosing?_regionCell:-1;
    private int _regionCell=-1,_regionNodePage;
    private int AutomationNode=>_regionCell>=0?_inspectedNode:_populationNode;
    private static Rectangle RegionNodeButton(int slot)=>new(84+slot%2*395,750+slot/2*46,380,40);
    private void OpenRegionInspector(int cell)
    {
        if(cell<0 || cell>=_setup.Cells.Length)return;
        var nodes=_setup.Nodes.InCell(cell).OrderByDescending(n=>n.Population.Total).ThenBy(n=>n.Id).ToArray();
        if(nodes.Length==0)return;
        bool routes=_routesBeforeRegionInspector??_net.ShowRoutes;
        ResetInspector();_routesBeforeRegionInspector=routes;_net.ShowRoutes=false;_regionCell=_selectedMapCell=cell;_inspectedNode=nodes[0].Id;_regionNodePage=0;
        QueueInspectorCenter(_inspectedNode);
    }
    private bool RegionInspectorClick(Point point)
    {
        if(_regionCell<0 || !NodeInspectorBounds.Contains(point))return false;
        point.X-=NodeInspectorOffset;
        if(NodeAutomationClick(point))return true;
        var nodes=_setup.Nodes.InCell(_regionCell).OrderByDescending(n=>n.Population.Total).ThenBy(n=>n.Id).ToArray();
        int pages=Math.Max(1,(nodes.Length+5)/6);
        if(new Rectangle(644,700,100,40).Contains(point))_regionNodePage=(_regionNodePage+pages-1)%pages;
        if(new Rectangle(754,700,100,40).Contains(point))_regionNodePage=(_regionNodePage+1)%pages;
        for(int i=0;i<6;i++){int index=_regionNodePage*6+i;if(index<nodes.Length && RegionNodeButton(i).Contains(point)){_inspectedNode=nodes[index].Id;_supervisorDialog=0;}}
        return true;
    }
    private void DrawRegionInspector()
    {
        var nodes=_setup.Nodes.InCell(_regionCell).OrderByDescending(n=>n.Population.Total).ThenBy(n=>n.Id).ToArray();
        var node=_setup.Nodes.All[_inspectedNode];var color=_setup.OwnerColor(node.Owner);
        _ui.Box(new(54,150,850,825),new Color(27,49,62));
        _ui.Text("地域インスペクター",new(84,170),.7f,Cream);
        string name=WorldCoordinates.Label(_setup,_regionCell);
        _ui.Center(name,new(84,215,780,45),Math.Min(.65f,760/Math.Max(1,_font.MeasureString(name).X)),Cream);
        var image=new Rectangle(229,286,500,326);
        _ui.Box(new(image.X-6,image.Y-6,image.Width+12,image.Height+12),color);
        DrawSupervisor(image,node.Owner);
        _ui.Center("地域監督",new(84,628,780,45),.64f,Cream);
        string owner=node.Owner<0?"未征服":_setup.ConquerorNames[node.Owner]+"の配下";
        _ui.Center(owner+" / 担当地点："+_setup.Nodes.DisplayName(node),new(84,678,780,28),Math.Min(.38f,760/Math.Max(1,_font.MeasureString(owner+" / 担当地点："+_setup.Nodes.DisplayName(node)).X)),Cream);
        _ui.Text("地域内の担当地点を選択",new(84,713),.38f,Cream);
        int pages=Math.Max(1,(nodes.Length+5)/6);
        if(pages>1){_ui.Button(new(644,700,100,40),"←",Muted,.5f);_ui.Button(new(754,700,100,40),"→",Muted,.5f);}
        for(int i=0;i<6;i++){
            int index=_regionNodePage*6+i;if(index>=nodes.Length)break;
            var n=nodes[index];var r=RegionNodeButton(i);string label=_setup.Nodes.DisplayName(n);
            _ui.Button(r,label,n.Id==_inspectedNode?Accent:Muted,Math.Min(.4f,(r.Width-16)/Math.Max(1,_font.MeasureString(label).X)));
        }
        DrawNodeAutomation();
    }
}
