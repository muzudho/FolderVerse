namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _popupHintPaper,_popupHintStamps;

    private static Rectangle PopupHintStampBounds(Rectangle paper)
    {
        bool compact=paper.Height<64;
        int width=Math.Min(132,Math.Min(paper.Width/5,(paper.Height-20)*3));
        int height=width/3;
        return new(paper.Right-(compact?14:26)-width,paper.Bottom-(compact?9:16)-height,width,height);
    }

    private static Rectangle PopupHintTextBounds(Rectangle paper)
    {
        int left=paper.X+(paper.Height<64?14:28);
        return new(left,paper.Y,PopupHintStampBounds(paper).Left-left-10,paper.Height);
    }

    private void DrawPopupHintLine(string text,Rectangle paper,int top,float maxScale)
    {
        var content=PopupHintTextBounds(paper);
        float scale=Math.Min(maxScale,content.Width/Math.Max(1,_font.MeasureString(text).X));
        _ui.Text(text,new(content.X,paper.Y+top),scale,RulebookInk);
    }

    private void DrawPopupHintPaper(Rectangle paper)
    {
        _popupHintPaper??=LoadUiTexture("popup-hint-paper-v001.png");
        _popupHintStamps??=LoadUiTexture("popup-hint-stamps-v001.png");
        var shadow=paper;
        shadow.Offset(4,6);
        DrawPopupHintFrame(shadow,Color.Black*.32f);
        DrawPopupHintFrame(paper,Color.White);
        _spriteBatch.Draw(_popupHintStamps,PopupHintStampBounds(paper),Color.White);
    }

    private void DrawPopupHintFrame(Rectangle paper,Color tint)
    {
        const int sourceCorner=24;
        int corner=Math.Min(22,Math.Min(paper.Width,paper.Height)/3);
        Span<int> sourceX=stackalloc int[]{0,sourceCorner,_popupHintPaper.Width-sourceCorner,_popupHintPaper.Width};
        Span<int> sourceY=stackalloc int[]{0,sourceCorner,_popupHintPaper.Height-sourceCorner,_popupHintPaper.Height};
        Span<int> targetX=stackalloc int[]{paper.X,paper.X+corner,paper.Right-corner,paper.Right};
        Span<int> targetY=stackalloc int[]{paper.Y,paper.Y+corner,paper.Bottom-corner,paper.Bottom};
        for(int row=0;row<3;row++)
        for(int col=0;col<3;col++)
        {
            var source=new Rectangle(sourceX[col],sourceY[row],sourceX[col+1]-sourceX[col],sourceY[row+1]-sourceY[row]);
            var target=new Rectangle(targetX[col],targetY[row],targetX[col+1]-targetX[col],targetY[row+1]-targetY[row]);
            _spriteBatch.Draw(_popupHintPaper,target,source,tint);
        }
    }
}
