namespace FolderVerse;

using System;
using Microsoft.Xna.Framework;

public sealed record NationalNewspaper(string Id, string LeftTitle, string RightTitle, Rectangle EntryNumberArea)
{
    public string Title(int entryNumber) => FormattableString.Invariant($"{LeftTitle} {entryNumber} {RightTitle}");
    public string MastheadFile => $"national-{Id}-masthead.png";
    public string DigitsFile => $"national-{Id}-digits.png";
    public const int DigitWidth = 64;
    public const int DigitHeight = 80;
}

public static class NationalNewspapers
{
    private static readonly NationalNewspaper[] Profiles =
    {
        new("breagan", "Bréagán", "Nuacht", new(383, 120, 71, 127)),
        new("tegan", "Tegan", "Papur", new(329, 123, 99, 129)),
        new("leikfang", "Leikfang", "Blað", new(427, 125, 86, 137)),
        new("jostailu", "Jostailu", "Egunkari", new(328, 127, 88, 135)),
    };

    public static NationalNewspaper ForEntry(int fortuneSeed, int entryNumber)
    {
        if (entryNumber < 1) throw new ArgumentOutOfRangeException(nameof(entryNumber));

        // Use a separate sequence so the assignment cannot affect the initial economy.
        var order = (NationalNewspaper[])Profiles.Clone();
        new SeedRandom(fortuneSeed ^ 0x4E455753).Shuffle(order);
        return order[(entryNumber - 1) % order.Length];
    }
}
