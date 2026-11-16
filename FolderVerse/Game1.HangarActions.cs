namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private const int HangarActionSize=68;
    private static Rectangle HangarActionBounds(int index)=>new(84+index*79,862,HangarActionSize,HangarActionSize);
    private static readonly Rectangle HangarAssemblyButton=HangarActionBounds(0),
        HangarReturnHomeButton=HangarActionBounds(6),HangarAbandonButton=HangarActionBounds(9);
    private static readonly int[] ReservationActions={5,3,1,4,2,7,8};
    private static Rectangle HangarReservationButton(int index)=>HangarActionBounds(ReservationActions[index]);
    private static readonly string[] HangarActionNames={
        "組み立て・分解","製造作業員予約","採取作業員予約","守備専用予約","迎撃専用予約",
        "廃棄予約","国へ帰らせる予約","倉庫管理人予約","倉庫管理人交代","放棄","放棄品置き場"
    };
    private Texture2D _hangarActionArt;

    private bool CanUseHangarAbandon=>DomesticOperationsAllowed && HangarPlanningNode==_populationNode &&
        _setup.Nodes.All[_populationNode].Owner==_setup.PlayerSlot;

    private bool CanAbandonFromHangar(Robot robot,RobotParts parts)=>CanUseHangarAbandon &&
        robot.Owner==_setup.PlayerSlot && parts!=0 && (robot.Parts&parts)==parts &&
        !_setup.Campaign.RobotBattles.LockedNode(_populationNode) &&
        !Enumerable.Range(0,3).Select(HangarPart).Any(part=>(parts&part)!=0 && _setup.Robots.PartLocked(_populationNode,robot,part)) &&
        (parts!=robot.Parts || !_setup.Robots.DirectDelivery.Orders.Any(o=>o.Target==_populationNode && o.TargetRobotId==robot.Id));

    private bool CanChooseWarehouseSwap(Robot robot)=>robot!=null && DomesticOperationsAllowed &&
        CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && !_setup.Campaign.RobotBattles.LockedNode(_populationNode) &&
        (robot.WarehouseKeeper || _warehouseSwapKeeper.HasValue && RobotPartPlanning.CanUseWarehouseSuccessor(_setup,PlanningStock,HangarPlanningNode,robot));

    private void DrawHangarActions(bool manage,bool assemblyMode)
    {
        if(HangarAssemblyAvailable)DrawHangarAction(HangarAssemblyButton,0,DomesticOperationsAllowed && CanOpenHangar,assemblyMode);
        if(!VisitingForeignHangar)
        {
            for(int i=0;i<ReservationActions.Length;i++)
                if(ShowsHangarReservation(i))
                    DrawHangarAction(HangarReservationButton(i),ReservationActions[i],manage,_hangarReservation==(HangarReservation)(i+1));
            DrawHangarAction(HangarReturnHomeButton,6,manage,_hangarReturnHomeMode);
        }
        if(HangarPlanningNode==_populationNode && _setup.Nodes.All[_populationNode].Owner==_setup.PlayerSlot)
            DrawHangarAction(HangarAbandonButton,9,CanUseHangarAbandon,_hangarAbandonMode);
    }

    private void DrawHangarAction(Rectangle bounds,int index,bool enabled,bool active=false)
    {
        if(_hangarActionArt==null)
        {
            using var stream=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"Content","Images","Robots","hangar-actions-v001.png"));
            _hangarActionArt=Texture2D.FromStream(GraphicsDevice,stream);
        }
        var frame=bounds;frame.Inflate(2,2);
        if(enabled && (active || bounds.Contains(_pointer)))
            PrimaryRoundBox(frame,active?new Color(255,223,148):Cream*.65f,12);
        _spriteBatch.Draw(_hangarActionArt,bounds,new Rectangle(index*96,0,96,96),
            enabled?Color.White:new Color(122,131,139));
        string label=HangarActionNames[index];
        _ui.Center(label,new(bounds.X-4,bounds.Bottom+3,bounds.Width+8,17),
            Math.Min(.30f,(bounds.Width+8)/Math.Max(1,_font.MeasureString(label).X)),enabled?Cream:Cream*.55f);
    }

    private void DrawHangarCandidate(Rectangle bounds)
    {
        if(bounds.Width<8 || bounds.Height<8)return;
        const int segments=8;
        var mint=new Color(117,255,220);
        var gold=new Color(255,231,150);
        for(int side=0;side<4;side++)
        {
            bool horizontal=side%2==0;
            int size=horizontal?bounds.Width:bounds.Height;
            for(int i=0;i<segments;i++)
            {
                int from=i*size/segments,to=(i+1)*size/segments;
                float wave=.5f+.5f*MathF.Cos((side+i/(float)segments)*MathHelper.PiOver2-_animationTime*2.2f);
                var color=Color.Lerp(mint,gold,wave);
                int x=side==2?bounds.Right-to:horizontal?bounds.X+from:side==1?bounds.Right-2:bounds.X;
                int y=side==3?bounds.Bottom-to:!horizontal?bounds.Y+from:side==0?bounds.Y:bounds.Bottom-2;
                var halo=side switch {0=>new Rectangle(x,y+2,to-from,3),1=>new Rectangle(x-3,y,3,to-from),
                    2=>new Rectangle(x,y-3,to-from,3),_=>new Rectangle(x+2,y,3,to-from)};
                _ui.Box(halo,color*(.12f+.24f*wave*wave));
                _ui.Box(new(x,y,horizontal?to-from:2,horizontal?2:to-from),color*MathHelper.Lerp(.36f,.95f,wave*wave));
            }
        }
    }
}
