namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private static readonly Rectangle LandGlobeArea=new(580,170,1280,780),LandConfirm=new(60,840,450,75);
    private readonly HashSet<int> _landSelection=new();
    private int[] _landOrder=Array.Empty<int>();
    private int _landTurn;
    private double _landAge;
    private Point? _landDrag;
    private int[] _automaticLand=Array.Empty<int>();
    private int _automaticLandIndex;
    private int[][] _automaticLandRectangles=Array.Empty<int[]>();
    private float _landStartYaw,_landStartPitch;
    private int LandRuler=>_landOrder[_landTurn];
    private void BeginLandConquest()
    {
        _landOrder=_setup.LandOrder();_landTurn=0;_landAge=0;_landSelection.Clear();_landDrag=null;_automaticLand=Array.Empty<int>();
        _screen=Screen.LandConquest;_world.GlobeZoom=1;_world.GlobeAnchor=null;_world.ShowLandConquest(_setup);
    }
    private int[] AutomaticLand(int ruler)
    {
        var random=new SeedRandom(_setup.PlacementSeed^ruler*7919);
        var available=_setup.Cells.Where(c=>_setup.Owners[c.Id]<0).Select(c=>c.Id).ToList();
        var chosen=new List<int>();
        int first=available[random.Next(available.Count)];chosen.Add(first);available.Remove(first);
        while(chosen.Count<_setup.TerritoryRights[ruler])
        {
            var frontier=chosen.SelectMany(id=>_setup.Cells[id].Neighbors).Where(available.Contains).Distinct().ToArray();
            int next=frontier.Length>0?frontier[random.Next(frontier.Length)]:available[random.Next(available.Count)];
            chosen.Add(next);available.Remove(next);
        }
        return chosen.ToArray();
    }
    private Quaternion _landStartOrientation;
    private void OrderAutomaticLandFaces()
    {
        var faces=_automaticLandRectangles.GroupBy(r=>_setup.Cells[r[0]].Face).ToArray();
        int[] best=null;float bestCost=float.MaxValue;
        (float Yaw,float Pitch) Angles(int index)
        {
            var normal=_setup.Cells[faces[index].First()[0]].Normal;
            float pitch=MathF.Atan2(normal.Y,MathF.Sqrt(normal.X*normal.X+normal.Z*normal.Z));
            var turned=Vector3.Transform(normal,Matrix.CreateRotationX(pitch));
            return (-MathF.Atan2(turned.X,turned.Z),pitch);
        }
        void Search(List<int> order,float yaw,float pitch,float cost)
        {
            if(cost>=bestCost)return;
            if(order.Count==faces.Length){bestCost=cost;best=order.ToArray();return;}
            for(int i=0;i<faces.Length;i++)if(!order.Contains(i))
            {
                var a=Angles(i);order.Add(i);
                Search(order,a.Yaw,a.Pitch,cost+MathF.Abs(MathHelper.WrapAngle(a.Yaw-yaw))+MathF.Abs(a.Pitch-pitch));
                order.RemoveAt(order.Count-1);
            }
        }
        Search(new(),_yaw,_pitch,0);
        _automaticLandRectangles=best.SelectMany(i=>faces[i]).ToArray();
    }
    private void ConfirmLand()
    {
        _setup.ClaimLand(LandRuler,_landSelection.ToArray());_world.ShowLandConquest(_setup);_landSelection.Clear();_landAge=0;_automaticLand=Array.Empty<int>();
        if(++_landTurn==_landOrder.Length){_screen=Screen.Fortune;_world.GlobeAnchor=null;_world.GlobeZoom=1;return;}
    }
    private void UpdateLandSetup(double elapsed,MouseState mouse,bool active,bool click,bool enter)
    {
        if(_screen is not (Screen.LandConquest or Screen.Fortune))return;
        _landAge+=elapsed;
        if(_screen==Screen.Fortune)
        {
            // Slow, continuous rotation from the final land-selection view.
            var orientation=_world.ViewOrientation(_yaw,_pitch);
            var drift=Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(.18f,1,.08f)),(float)elapsed*.14f);
            _world.ManualOrientation=Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(orientation*Matrix.CreateFromQuaternion(drift)));
            if(_landAge>=5){_setup.CompleteLand(_random.Next());_world.ShowSetup(_setup,true);_net.Home(_setup);_statusCell=_populationCell=-1;_screen=Screen.WorldStatus;}
            return;
        }
        if(LandRuler!=_setup.PlayerSlot || _demoActive)
        {
            if(_automaticLand.Length==0)
            {_automaticLand=AutomaticLand(LandRuler);_automaticLandRectangles=LandSelectionPlan.Rectangles(_automaticLand.Select(id=>_setup.Cells[id]));OrderAutomaticLandFaces();_automaticLandIndex=0;_landAge=0;_landStartOrientation=Quaternion.CreateFromRotationMatrix(_world.ViewOrientation(_yaw,_pitch));_landStartYaw=_yaw;_landStartPitch=_pitch;_world.GlobeAnchor=null;_world.GlobeZoom=1;}
            var normal=_setup.Cells[_automaticLandRectangles[_automaticLandIndex][0]].Normal;
            float pitch=MathF.Atan2(normal.Y,MathF.Sqrt(normal.X*normal.X+normal.Z*normal.Z));
            var turned=Vector3.Transform(normal,Matrix.CreateRotationX(pitch));
            float yaw=-MathF.Atan2(turned.X,turned.Z);
            float t=MathHelper.SmoothStep(0,1,(float)Math.Clamp(_landAge/.6,0,1));
            _yawAngle=_landStartYaw+MathHelper.WrapAngle(yaw-_landStartYaw)*t;_pitchAngle=MathHelper.Lerp(_landStartPitch,pitch,t);_world.ManualOrientation=Quaternion.Slerp(_landStartOrientation,Quaternion.CreateFromRotationMatrix(WorldPreview.Orientation(yaw,pitch)),t);
            if(_landAge>=.6)_landSelection.UnionWith(_automaticLandRectangles[_automaticLandIndex]);
            if(_landAge>=.9)
            {
                if(++_automaticLandIndex==_automaticLandRectangles.Length){ConfirmLand();return;}
                _landStartOrientation=Quaternion.CreateFromRotationMatrix(_world.ViewOrientation(_yaw,_pitch));_landStartYaw=_yaw;_landStartPitch=_pitch;_landAge=0;
            }
            return;
        }
        if(!active){_landDrag=null;return;}
        Rotate(mouse,click,active,LandGlobeArea);
        int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
        if(wheel!=0)_world.GlobeZoom=MathHelper.Clamp(_world.GlobeZoom*MathF.Pow(1.2f,wheel/120f),.5f,8);
        if(click && LandGlobeArea.Contains(_pointer))_landDrag=_pointer;
        if(_landDrag.HasValue && mouse.LeftButton==ButtonState.Released)
        {
            var a=_landDrag.Value;var bounds=new Rectangle(Math.Min(a.X,_pointer.X),Math.Min(a.Y,_pointer.Y),Math.Max(2,Math.Abs(a.X-_pointer.X)),Math.Max(2,Math.Abs(a.Y-_pointer.Y)));
            var visible=_setup.Cells.Where(c=>_setup.Owners[c.Id]<0 && _world.ProjectVisible(c.Center,c.Normal,_yaw,_pitch,LandGlobeArea,out var at) && bounds.Contains(at)).Select(c=>c.Id).ToArray();
            if(Math.Abs(a.X-_pointer.X)<6 && Math.Abs(a.Y-_pointer.Y)<6)
            {
                visible=_setup.Cells.Where(c=>_setup.Owners[c.Id]<0 && LandContains(c,_pointer)).Take(1).Select(c=>c.Id).ToArray();
            }
            foreach(int id in visible)if(!_landSelection.Add(id))_landSelection.Remove(id);_landDrag=null;
        }
        if((enter || click && LandConfirm.Contains(_pointer)) && _landSelection.Count==_setup.TerritoryRights[LandRuler])ConfirmLand();
    }
    private bool LandContains(SurfaceCell cell,Point pointer)
    {
        if(!_world.ProjectVisible(cell.Center,cell.Normal,_yaw,_pitch,LandGlobeArea,out _))return false;
        var points=cell.Corners.Select(v=>{_world.ProjectVisible(v,cell.Normal,_yaw,_pitch,LandGlobeArea,out var p);return p;}).ToArray();
        bool inside=false;
        for(int i=0,j=points.Length-1;i<points.Length;j=i++)
            if((points[i].Y>pointer.Y)!=(points[j].Y>pointer.Y) && pointer.X<(points[j].X-points[i].X)*(pointer.Y-points[i].Y)/(points[j].Y-points[i].Y)+points[i].X)inside=!inside;
        return inside;
    }    private void DrawLandSetupUi()
    {
        _ui.Text(_screen==Screen.Fortune?"運否天賦フェーズ":"土地征服フェーズ",new(60,70),1.1f,Cream);
        if(_screen==Screen.Fortune){_ui.Text("空から届く運命のロボット",new(60,230),.75f,Color.White);_ui.Text("配備・工場・廃炉・輸送計画を抽選中",new(60,300),.45f,Cream);return;}
        _ui.Text(_setup.ConquerorNames[LandRuler]+" の土地選択",new(60,220),.7f,Color.White);
        _ui.Text("選択 "+_landSelection.Count+" / 権利 "+_setup.TerritoryRights[LandRuler]+" セル",new(60,310),.65f,_landSelection.Count>_setup.TerritoryRights[LandRuler]?Color.Orange:Cream);
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[LandRuler],new Rectangle(60,370,450,300),true,forCreation:true);
        _ui.Text("左ドラッグ：範囲を反転\n右ドラッグ：星を回す\nホイール：拡大・縮小\n飛び地も選択できます",new(60,685),.38f,Color.White);
        _ui.Text("地形と交通路だけで土地を選ぼう",new(60,805),.32f,Cream);
        _ui.Button(LandConfirm,LandRuler==_setup.PlayerSlot?"この土地を獲得":"他国が土地を選択中",LandRuler==_setup.PlayerSlot && _landSelection.Count==_setup.TerritoryRights[LandRuler]?Accent:Muted,.65f);
    }
    private void DrawLandSetupGlobe(Rectangle canvas,Matrix transform,Viewport viewport)
    {
        GraphicsDevice.Viewport=OrientationViewport(LandGlobeArea,canvas);_world.Draw(_yaw,_pitch,_animationTime,false,true);GraphicsDevice.Viewport=viewport;
        _spriteBatch.Begin(transformMatrix:transform);
        foreach(var c in _setup.Cells)
        {
            if(!_world.ProjectVisible(c.Center,c.Normal,_yaw,_pitch,LandGlobeArea,out var p))continue;
            if(_setup.Owners[c.Id]>=0 || _landSelection.Contains(c.Id) || _screen==Screen.LandConquest)
            {
                var color=_landSelection.Contains(c.Id)?Color.Yellow:_setup.Owners[c.Id]>=0?WorldSetup.Colors[_setup.Owners[c.Id]]:new Color(110,140,150);
                foreach(var edge in c.Corners.Select((v,i)=>(A:v,B:c.Corners[(i+1)%c.Corners.Length])))
                    if(_world.ProjectVisible(edge.A,c.Normal,_yaw,_pitch,LandGlobeArea,out var a) && _world.ProjectVisible(edge.B,c.Normal,_yaw,_pitch,LandGlobeArea,out var b))LandLine(a,b,color,3);
            }
            foreach(var route in _setup.Routes.DisplayPaths(c.Id))
            {
                var curve=_setup.Routes.Curve(c.Id,route);
                for(int i=1;i<curve.Length;i++)
                    if(_world.ProjectVisible(curve[i-1],c.Normal,_yaw,_pitch,LandGlobeArea,out var a) && _world.ProjectVisible(curve[i],c.Normal,_yaw,_pitch,LandGlobeArea,out var b))LandLine(a,b,new Color(230,200,145),1);
            }
            foreach(var node in _setup.Nodes.InCell(c.Id).Where(n=>n.IsHarbor))
                if(_world.ProjectVisible(_setup.Routes.Position(c.Id,node.Center),c.Normal,_yaw,_pitch,LandGlobeArea,out var h))_ui.Box(new((int)h.X-3,(int)h.Y-3,6,6),Cream);        }
        if(_landDrag.HasValue){var a=_landDrag.Value;var b=_pointer;LandLine(new(a.X,a.Y),new(b.X,a.Y),Color.White,2);LandLine(new(b.X,a.Y),new(b.X,b.Y),Color.White,2);LandLine(new(b.X,b.Y),new(a.X,b.Y),Color.White,2);LandLine(new(a.X,b.Y),new(a.X,a.Y),Color.White,2);}
        if(_screen==Screen.Fortune)
            for(int i=0;i<90;i++){float t=(float)((_landAge+i*.071)%1.8/1.8);float x=MathHelper.Lerp(650+(i*173%1100),1080+i*47%310,t);float y=MathHelper.Lerp(-100,460+i*31%200,t);DrawRobotGlyph(new Rectangle((int)x,(int)y,26,46),i%20,(RobotParts)(1+i%7),false,1-t*.3f);}
        _spriteBatch.End();
    }
    private void LandLine(Vector2 a,Vector2 b,Color color,float width){var d=b-a;_spriteBatch.Draw(_pixel,a,null,color,MathF.Atan2(d.Y,d.X),Vector2.Zero,new Vector2(d.Length(),width),SpriteEffects.None,0);}
}

