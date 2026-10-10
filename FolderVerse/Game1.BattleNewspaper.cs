namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private NewspaperPortraitRenderer _newspaperPortraits;
    private double _newspaperClock;
    private int _newspaperEdition;

    private static readonly Rectangle NewspaperSwitch=new(1128,750,704,28);
    internal static Rectangle NewspaperColumn(int index,int count)
    {
        int columns=Math.Clamp(count,2,4),width=704/columns;
        return new(1128+index*width,324,width-16,438);
    }
    private void DrawBattleNewspaper(BattleState battle,BattleFrame frame)
    {
        var paper=new Color(238,225,199);var ink=new Color(58,49,42);
        _ui.Box(new(1109,217,752,571),new Color(8,18,25)*.4f);
        _ui.Box(new(1104,210,752,570),paper);
        // Uneven cut edges, fibres, fold and double rules give the sheet a printed-paper silhouette.
        for(int i=0;i<94;i++)
        {
            int x=1104+i*8,edge=(i*17%5);
            _ui.Box(new(x,207+edge,8,4),paper);
            _ui.Box(new(x,778,8,2+i*7%5),paper);
        }
        for(int i=0;i<57;i++)_ui.Box(new(1106,214+i*10,748,1),ink*.035f);
        _ui.Box(new(1478,211,2,566),ink*.08f);_ui.Box(new(1480,211,2,566),Color.White*.12f);
        var profile=NewspaperProfiles.All[_newspaperEdition];
        DrawNewspaperMasthead(_newspaperEdition,new Rectangle(1120,217,720,62));
        _ui.Box(new(1120,281,720,2),ink);_ui.Box(new(1120,286,720,1),ink);
        int patron=_newspaperEdition==0?(battle.Armies.FirstOrDefault(a=>a.Owner==_setup.PlayerSlot)?.Owner??battle.Armies[0].Owner):battle.Defender;
        if(patron<0 || patron>=_setup.Looks.Length)patron=battle.Armies[0].Owner;
        string affiliation=profile.FavorsArmy?$"#{patron+1} 陣営 / {profile.Label}":$"Node {battle.TargetNode+1} / {profile.Label}";
        _ui.Center($"{affiliation} / 第 {_setup.Population.Turn} 号",new(1128,293,704,24),.35f,ink);
        _newspaperPortraits??=new NewspaperPortraitRenderer(Content);
        int index=0;
        bool biased=profile.FavorsArmy;
        foreach(var army in battle.Armies.OrderBy(a=>biased && a.Owner==patron?0:1).Take(4))
        {
            int current=index++;
            var r=biased?(current==0?new Rectangle(1128,324,398,418):new Rectangle(1548,324+(current-1)*130,284,120)):NewspaperColumn(current,battle.Armies.Count);
            if(index>1)_ui.Box(new(r.X-9,r.Y,1,r.Height),ink*.35f);
            string name=_setup.ConquerorNames[army.Owner];
            _ui.Center(name,new(r.X,r.Y,r.Width,32),Math.Min(.48f,r.Width/_font.MeasureString(name).X),ink);
            int photoWidth=Math.Min(biased?192:180,r.Width-8),photoHeight=photoWidth*4/3;
            var photo=new Rectangle(r.Center.X-photoWidth/2,r.Y+41,photoWidth,photoHeight);
            bool textOnly=biased && current>0;
            bool known=!textOnly && _newspaperPortraits.Draw(_spriteBatch,_setup.Looks[army.Owner],photo,profile.PhotoKind,profile.AllowFallback);
            if(!known && !textOnly){_ui.Box(photo,paper*.95f);_ui.Center("写真なし",photo,.4f,ink*.6f);}
            DrawNewspaperToy(army.Owner,textOnly?new Vector2(r.Right-22,r.Bottom-25):new Vector2(photo.Right-8,photo.Bottom-8),textOnly?48:82);
            var alive=frame.Units.Where(u=>u.Owner==army.Owner && !u.Fallen && !u.Retreated).ToArray();
            int y=textOnly?r.Y+38:photo.Bottom+12;
            int spacing=biased?22:27;
            void Line(string text){_ui.Text(text,new(r.X,y),Math.Min(.4f,r.Width/_font.MeasureString(text).X),ink);y+=spacing;}
            Line($"征服者 #{army.Owner+1}");Line($"生存 {alive.Length} 駒");
            _ui.Box(new(r.X,y-5,r.Width,1),ink*.3f);
            if(!textOnly){Line($"短剣 {alive.Sum(u=>u.Daggers)}");Line($"小銃 {alive.Sum(u=>u.Rifles)} / 盾 {alive.Sum(u=>u.Shields)}");Line($"帰還先 Node {army.HomeNode+1}");}
        }
        _ui.Box(NewspaperSwitch,ink*.07f);
        _ui.Center("新聞を切り替える / "+(_newspaperEdition+1)+" / "+NewspaperProfiles.All.Length,NewspaperSwitch,.32f,ink);
    }
}
