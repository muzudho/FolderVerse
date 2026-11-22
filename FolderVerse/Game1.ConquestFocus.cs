namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    // Standard deviation in 1920x1080 logical display pixels, reduced from 7px to 3px after user review.
    private const float ConquestBackgroundSigmaPixels=3f;
    private const float ConquestBlurRenderScale=.5f;
    private Texture2D _conquestFocusedBackdrop;
    private string _conquestFocusKey;
    private WorldSetup _conquestFocusWorld;
    private void EnsureConquestFocusedBackdrop()
    {
        var node=_conquestBroadcast.Location>=0?_setup.Nodes.All[_conquestBroadcast.Location]:null;
        string key=$"{node?.Id}/{node?.Terrain}/{(node==null?-1:NodeSupervisors.For(node,_setup.Cells[node.Cell].Face))}/{_conquestBroadcast.Outdoor}/{ConquestBackgroundSigmaPixels}";
        if(_conquestFocusedBackdrop!=null && ReferenceEquals(_conquestFocusWorld,_setup) && _conquestFocusKey==key)return;
        int width=(int)(PortraitHudLayout.MainPanel.Width*ConquestBlurRenderScale),height=(int)(PortraitHudLayout.MainPanel.Height*ConquestBlurRenderScale);
        using var capture=new RenderTarget2D(GraphicsDevice,width,height,false,SurfaceFormat.Color,DepthFormat.None);
        var targets=GraphicsDevice.GetRenderTargets();var viewport=GraphicsDevice.Viewport;
        try{
            GraphicsDevice.SetRenderTarget(capture);GraphicsDevice.Clear(Color.Black);
            var transform=Matrix.CreateTranslation(-PortraitHudLayout.MainPanel.X,0,0)*Matrix.CreateScale(ConquestBlurRenderScale);
            _spriteBatch.Begin(transformMatrix:transform);
            try{DrawConquestLocationBackdrop(node);}finally{_spriteBatch.End();}
        }finally{
            GraphicsDevice.SetRenderTargets(targets);GraphicsDevice.Viewport=viewport;
        }
        var pixels=new Color[width*height];capture.GetData(pixels);
        var blurred=GaussianBlur.Apply(pixels,width,height,ConquestBackgroundSigmaPixels*ConquestBlurRenderScale);
        var texture=new Texture2D(GraphicsDevice,width,height);
        try{texture.SetData(blurred);}catch{texture.Dispose();throw;}
        _conquestFocusedBackdrop?.Dispose();_conquestFocusedBackdrop=texture;
        _conquestFocusKey=key;_conquestFocusWorld=_setup;
    }
    private void DisposeConquestFocus(){_conquestFocusedBackdrop?.Dispose();_conquestFocusedBackdrop=null;_conquestFocusKey=null;_conquestFocusWorld=null;}
}