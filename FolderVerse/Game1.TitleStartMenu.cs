namespace FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private Texture2D _titleStartPaper;
    private bool _titleMenuOpen,_titleMenuKeyboardFocus;
    private int _titleMenuSelection=2;
    private static Rectangle TitleMenuPaper=>new(570,80,780,920);
    // Slot 0 is reserved for the future How to Play mode.
    private static Rectangle TitleMenuItem(int slot)=>new(720,390+slot*150,480,84);
    private static readonly Color TitleMenuInk=new(43,62,68);
    private void OpenTitleStartMenu(){
        CheckAutoSave();_titleMenuOpen=true;_titleMenuKeyboardFocus=false;_titleMenuSelection=_hasAutoSave?1:2;_titleIdle=0;
    }
    private void TitleStartMenuInput(bool click,bool enter,bool escape,KeyboardState keys,bool active){
        if(!active)return;
        if(escape || click && !TitleMenuPaper.Contains(_pointer)){_titleMenuOpen=false;_titleIdle=0;return;}
        if(click || _pointer!=CanvasPoint(_previousMouse)){
            _titleMenuKeyboardFocus=false;
            if(_hasAutoSave && TitleMenuItem(1).Contains(_pointer))_titleMenuSelection=1;
            if(TitleMenuItem(2).Contains(_pointer))_titleMenuSelection=2;
        }
        bool navigating=(keys.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up)) || (keys.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down));
        if(navigating && _hasAutoSave){_titleMenuSelection=_titleMenuSelection==1?2:1;_titleMenuKeyboardFocus=true;}
        int chosen=click && _hasAutoSave && TitleMenuItem(1).Contains(_pointer)?1:click && TitleMenuItem(2).Contains(_pointer)?2:enter?_titleMenuSelection:-1;
        if(chosen==1){_titleMenuOpen=false;if(!ResumeAutoSave())_titleMenuOpen=true;}
        else if(chosen==2){_titleMenuOpen=false;BeginPlayerCount();}
    }
    private void DrawTitleStartMenu(){
        if(!_titleMenuOpen)return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.3f);
        _spriteBatch.Draw(_titleStartPaper,TitleMenuPaper,Color.White);
        void Item(int slot,string text){
            var r=TitleMenuItem(slot);
            bool highlighted=r.Contains(_pointer) || _titleMenuKeyboardFocus && _titleMenuSelection==slot;
            _ui.Center(text,r,.85f,highlighted?new Color(160,85,46):TitleMenuInk);
        }
        if(_hasAutoSave)Item(1,"続きから");
        Item(2,"最初から");
        if(_autoSaveError.Length>0)_ui.Center(_autoSaveError,new(680,840,560,55),.36f,new Color(145,57,46));
    }
}
