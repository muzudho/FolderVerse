namespace FolderVerse;
using System;
using System.Linq;

public sealed partial class RobotWorkshop
{
    private ProductionPlan[] _workerPlans;
    private Robot _workerAppointment;
    private ProductionPlan _selectedProductPlan;
    public Robot WorkerSnapshot=>_workerAppointment;
    public ProductionPlan CurrentProductionPlan=>_selectedProductPlan??new ProductionPlan(Product,ProductionPeriod,CheapProduct,IntegratedProduct);
    private static int WorkerPlanSeed(Robot worker,int worldSeed,int node,int country)
    {
        uint hash=2166136261;
        void Add(int value){hash=unchecked((hash^(uint)value)*16777619);}
        Add(worldSeed);Add(node);Add(country);Add((int)worker.Parts);
        foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}){
            if((worker.Parts&part)==0)continue;
            Add(worker.Origin(part));Add(worker.IsCheap(part)?1:0);Add((int)(worker.DamagedParts&part));Add(worker.IsDamaged(part)?1:0);Add((int)worker.ConnectedParts(part));
        }
        return (int)(hash&int.MaxValue);
    }
    private void GenerateWorkerPlans(Robot worker,int worldSeed,int node,int country,bool preserveProduction)
    {
        var active=preserveProduction && Manufacturing?CurrentProductionPlan:null;
        int seed=WorkerPlanSeed(worker,worldSeed,node,country);
        GeneratePlans(seed);
        var plans=new ProductionPlan[11];
        for(int i=0;i<plans.Length;i++)if(i!=3){
            var plan=BasePlanAt(i);plans[i]=plan with {HeadOrigin=country,BodyOrigin=country,LegsOrigin=country,CheapParts=plan.EffectiveCheapParts};
        }
        foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}){
            if(worker.IsCheap(part)){
                var regular=Enumerable.Range(0,11).Where(i=>i!=3 && (plans[i].Product&part)!=0 && (plans[i].EffectiveCheapParts&part)==0).ToArray();
                new SeedRandom(seed^0x38125^(int)part).Shuffle(regular);
                // Keep ceil(N/2) regular choices, including at least one when N > 0.
                foreach(int i in regular.Take(regular.Length/2)){
                    var cheap=plans[i].EffectiveCheapParts|part;
                    plans[i]=plans[i] with {CheapParts=cheap,Cheap=cheap==plans[i].Product};
                }
            }
            if(worker.Origin(part)!=country){
                var options=Enumerable.Range(0,11).Where(i=>i!=3 && (plans[i].Product&part)!=0).ToArray();
                new SeedRandom(seed^0x71259^(int)part).Shuffle(options);
                foreach(int i in options.Take(options.Length/2))plans[i]=part switch{
                    RobotParts.Head=>plans[i] with {HeadOrigin=worker.Origin(part)},
                    RobotParts.Body=>plans[i] with {BodyOrigin=worker.Origin(part)},
                    _=>plans[i] with {LegsOrigin=worker.Origin(part)}
                };
            }
        }
        _workerPlans=plans;
        _workerAppointment=worker.Materialize() with {Id=0,Disposal=false,DefenseOnly=false,DefenseFlag=null,InterceptOnly=false,FactoryWorker=false,LastDomesticPersuasionTurn=-1,LastBattlePersuasionTurn=-1};
        // Legacy saves did not snapshot the catalogue. Preserve their in-progress recipe.
        if(active!=null){_selectedProductPlan=active;SelectedPlan=-1;}
    }
}
