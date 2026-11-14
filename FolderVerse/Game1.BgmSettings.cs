namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Rectangle BgmMenuItem = new(1320,462,538,52);
    private static readonly Rectangle BgmPanel = new(620,320,680,560);
    private static readonly Rectangle BgmToggle = new(780,435,360,60);
    private static readonly Rectangle BgmSlider = new(730,555,460,46);
    private static readonly Rectangle SeSlider = new(730,714,460,46);
    private static readonly Rectangle BgmBack = new(830,798,260,54);
    private readonly string _bgmSettingsPath = Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData),"FolderVerse","bgm-volume.txt");
    private bool _bgmMenuOpen, _bgmDragging, _bgmChanged;
    private bool _seDragging, _seChanged;
    private int _sePercent = 100;
    private float SeGain => _sePercent / 100f;
    private string SeSettingsPath => Path.ChangeExtension(_bgmSettingsPath,"se.txt");
    private int _bgmPercent = 100;
    private int _bgmRestorePercent = 100;
    private int _bgmDragStartPercent;
    private float BgmGain => _bgmPercent / 100f;

    private void LoadBgmSettings()
    {
        try
        {
            if (File.Exists(SeSettingsPath) && int.TryParse(File.ReadAllText(SeSettingsPath),out int se) &&
                se is >= 0 and <= 100) _sePercent = se;
            if (File.Exists(_bgmSettingsPath) && int.TryParse(File.ReadAllText(_bgmSettingsPath),out int value) &&
                value is >= -100 and <= 100 && value != 0)
            {
                _bgmRestorePercent = Math.Abs(value);
                _bgmPercent = Math.Max(0,value);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void SaveBgmSettings()
    {
        if (!_bgmChanged && !_seChanged) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_bgmSettingsPath));
            if (_bgmChanged) File.WriteAllText(_bgmSettingsPath,(_bgmPercent > 0 ? _bgmPercent : -_bgmRestorePercent).ToString());
            if (_seChanged) File.WriteAllText(SeSettingsPath,_sePercent.ToString());
        }
        catch (IOException error) { RecordFailure(error,"bgm_settings"); }
        catch (UnauthorizedAccessException error) { RecordFailure(error,"bgm_settings"); }
        finally { _bgmChanged = _seChanged = false; }
    }

    private bool BgmMenuInput(MouseState mouse,KeyboardState keys,bool active,bool click,bool escape)
    {
        if (!_bgmMenuOpen) return false;
        if (!active || mouse.LeftButton == ButtonState.Released)
        {
            _bgmDragging = _seDragging = false;
            SaveBgmSettings();
        }
        if (!active) return true;
        if (escape || click && (BgmBack.Contains(_pointer) || !BgmPanel.Contains(_pointer)))
        {
            _bgmMenuOpen = _bgmDragging = _seDragging = false;
            SaveBgmSettings();
            return true;
        }
        if (click && SeSlider.Contains(_pointer)) _seDragging = true;
        if (_seDragging)
        {
            int percent = ReadVolumeSlider(SeSlider);
            if (percent != _sePercent)
            {
                _sePercent = percent;
                _seChanged = true;
            }
            return true;
        }
        bool Pressed(Keys key) => keys.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        bool toggle = click && BgmToggle.Contains(_pointer) || Pressed(Keys.Space);
        int next = _bgmPercent;
        if (toggle)
        {
            next = _bgmPercent == 0 ? _bgmRestorePercent : 0;
            _bgmDragging = false;
        }
        else
        {
            if (click && BgmSlider.Contains(_pointer))
            {
                _bgmDragging = true;
                _bgmDragStartPercent = _bgmPercent;
            }
            if (_bgmDragging) next = ReadVolumeSlider(BgmSlider);
            else if (Pressed(Keys.Left)) next = Math.Max(0,next - 1);
            else if (Pressed(Keys.Right)) next = Math.Min(100,next + 1);
        }
        if (next != _bgmPercent)
        {
            if (next == 0)
                _bgmRestorePercent = _bgmDragging && _bgmDragStartPercent > 0 ? _bgmDragStartPercent : _bgmPercent;
            _bgmPercent = next;
            _bgmChanged = true;
            _conquestMusic?.SetGain(BgmGain);
            if (!_bgmDragging) SaveBgmSettings();
        }
        return true;
    }

    private int ReadVolumeSlider(Rectangle bounds) => (int)MathF.Round(100 * Math.Clamp(
        (_pointer.X - bounds.Left) / (float)(bounds.Width - 1),0,1));

    private void DrawVolumeSlider(Rectangle bounds,int percent)
    {
        int x = bounds.Left + (int)MathF.Round((bounds.Width - 1) * percent / 100f);
        int y = bounds.Center.Y;
        _ui.Box(new(bounds.Left,y-4,bounds.Width,8),Muted);
        if (percent > 0) _ui.Box(new(bounds.Left,y-4,x-bounds.Left+1,8),Accent);
        _ui.Box(new(x-8,y-15,16,30),Cream);
    }

    private void DrawBgmMenu()
    {
        if (!_bgmMenuOpen) return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.65f);
        _ui.Box(BgmPanel,new Color(24,45,57));
        _ui.Center("BGM音量",new(720,350,480,60),.68f,Cream);
        bool enabled = _bgmPercent > 0;
        _ui.Button(BgmToggle,enabled ? "BGM　オン" : "BGM　オフ",enabled ? Accent : Muted,.52f);
        _ui.Center($"{_bgmPercent}%",new(730,510,460,40),.48f,Cream);
        DrawVolumeSlider(BgmSlider,_bgmPercent);
        _ui.Center($"SE音量　{_sePercent}%",new(730,661,460,42),.48f,Cream);
        DrawVolumeSlider(SeSlider,_sePercent);
        _ui.Button(BgmBack,"戻る / Esc",Muted,.44f);
    }
}
