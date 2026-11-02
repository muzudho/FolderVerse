namespace FolderVerse;
using System;

// Simulation durations are integer thirds; factory controls still specify whole turns.
public static class IndustryTime
{
    public const int UnitsPerTurn=3;
    public static int Whole(int turns)=>checked(turns*UnitsPerTurn);
    public static string Format(int units)
    {
        if(units<0)return "-"+Format(-units);
        int whole=units/UnitsPerTurn,remainder=units%UnitsPerTurn;
        return remainder==0?whole.ToString():whole==0?$"{remainder}/3":$"{whole}+{remainder}/3";
    }
}
