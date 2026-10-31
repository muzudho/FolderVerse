namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
public sealed class RobotTraining
{
    public int AssemblyPolicy {get;set;}=3;
    public int DeploymentPolicy {get;set;}=3;
    public int WorkerPolicy {get;set;}=3;
    public int RequiredWorkerNative=>WorkerPolicy switch{0=>3,1=>2,_=>1};
    public bool WorkerEligible(Robot robot)=>robot.CanFight && robot.NativeParts(robot.Owner)>=RequiredWorkerNative;
    public int RequiredNative=>AssemblyPolicy switch{0=>3,1=>2,_=>1};
    public bool Eligible(Robot robot)=>robot.CanFight && (DeploymentPolicy!=0 || robot.NativeParts(robot.Owner)==3);
    public IEnumerable<Robot> Order(IEnumerable<Robot> robots)=>DeploymentPolicy==3?robots:DeploymentPolicy==2?robots.OrderBy(r=>r.NativeParts(r.Owner)).ThenBy(r=>r.Id):DeploymentPolicy==1?robots.OrderByDescending(r=>r.NativeParts(r.Owner)).ThenBy(r=>r.Id):robots.OrderBy(r=>r.Id);
}
