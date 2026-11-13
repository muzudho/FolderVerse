namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool FactoryAlbumOpen => _nodeFactoryOpen && _factoryPlanLine >= 0 &&
        _setup.EquipmentMode == RobotEquipmentMode.Materials;

    private static Rectangle FactoryAlbumCard(int index) =>
        new(111 + index % 6 * 286, 214 + index / 6 * 346, 268, 324);

    private void DrawFactoryAlbum()
    {
        if (_populationNode < 0 || _populationNode >= _setup.Nodes.All.Length) return;
        var node = _setup.Nodes.All[_populationNode];
        var factory = PlanningWorkshop(node.Id);
        var lines = FactoryScreenLines(factory, PendingFactoryWorkers(factory));
        if (_factoryPlanLine >= lines.Length) return;
        var line = lines[_factoryPlanLine];

        var ink = new Color(64, 44, 37);
        var paper = new Color(249, 231, 193);
        _ui.Box(new(0, 0, 1920, 1080), new Color(0, 0, 0, 174));
        _ui.Box(new(63, 137, 1794, 830), new Color(56, 36, 37));
        _ui.Box(new(77, 148, 1766, 800), new Color(224, 196, 152));
        _ui.Box(new(94, 157, 1732, 777), paper);
        _ui.Text($"生産ライン {_factoryPlanLine + 1} / 製造候補", new(113, 169), .55f, ink);
        _ui.Batch.Draw(_robotFactoryIcon, new Rectangle(1162, 166, 31, 31), Color.White);
        _ui.Text("産業ターン", new(1200, 175), .36f, ink);
        _ui.Text("閉じる：Esc / 枠の外をクリック", new(1400, 175), .36f, ink);

        Robot mixedPreview = null;
        for (int i = 0; i < line.PlanCount; i++)
        {
            var card = FactoryAlbumCard(i);
            bool selected = line.SelectedPlan == i;
            bool hover = card.Contains(_pointer);
            _ui.Box(new(card.X + 3, card.Y + 5, card.Width, card.Height), new Color(175, 143, 113));
            _ui.Box(card, selected ? new Color(101, 145, 132) : hover ? new Color(189, 136, 76) : new Color(204, 175, 136));
            _ui.Box(new(card.X + 4, card.Y + 4, card.Width - 8, card.Height - 8), new Color(255, 245, 220));
            var photo = new Rectangle(card.X + 14, card.Y + 14, card.Width - 28, 208);
            _ui.Box(photo, new Color(35, 53, 67));
            if (i == 3)
            {
                _ui.Center("製造しない", photo, .54f, Cream);
                continue;
            }

            var plan = line.PlanAt(i);
            var preview = plan.Preview(node.Owner);
            var parts = RobotMaterials.Parts.Where(part => (preview.Parts & part) != 0).ToArray();
            bool mixed = parts.Select(preview.Material).Distinct().Skip(1).Any() ||
                parts.Select(preview.Origin).Distinct().Skip(1).Any();
            if (mixed && hover) mixedPreview = preview;
            var hangarSize = HangarRobotBounds(0).Size;
            DrawRobotGlyphCore(new(photo.Center.X - hangarSize.X / 2, photo.Center.Y - hangarSize.Y / 2,
                hangarSize.X, hangarSize.Y), preview, whiteOutline: true, opacity: 1,
                partOpacity: null, showIntegratedMaterial: true);
            DrawFactoryAlbumTimeSticker(photo, plan.Turns, ink);
            _ui.Center(mixed ? "混合品" : RobotMaterials.Label(preview.Material(parts[0])),
                new(card.X + 8, photo.Bottom + 1, card.Width - 16, 28), .40f, ink);
            _ui.Center(FactoryProductLabel(plan), new(card.X + 8, photo.Bottom + 29, card.Width - 16, 21), .32f, ink);
            DrawFactoryAlbumEquipment(card, preview.Equipment(node.Owner, _setup.EquipmentMode), ink);
        }
        if (mixedPreview != null && line.PlanCount < 12)
            DrawFactoryAlbumDetails(FactoryAlbumCard(11), mixedPreview, ink);
    }

    private void DrawFactoryAlbumTimeSticker(Rectangle photo, int turns, Color ink)
    {
        const float scale = .46f;
        int numberWidth = (int)System.Math.Ceiling(IndustryDurationSize(turns, scale).X) + 6;
        var sticker = new Rectangle(photo.Right - numberWidth - 42, photo.Y + 7, numberWidth + 35, 42);
        _ui.Box(new(sticker.X + 2, sticker.Y + 3, sticker.Width, sticker.Height), new Color(17, 28, 38));
        _ui.Box(sticker, new Color(250, 218, 130));
        DrawIndustryDuration(turns, new(sticker.X + 4, sticker.Y, numberWidth, sticker.Height), ink, scale);
        _ui.Batch.Draw(_robotFactoryIcon, new Rectangle(sticker.Right - 29, sticker.Y + 8, 26, 26), Color.White);
    }

    private void DrawFactoryAlbumDetails(Rectangle card, Robot preview, Color ink)
    {
        _ui.Box(card, new Color(234, 210, 169));
        _ui.Text("混合品の内訳", new(card.X + 16, card.Y + 14), .43f, ink);
        int y = card.Y + 57;
        foreach (var part in RobotMaterials.Parts)
        {
            if ((preview.Parts & part) == 0) continue;
            int origin = preview.Origin(part);
            _ui.Text($"{PartsLabel(part)}：{RobotMaterials.Label(preview.Material(part))}",
                new(card.X + 16, y), .32f, ink);
            CountryFlagArt.Draw(_ui, _setup, origin, new(card.X + 20, y + 25, 39, 25));
            _ui.Text($"#{origin + 1}", new(card.X + 65, y + 29), .33f, ink);
            y += 78;
        }
    }

    private void DrawFactoryAlbumEquipment(Rectangle card, RobotEquipment gear, Color ink)
    {
        DrawEquipmentCounts(new Rectangle(card.X+38,card.Bottom-23,204,20),gear,ink);
    }

    private void DrawEquipmentCounts(Rectangle area,RobotEquipment gear,Color ink)
    {
        int group=area.Width/3;
        float scale=Math.Min(1,Math.Min(area.Height/20f,group/48f));
        int x=area.X,y=area.Y+(area.Height-(int)(16*scale))/2;
        void Box(int dx,int dy,int width,int height,Color color)=>_ui.Box(
            new(x+(int)(dx*scale),y+(int)(dy*scale),Math.Max(1,(int)(width*scale)),Math.Max(1,(int)(height*scale))),color);
        void Tile(int dx,int dy,int width,int height,float angle,Color color)=>_ui.Tile(
            new(x+dx*scale,y+dy*scale),new(width*scale,height*scale),angle,color);
        void Count(int value,int offset)=>_ui.Text(value.ToString(),new(x+offset*scale,y-2*scale),.34f*scale,ink);
        var shield = new Color(59, 105, 135);
        Box(0,0,18,9,shield);
        Box(3,9,12,4,shield);
        Box(6,13,6,3,shield);
        Count(gear.Shields,22);

        x += group;
        Tile(10,6,17,5,-MathHelper.PiOver4,new Color(102,116,120));
        Tile(5,11,10,3,MathHelper.PiOver4,ink);
        Tile(3,14,7,4,-MathHelper.PiOver4,ink);
        Count(gear.Daggers,22);

        x += group;
        Box(0,6,14,6,ink);
        Box(11,5,11,3,ink);
        Box(2,10,5,6,ink);
        Count(gear.Rifles,24);
    }
}
