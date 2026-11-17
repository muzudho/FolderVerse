namespace FolderVerse;

using System.Linq;

public partial class Game1
{
    private void BeginPrimaplanaTutorial()
    {
        BeginPlayerCount();
        _titleMenuOpen=_tutorialMenuOpen=_openingActive=_autoPlay=_demoActive=false;
        _statusMenuOpen=_tutorialMessagesOpen=false;
        _tutorialAttackGuide=_tutorialAttackPending=_tutorialAttackAchieved=false;
        _tutorialAchievementAge=-1;
        _tutorialIntroShown=true;
        _setup.PreparePrimaplana();
        _statusCell=_populationCell=-1;
        _screen=Screen.WorldStatus;
        RestoreDomesticCamera();
        ShowTutorialGuide(23);
    }

    private void ReservePrimaplanaAttack()
    {
        if(_setup.Campaign.PlannedOrders.Any(order=>!order.Peaceful && order.TargetNode==_selectedMoveNode))return;
        var source=_invasionSources.FirstOrDefault(row=>row.Queen && InvasionRobots(row).Length>0)
            ?? _invasionSources.FirstOrDefault(row=>InvasionRobots(row).Length>0);
        if(source==null)return;
        source.Selected=source.Queen;
        source.Robots.UnionWith(InvasionRobots(source).Select(robot=>robot.Id));
        source.Count=source.Robots.Count;
        ReserveInvasion(source);
    }

    private void ResolvePrimaplanaCaptives()
    {
        foreach(var captive in _setup.Relations.Pending.ToArray())
            _setup.Relations.Choose(_setup,captive,DefeatedChoice.Recruit);
    }
}
