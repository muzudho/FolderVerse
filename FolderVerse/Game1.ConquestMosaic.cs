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
        bool calculating=!ConquestCard.Reminder && (_conquestStage==3 || _conquestStage==4 && _conquestAge<_conquestTally.FaceStart);
        foreach(var tile in _conquestTiles.Where(t=>_conquestRevealed.Contains(t.Slot) || calculating && t.Slot==ConquestCard.Ruler)){
            int ruler=tile.Slot;var r=tile.Bounds;bool focus=ruler==ConquestCard.Ruler;
            bool pending=focus && calculating;
            bool revealed=_conquestRevealed.Contains(ruler) && !pending;
            _ui.Box(r,focus && !calculating?new Color(255,228,140):pending?new Color(35,46,59):_setup.OwnerColor(ruler));
            var image=new Rectangle(r.X+4,r.Y+4,Math.Max(1,r.Width-8),Math.Max(1,r.Height-8));
            if(pending){_ui.Box(image,new Color(19,30,43));DrawConquestQuestion(image);}
            else _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],image,_setup.Relations.Powered[ruler],focalPoint:new Vector2(.5f,.3f));
            if(!revealed)continue;
            if(!focus)_ui.Box(image,Color.Black*.25f);
            int subordinateCells=ranking[ruler].Cells-ConquestPresentation.OwnCells(_setup,ruler).Length;
            var counterArea=image;
            if(subordinateCells>0)counterArea.Height=Math.Max(1,counterArea.Height-25);
            _counter.Draw(_spriteBatch,ranking[ruler].Cells,counterArea,_setup.OwnerColor(ruler));
            if(subordinateCells>0){
                string detail=$"（内、手下 {subordinateCells} {ToyCounterRenderer.SectorUnit(subordinateCells)}）";
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
    private void DrawConquestQuestion(Rectangle image){
        var measure=_font.MeasureString("?");
        float scale=Math.Min(image.Width*.75f/measure.X,image.Height*.8f/measure.Y);
        var position=new Vector2(image.Center.X-measure.X*scale/2,image.Center.Y-measure.Y*scale/2);
        int thickness=Math.Max(2,(int)(Math.Min(image.Width,image.Height)*.025f));
        // Repeated offsets make the existing font extra bold at every portrait size.
        for(int y=-thickness;y<=thickness;y++)for(int x=-thickness;x<=thickness;x++)
            if(x*x+y*y<=thickness*thickness)_ui.Text("?",position+new Vector2(x,y),scale,Color.White);
    }

}
