namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private Microsoft.Xna.Framework.Graphics.SpriteFont _transportHudFont;
    private Rectangle TransportHud(string text,Vector2 center,Color color)
    {
        _transportHudFont??=Content.Load<Microsoft.Xna.Framework.Graphics.SpriteFont>("ToyCounterFont");
        float scale=36f/_transportHudFont.LineSpacing/(CanvasBounds().Height/1080f);
        var size=_transportHudFont.MeasureString(text)*scale;var at=center-size/2;
        // Keep the white outline two physical pixels wide at every window size.
        float outline=2f/(CanvasBounds().Height/1080f);
        foreach(var offset in new[]{new Vector2(-1,-1),new Vector2(0,-1),new Vector2(1,-1),new Vector2(-1,0),new Vector2(1,0),new Vector2(-1,1),new Vector2(0,1),new Vector2(1,1)})
            _spriteBatch.DrawString(_transportHudFont,text,at+offset*outline,Color.White,0,Vector2.Zero,scale,Microsoft.Xna.Framework.Graphics.SpriteEffects.None,0);
        _spriteBatch.DrawString(_transportHudFont,text,at,color,0,Vector2.Zero,scale,Microsoft.Xna.Framework.Graphics.SpriteEffects.None,0);
        return new Rectangle((int)at.X,(int)at.Y,(int)size.X,(int)size.Y);
    }
    private bool TransportOverviewAllowed=>NodeTransportAllowed && _setup.TutorialStep!=2;
    private bool NodeTransportAllowed=>_setup.TutorialStep!=1;
    private bool _transportOverview;
    private bool _transportRoutesBefore;
    private Rectangle TransportOverviewButton=>new(780,1006,210,64);
    private readonly List<(Rectangle Bounds,int Source,int Target,Vector2 Start,Vector2 End)> _transportRouteHits=new();

    private static bool TransportArrowHit(Point p,Vector2 start,Vector2 end)
    {
        var delta=end-start;float size=delta.Length();if(size<1)return false;
        var direction=delta/size;var relative=new Vector2(p.X,p.Y)-start;
        float along=Vector2.Dot(relative,direction),across=Math.Abs(Vector2.Dot(relative,new Vector2(-direction.Y,direction.X)));
        float halfWidth=size<=10?5:size<=24?6:size<=40?7.5f:size<=58?9:10.5f;
        return along>=-3 && along<=size+3 && across<=halfWidth*(size<=10?1:1-Math.Clamp(along/size,0,1))+3;
    }
    private void DrawTransportOverviewButton()
    {
        if(!TransportOverviewAllowed || InspectorVisible)return; var rect=TransportOverviewButton;
        _ui.Button(rect,"ロボット輸送計画",Accent,.5f);
    }
    private void OpenTransportOverview()
    {
        _nodeInspectorClosing=false;_reignView=false;CloseMovement();_populationCell=-1;_nodeTransportOpen=false;_rateTarget=-1;
        _transportRoutesBefore=_net.ShowRoutes;_net.ShowRoutes=true;_transportOverview=true;
        _net.VisibleOperationNodes=null;_transportPage=0;_transportEditor=0;_transportDestinationPicking=false;
    }
    private void CloseTransportOverview()
    {
        _transportOverview=false;_rateTarget=-1;_rateDragging=false;_populationCell=-1;
        _net.ShowRoutes=_transportRoutesBefore;
    }
    private bool TransportOverviewClick(Point p)
    {
        if(TransportOverviewAllowed && !InspectorVisible && TransportOverviewButton.Contains(p)){OpenTransportOverview();return true;}
        if(!_transportOverview && !_nodeTransportOpen)return false;
        bool onMap=MapViewBounds.Contains(p);if(!_transportOverview && !onMap)return false;if(!onMap)p.X-=NodeInspectorOffset;
        if(!onMap && TransportInstructionsClick(p))return true;
        if(onMap)foreach(var hit in _transportRouteHits.AsEnumerable().Reverse())if(hit.Bounds.Contains(p) || Vector2.Distance(hit.Start,hit.End)>1 && TransportArrowHit(p,hit.Start,hit.End))
        {_populationNode=hit.Source;PrepareTransportInstructions();_transportPage=0;_rateTarget=-1;_transportEditor=0;return true;}
        if(onMap && !_transportOverview)return false;
        if(onMap)
        {
            int id=_statusGlobe?HitGlobeNode(p,_setup.Nodes.All.Select(n=>n.Id)):_net.HitNode(_setup,NetPanel,p,_setup.Nodes.All.Select(n=>n.Id));
            if(id>=0){_populationNode=id;PrepareTransportInstructions();_transportPage=0;_rateTarget=-1;return true;}
        }
        return new Rectangle(54,150,850,825).Contains(p);
    }
    private void DrawTransportOverviewPanel()
    {
        _ui.Box(new(54,150,850,825),new Color(27,49,62));
        _ui.Text("ロボット輸送計画",new(84,178),.8f,Cream);

        if(_populationNode<0){_ui.Text("地図から輸送元の節点を選択",new(84,270),.6f,Cream);return;}
        string label=_setup.Nodes.Label(_setup.Nodes.All[_populationNode]);_ui.Text(label,new(84,230),Math.Min(.43f,770/_font.MeasureString(label).X),Cream);
        DrawTransportInstructions();
    }
    private void DrawTransportOverviewMap()
    {
        _transportRouteHits.Clear();
        var nodes=_setup.Nodes.All;
        var routes=new List<(int Source,int Target,Vector2 From,Vector2 To,float WorldLength)>();
        foreach(var node in nodes)
        {
            if(_nodeTransportOpen && !_transportOverview && node.Id!=_populationNode)continue;
            if(!OperationPosition(node.Id,out var from))continue;
            foreach(var r in _setup.Routes.Neighbors(node))
            {
                var target=_setup.Nodes.At(r.Target,r.Entry);
                if(target.Id==node.Id)continue;
                // A destination outside the zoomed viewport still determines the outgoing direction.
                Vector2 to;
                if(!_statusGlobe)to=_net.NodePosition(_setup,target,NetPanel);
                else if(!OperationPosition(target.Id,out to))continue;
                float physical=Vector3.Distance(_setup.Routes.Position(node.Cell,node.Center),_setup.Routes.Position(target.Cell,target.Center));
                if(physical>.00001f)routes.Add((node.Id,target.Id,from,to,physical));
            }
        }
        foreach(var route in routes)
        {
            if(!_net.NodeVisible(_setup,nodes[route.Source]) || !_net.NodeVisible(_setup,nodes[route.Target]))continue;
            var delta=route.To-route.From;float distance=delta.Length();if(distance<1)continue;
            var direction=delta/distance;var normal=new Vector2(-direction.Y,direction.X);
            foreach(bool disposal in new[]{false,true}){
                double mean=_setup.Robots.Transport.AverageParts(route.Source,route.Target,disposal);int band=RobotTransport.FlowBand(mean);
                if(band==0 && route.Source!=_populationNode)continue;
                float size=band==0?10:band==1?24:band==2?40:band==3?58:78;
                var start=route.From+direction*(nodes[route.Source].IsHarbor?60:36)+normal*(disposal?16:-16);var end=start+direction*size;
                bool hovered=TransportArrowHit(_pointer,start,end);float angle=MathF.Atan2(direction.Y,direction.X);
                var color=band==0?new Color(145,150,157):disposal?new Color(226,54,65):new Color(14,17,22);
                if(band==0){var at=(start+end)/2;_ui.Box(new((int)at.X-5,(int)at.Y-5,10,10),Color.White);_ui.Box(new((int)at.X-4,(int)at.Y-4,8,8),color);}
                else for(int i=0;i<(int)size;i++){float width=(9+band*3)*(1-i/size);_ui.Tile(start+direction*i,new Vector2(2,width+4),angle,Color.White);_ui.Tile(start+direction*i,new Vector2(1.5f,width),angle,color);}
                var bounds=new Rectangle((int)Math.Min(start.X,end.X)-12,(int)Math.Min(start.Y,end.Y)-12,(int)Math.Abs(end.X-start.X)+24,(int)Math.Abs(end.Y-start.Y)+24);
                _transportRouteHits.Add((Rectangle.Empty,route.Source,route.Target,start,end));
                if(hovered)_ui.Text((disposal?"廃棄用 ":"納品物 ")+mean.ToString("0.##")+"個 / 全体ターン（５ターン平均）",new(991,900),.38f,Cream);
            }
        }
    }
}
