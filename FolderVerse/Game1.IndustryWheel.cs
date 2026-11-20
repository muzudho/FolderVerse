namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private static readonly Rectangle IndustryCabinetBounds=new(710,20,1040,1040);
    private static readonly Vector2 IndustryWheelCenter=new(1222,489);
    private const float IndustryWheelRadius=345;
    private Texture2D _industryCabinet,_industryEnamel,_industryBrass;
    private IReadOnlyList<IndustrySector> _industryEnamelWheel;

    private void DrawAntiqueIndustryWheel(float angle,bool stopped)
    {
        var wheel=_setup.Industry.Wheel;
        var center=IndustryWheelCenter;
        const float radius=IndustryWheelRadius;
        _industryCabinet??=LoadIndustryCabinet();
        _industryBrass??=CreateIndustryBrass();
        if(!ReferenceEquals(wheel,_industryEnamelWheel)){
            var enamel=CreateIndustryEnamel(wheel);
            _industryEnamel?.Dispose();
            _industryEnamel=enamel;
            _industryEnamelWheel=wheel;
        }
        float half=_industryEnamel.Width*.5f;
        _spriteBatch.Draw(_industryEnamel,center,null,Color.White,angle,new(half),364/half,SpriteEffects.None,0);
        _spriteBatch.Draw(_industryCabinet,IndustryCabinetBounds,Color.White);

        float percent=0;
        for(int i=0;i<wheel.Count;i++){
            float boundary=angle+percent/100*MathHelper.TwoPi;
            Brass(center+Direction(boundary)*(radius-15),15);
            if(stopped && i==_setup.Industry.SelectedSector){
                float end=boundary+wheel[i].Percent/100f*MathHelper.TwoPi;
                // An inset highlight stays visible even when the winning sector is only 1%.
                float edgeRadius=radius-30;
                int segments=Math.Max(2,wheel[i].Percent);
                var previous=center+Direction(boundary)*edgeRadius;
                for(int part=1;part<=segments;part++){
                    var next=center+Direction(MathHelper.Lerp(boundary,end,part/(float)segments))*edgeRadius;
                    var edge=next-previous;
                    _ui.Tile((next+previous)/2,new(edge.Length()+1,4),MathF.Atan2(edge.Y,edge.X),new Color(255,252,229));
                    previous=next;
                }
            }
            percent+=wheel[i].Percent;
        }

        var labels=IndustryLabelLayout(center,radius,angle);
        float labelPercent=0;
        for(int i=0;i<Math.Min(labels.Length,IndustryVisibleLabels);i++){
            var label=labels[i];
            float rotation=angle+(labelPercent+wheel[i].Percent*.5f)/100*MathHelper.TwoPi+MathHelper.PiOver2;
            var pivot=new Vector2(label.Bounds.Center.X,label.Bounds.Center.Y);
            var canvas=CanvasBounds();
            var canvasTransform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
            _spriteBatch.End();
            _spriteBatch.Begin(transformMatrix:Matrix.CreateTranslation(-pivot.X,-pivot.Y,0)*Matrix.CreateRotationZ(rotation)*Matrix.CreateTranslation(pivot.X,pivot.Y,0)*canvasTransform);
            DrawIndustryDuration(wheel[i].Turns,label.Bounds,new Color(56,38,24),label.Scale);
            _spriteBatch.End();_spriteBatch.Begin(transformMatrix:canvasTransform);
            labelPercent+=wheel[i].Percent;
        }
        Brass(center+new Vector2(0,3),72);
        Brass(center,60);
        DrawIndustryPointer(new Point((int)center.X,(int)(center.Y-radius)));

        void Brass(Vector2 at,int diameter)=>_spriteBatch.Draw(_industryBrass,
            new Rectangle((int)at.X-diameter/2,(int)at.Y-diameter/2,diameter,diameter),Color.White);
        static Vector2 Direction(float radians)=>new(MathF.Cos(radians),MathF.Sin(radians));
    }

    private Color[] _industryEnamelColors;
    private void DisposeIndustryWheel()
    {
        _industryCabinet?.Dispose();
        _industryEnamel?.Dispose();
        _industryBrass?.Dispose();
        _industryCabinet=_industryEnamel=_industryBrass=null;
        _industryEnamelWheel=null;
    }
    private Texture2D LoadIndustryCabinet()
    {
        using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Setup","industry-wheel-cabinet-v001.png"));
        return Texture2D.FromStream(GraphicsDevice,stream);
    }

    private Texture2D CreateIndustryEnamel(IReadOnlyList<IndustrySector> wheel)
    {
        const int size=768;
        const float half=size/2f;
        var palette=IndustryColors(wheel);
        var sectors=new int[100];
        var starts=new int[wheel.Count];
        int start=0;
        for(int i=0;i<wheel.Count;i++){
            starts[i]=start;
            Array.Fill(sectors,i,start,wheel[i].Percent);
            start+=wheel[i].Percent;
        }
        var pixels=new Color[size*size];
        var rim=new Color(100,60,33);
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++){
            float u=(x+.5f-half)/half,v=(y+.5f-half)/half;
            float distance=MathF.Sqrt(u*u+v*v);
            if(distance>1)continue;
            float turn=MathF.Atan2(v,u)/MathHelper.TwoPi;
            if(turn<0)turn++;
            float ticket=turn*100;
            int sector=sectors[Math.Min(99,(int)ticket)];
            float texture=((x*83+y*47+x*y*11)&31)/31f;
            var lacquer=Color.Lerp(palette[sector],Color.White,.04f+texture*.035f);
            float position=ticket-starts[sector];
            float seam=Math.Min(position,wheel[sector].Percent-position)*MathHelper.TwoPi*half*distance/100;
            lacquer=Color.Lerp(lacquer,rim,Math.Clamp(1-seam,0,1)*.45f);
            pixels[y*size+x]=Color.Lerp(lacquer,rim,Math.Max(0,distance-.88f)*2.25f);
        }
        var face=new Texture2D(GraphicsDevice,size,size);
        face.SetData(pixels);
        _industryEnamelColors=palette;
        return face;
    }

    private Texture2D CreateIndustryBrass()
    {
        const int size=80;
        var pixels=new Color[size*size];
        var shade=new Color(109,67,30);
        var light=new Color(255,222,149);
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++){
            float u=(x+.5f-size/2f)/(size/2f),v=(y+.5f-size/2f)/(size/2f);
            float distance=MathF.Sqrt(u*u+v*v);
            if(distance>1)continue;
            float bevel=distance>.78f?1-distance:.38f;
            float reflection=MathF.Pow(Math.Max(0,1-MathF.Sqrt((u+.31f)*(u+.31f)+(v+.42f)*(v+.42f))),2);
            pixels[y*size+x]=Color.Lerp(shade,light,Math.Clamp(bevel+reflection,0,1));
        }
        var brass=new Texture2D(GraphicsDevice,size,size);
        brass.SetData(pixels);
        return brass;
    }

    private void DrawIndustryPointer(Point anchor)
    {
        var dark=new Color(99,56,26);
        var gold=new Color(204,146,66);
        var highlight=new Color(255,219,135);
        _ui.Box(new(anchor.X-30,anchor.Y-58,60,34),dark);
        _ui.Box(new(anchor.X-28,anchor.Y-60,56,31),gold);
        _ui.Box(new(anchor.X-25,anchor.Y-58,50,3),highlight);
        foreach(int x in new[]{anchor.X-21,anchor.X+21})
        foreach(int y in new[]{anchor.Y-51,anchor.Y-36})
            _spriteBatch.Draw(_industryBrass,new Rectangle(x-4,y-4,8,8),Color.White);
        for(int y=anchor.Y-53;y<anchor.Y-14;y+=5){
            _ui.Box(new(anchor.X-9,y,18,4),dark);
            _ui.Box(new(anchor.X-8,y,16,2),highlight);
        }
        for(int y=0;y<40;y++){
            int width=Math.Max(1,22-y/2);
            _ui.Box(new(anchor.X-width+2,anchor.Y-13+y,width*2,2),dark);
            _ui.Box(new(anchor.X-width,anchor.Y-15+y,width*2,1),gold);
            _ui.Box(new(anchor.X-width,anchor.Y-15+y,width,1),highlight);
        }
    }
}
