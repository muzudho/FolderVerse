namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _primaryActionArt,_guideActionArt;
    private static Rectangle PrimaryActionArtBounds(Rectangle r)=>new(r.Center.X-32,r.Y+8,64,64);
    private void DrawPrimaryActionCaption(Rectangle r,string label,Color ink){
        var text=new Rectangle(r.X+24,r.Bottom-24,r.Width-28,20);
        _ui.Center(label,text,Math.Min(.34f,(text.Width-2)/Math.Max(1,_font.MeasureString(label).X)),ink);
    }
    private void DrawPrimaryActionSurface(Rectangle r,Color nation){
        var shadow=r;shadow.Y+=4;PrimaryRoundBox(shadow,new Color(7,23,35));
        PrimaryRoundBox(r,Color.Lerp(nation,Color.Black,.55f));
        const int radius=11;
        for(int y=2;y<r.Height-2;y++){
            int edge=y<radius?radius-y:y>=r.Height-radius?y-(r.Height-radius-1):0;
            int inset=2+(edge>0?radius-(int)MathF.Sqrt(Math.Max(0,radius*radius-edge*edge)):0);
            float t=y/(float)r.Height;
            var color=t<.5f?Color.Lerp(Color.Lerp(nation,Color.White,.26f),nation,t*2):Color.Lerp(nation,Color.Lerp(nation,Color.Black,.24f),(t-.5f)*2);
            _ui.Box(new(r.X+inset,r.Y+y,r.Width-inset*2,1),color);
        }
        PrimaryRoundBox(new(r.X+14,r.Y+4,r.Width-28,3),Color.White*.22f,1);
    }
    private static int PrimaryActionArtIndex(int action)=>action switch{
        -1=>0,1=>1,2=>2,3=>3,LongMapAction=>4,4=>5,5=>6,FunctionTransferAction=>7,TradeAction=>8,StratagemAction=>9,
        _=>throw new ArgumentOutOfRangeException(nameof(action))
    };
    private void DrawPrimaryActionArt(int action,Rectangle bounds,bool enabled){
        var tint=Color.Lerp(_setup.OwnerColor(_setup.PlayerSlot),Cream,.80f)*(enabled?1:.35f);
        if(action==-1){
            if(_guideActionArt==null){
                using var guideFile=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Ui","guide-welcome-neutral-v001.png"));
                _guideActionArt=Texture2D.FromStream(GraphicsDevice,guideFile);
                var guidePixels=new Color[_guideActionArt.Width*_guideActionArt.Height];_guideActionArt.GetData(guidePixels);
                for(int i=0;i<guidePixels.Length;i++){
                    var c=guidePixels[i];int gray=(int)MathF.Round((c.R*.2126f+c.G*.7152f+c.B*.0722f)*c.A/255);
                    guidePixels[i]=new Color(gray,gray,gray,c.A);
                }
                _guideActionArt.SetData(guidePixels);
            }
            _spriteBatch.Draw(_guideActionArt,bounds,tint);return;
        }
        if(_primaryActionArt==null){
            using var file=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Ui","primary-actions-neutral-atlas-v001.png"));
            _primaryActionArt=Texture2D.FromStream(GraphicsDevice,file);
            // The source remains an achromatic straight-alpha master. Premultiply only the runtime texture.
            var pixels=new Color[_primaryActionArt.Width*_primaryActionArt.Height];_primaryActionArt.GetData(pixels);
            for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color(c.R*c.A/255,c.G*c.A/255,c.B*c.A/255,(int)c.A);}
            _primaryActionArt.SetData(pixels);
        }
        int index=PrimaryActionArtIndex(action),width=_primaryActionArt.Width/4,height=_primaryActionArt.Height/3;
        var source=new Rectangle(index%4*width,index/4*height,width,height);
        _spriteBatch.Draw(_primaryActionArt,bounds,source,tint);
    }
}
