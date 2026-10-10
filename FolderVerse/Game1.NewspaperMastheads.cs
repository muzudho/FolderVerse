namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private readonly Texture2D[] _mastheads=new Texture2D[NewspaperProfiles.All.Length];
    private readonly Rectangle[] _mastheadBounds=new Rectangle[NewspaperProfiles.All.Length];
    private void DrawNewspaperMasthead(int edition,Rectangle area)
    {
        if(_mastheads[edition]==null)
        {
            string path=Path.Combine(AppContext.BaseDirectory,"Content","Images","Newspapers","masthead-"+edition+".png");
            using var stream=File.OpenRead(path);
            var texture=Texture2D.FromStream(GraphicsDevice,stream);_mastheads[edition]=texture;
            var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
            int minX=texture.Width,minY=texture.Height,maxX=-1,maxY=-1;
            for(int y=0;y<texture.Height;y++)for(int x=0;x<texture.Width;x++)
            {
                if(pixels[y*texture.Width+x].A<64)continue;
                minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);
            }
            if(maxX<minX)throw new InvalidDataException("Empty newspaper masthead "+edition);
            _mastheadBounds[edition]=new(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        var source=_mastheadBounds[edition];float scale=Math.Min(area.Width/(float)source.Width,area.Height/(float)source.Height);
        var size=new Point(Math.Max(1,(int)(source.Width*scale)),Math.Max(1,(int)(source.Height*scale)));
        _spriteBatch.Draw(_mastheads[edition],new Rectangle(area.Center.X-size.X/2,area.Center.Y-size.Y/2,size.X,size.Y),source,Color.White);
    }
    private void DisposeNewspaperMastheads(){foreach(var texture in _mastheads)texture?.Dispose();}
}
