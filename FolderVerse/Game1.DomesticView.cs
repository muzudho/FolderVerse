namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private Rectangle OrientationClip(Rectangle canvas)=>new(canvas.X+(int)(MapViewBounds.X*canvas.Width/1920f),canvas.Y+(int)(MapViewBounds.Y*canvas.Height/1080f),(int)(MapViewBounds.Width*canvas.Width/1920f),(int)(MapViewBounds.Height*canvas.Height/1080f));
    private float _domesticMapLeft=55;
    private float MeasureDomesticMapLeft(){
        var basis=new Rectangle(40,142,1840,820);float left=float.PositiveInfinity;
        if(_setup.Cells.Length==0 || _net.Faces.Length==0)return 55;
        if(_statusGlobe){
            foreach(var cell in _setup.Cells)foreach(var p in cell.Corners)
                if(_world.ProjectVisible(p,cell.Normal,_yaw,_pitch,basis,out var at))left=Math.Min(left,at.X);
        }else{
            var fit=_net.Fit(basis);
            foreach(var face in _net.Faces)foreach(var cell in _setup.Cells)if(cell.Face==face.Face)foreach(var p in cell.Corners)
                left=Math.Min(left,_net.RotateScreen(face.Project(p)*fit.Scale+fit.Origin,basis).X);
        }
        return float.IsFinite(left)?Math.Max(55,left):55;
    }
    private Rectangle NetPanel=>new(40+(int)Math.Max(0,MapViewBounds.Left+15-_domesticMapLeft),142,1840,820);
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
        CloseDirectDelivery();
        _wandererPolicyOpen=false;
        CancelTransportModal();
        _nodeAssemblyOpen=_nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=false;
        _factoryDropDown=_factoryPlanLine=-1;_factoryLine=0;
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
        if(!InspectorVisible){
            _ui.Button(StrategyToggle,"世界征服計画",Accent,.45f);
            var warLabel=WarReservationLabel;_ui.Button(WarButton,warLabel,Accent,Math.Min(.55f,(WarButton.Width-12)/_font.MeasureString(warLabel).X));
            if(TransportOverviewAllowed){_ui.Button(TransportOverviewButton,"ロボット輸送長期計画",Accent,.42f);_ui.Button(DirectDeliveryButton,"ロボットパーツ直送便",_directMap?Cream:Accent,.45f);}
            _ui.Button(PopulationTurnButton,"ターン終了",Accent,.8f);return;
        }
        var r=InspectorHandle;
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
        if(_domesticSlide==0)_domesticMapLeft=MeasureDomesticMapLeft();
        _domesticSlide=target>_domesticSlide?Math.Min(target,_domesticSlide+elapsed/.4f):Math.Max(target,_domesticSlide-elapsed/.4f);
        if(_nodeInspectorClosing && _domesticSlide==0)ResetInspector();
    }
    private static readonly Rectangle ReignPortrait=new(56,700,240,240); private Rectangle StrategyToggle=>new(54,1006,242,64);
    private bool DomesticViewClick(Point p)
    {
        if(_trainingOpen || _supervisorDialog>0)return false;
        if(TransportOverviewAllowed && !InspectorVisible && DirectDeliveryButton.Contains(p)){if(_directMap)CloseDirectDelivery();else OpenDirectMap();return true;}
        if(_reignView && !InspectingSupervisor && LessonCatalogButton.Contains(new Point(p.X-NodeInspectorOffset,p.Y))){_screen=Screen.LessonCatalog;return true;}
        if(WandererPolicyClick(p))return true;
        if(InspectorVisible && InspectorClose.Contains(p)){CloseNodeInspector();return true;}
        if(InspectorVisible)return InspectorHandle.Contains(p);
        if(StrategyToggle.Contains(p)){if(_reignView)CloseNodeInspector();else OpenStrategyInspector();return true;}
        if(WarButton.Contains(p)){if(_movementOpen && _warPlanning)CloseNodeInspector();else{ResetInspector();OpenWarPlanning();}return true;}
        if(TransportOverviewAllowed && TransportOverviewButton.Contains(p)){if(_transportOverview)CloseNodeInspector();else{ResetInspector();OpenTransportOverview();}return true;}
        if(PopulationTurnButton.Contains(p)){EndPlannedTurn();return true;}

        return false;
    }
    private void DrawDomesticPortrait()
    {
        var r=ReignPortrait;if(InspectorVisible)r.Offset((int)(960*_domesticSlide),0);var center=new Vector2(r.Center.X,r.Center.Y);
        DrawPortraitMedallion(center,_setup.OwnerColor(_setup.PlayerSlot));
        var image=new Rectangle((int)(center.X-MedallionPortraitRadius),(int)(center.Y-MedallionPortraitRadius),(int)(MedallionPortraitRadius*2),(int)(MedallionPortraitRadius*2));
        // Clip the portrait to a circular silhouette using horizontal scissor strips.
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.End();var old=GraphicsDevice.ScissorRectangle;
        using(var clip=new RasterizerState{ScissorTestEnable=true}){
            for(int y=-(int)MedallionPortraitRadius;y<(int)MedallionPortraitRadius;y+=2){
                int half=(int)MathF.Sqrt(Math.Max(0,MedallionPortraitRadius*MedallionPortraitRadius-y*y));
                GraphicsDevice.ScissorRectangle=new(canvas.X+(int)((r.Center.X-half)*canvas.Width/1920f),canvas.Y+(int)((r.Center.Y+y)*canvas.Height/1080f),Math.Max(1,(int)(half*2*canvas.Width/1920f)),Math.Max(1,(int)(2*canvas.Height/1080f)));
                _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
                _portraitRenderer.Draw(_spriteBatch,_setup.Looks[_setup.PlayerSlot],image);_spriteBatch.End();
            }
        }
        GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
        DrawPortraitMedallionName(center,_setup.ConquerorFullNames[_setup.PlayerSlot],_setup.OwnerColor(_setup.PlayerSlot));


    }
}
