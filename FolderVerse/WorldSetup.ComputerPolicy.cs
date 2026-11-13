namespace FolderVerse;

internal enum ComputerAttackPriority { Balanced, Capture, LightDefense }

internal sealed record ComputerPolicy(ComputerAttackPriority AttackPriority,bool KeepGuard)
{
    public int TargetScore(int captives,int defense,bool occupied)=>AttackPriority switch
    {
        ComputerAttackPriority.Capture=>captives*2000-defense*5+(occupied?50:0),
        ComputerAttackPriority.LightDefense=>captives*30-defense*100+(occupied?15:0),
        _=>captives*1000-defense*10+(occupied?50:0)
    };

    public int GuardCount(int available,bool hasGuard)=>KeepGuard && !hasGuard && available>1?1:0;
}

public sealed partial class WorldSetup
{
    private static readonly ComputerPolicy[] DefaultComputerPolicies=
    {
        new(ComputerAttackPriority.Balanced,true),
        new(ComputerAttackPriority.Capture,true),
        new(ComputerAttackPriority.LightDefense,true),
        new(ComputerAttackPriority.Balanced,false),
        new(ComputerAttackPriority.Capture,false),
        new(ComputerAttackPriority.LightDefense,false)
    };
    private ComputerPolicy[] _computerPolicies;

    internal ComputerPolicy CharacterComputerPolicy(int character)=>
        _computerPolicies?[character] ?? DefaultComputerPolicies[character%DefaultComputerPolicies.Length];

    internal ComputerPolicy RulerComputerPolicy(int ruler)=>CharacterComputerPolicy(Looks[ruler].BaseId);

    internal void SetCharacterComputerPolicy(int character,ComputerPolicy policy)
    {
        _computerPolicies??=new ComputerPolicy[ConquerorCatalog.BaseCount];
        _computerPolicies[character]=policy;
    }
}
