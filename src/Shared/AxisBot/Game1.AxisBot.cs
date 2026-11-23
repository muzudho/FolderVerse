namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private AxisBot _axisBot;
    private bool _axisBotDragging,_tutorialSlideDragged;
    private Vector2 _axisBotDragOffset;
    private bool DragAxisBot(MouseState mouse,bool active,bool click)
    {
        if(!IsStatusScreen || MapTransitionActive || AxisBotRotating || _net.IsAnimating || _seedDialog.IsOpen){
            _axisBotDragging=false;_tutorialSlideDragged=false;_axisBotDragOffset=Vector2.Zero;return false;
        }
        var toy=AxisBotWindow;
        if(!_axisBotDragging){
            // Keep the surrounding rotation and reset buttons separate from the drag handle.
            var handle=new Rectangle(toy.X+16,toy.Y+4,toy.Width-32,toy.Height-30);
            if(!click || !handle.Contains(_pointer))return false;
            _axisBotDragging=true;_tutorialSlideDragged=false;_axisBotDragOffset=Vector2.Zero;
            _dragging=_mapDragging=false;_globePressedCell=-1;
            return true;
        }
        if(!active || mouse.LeftButton!=ButtonState.Pressed){
            if(active && mouse.LeftButton==ButtonState.Released && _tutorialSlideDragged && TutorialMapCourse && (!_statusGlobe && _setup.TutorialMapStage==2 || _statusGlobe && _setup.TutorialMapStage==5) && !_tutorialAttackGuide){
                _setup.TutorialMapRotations|=_statusGlobe?TutorialGlobeSlide:8;_lastAutoSaveTurn=-1;AutoSaveDomestic();
            }
            _tutorialSlideDragged=false;
            _axisBotDragging=false;_axisBotDragOffset=Vector2.Zero;return true;
        }
        var old=CanvasPoint(_previousMouse);var delta=new Vector2(_pointer.X-old.X,_pointer.Y-old.Y);
        if(_statusGlobe){
            _world.GlobePan+=new Vector2(delta.X/StatusGlobeArea.Width,delta.Y/StatusGlobeArea.Height);
            if(TutorialMapCourse && _setup.TutorialMapStage==5 && !_tutorialAttackGuide && delta.LengthSquared()>0)_tutorialSlideDragged=true;
        }
        else {
            _net.Drag(delta);
            if(TutorialMapCourse && _setup.TutorialMapStage==2 && !_tutorialAttackGuide && delta.LengthSquared()>0 && (_setup.TutorialMapRotations&8)==0){
                _tutorialSlideDragged=true;
            }
        }
        _axisBotDragOffset+=delta;
        // The handle stays inside its small window even during a long map slide.
        float limit=toy.Width*.15f;
        if(_axisBotDragOffset.LengthSquared()>limit*limit)_axisBotDragOffset=Vector2.Normalize(_axisBotDragOffset)*limit;
        return true;
    }
    private static (Rectangle Globe,Rectangle Toy,Rectangle Legend) AxisBotLayout(Rectangle area,bool cast)
    {
        if(cast)area=new(area.X,area.Y+64,area.Width,area.Height-64);
        int width=Math.Min(230,Math.Min(area.Width/3,area.Height/3));
        var toy=new Rectangle(area.Right-width-65,area.Y+130,width,width);
        return (area,toy,new Rectangle(toy.X,toy.Bottom-26,toy.Width,26));
    }
    private void DrawAxisBotLegend(Rectangle legend)=>_ui.Center(AxisBot.JapaneseName,legend,.32f,Cream);
    private void DrawAxisBotOverlay(Rectangle area,Rectangle canvas,Matrix transform)
    {
        var areas=AxisBotLayout(area,false);var toy=areas.Toy;
        var robotBounds=new Rectangle(toy.X+16+(int)_axisBotDragOffset.X,toy.Y+4+(int)_axisBotDragOffset.Y,toy.Width-32,toy.Height-30);
        var pose=AxisBotUsesGlobe
            ? (IsStatusScreen?_world.DisplayOrientation(_yaw,_pitch):_world.ViewOrientation(_yaw,_pitch))
            : _net.BellyOrientation;
        DrawAxisBotBody(toy,robotBounds,canvas,transform,pose);
        _spriteBatch.Begin(transformMatrix:transform);
        for(int axis=0;axis<3;axis++){
            var at=_axisBot.ProjectScrew(axis,robotBounds);var away=at-toy.Center.ToVector2();
            if(away.LengthSquared()>1)at+=Vector2.Normalize(away)*18;
            _ui.Center("－"+"XYZ"[axis],new((int)at.X-18,(int)at.Y-10,36,20),.32f,AxisBotColors[axis]);
        }
        DrawAxisBotControls(toy);
        _spriteBatch.End();
    }
    private void DrawAxisBotBody(Rectangle toy,Rectangle robotBounds,Rectangle canvas,Matrix transform,Matrix pose)
    {
        _spriteBatch.Begin(transformMatrix:transform);
        int radius=toy.Width/2;
        for(int y=-radius;y<=radius;y++){
            int half=(int)MathF.Sqrt(radius*radius-y*y);
            _ui.Box(new(toy.Center.X-half,toy.Center.Y+y,half*2+1,1),new Color(26,51,65,235));
        }
        _spriteBatch.End();
        var viewport=GraphicsDevice.Viewport;
        GraphicsDevice.Viewport=CanvasViewport(robotBounds,canvas);
        try{_axisBot.Draw((PlanetHeadSystem)_setup.PolarAxis,pose);}
        finally{GraphicsDevice.Viewport=viewport;}
    }
}
