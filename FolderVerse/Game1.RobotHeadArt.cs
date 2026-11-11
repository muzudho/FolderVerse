namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private RobotHeadTexture _robotHeadTextures;
    private RobotHeadTexture _mapleHeadTextures;
    private RobotHeadTexture _paperHeadTextures;
    private RobotHeadTexture _mapleTorsoTextures, _mapleLegTextures;
    private RobotHeadTexture _robotTorsoTextures;
    private RobotHeadTexture _robotLegTextures;

    private RobotHeadTexture FindRobotBodyArt(Rectangle area, Robot robot, RobotParts part, bool allowCaptain = false)
    {
        if (area.Width < 48 || (robot.Role != RobotRole.Soldier && !(allowCaptain && robot.Role == RobotRole.Captain)) || robot.IntegratedParts != 0 ||
            (robot.Parts & part) == 0)
            return null;

        if(UsesMapleArt(robot,part))
            return part==RobotParts.Body
                ? _mapleTorsoTextures??=LoadMapleArt("torso",part,new(.425f,.42f,.10f,.17f))
                : _mapleLegTextures??=LoadMapleArt("legs",part,new(.57f,.51f,.10f,.13f));
        if(!UsesPolypropyleneArt(robot,part))return null;
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
    private static readonly Rectangle MapleTorsoCrop = new(24, 54, 208, 149);
    private static readonly Rectangle MapleLegsCrop = new(52, 43, 147, 169);

    private void DrawRobotBodyPart(RobotHeadTexture cache, Rectangle area, Robot robot, RobotParts part, float opacity)
    {
        bool maple=UsesMapleArt(robot,part);
        var crop = part == RobotParts.Body ? (maple?MapleTorsoCrop:TorsoCrop) : (maple?MapleLegsCrop:LegsCrop);
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
        bool mapleLegs=UsesMapleArt(robot,RobotParts.Legs),mapleTorso=UsesMapleArt(robot,RobotParts.Body);
        Draw(mapleLegs?_mapleLegTextures:_robotLegTextures, RobotParts.Legs, mapleLegs?MapleLegsCrop:LegsCrop, 102, 58, 48);
        Draw(mapleTorso?_mapleTorsoTextures:_robotTorsoTextures, RobotParts.Body, mapleTorso?MapleTorsoCrop:TorsoCrop, 54, 84, 54);
        Draw(FrontHeadTexture(robot), RobotParts.Head, FrontHeadCrop(robot) ?? new(15,38,215,176), 0, 72, 59);
    }
    private bool UsesPolypropyleneArt(Robot robot,RobotParts part)=>_setup.EquipmentMode==RobotEquipmentMode.Materials
        ?robot.Material(part)==RobotMaterial.Polypropylene:!robot.IsCheap(part) && !robot.IsPremium(part);
    private bool UsesMapleArt(Robot robot,RobotParts part)=>_setup.EquipmentMode==RobotEquipmentMode.Materials && robot.Material(part)==RobotMaterial.Maple;
    private bool UsesPaperHeadArt(Robot robot)=>_setup.EquipmentMode==RobotEquipmentMode.Materials &&
        robot.Material(RobotParts.Head)==RobotMaterial.Paper;
    private RobotHeadTexture FrontHeadTexture(Robot robot)=>UsesPaperHeadArt(robot)?_paperHeadTextures:
        UsesMapleArt(robot,RobotParts.Head)?_mapleHeadTextures:_robotHeadTextures;
    private Rectangle? FrontHeadCrop(Robot robot)=>UsesPaperHeadArt(robot)?new(46,51,160,163):
        UsesMapleArt(robot,RobotParts.Head)?new(50,37,154,176):null;
    private RobotHeadTexture LoadMapleArt(string name,RobotParts part,RobotHeadSurface.RectangleF sticker)
    {
        string path=Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots",$"maple-{name}-neutral-v001.png");
        return File.Exists(path)?new(GraphicsDevice,path,CountryEmblemArt.Tiles,stickerBounds:sticker,maplePart:part):null;
    }
    private bool HasRobotHeadArt(Rectangle area,Robot robot,bool allowCaptain=false)
    {
        if(area.Width<48 || (robot.Role!=RobotRole.Soldier && !(allowCaptain && robot.Role==RobotRole.Captain)) || (robot.Parts&RobotParts.Head)==0 ||
            robot.IntegratedParts!=0)return false;
        if(UsesMapleArt(robot,RobotParts.Head))
        {
            _mapleHeadTextures??=LoadMapleArt("head",RobotParts.Head,new(.61f,.61f,.09f,.14f));
            return _mapleHeadTextures!=null;
        }
        if(UsesPaperHeadArt(robot))
        {
            string paperPath=Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots","paper-head-neutral-v001.png");
            if(_paperHeadTextures==null && File.Exists(paperPath))
                _paperHeadTextures=new(GraphicsDevice,paperPath,CountryEmblemArt.Tiles,
                    stickerBounds:new(.595f,.719f,.052f,.069f),paperHead:true);
            return _paperHeadTextures!=null;
        }
        if(!UsesPolypropyleneArt(robot,RobotParts.Head))return false;
        if(_robotHeadTextures!=null)return true;
        string path=Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots","pp-head-neutral-v001.png");
        if(!File.Exists(path))return false;
        _robotHeadTextures=new(GraphicsDevice,path,CountryEmblemArt.Tiles);return true;
    }
    private void DrawRobotHeadArt(Rectangle area,Robot robot,float opacity)
    {
        var bounds=new Rectangle(area.X+area.Width/6,area.Y-area.Height/20,area.Width*2/3,area.Height/3+6);
        var art=FrontHeadTexture(robot);
        _spriteBatch.Draw(art.Get(_setup,robot.Origin(RobotParts.Head),robot.IsDamaged(RobotParts.Head)),bounds,
            FrontHeadCrop(robot),Color.White*opacity);
    }
}
