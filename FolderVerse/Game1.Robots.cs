namespace FolderVerse;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

public partial class Game1
{
    private static readonly Rectangle FrontCaptainCapCrop=new(528,304,473,434);
    private static Rectangle RobotCard(int index)=>new(54+index%6*149,220+index/6*153,137,141);
    private static string PartsLabel(RobotParts parts)=>((parts&RobotParts.Head)!=0?"頭":"")+((parts&RobotParts.Body)!=0?"胴":"")+((parts&RobotParts.Legs)!=0?"脚":"");
    private static string IntegratedLabel(RobotParts parts)=>parts==(RobotParts.Head|RobotParts.Body)?"頭／胴一体型":parts==(RobotParts.Body|RobotParts.Legs)?"胴／脚一体型":"";
    private static string PartsLabel(Robot robot)=>robot.RecoveredRaw is {} raw?$"スクラップ {new WorkProgress(raw.Units,9)} 格納庫":(robot.IsDismantling?"解体の仕掛品 / ":robot.IsWorkpiece?"製造の仕掛品 / ":"")+(robot.IntegratedParts==0?PartsLabel(robot.Parts):IntegratedLabel(robot.IntegratedParts)+((robot.Parts&~robot.IntegratedParts)!=0?"＋"+PartsLabel(robot.Parts&~robot.IntegratedParts):""));
    private void DrawRobotGlyph(Rectangle r,int owner,RobotParts parts,bool captain=false,float opacity=1,bool whiteOutline=false,bool damaged=false,bool cheap=false,RobotParts integrated=0,bool premium=false)
        =>DrawRobotGlyph(r,new Robot(0,owner,parts,captain?RobotRole.Captain:RobotRole.Soldier){IntegratedParts=integrated,DamagedParts=damaged?parts:0,CheapParts=cheap?parts:0,PremiumParts=premium?parts:0},whiteOutline,opacity);
    private void DrawRobotGlyph(Rectangle r,Robot robot,bool whiteOutline=false,float opacity=1,Func<RobotParts,float> partOpacity=null)
        => DrawRobotGlyphCore(r, robot, whiteOutline, opacity, partOpacity, showIntegratedMaterial: false);

    private void DrawRobotGlyphCore(Rectangle r,Robot robot,bool whiteOutline,float opacity,Func<RobotParts,float> partOpacity,bool showIntegratedMaterial,bool allowCaptain=false,bool captainCapOverflow=false)
    {
        if(robot.RecoveredRaw!=null){DrawScrapArt(r,opacity);return;}if(robot.IsDismantling)opacity*=.6f; // Outline only the exterior, including for translucent reservations and delivery ghosts.
        var shapes=new System.Collections.Generic.List<(Rectangle Bounds,RobotParts Part,bool Face)>();
        void Shape(Rectangle bounds,RobotParts part,bool face=false){if((robot.Parts&part)!=0)shapes.Add((bounds,part,face));}
        bool headBody=(robot.IntegratedParts&robot.Parts)==(RobotParts.Head|RobotParts.Body);
        bool bodyLegs=(robot.IntegratedParts&robot.Parts)==(RobotParts.Body|RobotParts.Legs);
        bool artHead=HasRobotHeadArt(r,robot,allowCaptain,showIntegratedMaterial);
        var torsoArt = FindRobotBodyArt(r, robot, RobotParts.Body, allowCaptain,showIntegratedMaterial);
        var legsArt = FindRobotBodyArt(r, robot, RobotParts.Legs, allowCaptain,showIntegratedMaterial);
        var cap=artHead && robot.Role==RobotRole.Captain
            ?(_captainCapTexture??=LoadBattleToyTexture("captain-cap")):null;
        Rectangle capBounds=default;
        if(cap!=null && captainCapOverflow){
            // Hangar hit regions stay fixed; only this decorative cap extends above them.
            capBounds=HangarCaptainCapBounds(r);
        }else if(cap!=null){
            int capHeight=r.Height/4;
            int capWidth=(r.Height-capHeight)*12/25;
            int overlap=Math.Max(1,r.Height/16);
            capBounds=new(r.Center.X-capWidth/2,r.Y+overlap,capWidth,capHeight);
            r=new(r.X,r.Y+capHeight,r.Width,r.Height-capHeight);
        }
        bool completeArt = artHead && torsoArt != null && legsArt != null;
        bool HasArt(RobotParts part) => part switch
        {
            RobotParts.Head => artHead,
            RobotParts.Body => torsoArt != null,
            _ => legsArt != null
        };
        int bodyTop=headBody?r.Height/4:r.Height/3,bodyBottom=r.Height/3+r.Height/3;
        int legsTop=bodyLegs?bodyBottom:r.Height*3/4,legsBottom=bodyLegs?r.Height:r.Height*3/4+r.Height/4;
        Shape(new(r.X+r.Width/4,r.Y,r.Width/2,r.Height/4),RobotParts.Head,true);
        Shape(new(r.X+r.Width/5+(robot.IsCheap(RobotParts.Head)?r.Width/10:0),r.Y,r.Width*(robot.IsCheap(RobotParts.Head)?2:3)/5,robot.Role==RobotRole.Captain?r.Height/7:r.Height/12),RobotParts.Head);
        Shape(new(r.X+r.Width/6,r.Y+bodyTop,r.Width*2/3,bodyBottom-bodyTop),RobotParts.Body);
        Shape(new(r.X+r.Width/6,r.Y+legsTop,r.Width/4,legsBottom-legsTop),RobotParts.Legs);
        Shape(new(r.X+r.Width*7/12,r.Y+legsTop,r.Width/4,legsBottom-legsTop),RobotParts.Legs);
        float Alpha(RobotParts part)=>opacity*(partOpacity?.Invoke(part)??1)*(robot.Work(part)!=null?.55f:1);
        if (completeArt)
            DrawCompleteRobotArt(r, robot, Alpha);
        else
        {
            var joint=new Color(105,119,125);
            if((robot.Parts&(RobotParts.Head|RobotParts.Body))==(RobotParts.Head|RobotParts.Body) && bodyTop>r.Height/4)
                _ui.Box(new(r.X+r.Width*5/12,r.Y+r.Height/4,Math.Max(1,r.Width/6),bodyTop-r.Height/4+2),joint*Math.Min(Alpha(RobotParts.Head),Alpha(RobotParts.Body)));
            if((robot.Parts&(RobotParts.Body|RobotParts.Legs))==(RobotParts.Body|RobotParts.Legs) && legsTop>bodyBottom){
                float alpha=Math.Min(Alpha(RobotParts.Body),Alpha(RobotParts.Legs));
                _ui.Box(new(r.X+r.Width*5/12,r.Y+bodyBottom,Math.Max(1,r.Width/6),legsTop-bodyBottom),joint*alpha);
                if (legsArt == null)
                    _ui.Box(new(r.X+r.Width/6,r.Y+legsTop-2,r.Width*2/3,Math.Max(2,r.Height/45)),joint*alpha);
            }
            if(whiteOutline)foreach(var shape in shapes){
                if(HasArt(shape.Part))continue;
                if(robot.IsCheap(shape.Part) || robot.IsPremium(shape.Part)){
                    for(int y=shape.Bounds.Top;y<shape.Bounds.Bottom;y++){
                        var row=RobotPartSilhouette.Row(shape.Bounds,y,robot.IsCheap(shape.Part),robot.IsPremium(shape.Part));row.Inflate(1,1);
                        _ui.Box(row,ControlEdge*Alpha(shape.Part));
                    }
                    continue;
                }
                var bounds=shape.Bounds;bounds.Inflate(2,2);
                var edges=new System.Collections.Generic.List<Rectangle>{bounds};
                foreach(var filled in shapes){
                    var next=new System.Collections.Generic.List<Rectangle>();
                    foreach(var edge in edges){
                        var cut=Rectangle.Intersect(edge,filled.Bounds);
                        if(cut.Width<=0 || cut.Height<=0){next.Add(edge);continue;}
                        if(edge.Top<cut.Top)next.Add(new(edge.X,edge.Y,edge.Width,cut.Top-edge.Top));
                        if(cut.Bottom<edge.Bottom)next.Add(new(edge.X,cut.Bottom,edge.Width,edge.Bottom-cut.Bottom));
                        if(edge.Left<cut.Left)next.Add(new(edge.X,cut.Top,cut.Left-edge.Left,cut.Height));
                        if(cut.Right<edge.Right)next.Add(new(cut.Right,cut.Top,edge.Right-cut.Right,cut.Height));
                    }
                    edges=next;
                }
                foreach(var edge in edges)_ui.Box(edge,ControlEdge*Alpha(shape.Part));
            }
            foreach(var shape in shapes){
                if(artHead && shape.Part==RobotParts.Head){if(shape.Face)DrawRobotHeadArt(r,robot,Alpha(RobotParts.Head));continue;}
                if (HasArt(shape.Part)) continue;
                DrawRobotPartFill(shape.Bounds,r,shape.Bounds,robot,shape.Part,robot.Origin(shape.Part),
                    shape.Face?Cream:_setup.OwnerColor(robot.Origin(shape.Part)),Alpha(shape.Part));
            }
            var emblemPart=(robot.Parts&RobotParts.Body)!=0?RobotParts.Body:(robot.Parts&RobotParts.Head)!=0?RobotParts.Head:RobotParts.Legs;
            int emblemSize=Math.Max(1,Math.Min(r.Width/3,r.Height/7));
            int emblemY=emblemPart==RobotParts.Body?r.Y+(bodyTop+bodyBottom-emblemSize)/2:emblemPart==RobotParts.Head?r.Y+(r.Height/4-emblemSize)/2:r.Y+legsTop;
            int emblemX=r.Center.X;
            if(emblemPart==RobotParts.Legs){emblemSize=Math.Max(1,Math.Min(emblemSize,r.Width/6));emblemX=r.X+r.Width*7/24;emblemY=r.Y+(legsTop+legsBottom-emblemSize)/2;}
            if (torsoArt != null)
                DrawRobotBodyPart(torsoArt, new(r.X, r.Y + bodyTop, r.Width, bodyBottom - bodyTop), robot, RobotParts.Body, Alpha(RobotParts.Body));
            if (legsArt != null)
                DrawRobotBodyPart(legsArt, new(r.X, r.Y + legsTop, r.Width, legsBottom - legsTop), robot, RobotParts.Legs, Alpha(RobotParts.Legs));
            if(!HasArt(emblemPart))DrawCountryEmblem(new(emblemX-emblemSize/2,emblemY,emblemSize,emblemSize),robot.Origin(emblemPart),Alpha(emblemPart));
        }
        if(cap!=null)_spriteBatch.Draw(cap,capBounds,FrontCaptainCapCrop,Color.White*Alpha(RobotParts.Head));
        foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})if((robot.Parts&part)!=0){
            float alpha=Alpha(part);bool damaged=robot.IsDamaged(part);
            if(damaged && !HasArt(part)){
                var metal=new Color(185,180,164)*alpha;
                int y=part==RobotParts.Head?r.Y+r.Height/12:part==RobotParts.Body?r.Y+r.Height/3:r.Y+r.Height*3/4;
                _ui.Box(new(r.X+r.Width/4,y+2,Math.Max(2,r.Width/5),Math.Max(2,r.Height/28)),metal);
                _ui.Box(new(r.X+r.Width/2,y+Math.Max(4,r.Height/12),Math.Max(2,r.Width/7),Math.Max(2,r.Height/28)),metal);
            }
        }
        int workOwner=WorkReservationOwner(robot);
        if ((workOwner >= 0 || robot.Disposal || robot.LongDeliveryNumber.HasValue) && (robot.Parts & RobotParts.Head) != 0)
            RobotWorkArt.DrawWindupKey(_ui, r, Alpha(RobotParts.Head),
                robot.ShoeOwner >= 0 ? DefenseShoeProgress(robot) : WorkShoeProgress(robot));
        DrawWorkReservationEffect(r,robot,opacity);
        if(robot.IsDismantling){
            int total=RobotWorkshop.DisposalTurns(robot.Parts),done=Math.Min(total,robot.DismantlingAge.Value);
            int h=Math.Max(3,r.Height/16);var bar=new Rectangle(r.X,r.Bottom-h,r.Width,h);
            _ui.Box(bar,new Color(39,43,49)*opacity);
            _ui.Box(new(bar.X,bar.Y,Math.Max(1,bar.Width*done/total),bar.Height),new Color(244,139,66)*opacity);
            if(r.Width>=96)_ui.Center("解体",new(r.X,r.Bottom-h-24,r.Width,24),.28f,Cream*opacity);
            return;
        }
        foreach(var part in RobotMaterials.Parts.Where(p=>(robot.Parts&p)!=0 && robot.Work(p)!=null)){
            int y=part==RobotParts.Head?r.Y:part==RobotParts.Body?r.Y+r.Height/3:r.Y+r.Height*2/3;
            if(r.Width<96){
                var progress=robot.Work(part).Progress;
                int height=Math.Clamp(r.Height/34,2,4);
                var track=new Rectangle(r.X,y+r.Height/3-height,Math.Max(1,r.Width),height);
                _ui.Box(track,new Color(38,45,52)*opacity);
                int filled=(int)(track.Width*progress.Numerator/progress.Denominator);
                if(filled>0)_ui.Box(new(track.X,track.Y,filled,track.Height),new Color(255,193,76)*opacity);
                continue;
            }
            var work=robot.Work(part);var band=new Rectangle(r.X,y+r.Height/6,Math.Max(1,r.Width),Math.Max(8,r.Height/9));
            _ui.Box(band,new Color(38,45,52)*opacity);
            for(int x=band.X;x<band.Right;x+=8)_ui.Box(new(x,band.Y,Math.Min(3,band.Right-x),Math.Max(1,band.Height/4)),new Color(255,193,76)*opacity);
            DrawMixedNumber((int)work.Progress.Numerator,(int)work.Progress.Denominator,band,Cream*opacity,Math.Min(.28f,r.Width/180f));
        }
    }
    private void DrawRobotPartFill(Rectangle area,Rectangle robotArea,Rectangle damageArea,Robot robot,RobotParts part,int owner,Color primary,float opacity)
    {
        int pattern=_setup.CountryPattern(owner),unit=Math.Max(1,robotArea.Width/16);
        var secondary=pattern==0?primary:WorldSetup.Colors[_setup.CountrySecondaryColor(owner)];
        for(int y=area.Top;y<area.Bottom;y++){
            var row=RobotPartSilhouette.Row(area,y,robot.IsCheap(part),robot.IsPremium(part));
            int start=row.Left;Color prior=default;
            for(int x=row.Left;x<=row.Right;x++){
                Color ink=default;
                if(x<row.Right){
                    ink=pattern!=0 && WorldSetup.CountryPatternSecondary(pattern,(x-robotArea.X)/unit,(y-robotArea.Y)/unit)?secondary:primary;
                    if(robot.IsPremium(part) && (y-area.Top<Math.Max(1,area.Height/12) || area.Bottom-y<=Math.Max(1,area.Height/12)))
                        ink=Color.Lerp(new Color(255,215,100),primary,.2f);
                    if(robot.IsDamaged(part))ink=RobotDamageArt.Tint(damageArea,x,y,ink);
                    ink*=opacity;
                }
                if(x==row.Left)prior=ink;
                if(x==row.Right || ink!=prior){_ui.Box(new(start,y,x-start,1),prior);start=x;prior=ink;}
            }
        }
    }
}
