using FolderVerse;
using System.Reflection;

static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
var record=typeof(ComputerAttackMemory).GetMethod("Record",F)!;
var recent=typeof(ComputerAttackMemory).GetMethod("Recent",F)!;
var world=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};
world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);
world.Nodes.All[1].Owner=1;
var memory=world.Campaign.AttackMemory;
void Record(int source,int force,int turn)=>record.Invoke(memory,new object[]{world,0,source,1,force,turn});
ComputerAttackFailure Recent(int source,int turn)=>(ComputerAttackFailure)recent.Invoke(memory,new object[]{world,0,source,1,turn});
Record(0,2,10);
Check(Recent(0,15)?.Force==2,"Memory must remain at the fifth turn boundary");
Check(Recent(0,16)==null && memory.Failures.Count==0,"Memory must expire after five turns");
Record(0,2,20);Record(0,1,21);
Check(Recent(0,21)?.Force==2 && memory.Failures.Count==1,"A smaller failure must not erase the failed force");
Record(2,3,21);
Check(Recent(2,21)?.Force==3 && Recent(0,21)?.Force==2,"Sources need separate memories");
world.Nodes.All[1].Owner=2;
Check(Recent(0,21)==null,"Changed target ownership must invalidate memory");
world.Nodes.All[1].Owner=1;memory.Reset();Record(0,2,20);
world.Relations.SetSuperior(1,0);
Check(Recent(0,21)==null,"Changed target faction must invalidate memory");
world.Relations.SetSuperior(1,-1);Record(0,2,30);Record(2,3,30);
var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));
Check(restored.Campaign.AttackMemory.Failures.SequenceEqual(memory.Failures),"Campaign memory must survive save restore");
var older=AutoSaveStore.Capture(world);
foreach(var entry in older.Objects.Where(e=>e.Type.StartsWith("FolderVerse.ConquestCampaign,")))
    entry.Fields.Remove("<AttackMemory>k__BackingField");
var compatible=AutoSaveStore.Restore<WorldSetup>(older);
Check(compatible.Campaign.AttackMemory!=null && compatible.Campaign.AttackMemory.Failures.Count==0,"Missing memory field must start empty");
memory.Reset();Record(0,99,world.Population.Turn);
var manual=new March(0,1,1,0,Queen:false,RobotIds:new long[]{123});
var submitted=new List<March>{manual};
typeof(ComputerAttackMemory).GetMethod("Adjust",F)!.Invoke(memory,new object[]{world,submitted,new[]{manual}});
Check(submitted.Count==1 && submitted[0]==manual && memory.PendingOrders.Count==0,"Submitted orders must be protected from computer adjustments");
memory.Reset();
var generated=new List<March>{manual};
typeof(ComputerAttackMemory).GetMethod("Adjust",F)!.Invoke(memory,new object[]{world,generated,Array.Empty<March>()});
var deferred=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));
Check(deferred.Campaign.AttackMemory.PendingOrders.Count==1 &&
    deferred.Campaign.AttackMemory.PendingOrders[0].RobotIds.SequenceEqual(manual.RobotIds),"Deferred computer orders must survive save restore");
world.Campaign.Reset();
Check(memory.Failures.Count==0 && memory.PendingOrders.Count==0,"A new campaign must clear attack memory");
Console.WriteLine("PASS attack memory: boundaries, separate sources, ownership/faction changes, save restore, missing-field compatibility, submitted-order protection, deferred orders, campaign reset.");
