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
            PortraitHudLayout.DrawMainBackground(_spriteBatch,BroadcastArt("annex"));
            DrawBroadcastBoard(4,ConquestFormulaSpace);
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
        PortraitHudLayout.DrawBackground(_spriteBatch,BroadcastArt("studio"));
        PortraitHudLayout.DrawSplitter(_ui);
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
        DrawBroadcastBoard(BroadcastBoardKind,ConquestBoard);
        _ui.Box(new(920,752,720,36),new Color(35,54,59));
    }
    private void DrawConquestLocationCaption()
    {
        var node=_conquestBroadcast.Location>=0?_setup.Nodes.All[_conquestBroadcast.Location]:null;
        string title=node==null?"現地中継":_setup.Nodes.DisplayName(node)+(_conquestBroadcast.Outdoor?" / 屋外中継":" / 室内中継");
        _ui.Box(new(700,955,1150,40),new Color(23,42,48)*.85f);
        _ui.Center(title,new(715,957,1120,36),Math.Min(.43f,1100/Math.Max(1,_font.MeasureString(title).X)),Cream);
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