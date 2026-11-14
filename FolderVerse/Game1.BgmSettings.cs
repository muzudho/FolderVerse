namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Rectangle BgmMenuItem = new(1320,462,538,52);
    private static readonly Rectangle BgmPanel = new(620,320,680,420);
    private static readonly Rectangle BgmToggle = new(780,435,360,60);
    private static readonly Rectangle BgmSlider = new(730,555,460,46);
    private static readonly Rectangle BgmBack = new(830,650,260,54);
    private readonly string _bgmSettingsPath = Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData),"FolderVerse","bgm-volume.txt");
    private bool _bgmMenuOpen, _bgmDragging, _bgmChanged;
    private bool _bgmEnabled = true;
    private int _bgmPercent = 100;
    private float BgmGain => _bgmEnabled ? _bgmPercent / 100f : 0;

    private void LoadBgmSettings()
    {
        try
        {
            if (!File.Exists(_bgmSettingsPath)) return;
            if (int.TryParse(File.ReadAllText(_bgmSettingsPath),out int value) &&
                value is >= -100 and <= 100 && value != 0)
            {
                _bgmEnabled = value > 0;
                _bgmPercent = Math.Abs(value);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void SaveBgmSettings()
    {
        if (!_bgmChanged) return;
        _bgmChanged = false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_bgmSettingsPath));
            File.WriteAllText(_bgmSettingsPath,(_bgmEnabled ? _bgmPercent : -_bgmPercent).ToString());
        }
        catch (IOException error) { RecordFailure(error,"bgm_settings"); }
        catch (UnauthorizedAccessException error) { RecordFailure(error,"bgm_settings"); }
    }

    private bool BgmMenuInput(MouseState mouse,KeyboardState keys,bool active,bool click,bool escape)
    {
        if (!_bgmMenuOpen) return false;
        if (!active || mouse.LeftButton == ButtonState.Released)
        {
            _bgmDragging = false;
            SaveBgmSettings();
        }
        if (!active) return true;
        if (escape || click && (BgmBack.Contains(_pointer) || !BgmPanel.Contains(_pointer)))
        {
            _bgmMenuOpen = _bgmDragging = false;
            SaveBgmSettings();
            return true;
        }
        bool Pressed(Keys key) => keys.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        bool toggle = click && BgmToggle.Contains(_pointer) || Pressed(Keys.Space);
        int next = _bgmPercent;
        if (toggle)
        {
            _bgmEnabled = !_bgmEnabled;
            _bgmDragging = false;
        }
        if (_bgmEnabled)
        {
            if (click && BgmSlider.Contains(_pointer)) _bgmDragging = true;
            if (_bgmDragging) next = 1 + (int)MathF.Round(99 * Math.Clamp(
                (_pointer.X - BgmSlider.Left) / (float)(BgmSlider.Width - 1),0,1));
            else if (Pressed(Keys.Left)) next = Math.Max(1,next - 1);
            else if (Pressed(Keys.Right)) next = Math.Min(100,next + 1);
        }
        if (toggle || next != _bgmPercent)
        {
            _bgmPercent = next;
            _bgmChanged = true;
            _conquestMusic?.SetGain(BgmGain);
            if (!_bgmDragging) SaveBgmSettings();
        }
        return true;
    }

    private void DrawBgmMenu()
    {
        if (!_bgmMenuOpen) return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.65f);
        _ui.Box(BgmPanel,new Color(24,45,57));
        _ui.Center("BGM音量",new(720,350,480,60),.68f,Cream);
        _ui.Button(BgmToggle,_bgmEnabled ? "BGM　オン" : "BGM　オフ",_bgmEnabled ? Accent : Muted,.52f);
        _ui.Center(_bgmEnabled ? $"{_bgmPercent}%" : $"オフ（{_bgmPercent}%）",new(730,510,460,40),.48f,Cream);
        int x = BgmSlider.Left + (int)MathF.Round((BgmSlider.Width - 1) * (_bgmPercent - 1) / 99f);
        int y = BgmSlider.Center.Y;
        _ui.Box(new(BgmSlider.Left,y-4,BgmSlider.Width,8),Muted);
        _ui.Box(new(BgmSlider.Left,y-4,x-BgmSlider.Left+1,8),_bgmEnabled ? Accent : Muted);
        _ui.Box(new(x-8,y-15,16,30),_bgmEnabled ? Cream : Muted);
        _ui.Button(BgmBack,"戻る / Esc",Muted,.44f);
    }
}
