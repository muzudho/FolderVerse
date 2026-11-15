namespace FolderVerse;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Rectangle OpeningInspectionMap=new(170,180,1260,595);
    private static readonly Rectangle OpeningInspectionToggle=new(1510,695,320,54);
    private static readonly Rectangle OpeningInspectionToy=new(1557,437,226,226);
    private readonly CubeNet _openingInspectionNet=new()
    {
        ShowRoutes=false,ShowCountryColors=false,DirectoryOnly=true
    };
    private bool _openingInspectionUnfolded,_openingInspectionDragging;

    private void OpeningInspectionDrag(MouseState mouse,bool active)
    {
        if(_screen!=Screen.OpeningReview || _openingInspectionUnfolded || !active ||
            mouse.RightButton!=ButtonState.Pressed)
        {
            _openingInspectionDragging=false;
            return;
        }
        var previous=CanvasPoint(_previousMouse);
        if(_previousMouse.RightButton!=ButtonState.Pressed)
            _openingInspectionDragging=OpeningInspectionMap.Contains(_pointer);
        if(!_openingInspectionDragging)return;
        var delta=_pointer-previous;
        if(_previousMouse.RightButton==ButtonState.Pressed && delta!=Point.Zero)
            _openingReviewPose=Quaternion.Normalize(_openingReviewPose*
                Quaternion.CreateFromYawPitchRoll(delta.X*.008f,delta.Y*.008f,0));
    }

    private void ToggleOpeningInspectionMap()
    {
        _openingInspectionDragging=false;
        _openingInspectionUnfolded=!_openingInspectionUnfolded;
        if(!_openingInspectionUnfolded)return;
        int face=Enumerable.Range(0,CubeNet.Normals.Length)
            .OrderByDescending(i=>Vector3.Transform(CubeNet.Normals[i],_openingReviewPose).Z).First();
        _openingInspectionNet.SetCenter(_setup,face);
        _openingInspectionNet.FitAll(OpeningInspectionMap);
    }

    private void DrawOpeningInspectionToy(Rectangle canvas,Matrix transform)
    {
        var toy=OpeningInspectionToy;
        var model=new Rectangle(toy.X+16,toy.Y+4,toy.Width-32,toy.Height-34);
        var pose=_openingInspectionUnfolded?_openingInspectionNet.BellyOrientation:
            Matrix.CreateFromQuaternion(_openingReviewPose)*_world.GlobeRock;
        DrawOrientationToyBody(toy,model,canvas,transform,pose);
        _spriteBatch.Begin(transformMatrix:transform);
        DrawOrientationLegend(toy,new Rectangle(toy.X,toy.Bottom-26,toy.Width,26));
        _spriteBatch.End();
    }
}
