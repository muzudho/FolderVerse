namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool DomesticOperationsAllowed=>_setup.Campaign.Phase==CampaignPhase.Domestic;
    private bool MilitaryOperationsAllowed=>_setup.Campaign.Phase==CampaignPhase.Military;
    private bool _militaryTransportPlayback;
    private bool DeliveryPlayback=>_setup.Campaign.Phase==CampaignPhase.Delivery;
    private IReadOnlyList<RobotShipment> PropagationShipments=>_militaryTransportPlayback?Array.Empty<RobotShipment>():_setup.Robots.Transport.LastTransfers;
    private string PlanningPhaseLabel=>MilitaryOperationsAllowed?"軍事フェーズ":"内政フェーズ";
    private bool PhasePrimaryActionAllowed(int index)=>index==2?MilitaryOperationsAllowed:index is 3 or 4 or 5?DomesticOperationsAllowed:true;
    private void EndDomesticPhase(bool automaticAll=false)
    {
        SaveDomesticCamera();ResetInspector();ClearCurrentPlayerNodeChecks();
        if(!_setup.Campaign.FinishDomestic(_setup,automaticAll,ShowComputerDomestic))
        {
            if(_computerDomesticNodes==null){_world.ShowSetup(_setup,true);RestoreDomesticCamera();}
            return;
        }
        FinishDomesticDelivery();
    }
    private void FinishDomesticDelivery()
    {
        _world.ShowSetup(_setup,true);_robotScenes=Array.Empty<BattleState>();_battleWave=0;
        _robotPendingSummary=false;_robotSummary=true;_militaryTransportPlayback=false;
        _dragging=false;_net.CancelAnimation();_screen=Screen.Transport;_transportAge=0;BeginPropagation();
    }
    private void OpenMilitaryPhase()
    {
        _setup.Campaign.BeginMilitary();_screen=Screen.WorldStatus;
        ResetInspector();RestoreDomesticCamera();_world.ShowSetup(_setup,true);
    }
}
