namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Color HangarWall = new(43, 67, 80);
    private static readonly Color HangarFloor = new(57, 77, 86);
    private static readonly Color HangarGuide = new(220, 173, 88);

    private void DrawHangarBay(Rectangle bay, bool hovered, bool canReserve)
    {
        var wall = hovered ? (canReserve ? new Color(54, 89, 102) : new Color(85, 54, 57)) : HangarWall;
        _ui.Box(bay, wall);
        _ui.Box(new(bay.X + 16, bay.Y + 32, bay.Width - 32, 128), new Color(30, 48, 60));
        for (int y = 38; y < 150; y += 14)
            _ui.Box(new(bay.X + 16, bay.Y + y, bay.Width - 32, 2), wall);

        _ui.Box(new(bay.X, bay.Y + 160, bay.Width, 100), HangarFloor);
        _ui.Box(new(bay.X, bay.Y + 201, bay.Width, 59), new Color(36, 54, 65));
        for (int side = -1; side <= 1; side += 2)
        {
            int post = side < 0 ? bay.Left : bay.Right - 10;
            _ui.Box(new(post, bay.Y + 12, 10, 148), new Color(70, 93, 103));
            _ui.Box(new(post + (side < 0 ? 7 : 0), bay.Y + 12, 3, 148), new Color(27, 46, 57));
            _ui.Tile(new(bay.Center.X + side * 75, bay.Y + 180), new(47, 3),
                side * -.9f, HangarGuide);
        }
        _ui.Box(new(bay.X, bay.Y + 201, bay.Width, 3), HangarGuide);
        _ui.Box(new(bay.X, bay.Y, bay.Width, 14), new Color(79, 105, 114));
        _ui.Box(new(bay.X, bay.Y + 14, bay.Width, 6), new Color(32, 51, 62));
        _ui.Box(new(bay.Center.X - 26, bay.Y + 6, 52, 4), new Color(180, 223, 221));
    }

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
