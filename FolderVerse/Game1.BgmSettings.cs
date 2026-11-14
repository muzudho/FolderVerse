namespace FolderVerse;
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private static readonly Rectangle BgmMenuItem = new(1320,462,538,52);
    private static readonly Rectangle BgmPanel = new(560,355,800,370);
    private static readonly Rectangle BgmToggle = new(610,458,210,56);
    private static readonly Rectangle SeToggle = new(610,542,210,56);
    private static readonly Rectangle BgmSlider = new(848,463,350,46);
    private static readonly Rectangle SeSlider = new(848,547,350,46);
    private static readonly Rectangle BgmBack = new(830,640,260,54);
    private readonly string _bgmSettingsPath = Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData),"FolderVerse","bgm-volume.txt");
    private bool _bgmMenuOpen, _bgmDragging, _bgmChanged;
    private bool _seDragging, _seChanged;
    private int _sePercent = 100;
    private int _seRestorePercent = 100;
    private int _seDragStartPercent;
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
                se is >= -100 and <= 100)
            {
                _sePercent = Math.Max(0,se);
                if (se != 0) _seRestorePercent = Math.Abs(se);
            }
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
            if (_seChanged) File.WriteAllText(SeSettingsPath,(_sePercent > 0 ? _sePercent : -_seRestorePercent).ToString());
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
        bool toggleSe = click && SeToggle.Contains(_pointer);
        if (click && SeSlider.Contains(_pointer))
        {
            _seDragging = true;
            _seDragStartPercent = _sePercent;
        }
        if (toggleSe || _seDragging)
        {
            int percent = toggleSe ? (_sePercent > 0 ? 0 : _seRestorePercent) : ReadVolumeSlider(SeSlider);
            if (percent != _sePercent)
            {
                if (percent == 0)
                    _seRestorePercent = _seDragging && _seDragStartPercent > 0 ? _seDragStartPercent : _sePercent;
                _sePercent = percent;
                _seChanged = true;
                if (toggleSe) SaveBgmSettings();
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
        _ui.Center("BGM・SE音量",new(720,375,480,60),.68f,Cream);
        bool enabled = _bgmPercent > 0;
        _ui.Button(BgmToggle,enabled ? "BGM　オン" : "BGM　オフ",enabled ? Accent : Muted,.52f);
        DrawVolumeSlider(BgmSlider,_bgmPercent);
        _ui.Center($"{_bgmPercent}%",new(1218,BgmSlider.Y,100,BgmSlider.Height),.48f,Cream);
        bool seEnabled = _sePercent > 0;
        _ui.Button(SeToggle,seEnabled ? "SE　オン" : "SE　オフ",seEnabled ? Accent : Muted,.52f);
        DrawVolumeSlider(SeSlider,_sePercent);
        _ui.Center($"{_sePercent}%",new(1218,SeSlider.Y,100,SeSlider.Height),.48f,Cream);
        _ui.Button(BgmBack,"戻る / Esc",Muted,.44f);
    }
}
