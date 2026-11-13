namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private List<DomesticPersuasion> _homePersuasion=new();
    private List<DomesticPersuasion> _homePersuasionResults=new();
    private HashSet<int> _homePersuasionShown=new();
    private WorldSetup _homePersuasionWorld;
    private int _homePersuasionTurn=-1;
    private bool _homePersuasionActive;
    private double _homePersuasionAge;
    private const double PersuasionOverviewSeconds=.8, PersuasionPanSeconds=.4, PersuasionRevealSeconds=1.25, PersuasionStopSeconds=2.45;
    private Point[] _persuasionStops=Array.Empty<Point>();
    private Dictionary<long,int> _persuasionStopByRobot=new();
    private int PersuasionStop=>Math.Clamp((int)((_homePersuasionAge-PersuasionOverviewSeconds)/PersuasionStopSeconds),0,Math.Max(0,_persuasionStops.Length-1));
    private double PersuasionStopAge=>_homePersuasionAge-PersuasionOverviewSeconds-PersuasionStop*PersuasionStopSeconds;

    private void PlanPersuasionTour()
    {
        var stops=new List<Point>();
        _persuasionStopByRobot.Clear();
        for(int slot=0;slot<_hangarSlots.Length;slot++){
            if(_hangarSlots[slot] is not long id || !_homePersuasion.Any(e=>e.Before.Id==id))continue;
            int x=slot%HangarGridColumns,y=slot/HangarGridColumns;
            int stop=stops.FindIndex(p=>x>=p.X && x<p.X+3 && y>=p.Y && y<p.Y+2);
            if(stop<0){
                stop=stops.Count;
                stops.Add(new(Math.Min(x/3*3,HangarLastColumn),Math.Min(y/2*2,HangarLastRow)));
            }
            _persuasionStopByRobot[id]=stop;
        }
        _persuasionStops=stops.ToArray();
        _hangarIntroStart=float.NegativeInfinity;
    }
    private void PrepareHomePersuasion()
    {
        if(!ReferenceEquals(_homePersuasionWorld,_setup) || _homePersuasionTurn!=_setup.Population.Turn)
        {
            _homePersuasionWorld=_setup;_homePersuasionTurn=_setup.Population.Turn;
            _homePersuasionResults=new();_homePersuasionShown=new();
        }
        _homePersuasionResults.AddRange(_setup.Robots.PersuadeAtHome(_setup));
        _homePersuasion=_homePersuasionShown.Contains(_setup.PlayerSlot)?new():_homePersuasionResults.Where(e=>e.Ruler==_setup.PlayerSlot).ToList();
        _homePersuasionActive=false;_homePersuasionAge=0;
    }
    private bool UpdateHomePersuasion(double elapsed,bool proceed,double now)
    {
        if(!IsStatusScreen || _homePersuasion==null || _homePersuasion.Count==0)return false;
        if(!_homePersuasionActive)
        {
            if(_conquestOverview || _tutorialAttackGuide || _tutorialMessagesOpen || _lastCutInPhase!="内政フェーズ" || now-_cutInStarted<PhaseCutInSeconds)return false;
            int previousTab=_lastNodeTab;OpenNodeInspector(_homePersuasion[0].Node);OpenNodeAssembly();_lastNodeTab=previousTab;_homePersuasionShown.Add(_setup.PlayerSlot);_homePersuasionActive=true;_homePersuasionAge=0;PlanPersuasionTour();
        }
        _homePersuasionAge+=elapsed;
        if(_persuasionStops.Length>0){
            var stop=_persuasionStops[PersuasionStop];
            _hangarColumn=stop.X;_hangarPage=stop.Y;
        }
        double end=PersuasionOverviewSeconds+_persuasionStops.Length*PersuasionStopSeconds;
        if(_homePersuasionAge>=end || _homePersuasionAge>=end-.45 && proceed)
        {_homePersuasionActive=false;_homePersuasion.Clear();CloseNodeInspector();return true;}
        return true;
    }
    private Robot HomePersuasionRobot(Robot robot)
    {
        if(!_homePersuasionActive || !_persuasionStopByRobot.TryGetValue(robot.Id,out int stop) || stop<PersuasionStop || stop==PersuasionStop && PersuasionStopAge>=PersuasionRevealSeconds)return robot;
        return _homePersuasion.FirstOrDefault(e=>e.Before.Id==robot.Id)?.Before??robot;
    }
    private string HomePersuasionMessage
    {
        get
        {
            if(PersuasionStopAge<PersuasionRevealSeconds)return _homePersuasion.Any(e=>e.Before.NativeParts(e.Ruler)>0)?"あなたたち、起きて！":"ロボットのみなさーん！　寝返ってくださーい！";
            if(_homePersuasion.Any(e=>_persuasionStopByRobot.GetValueOrDefault(e.Before.Id,-1)==PersuasionStop && e.Success && e.Before.Owner!=e.Ruler))return "やったあ！　こんにちわです～！";
            if(_homePersuasion.Any(e=>_persuasionStopByRobot.GetValueOrDefault(e.Before.Id,-1)==PersuasionStop && e.Success))return "これからも、よろしくお願いします～！";
            return "また、お話ししましょう～！";
        }
    }
    private void DrawHomePersuasion()
    {
        if(!_homePersuasionActive || !IsStatusScreen)return;
        int ruler=_homePersuasion[0].Ruler;
        _ui.Box(new(980,150,890,825),new Color(16,34,47,245));
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(1090,350,690,600),true,forCreation:true);
        _ui.Box(new(1000,180,850,140),Cream);_ui.Tile(new(1400,325),new(28,28),MathHelper.PiOver4,Cream);
        string text=HomePersuasionMessage;
        _ui.Center(text,new(1020,200,810,100),Math.Min(.6f,800/_font.MeasureString(text).X),Muted);
        foreach(int slot in VisibleAssemblySlots())
        {
            var result=_homePersuasion.FirstOrDefault(e=>e.Before.Id==_hangarSlots[slot]);if(result==null || _persuasionStopByRobot.GetValueOrDefault(result.Before.Id,-1)!=PersuasionStop || PersuasionStopAge<PersuasionPanSeconds)continue;
            var cell=AssemblyCell(slot);cell.Offset(NodeInspectorOffset,0);
            if(PersuasionStopAge<PersuasionRevealSeconds)
            {
                int bounce=(int)(Math.Sin(_homePersuasionAge*7+slot)*4);
                var bubble=new Rectangle(cell.X+20,cell.Y+114+bounce,140,28);
                _ui.Box(bubble,Cream);_ui.Center(slot%2==0?"ざわざわ……":"どうする？",bubble,.32f,Muted);
            }
            else if(result.Success)
            {
                float glow=.5f+.5f*MathF.Sin((float)_homePersuasionAge*8);
                _ui.Box(new(cell.X,cell.Y,cell.Width,4),Accent*glow);
                _ui.Box(new(cell.X,cell.Bottom-4,cell.Width,4),Accent*glow);
                var bubble=new Rectangle(cell.X+15,cell.Y+112,cell.Width-30,28);
                _ui.Box(bubble,Cream);_ui.Center(result.Before.Owner!=result.After.Owner?"寝返り成功！":"説得成功！",bubble,.38f,Muted);
            }
        }
    }
}
