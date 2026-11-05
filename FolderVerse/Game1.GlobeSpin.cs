namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _globeSpinning;

    private bool UpdateGlobeSpin(float elapsed,bool active,bool click,bool rightHeld)
    {
        if(!IsStatusScreen || !_statusGlobe){_globeSpinning=false;return false;}
        if(active && click && NetButton(1).Contains(_pointer)){
            _globeSpinning=!_globeSpinning;
            _dragging=_mapDragging=false;_globePressedCell=-1;
            return true;
        }
        if(!_globeSpinning)return false;
        if(active && MapViewBounds.Contains(_pointer) && rightHeld){
            _globeSpinning=false;
            return false;
        }
        if(active && elapsed>0 && !OrientationRotating){
            // Match Demo's 0.12 radians/second, preserving the current camera pose.
            var orientation=_world.ViewOrientation(_yaw,_pitch);
            _world.ManualOrientation=Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(
                orientation*Matrix.CreateRotationY(elapsed*.12f)));
        }
        return false;
    }
}
