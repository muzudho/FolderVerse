namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void RecordPetitstellaMedals()
    {
        if(!_autoSaveEnabled || _setup.PlayerSlot<0 || _demoActive || _autoPlay || _setup.TutorialStep!=1 || !_setup.TutorialMapBeforeWar)return;
        try{
            if(_tutorialAchievements==null)LoadTutorialAchievements();
            if(_tutorialAchievements==null)return;
            if(!_tutorialAchievements.Has(TutorialAchievements.PetitstellaLessons) && TutorialAchievements.PetitstellaLessonsCompleted(_setup))_tutorialAchievements.Award(TutorialAchievements.PetitstellaLessons);
            if(!_tutorialAchievements.Has(TutorialAchievements.PetitstellaConquest) && TutorialAchievements.PetitstellaWorldConquered(_setup))_tutorialAchievements.Award(TutorialAchievements.PetitstellaConquest);
        }catch(Exception e){_tutorialAchievementError="勲章の保存に失敗しました";RecordFailure(e,"tutorial_planet_medals_write");}
    }
    private void DrawPetitstellaMedals(Rectangle card)
    {
        int slot=0;
        void Medal(string id,string label){
            if(_tutorialAchievements?.Has(id)!=true)return;
            int x=card.Right-94+slot++*56;
            DrawTutorialMedal(new(x,card.Y+40));
            _ui.Center(label,new(x-28,card.Y+66,56,22),.24f,new Color(255,214,112));
        }
        Medal(TutorialAchievements.PetitstellaLessons,"修了");
        Medal(TutorialAchievements.PetitstellaConquest,"世界征服");
    }
}
