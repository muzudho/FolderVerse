namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _inspectorFoldersOpen;
    private int _folderScroll,_folderContextNode=-1,_strategyRuler=-1;
    private readonly HashSet<int> _expandedFolderRegions=new();
    private readonly List<(int Kind,int Id,int Depth,int Owner,string Label)> _inspectorFolderRows=new();
    private bool StrategyReadOnly=>_reignView && _strategyRuler>=0 && _strategyRuler!=_setup.PlayerSlot;
    private Rectangle InspectorFolderButton=>new(54,150,850,50);
    private static readonly Rectangle InspectorFolderPanel=new(64,238,820,650);
    private void DrawCountryFolder(Rectangle r,int owner)
    {
        var color=_setup.OwnerColor(owner);
        _ui.Box(new(r.X,r.Y+5,r.Width,r.Height-5),new Color(7,19,28));
        _ui.Box(new(r.X+2,r.Y,r.Width/2,9),color);
        _ui.Box(new(r.X+2,r.Y+7,r.Width-2,r.Height-9),color);
        _ui.Box(new(r.X+4,r.Y+9,r.Width-6,2),Cream*.65f);
    }
    private void BuildInspectorFolders()
    {
        _inspectorFolderRows.Clear();int ruler=_setup.PlayerSlot;
        var ancestors=new List<int>();var seen=new HashSet<int>{ruler};
        for(int parent=_setup.Relations.Superiors[ruler];parent>=0 && seen.Add(parent);parent=_setup.Relations.Superiors[parent])ancestors.Add(parent);
        ancestors.Reverse();int depth=0;
        foreach(int parent in ancestors)_inspectorFolderRows.Add((0,parent,depth++,parent,"征服者："+_setup.ConquerorFullNames[parent]));
        _inspectorFolderRows.Add((0,ruler,depth,ruler,"征服者："+_setup.ConquerorFullNames[ruler]+"（現在のターン）"));
        foreach(var cell in _setup.Cells.Where(c=>_setup.Nodes.InCell(c.Id).Any(n=>n.Owner==ruler))){
            _inspectorFolderRows.Add((1,cell.Id,depth+1,ruler,"地域監督："+WorldCoordinates.Label(_setup,cell.Id)));
            if(!_expandedFolderRegions.Contains(cell.Id))continue;
            foreach(var node in _setup.Nodes.InCell(cell.Id).Where(n=>n.Owner==ruler))
                _inspectorFolderRows.Add((2,node.Id,depth+2,ruler,"地点監督："+_setup.Nodes.DisplayName(node)));
        }
    }
    private void OpenInspectorFolders()
    {
        int node=_populationNode>=0?_populationNode:_regionCell>=0?_inspectedNode:_folderContextNode;
        if(node<0 || node>=_setup.Nodes.All.Length)node=_setup.Nodes.Current(_setup.PlayerSlot)?.Id??-1;
        _folderContextNode=node;
        if(node>=0)_expandedFolderRegions.Add(_setup.Nodes.All[node].Cell);
        BuildInspectorFolders();
        int current=_inspectorFolderRows.FindIndex(row=>IsCurrentInspectorFolder(row.Kind,row.Id));
        _folderScroll=Math.Clamp(current-4,0,Math.Max(0,_inspectorFolderRows.Count-10));_inspectorFoldersOpen=true;
    }
    private bool IsCurrentInspectorFolder(int kind,int id)=>kind switch {
        0=>_reignView && id==(_strategyRuler>=0?_strategyRuler:_setup.PlayerSlot),
        1=>_regionCell>=0 && id==_regionCell,
        2=>_populationNode>=0 && id==_populationNode,
        _=>false
    };
    private void OpenInspectorFolderRow(int index)
    {
        if(index<0 || index>=_inspectorFolderRows.Count)return;
        var row=_inspectorFolderRows[index];
        if(row.Kind==0){OpenStrategyInspector();_strategyRuler=row.Id;}
        else if(row.Kind==1)OpenRegionInspector(row.Id);
        else{OpenNodeInspector(row.Id);OpenNodeOffice();}
    }
    private bool InspectorFolderInput(MouseState mouse,bool click,bool escape,bool active)
    {
        if(!_inspectorFoldersOpen)return false;
        if(!IsStatusScreen || !InspectorVisible){_inspectorFoldersOpen=false;return false;}
        if(!active)return true;
        var p=new Point(_pointer.X-NodeInspectorOffset,_pointer.Y);
        if(escape || click && !InspectorFolderPanel.Contains(p)){_inspectorFoldersOpen=false;return true;}
        int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
        _folderScroll=Math.Clamp(_folderScroll-wheel/120*3,0,Math.Max(0,_inspectorFolderRows.Count-10));
        if(click && new Rectangle(824,248,44,36).Contains(p)){_inspectorFoldersOpen=false;return true;}
        if(click && p.Y>=298 && p.Y<858){
            int index=_folderScroll+(p.Y-298)/56;
            if(index<_inspectorFolderRows.Count){
                var row=_inspectorFolderRows[index];int x=90+Math.Min(row.Depth,10)*25;
                if(row.Kind==1 && p.X<x+24){if(!_expandedFolderRegions.Add(row.Id))_expandedFolderRegions.Remove(row.Id);BuildInspectorFolders();_folderScroll=Math.Min(_folderScroll,Math.Max(0,_inspectorFolderRows.Count-10));}
                else OpenInspectorFolderRow(index);
            }
        }
        return true;
    }
    private void DrawInspectorFolders()
    {
        if(!_reignView && _regionCell<0 && _populationCell<0)return;
        DrawInspector(()=>{
            int owner=_populationNode>=0?_setup.Nodes.All[_populationNode].Owner:_regionCell>=0?_setup.Nodes.All[_inspectedNode].Owner:_strategyRuler>=0?_strategyRuler:_setup.PlayerSlot;
            var header=InspectorFolderButton;
            _ui.Box(header,_inspectorFoldersOpen || header.Contains(_ui.Pointer)?new Color(65,96,106):new Color(40,67,82));
            _ui.Box(new(header.X,header.Y,header.Width,3),_setup.OwnerColor(owner));
            DrawCountryFolder(new(header.X+14,header.Y+12,32,28),owner);
            string label=_populationNode>=0?"地点 / "+_setup.Nodes.DisplayName(_setup.Nodes.All[_populationNode]):_regionCell>=0?"地域 / "+WorldCoordinates.Label(_setup,_regionCell):"国 / "+_setup.ConquerorFullNames[owner];
            _ui.Text(label,new(header.X+60,header.Y+13),Math.Min(.55f,(header.Width-116)/Math.Max(1,_font.MeasureString(label).X)),Cream);
            for(int row=0;row<10;row++)_ui.Box(new(header.Right-38+row,header.Y+21+row,20-row*2,1),Cream);
            if(!_inspectorFoldersOpen)return;
            _ui.Box(new(68,243,820,650),Color.Black*.6f);_ui.Box(InspectorFolderPanel,new Color(23,43,56));
            _ui.Text("所属フォルダー",new(90,254),.6f,Cream);_ui.Button(new(824,248,44,36),"×",Muted,.6f);
            for(int i=0;i<10;i++){
                int index=_folderScroll+i;if(index>=_inspectorFolderRows.Count)break;
                var row=_inspectorFolderRows[index];int x=90+Math.Min(row.Depth,10)*25,y=298+i*56;
                var r=new Rectangle(80,y,788,52);if(IsCurrentInspectorFolder(row.Kind,row.Id)){_ui.Box(r,new Color(65,96,106));_ui.Box(new(r.X,y,5,r.Height),Accent);}else if(r.Contains(_ui.Pointer))_ui.Box(r,new Color(48,72,87));
                if(row.Depth>0){_ui.Box(new(x-13,y-17,1,43),Muted);_ui.Box(new(x-13,y+25,18,1),Muted);}
                if(row.Kind==1)_ui.Text(_expandedFolderRegions.Contains(row.Id)?"－":"+",new(x,y+13),.4f,Cream);
                DrawCountryFolder(new(x+25,y+12,30,28),row.Owner);
                _ui.Text(row.Label,new(x+64,y+15),Math.Min(.42f,(850-x-64)/Math.Max(1,_font.MeasureString(row.Label).X)),Cream);
            }
            _ui.Text("ホイール：スクロール / ＋：地点を展開 / 名前：開く",new(90,863),.32f,Cream);
        });
    }
}
