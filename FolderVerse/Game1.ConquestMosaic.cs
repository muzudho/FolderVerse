namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private OverviewTile[] _conquestTiles=Array.Empty<OverviewTile>();
    private readonly HashSet<int> _conquestRevealed=new();
    private static Rectangle ConquestGridBounds(int x,int y,int w,int h)=>new(54+x*1806/28,112+y*900/20,(x+w)*1806/28-x*1806/28,(y+h)*900/20-y*900/20);
    private static Rectangle ConquestMapSpace=>ConquestGridBounds(0,0,8,8);
    private static Rectangle ConquestFormulaSpace=>ConquestGridBounds(0,8,8,8);
    private static Rectangle ConquestTreeSpace=>ConquestGridBounds(0,16,8,4);
    private void DrawConquestMosaic(){
        for(int y=0;y<20;y++)for(int x=0;x<28;x++){
            var cell=ConquestGridBounds(x,y,1,1);_ui.Box(cell,new Color(19,30,43));
            _ui.Box(new(cell.X,cell.Y,cell.Width,y%4==0?2:1),new Color(51,69,86));
            _ui.Box(new(cell.X,cell.Y,x%4==0?2:1,cell.Height),new Color(51,69,86));
        }
        var ranking=_setup.Relations.ProvisionalRanking(_setup).ToDictionary(r=>r.Ruler);
        foreach(var tile in _conquestTiles.Where(t=>_conquestRevealed.Contains(t.Slot))){
            int ruler=tile.Slot;var r=tile.Bounds;bool focus=ruler==ConquestCard.Ruler;
            _ui.Box(r,focus?new Color(255,228,140):_setup.OwnerColor(ruler));
            var image=new Rectangle(r.X+4,r.Y+4,Math.Max(1,r.Width-8),Math.Max(1,r.Height-8));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],image,_setup.Relations.Powered[ruler],focalPoint:new Vector2(.5f,.3f));
            if(!focus)_ui.Box(image,Color.Black*.25f);
            int subordinateCells=ranking[ruler].Cells-ConquestPresentation.OwnCells(_setup,ruler).Length;
            var counterArea=image;
            if(subordinateCells>0)counterArea.Height=Math.Max(1,counterArea.Height-25);
            _counter.Draw(_spriteBatch,ranking[ruler].Cells,counterArea,_setup.OwnerColor(ruler));
            if(subordinateCells>0){
                string detail=$"（内、手下 {subordinateCells} sectors）";
                var detailArea=new Rectangle(image.X,image.Bottom-25,image.Width,25);
                _ui.Box(detailArea,Color.Black*.7f);
                _ui.Center(detail,detailArea,Math.Min(.30f,(image.Width-8)/_font.MeasureString(detail).X),Cream);
            }
            string label=$"{ranking[ruler].Rank}位 / {_setup.ConquerorNames[ruler]}";
            _ui.Box(new(r.X+4,r.Y+4,r.Width-8,25),Color.Black*.7f);
            _ui.Center(label,new(r.X+4,r.Y+4,r.Width-8,25),Math.Min(.35f,(r.Width-12)/_font.MeasureString(label).X),Cream);
        }
        _ui.Box(ConquestMapSpace,Color.Black);_ui.Box(ConquestFormulaSpace,new Color(25,34,46));_ui.Box(ConquestTreeSpace,new Color(19,30,43));
    }
}
