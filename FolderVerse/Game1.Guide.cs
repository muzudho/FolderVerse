namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private GuideBriefing _guideBriefing;
    private WorldSetup _guideWorld;
    private int _guidePage,_guideRuler;
    private Texture2D _guidePortrait,_guideNameplate;
    private string _guidePortraitKey;
    private static readonly string[] GuideBodies={"elevator","stewardess","waitress","concierge"};
    private static readonly Rectangle GuidePrevious=new(700,850,180,64),GuideNext=new(900,850,240,64),
        GuideAction=new(1160,850,600,64),GuideSkip=new(1480,976,330,52);

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
        if(escape || click && GuideSkip.Contains(_pointer)){FinishGuide();return true;}
        if(click && GuidePrevious.Contains(_pointer)){_guidePage=Math.Max(0,_guidePage-1);return true;}
        bool last=_guidePage==_guideBriefing.Pages.Length-1;
        if(last && click && GuideAction.Contains(_pointer)){
            int node=_guideBriefing.OfficeNode;FinishGuide();
            if(node>=0 && node<_setup.Nodes.All.Length && _setup.Nodes.All[node].Owner==_guideRuler){OpenNodeInspector(node);OpenNodeOffice();}
            else {ResetInspector();RefreshStratagems();_stratagemManual=false;_stratagemSelected=-1;}
            return true;
        }
        if(enter || click && GuideNext.Contains(_pointer)){
            if(last)FinishGuide();else _guidePage++;
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
        _ui.Box(new(0,0,1920,1080),new Color(3,12,22)*.76f);
        _spriteBatch.Draw(_guidePortrait,new Rectangle(70,88,600,900),Color.White);
        _guideNameplate??=LoadGuideImage("character-nameplate-v001.png");
        _spriteBatch.Draw(_guideNameplate,new Rectangle(680,72,1120,373),Color.White);
        var ink=new Color(57,52,48);
        _ui.Center(GuideMessages.Text("role"),new(742,183,230,138),.65f,ink);
        _ui.Center(roster.Names[_guideRuler],new(1010,183,715,138),Math.Min(.9f,690/Math.Max(1,_font.MeasureString(roster.Names[_guideRuler]).X)),ink);
        ToyPaperBox(new(700,430,1100,385),new Color(250,236,202));
        var lines=_guideBriefing.Pages[_guidePage];
        for(int i=0;i<lines.Length;i++)NewsLine(lines[i],740,470+i*65,1020,.57f,ink);
        _ui.Center(GuideMessages.Page("progress",new Dictionary<string,string>{["page"]=(_guidePage+1).ToString(),["pages"]=_guideBriefing.Pages.Length.ToString()})[0],new(740,757,1020,30),.4f,ink);
        _ui.Button(GuidePrevious,GuideMessages.Text("previous"),Muted,.5f);
        bool last=_guidePage==_guideBriefing.Pages.Length-1;
        _ui.Button(GuideNext,GuideMessages.Text(last?"finish":"next"),Accent,.46f);
        if(last)_ui.Button(GuideAction,GuideMessages.Text(_guideBriefing.OfficeNode>=0?"officeButton":"cardsButton"),Accent,.52f);
        _ui.Button(GuideSkip,GuideMessages.Text("skip"),Muted,.4f);
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
    private void DisposeGuide(){_guidePortrait?.Dispose();_guideNameplate?.Dispose();}
}
