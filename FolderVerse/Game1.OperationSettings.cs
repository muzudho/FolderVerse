namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private bool _operationSettingsOpen;
    private float _operationSettingsSlide;
    private Rectangle OperationSettingsButton=>new(OrientationToyArea.Right+8,OrientationToyArea.Bottom-56,48,56);
    private static readonly Rectangle OperationSettingsClose=new(1792,180,48,48);
    private int OperationSettingsOffset=>(int)MathF.Round(600*(1-MathHelper.SmoothStep(0,1,_operationSettingsSlide)));
    private Rectangle OperationSettingsBounds=>new(1310+OperationSettingsOffset,150,580,830);
    private bool OperationSettingsBlocksMapDrag=>(_operationSettingsOpen || _operationSettingsSlide>0) &&
        (!_operationSettingsOpen || _operationSettingsSlide<1 || OperationSettingsBounds.Contains(_pointer) || OperationSettingsButton.Contains(_pointer) || !MapViewBounds.Contains(_pointer));
    private bool OperationSettingsAvailable=>IsStatusScreen && !_countrySettingsOpen && !_conquestOverview && !_demoActive && !_autoPlay &&
        !_tutorialAttackGuide && !_tutorialMessagesOpen && !_homePersuasionActive && !_directPairOpen && !_salvageCatalogOpen &&
        _invasionPicker<0 && !_hangarDrag.HasValue && _supervisorDialog==0 && !_trainingOpen && !_inspectorFoldersOpen && !_factionPickerOpen && !_wandererPolicyOpen && _rateTarget<0 && !_salvageAbandonConfirmation.HasValue && !_seedDialog.IsOpen;
    private void UpdateOperationSettings(float elapsed)
    {
        if(!IsStatusScreen || _countrySettingsOpen){_operationSettingsOpen=false;_operationSettingsSlide=0;_mapDragListOpen=false;return;}
        _operationSettingsSlide=Math.Clamp(_operationSettingsSlide+(_operationSettingsOpen?1:-1)*Math.Max(0,elapsed)/.3f,0,1);
    }
    private bool OperationSettingsInput(bool click,bool escape)
    {
        if(_operationSettingsOpen || _operationSettingsSlide>0){
            if(escape || click && _operationSettingsSlide==1 && OperationSettingsClose.Contains(_pointer)){
                _operationSettingsOpen=false;_mapDragListOpen=false;return true;
            }
            if(click && _operationSettingsOpen && _operationSettingsSlide==1)MapDragClick(_pointer);
            return true;
        }
        if(!OperationSettingsAvailable || MapTransitionActive || OrientationRotating || !click || !OperationSettingsButton.Contains(_pointer))return false;
        _operationSettingsOpen=true;_mapDragListOpen=_mapLayerListOpen=_trafficLayerListOpen=_statusMenuOpen=false;
        _dragging=_mapDragging=_orientationDragging=_tutorialSlideDragged=false;_orientationDragOffset=Vector2.Zero;_globePressedCell=-1;
        return true;
    }
    private void DrawOperationSettingsButton()
    {
        var r=OperationSettingsButton;
        _ui.Button(r,"",_operationSettingsOpen?Accent:Muted,.4f);
        var ink=UiPainter.ContrastInk(_operationSettingsOpen?Accent:Muted);
        // Mouse silhouette, split buttons and a scroll wheel.
        void Oval(int rx,int ry,Color color){
            for(int y=-ry;y<=ry;y++){
                int half=(int)MathF.Round(rx*MathF.Sqrt(Math.Max(0,1-y*y/(float)(ry*ry))));
                _ui.Box(new(r.Center.X-half,r.Center.Y+y,half*2+1,1),color);
            }
        }
        Oval(14,20,ink);Oval(11,17,_operationSettingsOpen?Accent:Muted);
        _ui.Box(new(r.Center.X-1,r.Center.Y-18,2,17),ink);
        _ui.Box(new(r.Center.X-12,r.Center.Y-2,25,2),ink);
        _ui.Box(new(r.Center.X-2,r.Center.Y-12,4,7),ink);
        if(r.Contains(_pointer))_ui.Text("操作設定",new(r.Right-110,r.Bottom+8),.34f,Cream);
    }
    private void DrawOperationSettings()
    {
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.End();var oldScissor=GraphicsDevice.ScissorRectangle;
        using var clip=new RasterizerState{ScissorTestEnable=true};GraphicsDevice.ScissorRectangle=canvas;
        _spriteBatch.Begin(transformMatrix:Matrix.CreateTranslation(OperationSettingsOffset,0,0)*transform,rasterizerState:clip);
        var hover=_ui.Pointer;_ui.Pointer=new(hover.X-OperationSettingsOffset,hover.Y);
        try{
            _ui.Box(new(1310,160,580,820),new Color(5,17,28,180));
            _ui.Box(new(1320,150,560,820),new Color(27,49,62));
            _ui.Text("操作設定",new(1350,185),.7f,Cream);_ui.Button(OperationSettingsClose,"×",Muted,.65f);
            _ui.Text("マウス",new(1350,240),.45f,Cream);
            DrawMapDragButton();
        }finally{
            _ui.Pointer=hover;_spriteBatch.End();GraphicsDevice.ScissorRectangle=oldScissor;_spriteBatch.Begin(transformMatrix:transform);
        }
    }
}
