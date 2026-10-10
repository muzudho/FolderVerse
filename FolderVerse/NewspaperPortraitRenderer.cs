namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

// Convert once, rather than reading pixels during each battle frame.
public sealed class NewspaperPortraitRenderer : IDisposable
{
    private readonly ContentManager _content;
    private readonly Dictionary<string,Texture2D> _photos=new();
    private static readonly string[] Kinds={"formal","paparazzi","friends","id"};
    public NewspaperPortraitRenderer(ContentManager content)=>_content=content;
    public bool Draw(SpriteBatch batch,ConquerorLook look,Rectangle destination,string kind="formal",bool allowFallback=true)
    {
        string root=Path.Combine(AppContext.BaseDirectory,"Content","Images","Portraits","newspaper");
        string stem="character-"+(look.BaseId+1).ToString("00");
        string file=Path.Combine(root,stem+"-"+kind+".png");
        if(!File.Exists(file) && allowFallback)file=Path.Combine(root,stem+"-formal.png");
        bool photo=File.Exists(file);
        if(!photo && !allowFallback)return false;
        string key=photo?file:stem+"-fallback-"+(look.VariantId+1);
        if(!_photos.TryGetValue(key,out var texture))
        {
            Texture2D source;
            if(photo){using var stream=File.OpenRead(file);source=Texture2D.FromStream(batch.GraphicsDevice,stream);}
            else source=_content.Load<Texture2D>("Images/Portraits/conqueror-creation/tiles/"+stem+"-"+(look.VariantId+1));
            var pixels=new Color[source.Width*source.Height];source.GetData(pixels);
            for(int i=0;i<pixels.Length;i++)
            {
                var c=pixels[i];byte gray=(byte)((c.R*299+c.G*587+c.B*114)/1000);
                pixels[i]=new Color(gray,gray,gray,c.A);
            }
            texture=new Texture2D(batch.GraphicsDevice,source.Width,source.Height);texture.SetData(pixels);
            if(photo)source.Dispose();_photos.Add(key,texture);
        }
        batch.Draw(texture,destination,PortraitRenderer.FitSource(new(0,0,texture.Width,texture.Height),destination),Color.White);
        return true;
    }
    public void Dispose(){foreach(var texture in _photos.Values)texture.Dispose();_photos.Clear();}
}
