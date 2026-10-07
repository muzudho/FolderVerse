namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _endingWinner=-1,_endingScore,_endingCells;
    private double _endingAge;
    private static readonly Rectangle EndingMenuItem=new(1320,182,538,52),QuitMenuItem=new(1320,238,538,52);
    private static readonly Rectangle EndingReturn=new(548,960,824,64);
    private void StartEnding()
    {
        var winner=_setup.Relations.ProvisionalRanking(_setup)[0];
        _endingWinner=winner.Ruler;_endingScore=winner.Nodes;_endingCells=winner.Cells;_endingAge=0;
        CloseMovement();_statusMenuOpen=false;_populationCell=-1;_mapTransitionAge=-1;_screen=Screen.Ending;
        _operations.Write("ending_started",new{winner=_endingWinner,cells=_endingCells,score=_endingScore,turn=_setup.Population.Turn});
    }
    private void ReturnToTitle()
    {
        CloseMovement();_statusMenuOpen=false;_dispositionReturnList=false;
        _populationCell=_populationNode=_statusCell=_globeSelectedNode=-1;
        _nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=_nodeAssemblyOpen=false;
        _hangarDrag=null;_dragging=_mapDragging=false;_globePressedCell=-1;
        _mapTransitionAge=-1;_net.CancelAnimation();_net.ResetView();
        _statusGlobe=true;_net.ShowRoutes=true;_world.GlobeZoom=1;_world.GlobeAnchor=null;
        _robotPlaying=false;_endingWinner=-1;_endingAge=0;
        _setup.Campaign.Reset();_setup.Relations.Reset();_setup.Robots.Initialize(0,0);_setup.ClearPlacement();
        _screen=Screen.Title;_inputOutcome="game_ended_to_title";
    }
    private void UpdateEnding(double elapsed,bool finish)
    {_endingAge+=elapsed;if(finish || _endingAge>=10)ReturnToTitle();}
    private void DrawEndingUi()
    {
        var color=_setup.OwnerColor(_endingWinner);
        _ui.Center("世界征服者",new(300,44,1320,82),1.3f,Cream);
        _ui.Center($"暫定順位 １位 / #{_endingWinner+1:00}",new(300,140,1320,48),.6f,Cream);
        float reveal=MathHelper.Clamp((float)_endingAge/1.2f,0,1);int size=420+(int)(60*reveal);
        var portrait=new Rectangle(960-size/2,214,size,size);
        _ui.Box(new(portrait.X-8,portrait.Y-8,size+16,size+16),color);
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[_endingWinner],portrait,_setup.Relations.Powered[_endingWinner]);
        _ui.Center(_setup.ConquerorNames[_endingWinner],new(260,728,1400,64),.95f,Cream);
        _ui.Center($"征服セル　{_endingCells} / 征服節点　{_endingScore}",new(300,810,1320,42),.6f,Cream);
        for(int i=0;i<42;i++)
        {
            float x=100+(i*137%1720),y=170+(float)((i*79+_endingAge*65)%670);
            _ui.Tile(new(x,y),new(7,15),(float)_endingAge+i,Color.Lerp(color,Cream,(i%5)/4f));
        }
        _ui.Button(EndingReturn,"ゲームを止めてタイトル画面に戻る",Accent,.6f);
        _ui.Center($"{Math.Max(0,10-(int)_endingAge)}秒後にタイトルへ / Enter・Esc でも戻る",new(300,891,1320,36),.43f,Cream);
    }
}
