namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Rectangle HangarViewport=new(84,300,780,530);
    private int _hangarZoom=1,_hangarColumn;
    private float _hangarIntroStart=float.NegativeInfinity,_hangarZoomAt;
    private bool _hangarPanning;
    private Point _hangarPanPointer,_hangarPanOrigin;
    private Point HangarZoom=>HangarZoomSizes[_hangarZoom];
    private int HangarGridColumns=>HangarColumnsForCapacity(_hangarSlots.Length);
    private int HangarGridRows=>(_hangarSlots.Length+HangarGridColumns-1)/HangarGridColumns;
    private int HangarLastColumn=>Math.Max(0,HangarGridColumns-HangarZoom.X);
    private float HangarIntroProgress=>Math.Clamp((_animationTime-_hangarIntroStart)/.2f,0,1);
    private Vector2 HangarCellPitch(Point zoom)=>HangarCellPitch(HangarViewport,zoom,new(12f/264,10f/270));
    private Rectangle HangarViewCell(int slot,Point zoom,int column,int row)=>
        HangarViewCell(HangarViewport,HangarCellPitch(zoom),new(12f/264,10f/270),slot,HangarGridColumns,column,row);

    private bool UpdateHangarPan(MouseState mouse)
    {
        if(mouse.RightButton!=ButtonState.Pressed)
        {
            _hangarPanning=false;
            return false;
        }
        if(!_hangarPanning)
        {
            if(_previousMouse.RightButton==ButtonState.Pressed || mouse.LeftButton==ButtonState.Pressed ||
                _hangarDrag.HasValue || !HangarViewport.Contains(_pointer))return false;
            _hangarPanning=true;
            _hangarIntroStart=float.NegativeInfinity;
            _hangarPanPointer=_pointer;
            _hangarPanOrigin=new(_hangarColumn,_hangarPage);
        }
        var pitch=HangarCellPitch(HangarZoom);
        var scroll=HangarPanOffset(_hangarPanOrigin,_hangarPanPointer-_pointer,pitch,new(HangarLastColumn,HangarLastRow));
        _hangarColumn=scroll.X;_hangarPage=scroll.Y;
        return true;
    }

    private bool UpdateHangarZoom(MouseState mouse,bool click)
    {
        if(HangarIntroProgress<1)
        {
            if(!click)return true;
            _hangarIntroStart=float.NegativeInfinity;
            return true;
        }
        int delta=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
        if(delta==0 || !HangarViewport.Contains(_pointer) || _hangarDrag.HasValue)return false;
        if(_animationTime<_hangarZoomAt)return true;
        _hangarZoomAt=_animationTime+.16f;
        int next=Math.Clamp(_hangarZoom-Math.Sign(delta),0,HangarZoomSizes.Length-1);
        if(next==_hangarZoom)return true;
        float x=(_pointer.X-HangarViewport.X)/792f,y=(_pointer.Y-HangarViewport.Y)/540f;
        var previous=HangarZoom;
        _hangarZoom=next;
        _hangarColumn=Math.Clamp(_hangarColumn+(int)MathF.Round(x*(previous.X-HangarZoom.X)),0,HangarLastColumn);
        _hangarPage=Math.Clamp(_hangarPage+(int)MathF.Round(y*(previous.Y-HangarZoom.Y)),0,HangarLastRow);
        return true;
    }

    private void DrawHangarOverview(RobotStore store)
        =>DrawHangarTransition(store,new(HangarGridColumns,Math.Max(1,HangarGridRows)),Point.Zero,Point.Zero,HangarIntroProgress);

    private void DrawPersuasionHangarTransition(RobotStore store)
    {
        int index=PersuasionStop;
        var startZoom=index==0?new Point(HangarGridColumns,Math.Max(1,HangarGridRows)):HangarZoom;
        var start=index==0?Point.Zero:_persuasionStops[index-1];
        DrawHangarTransition(store,startZoom,start,new(_hangarColumn,_hangarPage),
            Math.Clamp((float)(PersuasionStopAge/PersuasionPanSeconds),0,1));
    }

    private void DrawHangarTransition(RobotStore store,Point startZoom,Point from,Point to,float fraction)
    {
        float progress=MathHelper.SmoothStep(0,1,fraction);
        var bay=new Color(62,86,96);
        var materials=AssemblyMaterials(store);
        for(int slot=0;slot<_hangarSlots.Length;slot++)
        {
            var start=HangarViewCell(slot,startZoom,from.X,from.Y);
            var end=HangarViewCell(slot,HangarZoom,to.X,to.Y);
            int x=(int)MathHelper.Lerp(start.X,end.X,progress),y=(int)MathHelper.Lerp(start.Y,end.Y,progress);
            var cell=new Rectangle(x,y,(int)MathHelper.Lerp(start.Width,end.Width,progress),
                (int)MathHelper.Lerp(start.Height,end.Height,progress));
            var clipped=Rectangle.Intersect(cell,HangarViewport);
            if(clipped.IsEmpty || clipped.Width<=0 || clipped.Height<=0)continue;
            _ui.Box(clipped,bay);
            if(clipped!=cell)continue;
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);
            TrackWarehouseKeeperHover(cell,robot);
            if(robot!=null)DrawHangarRobot(HangarRobotArea(cell),HomePersuasionRobot(robot));
            for(int row=0;row<3;row++)if(materials.TryGetValue(slot*3+row,out var package))
                DrawMaplePackage(HangarCellRegion(cell,80,8+row*60,92,58+(package.Sections-1)*60),package,
                    opacity:MapleArrival(package)?.55f:1,compact:true);
            DrawHangarSlotNumber(slot,cell,robot,false);
        }
    }

    private void DrawCompactHangarSlot(int slot,Rectangle cell,Robot robot)
    {
        var fill=cell.Contains(_pointer)?new Color(54,89,102):new Color(43,67,80);
        _ui.Box(cell,fill);
        _ui.Box(HangarCellRegion(cell,0,0,252,14),new Color(79,105,114));
        for(int row=0;row<3;row++)
        {
            var part=HangarPart(row);
            if(_hangarReservation==HangarReservation.None && !_hangarReturnHomeMode && HangarPlaceEnabled(robot,part))
                _ui.Box(AssemblyPartBounds(slot,row),new Color(48,78,94));
        }
        if(robot!=null)
        {
            DrawHangarRobot(AssemblyRobotBounds(slot),HomePersuasionRobot(robot));
            if(_setup.Robots.Discarding.Reserved(robot.Id))DrawHangarReservationLabel(HangarCellRegion(cell,0,170,252,36),robot,"廃棄予定");else if(robot.WarehouseKeeper)DrawHangarReservationLabel(HangarCellRegion(cell,0,170,252,36),robot,"倉庫管理人");
            else if(robot.DismantlingWorker)DrawHangarReservationLabel(HangarCellRegion(cell,0,170,252,36),robot,"解体員");else if(robot.HarvestWorker)DrawHangarReservationLabel(HangarCellRegion(cell,0,170,252,36),robot,"採取員");
        }
        DrawHangarSlotNumber(slot,cell,robot,robot?.CanFight==true);
    }

    private void DrawHangarEquipmentForeground(RobotStore store)
    {
        if(HangarIntroProgress<1 || _homePersuasionActive && PersuasionStopAge<PersuasionPanSeconds)return;
        foreach(int slot in VisibleAssemblySlots())
        {
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);
            if(robot?.CanFight!=true)continue;
            var cell=AssemblyCell(slot);
            var area=_hangarZoom==1?new Rectangle(cell.X+92,cell.Y+235,cell.Width-96,22):HangarCellRegion(cell,8,208,236,48);
            DrawEquipmentCounts(area,HomePersuasionRobot(robot).Equipment(robot.Owner,_setup.EquipmentMode),Cream);
        }
    }
    private void DrawHangarSlotNumber(int slot,Rectangle cell,Robot robot,bool inCorner)
    {
        string label=(slot+1).ToString();
        var bounds=inCorner?HangarCellRegion(cell,0,0,60,56):
            HangarCellRegion(cell,0,208,252,44);
        _ui.Center(label,bounds,Math.Min(.38f,cell.Height/310f),
            robot==null?Cream*.65f:Cream);
    }
}
