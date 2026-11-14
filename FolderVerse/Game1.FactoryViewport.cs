namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Rectangle FactoryViewport=new(78,260,782,580);
    private static readonly Point[] FactoryZoomSizes={new(1,2),new(1,3),new(2,4),new(2,5),new(2,6),new(3,7),new(3,8),new(3,9)};
    private int _factoryZoom,_factoryColumn;
    private float _factoryIntroStart=float.NegativeInfinity;
    private Point? _factoryPan;
    private float FactoryIntroProgress=>Math.Clamp((_animationTime-_factoryIntroStart)/2f,0,1);
    private int FactoryZoomLevel=>float.IsNegativeInfinity(_factoryIntroStart) || FactoryIntroProgress<1?_factoryZoom:0;
    private bool FactoryOverviewActive=>FactoryZoomLevel>0;

    private void BeginFactoryViewport()
    {
        int count=FactoryLaneCount();
        _factoryZoom=Array.FindIndex(FactoryZoomSizes,s=>s.X*s.Y>=count);
        if(_factoryZoom<0)_factoryZoom=FactoryZoomSizes.Length-1;
        _factoryColumn=0;_factoryPan=null;
        _factoryIntroStart=_factoryZoom>0?_animationTime:float.NegativeInfinity;
    }

    private void StopFactoryIntro()=>_factoryIntroStart=float.NegativeInfinity;

    private static int FactoryGridRows(Point size,int count)=>Math.Max(size.Y,(count+2)/3);

    private Rectangle FactoryOverviewBounds(int lane,int count)
    {
        var size=FactoryZoomSizes[_factoryZoom];
        int rows=FactoryGridRows(size,count);
        int x=lane/rows-_factoryColumn,y=lane%rows-_factoryPage;
        if(x<0 || x>=size.X || y<0 || y>=size.Y)return Rectangle.Empty;
        int width=FactoryViewport.Width/size.X,height=FactoryViewport.Height/size.Y;
        var bounds=new Rectangle(FactoryViewport.X+x*width,FactoryViewport.Y+y*height,width-4,height-4);
        if(!float.IsNegativeInfinity(_factoryIntroStart) && lane<2){
            float t=MathHelper.SmoothStep(0,1,FactoryIntroProgress);
            bounds=new(
                (int)MathHelper.Lerp(bounds.X,FactoryViewport.X,t),
                (int)MathHelper.Lerp(bounds.Y,FactoryViewport.Y+lane*290,t),
                (int)MathHelper.Lerp(bounds.Width,FactoryViewport.Width,t),
                (int)MathHelper.Lerp(bounds.Height,286,t));
        }
        return bounds;
    }

    private bool FactoryViewportInput(MouseState mouse)
    {
        if(!_nodeFactoryOpen || _factoryPlanLine>=0 || _factoryDropDown>=0 || _populationNode<0){
            _factoryPan=null;return false;
        }
        if(!FactoryViewport.Contains(_pointer) && !_factoryPan.HasValue)return false;
        int count=FactoryLaneCount();
        int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
        if(wheel!=0){
            int anchor=_factoryPage;
            if(FactoryOverviewActive){
                for(int i=0;i<count;i++)if(FactoryOverviewBounds(i,count).Contains(_pointer)){anchor=i;break;}
            }else if(_pointer.Y>=FactoryViewport.Y+290)anchor++;
            _factoryZoom=Math.Clamp(FactoryZoomLevel-Math.Sign(wheel),0,FactoryZoomSizes.Length-1);
            StopFactoryIntro();
            var size=FactoryZoomSizes[_factoryZoom];
            int rows=_factoryZoom==0?Math.Max(size.Y,count):FactoryGridRows(size,count);
            int row=Math.Clamp((_pointer.Y-FactoryViewport.Y)*size.Y/FactoryViewport.Height,0,size.Y-1);
            _factoryPage=Math.Clamp(anchor%rows-row,0,Math.Max(0,rows-size.Y));
            int column=Math.Clamp((_pointer.X-FactoryViewport.X)*size.X/FactoryViewport.Width,0,size.X-1);
            _factoryColumn=Math.Clamp(anchor/rows-column,0,Math.Max(0,(count+rows-1)/rows-size.X));
            return true;
        }
        if(mouse.RightButton!=ButtonState.Pressed){_factoryPan=null;return false;}
        _factoryZoom=FactoryZoomLevel;
        StopFactoryIntro();
        if(!_factoryPan.HasValue){_factoryPan=_pointer;return true;}
        var zoom=FactoryZoomSizes[_factoryZoom];
        int step=Math.Min(64,FactoryViewport.Height/zoom.Y);
        int dy=(_factoryPan.Value.Y-_pointer.Y)/step,dx=(_factoryPan.Value.X-_pointer.X)/64;
        if(dy!=0 || dx!=0){
            int rows=_factoryZoom==0?Math.Max(zoom.Y,count):FactoryGridRows(zoom,count);
            _factoryPage=Math.Clamp(_factoryPage+dy,0,rows-zoom.Y);
            _factoryColumn=Math.Clamp(_factoryColumn+dx,0,Math.Max(0,(count+rows-1)/rows-zoom.X));
            _factoryPan=new(_factoryPan.Value.X-dx*64,_factoryPan.Value.Y-dy*step);
        }
        return true;
    }

    private void DrawFactoryOverview(RobotWorkshop[] lines)
    {
        float fade=float.IsNegativeInfinity(_factoryIntroStart)?1:1-FactoryIntroProgress;
        for(int i=lines.Length-1;i>=0;i--){
            var bounds=FactoryOverviewBounds(i,lines.Length);
            if(bounds.IsEmpty)continue;
            var line=lines[i];
            float opacity=i<2?1:fade;
            _ui.Box(bounds,new Color(39,64,78)*opacity);
            bool waiting=line.WaitingForMaple(_setup.Robots,_populationNode) || line.Waiting;
            string state=line.Paused?"停止":waiting?"入荷・材料待ち":line.RestRemaining>0?"休暇":
                line.MapleRemaining>0?"採取中":line.HarvestOnly?"採取レーン":line.Manufacturing && !line.CycleFinished?"製造中":"停止";
            _ui.Text($"{i+1}  {state}",new(bounds.X+8,bounds.Y+5),.30f,Cream*opacity);
            if(line.Manufacturing && bounds.Height>=60){
                var art=new Rectangle(bounds.Center.X-22,bounds.Y+21,44,Math.Min(68,bounds.Height-29));
                DrawRobotGlyph(art,line.CurrentProductionPlan.Preview(_setup.Nodes.All[_populationNode].Owner),opacity:opacity);
            }
        }
    }
}
