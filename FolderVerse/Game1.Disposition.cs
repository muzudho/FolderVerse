namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _statusMenuOpen,_dispositionReturnList;
    private static readonly Rectangle StatusMenuButton=new(1660,20,198,46),InactiveMenuItem=new(1320,70,538,52),InactiveBack=new(54,1020,270,50);
    private static readonly Rectangle HierarchyMenuItem=new(1320,126,538,52);
    private static Rectangle DispositionButton(int index)=>new(380+index*400,860,370,70);
    private static Rectangle InactiveCard(int index)=>new(54+index%5*362,140+index/5*214,346,198);
    private static Rectangle InsertBatteryButton(int index){var r=InactiveCard(index);return new(r.X+145,r.Y+100,190,60);}
    private void OpenDisposition()
    {
        if(_setup.Relations.Pending.Count>0){_screen=Screen.Disposition;_populationCell=-1;}
    }
    private bool StatusMenuClick(Point point)
    {
        if(StatusMenuButton.Contains(point)){_statusMenuOpen=!_statusMenuOpen;return true;}
        if(_statusMenuOpen && InactiveMenuItem.Contains(point))
        {CloseMovement();_statusMenuOpen=false;_screen=Screen.InactiveList;return true;}
        if(_statusMenuOpen && HierarchyMenuItem.Contains(point))
        {CloseMovement();_statusMenuOpen=false;_screen=Screen.Hierarchy;return true;}
        _statusMenuOpen=false;return false;
    }
    private void DrawStatusMenu()
    {
        _ui.Button(StatusMenuButton,"メニュー",Muted,.52f);
        if(_statusMenuOpen)
        {
            _ui.Button(InactiveMenuItem,"電池を引き抜いた征服者一覧",Muted,.5f);
            _ui.Button(HierarchyMenuItem,"征服者の関係ツリー",Muted,.5f);
        }
    }
    private void DispositionInput(bool click,bool escape)
    {
        if(_screen==Screen.InactiveList)
        {
            if(escape || click && InactiveBack.Contains(_pointer)){_screen=Screen.WorldStatus;return;}
            if(!click)return;
            var rulers=Enumerable.Range(0,_setup.ActiveCount).Where(r=>!_setup.Relations.Powered[r]).ToArray();
            for(int i=0;i<rulers.Length;i++)if(InsertBatteryButton(i).Contains(_pointer))
            {
                var captive=_setup.Relations.InsertBattery(_setup,rulers[i]);
                _operations.Write("battery_inserted",new{captive});_dispositionReturnList=true;OpenDisposition();return;
            }
            return;
        }
        if(!click)return;
        var pending=_setup.Relations.Pending[0];
        for(int i=0;i<3;i++)if(DispositionButton(i).Contains(_pointer))
        {
            var choice=(DefeatedChoice)i;
            bool accepted=_setup.Relations.Choose(_setup,pending,choice);
            _operations.Write("conqueror_disposition",new{pending,choice=choice.ToString(),accepted});
            if(!accepted)return;
            _world.ShowSetup(_setup,true);
            if(_setup.Relations.Pending.Count>0)return;
            _screen=_dispositionReturnList?Screen.InactiveList:Screen.WorldStatus;_dispositionReturnList=false;
            return;
        }
    }
    private void DrawDispositionUi()
    {
        var captive=_setup.Relations.Pending[0];bool accepts=_setup.Relations.Accepts(_setup,captive.Ruler);
        _ui.Text("征服者の処遇",new(54,30),1f,Cream);
        _ui.Center(_setup.Nodes.Label(_setup.Nodes.All[captive.Node]),new(160,110,1600,45),.58f,Cream);
        for(int side=0;side<2;side++)
        {
            int ruler=side==0?captive.Victor:captive.Ruler,x=side==0?360:1080;
            _ui.Box(new(x-6,200,492,506),_setup.OwnerColor(ruler));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(x,206,480,494));
            _ui.Center((side==0?"勝者　":"敗れた征服者　")+_setup.ConquerorNames[ruler],new(x-80,720,640,48),.65f,Cream);
        }
        _ui.Center(_setup.Relations.Message,new(200,790,1520,44),.54f,new(255,196,112));
        string[] choices={accepts?"手下にする":"手下にする（拒否）","逃がす","電池を引き抜く"};
        for(int i=0;i<3;i++)_ui.Button(DispositionButton(i),choices[i],i==0 && !accepts?Muted:Accent,.65f);
        _ui.Center("逃がす：その節点に留まる / 電池を引き抜く：所有する節点を未征服にする",new(160,970,1600,48),.48f,Cream);
    }
    private void DrawInactiveUi()
    {
        _ui.Text("電池を引き抜いた征服者一覧",new(54,30),.85f,Cream);
        var rulers=Enumerable.Range(0,_setup.ActiveCount).Where(r=>!_setup.Relations.Powered[r]).ToArray();
        if(rulers.Length==0)_ui.Center("電池を引き抜いた征服者はいない",new(200,400,1520,80),.8f,Cream);
        for(int i=0;i<rulers.Length;i++)
        {
            int ruler=rulers[i];var r=InactiveCard(i);_ui.Box(r,new(27,49,62));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(r.X+8,r.Y+8,130,160),false);
            _ui.Center(_setup.ConquerorNames[ruler],new(r.X+145,r.Y+14,190,70),Math.Min(.48f,185/_font.MeasureString(_setup.ConquerorNames[ruler]).X),Cream);
            _ui.Button(InsertBatteryButton(i),"電池を入れる",Accent,.49f);
            _ui.Text("Node "+(_setup.Nodes.Current(ruler).Id+1),new(r.X+10,r.Bottom-27),.38f,Cream);
        }
        _ui.Button(InactiveBack,"ゲームへ戻る",Muted,.58f);
    }
}
