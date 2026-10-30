namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private bool _mapOverlayMenu;
    private Rectangle MapFooterBounds(Rectangle bounds)=>bounds;
    private float _primaryActionSlide;
    private void UpdateMapFooter(float elapsed){
        float target=_directMap?1:0;
        _primaryActionSlide=target>_primaryActionSlide?Math.Min(target,_primaryActionSlide+elapsed/.25f):Math.Max(target,_primaryActionSlide-elapsed/.25f);
    }
    // Five squares and one double-width, 1.5-height end-turn button share a baseline.
    private static Rectangle PrimaryActionHomeBounds(int index)=>index switch {
        0=>new(54,974,96,96),1=>new(160,926,192,144),
        _=>new(362+(index-2)*106,974,96,96)
    };
    private Rectangle PrimaryActionBounds(int index){var r=PrimaryActionHomeBounds(index);if(index!=3)r.Y+=(int)MathF.Round(220*MathHelper.SmoothStep(0,1,_primaryActionSlide));return r;}
    private static Rectangle DirectMapCloseButton=>new(PrimaryActionHomeBounds(3).Right+10,1002,40,40);
    private bool MapMarkerMenuAvailable=>!_directPairOpen && !_trainingOpen && _supervisorDialog==0 && !_inspectorFoldersOpen && !_salvageAbandonConfirmation.HasValue && !_hangarDrag.HasValue && !_seedDialog.IsOpen;
    private bool MapOverlayClick(Point p,bool escape)
    {
        if(!MapMarkerMenuAvailable || InspectorVisible)return false;
        if(escape && _directMap){CloseDirectDelivery();return true;}
        if(escape)return false;
        if(_directMap && DirectMapCloseButton.Contains(p)){CloseDirectDelivery();return true;}
        if(_directMap && PrimaryActionBounds(3).Contains(p))return true;
        if((_directMap || _primaryActionSlide>0) && Enumerable.Range(0,6).Any(i=>i!=3 && (PrimaryActionHomeBounds(i).Contains(p) || PrimaryActionBounds(i).Contains(p))))return true;
        for(int i=0;i<6;i++)if(PrimaryActionBounds(i).Contains(p)){
            if(i==0)OpenStrategyInspector();
            else if(i==1){ResetInspector();EndPlannedTurn();}
            else if(i==2){ResetInspector();OpenWarPlanning();}
            else if(i==3 && TransportOverviewAllowed){OpenDirectMap();}
            else if(i==4 && TransportOverviewAllowed){ResetInspector();OpenTransportOverview();}
            else if(i==5){if(_salvagePlan)ResetInspector();else OpenSalvagePlan();}
            return true;
        }
        return false;
    }
    private void PrimaryRoundBox(Rectangle r,Color color,int radius=12)
    {
        _ui.Box(new(r.X+radius,r.Y,r.Width-radius*2,r.Height),color);
        for(int y=0;y<r.Height;y++){
            int edge=y<radius?radius-y-1:y>=r.Height-radius?y-(r.Height-radius):0;
            int inset=edge>0?radius-(int)MathF.Sqrt(radius*radius-edge*edge):0;
            _ui.Box(new(r.X+inset,r.Y+y,r.Width-inset*2,1),color);
        }
    }
    private void DrawMapOverlayMenu()
    {
        if(!MapMarkerMenuAvailable || InspectorVisible)return;
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.End();var old=GraphicsDevice.ScissorRectangle;
        using var clip=new RasterizerState{ScissorTestEnable=true};GraphicsDevice.ScissorRectangle=canvas;
        _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
        try{DrawPrimaryActionButtons();}finally{_spriteBatch.End();GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);}
    }
    private void DrawPrimaryActionButtons()
    {
        string[] labels={"わたしの国","陣営のターン終了","開戦予約","ロボットパーツ直送便","ロボット輸送長期計画","廃品回収計画"};
        for(int i=0;i<6;i++){
            var r=PrimaryActionBounds(i);if(r.Top>=1080)continue;
            bool enabled=(i==3 || !_directMap && _primaryActionSlide==0) && (i is not (3 or 4) || TransportOverviewAllowed);
            var color=_setup.OwnerColor(_setup.PlayerSlot);
            if(!enabled)color=Color.Lerp(color,new Color(45,49,54),.75f);
            else if(r.Contains(_pointer))color=Color.Lerp(color,Color.White,.22f);
            var shadow=r;shadow.Y+=4;PrimaryRoundBox(shadow,new Color(7,23,35));PrimaryRoundBox(r,color);
            var ink=UiPainter.ContrastInk(color);
            if(i==1){_ui.Center("陣営の",new(r.X,r.Y+32,r.Width,32),.44f,ink);_ui.Center("ターン終了",new(r.X,r.Y+73,r.Width,36),.57f,ink);}
            else {
                DrawPrimaryIcon(i,new(r.X+12,r.Y+8,r.Width-24,58),ink);
                string label=i==3?"パーツ直送便":i==4?"輸送長期計画":labels[i];
                _ui.Center(label,new(r.X+3,r.Bottom-28,r.Width-6,24),Math.Min(.34f,(r.Width-6)/_font.MeasureString(label).X),ink);
            }
            if((i==3 || !_directMap && _primaryActionSlide==0) && r.Contains(_pointer)){
                string hint=labels[i]+(!enabled?"（このステップでは利用できません）":i==2?$"（{WarReservationCount}件）":"");
                var box=new Rectangle(InspectorVisible?1000:54,884,760,34);_ui.Box(box,new Color(7,23,35));_ui.Center(hint,box,.36f,Cream);
            }
        }
        if(_directMap)_ui.Button(DirectMapCloseButton,"×",Muted,.55f);
    }
    private void DrawPrimaryIcon(int kind,Rectangle r,Color ink)
    {
        Vector2 P(float x,float y)=>new(r.X+x*r.Width,r.Y+y*r.Height);
        void Line(float x,float y,float xx,float yy,float width=4){var a=P(x,y);var b=P(xx,yy);var d=b-a;_ui.Tile((a+b)/2,new(d.Length(),width),MathF.Atan2(d.Y,d.X),ink);}
        void Box(float x,float y,float w,float h)=>_ui.Box(new((int)P(x,y).X,(int)P(x,y).Y,(int)(w*r.Width),(int)(h*r.Height)),ink);
        if(kind==0){
            // Project the live owned cells through an independent, unzoomed copy of the current net.
            var faces=_net.Faces;if(faces.Length==0)return;
            var ownedCells=_setup.Nodes.All.Where(n=>n.Owner==_setup.PlayerSlot).Select(n=>n.Cell).ToHashSet();
            float left=faces.Min(f=>f.Center.X-f.Width/2),right=faces.Max(f=>f.Center.X+f.Width/2);
            float top=faces.Min(f=>f.Center.Y-f.Height/2),bottom=faces.Max(f=>f.Center.Y+f.Height/2);
            float scale=Math.Min(r.Width/(right-left),r.Height/(bottom-top));
            foreach(var face in faces)foreach(var cell in _setup.Cells.Where(c=>c.Face==face.Face)){
                var points=cell.Corners.Select(c=>face.Project(c)).ToArray();
                float x=points.Min(p=>p.X),y=points.Min(p=>p.Y),w=points.Max(p=>p.X)-x,h=points.Max(p=>p.Y)-y;
                bool owned=ownedCells.Contains(cell.Id);
                _ui.Box(new(r.X+(int)((x-left)*scale),r.Y+(int)((y-top)*scale),Math.Max(1,(int)(w*scale)),Math.Max(1,(int)(h*scale))),owned?ink:ink*.2f);
            }
        }else if(kind==2){
            Line(.18f,.88f,.83f,.12f,7);Line(.82f,.88f,.17f,.12f,7);
            Line(.08f,.63f,.40f,.94f);Line(.60f,.94f,.92f,.63f);
        }else if(kind is 3 or 4){
            int selected=_populationNode>=0?_populationNode:_inspectedNode;
            var node=selected>=0 && selected<_setup.Nodes.All.Length?_setup.Nodes.All[selected]:_setup.Nodes.Current(_setup.PlayerSlot);
            bool sea=node!=null && !_setup.Routes.Land(node.Cell,node.Center);
            if(sea){Line(.06f,.65f,.86f,.65f,8);Line(.86f,.65f,.70f,.86f,6);Line(.12f,.65f,.24f,.86f,6);Line(.24f,.86f,.70f,.86f,6);Box(.25f,.32f,.40f,.25f);Line(.06f,.96f,.86f,.96f,2);}
            else {Box(.05f,.26f,.48f,.45f);Box(.56f,.43f,.29f,.28f);Box(.12f,.76f,.15f,.16f);Box(.62f,.76f,.15f,.16f);}
            if(kind==4){var c=P(.79f,.24f);for(int a=0;a<360;a+=12){float angle=MathHelper.ToRadians(a);var d=new Vector2(MathF.Cos(angle),MathF.Sin(angle));_ui.Tile(c+d*13,new(3,3),0,ink);}for(int a=-90;a<=0;a+=6){float angle=MathHelper.ToRadians(a);var d=new Vector2(MathF.Cos(angle),MathF.Sin(angle));_ui.Tile(c+d*6,new(13,2),angle,ink);}}
        }else{
            Box(.32f,.16f,.38f,.34f);Box(.39f,.05f,.07f,.11f);var eye=UiPainter.ContrastInk(ink);_ui.Box(new((int)P(.39f,.27f).X,(int)P(.39f,.27f).Y,4,4),eye);_ui.Box(new((int)P(.59f,.27f).X,(int)P(.59f,.27f).Y,4,4),eye);
            Line(.13f,.50f,.23f,.93f);Line(.87f,.50f,.77f,.93f);Line(.23f,.93f,.77f,.93f);Line(.10f,.50f,.90f,.50f,6);
            for(int x=0;x<4;x++)Line(.29f+x*.14f,.57f,.32f+x*.12f,.87f,2);
            Line(.20f,.70f,.80f,.70f,2);
        }
    }
}
