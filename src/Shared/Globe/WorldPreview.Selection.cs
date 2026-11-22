namespace FolderVerse;
using System.Collections.Generic;
public sealed partial class WorldPreview
{
    public int CountryFocus {get;set;}=-1;
    public float FocusBrightness {get;set;}=.65f;
    public HashSet<int> FocusCells {get;}=new();
    public int NodeFocus {get;set;}=-1;
    public bool RestrictNodeActions {get;set;}
    public int RegionFocusCell {get;set;}=-1;
    public int SelectedCell {get;set;}=-1;
    public WorldSetup SelectionSetup {get;set;}
    public HashSet<MapTerrainTile> HighlightTiles {get;}=new();
}
