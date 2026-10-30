namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public sealed partial class CubeNet
{
    private readonly List<(int Ruler,Rectangle Bounds)> _batteries=new();
    public int HoveredBattery {get;set;}=-1;
    public void ClearGlobeBatteries()=>_batteries.Clear();
    public void AddGlobeBatteryBadge(int ruler,Rectangle bounds)=>_batteries.Add((ruler,bounds));
    // Screen-space proxy only: no node, territory or physical location is added.
    public static Vector2 BatteryOffset(WorldSetup world,int ruler)
    {
        int index=Enumerable.Range(0,ruler).Count(r=>world.Relations.Powered[r] && world.Nodes.Current(r)?.Id==world.Nodes.Current(ruler)?.Id);
        return new(64,index*48-32);
    }
    public int HitBattery(Point point)
    {
        if(!ShowBatteries || IsAnimating)return -1;
        if(_badgePanel.HasValue)point=UnrotateScreen(point,_badgePanel.Value);
        return _batteries.Where(b=>b.Bounds.Contains(point)).Select(b=>b.Ruler).DefaultIfEmpty(-1).First();
    }
    private void DrawBatteries(UiPainter ui,WorldSetup world,Rectangle panel,Func<int,Vector2?> project,float pulse)
    {
        _batteries.Clear();if(!ShowBatteries)return;
        foreach(int ruler in Enumerable.Range(0,world.ActiveCount))
        {
            if(VisibleOperationBatteries!=null && !VisibleOperationBatteries.Contains(ruler))continue;
            if(!world.Relations.Powered[ruler] || !ShowOtherCountries && !world.Relations.Controls(ruler))continue;
            var origin=project(ruler);if(origin==null)continue;
            var at=origin.Value+BatteryOffset(world,ruler);
            bool hover=HoveredBattery==ruler;int zoom=1,width=25,height=16;
            var resting=at;
            int nodeId=world.Nodes.Current(ruler)?.Id??-1;
            if(hover || ShowRoutes && nodeId>=0 && EmphasizedNode(nodeId))at.Y-=MapObjectHop(pulse,ruler);
            var box=new Rectangle((int)at.X-width/2,(int)at.Y-height/2,width,height);
            if(!MapViewport(panel).Intersects(box))continue;
            var hit=new Rectangle((int)resting.X-width/2,(int)resting.Y-height/2,width,height);hit.Inflate(6,6);_batteries.Add((ruler,Rectangle.Union(hit,box)));
            var delta=at-origin.Value;ui.Tile((at+origin.Value)/2,new(delta.Length(),1),MathF.Atan2(delta.Y,delta.X),world.OwnerColor(ruler)*.65f);
            var backing=box;backing.Inflate(6,6);ui.Box(backing,new Color(12,27,36));ui.Box(box,world.OwnerColor(ruler));
            ui.Box(new(box.Right,box.Y+height/4,3*zoom,height/2),world.OwnerColor(ruler));
            for(int i=0;i<3;i++)ui.Box(new(box.X+(3+i*7)*zoom,box.Y+3*zoom,5*zoom,10*zoom),new Color(35,112,70));
            if(ShowRobots && hover)
            {
                var robots=world.Robots.Retinues[ruler].Robots;
                for(int i=0;i<robots.Count;i++)
                {
                    var robot=robots[i];var slot=at+RobotRingSlot(i,robots.Count)*1.6f;
                    DrawMapRobot(ui,new((int)slot.X-12,(int)slot.Y-18,24,36),world.OwnerColor(ruler),robot.Parts,robot.DamagedParts!=(RobotParts)0,robot.CheapParts!=(RobotParts)0);
                }
            }
        }
    }
}
