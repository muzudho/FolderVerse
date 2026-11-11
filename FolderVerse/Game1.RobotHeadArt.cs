namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private RobotHeadTexture _robotHeadTextures;
    private RobotHeadTexture _robotTorsoTextures;
    private RobotHeadTexture _robotLegTextures;

    private RobotHeadTexture FindRobotBodyArt(Rectangle area, Robot robot, RobotParts part, bool allowCaptain = false)
    {
        if (area.Width < 48 || (robot.Role != RobotRole.Soldier && !(allowCaptain && robot.Role == RobotRole.Captain)) || robot.IntegratedParts != 0 ||
            (robot.Parts & part) == 0 || robot.IsCheap(part) || robot.IsPremium(part))
            return null;

        if (part == RobotParts.Body)
            return _robotTorsoTextures ??= LoadRobotBodyArt("torso",
            new(136f/1536, 128f/1024, 1248f/1536, 800f/1024),
            new(.42f, .35f, .18f, .23f));
        return _robotLegTextures ??= LoadRobotBodyArt("legs",
            new(260f/1536, 136f/1024, 968f/1536, 800f/1024),
            new(.58f, .63f, .15f, .20f));
    }

    private static readonly Rectangle TorsoCrop = new(23, 32, 208, 200);
    private static readonly Rectangle LegsCrop = new(43, 34, 161, 200);

    private void DrawRobotBodyPart(RobotHeadTexture cache, Rectangle area, Robot robot, RobotParts part, float opacity)
    {
        var crop = part == RobotParts.Body ? TorsoCrop : LegsCrop;
        float aspect = crop.Width * 1.5f / crop.Height;
        int width = Math.Max(1, Math.Min(area.Width, (int)Math.Round(area.Height * aspect)));
        int height = Math.Max(1, Math.Min(area.Height, (int)Math.Round(width / aspect)));
        var destination = new Rectangle(area.Center.X - width / 2, area.Center.Y - height / 2, width, height);
        _spriteBatch.Draw(cache.Get(_setup, robot.Origin(part), robot.IsDamaged(part)), destination, crop, Color.White * opacity);
    }
    private RobotHeadTexture LoadRobotBodyArt(string part, RobotHeadSurface.RectangleF paint,
        RobotHeadSurface.RectangleF sticker)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Images", "Robots",
            $"pp-{part}-neutral-v001.png");
        return File.Exists(path) ? new(GraphicsDevice, path, CountryEmblemArt.Tiles, paint, sticker) : null;
    }

    private void DrawCompleteRobotArt(Rectangle area, Robot robot, Func<RobotParts, float> opacity)
    {
        float scale = Math.Min(area.Width / 84f, area.Height / 150f);
        int top = area.Center.Y - (int)Math.Round(75 * scale);
        void Draw(RobotHeadTexture cache, RobotParts part, Rectangle crop, int y, int width, int height)
        {
            int destWidth = (int)Math.Round(width * scale);
            var destination = new Rectangle(area.Center.X - destWidth / 2, top + (int)Math.Round(y * scale),
                destWidth, (int)Math.Round(height * scale));
            _spriteBatch.Draw(cache.Get(_setup, robot.Origin(part), robot.IsDamaged(part)), destination, crop, Color.White * opacity(part));
        }

        // Back to front at the waist and neck. Crops are in 256px cache coordinates.
        Draw(_robotLegTextures, RobotParts.Legs, LegsCrop, 102, 58, 48);
        Draw(_robotTorsoTextures, RobotParts.Body, TorsoCrop, 54, 84, 54);
        Draw(_robotHeadTextures, RobotParts.Head, new(15, 38, 215, 176), 0, 72, 59);
    }
    private bool HasRobotHeadArt(Rectangle area,Robot robot,bool allowCaptain=false)
    {
        if(area.Width<48 || (robot.Role!=RobotRole.Soldier && !(allowCaptain && robot.Role==RobotRole.Captain)) || (robot.Parts&RobotParts.Head)==0 ||
            robot.IntegratedParts!=0 || robot.IsCheap(RobotParts.Head) || robot.IsPremium(RobotParts.Head))return false;
        if(_robotHeadTextures!=null)return true;
        string path=Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots","pp-head-neutral-v001.png");
        if(!File.Exists(path))return false;
        _robotHeadTextures=new(GraphicsDevice,path,CountryEmblemArt.Tiles);return true;
    }
    private void DrawRobotHeadArt(Rectangle area,Robot robot,float opacity)
    {
        var bounds=new Rectangle(area.X+area.Width/6,area.Y-area.Height/20,area.Width*2/3,area.Height/3+6);
        _spriteBatch.Draw(_robotHeadTextures.Get(_setup,robot.Origin(RobotParts.Head),robot.IsDamaged(RobotParts.Head)),bounds,Color.White*opacity);
    }
}
