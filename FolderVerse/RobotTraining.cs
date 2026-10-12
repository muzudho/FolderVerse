namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
public sealed class RobotTraining
{
    public int AssemblyPolicy {get;set;}
    public int DeploymentPolicy {get;set;}
    public int RequiredNative=>AssemblyPolicy switch{0=>3,1=>2,_=>1};
    public bool Eligible(Robot robot)=>robot.CanFight && (DeploymentPolicy!=0 || robot.NativeParts(robot.Owner)==3);
    public IEnumerable<Robot> Order(IEnumerable<Robot> robots)=>DeploymentPolicy==2?robots.OrderBy(r=>r.NativeParts(r.Owner)).ThenBy(r=>r.Id):DeploymentPolicy==1?robots.OrderByDescending(r=>r.NativeParts(r.Owner)).ThenBy(r=>r.Id):robots.OrderBy(r=>r.Id);
}