namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private double _globeRockAge;
    private float _globeRockStrength;
    public static Matrix GlobeCradle(double seconds,float strength=1)=>
        Matrix.CreateRotationX((float)Math.Sin(seconds*.43+.6)*MathHelper.ToRadians(4)*strength)*
        Matrix.CreateRotationY((float)Math.Sin(seconds*.31+1)*MathHelper.ToRadians(6)*strength)*
        Matrix.CreateRotationZ((float)Math.Sin(seconds*.37+.4)*MathHelper.ToRadians(2)*strength);
    private void UpdateGlobeRock(double elapsed)
    {
        bool visible=IsStatusScreen && _statusGlobe && !_conquestOverview && !MapTransitionActive;
        if(!visible){_world.GlobeRock=Matrix.Identity;_globeRockStrength=0;return;}
        _globeRockAge+=elapsed;
        float target=_dragging || _mapDragging || OrientationRotating?0:1;
        _globeRockStrength=MathHelper.Lerp(_globeRockStrength,target,1-(float)Math.Exp(-Math.Max(0,elapsed)*4));
        _world.GlobeRock=GlobeCradle(_globeRockAge,_globeRockStrength);
    }
}
