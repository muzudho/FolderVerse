namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public partial class Game1
{
    private readonly Dictionary<(int Source,int Target),float> _propagationTravel=new();
    private (int Source,int Target)[] _propagationBatch=Array.Empty<(int,int)>();
    private readonly Queue<(int Face,int Side)> _propagationAttachments=new();
    private double _propagationStageAge;
    private bool _propagationMovingFaces;
    private bool PropagationComplete=>_propagationTravel.Values.All(t=>t>=1) && !_propagationNet.IsAnimating;
    private void BeginPropagation()
    {
        _propagationNet.SetCenter(_setup,_net.CenterFace);
        _propagationTravel.Clear();_propagationAttachments.Clear();_propagationBatch=Array.Empty<(int,int)>();
        _propagationStageAge=0;_propagationMovingFaces=false;
        foreach(var s in _setup.Robots.Transport.LastTransfers)_propagationTravel.TryAdd((s.Source,s.Target),0);
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
        var step=_propagationAttachments.Dequeue();
        if(!_propagationNet.AnimateAttachment(step.Face,step.Side))throw new InvalidOperationException("Propagation face attachment failed.");
        _propagationMovingFaces=true;_propagationStageAge=0;
    }
    private void UpdatePropagationMovement(double elapsed)
    {
        // Consume the complete elapsed interval, including low-FPS frames crossing stage boundaries.
        while(elapsed>0 && !PropagationComplete)
        {
            double duration=_propagationMovingFaces?.85:1.6;
            double step=Math.Min(elapsed,duration-_propagationStageAge);elapsed-=step;_propagationStageAge+=step;
            if(_propagationMovingFaces)_propagationNet.UpdateAnimation((float)step);
            else foreach(var route in _propagationBatch)_propagationTravel[route]=(float)Math.Min(1,_propagationStageAge/duration);
            if(_propagationStageAge+1e-6<duration)break;
            if(_propagationMovingFaces)
            {
                _propagationNet.CancelAnimation();
                if(_propagationAttachments.Count>0){StartPropagationAttachment();continue;}
                _propagationMovingFaces=false;
            }
            PreparePropagationBatch();
        }
    }
    private float PropagationTravel(int source,int target)
        =>Microsoft.Xna.Framework.MathHelper.SmoothStep(0,1,_propagationTravel.GetValueOrDefault((source,target),1));
}