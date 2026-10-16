namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
public partial class Game1{
 private int _withdrawalEncounter=-1;
 private static readonly Rectangle WithdrawalButton=new(84,350,760,72);
 private RobotEncounter SelectedWithdrawal=>_setup.Campaign.RobotBattles.Encounters.FirstOrDefault(e=>e.Id==_withdrawalEncounter && !e.Finished);
 private int[] WithdrawalArmies(RobotEncounter e)=>e.Battles.SelectMany(b=>b.Units.Where(u=>u.Active).Concat(b.Armies.SelectMany(a=>a.Waiting))).Select(u=>u.Owner).Distinct().Where(CanPlanRuler).ToArray();
 private Rectangle ContinuationBadge(RobotEncounter e){
  if(!OperationPosition(e.Target,out var at))return Rectangle.Empty;
  if(e.Kind==BattleKind.Edge){if(!OperationPosition(e.Source,out var from))return Rectangle.Empty;at=(at+from)/2;}
  return new((int)at.X-50,(int)at.Y-32,100,36);
 }
 private void DrawContinuationBadges(){
  if(!_movementOpen || !_warPlanning)return;
  foreach(var e in _setup.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished && WithdrawalArmies(e).Length>0)){
   var r=ContinuationBadge(e);if(r.IsEmpty)continue;_ui.Button(r,"継戦 "+e.Id,e.Id==_withdrawalEncounter?Accent:Muted,.4f);
  }
 }
 private bool SelectContinuation(Point p){
  if(!_movementOpen || !_warPlanning)return false;
  foreach(var e in _setup.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished && WithdrawalArmies(e).Length>0))
   if(ContinuationBadge(e).Contains(p)){_withdrawalEncounter=e.Id;_selectedMoveNode=-1;return true;}
  return false;
 }
 private bool WithdrawalClick(Point p){
  var e=SelectedWithdrawal;if(e==null || !WithdrawalButton.Contains(p))return false;
  var owners=WithdrawalArmies(e);bool cancel=owners.All(e.WithdrawalOwners.Contains);
  foreach(int owner in owners)_setup.Campaign.RobotBattles.RequestWithdrawal(_setup,e.Id,owner,!cancel);
  return true;
 }
 private void DrawWithdrawal(){
  var e=SelectedWithdrawal;if(e==null)return;
  _ui.Text($"継戦 {e.Id} / {(e.Kind==BattleKind.Edge?"辺戦":"節戦")}",new(84,245),.65f,Cream);
  bool reserved=WithdrawalArmies(e).All(e.WithdrawalOwners.Contains);
  _ui.Button(WithdrawalButton,reserved?"引き揚げ予約を取り消す":"引き揚げ",Accent,.65f);
  _ui.Text("自国・手下の部隊は次の戦闘開始から退却します",new(84,455),.48f,Cream);
  _ui.Text("退却中も攻撃を受けます",new(84,505),.55f,Cream);
 }
}
