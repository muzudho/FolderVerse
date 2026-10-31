namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    // Presentation only: accepting another command never waits for these clocks.
    private readonly Dictionary<long,(float Start,int Country)> _workReservationEffects=new();
    private readonly Dictionary<(int Node,int Owner,int Number),float> _transportPlanEffects=new();
    private void BeginWorkReservationEffect(Robot robot,int country)
    {
        foreach(var id in _workReservationEffects.Where(e=>_animationTime-e.Value.Start>1.3f).Select(e=>e.Key).ToArray())_workReservationEffects.Remove(id);
        _workReservationEffects[robot.Id]=(_animationTime,country);
    }
    private int WorkReservationOwner(Robot robot)
    {
        if(robot.ShoeOwner>=0)return robot.ShoeOwner;
        if(robot.Disposal || robot.InterceptOnly || robot.FactoryWorker)return robot.Owner;
        if(robot.Id<=0)return -1; // Decorative examples have no reservation identity.
        var order=_setup.Robots.DirectDelivery.Orders.FirstOrDefault(o=>o.RobotId==robot.Id);
        if(order!=null)return order.Owner;
        var march=_setup.Campaign.PlannedOrders.FirstOrDefault(o=>!o.Peaceful && o.RobotIds?.Contains(robot.Id)==true);
        if(march!=null)return march.Ruler;
        if(_warPlanning){
            if(_invasionPicker>=0 && _invasionDraft.Contains(robot.Id))return _invasionSources[_invasionPicker].Owner;
            var draft=_invasionSources.FirstOrDefault(row=>row.Robots.Contains(robot.Id));if(draft!=null)return draft.Owner;
        }
        return -1;
    }
    private float WorkShoeProgress(Robot robot)
        =>_workReservationEffects.TryGetValue(robot.Id,out var effect) && effect.Country==WorkReservationOwner(robot)?Math.Clamp((_animationTime-effect.Start-.28f)/.45f,0,1):1;
    private void DrawWorkReservationEffect(Rectangle glyph,Robot robot,float opacity)
    {
        if(!_workReservationEffects.TryGetValue(robot.Id,out var effect) || effect.Country!=WorkReservationOwner(robot))return;
        float age=_animationTime-effect.Start;if(age<0 || age>1.15f)return;
        // A compact flag stays inside the preview, rather than covering the next robot or its label.
        int height=Math.Clamp(glyph.Height/2,18,42),width=Math.Max(12,height*5/8);
        RobotWorkArt.DrawFlag(_ui,new(glyph.Right-width,glyph.Y,width,height),_setup.OwnerColor(effect.Country),MathF.Sin(age*18)*(1-age/1.15f),opacity);
    }
    private void BeginTransportPlanEffect(TransportInstruction rule)
    {
        foreach(var key in _transportPlanEffects.Where(e=>_animationTime-e.Value>1.3f).Select(e=>e.Key).ToArray())_transportPlanEffects.Remove(key);
        _transportPlanEffects[(_populationNode,TransportOwner,rule.Number)]=_animationTime;
    }
    private void DrawTransportPlanEffect(int row,TransportInstruction rule)
    {
        if(!_transportPlanEffects.TryGetValue((_populationNode,TransportOwner,rule.Number),out float start))return;
        float age=_animationTime-start;if(age<0 || age>1.15f)return;
        // Plans choose future parts, not a particular robot: use a clearly separate instruction marker.
        var glyph=new Rectangle(82,350+row*106,24,36);
        DrawRobotGlyph(glyph,TransportOwner,RobotParts.Complete);
        RobotWorkArt.DrawShoes(_ui,glyph,_setup.OwnerColor(TransportOwner),progress:Math.Clamp((age-.28f)/.45f,0,1));
        RobotWorkArt.DrawFlag(_ui,new(92,glyph.Y,14,24),_setup.OwnerColor(TransportOwner),MathF.Sin(age*18)*(1-age/1.15f));
    }
}
