namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private readonly Texture2D[] _mastheads=new Texture2D[NewspaperProfiles.All.Length];
    private readonly Rectangle[] _mastheadBounds=new Rectangle[NewspaperProfiles.All.Length];
    private readonly Dictionary<string,(Texture2D Masthead,Texture2D Digits)> _nationalMastheads=new();
    private int NationalNewspaperCountry => _setup.PlayerSlot >= 0 && _setup.PlayerSlot < _setup.ActiveCount
        ? _setup.PlayerSlot : -1;

    private void DrawNewspaperMasthead(int edition,Rectangle area)
    {
        if(edition==NewspaperProfiles.NationalEdition){
            if(NationalNewspaperCountry>=0){DrawNationalNewspaperMasthead(area,NationalNewspaperCountry+1);return;}
            edition=1;
        }
        if(NewspaperProfiles.All[edition].Wanted){_ui.Center("WANTED",area,.95f,new Color(112,42,34));return;}
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
    private void DrawNationalNewspaperMasthead(Rectangle area,int entry)
    {
        var profile=_setup.NationalNewspaperFor(entry);
        if(!_nationalMastheads.TryGetValue(profile.Id,out var art)){
            Texture2D Load(string filename){
                using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Newspapers",filename));
                return Texture2D.FromStream(GraphicsDevice,stream);
            }
            var masthead=Load(profile.MastheadFile);
            try{art=(masthead,Load(profile.DigitsFile));}
            catch{masthead.Dispose();throw;}
            _nationalMastheads.Add(profile.Id,art);
        }
        float scale=Math.Min(area.Width/(float)art.Masthead.Width,area.Height/(float)art.Masthead.Height);
        var bounds=new Rectangle(0,0,(int)(art.Masthead.Width*scale),(int)(art.Masthead.Height*scale));
        bounds.Location=area.Center-bounds.Center;
        _spriteBatch.Draw(art.Masthead,bounds,Color.White);

        var gap=profile.EntryNumberArea;
        string number=entry.ToString(CultureInfo.InvariantCulture);
        const int width=NationalNewspaper.DigitWidth,height=NationalNewspaper.DigitHeight;
        float digitScale=scale*Math.Min((gap.Width-8f)/(width*number.Length),(gap.Height-12f)/height);
        int w=Math.Max(1,(int)MathF.Round(width*digitScale)),h=Math.Max(1,(int)MathF.Round(height*digitScale));
        int x=bounds.X+(int)(gap.Center.X*scale)-w*number.Length/2;
        int y=bounds.Y+(int)(gap.Center.Y*scale)-h/2;
        foreach(char digit in number){
            _spriteBatch.Draw(art.Digits,new Rectangle(x,y,w,h),new Rectangle((digit-'0')*width,0,width,height),Color.White);
            x+=w;
        }
    }
    private void DisposeNewspaperMastheads(){
        foreach(var texture in _mastheads)texture?.Dispose();
        foreach(var art in _nationalMastheads.Values){art.Masthead.Dispose();art.Digits.Dispose();}
        _nationalMastheads.Clear();
    }
}
