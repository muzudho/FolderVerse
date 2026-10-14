namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private Microsoft.Xna.Framework.Graphics.SpriteFont _transportHudFont;
    private static float TransportTriangleSize(int degrees)=>15f+105f*Math.Clamp(degrees/360f,0,1);
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
    private bool _transportOverview;
    private bool _transportRoutesBefore;
    private static readonly Rectangle TransportOverviewButton=new(204,930,210,52);
    private static Rectangle TransportPartButton(int mask)=>new(84+(mask-1)%4*196,322+(mask-1)/4*136,182,96);
    private void DrawTransportPartButtons()
    {
        for(int mask=1;mask<=7;mask++)
        {
            var parts=(RobotParts)mask;var rect=TransportPartButton(mask);
            _ui.Box(rect,_nodeTransportPart==parts?Accent:rect.Contains(_pointer)?new Color(75,116,130):Muted);
            DrawRobotGlyph(new(rect.Center.X-27,rect.Y+6,54,84),_setup.PlayerSlot,parts);
            if(_nodeTransportPart==parts)_ui.Box(new(rect.X,rect.Y,rect.Width,3),Cream);
            string amount=_populationNode<0?"輸出 --%":$"輸出 {_setup.Robots.Transport.Plan(_populationNode,_setup.PlayerSlot,parts,_nodeTransportDisposal).Sum(w=>w.Degrees)*100/360f:0.#}%";
            _ui.Center(amount,new(rect.X,rect.Bottom+3,rect.Width,30),.48f,Cream);
        }
    }
    private readonly List<(Rectangle Bounds,int Source,int Target,Vector2 Start,Vector2 End)> _transportRouteHits=new();

    private static bool TransportArrowHit(Point p,Vector2 start,Vector2 end)
    {
        var delta=end-start;float size=delta.Length();if(size<1)return false;
        var direction=delta/size;var relative=new Vector2(p.X,p.Y)-start;
        float along=Vector2.Dot(relative,direction),across=Math.Abs(Vector2.Dot(relative,new Vector2(-direction.Y,direction.X)));
        return along>=-3 && along<=size+3 && across<=(size<=15?size/2:size*(1-Math.Clamp(along/size,0,1))/2)+3;
    }
    private void DrawTransportOverviewButton()
    {
        if(!_reignView)return; var rect=TransportOverviewButton;
        if(_transportOverview)
        {
            _ui.Box(rect,new Color(46,54,62));
            _ui.Center("ロボット輸送",new(rect.X+27,rect.Y,rect.Width-27,rect.Height),.45f,new Color(155,164,172));
        }
        else _ui.Button(rect,"ロボット輸送",Accent,.45f);
        var lamp=new Rectangle(rect.X+10,rect.Center.Y-7,14,14);
        _ui.Box(lamp,new Color(17,28,34));
        _ui.Box(new(lamp.X+2,lamp.Y+2,10,10),_transportOverview?new Color(255,234,124):new Color(67,83,92));
    }
    private void OpenTransportOverview()
    {
        CloseMovement();_populationCell=-1;_nodeTransportOpen=false;_rateTarget=-1;
        _transportRoutesBefore=_net.ShowRoutes;_net.ShowRoutes=true;_transportOverview=true;
        _net.VisibleOperationNodes=null;
    }
    private void CloseTransportOverview()
    {
        _transportOverview=false;_rateTarget=-1;_rateDragging=false;_populationCell=-1;
        _net.ShowRoutes=_transportRoutesBefore;
    }
    private bool TransportOverviewClick(Point p)
    {
        if(_reignView && TransportOverviewButton.Contains(p)){if(!_transportOverview)OpenTransportOverview();return true;}
        if(!_transportOverview)return false;
        if(_rateTarget>=0){if(RateDialogArea.Contains(p))RatePieClick(p);else _rateTarget=-1;return true;}
        if(new Rectangle(803,169,58,44).Contains(p)){CloseTransportOverview();return true;}
        if(new Rectangle(84,270,374,46).Contains(p)){_nodeTransportDisposal=false;return true;}
        if(new Rectangle(480,270,374,46).Contains(p)){_nodeTransportDisposal=true;return true;}
        for(int mask=1;mask<=7;mask++)if(TransportPartButton(mask).Contains(p)){_nodeTransportPart=(RobotParts)mask;return true;}
        foreach(var hit in _transportRouteHits.AsEnumerable().Reverse())if(hit.Bounds.Contains(p) || Vector2.Distance(hit.Start,hit.End)>1 && TransportArrowHit(p,hit.Start,hit.End))
        {_populationNode=hit.Source;_rateTarget=hit.Target;return true;}
        if(NetPanel.Contains(p))
        {
            int id=_statusGlobe?HitGlobeNode(p,_setup.Nodes.All.Select(n=>n.Id)):_net.HitNode(_setup,NetPanel,p,_setup.Nodes.All.Select(n=>n.Id));
            if(id>=0){_populationNode=id;return true;}
        }
        return new Rectangle(54,150,850,825).Contains(p);
    }
    private void DrawTransportOverviewPanel()
    {
        _ui.Box(new(54,150,850,825),new Color(27,49,62));
        _ui.Text("ロボット輸送計画",new(84,178),.8f,Cream);
        _ui.Button(new(803,169,58,44),"×",Muted,.7f);
        _ui.Button(new(84,270,374,46),"ふつうの移送",_nodeTransportDisposal?Muted:Accent,.5f);
        _ui.Button(new(480,270,374,46),"廃棄としての移送",_nodeTransportDisposal?Accent:Muted,.5f);
        DrawTransportPartButtons();
        _ui.Text("地図上の移送先と割合をクリック",new(84,630),.65f,Cream);
        _ui.Text("ふつう＋廃棄 / 残りは留まる",new(84,680),.5f,Cream);
        if(_populationNode>=0)_ui.Text(_setup.Nodes.Label(_setup.Nodes.All[_populationNode]),new(84,740),.43f,Cream);
    }
    private void DrawTransportOverviewMap()
    {
        _transportRouteHits.Clear();
        var nodes=_setup.Nodes.All;
        var routes=new List<(int Source,int Target,Vector2 From,Vector2 To,float WorldLength)>();
        foreach(var node in nodes)
        {
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
            int degrees=_setup.Robots.Transport.Plan(route.Source,_setup.PlayerSlot,_nodeTransportPart,_nodeTransportDisposal).FirstOrDefault(w=>w.Target==route.Target)?.Degrees??0;
            var delta=route.To-route.From;float distance=delta.Length();if(distance<1)continue;
            var direction=delta/distance;var normal=new Vector2(-direction.Y,direction.X);
            float size=TransportTriangleSize(degrees);
            // Leave a full node-marker diameter between the marker and triangle base.
            float nodeRadius=nodes[route.Source].IsHarbor?20:12;
            var start=route.From+direction*(nodeRadius*3);
            var end=start+direction*size;
            var source=nodes[route.Source];
            var position=_setup.Routes.Position(source.Cell,source.Center);
            float height=WorldTerrain.Elevation(position,WorldTerrain.Offset(_setup.WorldSeed));
            var color=degrees<18?new Color(150,155,164):height<.52f?new Color(111,228,255):height>=WorldTerrain.MountainHeight?new Color(255,163,70):new Color(255,230,140);
            bool hovered=_rateTarget<0 && TransportArrowHit(_pointer,start,end);
            float angle=MathF.Atan2(direction.Y,direction.X);
            if(hovered)color=Color.Lerp(color,Color.White,.35f);
            if(degrees==0)
            {
                if(hovered)_ui.Tile((start+end)/2,new Vector2(size+6),angle,Color.White);
                _ui.Tile((start+end)/2,new Vector2(size),angle,color);
            }
            else
            {
                // Match the hover outline to the full triangle rather than a thin center line.
                if(hovered)for(int i=-3;i<(int)size+3;i++)
                    _ui.Tile(start+direction*i,new Vector2(1.5f,size*(1-Math.Clamp(i/size,0,1))+6),angle,Color.White);
                for(int i=0;i<(int)size;i++)
                    _ui.Tile(start+direction*i,new Vector2(1.5f,size*(1-i/size)),angle,color);
            }
            var label=end+normal*15;
            var rect=degrees>0?TransportHud($"{degrees*100/360f:0.#}%",label,degrees<18?new Color(180,184,194):color):Rectangle.Empty;
            _transportRouteHits.Add((rect,route.Source,route.Target,start,end));
        }
    }
}
