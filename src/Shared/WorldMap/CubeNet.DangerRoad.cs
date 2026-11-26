namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class CubeNet
{
    private readonly Texture2D[] _dangerRoadTextures=new Texture2D[2];
    // A cached transparent raster strip: one quad per existing road segment,
    // regardless of the number of teeth. No zigzag geometry is added.
    private void DrawDangerRoad(UiPainter ui,Vector2 a,Vector2 b,ref float phase,bool wide=false)
    {
        var delta=b-a;float length=delta.Length();if(length<.01f)return;
        int style=wide?1:0;
        var texture=_dangerRoadTextures[style];
        if(texture==null || texture.IsDisposed){
            const int width=512,height=40;var pixels=new Color[width*height];
            for(int x=0;x<width;x++){
                float wave=(x%32)/32f;
                float y=15+10*(wave<.5f?wave*2:2-wave*2);
                for(int row=0;row<height;row++){
                    // Half-scale texture: 6px globe roads, 2px flat-map roads.
                    float distance=Math.Abs(row-y),radius=wide?6:2;
                    pixels[row*width+x]=distance<radius?new Color(235,68,66):distance<radius+2?new Color(85,24,35):Color.Transparent;
                }
            }
            texture=new Texture2D(ui.Batch.GraphicsDevice,width,height);
            texture.SetData(pixels);_dangerRoadTextures[style]=texture;
        }
        int start=(int)phase%32;
        int samples=Math.Clamp((int)MathF.Ceiling(length*2),1,480);
        var source=new Rectangle(start,0,samples,40);
        ui.Batch.Draw(texture,(a+b)/2+new Vector2(0,ui.OffsetY),source,Color.White,
            MathF.Atan2(delta.Y,delta.X),new Vector2(samples/2f,20),new Vector2((length+.5f)/samples,.5f),SpriteEffects.None,0);
        phase+=length*2;
    }
}
