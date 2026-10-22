namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool HasMoveReservation()=>_setup.Campaign.PlannedOrders.Any(o=>o.Peaceful && CanPlanRuler(o.Ruler));
    private void DrawMoveReservationButton()
    {
        _ui.Button(MovementButton,"   移動予約",Accent,.48f);
        var lamp=new Rectangle(MovementButton.X+10,MovementButton.Center.Y-8,16,16);
        _ui.Box(new(lamp.X-2,lamp.Y-2,20,20),new Color(10,28,35));
        _ui.Box(lamp,HasMoveReservation()?new Color(139,255,158):new Color(64,79,83));
    }
    private int[] MoveReservationTargets()=>_setup.Campaign.PlannedOrders.Where(o=>o.Peaceful && o.Queen && CanPlanRuler(o.Ruler)).Select(o=>o.TargetNode)
        .Concat(_movementOpen && !_warPlanning && _selectedMoveNode>=0?new[]{_selectedMoveNode}:Array.Empty<int>()).Distinct().ToArray();
    // The sword is above the node; the bus is below and to its right.
    private static Rectangle MoveBusBounds(Vector2 at)=>new((int)at.X+24,(int)at.Y+20,68,36);
    private Rectangle VisibleMoveBusBounds(Vector2 at)
    {
        var box=MoveBusBounds(at);
        box.X=Math.Clamp(box.X,MapViewBounds.Left+16,MapViewBounds.Right-box.Width-16);
        box.Y=Math.Clamp(box.Y,MapViewBounds.Top+16,MapViewBounds.Bottom-box.Height-42);
        var sword=new Rectangle((int)at.X-57,(int)at.Y-65,114,61);
        if(new Rectangle(box.X-13,box.Y-3,94,73).Intersects(sword)){
            box.X=(int)at.X+(at.X-MapViewBounds.Left<MapViewBounds.Right-at.X?80:-150);
            box.X=Math.Clamp(box.X,MapViewBounds.Left+16,MapViewBounds.Right-box.Width-16);
        }
        return box;
    }
    private void DrawMoveReservationMarkers()
    {
        foreach(int id in MoveReservationTargets()){
            if(!OperationPosition(id,out var at))continue;
            bool selected=_movementOpen && !_warPlanning && _selectedMoveNode==id;
            var box=VisibleMoveBusBounds(at);
            var color=selected?new Color(255,226,126):new Color(128,221,214);
            var link=new Vector2(box.Left,box.Center.Y)-at;
            _ui.Tile(at+link/2,new(link.Length(),2),MathF.Atan2(link.Y,link.X),color);
            _ui.Box(new(box.X-3,box.Y-3,box.Width+6,box.Height+10),new(11,28,40));
            _ui.Box(box,color);_ui.Box(new(box.X+3,box.Y+3,box.Width-6,4),Cream);
            for(int i=0;i<4;i++)_ui.Box(new(box.X+6+i*14,box.Y+10,11,13),new(39,77,96));
            _ui.Box(new(box.Right-7,box.Y+9,4,22),new(39,77,96));
            _ui.Box(new(box.X+10,box.Bottom-3,12,9),new(20,29,36));_ui.Box(new(box.Right-24,box.Bottom-3,12,9),new(20,29,36));
            var label=new Rectangle(box.X-13,box.Bottom+10,94,24);_ui.Box(label,new(11,28,40));
            _ui.Center(selected?"移動先選択中":"移動予約済",label,.27f,color);
        }
    }
}
