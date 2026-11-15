namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Rectangle OpeningPaletteButton=new(1230,955,620,70);
    private static readonly Rectangle TitlePaletteButton=new(610,606,700,53);
    private readonly string _terrainPalettePath=Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData),"FolderVerse","terrain-palette.txt");

    private void LoadTerrainPalette()
    {
        try { WorldTerrain.EarthPalette=File.Exists(_terrainPalettePath) && File.ReadAllText(_terrainPalettePath).Trim()=="earth"; }
        catch(IOException) { }
        catch(UnauthorizedAccessException) { }
    }

    private void ToggleTerrainPalette()
    {
        WorldTerrain.EarthPalette=!WorldTerrain.EarthPalette;
        if(_screen is Screen.OpeningLottery or Screen.OpeningReview)
            _world.ShowOpeningPlanet(_setup);
        else if(_screen!=Screen.Title && _setup.Cells.Length>0)
            _world.ShowSetup(_setup,true);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_terrainPalettePath));
            File.WriteAllText(_terrainPalettePath,WorldTerrain.EarthPalette?"earth":"planet");
        }
        catch(IOException error) { RecordFailure(error,"terrain_palette"); }
        catch(UnauthorizedAccessException error) { RecordFailure(error,"terrain_palette"); }
    }

    private void DrawTerrainPaletteButton(Rectangle area)
    {
        bool earth=WorldTerrain.EarthPalette;
        _ui.Button(area,(earth?"✓ ":"□ ")+"海と陸を＜地球＞の色に合わせる",earth?Accent:Muted,.48f);
    }
}
