namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _factionPickerOpen;
    private int _factionPickerPage;
    private Rectangle FactionPickerButton=>new((int)(OperationMedallionCenter.X+MedallionRadius+10),(int)OperationMedallionCenter.Y-24,52,48);
    private Rectangle FactionPickerPanel=>new(Math.Clamp(FactionPickerButton.Right+12,720,1340),190,540,560);
    private Rectangle FactionPickerRow(int row)=>new(FactionPickerPanel.X+12,FactionPickerPanel.Y+60+row*52,516,50);
    private (int Ruler,int Depth)[] FactionTree()
    {
        var forest=_setup.Relations.Forest(_setup);int start=Array.FindIndex(forest,e=>e.Ruler==_setup.TurnPlayer);
        if(start<0)return Array.Empty<(int,int)>();int depth=forest[start].Depth;
        return forest.Skip(start).TakeWhile((e,i)=>i==0 || e.Depth>depth).Select(e=>(e.Ruler,e.Depth-depth)).ToArray();
    }
    private bool FactionPickerAvailable=>!_directPairOpen && !_trainingOpen && _supervisorDialog==0 && !_inspectorFoldersOpen && !_salvageAbandonConfirmation.HasValue && !_hangarDrag.HasValue && !_seedDialog.IsOpen && _invasionPicker<0;
    private void StoreCurrentMovementDraft()
    {
        if(!_movementOpen || _selectedMoveNode<0)return;
        if(_warPlanning)ReserveInvasion();
        else _setup.Campaign.Plan(new(_setup.PlayerSlot,_selectedMoveNode,0,_setup.Nodes.Current(_setup.PlayerSlot).Id,true,true,Peaceful:true));
    }
    private bool SelectOperationRuler(int ruler)
    {
        if(!_setup.ControlledParty(_setup.TurnPlayer).Contains(ruler))return false;
        if(ruler==_setup.PlayerSlot){_factionPickerOpen=false;return true;}
        SaveDomesticCamera();StoreCurrentMovementDraft();ResetInspector();
        if(!_setup.SelectControlledRuler(ruler))return false;
        RestorePlayerView();_selectedMoveNode=-1;_net.SelectedTarget=-1;_inspectedNode=-1;_factionPickerOpen=false;return true;
    }
    private bool FactionPickerInput(bool click,bool escape)
    {
        if(!FactionPickerAvailable){_factionPickerOpen=false;return false;}
        if(escape && _factionPickerOpen){_factionPickerOpen=false;return true;}
        if(!click)return _factionPickerOpen;
        var tree=FactionTree();
        if(tree.Length>1 && FactionPickerButton.Contains(_pointer)){_factionPickerOpen=!_factionPickerOpen;_factionPickerPage=0;_mapOverlayMenu=false;return true;}
        if(!_factionPickerOpen)return false;
        var panel=FactionPickerPanel;
        if(new Rectangle(panel.X+12,panel.Bottom-56,100,40).Contains(_pointer))_factionPickerPage=Math.Max(0,_factionPickerPage-1);
        else if(new Rectangle(panel.Right-112,panel.Bottom-56,100,40).Contains(_pointer))_factionPickerPage=Math.Min((tree.Length-1)/8,_factionPickerPage+1);
        else{
            for(int row=0;row<8;row++){int index=_factionPickerPage*8+row;if(index>=tree.Length)break;
                if(FactionPickerRow(row).Contains(_pointer)){SelectOperationRuler(tree[index].Ruler);return true;}}
            if(!panel.Contains(_pointer))_factionPickerOpen=false;
        }
        return true;
    }
    private void DrawFactionPicker()
    {
        if(!_factionPickerOpen || !FactionPickerAvailable)return;
        var panel=FactionPickerPanel;var tree=FactionTree();_factionPickerPage=Math.Clamp(_factionPickerPage,0,Math.Max(0,(tree.Length-1)/8));
        _ui.Box(new(panel.X-3,panel.Y-3,panel.Width+6,panel.Height+6),Accent);_ui.Box(panel,new Color(22,39,53));
        _ui.Text("操作する国 / 上下関係",new(panel.X+16,panel.Y+14),.48f,Cream);
        for(int row=0;row<8;row++){
            int index=_factionPickerPage*8+row;if(index>=tree.Length)break;var entry=tree[index];int ruler=entry.Ruler;
            bool available=_setup.ControlledParty(_setup.TurnPlayer).Contains(ruler),selected=ruler==_setup.PlayerSlot;var box=FactionPickerRow(row);
            _ui.Box(box,selected?Accent:new Color(32,53,69));int indent=Math.Min(7,entry.Depth)*22;
            if(entry.Depth>0){_ui.Box(new(box.X+7+indent-15,box.Y+3,2,24),Cream);_ui.Box(new(box.X+7+indent-15,box.Y+25,13,2),Cream);}
            var photo=new Rectangle(box.X+8+indent,box.Y+5,42,40);_portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],photo,_setup.Relations.Powered[ruler]);
            string role=ruler==_setup.TurnPlayer?"あなた":!available?(_setup.ControllerOf(ruler)!=_setup.TurnPlayer?"別プレイヤー":"操作不可"):selected?"操作中":"手下";
            var label=_setup.ConquerorNames[ruler]+" / "+role;
            _ui.Text(label,new(photo.Right+10,box.Y+15),Math.Min(.38f,(box.Right-photo.Right-18)/_font.MeasureString(label).X),selected?new Color(18,32,44):available?Cream:new Color(154,172,189));
        }
        _ui.Button(new(panel.X+12,panel.Bottom-56,100,40),"←",Muted,.4f);_ui.Button(new(panel.Right-112,panel.Bottom-56,100,40),"→",Muted,.4f);
        _ui.Center($"{_factionPickerPage+1}/{Math.Max(1,(tree.Length+7)/8)} / 陣営のターンは共通",new(panel.X+116,panel.Bottom-56,panel.Width-232,40),.3f,Cream);
    }
}
