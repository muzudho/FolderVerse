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
    private readonly Dictionary<long,int>[] _directMaterialPositions={new(),new()};
    private Point _directMaterialGrab;
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
            if(DirectHangarViewport(1-drag.Side).Contains(_pointer)){
                int side=1-drag.Side;
                int key=DirectMaterialDropKey(side,drag.Id);
                if(key<0){_directMessage="楓の木を置ける空き区画を選んでください";return true;}
                bool reserved=_setup.Robots.MapleShipments.Any(o=>o.Source==source && o.Target==target && o.PackageId==drag.Id)
                    || _setup.Robots.ReserveMapleShipment(_setup,source,target,drag.Id);
                _directMessage=reserved
                    ?"素材の直送を予約しました / 配送時に空き枠を確認"
                    :"素材を出荷できません";
                if(reserved)_directMaterialPositions[side][drag.Id]=key;
            }
            else if(DirectVisibleSlots(drag.Side).Contains(drag.Slot/3) &&
                DirectStoredMaterials(drag.Side).TryGetValue(drag.Slot,out var package) &&
                Rectangle.Union(DirectViewPart(drag.Side,drag.Slot/3,drag.Slot%3),
                    DirectViewPart(drag.Side,drag.Slot/3,drag.Slot%3+package.Sections-1)).Contains(_pointer)
                && Vector2.DistanceSquared(_pointer.ToVector2(),_directMaterialGrab.ToVector2())<=36
                && _setup.Robots.CancelMapleShipment(source,drag.Id))
                _directMessage="素材の出荷予約を取り消しました";
            else if(drag.Slot<0 && DirectHangarViewport(drag.Side).Contains(_pointer)){
                if(_setup.Robots.CancelMapleShipment(source,drag.Id))_directMessage="素材の出荷予約を取り消しました";
            }
            else if(DirectHangarViewport(drag.Side).Contains(_pointer)){
                int key=DirectMaterialDropKey(drag.Side,drag.Id);
                if(key>=0){
                    _directMaterialPositions[drag.Side][drag.Id]=key;
                    _directMessage="格納庫内で楓の木を移動しました";
                }else _directMessage="楓の木を置ける空き区画を選んでください";
            }
            return true;
        }
        if(!click || _directDrag.HasValue)return false;
        for(int side=0;side<2;side++)foreach(var cargo in DirectStoredMaterials(side).Concat(DirectIncomingMaterials(side))){
            if(!DirectVisibleSlots(side).Contains(cargo.Key/3) || !Rectangle.Union(DirectViewPart(side,cargo.Key/3,cargo.Key%3),DirectViewPart(side,cargo.Key/3,cargo.Key%3+cargo.Value.Sections-1)).Contains(_pointer))continue;
            int source=_directNodes[side];
            if(!_setup.Robots.Nodes[source].MaplePackages.Any(p=>p.Id==cargo.Value.Id)){
                if(!_setup.Robots.MapleShipments.Any(o=>o.Source==_directNodes[1-side] && o.Target==source && o.PackageId==cargo.Value.Id))return false;
                _directMaterialDrag=(1-side,-1,cargo.Value.Id);
            }else _directMaterialDrag=(side,cargo.Key,cargo.Value.Id);
            _directMaterialGrab=_pointer;
            return true;
        }
        return false;
    }

    private int DirectMaterialDropKey(int side,long id)
    {
        var package=_setup.Robots.Nodes[_directNodes[0]].MaplePackages.Concat(_setup.Robots.Nodes[_directNodes[1]].MaplePackages).First(p=>p.Id==id);
        var store=PlanningStock.Nodes[_directNodes[side]];
        var cargo=DirectStoredMaterials(side).Concat(DirectIncomingMaterials(side)).Where(p=>p.Value.Id!=id).ToDictionary(p=>p.Key,p=>p.Value);
        foreach(int slot in DirectVisibleSlots(side)){
            if(!DirectViewCell(side,slot).Contains(_pointer) || DirectMaterialSlotReserved(side,slot))continue;
            int row=Enumerable.Range(0,3).FirstOrDefault(r=>DirectViewPart(side,slot,r).Contains(_pointer),-1);
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_directSlots[side][slot]);
            bool Fits(int start)=>start+package.Sections<=3 && Enumerable.Range(start,package.Sections).All(r=>
                !MaterialOccupies(cargo,slot,r) && (robot==null || (robot.Parts&HangarPart(r))==0));
            if(row>=0 && Fits(row))return slot*3+row;
            if(row<0)for(int r=0;r<3;r++)if(Fits(r))return slot*3+r;
        }
        return -1;
    }

    private Dictionary<int,MaplePackage> DirectStoredMaterials(int side)
    {
        var store=PlanningStock.Nodes[_directNodes[side]];
        return HangarMaterials(_directSlots[side],store,store.MaplePackages,slot=>DirectMaterialSlotReserved(side,slot),_directMaterialPositions[side]);
    }

    private bool DirectMaterialSlotReserved(int side,int slot)=>_setup.Robots.DirectDelivery.Orders.Any(
        o=>o.Target==_directNodes[side] && (o.TargetRobotId.HasValue?o.TargetRobotId==_directSlots[side][slot]:o.TargetSlot==slot));

    private Dictionary<int,MaplePackage> DirectIncomingMaterials(int side)
    {
        int node=_directNodes[side];
        var store=PlanningStock.Nodes[node];
        var incoming=_setup.Robots.MapleShipments.Where(o=>o.Target==node)
            .Select(o=>_setup.Robots.Nodes[o.Source].MaplePackages.FirstOrDefault(p=>p.Id==o.PackageId))
            .OfType<MaplePackage>();
        var stored=DirectStoredMaterials(side);
        return HangarMaterials(_directSlots[side],store,incoming,slot=>DirectMaterialSlotReserved(side,slot),_directMaterialPositions[side],stored)
            .Where(pair=>!stored.Values.Any(p=>p.Id==pair.Value.Id)).ToDictionary(pair=>pair.Key,pair=>pair.Value);
    }
}
