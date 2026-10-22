namespace FolderVerse;
using Microsoft.Xna.Framework.Audio;
public partial class Game1
{
    private IndustryMusic _industryMusic;
    private bool _industryMusicUnavailable;
    private void StopIndustryMusic(){_industryMusic?.Dispose();_industryMusic=null;}
    private void UpdateIndustryMusic()
    {
        if(_screen!=Screen.Industry){StopIndustryMusic();return;}
        if(_industryMusicUnavailable)return;
        try{_industryMusic??=new();_industryMusic.Update();}
        catch(NoAudioHardwareException error){StopIndustryMusic();_industryMusicUnavailable=true;RecordFailure(error,"industry_music");}
    }
}
