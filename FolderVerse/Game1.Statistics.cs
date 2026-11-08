namespace FolderVerse;
using System;
using System.IO;
using System.Reflection;

public partial class Game1
{
    private bool _statisticsEnabled=Assembly.GetEntryAssembly()==typeof(Game1).Assembly;
    private StatisticsLog _statistics;
    private void WriteTurnStatistics()
    {
        if(!_statisticsEnabled || _demoActive || _setup.Industry.Pending || _setup.Nodes.All.Length==0)return;
        try{
            _statistics??=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FolderVerse","Statistics"));
            _statistics.Write(_setup);
        }catch(Exception error) when(error is IOException or UnauthorizedAccessException){
            RecordFailure(error,"statistics_write");
        }
    }
}
