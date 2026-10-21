namespace FolderVerse;
using System;
using System.Linq;

public enum WandererPolicy { Retreat,Win,Persuade,Random }
public sealed partial class WorldSetup
{
    public WandererPolicy[] WandererPolicies=new WandererPolicy[CastCount];
    public WandererPolicy WandererPolicyFor(int ruler)=>WandererPolicies!=null && ruler<WandererPolicies.Length?WandererPolicies[ruler]:WandererPolicy.Retreat;
    public void SetWandererPolicy(int ruler,WandererPolicy policy)
    {
        if(ruler<0 || ruler>=ActiveCount || !Enum.IsDefined(policy))throw new ArgumentOutOfRangeException();
        if(WandererPolicies==null || WandererPolicies.Length!=CastCount)WandererPolicies=new WandererPolicy[CastCount];
        WandererPolicies[ruler]=policy;
    }
}
