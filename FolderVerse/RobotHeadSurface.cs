namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

// Maps for the neutral PP sprites. Face regions use measured geometry.
public static class RobotHeadSurface
{
    // Bounds measured on the neutral PP torso and legs. Transparent pixels
    // stay outside the paint mask, including the gaps between the feet.
    public static RobotFlagComposite.Surface[] PolypropyleneBody(
        Color[] neutral, int width, int height, RectangleF bounds, RectangleF? protectedFace = null,
        float faceCornerRadius = .05f)
    {
        if (width < 1 || height < 1 || neutral == null || (long)width * height != neutral.Length)
            throw new ArgumentException("Body image dimensions do not match its pixels.");
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new ArgumentException("The paint bounds must have positive dimensions.", nameof(bounds));

        var result = new RobotFlagComposite.Surface[neutral.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;
            Color pixel = neutral[index];
            if (pixel.A == 0)
                continue;

            float imageX = (x + .5f) / width;
            float imageY = (y + .5f) / height;
            float u = (imageX - bounds.X) / bounds.Width;
            float v = (imageY - bounds.Y) / bounds.Height;
            float coverage = 1;
            if (protectedFace is { } face)
            {
                float radius = faceCornerRadius;
                float dx = Math.Max(Math.Abs(imageX - face.X - face.Width / 2) - (face.Width / 2 - radius), 0);
                float dy = Math.Max(Math.Abs(imageY - face.Y - face.Height / 2) - (face.Height / 2 - radius), 0);
                coverage = Math.Clamp((MathF.Sqrt(dx * dx + dy * dy) - radius) / .008f, 0, 1);
            }
            float light = (pixel.R + pixel.G + pixel.B) / (3f * 255);
            result[index] = new(
                new(Math.Clamp(u, 0, 1), Math.Clamp(v, 0, 1)),
                Byte(coverage),
                Byte(Math.Clamp(light / .612f, 0, 1)),
                Byte(Math.Clamp((light - .738f) / .262f, 0, 1) * .95f));
        }
        return result;
    }

    public readonly record struct RectangleF(float X, float Y, float Width, float Height);

    public static RobotFlagComposite.Surface[] TinplateHead(Color[] neutral, int width, int height,
        BattleFacing facing = BattleFacing.South)
    {
        bool side = facing is BattleFacing.East or BattleFacing.West;
        var shell = side ? new RectangleF(230f / 1536, 82f / 1024, 1052f / 1536, 850f / 1024)
            : new RectangleF(234f / 1536, 89f / 1024, 1056f / 1536, 811f / 1024);
        RectangleF? faceplate = facing == BattleFacing.North ? null
            : side ? new(1064f / 1536, 331f / 1024, 240f / 1536, 447f / 1024)
            : new(318f / 1536, 242f / 1024, 891f / 1536, 621f / 1024);
        var surface = PolypropyleneBody(neutral, width, height, shell, faceplate, faceCornerRadius: side ? .055f : .09f);
        ApplyTinplateFinish(neutral, surface, protectGold: facing != BattleFacing.South);
        return surface;
    }

    public static RobotFlagComposite.Surface[] TinplateBody(Color[] neutral, int width, int height, RobotParts part,
        BattleFacing facing = BattleFacing.South)
    {
        var shell = (part, facing) switch
        {
            (RobotParts.Body, BattleFacing.North) => new RectangleF(124f / 1536, 135f / 1024, 1288f / 1536, 759f / 1024),
            (RobotParts.Legs, BattleFacing.North) => new RectangleF(257f / 1536, 89f / 1024, 1023f / 1536, 844f / 1024),
            (RobotParts.Body, BattleFacing.East or BattleFacing.West) => new RectangleF(399f / 1536, 109f / 1024, 732f / 1536, 832f / 1024),
            (RobotParts.Legs, BattleFacing.East or BattleFacing.West) => new RectangleF(383f / 1536, 93f / 1024, 699f / 1536, 856f / 1024),
            (RobotParts.Body, _) => new RectangleF(130f / 1536, 137f / 1024, 1274f / 1536, 756f / 1024),
            (RobotParts.Legs, _) => new RectangleF(259f / 1536, 93f / 1024, 1018f / 1536, 839f / 1024),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        var surface = PolypropyleneBody(neutral, width, height, shell);
        ApplyTinplateFinish(neutral, surface, protectGold: true);
        return surface;
    }

    private static void ApplyTinplateFinish(Color[] neutral, RobotFlagComposite.Surface[] surface, bool protectGold)
    {
        for (int i = 0; i < surface.Length; i++)
        {
            Color pixel = neutral[i];
            float light = (pixel.R + pixel.G + pixel.B) / (3f * 255);
            float edge = Math.Clamp((pixel.A - 24) / 64f, 0, 1);
            float paint = protectGold ? Math.Clamp((20 - (pixel.R - pixel.B)) / 14f, 0, 1) : 1;
            surface[i] = surface[i] with
            {
                Coverage = (byte)MathF.Round(surface[i].Coverage * edge * paint),
                Diffuse = Byte(Math.Clamp(light / .82f, 0, 1)),
                Reflection = Byte(Math.Clamp((light - .72f) / .28f, 0, 1) * .96f)
            };
        }
    }

    public static RobotFlagComposite.Surface[] SiliconeHead(Color[] neutral, int width, int height,
        BattleFacing facing = BattleFacing.South)
    {
        // Measured on the three 1536 × 1024 neutral silicone head sprites.
        bool side = facing is BattleFacing.East or BattleFacing.West;
        var shell = facing switch
        {
            BattleFacing.East or BattleFacing.West => new RectangleF(250f / 1536, 104f / 1024, 1007f / 1536, 832f / 1024),
            BattleFacing.North => new RectangleF(281f / 1536, 127f / 1024, 968f / 1536, 768f / 1024),
            _ => new RectangleF(294f / 1536, 142f / 1024, 938f / 1536, 741f / 1024)
        };
        RectangleF? face = side ? new(1064f / 1536, 351f / 1024, 208f / 1536, 412f / 1024)
            : facing == BattleFacing.South ? new(463f / 1536, 378f / 1024, 589f / 1536, 371f / 1024) : null;
        var surface = PolypropyleneBody(neutral, width, height, shell, face, faceCornerRadius: side ? .042f : .065f);
        ApplySiliconeFinish(neutral, surface);
        return surface;
    }

    public static RobotFlagComposite.Surface[] SiliconeBody(Color[] neutral, int width, int height, RobotParts part,
        BattleFacing facing = BattleFacing.South)
    {
        var shell = (part, facing) switch
        {
            (RobotParts.Body, BattleFacing.North) => new RectangleF(124f / 1536, 130f / 1024, 1286f / 1536, 798f / 1024),
            (RobotParts.Legs, BattleFacing.North) => new RectangleF(252f / 1536, 112f / 1024, 1020f / 1536, 833f / 1024),
            (RobotParts.Body, BattleFacing.East or BattleFacing.West) => new RectangleF(412f / 1536, 110f / 1024, 746f / 1536, 846f / 1024),
            (RobotParts.Legs, BattleFacing.East or BattleFacing.West) => new RectangleF(407f / 1536, 104f / 1024, 691f / 1536, 853f / 1024),
            (RobotParts.Body, _) => new RectangleF(131f / 1536, 138f / 1024, 1264f / 1536, 774f / 1024),
            (RobotParts.Legs, _) => new RectangleF(268f / 1536, 141f / 1024, 995f / 1536, 785f / 1024),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        var surface = PolypropyleneBody(neutral, width, height, shell);
        ApplySiliconeFinish(neutral, surface, protectShoulders: part == RobotParts.Body,
            shoulderColorDifference: facing == BattleFacing.South ? 25 : 20);
        return surface;
    }

    private static void ApplySiliconeFinish(Color[] neutral, RobotFlagComposite.Surface[] surface,
        bool protectShoulders = false, int shoulderColorDifference = 25)
    {
        for (int i = 0; i < surface.Length; i++)
        {
            Color pixel = neutral[i];
            float light = (pixel.R + pixel.G + pixel.B) / (3f * 255);
            float edge = Math.Clamp((pixel.A - 24) / 64f, 0, 1);
            float paint = protectShoulders ? Math.Clamp((shoulderColorDifference - (pixel.R - pixel.B)) / 13f, 0, 1) : 1;
            surface[i] = surface[i] with
            {
                Coverage = (byte)MathF.Round(surface[i].Coverage * edge * paint),
                Diffuse = Byte(Math.Clamp(light / .80f, 0, 1)),
                Reflection = Byte(Math.Clamp((light - .64f) / .36f, 0, 1) * .26f)
            };
        }
    }

    public static RobotFlagComposite.Surface[] SoftVinylHead(Color[] neutral, int width, int height,
        BattleFacing facing = BattleFacing.South)
    {
        // Coordinates measured on the three 1536 × 1024 neutral head sprites.
        bool side = facing is BattleFacing.East or BattleFacing.West;
        var shell = facing switch
        {
            BattleFacing.East or BattleFacing.West => new RectangleF(260f / 1536, 114f / 1024, 993f / 1536, 818f / 1024),
            BattleFacing.North => new RectangleF(289f / 1536, 144f / 1024, 943f / 1536, 747f / 1024),
            _ => new RectangleF(302f / 1536, 145f / 1024, 925f / 1536, 735f / 1024)
        };
        RectangleF? face = side ? new(1069f / 1536, 360f / 1024, 201f / 1536, 397f / 1024)
            : facing == BattleFacing.South ? new(465f / 1536, 379f / 1024, 586f / 1536, 367f / 1024) : null;
        var surface = PolypropyleneBody(neutral, width, height, shell, face, faceCornerRadius: side ? .043f : .065f);
        ApplySoftVinylFinish(neutral, surface);
        return surface;
    }

    public static RobotFlagComposite.Surface[] SoftVinylBody(Color[] neutral, int width, int height, RobotParts part,
        BattleFacing facing = BattleFacing.South)
    {
        bool side = facing is BattleFacing.East or BattleFacing.West;
        bool rear = facing == BattleFacing.North;
        var shell = (part, side, rear) switch
        {
            (RobotParts.Body, true, _) => new RectangleF(438f / 1536, 148f / 1024, 586f / 1536, 769f / 1024),
            (RobotParts.Legs, true, _) => new RectangleF(410f / 1536, 105f / 1024, 685f / 1536, 851f / 1024),
            (RobotParts.Body, _, true) => new RectangleF(106f / 1536, 159f / 1024, 1323f / 1536, 732f / 1024),
            (RobotParts.Legs, _, true) => new RectangleF(237f / 1536, 119f / 1024, 1058f / 1536, 808f / 1024),
            (RobotParts.Body, _, _) => new RectangleF(108f / 1536, 168f / 1024, 1317f / 1536, 723f / 1024),
            (RobotParts.Legs, _, _) => new RectangleF(246f / 1536, 126f / 1024, 1034f / 1536, 795f / 1024),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        var surface = PolypropyleneBody(neutral, width, height, shell);
        ApplySoftVinylFinish(neutral, surface, protectCreamTrim: part == RobotParts.Body);
        if (side && part == RobotParts.Body)
        {
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) * 1536 / width;
                float py = (y + .5f) * 1024 / height;
                int i = y * width + x;
                // The strongest highlights on the cream trim are nearly neutral.
                float shoulder = Vector2.DistanceSquared(new((px - 707) / 124, (py - 342) / 78), Vector2.Zero);
                float cuff = Vector2.DistanceSquared(new((px - 940) / 74, (py - 674) / 50), Vector2.Zero);
                float coverage = Math.Min(Math.Clamp((py - 248) / 9, 0, 1), Math.Clamp((839 - py) / 9, 0, 1));
                coverage *= Math.Clamp((Math.Min(shoulder, cuff) - .82f) / .18f, 0, 1);
                surface[i] = surface[i] with { Coverage = (byte)MathF.Round(surface[i].Coverage * coverage) };
            }
        }
        return surface;
    }

    private static void ApplySoftVinylFinish(Color[] neutral, RobotFlagComposite.Surface[] surface,
        bool protectCreamTrim = false)
    {
        for (int i = 0; i < surface.Length; i++)
        {
            Color pixel = neutral[i];
            float light = (pixel.R + pixel.G + pixel.B) / (3f * 255);
            float edge = Math.Clamp((pixel.A - 24) / 64f, 0, 1);
            // The neutral chest and sleeves have a much smaller warm-channel spread than the cream trim.
            float paint = protectCreamTrim ? Math.Clamp((33 - (pixel.R - pixel.B)) / 16f, 0, 1) : 1;
            surface[i] = surface[i] with
            {
                Coverage = (byte)MathF.Round(surface[i].Coverage * edge * paint),
                Reflection = Byte(Math.Clamp((light - .63f) / .37f, 0, 1) * .68f)
            };
        }
    }

    public static RobotFlagComposite.Surface[] PolystyreneHead(Color[] neutral, int width, int height,
        BattleFacing facing = BattleFacing.South)
    {
        var bounds = facing switch
        {
            BattleFacing.East => new RectangleF(262f / 1536, 108f / 1024, 878f / 1536, 796f / 1024),
            BattleFacing.North => new RectangleF(222f / 1536, 110f / 1024, 1090f / 1536, 796f / 1024),
            _ => new RectangleF(230f / 1536, 112f / 1024, 1070f / 1536, 790f / 1024)
        };
        RectangleF? face = facing == BattleFacing.South
            ? new(417f / 1536, 374f / 1024, 686f / 1536, 396f / 1024) : null;
        var result = PolypropyleneBody(neutral, width, height, bounds, face);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float px = (x + .5f) * 1536 / width;
            float py = (y + .5f) * 1024 / height;
            bool latchColumn = facing switch
            {
                BattleFacing.East => px is >= 1075 and <= 1147,
                BattleFacing.North => px is >= 270 and <= 350 || px is >= 1183 and <= 1264,
                _ => px is >= 273 and <= 348 || px is >= 1175 and <= 1252
            };
            bool latch = latchColumn && (py is >= 415 and <= 508 || py is >= 670 and <= 765);
            bool upperSeam = px is >= 448 and <= 1088 && py is >= 163 and <= 176;
            bool sideFace = facing == BattleFacing.East && px >= 1143 && py is >= 345 and <= 756;
            if (latch || upperSeam || sideFace)
            {
                int index = y * width + x;
                result[index] = result[index] with { Coverage = 0 };
            }
        }
        return result;
    }

    public static RobotFlagComposite.Surface[] PolystyreneBody(Color[] neutral, int width, int height, RobotParts part,
        BattleFacing facing = BattleFacing.South)
    {
        var bounds = (part, facing) switch
        {
            (RobotParts.Body, BattleFacing.East) => new RectangleF(405f / 1536, 115f / 1024, 663f / 1536, 810f / 1024),
            (RobotParts.Legs, BattleFacing.East) => new RectangleF(273f / 1312, 127f / 1199, 715f / 1312, 945f / 1199),
            (RobotParts.Body, BattleFacing.North) => new RectangleF(78f / 1536, 132f / 1024, 1380f / 1536, 742f / 1024),
            (RobotParts.Legs, BattleFacing.North) => new RectangleF(230f / 1536, 98f / 1024, 1074f / 1536, 853f / 1024),
            (RobotParts.Body, _) => new RectangleF(56f / 1536, 165f / 1024, 1424f / 1536, 685f / 1024),
            (RobotParts.Legs, _) => new RectangleF(272f / 1536, 148f / 1024, 992f / 1536, 772f / 1024),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        var result = PolypropyleneBody(neutral, width, height, bounds);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float px = (x + .5f) * 1536 / width;
            float py = (y + .5f) * 1024 / height;
            bool bare = (part, facing) switch
            {
                (RobotParts.Body, BattleFacing.East) => px is >= 929 and <= 1000 && py is >= 388 and <= 481,
                (RobotParts.Legs, BattleFacing.East) => neutral[y * width + x].A < 32,
                (RobotParts.Body, BattleFacing.North) => (px is >= 409 and <= 497 || px is >= 1040 and <= 1129)
                    && (py is >= 403 and <= 490 || py is >= 634 and <= 721),
                (RobotParts.Body, _) => (px is >= 427 and <= 510 || px is >= 1024 and <= 1108)
                    && (py is >= 407 and <= 490 || py is >= 628 and <= 713),
                _ => px is >= 722 and <= 814 && py >= 400
            };
            if (bare)
            {
                int index = y * width + x;
                result[index] = result[index] with { Coverage = 0 };
            }
        }
        return result;
    }

    public static RobotFlagComposite.Surface[] FoamHead(Color[] neutral, int width, int height)
        => Foam(neutral, width, height, RobotParts.Head);

    public static RobotFlagComposite.Surface[] Foam(Color[] neutral, int width, int height, RobotParts part,
        BattleFacing facing = BattleFacing.South)
    {
        if (width < 1 || height < 1 || neutral == null || (long)width * height != neutral.Length)
            throw new ArgumentException("Foam image dimensions do not match its pixels.");

        var uv = (part, facing) switch
        {
            (RobotParts.Head, BattleFacing.East) => new RectangleF(303,176,731,673),
            (RobotParts.Head, BattleFacing.North) => new RectangleF(390,288,747,502),
            (RobotParts.Head, _) => new RectangleF(216,129,1050,740),
            (RobotParts.Body, BattleFacing.North) => new RectangleF(456,354,550,396),
            (RobotParts.Legs, BattleFacing.North) => new RectangleF(438,463,664,198),
            (RobotParts.Body, BattleFacing.East) => new RectangleF(405,344,641,405),
            (RobotParts.Legs, BattleFacing.East) => new RectangleF(495,482,441,177),
            (RobotParts.Body, _) => new RectangleF(175,264,1155,511),
            (RobotParts.Legs, _) => new RectangleF(433,478,658,188),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        var result = new RobotFlagComposite.Surface[neutral.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;
            var pixel = neutral[index];
            if (pixel.A == 0) continue;
            float px = (x + .5f) * 1536 / width;
            float py = (y + .5f) * 1024 / height;
            // The inset face remains bare. The brightest bead tops retain more white foam.
            float dx = Math.Max(Math.Abs(px - 671) - 231, 0);
            float dy = Math.Max(Math.Abs(py - 562) - 99, 0);
            float distance = (part, facing) switch
            {
                (RobotParts.Head, BattleFacing.East) => Panel(px,py,668.5f,512.5f,365.5f,336.5f,156),
                (RobotParts.Head, BattleFacing.North) => Panel(px,py,763.5f,539,373.5f,251,133),
                (RobotParts.Head, _) => new Vector2(dx, dy).Length() - 91,
                (RobotParts.Body, BattleFacing.North) => Panel(px,py,731,552,275,198,65),
                (RobotParts.Legs, BattleFacing.North) => Math.Max(Panel(px,py,556,562,118,99,43),
                    Panel(px,py,973,562,129,99,43)),
                (RobotParts.Body, BattleFacing.East) => Math.Max(Panel(px,py,868,548,177,198,82),
                    Panel(px,py,517,530,111,89,74)),
                (RobotParts.Legs, BattleFacing.East) => Math.Max(Panel(px,py,602,572,107,88,55),
                    Panel(px,py,846,548,90,66,43)),
                (RobotParts.Body, _) => Math.Max(Panel(px, py, 710, 550, 280, 214, 70),
                    Math.Max(Panel(px, py, 302, 538, 127, 110, 94), Panel(px, py, 1174, 541, 132, 110, 94))),
                _ => Math.Max(Panel(px, py, 575, 575, 135, 88, 46), Panel(px, py, 938, 575, 137, 88, 46))
            };
            float shell = Math.Clamp(distance / 10, 0, 1);
            float light = (pixel.R + pixel.G + pixel.B) / (3f * 255);
            float beadTop = Math.Clamp((light - .84f) / .16f, 0, 1);
            float mottling = MathF.Sin(px / 53 + MathF.Sin(py / 43)) * MathF.Sin(py / 47);
            float coverage = shell * (.94f - .34f * beadTop + .06f * mottling);
            result[index] = new(new(Math.Clamp((px - uv.X) / uv.Width, 0, .999f),
                Math.Clamp((py - uv.Y) / uv.Height, 0, .999f)), Byte(coverage), Byte(light), 0);
        }
        return result;

        static float Panel(float x, float y, float cx, float cy, float halfWidth, float halfHeight, float radius)
        {
            var offset = new Vector2(Math.Max(Math.Abs(x - cx) - halfWidth + radius, 0),
                Math.Max(Math.Abs(y - cy) - halfHeight + radius, 0));
            return radius - offset.Length();
        }
    }

    public static RobotFlagComposite.Surface[] CardboardHead(Color[] neutral, int width, int height,
        BattleFacing facing = BattleFacing.South)
    {
        if (width < 1 || height < 1 || neutral == null || (long)width * height != neutral.Length)
            throw new ArgumentException("Cardboard image dimensions do not match its pixels.");

        var (bounds, radius) = facing switch
        {
            BattleFacing.East => (new RectangleF(354, 252, 760, 552), 113f),
            BattleFacing.North => (new RectangleF(370, 260, 796, 544), 102f),
            BattleFacing.South => (new RectangleF(242, 249, 890, 582), 112f),
            _ => throw new ArgumentOutOfRangeException(nameof(facing))
        };
        float halfWidth = bounds.Width / 2, halfHeight = bounds.Height / 2;
        var result = new RobotFlagComposite.Surface[neutral.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;
            var pixel = neutral[index];
            if (pixel.A == 0) continue;

            float px = (x + .5f) * 1536 / width;
            float py = (y + .5f) * 1024 / height;
            // Leave the corrugated rim and printed features in their original colors.
            float panel = -RoundedDistance(px, py, bounds.X + halfWidth, bounds.Y + halfHeight, halfWidth, halfHeight, radius);
            if (facing == BattleFacing.South)
            {
                float eyes = Math.Min(EyeDistance(px, py, 481), EyeDistance(px, py, 907));
                float mouth = RoundedDistance(px, py, 694, 634, 99, 50, 29);
                panel = Math.Min(panel, Math.Min(eyes, mouth));
            }
            float coverage = Math.Clamp(panel / 7, 0, 1);
            float light = (pixel.R + pixel.G + pixel.B) / (3f * 255);
            result[index] = new(new(Math.Clamp((px - bounds.X) / bounds.Width, 0, .999f),
                Math.Clamp((py - bounds.Y) / bounds.Height, 0, .999f)), Byte(coverage), Byte(Math.Min(light * 1.18f, 1)), 0);
        }
        return result;

        static float RoundedDistance(float x, float y, float cx, float cy, float halfWidth, float halfHeight, float radius)
        {
            float dx = Math.Abs(x - cx) - halfWidth + radius;
            float dy = Math.Abs(y - cy) - halfHeight + radius;
            return new Vector2(Math.Max(dx, 0), Math.Max(dy, 0)).Length() + Math.Min(Math.Max(dx, dy), 0) - radius;
        }

        static float EyeDistance(float x, float y, float centerX)
            => (new Vector2((x - centerX) / 55, (y - 537) / 99).Length() - 1) * 55;
    }

    public static RobotFlagComposite.Surface[] PaperHead(Color[] neutral, int width, int height)
        => Paper(neutral, width, height, RobotParts.Head);

    public static RobotFlagComposite.Surface[] CardboardBody(Color[] neutral, int width, int height, RobotParts part,
        BattleFacing facing = BattleFacing.South)
    {
        if (width < 1 || height < 1 || neutral == null || (long)width * height != neutral.Length)
            throw new ArgumentException("Cardboard image dimensions do not match its pixels.");

        Vector2[][] sheets = (part, facing) switch
        {
            (RobotParts.Body, BattleFacing.East) => [[new(313,304),new(861,304),new(855,752),new(839,761),new(218,761),new(212,751)],
                [new(1022,432),new(1158,369),new(1331,655),new(1177,721)]],
            (RobotParts.Legs, BattleFacing.East) => [[new(410,396),new(553,396),new(624,814),new(275,814)],
                [new(640,396),new(694,396),new(757,662),new(700,662)]],
            (RobotParts.Body, BattleFacing.North) => [[new(463,344),new(1069,344),new(1180,760),new(353,760)],
                [new(274,423),new(377,478),new(239,710),new(113,660)],
                [new(1261,423),new(1421,660),new(1295,710),new(1157,478)]],
            (RobotParts.Legs, BattleFacing.North) => [[new(490,434),new(618,434),new(652,678),new(443,678)],
                [new(918,434),new(1044,434),new(1092,678),new(884,678)]],
            (RobotParts.Body, BattleFacing.South) => [[new(442,301),new(999,301),new(1058,699),new(1051,727),
                new(1025,740),new(366,740),new(342,727),new(336,701)],
                [new(269,384),new(335,412),new(218,651),new(115,612)],
                [new(1138,411),new(1231,378),new(1380,610),new(1260,651)]],
            (RobotParts.Legs, BattleFacing.South) => [[new(462,377),new(596,377),new(607,650),new(380,650)],
                [new(875,377),new(1019,377),new(1089,650),new(854,650)]],
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        var uv = (part, facing) switch
        {
            (RobotParts.Body, BattleFacing.East) => new RectangleF(212,304,1119,457),
            (RobotParts.Legs, BattleFacing.East) => new RectangleF(275,396,482,418),
            (RobotParts.Body, BattleFacing.North) => new RectangleF(113,344,1308,416),
            (RobotParts.Legs, BattleFacing.North) => new RectangleF(443,434,649,244),
            (RobotParts.Body, _) => new RectangleF(115,301,1265,439),
            _ => new RectangleF(380,377,709,273)
        };
        bool sideLegs = part == RobotParts.Legs && facing == BattleFacing.East;
        return MatteSheets(neutral, width, height, sheets, uv, sideLegs ? 1312 : 1536, sideLegs ? 1199 : 1024, 1.18f);
    }

    public static RobotFlagComposite.Surface[] Paper(Color[] neutral, int width, int height, RobotParts part,
        BattleFacing facing = BattleFacing.South)
    {
        if (width < 1 || height < 1 || neutral == null || (long)width * height != neutral.Length)
            throw new ArgumentException("Paper image dimensions do not match its pixels.");

        // Each polygon follows one sheet on the matching neutral sprite.
        Vector2[][] sheets = (part, facing) switch
        {
            (RobotParts.Head, BattleFacing.East) => [[new(377,357),new(1093,357),new(1093,726),new(377,726)]],
            (RobotParts.Body, BattleFacing.East) => [[new(305,267),new(871,267),new(802,817),new(205,817)],
                [new(895,485),new(1030,426),new(1183,705),new(1078,839)],
                [new(1042,421),new(1166,364),new(1333,651),new(1197,697)]],
            (RobotParts.Legs, BattleFacing.East) => [[new(428,431),new(612,431),new(656,727),new(329,727)],
                [new(632,431),new(724,431),new(768,639),new(676,639)]],
            (RobotParts.Head, BattleFacing.North) => [[new(472,244),new(1059,244),new(1136,374),new(1136,717),
                new(1063,835),new(466,835),new(390,716),new(390,374)]],
            (RobotParts.Body, BattleFacing.North) => [[new(472,322),new(1064,322),new(1175,789),new(361,789)],
                [new(281,427),new(385,487),new(250,711),new(125,657)],
                [new(1255,427),new(1411,657),new(1286,711),new(1151,487)]],
            (RobotParts.Legs, BattleFacing.North) => [[new(473,430),new(642,430),new(676,666),new(414,666)],
                [new(893,430),new(1062,430),new(1122,666),new(859,666)]],
            (RobotParts.Head, BattleFacing.South) => [[new(412,297),new(929,300),new(991,428),new(991,752),
                new(934,849),new(397,844),new(340,745),new(340,422)]],
            (RobotParts.Body, BattleFacing.South) => [[new(456,321),new(984,321),new(939,801),new(352,792)],
                [new(280,432),new(381,487),new(262,707),new(130,647)],
                [new(1124,440),new(1234,397),new(1383,636),new(1261,677)],
                [new(1015,490),new(1113,444),new(1249,684),new(1150,807)]],
            (RobotParts.Legs, BattleFacing.South) => [[new(523,511),new(649,515),new(618,651),new(463,638)],
                [new(823,516),new(951,519),new(989,658),new(791,654)]],
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        bool frontHead = part == RobotParts.Head && facing == BattleFacing.South;
        bool sideLegs = part == RobotParts.Legs && facing == BattleFacing.East;
        int sourceWidth = frontHead ? 1485 : sideLegs ? 1312 : 1536;
        int sourceHeight = frontHead ? 1059 : sideLegs ? 1199 : 1024;
        var uv = (part, facing) switch {
            (RobotParts.Head, BattleFacing.East) => new RectangleF(377,357,716,369),
            (RobotParts.Head, BattleFacing.North) => new RectangleF(390,244,746,591),
            (RobotParts.Body, BattleFacing.East) => new RectangleF(205,267,1128,572),
            (RobotParts.Legs, BattleFacing.East) => new RectangleF(329,431,439,296),
            (RobotParts.Body, BattleFacing.North) => new RectangleF(125,322,1286,467),
            (RobotParts.Legs, BattleFacing.North) => new RectangleF(414,430,708,236),
            (RobotParts.Head, _) => new RectangleF(340,297,652,553),
            (RobotParts.Body, _) => new RectangleF(125,320,1260,490),
            _ => new RectangleF(460,510,535,150)
        };
        return MatteSheets(neutral, width, height, sheets, uv, sourceWidth, sourceHeight, 1.06f, frontHead);
    }

    private static RobotFlagComposite.Surface[] MatteSheets(Color[] neutral, int width, int height,
        Vector2[][] sheets, RectangleF uv, int sourceWidth, int sourceHeight, float lightScale, bool protectPaperFace = false)
    {
        var normals = Array.ConvertAll(sheets, sheet => {
            var result = new Vector2[sheet.Length];
            for (int i = 0; i < sheet.Length; i++)
            {
                var side = sheet[(i + 1) % sheet.Length] - sheet[i];
                result[i] = Vector2.Normalize(new(-side.Y, side.X));
            }
            return result;
        });
        var result = new RobotFlagComposite.Surface[neutral.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;
            var pixel = neutral[index];
            if (pixel.A == 0) continue;
            var point = new Vector2((x + .5f) * sourceWidth / width, (y + .5f) * sourceHeight / height);
            float sheetDistance = 0;
            for (int sheet = 0; sheet < sheets.Length; sheet++)
            {
                float distance = float.PositiveInfinity;
                for (int i = 0; i < sheets[sheet].Length; i++)
                    distance = Math.Min(distance, Vector2.Dot(point - sheets[sheet][i], normals[sheet][i]));
                sheetDistance = Math.Max(sheetDistance, distance);
            }
            if (sheetDistance <= 0) continue;

            // Keep the inset and its rim outside the dyed paper.
            if (protectPaperFace)
            {
                float dx = Math.Max(Math.Abs(point.X - 644) - 155, 0);
                float dy = Math.Max(Math.Abs(point.Y - 594) - 68, 0);
                sheetDistance = Math.Min(sheetDistance, MathF.Sqrt(dx * dx + dy * dy) - 108);
            }
            float coverage = Math.Clamp(sheetDistance / 7, 0, 1);
            float light = (pixel.R + pixel.G + pixel.B) / (3f * 255);
            result[index] = new(new(Math.Clamp((point.X - uv.X) / uv.Width, 0, .999f),
                Math.Clamp((point.Y - uv.Y) / uv.Height, 0, .999f)), Byte(coverage), Byte(Math.Min(light * lightScale, 1)), 0);
        }
        return result;
    }

    public static RobotFlagComposite.Surface[] Maple(Color[] neutral,int width,int height,RobotParts part=RobotParts.Head,
        RectangleF? paintBounds=null)
    {
        if(width<1 || height<1 || neutral==null || (long)width*height!=neutral.Length)
            throw new ArgumentException("Head image dimensions do not match its pixels.");
        var result=new RobotFlagComposite.Surface[neutral.Length];
        var bounds=paintBounds ?? new RectangleF(.20f,.15f,.49f,.68f);
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int i=y*width+x;
            var pixel=neutral[i];
            if(pixel.A==0)continue;
            float u=(x+.5f)/width,v=(y+.5f)/height;
            float coverage;
            if(paintBounds.HasValue)
            {
                // Keep the wooden boot heels and the profile face outside the paint.
                coverage=part==RobotParts.Legs
                    ? Math.Clamp((bounds.Y+bounds.Height-v)/.025f,0,1)
                    : Math.Clamp((bounds.X+bounds.Width-u)/.008f,0,1);
            }
            else if(part==RobotParts.Body)
            {
                // Bare end-grain medallion on the chest.
                float dx=(u-.476f)/.133f,dy=(v-.517f)/.204f;
                coverage=Math.Clamp((MathF.Sqrt(dx*dx+dy*dy)-1)/.055f,0,1);
            }
            else if(part==RobotParts.Legs)
                coverage=Math.Clamp((.63f-v)/.025f,0,1);
            else
            {
                float dx=Math.Max(Math.Abs(u-.4235f)-.1025f,0);
                float dy=Math.Max(Math.Abs(v-.534f)-.069f,0)*2/3;
                float face=Math.Clamp((MathF.Sqrt(dx*dx+dy*dy)-.072f)/.008f,0,1);
                coverage=face*Math.Clamp((.686f-u)/.008f,0,1);
            }
            byte diffuse=(byte)Math.Clamp((pixel.R+pixel.G+pixel.B)/3+30,0,255);
            result[i]=new(new(Math.Clamp((u-bounds.X)/bounds.Width,0,.999f),Math.Clamp((v-bounds.Y)/bounds.Height,0,.999f)),
                (byte)(220*coverage),diffuse,0);
        }
        return result;
    }

    public static RobotFlagComposite.Surface[] Polypropylene(Color[] neutral,int width,int height)
    {
        if(width<1 || height<1 || neutral==null || (long)width*height!=neutral.Length)
            throw new ArgumentException("Head image dimensions do not match its pixels.");
        var result=new RobotFlagComposite.Surface[neutral.Length];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){
            int i=y*width+x;float u=(x+.5f)/width,v=(y+.5f)/height;
            // Protect the cream inset, its rim, eyes and smile with a rounded
            // rectangle measured on the approved neutral head, plus a soft edge.
            float dx=Math.Max(Math.Abs(u-.390f)-.168f,0),dy=Math.Max(Math.Abs(v-.537f)-.077f,0);
            float distance=MathF.Sqrt(dx*dx+dy*dy)-.098f;
            float coverage=Math.Clamp(distance/.008f,0,1);
            if(neutral[i].A==0)coverage=0;
            float light=(neutral[i].R+neutral[i].G+neutral[i].B)/(3f*255);
            float reflection=Math.Clamp((light-.738f)/.262f,0,1)*.95f;
            float diffuse=Math.Clamp(light/.612f,0,1);
            // Receding right side compresses the design, as in the approved
            // prototype. Full flag and emblem sampling remains shared C# code.
            float surfaceX=(u-.06f)+MathF.Pow(Math.Max(0,u-.68f),2)*2.2f;
            float surfaceY=(v-.15f)+.032f*MathF.Sin((u-.1f)*MathF.PI);
            result[i]=new(new(surfaceX,surfaceY),Byte(coverage),Byte(diffuse),Byte(reflection));
        }
        return result;
    }
    private static byte Byte(float value)=>(byte)Math.Clamp((int)MathF.Round(value*255),0,255);
}
