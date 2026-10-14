namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private bool _reignView;
    private float _domesticSlide;
    private bool DomesticPanelOpen=>_reignView || _populationCell>=0;
    private static readonly Rectangle ReignPortrait=new(56,700,240,240);
    private bool DomesticViewClick(Point p)
    {
        if(Vector2.DistanceSquared(new(p.X,p.Y),new(ReignPortrait.Center.X,ReignPortrait.Center.Y))<=120*120){
            _reignView=!_reignView;_populationCell=_populationNode=_inspectedNode=-1;return true;
        }
        if(_reignView && new Rectangle(803,169,58,44).Contains(p)){_reignView=false;return true;}
        return false;
    }
    private void DrawDomesticPortrait()
    {
        var r=ReignPortrait;var center=new Vector2(r.Center.X,r.Center.Y);
        for(int y=-124;y<=124;y++){int half=(int)MathF.Sqrt(124*124-y*y);_ui.Box(new(r.Center.X-half,r.Center.Y+y,half*2+1,1),Cream);}
        var image=new Rectangle(r.X,r.Y,r.Width,r.Height);
        // Clip the portrait to a circular silhouette using horizontal scissor strips.
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.End();var old=GraphicsDevice.ScissorRectangle;
        using(var clip=new RasterizerState{ScissorTestEnable=true}){
            for(int y=-120;y<120;y+=4){
                int half=(int)MathF.Sqrt(120*120-y*y);
                GraphicsDevice.ScissorRectangle=new(canvas.X+(int)((r.Center.X-half)*canvas.Width/1920f),canvas.Y+(int)((r.Center.Y+y)*canvas.Height/1080f),Math.Max(1,(int)(half*2*canvas.Width/1920f)),Math.Max(1,(int)(4*canvas.Height/1080f)));
                _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
                _portraitRenderer.Draw(_spriteBatch,_setup.Looks[_setup.PlayerSlot],image);_spriteBatch.End();
            }
        }
        GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
        if(ReignPortrait.Contains(_pointer))_ui.Text("君臨ビュー",new(r.X,r.Bottom+8),.5f,Cream);
        if(_reignView)_ui.Button(new(803,169,58,44),"×",Muted,.7f);
    }
}