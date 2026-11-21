namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private GuideBriefing _guideBriefing;
    private WorldSetup _guideWorld;
    private int _guidePage,_guideRuler;
    private Texture2D _guidePortrait,_guideNameplate,_guideScenery;
    private int _guideSceneryKind=-1;
    private BalloonRenderer _speechBalloon;
    private string _guidePortraitKey;
    private static readonly string[] GuideBodies={"elevator","stewardess","waitress","concierge"};
    private static readonly Rectangle GuideNext=new(1320,320,240,60);

    private bool GuideInput(bool active,bool click,bool enter,bool escape)
    {
        if(!IsStatusScreen || !DomesticOperationsAllowed || _autoPlay || _demoActive || _setup.ComputerOnly || _setup.TutorialStep>0){_guideBriefing=null;return false;}
        if(_setup.ControllerOf(_setup.PlayerSlot)<0 || !_setup.Relations.CanAct(_setup.PlayerSlot) || _conquestOverview ||
            _diplomacyPending || _setup.Industry.Pending || _subordinateNodes!=null || _computerDomesticNodes!=null)return false;
        if(!ReferenceEquals(_guideWorld,_setup)){_guideWorld=_setup;_guideBriefing=null;}
        var roster=_setup.EnsureGuides();
        if(_guideBriefing==null){
            if(roster.LastTurns[_setup.PlayerSlot]==_setup.Population.Turn)return false;
            _guideRuler=_setup.PlayerSlot;_guidePage=0;
            _guideBriefing=GuideNarration.Build(_setup,_guideRuler,!roster.Introduced[_guideRuler]);
            return true; // Do not pass the click that closed the previous phase into the guide.
        }
        if(!active)return true;
        if(escape){FinishGuide();return true;}
        bool last=_guidePage==_guideBriefing.Pages.Length-1;
        if(enter || click && GuideNext.Contains(_pointer)){
            if(last){
                int node=_guideBriefing.OfficeNode;FinishGuide();
                if(node>=0 && node<_setup.Nodes.All.Length && _setup.Nodes.All[node].Owner==_guideRuler){OpenNodeInspector(node);OpenNodeOffice();}
                else {ResetInspector();RefreshStratagems();_stratagemManual=false;_stratagemSelected=-1;}
            }else _guidePage++;
        }
        return true;
    }
    private void FinishGuide()
    {
        var roster=_setup.EnsureGuides();roster.Introduced[_guideRuler]=true;roster.LastTurns[_guideRuler]=_setup.Population.Turn;
        _guideBriefing=null;
    }
    private void DrawGuide()
    {
        if(_guideBriefing==null || !IsStatusScreen || !ReferenceEquals(_guideWorld,_setup))return;
        var roster=_setup.EnsureGuides();
        EnsureGuidePortrait(roster.Bodies[_guideRuler],_guideRuler);
        _spriteBatch.Draw(_guidePortrait,PortraitHudLayout.Character,Color.White);
        _guideNameplate??=LoadGuideImage("character-nameplate-v001.png");
        _spriteBatch.Draw(_guideNameplate,PortraitHudLayout.Nameplate,Color.White);
        var ink=new Color(57,52,48);
        PortraitHudLayout.CenterLines(_ui,_font,new[]{GuideMessages.Text("role")},PortraitHudLayout.RoleText,.34f,ink);
        string name=roster.Names[_guideRuler];float nameScale=Math.Min(.46f,345/Math.Max(1,_font.MeasureString(name).X));
        string[] names=_font.MeasureString(name).X*nameScale<=178?new[]{name}:new[]{name[..((name.Length+1)/2)],name[((name.Length+1)/2)..]};
        PortraitHudLayout.CenterLines(_ui,_font,names,PortraitHudLayout.NameText,nameScale,ink);
        DrawSpeechBalloon(new(500,40,1380,420));        var lines=_guideBriefing.Pages[_guidePage];
        for(int i=0;i<lines.Length;i++)NewsLine(lines[i],830,115+i*43,850,.48f,ink);
        bool last=_guidePage==_guideBriefing.Pages.Length-1;
        _ui.StateButton(GuideNext,GuideMessages.Text(last?"finish":"next"),Accent,Muted,false,GuideNext.Contains(_pointer),attention:true,seconds:_animationTime,scale:.46f);
        float light=.35f+.65f*(1+MathF.Sin(_animationTime*3))/2;
        _ui.Box(new(1331,341,18,18),new Color(20,55,38));
        _ui.Box(new(1334,344,12,12),Color.Lerp(new Color(35,105,63),new Color(170,255,186),light));
    }
    private bool GuideVisible=>_guideBriefing!=null && ReferenceEquals(_guideWorld,_setup);
    private void DrawGuideScene()
    {
        // An HUD scene: scenery is limited to the left third, the right remains a live globe.
        DrawSpaceBackdrop(new(0,0,1920,1080));
        var node=_setup.Nodes.Current(_guideRuler)??_setup.Nodes.All.FirstOrDefault(n=>n.Cell==_setup.ConquerorLocations[_guideRuler]);
        {
            int cell=_setup.ConquerorLocations[_guideRuler];
            int kind=node!=null?NodeSupervisors.For(node,_setup.Cells[node.Cell].Face):_setup.Cells[cell].Face is 2 or 3?8:_setup.Routes.Terrain(cell,_setup.ConquerorPoints[_guideRuler])==TravelTerrain.Sea?5:3;
            if(kind!=_guideSceneryKind){_guideScenery?.Dispose();_guideScenery=LoadGuideImage("background-"+NodeSupervisors.Kinds[kind]+"-v001.png");_guideSceneryKind=kind;}
            PortraitHudLayout.DrawBackground(_spriteBatch,_guideScenery);
        }
        var canvas=CanvasBounds();var viewport=GraphicsDevice.Viewport;
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.End();GraphicsDevice.Viewport=OrientationViewport(new(960,170,920,850),canvas);
        _world.SelectionSetup=_setup;_world.DaylightWorld=_setup;
        _world.DawnMeridian=WorldDayCycle.DawnPhase(_setup,_guideRuler);
        _world.Draw(_yaw,_pitch,_animationTime,false,true);
        GraphicsDevice.Viewport=viewport;_spriteBatch.Begin(transformMatrix:transform);
        DrawGuide();
    }
    private void DrawSpeechBalloon(Rectangle bounds)
    {
        _speechBalloon??=new BalloonRenderer(GraphicsDevice);_speechBalloon.Draw(_spriteBatch,bounds);
    }
    private Texture2D LoadGuideImage(string name)
    {
        using var file=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Guides",name));
        var image=Texture2D.FromStream(GraphicsDevice,file);
        var pixels=new Color[image.Width*image.Height];image.GetData(pixels);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color(c.R*c.A/255,c.G*c.A/255,c.B*c.A/255,(int)c.A);}
        image.SetData(pixels);return image;
    }
    private void EnsureGuidePortrait(int body,int ruler)
    {
        var primary=_setup.OwnerColor(ruler);var secondary=WorldSetup.Colors[_setup.CountrySecondaryColor(ruler)];
        int pattern=_setup.CountryPattern(ruler);string key=$"{body}/{primary.PackedValue}/{secondary.PackedValue}/{pattern}";
        if(_guidePortraitKey==key)return;
        _guidePortrait?.Dispose();_guidePortrait=LoadGuideImage("guide-"+GuideBodies[body]+"-v001.png");_guidePortraitKey=key;
        var pixels=new Color[_guidePortrait.Width*_guidePortrait.Height];_guidePortrait.GetData(pixels);
        for(int y=0;y<_guidePortrait.Height;y++)for(int x=0;x<_guidePortrait.Width;x++){
            int i=y*_guidePortrait.Width+x;var c=pixels[i];if(c.A==0)continue;
            float nx=x/(float)_guidePortrait.Width,ny=y/(float)_guidePortrait.Height;
            // Recolor neutral fabric without rectangular cutouts across the jacket.
            // Warm skin and colored hair are excluded by chroma; shoes stay original.
            if(!(ny<.09f || ny>.18f && ny<.74f))continue;
            float alpha=c.A/255f,r=c.R/alpha,g=c.G/alpha,b=c.B/alpha;
            if(Math.Min(r,Math.Min(g,b))<90 || Math.Max(r,Math.Max(g,b))-Math.Min(r,Math.Min(g,b))>30)continue;
            var color=WorldSetup.CountryPatternSecondary(pattern,x*20/_guidePortrait.Width,y*30/_guidePortrait.Height)?secondary:primary;
            float light=(r+g+b)/(3*255f);
            pixels[i]=new Color((int)(color.R*light*alpha),(int)(color.G*light*alpha),(int)(color.B*light*alpha),(int)c.A);
        }
        _guidePortrait.SetData(pixels);
    }
    private void DisposeGuide(){_guidePortrait?.Dispose();_guideNameplate?.Dispose();_speechBalloon?.Dispose();_guideScenery?.Dispose();}
}
