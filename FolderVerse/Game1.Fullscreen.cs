namespace FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Point _windowedSize;
    private void UpdateFullscreen(KeyboardState keyboard,bool active)
    {
        if(!active || keyboard.IsKeyUp(Keys.F11) || _previousKeyboard.IsKeyDown(Keys.F11))return;
        if(!_graphics.IsFullScreen){
            _windowedSize=new(Window.ClientBounds.Width,Window.ClientBounds.Height);
            var display=GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth=display.Width;
            _graphics.PreferredBackBufferHeight=display.Height;
        }else{
            _graphics.PreferredBackBufferWidth=_windowedSize.X;
            _graphics.PreferredBackBufferHeight=_windowedSize.Y;
        }
        _graphics.HardwareModeSwitch=false;
        _graphics.IsFullScreen=!_graphics.IsFullScreen;
        _graphics.ApplyChanges();
    }
}
