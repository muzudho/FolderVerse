namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private readonly int[] _directZoom={1,1},_directColumn=new int[2],_directRow=new int[2];
    private int _directPanSide=-1;
    private Point _directPanStart,_directPanOrigin;
    private (int Side,int Slot,long Id)? _directMaterialDrag;
    private static readonly Vector2 DirectHangarGap=new(10f/174,10f/290);
    private static Rectangle DirectHangarViewport(int side)=>new(40+side*1320,300,512,570);
    private int DirectColumns(int side)=>HangarColumnsForCapacity(_directSlots[side].Length);
    private Point DirectScrollLimit(int side)
    {
        var zoom=HangarZoomSizes[_directZoom[side]];
        int columns=DirectColumns(side);
        return new(Math.Max(0,columns-zoom.X),Math.Max(0,(_directSlots[side].Length+columns-1)/columns-zoom.Y));
    }
    private void ClampDirectScroll(int side)
    {
        var limit=DirectScrollLimit(side);
        _directColumn[side]=Math.Clamp(_directColumn[side],0,limit.X);
        _directRow[side]=Math.Clamp(_directRow[side],0,limit.Y);
    }
    private Rectangle DirectViewCell(int side,int slot)
    {
        var viewport=DirectHangarViewport(side);
        var pitch=HangarCellPitch(viewport,HangarZoomSizes[_directZoom[side]],DirectHangarGap);
        return HangarViewCell(viewport,pitch,DirectHangarGap,slot,DirectColumns(side),_directColumn[side],_directRow[side]);
    }
    private Rectangle DirectViewPart(int side,int slot,int row)
    {
        var cell=DirectViewCell(side,slot);
        return new(cell.X+6*cell.Width/164,cell.Y+(8+row*60)*cell.Height/280,
            Math.Max(1,90*cell.Width/164),Math.Max(1,58*cell.Height/280));
    }
    private IEnumerable<int> DirectVisibleSlots(int side)
    {
        var zoom=HangarZoomSizes[_directZoom[side]];
        int columns=DirectColumns(side);
        for(int y=0;y<zoom.Y;y++)for(int x=0;x<zoom.X;x++){
            int slot=(_directRow[side]+y)*columns+_directColumn[side]+x;
            if(_directColumn[side]+x<columns && slot<_directSlots[side].Length)yield return slot;
        }
    }
    private bool DirectHangarNavigation(MouseState mouse)
    {
        if(mouse.RightButton!=ButtonState.Pressed)_directPanSide=-1;
        bool carrying=_directDrag.HasValue || _directMaterialDrag.HasValue;
        for(int side=0;side<2;side++)
        {
            var view=DirectHangarViewport(side);
            if(_directPanSide!=side && !view.Contains(_pointer))continue;
            int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
            if(_directPanSide<0 && wheel!=0 && (!carrying || mouse.LeftButton==ButtonState.Pressed)){
                _directZoom[side]=Math.Clamp(_directZoom[side]-Math.Sign(wheel),0,HangarZoomSizes.Length-1);
                ClampDirectScroll(side);return true;
            }
            if(carrying)continue;
            if(_directPanSide<0 && mouse.RightButton==ButtonState.Pressed && _previousMouse.RightButton!=ButtonState.Pressed){
                _directPanSide=side;_directPanStart=_pointer;_directPanOrigin=new(_directColumn[side],_directRow[side]);
            }
            if(_directPanSide!=side)continue;
            var pitch=HangarCellPitch(view,HangarZoomSizes[_directZoom[side]],DirectHangarGap);
            var scroll=HangarPanOffset(_directPanOrigin,_directPanStart-_pointer,pitch,DirectScrollLimit(side));
            _directColumn[side]=scroll.X;_directRow[side]=scroll.Y;
            return true;
        }
        return false;
    }

    private bool DirectMaterialInput(MouseState mouse,bool click)
    {
        if(_directMaterialDrag is {} drag){
            if(mouse.LeftButton!=ButtonState.Released)return true;
            _directMaterialDrag=null;
            int source=_directNodes[drag.Side],target=_directNodes[1-drag.Side];
            if(DirectHangarViewport(1-drag.Side).Contains(_pointer))
                _directMessage=_setup.Robots.ReserveMapleShipment(_setup,source,target,drag.Id)
                    ?"素材の直送を予約しました / 配送時に空き枠を確認"
                    :"素材を出荷できません";
            else if(DirectVisibleSlots(drag.Side).Contains(drag.Slot) && DirectViewCell(drag.Side,drag.Slot).Contains(_pointer)
                && _setup.Robots.CancelMapleShipment(source,drag.Id))
                _directMessage="素材の出荷予約を取り消しました";
            return true;
        }
        if(!click || _directDrag.HasValue)return false;
        for(int side=0;side<2;side++)foreach(int slot in DirectVisibleSlots(side)){
            if(!DirectViewCell(side,slot).Contains(_pointer))continue;
            int source=_directNodes[side];
            if(_directSlots[side][slot] is not long id || !_setup.Robots.Nodes[source].MaplePackages.Any(p=>p.Id==id))return false;
            _directMaterialDrag=(side,slot,id);
            return true;
        }
        return false;
    }

    private Dictionary<int,MaplePackage> DirectIncomingMaterials(int side)
    {
        int node=_directNodes[side];
        var slots=Enumerable.Range(0,_directSlots[side].Length).Where(slot=>!_directSlots[side][slot].HasValue &&
            !_setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==node && o.TargetRobotId==null && o.TargetSlot==slot));
        var incoming=_setup.Robots.MapleShipments.Where(o=>o.Target==node)
            .Select(o=>_setup.Robots.Nodes[o.Source].MaplePackages.FirstOrDefault(p=>p.Id==o.PackageId))
            .OfType<MaplePackage>();
        return slots.Zip(incoming).ToDictionary(pair=>pair.First,pair=>pair.Second);
    }
}
