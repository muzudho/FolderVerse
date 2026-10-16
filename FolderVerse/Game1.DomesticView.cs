namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private bool _reignView;
    private float _domesticSlide;
    private bool _nodeInspectorClosing;
    private bool DomesticPanelOpen=>_reignView || _populationCell>=0 && !_nodeInspectorClosing;
    private int NodeInspectorOffset=>(int)MathF.Round(-960*(1-_domesticSlide));
    private Rectangle NodeInspectorBounds=>new(54+NodeInspectorOffset,150,850,825);
    private Rectangle MapExpandButton=>new(ReignPortrait.X+(int)(960*_domesticSlide)+260,ReignPortrait.Y+140,242,64);
    private void OpenNodeInspector(int id){
        var node=_setup.Nodes.All[id];_nodeInspectorClosing=false;_reignView=false;
        _populationCell=node.Cell;_populationNode=_inspectedNode=id;_supervisorDialog=0;_hangarDrag=null;_rateTarget=-1;
        _nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=false;_movementOpen=false;OpenNodeAssembly();
    }
    private void CloseNodeInspector(){_nodeInspectorClosing=true;_hangarDrag=null;_supervisorDialog=0;}
    private void UpdateDomesticSlide(float elapsed){
        float target=DomesticPanelOpen?1:0;
        _domesticSlide=target>_domesticSlide?Math.Min(target,_domesticSlide+elapsed/.4f):Math.Max(target,_domesticSlide-elapsed/.4f);
        if(_nodeInspectorClosing && _domesticSlide==0){_populationCell=_populationNode=_inspectedNode=-1;_nodeInspectorClosing=false;}
    }
    private static readonly Rectangle ReignPortrait=new(56,700,240,240); private static readonly Rectangle StrategyToggle=new(54,1006,242,64);
    private bool DomesticViewClick(Point p)
    {
        if(_populationCell>=0 && MapExpandButton.Contains(p)){CloseNodeInspector();return true;}
        if(_populationCell>=0)return false;
        if(StrategyToggle.Contains(p)){
            if(_movementOpen)CloseMovement(); if(_transportOverview)CloseTransportOverview(); _trainingOpen=false; _reignView=!_reignView;_populationCell=_populationNode=_inspectedNode=-1;return true;
        }

        return false;
    }
    private void DrawDomesticPortrait()
    {
        if(_reignView){_ui.Button(StrategyToggle,"大地図へ戻る",Accent,.65f);return;} var r=ReignPortrait;if(_populationCell>=0)r.Offset((int)(960*_domesticSlide),0);var center=new Vector2(r.Center.X,r.Center.Y);
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
        if(_populationCell>=0){_ui.Button(MapExpandButton,"地図を広げる",Accent,.58f);return;}
        _ui.Button(StrategyToggle,"全体作戦",_reignView?Accent:Muted,.6f); _ui.Box(new(StrategyToggle.X+12,StrategyToggle.Y+23,18,18),Cream); _ui.Box(new(StrategyToggle.X+15,StrategyToggle.Y+26,12,12),_reignView?Accent:new Color(27,49,62));

    }
}
