namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private RobotHeadTexture _robotHeadTextures;
    private RobotHeadTexture _foamHeadTextures, _foamTorsoTextures, _foamLegTextures;
    private RobotHeadTexture _mapleHeadTextures;
    private RobotHeadTexture _paperHeadTextures, _paperTorsoTextures, _paperLegTextures;
    private RobotHeadTexture _cardboardHeadTextures, _cardboardTorsoTextures, _cardboardLegTextures;
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
        if(UsesPaperArt(robot,part))
            return part==RobotParts.Body
                ? _paperTorsoTextures??=LoadPaperArt("torso",part,new(.405f,.45f,.11f,.16f))
                : _paperLegTextures??=LoadPaperArt("legs",part,new(.52f,.706f,.116f,.080f));
        if(UsesCardboardArt(robot,part))
            return part==RobotParts.Body
                ? _cardboardTorsoTextures??=LoadCardboardArt("torso",part,new(.44f,.42f,.15f,.18f))
                : _cardboardLegTextures??=LoadCardboardArt("legs",part,new(.58f,.49f,.095f,.13f));
        if (UsesFoamArt(robot, part))
            return part == RobotParts.Body
                ? _foamTorsoTextures ??= LoadFoamArt("torso", part, new(.45f,.43f,.14f,.18f))
                : _foamLegTextures ??= LoadFoamArt("legs", part, new(.58f,.49f,.085f,.13f));
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
    private static readonly Rectangle PaperTorsoCrop = new(18, 56, 216, 152);
    private static readonly Rectangle PaperLegsCrop = new(62, 71, 122, 136);

    private Rectangle FrontBodyCrop(Robot robot,RobotParts part)
    {
        if (UsesFoamArt(robot, part))
            return part == RobotParts.Body ? new(20,47,209,162) : new(52,40,144,194);
        if(UsesCardboardArt(robot,part))return part==RobotParts.Body?new(15,46,220,159):new(32,44,181,169);
        if(UsesPaperArt(robot,part))return part==RobotParts.Body?PaperTorsoCrop:PaperLegsCrop;
        if(UsesMapleArt(robot,part))return part==RobotParts.Body?MapleTorsoCrop:MapleLegsCrop;
        return part==RobotParts.Body?TorsoCrop:LegsCrop;
    }

    private void DrawRobotBodyPart(RobotHeadTexture cache, Rectangle area, Robot robot, RobotParts part, float opacity)
    {
        var crop = FrontBodyCrop(robot,part);
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
        Draw(UsesFoamArt(robot, RobotParts.Legs) ? _foamLegTextures :
            UsesCardboardArt(robot,RobotParts.Legs)?_cardboardLegTextures:
            UsesPaperArt(robot,RobotParts.Legs)?_paperLegTextures:mapleLegs?_mapleLegTextures:_robotLegTextures,
            RobotParts.Legs, FrontBodyCrop(robot,RobotParts.Legs), 102, 58, 48);
        Draw(UsesFoamArt(robot, RobotParts.Body) ? _foamTorsoTextures :
            UsesCardboardArt(robot,RobotParts.Body)?_cardboardTorsoTextures:
            UsesPaperArt(robot,RobotParts.Body)?_paperTorsoTextures:mapleTorso?_mapleTorsoTextures:_robotTorsoTextures,
            RobotParts.Body, FrontBodyCrop(robot,RobotParts.Body), 54, 84, 54);
        Draw(FrontHeadTexture(robot), RobotParts.Head, FrontHeadCrop(robot) ?? new(15,38,215,176), 0, 72, 59);
    }
    private bool UsesPolypropyleneArt(Robot robot,RobotParts part)=>_setup.EquipmentMode==RobotEquipmentMode.Materials
        ?robot.Material(part)==RobotMaterial.Polypropylene:!robot.IsCheap(part) && !robot.IsPremium(part);
    private bool UsesMapleArt(Robot robot,RobotParts part)=>_setup.EquipmentMode==RobotEquipmentMode.Materials && robot.Material(part)==RobotMaterial.Maple;
    private bool UsesPaperArt(Robot robot,RobotParts part)=>_setup.EquipmentMode==RobotEquipmentMode.Materials &&
        robot.Material(part)==RobotMaterial.Paper;
    private bool UsesPaperHeadArt(Robot robot)=>UsesPaperArt(robot,RobotParts.Head);
    private bool UsesCardboardArt(Robot robot,RobotParts part)=>_setup.EquipmentMode==RobotEquipmentMode.Materials &&
        robot.Material(part)==RobotMaterial.Cardboard;
    private bool UsesCardboardHeadArt(Robot robot)=>UsesCardboardArt(robot,RobotParts.Head);
    private RobotHeadTexture LoadCardboardArt(string name,RobotParts part,RobotHeadSurface.RectangleF sticker,
        BattleFacing facing = BattleFacing.South)
    {
        string view=facing switch { BattleFacing.East=>"side", BattleFacing.North=>"back", _=>"neutral" };
        string path=Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots",$"cardboard-{name}-{view}-v001.png");
        return File.Exists(path)?new(GraphicsDevice,path,CountryEmblemArt.Tiles,
            stickerBounds:sticker,cardboardPart:part,cardboardFacing:facing):null;
    }
    private RobotHeadTexture LoadPaperArt(string name,RobotParts part,RobotHeadSurface.RectangleF sticker)
    {
        string path=Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots",$"paper-{name}-neutral-v001.png");
        return File.Exists(path)?new(GraphicsDevice,path,CountryEmblemArt.Tiles,stickerBounds:sticker,paperPart:part):null;
    }
    private bool UsesFoamArt(Robot robot, RobotParts part) =>
        _setup.EquipmentMode == RobotEquipmentMode.Materials && robot.Material(part) == RobotMaterial.Foam;
    private bool UsesFoamHeadArt(Robot robot) => UsesFoamArt(robot, RobotParts.Head);
    private RobotHeadTexture LoadFoamArt(string name, RobotParts part, RobotHeadSurface.RectangleF sticker)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Images", "Robots", $"foam-{name}-neutral-v001.png");
        return File.Exists(path) ? new(GraphicsDevice, path, CountryEmblemArt.Tiles,
            stickerBounds: sticker, foamPart: part) : null;
    }
    private RobotHeadTexture FrontHeadTexture(Robot robot)=>UsesFoamHeadArt(robot)?_foamHeadTextures:
        UsesPaperHeadArt(robot)?_paperHeadTextures:
        UsesCardboardHeadArt(robot)?_cardboardHeadTextures:
        UsesMapleArt(robot,RobotParts.Head)?_mapleHeadTextures:_robotHeadTextures;
    private Rectangle? FrontHeadCrop(Robot robot)=>UsesFoamHeadArt(robot)?new(34,30,179,189):
        UsesPaperHeadArt(robot)?new(46,51,160,163):
        UsesCardboardHeadArt(robot)?new(23,25,209,207):
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
            _paperHeadTextures??=LoadPaperArt("head",RobotParts.Head,new(.554f,.730f,.078f,.068f));
            return _paperHeadTextures!=null;
        }
        if(UsesCardboardHeadArt(robot))
        {
            _cardboardHeadTextures??=LoadCardboardArt("head",RobotParts.Head,new(.58f,.69f,.115f,.105f));
            return _cardboardHeadTextures!=null;
        }
        if(UsesFoamHeadArt(robot))
        {
            _foamHeadTextures ??= LoadFoamArt("head", RobotParts.Head, new(.62f,.26f,.105f,.095f));
            return _foamHeadTextures!=null;
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
