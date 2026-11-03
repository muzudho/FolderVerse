namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private float _tutorialChromeSlide;
    private float _tutorialGuideClosing=-1;
    private bool _tutorialDrawingHost;
    private void UpdateTutorialChrome(float elapsed)
    {
        if(!TutorialReviewAvailable){_tutorialChromeSlide=0;_tutorialGuideClosing=-1;return;}
        if(_tutorialGuideClosing>=0){_tutorialGuideClosing+=Math.Max(0,elapsed);if(_tutorialGuideClosing>=.35f)_tutorialGuideClosing=-1;}
        bool hidden=_tutorialAttackGuide || _tutorialGuideClosing>=0 || _tutorialAchievementAge>=0;
        _tutorialChromeSlide=Math.Clamp(_tutorialChromeSlide+(hidden?1:-1)*Math.Max(0,elapsed)/.35f,0,1);
    }
    private int TutorialButtonOffset(Rectangle bounds)=>_tutorialDrawingHost?0:TutorialChromeOffset(bounds.Center.Y<540);
    private int TutorialChromeOffset(bool top)=>(top?-1:1)*(int)MathF.Round(1100*MathHelper.SmoothStep(0,1,_tutorialChromeSlide));
    private void DrawTutorialChrome(Action draw,bool top)
    {
        int previous=_ui.OffsetY;
        _ui.OffsetY=TutorialChromeOffset(top);
        try{draw();}finally{_ui.OffsetY=previous;}
    }
}
