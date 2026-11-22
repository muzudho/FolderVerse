namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Texture2D _conquestScenery;
    private string _conquestSceneryKey;
    private static Rectangle ConquestRelationsButton=>new(1630,28,250,48);
    private void DrawConquestBroadcastBackground()
    {
        var show=_conquestBroadcast;var node=show.Location>=0?_setup.Nodes.All[show.Location]:null;
        if(ConquestStudioCut){
            _ui.Box(PortraitHudLayout.MainPanel,new Color(74,104,117));
            for(int x=665;x<1920;x+=180)_ui.Box(new(x,85,5,900),new Color(96,130,139));
            _ui.Box(new(640,860,1280,220),new Color(151,117,79));
            _ui.Box(new(1230,340,650,510),new Color(151,103,53));
            _ui.Box(new(1240,350,630,490),new Color(31,56,62));
            _ui.Center("スタジオ別室 / 得点計算",new(1100,230,730,56),.57f,Cream);
            _ui.Box(new(870,850,150,70),new Color(202,165,99));
        }else if(node!=null){
            int kind=NodeSupervisors.For(node,_setup.Cells[node.Cell].Face);
            string key=NodeSupervisors.Kinds[kind];
            // Existing harbour and polar scenery is outdoors; the rest is indoor scenery.
            if(!show.Outdoor && kind==8)key="construction";
            if(!show.Outdoor && kind==4)key="captain";
            if(show.Outdoor && (node.Terrain==TravelTerrain.Sea || node.IsHarbor))key=kind==8?"polar":"harbor";
            if(key!=_conquestSceneryKey){_conquestScenery?.Dispose();_conquestScenery=LoadGuideImage("background-"+key+"-v001.png");_conquestSceneryKey=key;}
            if(show.Outdoor && key!="harbor" && key!="polar")DrawConquestOutdoor(node);
            else PortraitHudLayout.DrawMainBackground(_spriteBatch,_conquestScenery);
        }else DrawConquestOutdoor(null);
        // Dedicated indoor morning studio, separate from the remote location.
        _ui.Box(PortraitHudLayout.Background,new Color(77,126,137));
        for(int x=20;x<640;x+=100)_ui.Box(new(x,0,4,870),new Color(124,169,171));
        _ui.Box(new(0,860,640,220),new Color(150,111,78));
        _ui.Box(new(32,120,540,650),new Color(231,211,160));
        _ui.Box(new(44,132,516,626),new Color(117,162,163));
        _ui.Center("おはよう！ 世界のようす",new(55,870,530,50),.48f,Cream);
        _ui.Box(new(55,945,185,44),new Color(145,57,62));
        _ui.Center("放送中",new(55,945,185,44),.43f,Cream);
        PortraitHudLayout.DrawSplitter(_ui);
        if(!ConquestStudioCut){
            string title=node==null?"現地中継":_setup.Nodes.DisplayName(node)+(show.Outdoor?" / 屋外中継":" / 室内中継");
            _ui.Box(new(720,230,1140,38),new Color(23,42,48)*.85f);
            _ui.Center(title,new(735,232,1110,34),Math.Min(.43f,1090/Math.Max(1,_font.MeasureString(title).X)),Cream);
        }
    }
    private void DrawConquestOutdoor(Node node)
    {
            // A small toy outdoor set; no characters are baked into the scenery.
            bool sea=node!=null && node.Terrain==TravelTerrain.Sea;
            _ui.Box(PortraitHudLayout.MainPanel,new Color(160,204,216));
            _ui.Box(new(640,570,1280,510),sea?new Color(68,135,157):new Color(114,153,117));
            for(int i=0;i<8;i++){
                int x=660+i*175;
                _ui.Tile(new(x+70,590),new(250,180),-.65f,new Color(103+i%2*20,130+i%2*15,137));
                _ui.Box(new(x,730,115,170),new Color(230,209,165));
                _ui.Tile(new(x+58,715),new(115,60),0,new Color(156,97,69));
                _ui.Box(new(x+20,770,24,40),new Color(69,107,119));
            }
            if(sea)for(int y=920;y<1080;y+=45)_ui.Box(new(650,y,1250,3),new Color(155,208,216));
    }
    private void DrawConquestLocationBoard()
    {
        var r=ConquestBoard;bool electric=!_conquestBroadcast.Outdoor;
        var rim=electric?new Color(68,84,91):new Color(153,105,57);
        _ui.Box(new(r.X-8,r.Y+12,r.Width+32,r.Height+32),Color.Black*.22f);
        _ui.Box(new(r.X-16,r.Y-16,r.Width+32,r.Height+32),rim);
        _ui.Box(r,electric?new Color(21,42,49):new Color(224,203,157));
        if(!electric)for(int y=r.Y+9;y<r.Bottom;y+=30)_ui.Box(new(r.X,y,r.Width,1),new Color(174,143,100)*.25f);
        foreach(int x in new[]{r.X+25,r.Right-25})foreach(int y in new[]{r.Y-9,r.Bottom+9})_ui.Box(new(x,y,8,8),new Color(229,217,180));
        _ui.Box(new(800,980,1020,34),new Color(35,54,59));
    }
    private void DrawConquestBroadcastHost()
    {
        // The independent broadcaster uses the original mob outfit, never a country's flag.
        if(_setupHost!=null)_spriteBatch.Draw(_setupHost,PortraitHudLayout.Character,Color.White);
        _guideNameplate??=LoadGuideImage("character-nameplate-v001.png");
        _spriteBatch.Draw(_guideNameplate,PortraitHudLayout.Nameplate,Color.White);
        var ink=new Color(57,52,48);
        PortraitHudLayout.CenterLines(_ui,_font,new[]{"局アナ"},PortraitHudLayout.RoleText,.34f,ink);
        string name=_conquestBroadcast.Announcer;
        string[] names=_font.MeasureString(name).X*.40f<=178?new[]{name}:new[]{name[..((name.Length+1)/2)],name[((name.Length+1)/2)..]};
        PortraitHudLayout.CenterLines(_ui,_font,names,PortraitHudLayout.NameText,.40f,ink);
        string[] lines=_conquestStage<4?new[]{"みなさん、おはようございます！","それでは今日の世界征服状況を","見てみましょう！"}:_conquestStage>4?new[]{"今日も色んな動きがありましたね！","それでは、朝刊の時間です！"}:ConquestStudioCut?new[]{$"{_setup.ConquerorNames[ConquestCard.Ruler]}さんの","支配地域と得点を見てみましょう！"}:_conquestBroadcast.Lines(ConquestCard.Ruler);
        if(lines.Length>3){
            int page=(int)(Math.Max(0,_conquestAge-(ConquestDetailedCard?ConquestStudioEnd:0))/3)%2;
            lines=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(System.Linq.Enumerable.Skip(lines,page*3),3));
        }
        var speech=new Rectangle(500,20,1000,205);DrawSpeechBalloon(speech);
        var area=BalloonRenderer.TextArea(speech);
        float scale=Math.Min(.55f,Math.Min(area.Width/Math.Max(1,System.Linq.Enumerable.Max(lines,l=>_font.MeasureString(l).X)),area.Height/(float)(lines.Length*_font.LineSpacing)));
        PortraitHudLayout.CenterLines(_ui,_font,lines,area,scale,ink);
    }
}