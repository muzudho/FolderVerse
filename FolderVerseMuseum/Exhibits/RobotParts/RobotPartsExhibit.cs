namespace FolderVerseMuseum;

using Microsoft.Xna.Framework;

internal sealed class RobotPartsExhibit
{
    private static readonly string[] Grades = { "安物", "正規", "高級" };
    private static readonly string[] Sections = { "頭", "胴", "脚" };
    private static readonly Color[] Paint = { new(193, 96, 84), new(82, 168, 190), new(107, 174, 124) };
    private readonly int[,] _grade = new int[3, 3];
    private (int Bay, int Part)? _drag;

    internal RobotPartsExhibit() => Reset();
    internal void Reset()
    {
        for (int bay = 0; bay < 3; bay++)
            for (int part = 0; part < 3; part++) _grade[bay, part] = bay;
        CancelDrag();
    }

    internal void CancelDrag() => _drag = null;

    private static Rectangle Slot(int bay, int part) => new(101 + bay * 261, 157 + part * 114, 218, 104);

    internal void Input(Point at, bool pressed, bool released)
    {
        if (!pressed && !released) return;
        for (int bay = 0; bay < 3; bay++)
            for (int part = 0; part < 3; part++)
            {
                if (!Slot(bay, part).Contains(at)) continue;
                if (pressed) _drag = (bay, part);
                else if (_drag is { } from && part == from.Part)
                    (_grade[bay, part], _grade[from.Bay, part]) = (_grade[from.Bay, part], _grade[bay, part]);
            }
        if (released) CancelDrag();
    }

    internal void Draw(Action<Rectangle, Color> box, Action<string, Rectangle, Color, float> text, Point pointer)
    {
        var paper = new Color(249, 229, 190);
        for (int bay = 0; bay < 3; bay++)
        {
            box(new Rectangle(91 + bay * 261, 104, 238, 399), new Color(36, 58, 71));
            text((bay + 1).ToString(), new Rectangle(101 + bay * 261, 110, 218, 40), paper, 1);
            for (int part = 0; part < 3; part++)
            {
                var slot = Slot(bay, part);
                bool target = slot.Contains(pointer) && (_drag == null || _drag.Value.Part == part);
                box(slot, target ? new Color(60, 102, 111) : new Color(24, 46, 59));
                int grade = _grade[bay, part];
                text(Sections[part], new Rectangle(slot.X + 6, slot.Y, 42, slot.Height), paper, 1);
                text(Grades[grade], new Rectangle(slot.Right - 63, slot.Y, 57, slot.Height), paper, .9f);
                if (_drag != (bay, part)) DrawPart(box, slot.Center, part, grade);
            }
        }
        if (_drag is { } held) DrawPart(box, pointer, held.Part, _grade[held.Bay, held.Part]);
    }

    // Preserve the original Legacy geometry and gold edging.
    private static void DrawPart(Action<Rectangle, Color> box, Point center, int part, int grade)
    {
        bool cheap = grade == 0, premium = grade == 2;
        var paint = Paint[grade];
        const int width = 96, height = 180;
        int x = center.X - width / 2;
        int y = center.Y - (part == 0 ? height / 8 : part == 1 ? height / 2 : height * 7 / 8);
        void Fill(Rectangle shape, Color color)
        {
            for (int row = shape.Top; row < shape.Bottom; row++)
            {
                var bounds = RobotPartSilhouette.Row(shape, row, cheap, premium);
                bool gold = premium && Math.Min(row - shape.Top, shape.Bottom - 1 - row) < Math.Max(1, shape.Height / 12);
                box(bounds, gold ? Color.Lerp(new Color(255, 215, 100), color, .2f) : color);
            }
        }
        switch (part)
        {
            case 0:
                Fill(new(x + width / 4, y, width / 2, height / 4), new Color(249, 229, 190));
                Fill(new(x + width / 5 + (cheap ? width / 10 : 0), y, width * (cheap ? 2 : 3) / 5, height / 12), paint);
                break;
            case 1:
                Fill(new(x + width / 6, y + height / 3, width * 2 / 3, height / 3), paint);
                break;
            case 2:
                Fill(new(x + width / 6, y + height * 3 / 4, width / 4, height / 4), paint);
                Fill(new(x + width * 7 / 12, y + height * 3 / 4, width / 4, height / 4), paint);
                break;
        }
    }

    internal void CheckSwap()
    {
        Input(Slot(0, 0).Center, true, false);
        Input(Slot(2, 1).Center, false, true);
        if (_grade[0, 0] != 0 || _grade[2, 1] != 2 || _drag != null)
            throw new InvalidOperationException("Cross-part drop changed inventory.");
        Input(Slot(0, 0).Center, true, false);
        Input(Slot(2, 0).Center, false, true);
        if (_grade[0, 0] != 2 || _grade[2, 0] != 0 || _grade[0, 1] != 0)
            throw new InvalidOperationException("Head swap failed.");
        Reset();
    }
}
