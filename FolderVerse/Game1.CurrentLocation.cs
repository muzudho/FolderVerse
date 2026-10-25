namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private bool WandererLocationVisible(out Vector2 at)
    {
        at=Vector2.Zero;int ruler=_setup.PlayerSlot;
        if(ruler<0 || !_setup.Relations.Powered[ruler] || MapTransitionActive || _net.IsAnimating)return false;
        var node=_setup.Nodes.Current(ruler);if(node==null)return false;
        if(_statusGlobe){
            if(!_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),_setup.Cells[node.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at))return false;
        }else at=_net.NodePosition(_setup,node,NetPanel);
        return MapViewBounds.Contains(at.ToPoint());
    }
    private void DrawWandererLocation()
    {
        if(!WandererLocationVisible(out var at)){
            int ruler=_setup.PlayerSlot;
            if(ruler>=0 && _setup.Relations.Powered[ruler] && !MapTransitionActive && !_net.IsAnimating){
                var hint=new Rectangle(MapViewBounds.X+16,MapViewBounds.Y+105,480,38);
                _ui.Box(hint,new(12,31,43));_ui.Center("現在地は画面外 / 征服者はどこ？ で表示",hint,.42f,Cream);
            }
            return;
        }
        var gold=new Color(255,231,133);float radius=15+MathF.Sin(_animationTime*5)*3;
        for(int i=0;i<4;i++){
            float angle=i*MathHelper.PiOver2;var direction=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
            _ui.Tile(at+direction*radius,new(10,3),angle,gold);
        }
        var center=new Vector2(Math.Clamp(at.X-46,MapViewBounds.Left+40,MapViewBounds.Right-40),Math.Clamp(at.Y-56,MapViewBounds.Top+40,MapViewBounds.Bottom-40));
        var delta=at-center;_ui.Tile((center+at)/2,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),gold);
        void Disc(Vector2 point,int r,Color color){for(int y=-r;y<=r;y++){int half=(int)MathF.Sqrt(Math.Max(0,r*r-y*y));_ui.Box(new((int)point.X-half,(int)point.Y+y,Math.Max(1,half*2),1),color);}}
        Disc(center+new Vector2(3,5),37,new(6,18,27));Disc(center+new Vector2(0,3),36,new(142,99,42));Disc(center,36,gold);Disc(center,32,_setup.OwnerColor(_setup.PlayerSlot));Disc(center,29,Cream);
        DrawLocationChipFace(center,27);
        var number=new Rectangle((int)center.X-15,(int)center.Y+21,30,19);_ui.Box(number,gold);_ui.Center("#"+(_setup.PlayerSlot+1),number,.27f,new(52,45,33));
    }
    private void DrawLocationChipFace(Vector2 center,int radius)
    {
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        var image=new Rectangle((int)center.X-radius,(int)center.Y-radius,radius*2,radius*2);
        var old=GraphicsDevice.ScissorRectangle;_spriteBatch.End();
        using(var clip=new RasterizerState{ScissorTestEnable=true}){
            for(int y=-radius;y<radius;y+=2){
                int half=(int)MathF.Sqrt(Math.Max(0,radius*radius-y*y));
                var strip=new Rectangle(canvas.X+(int)((center.X-half)*canvas.Width/1920f),canvas.Y+(int)((center.Y+y)*canvas.Height/1080f),Math.Max(1,(int)(half*2*canvas.Width/1920f)),Math.Max(1,(int)(2*canvas.Height/1080f)));
                GraphicsDevice.ScissorRectangle=Rectangle.Intersect(strip,canvas);_spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
                _portraitRenderer.Draw(_spriteBatch,_setup.Looks[_setup.PlayerSlot],image,faceOnly:true);_spriteBatch.End();
            }
        }
        GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
    }
}
