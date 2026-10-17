namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private readonly System.Diagnostics.Stopwatch _rateBlink=System.Diagnostics.Stopwatch.StartNew();
    private static readonly Rectangle RateDialogArea=new(1100,190,800,820);
    private static readonly Vector2 RatePieCenter=new(1500,600);
    private static readonly Rectangle RateZeroButton=new(1476,362,48,46);
    private static readonly Rectangle RateMinusButton=new(1408,362,58,46);
    private static readonly Rectangle RatePlusButton=new(1534,362,58,46);
    private static readonly int[] RateDivisions={8,9,10,12};
    private readonly Dictionary<int,Texture2D> _ratePieIcons=new();
    private static string RateLabel(int degrees)=>$"{degrees*100/360f:0.#}% ({degrees}°)";
    private static int PointerDegrees(Point p)=>(int)Math.Round((Math.Atan2(p.X-RatePieCenter.X,RatePieCenter.Y-p.Y)+Math.PI*2)%(Math.PI*2)*180/Math.PI);
    private static Rectangle RatePresetBounds(int row,int column)
    {
        float angle=(column+1)*MathHelper.TwoPi/RateDivisions[row];
        var at=RatePieCenter+new Vector2(MathF.Sin(angle),-MathF.Cos(angle))*(155+row*48);
        return new((int)at.X-24,(int)at.Y-23,48,46);
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
    private void DrawRateFractionButton(Rectangle rect,string fraction,bool enabled,bool selected)
    {
        bool hover=enabled && rect.Contains(_pointer);
        _ui.Box(new(rect.X+2,rect.Y+4,rect.Width,rect.Height),new Color(8,20,29));
        _ui.Box(rect,selected?new Color(255,154,103):hover?Color.White:new Color(91,180,199));
        var face=new Rectangle(rect.X+3,rect.Y+3,rect.Width-6,rect.Height-6);
        var white=enabled?new Color(255,253,241):new Color(138,146,150);
        var yellow=enabled?new Color(255,221,91):new Color(151,147,119);
        _ui.Box(face,yellow);
        for(int y=0;y<face.Height;y++)
        {
            int width=(int)(face.Width*(1-y/(float)face.Height));
            _ui.Box(new(face.X,face.Y+y,width,1),white);
            _ui.Box(new(face.X+width-1,face.Y+y,2,1),new Color(75,83,87));
        }
        _transportHudFont??=Content.Load<SpriteFont>("ToyCounterFont");
        float scale=25f/_transportHudFont.LineSpacing;
        void Number(string text,Vector2 center)
        {
            var size=_transportHudFont.MeasureString(text)*scale;
            _ui.Batch.DrawString(_transportHudFont,text,center-size/2,new Color(23,31,37),0,Vector2.Zero,scale,SpriteEffects.None,0);
        }
        var parts=fraction.Split('/');
        Number(parts[0],new(face.X+face.Width*.28f,face.Y+face.Height*.25f));
        Number(parts[1],new(face.X+face.Width*.72f,face.Y+face.Height*.75f));
    }
    private void DrawRateStepButton(Rectangle rect,bool positive,bool enabled)
    {
        var color=enabled?(rect.Contains(_pointer)?Color.White:new Color(134,219,216)):Muted;
        for(int y=0;y<rect.Height;y++)
        {
            int inset=(int)(Math.Abs(y-rect.Height/2f)/(rect.Height/2f)*rect.Width*.45f);
            _ui.Box(new(rect.X+(positive?0:inset),rect.Y+y,rect.Width-inset,1),color);
        }
        _ui.Center(positive?"+1":"-1",new(rect.X+(positive?-5:5),rect.Y,rect.Width,rect.Height),.48f,new Color(23,31,37));
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
        if(RateZeroButton.Contains(p))SetDestinationRate(_rateTarget,0);
        if(RateMinusButton.Contains(p))SetDestinationRate(_rateTarget,Rate(_rateTarget)-1);
        if(RatePlusButton.Contains(p))SetDestinationRate(_rateTarget,Rate(_rateTarget)+1);
        if(Vector2.Distance(new Vector2(p.X,p.Y),RatePieCenter)<=110)SetDestinationRate(_rateTarget,PointerDegrees(p));
        for(int row=0;row<4;row++)for(int col=0;col<RateDivisions[row];col++)
        {
            int degrees=(col+1)*360/RateDivisions[row];
            if(RatePresetUnique(row,col) && RatePresetBounds(row,col).Contains(p) && degrees<=RateMaximum(_rateTarget))SetDestinationRate(_rateTarget,degrees);
        }
        if(new Rectangle(1740,950,130,40).Contains(p))_rateTarget=-1;
    }
    private void DrawRatePieDialog()
    {
        int value=Rate(_rateTarget),maximum=RateMaximum(_rateTarget);
        _ui.Box(RateDialogArea,new Color(18,35,48));
        _ui.Text($"指示 #{_rateTarget} の採用率  {RateLabel(value)}",new(1130,210),.65f,Cream);
        var segments=new List<(int Degrees,Color Color,int Number)>{(value,Accent,_rateTarget)};
        foreach(var rule in TransportRules.Where(r=>r.Number!=_rateTarget))segments.Add((rule.Degrees,new Color(80+(rule.Number*37)%120,110+(rule.Number*17)%100,150),rule.Number));
        segments.Add((_setup.Robots.Transport.RestDegrees(_populationNode,TransportOwner),new Color(105,115,125),0));
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
        // Tiny 1-degree sectors cannot fit a readable label; the color key retains every number.
        for(int i=0;i<segments.Count;i++){var seg=segments[i];var r=seg.Number==0?new Rectangle(1130,322,190,22):new Rectangle(1130+i%8*92,275+i/8*21,88,19);_ui.Box(new(r.X,r.Y+4,7,10),seg.Color);_ui.Text((seg.Number==0?"#0 休憩":"#"+seg.Number)+" "+(seg.Degrees*100/360f).ToString("0.#")+"%",new(r.X+10,r.Y),.28f,Cream);}
        float sectorStart=0;var labels=new List<Rectangle>();
        for(int i=0;i<segments.Count;i++){
            var seg=segments[i];
            if(seg.Degrees>=9){float a=(sectorStart+seg.Degrees/2f)*MathHelper.Pi/180;float radius=i%2==0?62:88;
                var at=RatePieCenter+new Vector2(MathF.Sin(a),-MathF.Cos(a))*radius;var box=new Rectangle((int)at.X-14,(int)at.Y-10,28,20);
                if(!labels.Any(r=>r.Intersects(box))){labels.Add(box);_ui.Box(box,new Color(18,35,48));_ui.Center("#"+seg.Number,box,.3f,Cream);}
            }sectorStart+=seg.Degrees;
        }
        float radians=value*MathF.PI/180;Vector2 edge=RatePieCenter+new Vector2(MathF.Sin(radians),-MathF.Cos(radians))*110;
        _ui.Box(new((int)edge.X-7,(int)edge.Y-7,14,14),new Color(255,231,54));
        // Pulse the selected sector boundary, including its two radial edges.
        float pulse=.45f+.55f*(.5f+.5f*MathF.Sin((float)_rateBlink.Elapsed.TotalSeconds*MathHelper.TwoPi));
        var outline=new Color(255,231,54)*pulse;
        void Line(Vector2 a,Vector2 b)
        {
            var delta=b-a;
            _ui.Tile((a+b)/2,new Vector2(delta.Length()+1,4),MathF.Atan2(delta.Y,delta.X),outline);
        }
        if(value>0)
        {
            if(value<360){Line(RatePieCenter,RatePieCenter+new Vector2(0,-110));Line(RatePieCenter,edge);}
            for(int degree=0;degree<value;degree+=2)
            {
                float a=degree*MathF.PI/180,b=Math.Min(value,degree+2)*MathF.PI/180;
                Line(RatePieCenter+new Vector2(MathF.Sin(a),-MathF.Cos(a))*110,RatePieCenter+new Vector2(MathF.Sin(b),-MathF.Cos(b))*110);
            }
        }
        _ui.Text("円の境界を動かして１度ずつ設定",new(1130,920),.42f,Cream);
        _ui.Text($"設定可能 0° - {maximum}°",new(1130,245),.4f,Cream);
        _ui.Button(RateZeroButton,"0",value==0?new Color(255,174,110):new Color(255,221,91),.65f);
        DrawRateStepButton(RateMinusButton,false,value>0);
        DrawRateStepButton(RatePlusButton,true,value<maximum);
        for(int row=0;row<4;row++)for(int col=0;col<RateDivisions[row];col++)
        {
            if(!RatePresetUnique(row,col))continue;
            int degree=(col+1)*360/RateDivisions[row];var rect=RatePresetBounds(row,col);bool enabled=degree<=maximum;
            DrawRateFractionButton(rect,RateFraction(col+1,RateDivisions[row]),enabled,degree==value);
        }
        _ui.Text("合計100%。#0休憩は残りの割合。編集不可。",new(1130,965),.36f,Cream);
        _ui.Button(new(1740,950,130,40),"閉じる",Accent,.45f);
    }
}
