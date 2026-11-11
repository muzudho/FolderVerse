namespace FolderVerse;

public enum RobotEquipmentMode { Legacy, Materials }
public enum RobotMaterial { Polypropylene, Paper, Foam, Cardboard, Polystyrene, SoftVinyl, Silicone, Maple, Tin }

public readonly record struct RobotEquipment(int Shields, int Daggers, int Rifles)
{
    public int Total => Shields + Daggers + Rifles;
}

public static class RobotMaterials
{
    internal static readonly RobotParts[] Parts = { RobotParts.Head, RobotParts.Body, RobotParts.Legs };

    // Values are thirds of one use. Add the three parts before rounding down.
    internal static RobotEquipment Thirds(RobotMaterial material) => material switch
    {
        RobotMaterial.Paper => new(0,0,1),
        RobotMaterial.Foam => new(5,1,1),
        RobotMaterial.Cardboard => new(2,2,2),
        RobotMaterial.Polystyrene => new(0,6,2),
        RobotMaterial.SoftVinyl => new(7,1,0),
        RobotMaterial.Silicone => new(8,0,3),
        RobotMaterial.Maple => new(4,5,3),
        RobotMaterial.Tin => new(19,22,13),
        _ => new(3,3,4)
    };

    public static string Label(RobotMaterial material) => material switch
    {
        RobotMaterial.Paper => "紙製",
        RobotMaterial.Foam => "発泡スチロール製",
        RobotMaterial.Cardboard => "段ボール製",
        RobotMaterial.Polystyrene => "ポリスチレン製",
        RobotMaterial.SoftVinyl => "ソフトビニール製",
        RobotMaterial.Silicone => "シリコン製",
        RobotMaterial.Maple => "メープル製",
        RobotMaterial.Tin => "ブリキ製",
        _ => "ポリプロピレン製"
    };
}
