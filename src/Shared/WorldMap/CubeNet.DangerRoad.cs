namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class CubeNet
{
    private Texture2D _dangerRoadTexture;
    // A cached transparent raster strip: one quad per existing road segment,
    // regardless of the number of teeth. No zigzag geometry is added.
    private void DrawDangerRoad(UiPainter ui,Vector2 a,Vector2 b,ref float phase)
    {
        var delta=b-a;float length=delta.Length();if(length<.01f)return;
        if(_dangerRoadTexture==null || _dangerRoadTexture.IsDisposed){
            const int width=512,height=16;var pixels=new Color[width*height];
            for(int x=0;x<width;x++){
                float wave=(x%32)/32f;
                float y=3+10*(wave<.5f?wave*2:2-wave*2);
                for(int row=0;row<height;row++){
                    float distance=Math.Abs(row-y);
                    pixels[row*width+x]=distance<1.8f?new Color(235,68,66):distance<3?new Color(85,24,35):Color.Transparent;
                }
            }
            _dangerRoadTexture=new Texture2D(ui.Batch.GraphicsDevice,width,height);
            _dangerRoadTexture.SetData(pixels);
        }
        int start=(int)phase%32;
        int samples=Math.Clamp((int)MathF.Ceiling(length*2),1,480);
        var source=new Rectangle(start,0,samples,16);
        ui.Batch.Draw(_dangerRoadTexture,(a+b)/2+new Vector2(0,ui.OffsetY),source,Color.White,
            MathF.Atan2(delta.Y,delta.X),new Vector2(samples/2f,8),new Vector2((length+.5f)/samples,.5f),SpriteEffects.None,0);
        phase+=length*2;
    }
}
