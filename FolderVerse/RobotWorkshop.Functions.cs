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
    internal void PreserveFunctionOrigin(int node){TransferredLineLimit=LineLimit;foreach(var line in ProductionLines)line.FunctionOriginNode??=node;}
    public RobotWorkshop[] DisposalFacilities=>new[]{this}.Concat(_disposalAnnexes??Array.Empty<RobotWorkshop>()).ToArray();
    public bool IsDisposing(long id)=>DisposalFacilities.Any(f=>f.DisposalTarget==id);
    internal static RobotWorkshop EmptyFunctions(){var w=new RobotWorkshop();w.RemoveProductionLines();w._lineLimit=-1;return w;}
    internal void AbsorbFunctions(RobotWorkshop other)
    {
        var baseLines=ProductionLines.Take(BaseLineCount).Concat(other.ProductionLines.Take(other.BaseLineCount)).ToArray();
        _workerLines??=new();if(other._workerLines!=null)foreach(var p in other._workerLines)_workerLines.Add(p.Key,p.Value);
        _lineLimit=checked(LineLimit+other.LineLimit);_baseLineCount=baseLines.Length;
        TransferredLineLimit=_lineLimit;
        _storageSlotLimit=checked(ReservedSlotLimit+other.ReservedSlotLimit);ReservedSlots=checked(ReservedSlots+other.ReservedSlots);
        ProductionLines=baseLines.Concat(_workerLines.OrderBy(p=>p.Key).Select(p=>p.Value)).ToArray();
        _disposalAnnexes=(_disposalAnnexes??Array.Empty<RobotWorkshop>()).Concat(other.DisposalFacilities).ToArray();
        AutoAssemblyDisabled|=other.AutoAssemblyDisabled;
    }
}
