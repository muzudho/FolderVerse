namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _orientationDragging,_tutorialSlideDragged;
    private Vector2 _orientationDragOffset;
    private bool DragOrientationRobot(MouseState mouse,bool active,bool click)
    {
        if(!IsStatusScreen || MapTransitionActive || OrientationRotating || _net.IsAnimating || _seedDialog.IsOpen){
            _orientationDragging=false;_tutorialSlideDragged=false;_orientationDragOffset=Vector2.Zero;return false;
        }
        var toy=OrientationToyArea;
        if(!_orientationDragging){
            // Keep the surrounding rotation and reset buttons separate from the drag handle.
            var handle=new Rectangle(toy.X+16,toy.Y+4,toy.Width-32,toy.Height-30);
            if(!click || !handle.Contains(_pointer))return false;
            _orientationDragging=true;_tutorialSlideDragged=false;_orientationDragOffset=Vector2.Zero;
            _dragging=_mapDragging=false;_globePressedCell=-1;
            return true;
        }
        if(!active || mouse.LeftButton!=ButtonState.Pressed){
            if(active && mouse.LeftButton==ButtonState.Released && _tutorialSlideDragged && !_statusGlobe && _setup.TutorialStep==3 && _setup.TutorialMapStage==2 && !_tutorialAttackGuide){
                _setup.TutorialMapRotations|=8;_lastAutoSaveTurn=-1;AutoSaveDomestic();
            }
            _tutorialSlideDragged=false;
            _orientationDragging=false;_orientationDragOffset=Vector2.Zero;return true;
        }
        var old=CanvasPoint(_previousMouse);var delta=new Vector2(_pointer.X-old.X,_pointer.Y-old.Y);
        if(_statusGlobe)_world.GlobePan+=new Vector2(delta.X/StatusGlobeArea.Width,delta.Y/StatusGlobeArea.Height);
        else {
            _net.Drag(delta);
            if(_setup.TutorialStep==3 && _setup.TutorialMapStage==2 && !_tutorialAttackGuide && delta.LengthSquared()>0 && (_setup.TutorialMapRotations&8)==0){
                _tutorialSlideDragged=true;
            }
        }
        _orientationDragOffset+=delta;
        // The handle stays inside its small window even during a long map slide.
        float limit=toy.Width*.15f;
        if(_orientationDragOffset.LengthSquared()>limit*limit)_orientationDragOffset=Vector2.Normalize(_orientationDragOffset)*limit;
        return true;
    }
    private static (Rectangle Globe,Rectangle Toy,Rectangle Legend) OrientationAreas(Rectangle area,bool cast)
    {
        if(cast)area=new(area.X,area.Y+64,area.Width,area.Height-64);
        int width=Math.Min(230,Math.Min(area.Width/3,area.Height/3));
        var toy=new Rectangle(area.Right-width-65,area.Y+130,width,width);
        return (area,toy,new Rectangle(toy.X,toy.Bottom-26,toy.Width,26));
    }
    private static Viewport OrientationViewport(Rectangle area,Rectangle canvas)=>new(
        canvas.X+(int)(area.X*canvas.Width/1920f),canvas.Y+(int)(area.Y*canvas.Height/1080f),
        Math.Max(1,(int)(area.Width*canvas.Width/1920f)),Math.Max(1,(int)(area.Height*canvas.Height/1080f)));
    private void DrawOrientationLegend(Rectangle toy,Rectangle legend,string title="方向のロボット")
    {
        _ui.Center(title,legend,.32f,Cream);
    }
    private void DrawOrientationOverlay(Rectangle area,Rectangle canvas,Matrix transform)
    {
        var areas=OrientationAreas(area,false);var toy=areas.Toy;
        _spriteBatch.Begin(transformMatrix:transform);
        int radius=toy.Width/2;
        for(int y=-radius;y<=radius;y++){
            int half=(int)MathF.Sqrt(radius*radius-y*y);
            _ui.Box(new(toy.Center.X-half,toy.Center.Y+y,half*2+1,1),new Color(26,51,65,235));
        }
        _spriteBatch.End();
        var viewport=GraphicsDevice.Viewport;
        var robotBounds=new Rectangle(toy.X+16+(int)_orientationDragOffset.X,toy.Y+4+(int)_orientationDragOffset.Y,toy.Width-32,toy.Height-30);
        GraphicsDevice.Viewport=OrientationViewport(robotBounds,canvas);
        _orientationToy.Draw(_yaw,_pitch,OrientationGlobe?(IsStatusScreen?_world.DisplayOrientation(_yaw,_pitch):_world.ViewOrientation(_yaw,_pitch)):_net.BellyOrientation);
        GraphicsDevice.Viewport=viewport;
        _spriteBatch.Begin(transformMatrix:transform);
        for(int axis=0;axis<3;axis++){
            var at=_orientationToy.ProjectScrew(axis,robotBounds);var away=at-toy.Center.ToVector2();
            if(away.LengthSquared()>1)at+=Vector2.Normalize(away)*18;
            _ui.Center("－"+"XYZ"[axis],new((int)at.X-18,(int)at.Y-10,36,20),.32f,AxisColors[axis]);
        }
        DrawOrientationControls(toy);
        _spriteBatch.End();
    }
}
