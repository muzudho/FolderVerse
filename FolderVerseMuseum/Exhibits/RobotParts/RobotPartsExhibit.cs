namespace FolderVerseMuseum;

using Microsoft.Xna.Framework;

internal sealed class RobotPartsExhibit
{
    private static readonly string[] Grades = { "安物", "正規", "高級" };
    private static readonly string[] Sections = { "頭", "胴", "脚" };
    private static readonly Color[] Paint = { new(193, 96, 84), new(82, 168, 190), new(107, 174, 124) };
    private readonly int[,] _grade = new int[3, 3];
    private (int Bay, int Part)? _drag;
    private int _view;
    private static readonly string[] Views = { "分割型", "頭/胴一体型", "胴/脚一体型" };
    internal bool IsIntegrated => _view != 0;
    private static Rectangle Tab(int index)=>new(101+index*261,86,218,38);

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
        if(pressed)
            for(int i=0;i<Views.Length;i++)
                if(Tab(i).Contains(at)){_view=i;CancelDrag();return;}
        if(IsIntegrated)return;
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
        for(int i=0;i<Views.Length;i++){
            var tab=Tab(i);
            box(tab,i==_view?new Color(70,118,126):new Color(36,58,71));
            text(Views[i],tab,paper,.9f);
        }
        for (int bay = 0; bay < 3; bay++)
        {
            box(new Rectangle(91 + bay * 261, 132, 238, 371), new Color(36, 58, 71));
            if(IsIntegrated){
                var area=new Rectangle(101+bay*261,147,218,330);
                box(area,new Color(24,46,59));
                text(Grades[bay],new Rectangle(area.X,area.Y+25,area.Width,36),paper,1);
                DrawPart(box,area.Center,0,bay,_view);
                continue;
            }
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
    private static void DrawPart(Action<Rectangle, Color> box, Point center, int part, int grade,int integrated=0)
    {
        bool cheap = grade == 0, premium = grade == 2;
        var paint = Paint[grade];
        const int width = 96, height = 180;
        int x = center.X - width / 2;
        int y = center.Y - (integrated==1?height/3:integrated==2?height*2/3:
            part == 0 ? height / 8 : part == 1 ? height / 2 : height * 7 / 8);
        void Fill(Rectangle shape, Color color)
        {
            for (int row = shape.Top; row < shape.Bottom; row++)
            {
                var bounds = RobotPartSilhouette.Row(shape, row, cheap, premium);
                bool gold = premium && Math.Min(row - shape.Top, shape.Bottom - 1 - row) < Math.Max(1, shape.Height / 12);
                box(bounds, gold ? Color.Lerp(new Color(255, 215, 100), color, .2f) : color);
            }
        }
        bool Has(int section)=>integrated==0?part==section:section==integrated-1 || section==integrated;
        if(Has(0))
        {
                Fill(new(x + width / 4, y, width / 2, height / 4), new Color(249, 229, 190));
                Fill(new(x + width / 5 + (cheap ? width / 10 : 0), y, width * (cheap ? 2 : 3) / 5, height / 12), paint);
        }
        if(Has(1)){
            int top=integrated==1?height/4:height/3;
            Fill(new(x+width/6,y+top,width*2/3,height*2/3-top),paint);
        }
        if(Has(2)){
            int top=integrated==2?height*2/3:height*3/4;
            Fill(new(x+width/6,y+top,width/4,height-top),paint);
            Fill(new(x+width*7/12,y+top,width/4,height-top),paint);
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
