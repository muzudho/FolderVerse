namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// Owns generated GPU resources. A Game instance must dispose this with its
// other non-ContentManager textures, on the graphics thread.
public sealed class RobotHeadTexture:IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly Color[] _neutral;
    private readonly RobotFlagComposite.Surface[] _surface;
    private readonly Rectangle _sticker;
    private readonly Vector2 _bandage;
    private readonly byte _wearCoverage;
    private readonly Func<int,IReadOnlyList<Rectangle>> _emblems;
    private readonly Dictionary<(int,int,int,int,int,bool,bool),Texture2D> _textures=new();
    private readonly Queue<(int,int,int,int,int,bool,bool)> _age=new();
    private readonly List<Texture2D> _retired=new();
    private bool _disposed;
    public const int Resolution=256,CacheLimit=16;
    public int Count=>_textures.Count;

    public RobotHeadTexture(GraphicsDevice device,string path,Func<int,IReadOnlyList<Rectangle>> emblems,
        RobotHeadSurface.RectangleF? bodyBounds = null, RobotHeadSurface.RectangleF? stickerBounds = null,
        RobotHeadSurface.RectangleF? protectedFace = null, RobotParts? maplePart = null, RobotParts? paperPart = null,
        BattleFacing paperFacing = BattleFacing.South, RobotParts? cardboardPart = null,
        BattleFacing cardboardFacing = BattleFacing.South, RobotParts? foamPart = null)
    {
        _device=device??throw new ArgumentNullException(nameof(device));
        _emblems=emblems??throw new ArgumentNullException(nameof(emblems));
        using var stream=File.OpenRead(path);
        using var source=Texture2D.FromStream(device,stream);
        var pixels=new Color[source.Width*source.Height];source.GetData(pixels);
        _neutral=new Color[Resolution*Resolution];
        for(int y=0;y<Resolution;y++)for(int x=0;x<Resolution;x++)
            _neutral[y*Resolution+x]=pixels[Math.Min(source.Height-1,(2*y+1)*source.Height/(2*Resolution))*source.Width+Math.Min(source.Width-1,(2*x+1)*source.Width/(2*Resolution))];
        _surface = foamPart is { } foam ? RobotHeadSurface.Foam(_neutral, Resolution, Resolution, foam)
            : cardboardPart is { } cardboard
            ? cardboard == RobotParts.Head ? RobotHeadSurface.CardboardHead(_neutral, Resolution, Resolution, cardboardFacing)
                : RobotHeadSurface.CardboardBody(_neutral, Resolution, Resolution, cardboard, cardboardFacing)
            : paperPart is { } paper ? RobotHeadSurface.Paper(_neutral, Resolution, Resolution, paper, paperFacing)
            : maplePart is {} part ? RobotHeadSurface.Maple(_neutral,Resolution,Resolution,part,bodyBounds) : bodyBounds is { } bounds
            ? RobotHeadSurface.PolypropyleneBody(_neutral, Resolution, Resolution, bounds, protectedFace)
            : RobotHeadSurface.Polypropylene(_neutral, Resolution, Resolution);
        var sticker = stickerBounds ?? new RobotHeadSurface.RectangleF(.70f, .59f, .15f, .16f);
        _sticker = new((int)(Resolution * sticker.X), (int)(Resolution * sticker.Y),
            (int)(Resolution * sticker.Width), (int)(Resolution * sticker.Height));
        _bandage = foamPart switch
        {
            RobotParts.Head => new(.35f,.27f),
            RobotParts.Body => new(.36f,.44f),
            RobotParts.Legs => new(.35f,.53f),
            _ => cardboardPart switch
        {
            RobotParts.Head => cardboardFacing == BattleFacing.South ? new(.37f,.36f) : new(.37f,.46f),
            RobotParts.Body => new(.36f,.44f),
            RobotParts.Legs => cardboardFacing == BattleFacing.East ? new(.34f,.48f) : new(.33f,.50f),
            _ => paperPart switch
        {
            RobotParts.Body => new(.40f,.48f),
            RobotParts.Legs => paperFacing==BattleFacing.East ? new(.38f,.47f) : new(.37f,.57f),
            RobotParts.Head => paperFacing==BattleFacing.South ? new(.36f,.36f) : new(.40f,.49f),
            _ => maplePart switch
        {
            RobotParts.Body=>new(.29f,.44f),
            RobotParts.Legs=>new(.36f,.53f),
            _=>bodyBounds == null ? new(.57f,.32f) : new(.36f,.64f)
        }}}};
        _wearCoverage = foamPart.HasValue ? (byte)135 : maplePart.HasValue ? (byte)220 : (byte)255;
    }
    public Texture2D Get(WorldSetup world,int owner,bool worn=false,bool mirrorSticker=false)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if(world==null || owner<0 || owner>=world.ActiveCount)throw new ArgumentOutOfRangeException(nameof(owner));
        var key=(world.CountryFlagShape(owner),world.CountryPattern(owner),world.ColorIndices[owner]%WorldSetup.Colors.Length,world.CountrySecondaryColor(owner),world.CountryEmblem(owner),worn,mirrorSticker);
        if(_textures.TryGetValue(key,out var existing))return existing;
        var pixels=RobotFlagComposite.Compose(world,owner,_neutral,_surface,fieldOnly:true);
        if (worn)
            ApplyWear(pixels);
        RobotFlagComposite.Sticker(pixels,Resolution,Resolution,_sticker,_emblems(world.CountryEmblem(owner)),WorldSetup.Colors[world.CountrySecondaryColor(owner)],mirrorSticker);
        // SpriteBatch's normal AlphaBlend expects premultiplied colours.
        for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=Color.FromNonPremultiplied(p.R,p.G,p.B,p.A);}
        var texture=new Texture2D(_device,Resolution,Resolution);
        try{texture.SetData(pixels);}catch{texture.Dispose();throw;}
        if(_textures.Count>=CacheLimit){var oldest=_age.Dequeue();_retired.Add(_textures[oldest]);_textures.Remove(oldest);}
        _textures.Add(key,texture);_age.Enqueue(key);return texture;
    }

    private void ApplyWear(Color[] pixels)
    {
        for (int y = 0; y < Resolution; y++)
        for (int x = 0; x < Resolution; x++)
        {
            int index = y * Resolution + x;
            if (_surface[index].Coverage < _wearCoverage || pixels[index].A == 0 || _sticker.Contains(x, y))
                continue;

            float dx = x - _bandage.X * Resolution;
            float dy = y - _bandage.Y * Resolution;
            float length = dx + dy * .35f;
            float cross = dy - dx * .35f;
            Color color = pixels[index];
            if (Math.Abs(length) < 23 && Math.Abs(cross) < 11 && Math.Abs(length) + Math.Abs(cross) < 30)
                color = Math.Abs(length) < 8 ? new Color(255, 231, 195) : new Color(224, 168, 119);
            else if (x < Resolution / 2 && ((x + y) % 13 < 3 || (x - y + Resolution) % 13 < 3))
                color = Color.Lerp(color, _neutral[index], .48f);
            pixels[index] = new Color(color.R, color.G, color.B, pixels[index].A);
        }
    }
    // Call only after SpriteBatch.End (or before the next frame starts).
    public void EndFrame(){foreach(var texture in _retired)texture.Dispose();_retired.Clear();}
    public void Dispose()
    {
        if(_disposed)return;
        foreach(var texture in _textures.Values)texture.Dispose();
        EndFrame();
        _textures.Clear();_age.Clear();_disposed=true;
    }
}
