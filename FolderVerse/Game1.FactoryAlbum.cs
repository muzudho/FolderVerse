namespace FolderVerse;
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
        _ui.Text("閉じる：Esc / 枠の外をクリック", new(1400, 175), .36f, ink);

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
            var hangarSize = HangarRobotBounds(0).Size;
            DrawRobotGlyphCore(new(photo.Center.X - hangarSize.X / 2, photo.Center.Y - hangarSize.Y / 2,
                hangarSize.X, hangarSize.Y), plan.Preview(node.Owner), whiteOutline: true, opacity: 1,
                partOpacity: null, showIntegratedMaterial: true);
            _ui.Center(RobotMaterials.Label(plan.Material), new(card.X + 8, photo.Bottom + 6, card.Width - 16, 32), .40f, ink);
            _ui.Center(FactoryProductLabel(plan), new(card.X + 8, photo.Bottom + 38, card.Width - 16, 24), .32f, ink);
            DrawIndustryDuration(plan.Turns, new(card.X + 8, photo.Bottom + 66, card.Width - 16, 27), ink,
                maxScale: .37f, suffix: " 産業ターン");
        }
    }
}
