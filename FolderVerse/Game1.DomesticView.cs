namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private bool _reignView;
    private float _domesticSlide;
    private bool DomesticPanelOpen=>_reignView || _populationCell>=0;
    private static readonly Rectangle ReignPortrait=new(56,700,240,240); private static readonly Rectangle StrategyToggle=new(54,1006,242,64);
    private bool DomesticViewClick(Point p)
    {
        if(StrategyToggle.Contains(p)){
            if(_movementOpen)CloseMovement(); if(_transportOverview)CloseTransportOverview(); _trainingOpen=false; _reignView=!_reignView;_populationCell=_populationNode=_inspectedNode=-1;return true;
        }
        
        return false;
    }
    private void DrawDomesticPortrait()
    {
        if(_reignView){_ui.Button(StrategyToggle,"大地図へ戻る",Accent,.65f);return;} var r=ReignPortrait;var center=new Vector2(r.Center.X,r.Center.Y);
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
        _ui.Button(StrategyToggle,"全体作戦",_reignView?Accent:Muted,.6f); _ui.Box(new(StrategyToggle.X+12,StrategyToggle.Y+23,18,18),Cream); _ui.Box(new(StrategyToggle.X+15,StrategyToggle.Y+26,12,12),_reignView?Accent:new Color(27,49,62));
        
    }
}