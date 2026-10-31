namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private const float DirectPairTransitionSeconds=.35f;
    private bool _directPairClosing,_directPairFromMain;
    private Rectangle _directFromPanel;
    private float _directFromScale,_directFromGlobeZoom;
    private Vector2 _directFromPan,_directFromGlobePan;
    private Vector3 _directFromAnchor;
    private Rectangle? _directRenderPanel;
    private float DirectPairEase=>MathHelper.SmoothStep(0,1,_directPairSlide);
    private Rectangle DirectPairAnimatedPanel=>LerpDirectRectangle(_directFromPanel,DirectPairMapBounds,DirectPairEase);
    private static Rectangle LerpDirectRectangle(Rectangle a,Rectangle b,float t)=>new(
        (int)MathF.Round(MathHelper.Lerp(a.X,b.X,t)),(int)MathF.Round(MathHelper.Lerp(a.Y,b.Y,t)),
        (int)MathF.Round(MathHelper.Lerp(a.Width,b.Width,t)),(int)MathF.Round(MathHelper.Lerp(a.Height,b.Height,t)));
    private void BeginDirectPairTransition(bool switching)
    {
        _directPairFromMain=!switching;_directTrucks.Clear();_directPairTrucks.Clear();
        _directFromPanel=switching?DirectPairMapBounds:(_statusGlobe?StatusGlobeArea:NetPanel);
        var net=switching?_directPairNet:_net;var globe=switching?_directPairWorld:_world;
        _directFromScale=net.Fit(_directFromPanel).Scale;_directFromPan=net.Pan;
        _directFromGlobeZoom=globe?.GlobeZoom??1;_directFromGlobePan=globe?.GlobePan??Vector2.Zero;_directFromAnchor=globe?.GlobeAnchor??Vector3.Zero;
        _mapLayerListOpen=_trafficLayerListOpen=_mapDragListOpen=_statusMenuOpen=false;
    }
    private void CloseDirectPair()
    {
        if(_directPairClosing)return;
        // An Escape during a pair-to-pair pan starts the return at the currently visible camera.
        if(!_directPairFromMain){
            float t=DirectPairEase,scale=_directPairNet.Fit(DirectPairMapBounds).Scale;
            _directPairNet.SetFitCamera(DirectPairMapBounds,MathHelper.Lerp(_directFromScale,scale,t),Vector2.Lerp(_directFromPan,_directPairNet.Pan,t));
            if(_directPairGlobe){_directPairWorld.GlobeZoom=MathHelper.Lerp(_directFromGlobeZoom,_directPairWorld.GlobeZoom,t);_directPairWorld.GlobePan=Vector2.Lerp(_directFromGlobePan,_directPairWorld.GlobePan,t);_directPairWorld.GlobeAnchor=Vector3.Lerp(_directFromAnchor,_directPairWorld.GlobeAnchor??Vector3.Zero,t);}
            _directPairSlide=1;
        }
        // Return to the untouched main camera, including after several consecutive pairs.
        BeginDirectPairTransition(false);_directPairClosing=true;_directDrag=null;
    }
    private void UpdateDirectPairTransition(float elapsed)
    {
        _directPairSlide=MathHelper.Clamp(_directPairSlide+(_directPairClosing?-1:1)*elapsed/DirectPairTransitionSeconds,0,1);
        if(_directPairClosing && _directPairSlide==0){_directPairOpen=_directPairClosing=false;_directPairTrucks.Clear();}
    }
    private void DrawDepartingDirectControls()
    {
        if(DirectPairEase>=1 || !_directPairFromMain)return;
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.End();_spriteBatch.Begin(transformMatrix:Matrix.CreateTranslation(0,-160*DirectPairEase,0)*transform);
        _ui.Text("世界征服状況",new(54,22),1.1f,Cream);
        DrawRouteLayerButtons();DrawStatusMenu();
        _spriteBatch.End();_spriteBatch.Begin(transformMatrix:Matrix.CreateTranslation(0,220*DirectPairEase,0)*transform);
        DrawPrimaryActionButtons();
        _spriteBatch.End();_spriteBatch.Begin(transformMatrix:transform);
    }
}
