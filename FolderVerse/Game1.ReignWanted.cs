namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawReignWantedPoster(int player)
    {
        var paper=new Color(238,225,199);var ink=new Color(92,52,40);
        var sheet=new Rectangle(263,204,418,419);
        _ui.Box(new(sheet.X+8,sheet.Y+9,sheet.Width,sheet.Height),new Color(4,14,23)*.45f);
        _ui.Box(sheet,paper);
        for(int i=0;i<52;i++)
        {
            _ui.Box(new(sheet.X+i*8,sheet.Y-2+i%3,8,3),paper);
            _ui.Box(new(sheet.X+i*8,sheet.Bottom-1,8,2+i%4),paper);
        }
        for(int i=0;i<45;i++)_ui.Box(new(sheet.X+4,sheet.Y+8+i*10,sheet.Width-8,1),ink*.035f);
        _ui.Center("WANTED",new(sheet.X+18,sheet.Y+12,sheet.Width-36,62),1.05f,ink);
        _ui.Box(new(sheet.X+22,sheet.Y+81,sheet.Width-44,2),ink*.7f);
        var photo=new Rectangle(sheet.Center.X-120,sheet.Y+90,240,270);
        _newspaperPortraits??=new NewspaperPortraitRenderer(Content);
        _newspaperPortraits.Draw(_spriteBatch,_setup.Looks[player],photo,"id",true);
        DrawWantedStamp(photo);
        _ui.Center("国際警察",new(sheet.X+22,sheet.Bottom-44,sheet.Width-44,32),.5f,ink);
    }
}