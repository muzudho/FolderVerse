namespace FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private Point _windowedSize;
    private Point _windowedPosition;
    private void UpdateFullscreen(KeyboardState keyboard,bool active)
    {
        if(!active || keyboard.IsKeyUp(Keys.F11) || _previousKeyboard.IsKeyDown(Keys.F11))return;
        if(!_graphics.IsFullScreen){
            _windowedSize=new(Window.ClientBounds.Width,Window.ClientBounds.Height);
            _windowedPosition=Window.Position;
            var display=GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth=display.Width;
            _graphics.PreferredBackBufferHeight=display.Height;
        }else{
            var display=GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            // A desktop-sized client leaves its title bar outside the display.
            // Restore a visibly windowed size when the saved size cannot fit.
            bool oversized=_windowedSize.X>=display.Width || _windowedSize.Y>=display.Height;
            var size=oversized?new Point(display.Width*9/10,display.Height*9/10):_windowedSize;
            _graphics.PreferredBackBufferWidth=System.Math.Max(1,size.X);
            _graphics.PreferredBackBufferHeight=System.Math.Max(1,size.Y);
            if(oversized)_windowedPosition=new(display.Width/20,display.Height/20);
        }
        _graphics.HardwareModeSwitch=false;
        _graphics.IsFullScreen=!_graphics.IsFullScreen;
        _graphics.ApplyChanges();
        if(!_graphics.IsFullScreen){Window.IsBorderless=false;Window.Position=_windowedPosition;}
        _operations.Write("fullscreen_changed",new{fullscreen=_graphics.IsFullScreen,width=Window.ClientBounds.Width,height=Window.ClientBounds.Height});
        IsMouseVisible=!_graphics.IsFullScreen && !_demoActive;
    }
    private void DrawFullscreenCursor()
    {
        IsMouseVisible=!_graphics.IsFullScreen && !_demoActive;
        if(!_graphics.IsFullScreen || _demoActive)return;
        var mouse=Mouse.GetState();
        DrawMouseArrow(new(mouse.X,mouse.Y));
    }
    // Physical backbuffer coordinates keep the pointer aligned at every aspect ratio.
    private void DrawMouseArrow(Point at)
    {
        _spriteBatch.Begin();
        for(int y=0;y<23;y++){
            int width=y*3/4+1;
            _spriteBatch.Draw(_pixel,new Rectangle(at.X,at.Y+y,width,1),Color.Black);
            if(y>2 && y<20 && width>3)_spriteBatch.Draw(_pixel,new Rectangle(at.X+2,at.Y+y,width-3,1),Color.White);
        }
        _spriteBatch.Draw(_pixel,new Vector2(at.X+8,at.Y+17),null,Color.Black,-.4f,Vector2.Zero,new Vector2(7,14),Microsoft.Xna.Framework.Graphics.SpriteEffects.None,0);
        _spriteBatch.Draw(_pixel,new Vector2(at.X+10,at.Y+18),null,Color.White,-.4f,Vector2.Zero,new Vector2(3,11),Microsoft.Xna.Framework.Graphics.SpriteEffects.None,0);
        _spriteBatch.End();
    }
}
