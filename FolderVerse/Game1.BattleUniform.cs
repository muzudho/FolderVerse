namespace FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

public partial class Game1
{
    private readonly Texture2D[] _queenTextures = new Texture2D[3];
    private RobotHeadTexture _sideHead, _sideTorso, _sideLegs;
    private RobotHeadTexture _rearHead, _rearTorso, _rearLegs;
    private RobotHeadTexture _cardboardSideHead, _cardboardRearHead;
    private RobotHeadTexture _foamSideHead, _foamRearHead;
    private RobotHeadTexture _cardboardSideTorso, _cardboardSideLegs;
    private RobotHeadTexture _cardboardRearTorso, _cardboardRearLegs;
    private RobotHeadTexture _mapleSideHead, _mapleRearHead, _mapleRearTorso, _mapleRearLegs;
    private RobotHeadTexture _mapleSideTorso, _mapleSideLegs;
    private Texture2D _rearCaptainCap, _sideCaptainCap;
    private Texture2D _soldierCapTexture, _captainCapTexture, _toyRifleTexture;

    private bool DrawQueenArt(Rectangle area, BattleFacing facing, int owner, float opacity)
    {
        var (textureIndex, view) = facing switch
        {
            BattleFacing.North => (0, "back"),
            BattleFacing.South => (1, "front"),
            _ => (2, "side")
        };
        ref var texture = ref _queenTextures[textureIndex];
        texture ??= LoadBattleToyTexture($"queen-{view}");
        if (texture == null)
            return false;

        int width = Math.Max(1, area.Height * texture.Width / texture.Height);
        var flip = facing == BattleFacing.West ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        _spriteBatch.Draw(texture, new Rectangle(area.Center.X - width / 2, area.Y, width, area.Height),
            null, Color.White * opacity, 0, Vector2.Zero, flip, 0);
        int flagWidth = area.Width / 3;
        int flagHeight = flagWidth * 2 / 3;
        CountryFlagArt.Draw(_ui, _setup, owner,
            new Rectangle(area.Right - flagWidth, area.Bottom - flagHeight, flagWidth, flagHeight), opacity);
        return true;
    }

    private Texture2D LoadBattleToyTexture(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Images", "Robots", $"{name}-v001.png");
        if (!File.Exists(path))
            return null;
        using var stream = File.OpenRead(path);
        using var source = Texture2D.FromStream(GraphicsDevice, stream);
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        for (int index = 0; index < pixels.Length; index++)
        {
            var pixel = pixels[index];
            pixels[index] = Color.FromNonPremultiplied(pixel.R, pixel.G, pixel.B, pixel.A);
        }
        var uploaded = new Texture2D(GraphicsDevice, source.Width, source.Height);
        try { uploaded.SetData(pixels); }
        catch { uploaded.Dispose(); throw; }
        return uploaded;
    }

    private bool DrawBattleSoldierArt(Rectangle area, Robot robot, BattleRole role, BattleFacing facing, bool hasRifle, float opacity)
    {
        if (robot == null || robot.Parts != RobotParts.Complete ||
            facing is not (BattleFacing.North or BattleFacing.South or BattleFacing.East or BattleFacing.West) ||
            role is not (BattleRole.Soldier or BattleRole.Captain))
            return false;

        if (!HasRobotHeadArt(area, robot, allowCaptain: true) ||
            FindRobotBodyArt(area, robot, RobotParts.Body, allowCaptain: true) == null ||
            FindRobotBodyArt(area, robot, RobotParts.Legs, allowCaptain: true) == null)
            return false;

        bool captain = role == BattleRole.Captain;
        bool rear = facing == BattleFacing.North;
        bool side = facing is BattleFacing.East or BattleFacing.West;
        bool west = facing == BattleFacing.West;
        var cap = (captain, rear, side) switch
        {
            (true, true, _) => _rearCaptainCap ??= LoadBattleToyTexture("captain-cap-back"),
            (true, _, true) => _sideCaptainCap ??= LoadBattleToyTexture("captain-cap-side"),
            (true, _, _) => _captainCapTexture ??= LoadBattleToyTexture("captain-cap"),
            _ => _soldierCapTexture ??= LoadBattleToyTexture("soldier-cap")
        };
        var rifle = hasRifle ? (_toyRifleTexture ??= LoadBattleToyTexture("toy-rifle")) : null;
        if (cap == null || hasRifle && rifle == null)
            return false;

        int hatSpace = area.Height / 4;
        var body = new Rectangle(area.X, area.Y + hatSpace, area.Width, area.Height - hatSpace);
        if (side || rear)
        {
            if (!DrawOrientedRobotArt(body, robot, rear, west, opacity))
                return false;
        }
        else
            DrawCompleteRobotArt(body, robot, _ => opacity);
        int capWidth = body.Height * 12 / 25;
        int capHeight = captain ? hatSpace : hatSpace + area.Height / 10;
        var capBounds = new Rectangle(area.Center.X - capWidth / 2, body.Y + 7 - capHeight, capWidth, capHeight);
        var capCrop = (captain, rear, side) switch
        {
            (true, true, _) => new Rectangle(380, 168, 776, 664),
            (true, _, true) => new Rectangle(436, 200, 714, 620),
            (true, _, _) => new Rectangle(528, 304, 473, 434),
            _ => new Rectangle(196, 156, 913, 859)
        };
        _spriteBatch.Draw(cap, capBounds, capCrop, Color.White * opacity, 0, Vector2.Zero,
            captain && west ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        if (rifle != null)
        {
            int width = Math.Max(4, area.Width / 7);
            int offset = side ? area.Width / 9 : area.Width / 4;
            int x = west || rear ? area.Center.X - offset - width : area.Center.X + offset;
            int y = area.Y + (side ? area.Height / 3 : hatSpace);
            var rifleBounds = new Rectangle(x, y, width, area.Height * 3 / 5);
            _spriteBatch.Draw(rifle, rifleBounds, new Rectangle(176, 104, 361, 1984),
                Color.White * opacity, 0, Vector2.Zero, west ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }
        return true;
    }

    private bool DrawOrientedRobotArt(Rectangle area, Robot robot, bool rear, bool west, float opacity)
    {
        if (UsesFoamArt(robot, RobotParts.Body) || UsesFoamArt(robot, RobotParts.Legs))
            return false;
        bool foam = UsesFoamHeadArt(robot);
        bool cardboardTorso = UsesCardboardArt(robot, RobotParts.Body);
        bool cardboardLegs = UsesCardboardArt(robot, RobotParts.Legs);
        bool cardboard = UsesCardboardHeadArt(robot);
        bool paperTorso = UsesPaperArt(robot, RobotParts.Body);
        bool paperLegs = UsesPaperArt(robot, RobotParts.Legs);
        float scale = Math.Min(area.Width / 84f, area.Height / 150f);
        int top = area.Center.Y - (int)Math.Round(75 * scale);
        var flip = west ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        bool maple = UsesMapleArt(robot, RobotParts.Head);
        bool paper = UsesPaperHeadArt(robot);
        bool mapleTorso = UsesMapleArt(robot, RobotParts.Body);
        bool mapleLegs = UsesMapleArt(robot, RobotParts.Legs);
        if (rear)
        {
            var head = foam ? _foamRearHead ??= LoadFoamArt("head", RobotParts.Head, new(.52f,.49f,.14f,.18f), BattleFacing.North)
                : cardboard
                ? _cardboardRearHead ??= LoadCardboardArt("head", RobotParts.Head, new(.54f,.53f,.13f,.16f), BattleFacing.North)
                : paper ? _paperRearHead ??= LoadOrientedPaperArt("head-back", RobotParts.Head, BattleFacing.North, new(.56f,.55f,.10f,.13f)) : maple
                ? _mapleRearHead ??= LoadOrientedRobotArt("head-back", new(.20f, .15f, .61f, .72f), new(.50f, .54f, .15f, .19f), maplePart: RobotParts.Head)
                : _rearHead ??= LoadOrientedRobotArt("head-back", new(.06f, .15f, .86f, .71f), new(.48f, .56f, .16f, .18f));
            var torso = cardboardTorso
                ? _cardboardRearTorso ??= LoadCardboardArt("torso", RobotParts.Body, new(.44f,.43f,.15f,.18f), BattleFacing.North)
                : paperTorso
                ? _paperRearTorso ??= LoadOrientedPaperArt("torso-back", RobotParts.Body, BattleFacing.North, new(.42f,.43f,.16f,.20f))
                : mapleTorso
                ? _mapleRearTorso ??= LoadOrientedRobotArt("torso-back", new(.06f, .13f, .86f, .78f), new(.42f, .40f, .16f, .20f), maplePart: RobotParts.Body)
                : _rearTorso ??= LoadOrientedRobotArt("torso-back", new(128f/1536, 128f/1024, 1272f/1536, 800f/1024), new(.42f, .40f, .16f, .20f));
            var legs = cardboardLegs
                ? _cardboardRearLegs ??= LoadCardboardArt("legs", RobotParts.Legs, new(.60f,.50f,.09f,.14f), BattleFacing.North)
                : paperLegs
                ? _paperRearLegs ??= LoadOrientedPaperArt("legs-back", RobotParts.Legs, BattleFacing.North, new(.57f,.73f,.16f,.10f))
                : mapleLegs
                ? _mapleRearLegs ??= LoadOrientedRobotArt("legs-back", new(.16f, .11f, .68f, .53f), new(.58f, .62f, .14f, .19f), maplePart: RobotParts.Legs)
                : _rearLegs ??= LoadOrientedRobotArt("legs-back", new(240f/1536, 104f/1024, 1048f/1536, 832f/1024), new(.59f, .64f, .14f, .19f));
            if (head == null || torso == null || legs == null)
                return false;
            Draw(legs, RobotParts.Legs, cardboardLegs ? new(56,35,145,185) : paperLegs ? new(58,38,140,182) : mapleLegs ? new(40, 26, 176, 209) : new(40, 26, 175, 208), 102, 58, 48);
            Draw(torso, RobotParts.Body, cardboardTorso ? new(14,50,229,157) : paperTorso ? new(18,57,220,145) : mapleTorso ? new(14, 32, 224, 201) : new(21, 32, 213, 200), 54, 84, 54);
            Draw(head, RobotParts.Head, foam ? new(43,29,170,194) : cardboard ? new(39,25,178,201) : paper ? new(49, 37, 158, 184) : maple ? new(49, 35, 160, 190) : new(16, 38, 220, 180), 0, 72, 59);
        }
        else
        {
            var head = foam ? _foamSideHead ??= LoadFoamArt("head", RobotParts.Head, new(.50f,.48f,.14f,.18f), BattleFacing.East)
                : cardboard
                ? _cardboardSideHead ??= LoadCardboardArt("head", RobotParts.Head, new(.53f,.53f,.13f,.16f), BattleFacing.East)
                : paper ? _paperSideHead ??= LoadOrientedPaperArt("head-side", RobotParts.Head, BattleFacing.East, new(.56f,.55f,.10f,.13f)) : maple
                ? _mapleSideHead ??= LoadOrientedRobotArt("head-side", new(.17f, .10f, .49f, .78f), new(.42f, .55f, .15f, .19f), maplePart: RobotParts.Head)
                : _sideHead ??= LoadOrientedRobotArt("head-side", new(.06f, .15f, .86f, .71f), new(.48f, .56f, .16f, .18f), new(.765f, .374f, .19f, .35f));
            var torso = cardboardTorso
                ? _cardboardSideTorso ??= LoadCardboardArt("torso", RobotParts.Body, new(.34f,.45f,.15f,.18f), BattleFacing.East)
                : paperTorso
                ? _paperSideTorso ??= LoadOrientedPaperArt("torso-side", RobotParts.Body, BattleFacing.East, new(.33f,.49f,.14f,.20f))
                : mapleTorso ? _mapleSideTorso ??= LoadOrientedRobotArt("torso-side", new(.25f, .13f, .50f, .78f), new(.33f, .53f, .12f, .18f), maplePart: RobotParts.Body)
                : _sideTorso ??= LoadOrientedRobotArt("torso-side", new(424f/1536, 136f/1024, 696f/1536, 792f/1024), new(.32f, .52f, .11f, .17f));
            var legs = cardboardLegs
                ? _cardboardSideLegs ??= LoadCardboardArt("legs", RobotParts.Legs, new(.30f,.56f,.12f,.10f), BattleFacing.East)
                : paperLegs
                ? _paperSideLegs ??= LoadOrientedPaperArt("legs-side", RobotParts.Legs, BattleFacing.East, new(.49f,.67f,.13f,.10f))
                : mapleLegs ? _mapleSideLegs ??= LoadOrientedRobotArt("legs-side", new(.22f, .13f, .53f, .49f), new(.32f, .61f, .16f, .15f), maplePart: RobotParts.Legs)
                : _sideLegs ??= LoadOrientedRobotArt("legs-side", new(288f/1312, 120f/1199, 712f/1312, 960f/1199), new(.36f, .72f, .17f, .15f));
            if (head == null || torso == null || legs == null)
                return false;
            Draw(legs, RobotParts.Legs, cardboardLegs ? new(39,29,166,187) : paperLegs ? new(53,32,140,172) : mapleLegs ? new(55, 29, 140, 183) : new(55, 25, 141, 205), 103, 35, 47);
            Draw(torso, RobotParts.Body, cardboardTorso ? new(29,37,199,177) : paperTorso ? new(30,37,196,179) : mapleTorso ? new(62, 32, 133, 201) : new(70, 34, 117, 198), 53, 46, 55);
            Draw(head, RobotParts.Head, foam ? new(43,25,162,204) : cardboard ? new(42,25,169,202) : paper ? new(47, 25, 158, 201) : maple ? new(40, 24, 170, 207) : new(15, 38, 220, 182), 0, 71, 59);
        }
        return true;

        void Draw(RobotHeadTexture cache, RobotParts part, Rectangle crop, int y, int width, int height)
        {
            int destWidth = (int)Math.Round(width * scale);
            var destination = new Rectangle(area.Center.X - destWidth / 2, top + (int)Math.Round(y * scale),
                destWidth, (int)Math.Round(height * scale));
            // The sprite flip supplies the left-facing pose. Counter-flip the emblem so it remains readable.
            _spriteBatch.Draw(cache.Get(_setup, robot.Origin(part), robot.IsDamaged(part), mirrorSticker: west),
                destination, crop, Color.White * opacity, 0, Vector2.Zero, flip, 0);
        }
    }

    private RobotHeadTexture _paperSideHead, _paperRearHead, _paperSideTorso, _paperSideLegs;
    private RobotHeadTexture _paperRearTorso, _paperRearLegs;

    private RobotHeadTexture LoadOrientedPaperArt(string asset, RobotParts part, BattleFacing facing,
        RobotHeadSurface.RectangleF sticker)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Images", "Robots", $"paper-{asset}-v001.png");
        return File.Exists(path) ? new(GraphicsDevice, path, CountryEmblemArt.Tiles,
            stickerBounds:sticker, paperPart:part, paperFacing:facing) : null;
    }

    private RobotHeadTexture LoadOrientedRobotArt(string part, RobotHeadSurface.RectangleF paint,
        RobotHeadSurface.RectangleF sticker, RobotHeadSurface.RectangleF? protectedFace = null, RobotParts? maplePart = null)
    {
        string material = maplePart.HasValue ? "maple" : "pp";
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Images", "Robots", $"{material}-{part}-v001.png");
        return File.Exists(path) ? new(GraphicsDevice, path, CountryEmblemArt.Tiles, paint, sticker, protectedFace,
            maplePart) : null;
    }

    private void DisposeBattleToyArt()
    {
        _foamHeadTextures?.Dispose();
        _foamHeadTextures = null;
        _foamSideHead?.Dispose();
        _foamRearHead?.Dispose();
        _foamSideHead = _foamRearHead = null;
        _foamTorsoTextures?.Dispose();
        _foamTorsoTextures = null;
        _foamLegTextures?.Dispose();
        _foamLegTextures = null;
        _cardboardSideHead?.Dispose();
        _cardboardRearHead?.Dispose();
        _cardboardSideHead = _cardboardRearHead = null;
        _cardboardSideTorso?.Dispose();
        _cardboardSideLegs?.Dispose();
        _cardboardSideTorso = _cardboardSideLegs = null;
        _cardboardRearTorso?.Dispose();
        _cardboardRearLegs?.Dispose();
        _cardboardRearTorso = _cardboardRearLegs = null;
        _cardboardHeadTextures?.Dispose();
        _cardboardTorsoTextures?.Dispose();
        _cardboardLegTextures?.Dispose();
        _cardboardHeadTextures = _cardboardTorsoTextures = _cardboardLegTextures = null;
        _paperSideHead?.Dispose(); _paperRearHead?.Dispose();
        _paperSideHead = _paperRearHead = null;
        _paperSideTorso?.Dispose(); _paperSideLegs?.Dispose();
        _paperSideTorso = _paperSideLegs = null;
        _paperRearTorso?.Dispose(); _paperRearLegs?.Dispose();
        _paperRearTorso = _paperRearLegs = null;
        _paperHeadTextures?.Dispose();
        _paperHeadTextures=null;
        _paperTorsoTextures?.Dispose();
        _paperLegTextures?.Dispose();
        _paperTorsoTextures=_paperLegTextures=null;
        _mapleHeadTextures?.Dispose();
        _mapleTorsoTextures?.Dispose();
        _mapleLegTextures?.Dispose();
        _mapleHeadTextures=_mapleTorsoTextures=_mapleLegTextures=null;
        _mapleSideHead?.Dispose();
        _mapleSideTorso?.Dispose();
        _mapleSideLegs?.Dispose();
        _mapleSideTorso = _mapleSideLegs = null;
        _mapleRearHead?.Dispose();
        _mapleRearTorso?.Dispose();
        _mapleRearLegs?.Dispose();
        _mapleSideHead = _mapleRearHead = _mapleRearTorso = _mapleRearLegs = null;
        _sideHead?.Dispose();
        _sideTorso?.Dispose();
        _sideLegs?.Dispose();
        _sideHead = _sideTorso = _sideLegs = null;
        _rearHead?.Dispose();
        _rearTorso?.Dispose();
        _rearLegs?.Dispose();
        _rearHead = _rearTorso = _rearLegs = null;
        _rearCaptainCap?.Dispose();
        _sideCaptainCap?.Dispose();
        _rearCaptainCap = _sideCaptainCap = null;
        foreach (var texture in _queenTextures)
            texture?.Dispose();
        Array.Clear(_queenTextures);
        _soldierCapTexture?.Dispose();
        _captainCapTexture?.Dispose();
        _toyRifleTexture?.Dispose();
        _soldierCapTexture = _captainCapTexture = _toyRifleTexture = null;
    }

    private void DrawBattleRobotParts(Rectangle area, Robot robot, int army, float opacity)
    {
        DrawRobotGlyph(area, robot, opacity: opacity,
            partOpacity: part => BattlePartOpacity(robot.IsDamaged(part) || robot.Origin(part) == army));
    }

    private void DrawBattleUniformPatch(Rectangle area, Rectangle robotArea, Rectangle damageArea,
        Robot robot, RobotParts part, float opacity)
    {
        int origin = robot.Origin(part);
        DrawRobotPartFill(area, robotArea, damageArea, robot, part, origin, _setup.OwnerColor(origin), opacity);
    }
}
