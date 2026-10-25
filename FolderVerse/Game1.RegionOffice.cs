namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private int RegionOfficeNode=>RegionMapFocusCell>=0?_inspectedNode:-1;
    private bool RegionOfficeOnLand=>RegionOfficeNode>=0 && _setup.Routes.Land(_setup.Nodes.All[RegionOfficeNode].Cell,_setup.Nodes.All[RegionOfficeNode].Center);
    private string RegionOfficeName=>RegionOfficeOnLand?"地域庁舎":"地域監督船";
    private void DrawRegionOffice()
    {
        int id=RegionOfficeNode;
        if(id>=0)DrawAdjacentOffice(id,"地域監督 / "+RegionOfficeName,RegionOfficeOnLand,false);
    }
    private bool HasConqueredWorld(int ruler)=>_setup.Cells.Length>0 && _setup.Cells.All(c=>_setup.Nodes.FullOwner(c.Id)==ruler);
    private int ConquerorResidenceNode(int ruler)
    {
        if(_setup.TerritoryCounts[ruler]==0)return -1;
        var owned=_setup.Nodes.All.Where(n=>n.Owner==ruler);
        return owned.Where(n=>n.Cell==_setup.Capitals[ruler]).OrderBy(n=>n.Id).FirstOrDefault()?.Id
            ??owned.OrderByDescending(n=>n.Population.Total).ThenBy(n=>n.Id).FirstOrDefault()?.Id??-1;
    }
    private void DrawConquerorResidences()
    {
        if(_salvagePlan)return;
        int ruler=_reignView?(_strategyRuler>=0?_strategyRuler:_setup.PlayerSlot)
            :_populationNode>=0?_setup.Nodes.All[_populationNode].Owner
            :RegionOfficeNode>=0?_setup.Nodes.All[RegionOfficeNode].Owner:_setup.PlayerSlot;
        if(ruler<0)return;
        int id=ConquerorResidenceNode(ruler);if(id<0)return;
        DrawAdjacentOffice(id,_setup.ConquerorNames[ruler]+" / 征服者官邸",true,true);
    }
    private void DrawAdjacentOffice(int id,string title,bool onLand,bool residence)
    {
        if(id<0 || id>=_setup.Nodes.All.Length || MapTransitionActive || _net.IsAnimating)return;
        var node=_setup.Nodes.All[id];var surface=_setup.Cells[node.Cell];Vector2 at;
        if(_statusGlobe){
            if(!_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),surface.Normal,_yaw,_pitch,StatusGlobeArea,out at))return;
        }else at=_net.NodePosition(_setup,node,NetPanel);
        if(!MapViewBounds.Contains(at.ToPoint()))return;
        // Temporary adjacent office annotation, independent of generated world landmarks and saves.
        var center=new Vector2(Math.Clamp(at.X+(residence?-65:60),MapViewBounds.Left+65,MapViewBounds.Right-65),Math.Clamp(at.Y-62,MapViewBounds.Top+88,MapViewBounds.Bottom-60));
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        var old=GraphicsDevice.ScissorRectangle;_spriteBatch.End();
        using var clip=new RasterizerState{ScissorTestEnable=true};GraphicsDevice.ScissorRectangle=OrientationClip(canvas);
        _spriteBatch.Begin(transformMatrix:transform,rasterizerState:clip);
        var ink=new Color(15,31,39);var cream=new Color(247,224,174);var accent=_setup.OwnerColor(node.Owner);
        var delta=center-at;
        _ui.Tile((center+at)/2,new Vector2(delta.Length(),4),MathF.Atan2(delta.Y,delta.X),ink);
        _ui.Tile((center+at)/2,new Vector2(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),cream);
        const float size=22;
        Vector2 P(float x,float y)=>center+new Vector2(x,y)*size;
        void Bar(float x,float y,float w,float h,Color color)=>_ui.Tile(P(x,y),new Vector2(w,h)*size,0,color);
        void Line(float x,float y,float xx,float yy,float width,Color color){var a=P(x,y);var b=P(xx,yy);var d=b-a;_ui.Tile((a+b)/2,new Vector2(d.Length(),width*size),MathF.Atan2(d.Y,d.X),color);}
        void Box(float x,float y,float w,float h,Color color){Bar(x,y,w+.14f,h+.14f,ink);Bar(x,y,w,h,color);}
        if(onLand){
            Box(0,.1f,1.8f,1.1f,cream);Box(0,-.6f,2,.25f,accent);Box(0,.72f,2.1f,.2f,cream);
            foreach(float x in new[]{-.65f,-.32f,.32f,.65f}){Box(x,.07f,.16f,.65f,cream);Bar(x,-.18f,.12f,.18f,new Color(104,167,195));}
            Box(0,.4f,.28f,.55f,accent);Line(0,-.78f,0,-1.35f,.08f,ink);Box(.3f,-1.2f,.55f,.27f,accent);
            if(residence){
                Line(-1,-.78f,0,-1.16f,.2f,ink);Line(0,-1.16f,1,-.78f,.2f,ink);
                Line(-.9f,-.78f,0,-1.1f,.1f,accent);Line(0,-1.1f,.9f,-.78f,.1f,accent);
                Box(0,.78f,2.35f,.15f,cream);
            }
        }else{
            Box(0,-.05f,1.15f,.65f,cream);Box(.28f,-.5f,.3f,.3f,accent);
            foreach(float x in new[]{-.36f,0,.36f})Bar(x,-.12f,.18f,.18f,new Color(104,167,195));
            Line(-1,.25f,-.65f,.65f,.3f,ink);Line(-.65f,.65f,.65f,.65f,.3f,ink);Line(.65f,.65f,1,.25f,.3f,ink);Line(-1,.25f,1,.25f,.3f,ink);
            Line(-.9f,.27f,-.58f,.57f,.16f,accent);Line(-.58f,.57f,.58f,.57f,.16f,accent);Line(.58f,.57f,.9f,.27f,.16f,accent);
            Line(-.4f,-.4f,-.4f,-1.2f,.08f,ink);Box(-.13f,-1.05f,.5f,.27f,accent);
            Line(-1,.85f,-.2f,.85f,.07f,new Color(136,220,240));Line(.1f,.93f,.95f,.93f,.07f,new Color(136,220,240));
        }
        var label=new Rectangle((int)center.X-120,(int)center.Y+26,240,29);
        label.X=Math.Clamp(label.X,MapViewBounds.Left+4,MapViewBounds.Right-label.Width-4);
        _ui.Box(label,ink);_ui.Center(title,label,Math.Min(.32f,(label.Width-8)/Math.Max(1,_font.MeasureString(title).X)),cream);
        _spriteBatch.End();GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
    }
}
