namespace FolderVerse;

internal enum ComputerAttackPriority { Balanced, Capture, LightDefense }

internal sealed record ComputerPolicy(ComputerAttackPriority AttackPriority,bool KeepGuard)
{
    public int ReleaseExhaustedHarvester {get;init;}=75;
    public int ClearExhaustedHarvestPurpose {get;init;}=75;
    public int ReleaseUnplannedHarvester {get;init;}=50;
    public int ClearUnplannedHarvestPurpose {get;init;}=50;
    public int ReleaseUnsuppliedManufacturer {get;init;}=50;
    public int ClearUnsuppliedFactoryPurpose {get;init;}=50;
    public int ClearUnstaffedFactoryPurpose {get;init;}=75;
    public int NearResourceFactoryPreference {get;init;}=50;
    public int CentralResourceFactoryPreference {get;init;}=50;
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
        _computerPolicies?[character] ?? DefaultComputerPolicies[character%DefaultComputerPolicies.Length] with {
            ReleaseExhaustedHarvester=25+25*(character%4),
            ClearExhaustedHarvestPurpose=25+25*((character/4)%4),
            ReleaseUnplannedHarvester=25+25*((character/3)%4),
            ClearUnplannedHarvestPurpose=25+25*((character/5)%4),
            ReleaseUnsuppliedManufacturer=25+25*((character/2)%4),
            ClearUnsuppliedFactoryPurpose=25+25*((character/7)%4),
            ClearUnstaffedFactoryPurpose=25+25*((character/6)%4),
            NearResourceFactoryPreference=25*(character%5),
            CentralResourceFactoryPreference=100-25*(character%5)
        };

    internal ComputerPolicy RulerComputerPolicy(int ruler)=>CharacterComputerPolicy(Looks[ruler].BaseId);

    internal void SetCharacterComputerPolicy(int character,ComputerPolicy policy)
    {
        _computerPolicies??=new ComputerPolicy[ConquerorCatalog.BaseCount];
        _computerPolicies[character]=policy;
    }
}
