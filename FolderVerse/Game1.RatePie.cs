namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private static readonly Rectangle RateDialogArea=new(1100,190,800,820);
    private static readonly Vector2 RatePieCenter=new(1500,600);
    private static readonly int[] RateDivisions={8,9,10,12};
    private readonly Dictionary<int,Texture2D> _ratePieIcons=new();
    private static string RateLabel(int degrees)=>$"{degrees*100/360f:0.#}% ({degrees}°)";
    private static int PointerDegrees(Point p)=>(int)Math.Round((Math.Atan2(p.X-RatePieCenter.X,RatePieCenter.Y-p.Y)+Math.PI*2)%(Math.PI*2)*180/Math.PI);
    private static Rectangle RatePresetBounds(int row,int column)
    {
        float angle=(column+1)*MathHelper.TwoPi/RateDivisions[row];
        var at=RatePieCenter+new Vector2(MathF.Sin(angle),-MathF.Cos(angle))*(155+row*48);
        return new((int)at.X-28,(int)at.Y-26,56,52);
    }
    private static bool RatePresetUnique(int row,int column)
    {
        int degree=(column+1)*360/RateDivisions[row];
        for(int earlier=0;earlier<row;earlier++)
            if(degree*RateDivisions[earlier]%360==0)return false;
        return true;
    }
    private static string RateFraction(int numerator,int denominator)
    {
        int a=numerator,b=denominator;while(b!=0){int t=a%b;a=b;b=t;}
        return $"{numerator/a}/{denominator/a}";
    }
    private Texture2D RatePieIcon(int degrees,int divisions)
    {
        if(_ratePieIcons.TryGetValue(degrees*100+divisions,out var icon))return icon;
        const int size=48;var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float dx=x-23.5f,dy=y-23.5f,r=MathF.Sqrt(dx*dx+dy*dy);
            if(r>23)continue;
            double angle=(Math.Atan2(dx,-dy)+Math.PI*2)%(Math.PI*2)*180/Math.PI;
            double nearest=angle%(360d/divisions);bool ray=r*Math.Sin(Math.Min(nearest,360d/divisions-nearest)*Math.PI/180)<.6;
            pixels[y*size+x]=r>21 || ray?Color.White:angle<degrees?Accent:new Color(56,73,86);
        }
        icon=new Texture2D(GraphicsDevice,size,size);icon.SetData(pixels);_ratePieIcons[degrees*100+divisions]=icon;return icon;
    }
    private void RatePieClick(Point p)
    {
        if(new Rectangle(1130,270,125,40).Contains(p))SetDestinationRate(_rateTarget,0);
        if(new Rectangle(1270,270,60,40).Contains(p))SetDestinationRate(_rateTarget,Rate(_rateTarget)-1);
        if(new Rectangle(1340,270,60,40).Contains(p))SetDestinationRate(_rateTarget,Rate(_rateTarget)+1);
        if(Vector2.Distance(new Vector2(p.X,p.Y),RatePieCenter)<=110)SetDestinationRate(_rateTarget,PointerDegrees(p));
        for(int row=0;row<4;row++)for(int col=0;col<RateDivisions[row];col++)
        {
            int degrees=(col+1)*360/RateDivisions[row];
            if(RatePresetUnique(row,col) && RatePresetBounds(row,col).Contains(p) && degrees<=RateMaximum(_rateTarget))SetDestinationRate(_rateTarget,degrees);
        }
        if(new Rectangle(1670,270,160,36).Contains(p) && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))
        {var order=_setup.Robots.Transport.Priority(_populationNode).Concat(DestinationNodes()).Distinct().ToList();order.Remove(_rateTarget);order.Insert(0,_rateTarget);_setup.Robots.Transport.SetPriority(_populationNode,order);}
        if(new Rectangle(1740,950,130,40).Contains(p))_rateTarget=-1;
    }
    private void DrawRatePieDialog()
    {
        int value=Rate(_rateTarget),maximum=RateMaximum(_rateTarget);
        _ui.Box(RateDialogArea,new Color(18,35,48));
        _ui.Text($"移送割合  {RateLabel(value)}",new(1130,210),.65f,Cream);
        // Selected destination, other reserved routes, and stay share one complete circle.
        var segments=new List<(int Degrees,Color Color)>{(value,Accent)};
        foreach(bool disposal in new[]{false,true})
            foreach(var w in _setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,_nodeTransportPart,disposal))
                if(disposal!=_nodeTransportDisposal || w.Target!=_rateTarget)
                    segments.Add((w.Degrees,disposal?new Color(155,106,147):new Color(92+(w.Target*37)%100,130,170)));
        segments.Add((maximum-value,new Color(56,73,86)));
        // One small scanline run per sector; no new graphics resources per frame.
        for(int y=-110;y<=110;y+=2)
        {
            int extent=(int)Math.Sqrt(110*110-y*y),start=-extent;Color previous=Color.Transparent;
            for(int x=-extent;x<=extent+1;x++)
            {
                Color color=Color.Transparent;
                if(x<=extent){double angle=(Math.Atan2(x,-y)+Math.PI*2)%(Math.PI*2)*180/Math.PI;int sum=0;foreach(var seg in segments){sum+=seg.Degrees;if(angle<sum){color=seg.Color;break;}}}
                if(x==-extent){previous=color;continue;}
                if(color!=previous){_ui.Box(new((int)RatePieCenter.X+start,(int)RatePieCenter.Y+y,x-start,2),previous);start=x;previous=color;}
            }
        }
        float radians=value*MathF.PI/180;Vector2 edge=RatePieCenter+new Vector2(MathF.Sin(radians),-MathF.Cos(radians))*110;
        _ui.Box(new((int)edge.X-7,(int)edge.Y-7,14,14),Cream);
        _ui.Text("円の境界を動かして１度ずつ設定",new(1130,920),.42f,Cream);
        _ui.Text($"設定可能 0° - {maximum}°",new(1130,245),.4f,Cream);
        _ui.Button(new(1130,270,125,40),"クリアー",Accent,.42f);
        _ui.Button(new(1270,270,60,40),"-1",Accent,.5f);
        _ui.Button(new(1340,270,60,40),"+1",value<maximum?Accent:Muted,.5f);
        _ui.Button(new(1670,270,160,36),"受入１位",CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner)?Accent:Muted,.4f);
        for(int row=0;row<4;row++)for(int col=0;col<RateDivisions[row];col++)
        {
            if(!RatePresetUnique(row,col))continue;
            int degree=(col+1)*360/RateDivisions[row];var rect=RatePresetBounds(row,col);bool enabled=degree<=maximum;
            _ui.Box(rect,degree==value?new Color(90,103,110):new Color(27,47,60));
            _ui.Batch.Draw(RatePieIcon(degree,RateDivisions[row]),new Rectangle(rect.X+15,rect.Y+2,26,26),enabled?Color.White:new Color(85,85,85));
            _ui.Center(RateFraction(col+1,RateDivisions[row]),new(rect.X,rect.Y+28,rect.Width,24),.4f,enabled?Cream:Muted);
        }
        _ui.Text("他の移送先の配分は維持。残りは現在の節点に留まる",new(1130,965),.36f,Cream);
        _ui.Button(new(1740,950,130,40),"閉じる",Accent,.45f);
    }
}
