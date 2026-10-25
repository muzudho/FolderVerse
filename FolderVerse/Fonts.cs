namespace FolderVerse;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

// Font assets and the last-resort fallback live here. Static glyph coverage is checked before build.
public static class Fonts
{
    public const string Ui="UiFont",Counter="ToyCounterFont";
    public static SpriteFont LoadUi(ContentManager content)
    {
        var font=content.Load<SpriteFont>(Ui);
        // External names/input may contain characters that cannot be predicted from source code.
        // This fallback does not suppress the strict build-time static text check.
        font.DefaultCharacter='?';
        return font;
    }
}
