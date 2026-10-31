namespace FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private Texture2D _titleStartButton;
    private Rectangle _titleStartButtonSource;
    private bool _titleMenuOpen,_titleMenuKeyboardFocus;
    private int _titleMenuSelection=3;
    private static Rectangle TitleMenuPaper=>new(765,310,390,550);
    // Four title actions share the paper menu.
    private static Rectangle TitleMenuItem(int slot)=>new(815,385+slot*100,290,64);
    private static Color TitleMenuInk=>RulebookInk;
    private void LoadTitleStartAssets(){
        _titleStartButton=Content.Load<Texture2D>("Images/title-start-button-v001");
        var pixels=new Color[_titleStartButton.Width*_titleStartButton.Height];_titleStartButton.GetData(pixels);
        int left=_titleStartButton.Width,top=_titleStartButton.Height,right=-1,bottom=-1;
        for(int y=0;y<_titleStartButton.Height;y++)for(int x=0;x<_titleStartButton.Width;x++)if(pixels[y*_titleStartButton.Width+x].A>=24){
            left=System.Math.Min(left,x);top=System.Math.Min(top,y);right=System.Math.Max(right,x);bottom=System.Math.Max(bottom,y);
        }
        _titleStartButtonSource=right>=left?new(left,top,right-left+1,bottom-top+1):_titleStartButton.Bounds;
    }
    private void OpenTitleStartMenu(){
        CheckAutoSave();_titleMenuOpen=true;_titleMenuKeyboardFocus=false;_titleMenuSelection=_hasAutoSave?2:3;_titleIdle=0;
    }
    private void TitleStartMenuInput(bool click,bool enter,bool escape,KeyboardState keys,bool active){
        if(!active)return;
        if(_campaignMenuStage>0){CampaignMenuInput(click,enter,escape,keys);return;}
        if(_tutorialMenuOpen){TutorialMenuInput(click,enter,escape,keys);return;}
        if(escape || click && !TitleMenuPaper.Contains(_pointer)){_titleMenuOpen=false;_titleIdle=0;return;}
        int[] slots=_hasAutoSave?new[]{0,1,2,3}:new[]{0,1,3};
        if(click || _pointer!=CanvasPoint(_previousMouse)){
            _titleMenuKeyboardFocus=false;
            foreach(int slot in slots)if(TitleMenuItem(slot).Contains(_pointer))_titleMenuSelection=slot;
        }
        bool navigating=(keys.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up)) || (keys.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down));
        if(navigating){int i=System.Array.IndexOf(slots,_titleMenuSelection);_titleMenuSelection=slots[(i+(keys.IsKeyDown(Keys.Up)?slots.Length-1:1))%slots.Length];_titleMenuKeyboardFocus=true;}
        int chosen=-1;
        if(click)foreach(int slot in slots)if(TitleMenuItem(slot).Contains(_pointer))chosen=slot;
        if(enter)chosen=_titleMenuSelection;
        if(chosen==0)_tutorialMenuOpen=true;
        else if(chosen==1)OpenCampaignMenu();
        else if(chosen==2){_titleMenuOpen=false;if(!ResumeAutoSave())_titleMenuOpen=true;}
        else if(chosen==3){_titleMenuOpen=false;BeginPlayerCount();}
    }
    private void DrawTitleStartMenu(){
        if(_campaignMenuStage>0){DrawCampaignMenu();return;}
        if(_tutorialMenuOpen){DrawTutorialMenu();return;}
        if(!_titleMenuOpen){
            _spriteBatch.Draw(_titleStartButton,StartButton,_titleStartButtonSource,Color.White);
            _ui.Center("PRESS ENTER",new(740,1000,440,40),.42f,new Color(26,55,80));return;
        }
        _ui.Box(new(0,0,1920,1080),Color.Black*.3f);
        DrawRulebookPaper(TitleMenuPaper);
        var heading=new Rectangle(790,336,340,34);
        // Expand the printed strokes in every direction for an extra-bold heading.
        for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){
            var impression=heading;impression.Offset(x,y);
            _ui.Center("スタートメニュー",impression,.6f,RulebookRose);
        }
        void Item(int slot,string text,bool enabled=true){
            var r=TitleMenuItem(slot);
            bool highlighted=enabled && (r.Contains(_pointer) || _titleMenuKeyboardFocus && _titleMenuSelection==slot);
            if(highlighted){_ui.Box(new(r.X,r.Bottom-5,r.Width,1),TitleMenuInk);_ui.Text("›",new(r.X-14,r.Y+12),.6f,TitleMenuInk);}
            _ui.Center(text,r,.65f,enabled?TitleMenuInk:new Color(139,129,125));
        }
        Item(0,"遊び方");
        Item(1,"キャンペーンモード");
        Item(2,"続きから",_hasAutoSave);
        Item(3,"最初から");
        if(_autoSaveError.Length>0)_ui.Center(_autoSaveError,new(795,800,330,45),.26f,TitleMenuInk);
    }
}
