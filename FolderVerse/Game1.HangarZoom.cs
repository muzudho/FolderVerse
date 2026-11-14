namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Point[] HangarZoomSizes={new(2,1),new(3,2),new(4,3),new(6,4),new(8,5),new(10,6)};
    private static readonly Rectangle HangarViewport=new(84,300,780,530);
    private int _hangarZoom=1,_hangarColumn;
    private float _hangarIntroStart=float.NegativeInfinity,_hangarZoomAt;
    private bool _hangarPanning;
    private Point _hangarPanPointer,_hangarPanOrigin;
    private Point HangarZoom=>HangarZoomSizes[_hangarZoom];
    private int HangarGridColumns=>HangarColumnsForCapacity(_hangarSlots.Length);
    private static int HangarColumnsForCapacity(int capacity)=>HangarZoomSizes.FirstOrDefault(size=>size.X>=3 &&
        size.X*size.Y>=capacity,new Point(10,6)).X;
    private int HangarGridRows=>(_hangarSlots.Length+HangarGridColumns-1)/HangarGridColumns;
    private int HangarLastColumn=>Math.Max(0,HangarGridColumns-HangarZoom.X);
    private float HangarIntroProgress=>Math.Clamp((_animationTime-_hangarIntroStart)/.2f,0,1);
    private Vector2 HangarCellPitch(Point zoom)=>HangarCellPitch(HangarViewport,zoom,new(12f/264,10f/270));
    private static Vector2 HangarCellPitch(Rectangle viewport,Point zoom,Vector2 gap)=>new(
        viewport.Width/(zoom.X-gap.X),viewport.Height/(zoom.Y-gap.Y));

    private Rectangle HangarViewCell(int slot,Point zoom,int column,int row)=>
        HangarViewCell(HangarViewport,HangarCellPitch(zoom),new(12f/264,10f/270),slot,HangarGridColumns,column,row);

    private static Rectangle HangarViewCell(Rectangle viewport,Vector2 pitch,Vector2 gap,int slot,int columns,int column,int row)
    {
        float width=pitch.X,height=pitch.Y;
        int x=slot%columns-column,y=slot/columns-row;
        return new(viewport.X+(int)(x*width),viewport.Y+(int)(y*height),
            (int)(width*(1-gap.X)+.001f),(int)(height*(1-gap.Y)+.001f));
    }

    private static void SyncHangarOccupants(long?[] slots,RobotStore store,Func<int,bool> reserved)
    {
        var ids=store.Robots.Select(r=>r.Id).ToHashSet();
        for(int i=0;i<slots.Length;i++)if(slots[i] is long id && !ids.Contains(id))slots[i]=null;
        foreach(long id in ids.Where(id=>!slots.Contains(id))){
            int slot=Array.FindIndex(slots,item=>!item.HasValue);
            for(int i=slot;i>=0 && i<slots.Length;i++)if(!slots[i].HasValue && !reserved(i)){slot=i;break;}
            if(slot>=0)slots[slot]=id;
        }
    }
    private static Dictionary<int,MaplePackage> HangarMaterials(long?[] slots,RobotStore store,IEnumerable<MaplePackage> packages,Func<int,bool> reserved=null,
        IReadOnlyDictionary<long,int> positions=null, IReadOnlyDictionary<int,MaplePackage> occupied=null)
    {
        var robots=store.Robots.ToDictionary(r=>r.Id);
        var order=Enumerable.Range(0,slots.Length).Where(slot=>reserved?.Invoke(slot)!=true)
            .OrderBy(slot=>slots[slot].HasValue?0:1).ToArray();
        var used=new bool[slots.Length*3];
        foreach(int slot in order)
            if(slots[slot] is long id && robots.TryGetValue(id,out var robot))
                for(int row=0;row<3;row++)used[slot*3+row]=(robot.Parts&HangarPart(row))!=0;
        var result=new Dictionary<int,MaplePackage>();
        if(occupied!=null)
            foreach(var entry in occupied)
                for(int i=0;i<entry.Value.Sections;i++)used[entry.Key+i]=true;
        foreach(var package in packages.OrderByDescending(p=>p.Sections).ThenByDescending(p=>positions?.ContainsKey(p.Id)==true)){
            int sections=package.Sections;
            int key=order.SelectMany(slot=>Enumerable.Range(0,4-sections).Reverse().Select(row=>slot*3+row))
                .FirstOrDefault(start=>Enumerable.Range(start,sections).All(index=>!used[index]),-1);
            if(positions?.TryGetValue(package.Id,out int preferred)==true && preferred>=0 &&
                order.Contains(preferred/3) && preferred%3+sections<=3 &&
                Enumerable.Range(preferred,sections).All(index=>!used[index]))key=preferred;
            if(key<0)continue;
            result.Add(key,package);
            for(int i=0;i<sections;i++)used[key+i]=true;
        }
        return result;
    }

    private static bool MaterialOccupies(Dictionary<int,MaplePackage> cargo,int slot,int row)=>
        cargo.Any(p=>p.Key/3==slot && row>=p.Key%3 && row<p.Key%3+p.Value.Sections);

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

    private static Point HangarPanOffset(Point origin,Point delta,Vector2 pitch,Point limit)=>new(
        Math.Clamp(origin.X+(int)MathF.Round(delta.X/pitch.X),0,limit.X),
        Math.Clamp(origin.Y+(int)MathF.Round(delta.Y/pitch.Y),0,limit.Y));

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
            if(robot!=null)DrawRobotGlyph(HangarRobotArea(cell),HomePersuasionRobot(robot),whiteOutline:true);
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
            DrawRobotGlyph(AssemblyRobotBounds(slot),HomePersuasionRobot(robot),whiteOutline:true);
            if(robot.CanFight)DrawEquipmentCounts(HangarCellRegion(cell,8,208,236,48),
                robot.Equipment(robot.Owner,_setup.EquipmentMode),Cream);
            if(robot.HarvestWorker)DrawHangarReservationLabel(HangarCellRegion(cell,0,170,252,36),robot,"採取員");
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
