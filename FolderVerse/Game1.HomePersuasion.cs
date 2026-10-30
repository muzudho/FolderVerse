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
            int previousTab=_lastNodeTab;OpenNodeInspector(_homePersuasion[0].Node);OpenNodeAssembly();_lastNodeTab=previousTab;_homePersuasionShown.Add(_setup.PlayerSlot);_homePersuasionActive=true;_homePersuasionAge=0;
        }
        _homePersuasionAge+=elapsed;
        if(_homePersuasionAge>=8 || _homePersuasionAge>=5 && proceed)
        {_homePersuasionActive=false;_homePersuasion.Clear();CloseNodeInspector();return true;}
        return true;
    }
    private Robot HomePersuasionRobot(Robot robot)
    {
        if(!_homePersuasionActive || _homePersuasionAge>=4)return robot;
        return _homePersuasion.FirstOrDefault(e=>e.Before.Id==robot.Id)?.Before??robot;
    }
    private string HomePersuasionMessage
    {
        get
        {
            if(_homePersuasionAge<4)return "ロボットのみなさーん！　寝返ってくださーい！";
            if(_homePersuasion.Any(e=>e.Success && e.Before.Owner!=e.Ruler))return "やったあ！　こんにちわです～！";
            if(_homePersuasion.Any(e=>e.Success))return "これからも、よろしくお願いします～！";
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
        for(int slot=0;slot<_hangarSlots.Length;slot++)
        {
            var result=_homePersuasion.FirstOrDefault(e=>e.Before.Id==_hangarSlots[slot]);if(result==null)continue;
            var cell=HangarCell(slot);cell.Offset(NodeInspectorOffset,0);
            if(_homePersuasionAge>=1 && _homePersuasionAge<4)
            {
                int bounce=(int)(Math.Sin(_homePersuasionAge*7+slot)*4);
                var bubble=new Rectangle(cell.X+20,cell.Y+114+bounce,140,28);
                _ui.Box(bubble,Cream);_ui.Center(slot%2==0?"ざわざわ……":"どうする？",bubble,.32f,Muted);
            }
            else if(_homePersuasionAge>=4 && result.Success)
            {
                float glow=.5f+.5f*MathF.Sin((float)_homePersuasionAge*8);
                _ui.Box(new(cell.X,cell.Y,cell.Width,4),Accent*glow);
                _ui.Box(new(cell.X,cell.Bottom-4,cell.Width,4),Accent*glow);
                var bubble=new Rectangle(cell.X+15,cell.Y+112,cell.Width-30,28);
                _ui.Box(bubble,Cream);_ui.Center("説得成功！",bubble,.38f,Muted);
            }
        }
    }
}
