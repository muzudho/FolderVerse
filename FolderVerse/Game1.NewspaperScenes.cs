namespace FolderVerse;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private readonly Dictionary<string,Texture2D> _paperSceneArt=new();
    private static readonly string[] PaperMailboxKinds={"american","british","japanese","french"};
    private int _paperMailboxKind;
    private bool _paperBreakfast,_paperCarrying;
    private void BeginPaperScenes()
    {
        // Presentation randomness must not consume the simulation's random sequence.
        ResetPaperTileSnapshots();_paperTilePattern=new FallingTilePattern(Random.Shared.Next());_paperLastTileBeat=-1;
        _paperMailboxKind=Random.Shared.Next(PaperMailboxKinds.Length);
        _paperBreakfast=Random.Shared.Next(2)==0;_paperCarrying=Random.Shared.Next(2)==0;
    }
    private Texture2D PaperSceneArt(string name)
    {
        if(_paperSceneArt.TryGetValue(name,out var cached))return cached;
        using var file=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Newspaper",name+"-v001.png"));
        var image=Texture2D.FromStream(GraphicsDevice,file);var pixels=new Color[image.Width*image.Height];image.GetData(pixels);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color(c.R*c.A/255,c.G*c.A/255,c.B*c.A/255,(int)c.A);}
        image.SetData(pixels);_paperSceneArt.Add(name,image);return image;
    }
    private void DrawPaperSceneBackground(string name,Rectangle area,float alpha=1)
    {
        var image=PaperSceneArt(name);
        _spriteBatch.Draw(image,area,PortraitRenderer.FitSource(new(0,0,image.Width,image.Height),area),Color.White*alpha);
    }
    private void DrawPaperScenery(bool breakfast)
    {
        string kind=PaperMailboxKinds[_paperMailboxKind];
        float inside=breakfast?1:0;
        if(inside<1){
            DrawPaperSceneBackground("mailbox-"+kind,PortraitHudLayout.Background);
            float taken=(float)Math.Clamp((_paperAge-.65)/.25,0,1);
            DrawPaperSceneBackground("mailbox-"+kind+"-empty",PortraitHudLayout.Background,taken);
            DrawPaperSceneBackground("restaurant-wide",PortraitHudLayout.MainPanel);
            // The four poses reach different heights; all stay inside the left pane.
            var person=_paperMailboxKind switch{
                1=>new Rectangle(60,145,566,850),
                2=>new Rectangle(130,245,466,700),
                3=>new Rectangle(65,155,566,850),
                _=>new Rectangle(75,90,566,850)
            };
            person.X+=(int)(24*(1-taken));
            var actor=PaperSceneArt("servant-mailbox-"+kind);
            _spriteBatch.Draw(actor,person,Color.White*taken);
        }
        if(inside>0){
            DrawPaperSceneBackground("restaurant",PortraitHudLayout.Background,inside);
            DrawPaperSceneBackground(_paperBreakfast?"table-breakfast":"table-empty",PortraitHudLayout.MainPanel,inside);
            bool carrying=_paperCarrying;
            var actor=PaperSceneArt(carrying?"breakfast-carrying":"breakfast-buying");
            // Preserve the sprite's aspect ratio, with a small living movement.
            int sway=(int)(Math.Sin(_paperAge*1.2)*3);
            _spriteBatch.Draw(actor,new Rectangle(65+sway,85,560,840),Color.White*inside);
        }
        PortraitHudLayout.DrawSplitter(_ui);
    }
    private void DisposePaperScenes(){ResetPaperTileSnapshots();_paperTileRenderer?.Dispose();_paperTileSound?.Dispose();foreach(var image in _paperSceneArt.Values)image.Dispose();_paperSceneArt.Clear();}
}