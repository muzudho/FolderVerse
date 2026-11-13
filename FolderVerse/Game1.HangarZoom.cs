namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Point[] HangarZoomSizes={new(2,1),new(3,2),new(4,3),new(6,4),new(8,5),new(10,6)};
    private static readonly Rectangle HangarViewport=new(84,300,780,530);
    private int _hangarZoom=1,_hangarColumn;
    private float _hangarIntroStart=float.NegativeInfinity,_hangarZoomAt;
    private Point HangarZoom=>HangarZoomSizes[_hangarZoom];
    private int HangarGridColumns=>HangarZoomSizes.FirstOrDefault(size=>size.X>=3 &&
        size.X*size.Y>=_hangarSlots.Length,new Point(10,6)).X;
    private int HangarGridRows=>(_hangarSlots.Length+HangarGridColumns-1)/HangarGridColumns;
    private int HangarLastColumn=>Math.Max(0,HangarGridColumns-HangarZoom.X);
    private float HangarIntroProgress=>Math.Clamp((_animationTime-_hangarIntroStart)/.2f,0,1);

    private Rectangle HangarViewCell(int slot,Point zoom,int column,int row)
    {
        float width=780f/(zoom.X-12f/264),height=530f/(zoom.Y-10f/270);
        int x=slot%HangarGridColumns-column,y=slot/HangarGridColumns-row;
        return new(HangarViewport.X+(int)(x*width),HangarViewport.Y+(int)(y*height),
            (int)(width*252/264),(int)(height*260/270));
    }

    private static Rectangle HangarCellRegion(Rectangle cell,int x,int y,int width,int height)=>
        new(cell.X+x*cell.Width/252,cell.Y+y*cell.Height/260,
            Math.Max(1,width*cell.Width/252),Math.Max(1,height*cell.Height/260));

    private static Rectangle HangarRobotArea(Rectangle cell)
    {
        float scale=Math.Min(cell.Width/252f,cell.Height/260f);
        int width=Math.Max(1,(int)(100*scale));
        return new(cell.Center.X-width/2,cell.Y+(int)(12*scale),width,Math.Max(1,(int)(180*scale)));
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
    {
        var overview=new Point(HangarGridColumns,Math.Max(1,HangarGridRows));
        float progress=MathHelper.SmoothStep(0,1,HangarIntroProgress);
        var bay=new Color(62,86,96);
        for(int slot=0;slot<_hangarSlots.Length;slot++)
        {
            var start=HangarViewCell(slot,overview,0,0);
            var end=HangarViewCell(slot,HangarZoom,0,0);
            int x=(int)MathHelper.Lerp(start.X,end.X,progress),y=(int)MathHelper.Lerp(start.Y,end.Y,progress);
            var cell=new Rectangle(x,y,(int)MathHelper.Lerp(start.Width,end.Width,progress),
                (int)MathHelper.Lerp(start.Height,end.Height,progress));
            var clipped=Rectangle.Intersect(cell,HangarViewport);
            if(clipped.IsEmpty || clipped.Width<=0 || clipped.Height<=0)continue;
            _ui.Box(clipped,bay);
            if(clipped!=cell)continue;
            var robot=store.Robots.FirstOrDefault(r=>r.Id==_hangarSlots[slot]);
            if(robot!=null)DrawRobotGlyph(HangarRobotArea(cell),robot,whiteOutline:true);
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
            DrawRobotGlyph(AssemblyRobotBounds(slot),HomePersuasionRobot(robot),whiteOutline:true);
            if(robot.CanFight)DrawEquipmentCounts(HangarCellRegion(cell,8,208,236,48),
                robot.Equipment(robot.Owner,_setup.EquipmentMode),Cream);
        }
        DrawHangarSlotNumber(slot,cell,robot,robot?.CanFight==true);
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
