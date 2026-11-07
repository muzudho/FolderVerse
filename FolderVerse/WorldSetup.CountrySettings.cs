namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class WorldSetup
{
    // Null means the palette assigned by world setup (also the legacy-save default).
    private int[] _countrySecondaryColors, _countryPatterns;
    private int[] _countryEmblems;
    private void AssignCastCountryAppearance(int count)
    {
        _countryPatterns=new int[count];_countrySecondaryColors=new int[count];_countryEmblems=new int[count];
        var usedPairs=new System.Collections.Generic.HashSet<(int,int)>();
        for(int ruler=0;ruler<count;ruler++){
            int rank=ColorIndices[ruler],primary=rank%Colors.Length;
            var baseColor=Colors[primary];
            double Luma(Color c)=>.2126*c.R+.7152*c.G+.0722*c.B;
            _countrySecondaryColors[ruler]=Enumerable.Range(0,Colors.Length).Where(i=>i!=primary && !usedPairs.Contains((Math.Min(i,primary),Math.Max(i,primary))))
                .OrderByDescending(i=>Math.Abs(Luma(Colors[i])-Luma(baseColor))).ThenBy(i=>i).First();
            int secondary=_countrySecondaryColors[ruler];usedPairs.Add((Math.Min(primary,secondary),Math.Max(primary,secondary)));
            if(count>7){
                _countryPatterns[ruler]=1+(rank+rank/Colors.Length)%4;
                _countryEmblems[ruler]=1+rank%(CountryEmblemNames.Length-1);
            }
        }
    }
    public static readonly string[] CountryEmblemNames={"なし","太陽","月","星","スペード","ハート","ダイヤ","クローバー","炎","水滴","雷","うずまき","雪の結晶","チューリップ"};
    public int CountryEmblem(int ruler)=>ruler<0?0:_countryEmblems?[ruler]??0;
    public void SetCountryEmblem(int ruler,int emblem)
    {
        if(ruler<0 || ruler>=ActiveCount || emblem<0 || emblem>=CountryEmblemNames.Length)throw new ArgumentOutOfRangeException();
        _countryEmblems??=new int[ColorIndices.Length];_countryEmblems[ruler]=emblem;
    }
    private bool[] _showComputerOperations;
    public bool ShowComputerOperations(int ruler)=>_showComputerOperations?[ruler]??false;
    public void ToggleComputerOperations(int ruler)
    {
        if(IsHumanPlayer(ruler))return;
        _showComputerOperations??=new bool[CastCount];
        _showComputerOperations[ruler]=!_showComputerOperations[ruler];
    }
    public static readonly string[] CountryPatternNames={"無地","横縞","水玉","格子","市松"};
    public bool IsHumanPlayer(int ruler)=>!ComputerOnly && (HumanPlayers.Count>0?HumanPlayers.Contains(ruler):ruler==Relations.Leader);
    public void SetHumanPlayer(int ruler,bool human)
    {
        if(ruler<0 || ruler>=ActiveCount)throw new ArgumentOutOfRangeException(nameof(ruler));
        int total=ParticipantCount;
        if(!ComputerOnly && HumanPlayers.Count==0 && Relations.Leader>=0)HumanPlayers.Add(Relations.Leader);
        if(human){if(!HumanPlayers.Contains(ruler))HumanPlayers.Add(ruler);}
        else HumanPlayers.Remove(ruler);
        PlayerCount=HumanPlayers.Count;ComputerPlayerCount=total-PlayerCount;_participantCountsConfigured=true;
        Campaign.ReconcileControllers(this);
    }
    public int CountryPattern(int ruler)=>ruler<0?0:_countryPatterns?[ruler]??ColorIndices[ruler]/Colors.Length%CountryPatternNames.Length;
    public int CountrySecondaryColor(int ruler)
    {
        if(ruler<0)return 0;
        int primary=ColorIndices[ruler]%Colors.Length,secondary=_countrySecondaryColors?[ruler]??(ColorIndices[ruler]+10)%Colors.Length;
        return secondary==primary?(primary+Colors.Length/2)%Colors.Length:secondary;
    }
    public void SetCountryAppearance(int ruler,int primary,int secondary,int pattern)
    {
        if(ruler<0 || ruler>=ActiveCount || primary<0 || primary>=Colors.Length || secondary<0 || secondary>=Colors.Length || pattern<0 || pattern>=CountryPatternNames.Length)throw new ArgumentOutOfRangeException(nameof(ruler));
        if(secondary==primary)secondary=(primary+Colors.Length/2)%Colors.Length;
        _countryPatterns??=Enumerable.Range(0,ColorIndices.Length).Select(CountryPattern).ToArray();
        _countrySecondaryColors??=Enumerable.Range(0,ColorIndices.Length).Select(CountrySecondaryColor).ToArray();
        ColorIndices[ruler]=primary;_countrySecondaryColors[ruler]=secondary;_countryPatterns[ruler]=pattern;
    }
    // Canonical patterns only; rotated stripe/grid variants are not separate choices.
    // Coordinates are local to the surface, so the pattern follows the rotating map.
    public static bool CountryPatternSecondary(int pattern,int x,int y)
    {
        x=(x%10+10)%10;y=(y%10+10)%10;
        return pattern switch {1=>y%5<2,2=>x%5==2 && y%5==2,3=>x%5==0 || y%5==0,4=>(x/2+y/2)%2==0,_=>false};
    }
    public Color CountryMapColor(int ruler,int x,int y)=>ruler<0?OwnerColor(ruler):CountryPatternSecondary(CountryPattern(ruler),x,y)?Colors[CountrySecondaryColor(ruler)]:OwnerColor(ruler);
}
