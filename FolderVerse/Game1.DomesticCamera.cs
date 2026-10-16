namespace FolderVerse;
using Microsoft.Xna.Framework;
public partial class Game1{
 private CubeNetViewState _domesticNetView;
 private bool _domesticGlobeView;
 private float _domesticYaw,_domesticPitch,_domesticZoom;
 private Quaternion? _domesticOrientation;
 private Vector3? _domesticAnchor;
 private void SaveDomesticCamera(){
  _net.CancelAnimation();_domesticNetView=_net.CaptureView();_domesticGlobeView=_statusGlobe;_domesticYaw=_yaw;_domesticPitch=_pitch;
  _domesticOrientation=_world.ManualOrientation;_domesticAnchor=_world.GlobeAnchor;_domesticZoom=_world.GlobeZoom;
 }
 private void RestoreDomesticCamera(){
  if(_domesticNetView==null)return;
  ResetInspector();_domesticSlide=0;_net.RestoreView(_domesticNetView);_statusGlobe=_domesticGlobeView;
  _yaw=_domesticYaw;_pitch=_domesticPitch;_world.ManualOrientation=_domesticOrientation;_world.GlobeAnchor=_domesticAnchor;_world.GlobeZoom=_domesticZoom;
  _domesticNetView=null;
 }
}
