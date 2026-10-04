namespace FolderVerse;
using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private int _populationCell=-1,_populationRole;
    private int _populationNode=-1;
    private static readonly Rectangle PopulationTurnButton=new(548,1018,350,52);
    private static readonly string[] PopulationRoles={"戦闘員数","生産者数","残数"};
    private static readonly string[] PopulationDirections={"北","東","南","西"};
    private static Rectangle RoleButton(int role)=>new(84+role*264,326,250,94);
    private static Rectangle RateButton(int row,bool plus)=>new(plus?798:726,463+row*59,62,44);
    private static Rectangle MigrationButton(int direction,bool plus)=>new(plus?798:726,682+direction*61,62,44);
    private static string Percent(decimal value)=>value.ToString("0.0#####",CultureInfo.InvariantCulture)+" %";
    private bool PopulationClick(Point pointer,KeyboardState keyboard)
    {
        decimal step=keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl)?100:
            keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift)?10:1;
        if(PopulationTurnButton.Contains(pointer)){_setup.Campaign.Advance(_setup,-1,0);_world.ShowSetup(_setup,true);if(_populationCell>=0 && _setup.Nodes.All[_populationNode].Owner!=_setup.PlayerSlot)_populationCell=-1;OpenBattle();return true;}
        if(_populationCell<0)return false;
        if(new Rectangle(803,169,58,44).Contains(pointer)){_populationCell=-1;return true;}
        var simulation=_setup.Population;var population=_setup.Nodes.All[_populationNode].Population;
        for(int role=0;role<3;role++)if(RoleButton(role).Contains(pointer)){_populationRole=role;return true;}
        for(int row=0;row<3;row++)foreach(bool plus in new[]{false,true})
        {
            if(!RateButton(row,plus).Contains(pointer))continue;
            if(row<2)simulation.AdjustNodeConversion(_setup,_populationNode,row,(plus?1:-1)*step);
            else population.BirthPercent=Math.Clamp(population.BirthPercent+(plus?0.00001m:-0.00001m)*step,0,100);
            return true;
        }
        for(int direction=0;direction<4;direction++)foreach(bool plus in new[]{false,true})
        {
            if(!MigrationButton(direction,plus).Contains(pointer))continue;
            if(simulation.CanMigrateNode(_setup,_populationNode,direction))simulation.AdjustNodeMigration(_setup,_populationNode,_populationRole,direction,(plus?0.1m:-0.1m)*step);
            return true;
        }
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void DrawPopulationPanel()
    {
        var simulation=_setup.Population;var post=_setup.Nodes.All[_populationNode];var population=post.Population;
        _ui.Box(new(64,162,850,825),new Color(3,13,20)*.8f);
        _ui.Box(new(54,150,850,825),new(27,49,62));
        _ui.Box(new(54,150,850,6),_setup.OwnerColor(_setup.PlayerSlot));
        string city=_setup.Nodes.Label(post)+" / "+_setup.Nodes.Pattern(post);
        _ui.Text(city,new(84,178),Math.Min(.7f,690/_font.MeasureString(city).X),Cream);
        _ui.Button(new(803,169,58,44),"×",Muted,.7f);
        _ui.Text("健康な若者の総数",new(84,235),.75f,Cream);
        _ui.Text(population.Total.ToString("N0",CultureInfo.InvariantCulture)+" 人",new(84,273),1.0f,Color.White);
        for(int role=0;role<3;role++)
        {
            var button=RoleButton(role);
            _ui.Button(button,"",role==_populationRole?Accent:Muted);
            _ui.Center(PopulationRoles[role],new(button.X,button.Y+7,button.Width,33),.6f,Cream);
            _ui.Center(population.People[role].ToString("N0",CultureInfo.InvariantCulture)+" 人",new(button.X,button.Y+45,button.Width,42),.65f,Color.White);
        }
        _ui.Text("役割の異動 / ＋は残数から、－は残数へ戻す",new(84,432),.47f,new(178,210,219));
        for(int row=0;row<3;row++)
        {
            int y=463+row*59;
            string label=row<2?"残数 → "+PopulationRoles[row]:"生産者による若者の生産";
            _ui.Text(label,new(84,y+7),.53f,Cream);
            _ui.Center(Percent(row<2?population.Conversion[row]:population.BirthPercent),new(496,y,216,44),.63f,Color.White);
            _ui.Button(RateButton(row,false),"−",Muted,.7f);_ui.Button(RateButton(row,true),"＋",Muted,.7f);
        }
        _ui.Text("１ターン後の移住 / "+PopulationRoles[_populationRole]+" / 0.1 %ずつ",new(84,647),.50f,Cream);
        for(int direction=0;direction<4;direction++)
        {
            int y=682+direction*61;bool enabled=simulation.CanMigrateNode(_setup,_populationNode,direction);
            int target=simulation.Neighbor(_setup,_populationCell,direction);
            _ui.Text(PopulationDirections[direction]+"へ",new(84,y+5),.7f,enabled?Cream:new Color(134,156,164));
            string destination=enabled?_setup.CityNames[target]:"陸路なし・他国節点：移住不可";
            _ui.Text(destination,new(170,y+12),Math.Min(.42f,300/_font.MeasureString(destination).X),new(170,200,211));
            _ui.Center(Percent(population.Migration[_populationRole,direction]),new(496,y,216,44),.65f,enabled?Color.White:new Color(134,156,164));
            _ui.Button(MigrationButton(direction,false),"−",enabled?Muted:new Color(37,52,60),.7f);
            _ui.Button(MigrationButton(direction,true),"＋",enabled?Muted:new Color(37,52,60),.7f);
        }
        _ui.Text("役割：1 % / 生産：0.00001 % / Shift：10倍 / Ctrl：100倍",new(84,934),.39f,new(178,210,219));
    }
}
