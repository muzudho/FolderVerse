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
    private bool InspectorVisible=>_folderTreeOpen || _reignView || _regionCell>=0 || _populationCell>=0 || _movementOpen || _transportOverview || _salvagePlan;
    private bool DomesticPanelOpen=>InspectorVisible && !_nodeInspectorClosing;
    private int NodeInspectorOffset=>(int)MathF.Round(-960*(1-_domesticSlide));
    private Rectangle NodeInspectorBounds=>new(54+NodeInspectorOffset,150,850,825);
    private Rectangle InspectorHandle=>new(904+NodeInspectorOffset,450,64,170);
    private Rectangle InspectorClose=>new(912+NodeInspectorOffset,464,48,48);
    private void ResetInspector(){
        CloseFolderTreeState();
        CloseReservationDirectory();
        _salvageMapTarget=-1;_salvageCatalogOpen=false;_mapOverlayMenu=false;_salvagePlan=false;_hangarAbandonMode=false;_salvageAbandonConfirmation=null;
        if(_salvageRoutesBefore.HasValue){_net.ShowRoutes=_salvageRoutesBefore.Value;_salvageRoutesBefore=null;}
        _inspectorFoldersOpen=false;_strategyRuler=-1;_factionPickerOpen=false;
        _nodeOfficeOpen=false;
        if(_routesBeforeRegionInspector.HasValue){_net.ShowRoutes=_routesBeforeRegionInspector.Value;_routesBeforeRegionInspector=null;}
        _regionCell=-1;
        QueueInspectorCenter(-1);
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
            return; // Primary actions are drawn together after the inspectors.
        }
        var r=InspectorHandle;
        _ui.Box(new(r.X+4,r.Y+5,r.Width,r.Height),new Color(5,17,28));_ui.Box(r,_setup.OwnerColor(_setup.PlayerSlot));
        _ui.Button(InspectorClose,"×",Muted,.75f);
        for(int y=r.Y+90;y<r.Bottom-16;y+=12)_ui.Box(new(r.X+18,y,r.Width-36,4),new Color(30,48,58));
    }
    private void OpenNodeInspector(int id){
        ResetInspector();var node=_setup.Nodes.All[id];_nodeInspectorClosing=false;_reignView=false;
        _populationCell=node.Cell;_populationNode=_inspectedNode=id;_supervisorDialog=0;_hangarDrag=null;_rateTarget=-1;
        _nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=false;_movementOpen=false;RestoreNodeTab();
        QueueInspectorCenter(id);
    }
    private void CloseNodeInspector(){QueueInspectorCenter(_regionCell>=0?_inspectedNode:_populationNode);_nodeInspectorClosing=true;_trainingOpen=false;_hangarDrag=null;_supervisorDialog=0;}
    private void UpdateDomesticSlide(float elapsed){
        float target=DomesticPanelOpen?1:0;
        bool layoutSettled=_domesticSlide==target;
        if(_domesticSlide==0)_domesticMapLeft=MeasureDomesticMapLeft();
        _domesticSlide=target>_domesticSlide?Math.Min(target,_domesticSlide+elapsed/.4f):Math.Max(target,_domesticSlide-elapsed/.4f);
        if(_nodeInspectorClosing && _domesticSlide==0){var node=_inspectorCenterNode;ResetInspector();QueueInspectorCenter(node);}
        if(layoutSettled)UpdateInspectorCenter(elapsed);
    }
    private static readonly Rectangle ReignPortrait=new(56,650,240,240); private Rectangle StrategyToggle=>PrimaryActionBounds(0);
    private bool DomesticViewClick(Point p)
    {
        if(_trainingOpen || _supervisorDialog>0)return false;
        if((_reignView || _regionCell>=0 || _populationCell>=0) && new Rectangle(InspectorFolderButton.X+NodeInspectorOffset,InspectorFolderButton.Y,InspectorFolderButton.Width,InspectorFolderButton.Height).Contains(p)){OpenInspectorFolders();return true;}
        if(StrategyReadOnly && new Rectangle(54+NodeInspectorOffset,96,850,879).Contains(p) && !InspectorClose.Contains(p))return true;
        if(_reignView && !InspectingSupervisor && LessonCatalogButton.Contains(new Point(p.X-NodeInspectorOffset,p.Y))){_screen=Screen.LessonCatalog;return true;}
        if(WandererPolicyClick(p))return true;
        if(InspectorVisible && InspectorClose.Contains(p)){CloseNodeInspector();return true;}
        if(RegionInspectorClick(p))return true;
        if(InspectorVisible)return InspectorHandle.Contains(p);
        if(StrategyToggle.Contains(p)){if(_reignView)CloseNodeInspector();else OpenStrategyInspector();return true;}
        if(WarButton.Contains(p)){if(_movementOpen && _warPlanning)CloseNodeInspector();else{ResetInspector();OpenWarPlanning();}return true;}
        if(TransportOverviewAllowed && TransportOverviewButton.Contains(p)){if(_transportOverview)CloseNodeInspector();else{ResetInspector();OpenTransportOverview();}return true;}
        if(PopulationTurnButton.Contains(p)){EndPlannedTurn();return true;}

        return false;
    }
    private Vector2 PlayerMedallionCenter=>new(ReignPortrait.Center.X+(InspectorVisible?(int)(960*_domesticSlide):0),ReignPortrait.Center.Y);
    private Vector2 OperationMedallionCenter=>PlayerMedallionCenter+new Vector2(_setup.PlayerSlot==_setup.TurnPlayer?0:320,0);
    private void DrawDomesticPortrait()
    {
        int player=_setup.TurnPlayer;DrawRulerMedallion(player,PlayerMedallionCenter);
        _ui.Center(_setup.PlayerSlot==player?"あなたの陣営 / 操作中":"あなたの陣営のターン",new((int)PlayerMedallionCenter.X-145,600,290,30),.36f,Cream);
        if(_setup.PlayerSlot!=player){
            DrawRulerMedallion(_setup.PlayerSlot,OperationMedallionCenter);
            _ui.Center("操作中の手下",new((int)OperationMedallionCenter.X-145,600,290,30),.36f,Cream);
        }
        if(FactionTree().Length>1)_ui.Button(FactionPickerButton,"▼",Accent,.65f);
    }
    private void DrawRulerMedallion(int ruler,Vector2 center)
    {
        var r=new Rectangle((int)center.X-120,(int)center.Y-120,240,240);
        DrawPortraitMedallion(center,_setup.OwnerColor(ruler));
        var image=new Rectangle((int)(center.X-MedallionPortraitRadius),(int)(center.Y-MedallionPortraitRadius),(int)(MedallionPortraitRadius*2),(int)(MedallionPortraitRadius*2));
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.End();var old=GraphicsDevice.ScissorRectangle;
        using(var clip=new RasterizerState{ScissorTestEnable=true}){
            for(int y=-(int)MedallionPortraitRadius;y<(int)MedallionPortraitRadius;y+=2){
                int half=(int)MathF.Sqrt(Math.Max(0,MedallionPortraitRadius*MedallionPortraitRadius-y*y));
                GraphicsDevice.ScissorRectangle=new(canvas.X+(int)((r.Center.X-half)*canvas.Width/1920f),canvas.Y+(int)((r.Center.Y+y)*canvas.Height/1080f),Math.Max(1,(int)(half*2*canvas.Width/1920f)),Math.Max(1,(int)(2*canvas.Height/1080f)));
                _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
                _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],image,_setup.Relations.Powered[ruler]);_spriteBatch.End();
            }
        }
        GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
        DrawPortraitMedallionName(center,_setup.ConquerorFullNames[ruler],_setup.OwnerColor(ruler));
    }
}
