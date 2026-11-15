namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawHangarContinuation()
    {
        if(_hangarZoom!=1)return;
        const int width = 22;
        var surround = new Color(27, 49, 62);
        for (int row = 0; row < 2; row++)
        {
            var first = HangarCell(row * HangarColumns);
            var last = HangarCell(row * HangarColumns + HangarColumns - 1);
            DrawEdge(first.Left - width - 2, first.Y, true, _hangarColumn > 0);
            DrawEdge(last.Right + 2, last.Y, false, _hangarColumn < HangarLastColumn);
        }

        void DrawEdge(int x, int y, bool left, bool continues)
        {
            if (!continues)
            {
                int post = left ? x + width - 10 : x;
                _ui.Box(new(post, y, 10, 260), new Color(53, 76, 88));
                _ui.Box(new(post + (left ? 7 : 0), y, 3, 260), new Color(76, 98, 106));
                return;
            }
            for (int distance = 0; distance < width; distance += 2)
            {
                int strip = left ? x + width - distance - 2 : x + distance;
                float fade = distance / (float)(width - 2);
                _ui.Box(new(strip, y, 2, 160), Color.Lerp(HangarWall, surround, fade));
                _ui.Box(new(strip, y, 2, 14), Color.Lerp(new Color(79, 105, 114), surround, fade));
                _ui.Box(new(strip, y + 160, 2, 44), Color.Lerp(HangarFloor, surround, fade));
                _ui.Box(new(strip, y + 201, 2, 3), Color.Lerp(HangarGuide, surround, fade));
                _ui.Box(new(strip, y + 204, 2, 56), Color.Lerp(new Color(36, 54, 65), surround, fade));
            }
            _ui.Box(new(left ? x + width - 4 : x, y + 20, 4, 140), new Color(70, 93, 103));
        }
    }
}
