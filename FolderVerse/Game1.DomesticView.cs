namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private bool _reignView;
    private float _domesticSlide;
    private bool _nodeInspectorClosing;
    private bool InspectorVisible=>_reignView || _populationCell>=0 || _movementOpen || _transportOverview;
    private bool DomesticPanelOpen=>InspectorVisible && !_nodeInspectorClosing;
    private int NodeInspectorOffset=>(int)MathF.Round(-960*(1-_domesticSlide));
    private Rectangle NodeInspectorBounds=>new(54+NodeInspectorOffset,150,850,825);
    private Rectangle InspectorHandle=>new(904+NodeInspectorOffset,450,64,170);
    private Rectangle InspectorClose=>new(912+NodeInspectorOffset,464,48,48);
    private void ResetInspector(){
        if(_movementOpen)CloseMovement();if(_transportOverview)CloseTransportOverview();
        _reignView=false;_populationCell=_populationNode=_inspectedNode=-1;_nodeInspectorClosing=false;_trainingOpen=false;_hangarDrag=null;_supervisorDialog=0;
    }
    private void OpenStrategyInspector(){ResetInspector();_reignView=true;}
    private void DrawInspector(Action draw){
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        var pointer=_pointer;var hover=_ui.Pointer;_pointer.X-=NodeInspectorOffset;_ui.Pointer=new(hover.X-NodeInspectorOffset,hover.Y);
        _spriteBatch.End();_spriteBatch.Begin(transformMatrix:Matrix.CreateTranslation(NodeInspectorOffset,0,0)*transform);
        try{draw();}finally{_pointer=pointer;_ui.Pointer=hover;_spriteBatch.End();_spriteBatch.Begin(transformMatrix:transform);}
    }
    private void DrawInspectorHandle(){
        if(!InspectorVisible)return;var r=InspectorHandle;
        _ui.Box(new(r.X+4,r.Y+5,r.Width,r.Height),new Color(5,17,28));_ui.Box(r,_setup.OwnerColor(_setup.PlayerSlot));
        _ui.Button(InspectorClose,"×",Muted,.75f);
        for(int y=r.Y+90;y<r.Bottom-16;y+=12)_ui.Box(new(r.X+18,y,r.Width-36,4),new Color(30,48,58));
    }
    private void OpenNodeInspector(int id){
        ResetInspector();var node=_setup.Nodes.All[id];_nodeInspectorClosing=false;_reignView=false;
        _populationCell=node.Cell;_populationNode=_inspectedNode=id;_supervisorDialog=0;_hangarDrag=null;_rateTarget=-1;
        _nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=false;_movementOpen=false;OpenNodeAssembly();
    }
    private void CloseNodeInspector(){_nodeInspectorClosing=true;_trainingOpen=false;_hangarDrag=null;_supervisorDialog=0;}
    private void UpdateDomesticSlide(float elapsed){
        float target=DomesticPanelOpen?1:0;
        _domesticSlide=target>_domesticSlide?Math.Min(target,_domesticSlide+elapsed/.4f):Math.Max(target,_domesticSlide-elapsed/.4f);
        if(_nodeInspectorClosing && _domesticSlide==0)ResetInspector();
    }
    private static readonly Rectangle ReignPortrait=new(56,700,240,240); private static readonly Rectangle StrategyToggle=new(54,1006,242,64);
    private bool DomesticViewClick(Point p)
    {
        if(InspectorVisible && InspectorClose.Contains(p)){CloseNodeInspector();return true;}
        if(InspectorVisible && InspectorHandle.Contains(p))return true;
        if(InspectorVisible)return false;
        if(StrategyToggle.Contains(p)){OpenStrategyInspector();return true;}
        if(WarButton.Contains(p)){OpenWarPlanning();return true;}
        if(TransportOverviewButton.Contains(p)){OpenTransportOverview();return true;}

        return false;
    }
    private void DrawDomesticPortrait()
    {
        var r=ReignPortrait;if(InspectorVisible)r.Offset((int)(960*_domesticSlide),0);var center=new Vector2(r.Center.X,r.Center.Y);
        for(int y=-130;y<=130;y++){int half=(int)MathF.Sqrt(130*130-y*y);_ui.Box(new(r.Center.X-half,r.Center.Y+y,half*2+1,1),_setup.OwnerColor(_setup.PlayerSlot));}
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
        if(InspectorVisible)return;
        _ui.Button(StrategyToggle,"全体作戦",Accent,.6f);

    }
}
