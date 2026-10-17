namespace FolderVerse;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public partial class Game1{
 private sealed record DomesticViewState(CubeNetViewState Net,bool Globe,float Yaw,float Pitch,
  Quaternion? Orientation,Vector3? Anchor,float Zoom,int GlobeNode,int MapCell,int StatusCell,
  bool Symbols,bool Equator,bool Robots,bool OtherCountries,bool Batteries,bool Routes,bool CountryColors);
 // Runtime preferences belong to each controlled ruler, including separately controlled subordinates.
 private readonly Dictionary<int,DomesticViewState> _domesticViews=new();
 private void SaveDomesticCamera(){
  _net.CancelAnimation();
  bool routes=_movementOpen?_routesBeforeMovement:_transportOverview?_transportRoutesBefore:_net.ShowRoutes;
  _domesticViews[_setup.PlayerSlot]=new(_net.CaptureView(),_statusGlobe,_yaw,_pitch,
   _world.ManualOrientation,_world.GlobeAnchor,_world.GlobeZoom,_globeSelectedNode,_selectedMapCell,_statusCell,
   _net.ShowMapSymbols,_net.ShowEquator,_net.ShowRobots,_net.ShowOtherCountries,_net.ShowBatteries,routes,_net.ShowCountryColors);
 }
 private void RestoreDomesticCamera(){
  ResetInspector();_domesticSlide=0;RestorePlayerView();
 }
 private void RestorePlayerView(){
  _orientationAge=-1;_mapTransitionAge=-1;_dragging=_mapDragging=false;
  _mapLayerListOpen=_trafficLayerListOpen=false;_net.ClearGlobeRobotBadges();
  if(_domesticViews.TryGetValue(_setup.PlayerSlot,out var state)){
   _net.RestoreView(state.Net);_statusGlobe=state.Globe;_yaw=state.Yaw;_pitch=state.Pitch;
   _world.ManualOrientation=state.Orientation;_world.GlobeAnchor=state.Anchor;_world.GlobeZoom=state.Zoom;
   _globeSelectedNode=state.GlobeNode;_selectedMapCell=state.MapCell;_statusCell=state.StatusCell;
   _net.ShowMapSymbols=state.Symbols;_net.ShowEquator=state.Equator;_net.ShowRobots=state.Robots;
   _net.ShowOtherCountries=state.OtherCountries;_net.ShowBatteries=state.Batteries;_net.ShowRoutes=state.Routes;
   _net.ShowCountryColors=state.CountryColors;
  }else{
   _net.CancelAnimation();_net.Home(_setup);_net.ResetView();_statusGlobe=true;
   _world.ManualOrientation=null;_world.GlobeZoom=1;_world.GlobeAnchor=null;
   _selectedMapCell=_statusCell=-1;
   _net.ShowMapSymbols=_net.ShowEquator=_net.ShowRobots=_net.ShowOtherCountries=_net.ShowBatteries=_net.ShowRoutes=true;
   _net.ShowCountryColors=false;FocusCurrentNode();FocusStatusGlobe();
  }
  _world.CountryColors=_net.ShowCountryColors;
  _world.ControlledCountryColors=_net.ControlledCountryColors=!_net.ShowOtherCountries;
  _world.ShowSetup(_setup,true);
 }
}
