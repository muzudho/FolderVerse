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

        if (facing!=BattleFacing.South && Array.Exists(RobotMaterials.Parts,part=>UsesMapleArt(robot,part)) ||
            !HasRobotHeadArt(area, robot, allowCaptain: true) ||
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
        float scale = Math.Min(area.Width / 84f, area.Height / 150f);
        int top = area.Center.Y - (int)Math.Round(75 * scale);
        var flip = west ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (rear)
        {
            _rearHead ??= LoadOrientedRobotArt("head-back", new(.06f, .15f, .86f, .71f), new(.48f, .56f, .16f, .18f));
            _rearTorso ??= LoadOrientedRobotArt("torso-back", new(128f/1536, 128f/1024, 1272f/1536, 800f/1024), new(.42f, .40f, .16f, .20f));
            _rearLegs ??= LoadOrientedRobotArt("legs-back", new(240f/1536, 104f/1024, 1048f/1536, 832f/1024), new(.59f, .64f, .14f, .19f));
            if (_rearHead == null || _rearTorso == null || _rearLegs == null)
                return false;
            Draw(_rearLegs, RobotParts.Legs, new(40, 26, 175, 208), 102, 58, 48);
            Draw(_rearTorso, RobotParts.Body, new(21, 32, 213, 200), 54, 84, 54);
            Draw(_rearHead, RobotParts.Head, new(16, 38, 220, 180), 0, 72, 59);
        }
        else
        {
            _sideHead ??= LoadOrientedRobotArt("head-side", new(.06f, .15f, .86f, .71f), new(.48f, .56f, .16f, .18f), new(.765f, .374f, .19f, .35f));
            _sideTorso ??= LoadOrientedRobotArt("torso-side", new(424f/1536, 136f/1024, 696f/1536, 792f/1024), new(.32f, .52f, .11f, .17f));
            _sideLegs ??= LoadOrientedRobotArt("legs-side", new(288f/1312, 120f/1199, 712f/1312, 960f/1199), new(.36f, .72f, .17f, .15f));
            if (_sideHead == null || _sideTorso == null || _sideLegs == null)
                return false;
            Draw(_sideLegs, RobotParts.Legs, new(55, 25, 141, 205), 103, 35, 47);
            Draw(_sideTorso, RobotParts.Body, new(70, 34, 117, 198), 53, 46, 55);
            Draw(_sideHead, RobotParts.Head, new(15, 38, 220, 182), 0, 71, 59);
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

    private RobotHeadTexture LoadOrientedRobotArt(string part, RobotHeadSurface.RectangleF paint,
        RobotHeadSurface.RectangleF sticker, RobotHeadSurface.RectangleF? protectedFace = null)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Images", "Robots", $"pp-{part}-v001.png");
        return File.Exists(path) ? new(GraphicsDevice, path, CountryEmblemArt.Tiles, paint, sticker, protectedFace) : null;
    }

    private void DisposeBattleToyArt()
    {
        _mapleHeadTextures?.Dispose();
        _mapleTorsoTextures?.Dispose();
        _mapleLegTextures?.Dispose();
        _mapleHeadTextures=_mapleTorsoTextures=_mapleLegTextures=null;
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
