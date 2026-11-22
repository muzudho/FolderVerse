namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private OverviewTile[] _conquestTiles=Array.Empty<OverviewTile>();
    private readonly HashSet<int> _conquestRevealed=new();
    private static Rectangle ConquestBoard=>new(820,280,980,700); // Inside: 7:5.
    private static Rectangle ConquestMapSpace=>new(680,280,550,580);
    private static Rectangle ConquestFormulaSpace=>new(1250,380,610,410);
    private static Rectangle ConquestTreeSpace=>Rectangle.Empty;
    private void DrawConquestMosaic(){
        DrawConquestLocationBoard();
        var ranking=_setup.Relations.ProvisionalRanking(_setup).ToDictionary(r=>r.Ruler);
        bool calculating=!ConquestCard.Reminder && (_conquestStage==3 || _conquestStage==4 && _conquestAge<_conquestTally.FaceStart);
        foreach(var tile in _conquestTiles.Where(t=>_conquestRevealed.Contains(t.Slot) || calculating && t.Slot==ConquestCard.Ruler)){
            int ruler=tile.Slot;var board=ConquestBoard;var r=new Rectangle(board.X+(tile.X-8)*board.Width/20,board.Y+tile.Y*board.Height/20,tile.Width*board.Width/20,tile.Height*board.Height/20);bool focus=ruler==ConquestCard.Ruler;
            bool pending=focus && calculating;
            bool revealed=_conquestRevealed.Contains(ruler) && !pending;
            _ui.Box(r,focus && !calculating?new Color(255,228,140):pending?new Color(35,46,59):_setup.OwnerColor(ruler));
            var image=new Rectangle(r.X+4,r.Y+4,Math.Max(1,r.Width-8),Math.Max(1,r.Height-8));
            if(pending){_ui.Box(image,new Color(19,30,43));DrawConquestQuestion(image);}
            else _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],image,_setup.Relations.Powered[ruler],focalPoint:new Vector2(.5f,.3f));
            if(!revealed)continue;
            bool powered=_setup.Relations.Powered[ruler];
            if(!powered || !focus)_ui.Box(image,Color.Black*(powered?.25f:.60f));
            bool subordinate=_setup.Relations.Superiors[ruler]>=0;
            int subordinateCells=powered && !subordinate?ranking[ruler].Cells-ConquestPresentation.OwnCells(_setup,ruler).Length:0;
            if(powered && !subordinate){
                var counterArea=image;
                if(subordinateCells>0)counterArea.Height=Math.Max(1,counterArea.Height-25);
                var digits=_counter.Draw(_spriteBatch,ranking[ruler].Cells,counterArea,_setup.OwnerColor(ruler));
                DrawSectorChange(digits,image,ruler,ranking[ruler].Cells);
            }else{
                int width=Math.Min(56,image.Width),height=Math.Clamp(image.Height-25,1,28);
                var badge=new Rectangle(image.Right-width,image.Bottom-height,width,height);
                _ui.Box(badge,Color.Black*.75f);
                string status=powered?"手下":"脱落";
                var textSize=_font.MeasureString(status);
                _ui.Center(status,badge,Math.Min(.36f,Math.Min(width/textSize.X,height/textSize.Y)),Cream);
            }
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
        int eliminated=Enumerable.Range(0,_setup.ActiveCount).Count(r=>!_setup.Relations.Powered[r]);
        int followers=Enumerable.Range(0,_setup.ActiveCount).Count(r=>_setup.Relations.Powered[r] && _setup.Relations.Superiors[r]>=0);
        _ui.Center($"脱落 {eliminated}名 / 手下 {followers}名",new(820,982,980,30),.40f,Cream);
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
