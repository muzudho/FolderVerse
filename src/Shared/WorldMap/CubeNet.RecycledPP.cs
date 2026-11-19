namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class CubeNet
{
    private Texture2D _recycledPPMapArt;
    private void DrawRecycledPPMapArt(UiPainter ui,Vector2 center,float width,bool depleted)
    {
        if(_recycledPPMapArt==null){
            using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","recycled-pp-facility-map-v001.png"));
            _recycledPPMapArt=Texture2D.FromStream(ui.Batch.GraphicsDevice,stream);
            var pixels=new Color[_recycledPPMapArt.Width*_recycledPPMapArt.Height];
            _recycledPPMapArt.GetData(pixels);
            for(int i=0;i<pixels.Length;i++)pixels[i]=Color.FromNonPremultiplied(pixels[i].ToVector4());
            _recycledPPMapArt.SetData(pixels);
        }
        float scale=width/_recycledPPMapArt.Width;
        ui.Batch.Draw(_recycledPPMapArt,center,null,depleted?new Color(115,115,115):Color.White,
            0,new Vector2(_recycledPPMapArt.Width*.5f,_recycledPPMapArt.Height*.65f),scale,SpriteEffects.None,0);
    }
}
