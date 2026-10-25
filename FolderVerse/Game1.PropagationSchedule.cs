namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private readonly Dictionary<(int Source,int Target),float> _propagationTravel=new();
    private (int Source,int Target)[] _propagationBatch=Array.Empty<(int,int)>();
    private readonly Queue<(int Face,int Side)> _propagationAttachments=new();
    private double _propagationStageAge;
    private bool _propagationMovingFaces;
    private bool _propagationCentering;
    private float _propagationFromScale,_propagationToScale;
    private Vector2 _propagationFromPan,_propagationToPan;
    private static readonly Rectangle PropagationMapPanel=new(40,100,1840,860);
    private const double PropagationCenterSeconds=.6;
    private bool PropagationComplete=>_propagationTravel.Values.All(t=>t>=1) && !_propagationNet.IsAnimating && !_propagationCentering;
    private void BeginPropagation()
    {
        _propagationNet.SetCenter(_setup,_net.CenterFace);
        _propagationTravel.Clear();_propagationAttachments.Clear();_propagationBatch=Array.Empty<(int,int)>();
        _propagationStageAge=0;_propagationMovingFaces=_propagationCentering=false;
        foreach(var s in _setup.Robots.Transport.LastTransfers)_propagationTravel.TryAdd((s.Source,s.Target),0);
        foreach(var a in _setup.Salvage.Agents.Where(a=>a.PreviousNode!=a.Node && _setup.Relations.Controls(a.Owner)))_propagationTravel.TryAdd((a.PreviousNode,a.Node),0);
        foreach(var m in _setup.Campaign.RobotBattles.LastMarches)_propagationTravel.TryAdd((m.Source,m.Target),0);
        PreparePropagationBatch();
    }
    private int PropagationFace(int node)=>_setup.Cells[_setup.Nodes.All[node].Cell].Face;
    private void PreparePropagationBatch()
    {
        _propagationBatch=_propagationTravel.Where(p=>p.Value<1 && _propagationNet.FacesJoined(PropagationFace(p.Key.Source),PropagationFace(p.Key.Target))).Select(p=>p.Key).ToArray();
        _propagationStageAge=0;
        if(_propagationBatch.Length>0 || PropagationComplete)return;
        var next=_propagationTravel.First(p=>p.Value<1).Key;
        foreach(var step in _propagationNet.JoinPath(PropagationFace(next.Source),PropagationFace(next.Target)))_propagationAttachments.Enqueue(step);
        StartPropagationAttachment();
    }
    private void StartPropagationAttachment()
    {
        _propagationNet.FitAll(PropagationMapPanel);
        var step=_propagationAttachments.Dequeue();
        if(!_propagationNet.AnimateAttachment(step.Face,step.Side))throw new InvalidOperationException("Propagation face attachment failed.");
        _propagationMovingFaces=true;_propagationStageAge=0;
    }
    private void UpdatePropagationMovement(double elapsed)
    {
        // Consume the complete elapsed interval, including low-FPS frames crossing stage boundaries.
        while(elapsed>0 && !PropagationComplete)
        {
            double duration=_propagationMovingFaces?.85:_propagationCentering?PropagationCenterSeconds:1.6;
            double step=Math.Min(elapsed,duration-_propagationStageAge);elapsed-=step;_propagationStageAge+=step;
            if(_propagationMovingFaces)_propagationNet.UpdateAnimation((float)step);
            else if(_propagationCentering)ApplyPropagationCentering();
            else foreach(var route in _propagationBatch)_propagationTravel[route]=(float)Math.Min(1,_propagationStageAge/duration);
            if(_propagationStageAge+1e-6<duration)break;
            if(_propagationMovingFaces)
            {
                _propagationNet.CancelAnimation();
                _propagationMovingFaces=false;
                _propagationFromScale=_propagationNet.Fit(PropagationMapPanel).Scale;_propagationFromPan=_propagationNet.Pan;
                _propagationNet.FitAll(PropagationMapPanel);
                _propagationToScale=_propagationNet.Fit(PropagationMapPanel).Scale;_propagationToPan=_propagationNet.Pan;
                _propagationCentering=true;_propagationStageAge=0;ApplyPropagationCentering();continue;
            }
            if(_propagationCentering)
            {
                _propagationCentering=false;
                if(_propagationAttachments.Count>0){StartPropagationAttachment();continue;}
            }
            PreparePropagationBatch();
        }
    }
    private void ApplyPropagationCentering()
    {
        float t=MathHelper.SmoothStep(0,1,(float)Math.Clamp(_propagationStageAge/PropagationCenterSeconds,0,1));
        _propagationNet.SetFitCamera(PropagationMapPanel,MathHelper.Lerp(_propagationFromScale,_propagationToScale,t),Vector2.Lerp(_propagationFromPan,_propagationToPan,t));
    }
    private float PropagationTravel(int source,int target)
        =>Microsoft.Xna.Framework.MathHelper.SmoothStep(0,1,_propagationTravel.GetValueOrDefault((source,target),1));
    private float PropagationMarchTravel(int owner,int source,int target)=>PropagationTravel(source,target)*
        (_robotScenes.Any(b=>b.Kind==BattleKind.Edge && b.Armies.Any(a=>a.Owner==owner) &&
            (b.SourceNode==source && b.TargetNode==target || b.SourceNode==target && b.TargetNode==source))?.5f:1);
}
