namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _spaceBackdrop;

    // A fixed, unlit scenic layer. Never consumes the world's generation random stream.
    private void DrawSpaceBackdrop(Rectangle area)
    {
        if(_spaceBackdrop==null){
            const int width=960,height=540;
            var pixels=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                float u=x/(float)width,v=y/(float)height;
                float cloud=0,amplitude=.55f,frequency=3;
                for(int octave=0;octave<5;octave++){
                    cloud+=SpaceNoise(u*frequency,v*frequency,octave)*amplitude;
                    frequency*=2;amplitude*=.5f;
                }
                float ribbon=v-(.32f+.16f*MathF.Sin(u*6.5f));
                float haze=MathF.Exp(-ribbon*ribbon*20)*MathF.Max(0,cloud-.19f);
                float purple=.5f+.5f*MathF.Sin(u*5.2f+v*2);
                pixels[y*width+x]=new Color((int)(4+haze*(24+22*purple)),
                    (int)(10+haze*(35-15*purple)),(int)(22+haze*67));
            }
            var random=new Random(85921);
            for(int i=0;i<420;i++){
                int x=random.Next(width),y=random.Next(height);
                float light=.25f+(float)random.NextDouble()*.55f;
                pixels[y*width+x]=Color.Lerp(new Color(149,183,225),new Color(255,225,185),(float)random.NextDouble())*light;
            }
            _spaceBackdrop=new Texture2D(GraphicsDevice,width,height);
            _spaceBackdrop.SetData(pixels);
        }
        // Source crop preserves the nebula's aspect ratio even in narrow inspector map views.
        float aspect=area.Width/(float)Math.Max(1,area.Height);
        int sw=_spaceBackdrop.Width,sh=_spaceBackdrop.Height;
        if(aspect<sw/(float)sh)sw=Math.Max(1,(int)(sh*aspect));else sh=Math.Max(1,(int)(sw/aspect));
        var source=new Rectangle((_spaceBackdrop.Width-sw)/2,(_spaceBackdrop.Height-sh)/2,sw,sh);
        _spriteBatch.Draw(_spaceBackdrop,area,source,Color.White);
    }

    private static float SpaceNoise(float x,float y,int octave)
    {
        int ix=(int)MathF.Floor(x),iy=(int)MathF.Floor(y);
        float fx=x-ix,fy=y-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
        float Sample(int a,int b){unchecked{uint h=(uint)(a*374761393+b*668265263+octave*1447+85921);h=(h^(h>>13))*1274126177;return (h&65535)/65535f;}}
        return MathHelper.Lerp(MathHelper.Lerp(Sample(ix,iy),Sample(ix+1,iy),fx),
            MathHelper.Lerp(Sample(ix,iy+1),Sample(ix+1,iy+1),fx),fy);
    }
}
