namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private readonly DisplayBreaker _displayBreaker=new();
    private string _breakerMessage="";
    private double _breakerPreviousFrame=-1;
    private readonly bool[] _displayFeatureForcedOff=new bool[8];
    private void ApplyDisplayBreakerLatches(){for(int i=0;i<8;i++)if(_displayFeatureForcedOff[i]&&DisplayFeatureEnabled(i))SetDisplayFeature(i,false);}
    private static double DisplayClock=>System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency;
    private static readonly Rectangle DisplayBreakerAutomaticRow=new(780,146,650,44);
    private static readonly string[] DisplayFeatureNames={"経番・緯番","赤道","配備ロボット","電池","煙","隣接オブジェクト","国旗のなびき","地形の高低差"};
    private bool DisplayFeatureEnabled(int i)=>i switch{0=>_net.ShowMapSymbols,1=>_net.ShowEquator,2=>_net.ShowRobots,3=>_net.ShowBatteries,4=>_net.ShowSmoke,5=>_net.ShowAdjacentObjects,6=>_net.AnimateFlags,_=>!WorldTerrain.FlattenRelief};
    private void SetDisplayFeature(int i,bool enabled){
        _displayFeatureForcedOff[i]=!enabled;
        switch(i){case 0:_net.ShowMapSymbols=enabled;break;case 1:_net.ShowEquator=enabled;break;case 2:_net.ShowRobots=enabled;_net.ClearGlobeRobotBadges();break;case 3:_net.ShowBatteries=enabled;_net.ClearGlobeBatteries();break;case 4:_net.ShowSmoke=enabled;break;case 5:_net.ShowAdjacentObjects=enabled;break;case 6:_net.AnimateFlags=enabled;break;case 7:WorldTerrain.FlattenRelief=!enabled;_world.ShowSetup(_setup,true);_net.ClearGlobeRobotBadges();break;}
    }
    private void DrawBreakerCheck(Rectangle r,bool on){_ui.Box(new(r.X+12,r.Y+13,18,18),Cream);_ui.Box(new(r.X+14,r.Y+15,14,14),on?new Color(82,189,133):new Color(18,36,48));}
    private void UpdateDisplayBreaker(double now,double drawMs){
        double frameMs=_breakerPreviousFrame<0?drawMs:Math.Max(0,(now-_breakerPreviousFrame)*1000);_breakerPreviousFrame=now;
        var enabled=new bool[8];for(int i=0;i<8;i++)enabled[i]=DisplayFeatureEnabled(i);
        bool eligible=_loggedActive!=false&&IsStatusScreen&&!_conquestOverview&&!GuideVisible&&!MapTransitionActive&&!OrientationRotating&&!_autoPaused;
        if(_displayBreaker.Observe(now,Math.Max(drawMs,frameMs),enabled,RenderFeatureCost.Milliseconds,eligible) is int item){
            SetDisplayFeature(item,false);_breakerMessage=DisplayFeatureNames[item]+"を自動でOFFにしました";
            _operations.Write("display_breaker_tripped",new{item=DisplayFeatureNames[item],cpuMs=_displayBreaker.LastCost[item],budgetMs=DisplayBreaker.BudgetMilliseconds});
        }
    }
}