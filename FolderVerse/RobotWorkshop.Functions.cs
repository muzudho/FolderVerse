namespace FolderVerse;
using System;
using System.Linq;

public sealed partial class RobotWorkshop
{
    private RobotWorkshop[] _disposalAnnexes;
    internal int? TransferredLineLimit;
    public int? FunctionOriginNode;
    private int? _storageSlotLimit;
    public int ReservedSlotLimit=>_storageSlotLimit??RobotStore.Capacity;
    internal void SetStorageSlotLimit(int limit)=>_storageSlotLimit=limit;
    internal void PreserveFunctionOrigin(int node){TransferredLineLimit=LineLimit;foreach(var line in ProductionLines)line.FunctionOriginNode??=node;}
    public RobotWorkshop[] DisposalFacilities=>new[]{this}.Concat(_disposalAnnexes??Array.Empty<RobotWorkshop>()).ToArray();
    public bool IsDisposing(long id)=>DisposalFacilities.Any(f=>f.DisposalTarget==id);
    internal static RobotWorkshop EmptyFunctions(){var w=new RobotWorkshop();w.RemoveProductionLines();w._lineLimit=-1;w._storageSlotLimit=0;return w;}
    internal static RobotWorkshop PlanningView(System.Collections.Generic.IEnumerable<RobotWorkshop> sources)
    {var result=EmptyFunctions();foreach(var source in sources)result.AbsorbFunctions(source);return result;}
    internal void ForgetWorkerLine(long id)=>_workerLines?.Remove(id);
    internal void MoveProductionTo(RobotWorkshop destination,int? source)
    {
        if(source.HasValue)foreach(var line in ProductionLines)line.FunctionOriginNode??=source;
        destination.AbsorbProduction(this);
        // The first base lane can be the workshop itself. Leave its production state
        // intact for the destination while detaching it from this workshop's roster.
        ProductionLines=Array.Empty<RobotWorkshop>();
        _workerLines=null;
        _baseLineCount=0;
        _lineLimit=-1;
        TransferredLineLimit=0;
    }
    private void AbsorbProduction(RobotWorkshop other)
    {
        var baseLines=ProductionLines.Take(BaseLineCount).Concat(other.ProductionLines.Take(other.BaseLineCount)).ToArray();
        _workerLines??=new();
        if(other._workerLines!=null)foreach(var worker in other._workerLines)_workerLines.Add(worker.Key,worker.Value);
        int limit=checked(LineLimit+other.LineLimit);
        _lineLimit=limit==0?-1:limit;
        _baseLineCount=baseLines.Length;
        TransferredLineLimit=limit;
        ProductionLines=baseLines.Concat(_workerLines.OrderBy(p=>p.Key).Select(p=>p.Value)).ToArray();
    }
    internal void AbsorbFunctions(RobotWorkshop other)
    {
        AbsorbProduction(other);
        _storageSlotLimit=checked(ReservedSlotLimit+other.ReservedSlotLimit);ReservedSlots=checked(ReservedSlots+other.ReservedSlots);
        _disposalAnnexes=(_disposalAnnexes??Array.Empty<RobotWorkshop>()).Concat(other.DisposalFacilities).ToArray();
        AutoAssemblyDisabled|=other.AutoAssemblyDisabled;
        _materialCatalogueOffset??=other._materialCatalogueOffset;
    }
}
