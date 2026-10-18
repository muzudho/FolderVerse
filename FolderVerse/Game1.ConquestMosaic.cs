namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private CharacterTile[] _conquestTiles=Array.Empty<CharacterTile>();
    private bool ConquestMosaicVisible=>_conquestStage==4 && !ConquestCard.Reminder && _conquestAge>=_conquestTally.RankStart;
    private static Rectangle ConquestMosaicBounds(CharacterTile tile){var r=tile.Bounds;return new(510+(r.X-54)*1350/1806,125+(r.Y-112)*850/900,r.Width*1350/1806,r.Height*850/900);}
    private void DrawConquestMosaic(){
        var ranking=_setup.Relations.ProvisionalRanking(_setup).ToDictionary(r=>r.Ruler);
        foreach(var tile in _conquestTiles.Where(t=>t.Slot>=0)){
            int ruler=tile.Slot;var r=ConquestMosaicBounds(tile);bool focus=ruler==ConquestCard.Ruler;
            _ui.Box(r,focus?new Color(255,228,140):_setup.OwnerColor(ruler));
            var image=new Rectangle(r.X+4,r.Y+4,Math.Max(1,r.Width-8),Math.Max(1,r.Height-8));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],image,_setup.Relations.Powered[ruler]);
            if(!focus)_ui.Box(image,Color.Black*.4f);
            _counter.Draw(_spriteBatch,ranking[ruler].Cells,image,_setup.OwnerColor(ruler));
            string label=$"{ranking[ruler].Rank}位 / Entry {ruler+1}";
            _ui.Box(new(r.X+4,r.Y+4,r.Width-8,25),Color.Black*.7f);
            _ui.Center(label,new(r.X+4,r.Y+4,r.Width-8,25),Math.Min(.35f,(r.Width-12)/_font.MeasureString(label).X),Cream);
        }
    }
}
