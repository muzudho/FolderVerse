namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void HierarchyRoundBox(Rectangle box,Color color)
    {
        int radius=Math.Max(2,Math.Min(10,box.Width/9));
        for(int y=0;y<box.Height;y++)
        {
            int dy=y<radius?radius-y-1:y>=box.Height-radius?y-(box.Height-radius):0;
            int inset=dy==0?0:radius-(int)Math.Sqrt(radius*radius-dy*dy);
            _ui.Box(new(box.X+inset,box.Y+y,Math.Max(1,box.Width-2*inset),1),color);
        }
    }
    private void DrawRulerFaceCard(int ruler,Rectangle box,int? rank=null,bool own=false)
    {
            HierarchyRoundBox(box,own?new Color(255,226,124):_setup.OwnerColor(ruler));
            var inside=new Rectangle(box.X+3,box.Y+3,box.Width-6,box.Height-6);HierarchyRoundBox(inside,new(27,49,62));
            int pad=Math.Max(1,box.Width/12),label=Math.Max(2,box.Width/4);
            var face=new Rectangle(box.X+pad,box.Y+pad,box.Width-2*pad,box.Height-2*pad);
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],face,_setup.Relations.Powered[ruler],faceOnly:true);
            string rankLabel=rank.HasValue?HierarchyLayout.Ordinal(rank.Value):"",number="#"+(ruler+1);
            float scale=Math.Min(label/(float)_font.LineSpacing,Math.Min(.46f,box.Width*.62f/Math.Max(_font.MeasureString(rankLabel).X,_font.MeasureString(number).X)));
            int labelWidth=Math.Min(box.Width-6,(int)Math.Ceiling(_font.MeasureString(rankLabel).X*scale)+8);
            if(rank.HasValue){_ui.Box(new(box.X+3,box.Y+3,labelWidth,label),new(16,33,44));_ui.Text(rankLabel,new(box.X+6,box.Y+3),scale,Cream);}
            int nw=(int)Math.Ceiling(_font.MeasureString(number).X*scale)+8;
            _ui.Box(new(box.Right-nw-3,box.Bottom-label-3,nw,label),new(16,33,44));_ui.Text(number,new(box.Right-nw,box.Bottom-label-3),scale,Cream);
    }
}
