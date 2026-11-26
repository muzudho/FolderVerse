namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public sealed partial class PortraitRenderer
{
    // A commemorative coin uses the existing portrait as a runtime engraved relief.
    public Texture2D MedallionRelief(GraphicsDevice device,ConquerorLook look,int size,Color metal){
        Texture2D source;Rectangle area;
        if(PortraitAssets.Version=="v3-standardized"){source=_individual[look.BaseId*6+look.VariantId];area=new(0,0,source.Width,source.Height);}
        else{source=_sheets[look.BaseId/6];area=SourceBounds(look,source.Width,source.Height);if(_normalized.TryGetValue(look.BaseId,out var replacement)){source=replacement;area=NormalizedSourceBounds(look.VariantId,source.Width,source.Height);}}
        area=FitSource(area,new(0,0,size,size));var input=new Color[area.Width*area.Height];source.GetData(0,area,input,0,input.Length);
        var height=new float[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){var c=input[Math.Min(area.Height-1,y*area.Height/size)*area.Width+Math.Min(area.Width-1,x*area.Width/size)];height[y*size+x]=(c.R*.2126f+c.G*.7152f+c.B*.0722f)/255;}
        var pixels=new Color[height.Length];float radius=size*.5f;
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float dx=x+.5f-radius,dy=y+.5f-radius;float alpha=Math.Clamp(radius-MathF.Sqrt(dx*dx+dy*dy),0,1);if(alpha<=0)continue;
            int at=y*size+x;float relief=height[at];float slope=height[Math.Max(0,y-1)*size+Math.Max(0,x-1)]-height[Math.Min(size-1,y+1)*size+Math.Min(size-1,x+1)];
            var shade=Color.Lerp(new Color(37,31,25),metal,Math.Clamp(.25f+relief*.7f+slope*.55f,0,1));pixels[at]=shade*alpha;
        }
        var result=new Texture2D(device,size,size);result.SetData(pixels);return result;
    }
}
