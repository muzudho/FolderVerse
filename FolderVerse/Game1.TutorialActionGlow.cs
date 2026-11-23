namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool TutorialCanPocketRobot=>_setup.Nodes.Current(_setup.PlayerSlot) is { } node &&
        _setup.Robots.Nodes[node.Id].Robots.Any(robot=>PocketRobotSelectable(robot,true));

    // Derive guidance from live input state; never keep a stale screen coordinate.
    private IEnumerable<Rectangle> TutorialActionTargets()
    {
        if(!TutorialReviewAvailable || _autoPlay || _demoActive || _conquestOverview || _tutorialMessagesOpen || _statusMenuOpen)return Array.Empty<Rectangle>();
        if(_tutorialAttackGuide)return new[]{new Rectangle(1360,730,400,60)};
        if(_screen==Screen.Pocket && !_tutorialAttackGuide && _tutorialGuideClosing<0) return _setup.TutorialPocketed || !TutorialCanPocketRobot?new[]{InactiveBack}:new[]{PocketViewport(1),PocketViewport(0)};
        if(!IsStatusScreen || _tutorialGuideClosing>=0 || _tutorialChromeSlide>0 || _tutorialAchievementAge>=0 ||
            _homePersuasionActive || _lastCutInPhase!=PlanningPhaseLabel || _animationTime-_cutInStarted<PhaseCutInSeconds ||
            MapTransitionActive || _net.IsAnimating || _nodeInspectorClosing)return Array.Empty<Rectangle>();
        if(TutorialMapCourse && _setup.TutorialMapStage<6){
            int stage=_setup.TutorialMapStage,bits=_setup.TutorialMapRotations;
            if(stage is 0 or 3)return new[]{MapViewButton};
            if(stage is 2 or 5)return stage==2 && (bits&8)!=0?Array.Empty<Rectangle>():new[]{AxisBotWindow};
            var targets=new List<Rectangle>();
            for(int axis=stage==1?2:0;axis<3;axis++)foreach(int sign in new[]{-1,1}){
                int bit=stage==1?(sign>0?1:4):1<<(4+axis*2+(sign>0?1:0));
                if((bits&bit)==0)targets.Add(AxisBotRotationButton(AxisBotWindow,axis,sign));
            }
            if((bits&(stage==1?2:1<<10))==0)targets.Add(AxisBotResetButton);
            return targets;
        }
        bool attack=_setup.IsPrimaplana || TutorialAttackRequired || _setup.TutorialMapBeforeWar && _setup.TutorialCompletionShown;
        if(!attack){
            if(_setup.TutorialPocketCourse && !_setup.TutorialCompletionShown){
                if(_movementOpen){
                    if(_selectedMoveNode<0)return Array.Empty<Rectangle>();
                    return new[]{InspectorScreenBounds(ConfirmMoveButton)};
                }
                if(_setup.Campaign.PlannedOrders.Any(order=>order.Peaceful && order.Ruler==0))
                    return InspectorVisible?new[]{InspectorClose}:new[]{PrimaryActionBounds(1)};
                if(!_reignView)return InspectorVisible?new[]{InspectorClose}:new[]{StrategyToggle};
                return new[]{InspectorScreenBounds(_setup.TutorialPocketed || !TutorialCanPocketRobot?MovementButton:PocketButton)};
            }
            return _setup.TutorialCompletionShown && !InspectorVisible?new[]{StatusMenuButton}:Array.Empty<Rectangle>();
        }
        if(_invasionPicker>=0){
            var robots=InvasionRobots(_invasionSources[_invasionPicker]);
            var cards=robots.Skip(_invasionPickerPage*InvasionCardsPerPage).Take(InvasionCardsPerPage)
                .Select((robot,index)=>(robot,index)).Where(x=>!_invasionDraft.Contains(x.robot.Id)).Select(x=>InvasionRobotCard(x.index)).ToArray();
            return cards.Length>0?cards:new[]{InvasionPickerDone};
        }
        if(_movementOpen && _warPlanning){
            if(_selectedMoveNode<0)return Array.Empty<Rectangle>(); // Map targets already have their own selection light.
            if(_setup.IsPrimaplana && TutorialHasAttackReservation)return new[]{InspectorClose};
            var choices=new List<Rectangle>();
            foreach(var layout in InvasionVisibleRows()){
                var row=_invasionSources[layout.Index];var robots=InvasionPreviewRobots(row);
                var available=InvasionRobots(row).Select(robot=>robot.Id).ToHashSet();
                for(int slot=layout.FirstSlot;slot<layout.FirstSlot+layout.SlotCount;slot++)if(available.Contains(robots[slot].Id) && !row.Robots.Contains(robots[slot].Id)){
                    var r=InvasionCardAt(layout.Bounds,slot-layout.FirstSlot);r.X+=NodeInspectorOffset;choices.Add(r);
                }
            }
            return choices.Count>0?choices:TutorialHasAttackReservation?new[]{InspectorClose}:Array.Empty<Rectangle>();
        }
        if(InspectorVisible)return Array.Empty<Rectangle>();
        int action=TutorialHasAttackReservation?1:2;
        return MapMarkerMenuAvailable && TutorialPrimaryActionAllowed(action) && PhasePrimaryActionAllowed(action) && PrimaryActionInUse(action)
            ?new[]{PrimaryActionBounds(action)}:Array.Empty<Rectangle>();
    }
    private void DrawTutorialActionGlow()
    {
        float light=.5f+.5f*MathF.Sin(_animationTime*MathHelper.TwoPi/1.6f);
        foreach(var target in TutorialActionTargets()){
            // Keep text readable and the full control clickable, including at the trough.
            for(int spread=12;spread>=2;spread-=2){
                var r=target;r.Inflate(spread,spread);
                var color=new Color(255,232,113)*((.025f+.055f*light)*(14-spread)/12f);
                _ui.Box(new(r.X,r.Y,r.Width,2),color);_ui.Box(new(r.X,r.Bottom-2,r.Width,2),color);
                _ui.Box(new(r.X,r.Y,2,r.Height),color);_ui.Box(new(r.Right-2,r.Y,2,r.Height),color);
            }
            var edge=new Color(255,242,166)*(.25f+.7f*light);
            _ui.Box(new(target.X,target.Y,target.Width,3),edge);_ui.Box(new(target.X,target.Bottom-3,target.Width,3),edge);
            _ui.Box(new(target.X,target.Y,3,target.Height),edge);_ui.Box(new(target.Right-3,target.Y,3,target.Height),edge);
        }
    }
}
