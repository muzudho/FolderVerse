# 製造の段階消費と開始条件

2026-11-21 利用者承認の４項目を実装。

- 原料は16分率の整数単位。各産業tickの累計必要量を `floor(必要量 × 次の進捗 / 実効工期)` とし、既消費との差を消費する。完成時にレシピ必要量と一致する。
- １工程分が揃えば見切り発車する。不足した工程では進捗も消費も停止。混成原料は工程ごとに全種を確認してから消費する。未消費の在庫に製造専用ロックはかけない。
- 原料消費の記録はレーンに保存する。プラン変更・停止では在庫へ返却せず記録を初期化。単なる一時停止、出荷待機、保存復元では加工済み量を維持する。
- 完成後の入荷枠待ちでは追加消費しない。入荷成立まで完成状態を保持する。
- UIは［繰り返す］。新規レーンはOFF、指定個数（×1～9）を完了すると停止。既存保存の繰り返し値は維持する。OFF切替は現在の指定個数を終えて停止する。生産完了トリガーは休暇終了を待たない。
- ［今すぐ開始］または［レーンn 生産完了後に開始］を選択。対象レーンの現在の予定個数が完成したら、次の産業tickから開始する。繰り返す元でも次の１バッチ完了を使う。既に完了済みなら次tickで開始する。
- 依存は作業員IDへ紐付け、番号変化で別人を参照しない。自己参照・循環を拒否。参照元が消えれば待機を続け、今すぐ開始への変更で解除できる。採取レーンは完了トリガーの対象外。
- 追加任命予定のレーンは、任命確定後に開始条件を設定する。既存レーンの設定は工場画面で変更できる。
- 工場と産業フェーズに消費済み／残り必要原料とバーを表示。製造候補画面に原料が戻らない注意を表示。CPUの既選択レシピに対する不足判定も残必要量を参照する。

## 保存・検証

新しい消費履歴・完了回数・依存情報を保存。古いMaterials保存は既消費0・依存なし・完了カウンター0として読み込む。進行中の旧レーンは最初の加工tickで現在進捗までの累計原料を確保する（旧版は途中消費していないため）。

NAS `archives/diagnostics/production-progress-2026-11-21` に25項目のモデル回帰、工場画面と開始条件メニューの描画・実クリック、旧Materials400停止保存の読込、10ターンheadless機能スモークを保存。ビルド警告・エラー0。スモークは収束比較ではない。実ゲームと元保存は操作しない。

## 次の仕様：仕掛品（WIP）

利用者は名称［仕掛品］と、完成予定と同じパーツ占有数（1～3）を承認した。最初にスケルトンを作る世界観。廃品として残す、回収して続きを製造する案がある。互いに重ならない部位の仕掛品の組み合わせ案も相談中。

仕掛品の格納・回収・再開と加工合成は実装済み。最新仕様・操作・検証範囲は [workpieces.md](workpieces.md) を参照。以下の過去記録の未実装記述より最新仕様を優先する。

2026-11-21 仕掛具合の有理数を実装。WorkProgressは整数long分子/分母を最大公約数で約分、0/1/補数と保存復元を定義し小数丸めを保存しない。現行レーンの工数も整数の既存３分率のまま、WorkDone/WorkRemainingを追加。工場と産業の共通表示は仕掛具合・残工数、原料の消費済み/残必要量を既存DrawMixedNumberの横括線・帯分数で表示し約分。フォント「掛」追加。15153有理数/保存項目PASS、２UI状態の描画・目視と開始条件実クリックPASS、製品/ハーネスビルド警告エラー0。現在はレーン全体の共通進捗であり、部位ごとに独立した仕掛品オブジェクト/回収/再開/合成はまだ未実装。利用者は部位別の仕掛具合、残工数と残原料の別管理を相談。後続は部位の製造プラン/工数と原料消費履歴を保存し、組替えで工数や原料を増やさず、完成予定占有数を守る。NAS archives/diagnostics/work-fraction-2026-11-21、再生成tmp/delete-after-2026-11-21/work-fraction、tests/WorkProgress。元ゲーム/元保存/公開物変更なし。

2026-11-21 04:59 JST 再開仕掛品が加工完了後に無効な入荷先で待機→入荷先変更あり/なし→製造しないへ停止、を隔離fixtureで検証。完成現物が廃品として一度だけ残り、原料再消費/架空入荷/二重廃品化なし。停止のみではIntakeFailureの古い入荷先エラーが残る表示不整合を再現。ResetRawProgressでIntakeFailureを消す最小修正を実施し、両順序で失敗理由クリアを含む23項目PASS。無効入荷先はint.MaxValueの診断条件で、自然な配送先変更や実作業員解除の全組合せではない。製造停止（プラン変更）を検証し、今回のfixture自体は実作業員解除予約ではない。後者は先のworkpieces367項目で別途確認。製品/ハーネス警告エラー0、DLL 986406567A08C4B12911FA6B32B6F57A96787D21B98DFF30C88E0CD819D38956。元ゲーム/元保存/公開物無操作、コミットなし。NAS archives/raw-materials/wip-retarget-stop-2026-11-21-0459、再生成tmp/delete-after-2026-11-21/raw-materials/wip-retarget-stop-0459。

2026-11-21 05:41 JST 固定986406567A08C4B12911FA6B32B6F57A96787D21B98DFF30C88E0CD819D38956で再開仕掛品の次工程原料不足を隔離回帰。PP完全体/頭胴一体・工期12の半加工状態から原料在庫を空にし、累計floorで追加費用0の次工程は進行、その次の各部位1ずつ計PP3が必要な工程では加工/既払履歴とも不変、材料待ち表示あり。保存復元でも状態同値、PP3を補給すると１工程だけ進み3を正確に消費。22assertions通過、ハーネス警告エラー0、製品コード変更なし。単一素材/頭胴結合の診断fixtureであり５素材同時不足や実UI/自然補給全経路の証明ではない。NAS archives/raw-materials/wip-step-shortage-2026-11-21-0541、再生成tmp/delete-after-2026-11-21/raw-materials/wip-step-shortage-0541。元ゲーム/元保存/公開物無操作。次は必要なら異素材の非同期部位進捗と原子的不足、または実作業員解除後の孤立待機導線へ。解体レーンは相談段階として未変更。

2026-11-21 06:43 JST 混成仕掛品（頭Cardboard/胴PP/脚SoftVinyl、頭胴一体）の再開工程でMaple/Vinyl/PPを１種ずつ不足させる隔離回帰33項目通過。実加工由来の半完成品を回収/再開予約/保存/確定し、複数原料が必要な次tickを実StepCostで選択。不足時は全ての部位進捗・既払履歴・他原料在庫が不変、材料待ち判定あり。保存復元後に当該工程の全必要量だけ補給すると１工程再開して正確に全量消費。固定DLL986406567A08C4B12911FA6B32B6F57A96787D21B98DFF30C88E0CD819D38956、最終ハーネス警告エラー0/exit0。初回ハーネス変数paid競合をledgerへ訂正。１混成/結合のfixtureであり５原料全レシピや非同期進捗全組合せではない。製品変更なし。NAS archives/raw-materials/wip-mixed-shortage-2026-11-21-0643、再生成tmp/delete-after-2026-11-21/raw-materials/wip-mixed-shortage-0643。元保存/実ゲーム/公開物無操作。次は必要なら部位ごとに工期が異なる仕掛品の再開完了/入荷待ちへ。

2026-11-21 07:42 JST 部位ごとに工期が違う仕掛品再開を隔離回帰17項目通過。PP完全体/頭胴一体で頭Done2/Total5/Paid3、胴4/8/4、脚6/10/4の人工状態を格納→再開予約→確定。WorkRemaining4、各部位の別進捗、２tick後保存復元、３tick目に頭だけ完成、４tick目に全完成し入荷キュー１個、以後重複なし、追加PP13の正確消費を確認。頭の早期完成後に再消費しない。固定DLL986406567A08C4B12911FA6B32B6F57A96787D21B98DFF30C88E0CD819D38956、ハーネス警告エラー0/exit0。人工的な部位別工期のfixtureで自然な異工期合成操作/全素材/全用途の速度補正/満杯待ちは別。NAS archives/raw-materials/wip-asynchronous-2026-11-21-0742、再生成tmp/delete-after-2026-11-21/raw-materials/wip-asynchronous-0742。製品/元保存/実ゲーム/公開物変更なし。次は必要なら採取/製造重点の工期補正と仕掛品再開の整合を仕様から監査する。

2026-11-21 08:40 JST 仕掛品再開と地点用途補正を３用途で実測。PP頭胴結合の各部位15/30・既払4の合成状態はGeneral/Manufacturing/Harvestの全て残り15tickで完成する一方、通常レシピ全工期は30/27/33。現行AdvanceWorkpieceは保存された部位Totalに沿い１ずつ加工し再開先用途の補正を再適用しない。CaptureWorkpieceは既に元用途補正済み_lastEffectiveProductionPeriodをTotalへ保存するため、単純に再開で±10%すると同じ用途で二重補正する恐れがある。用途変更後に残工数をどう扱うかは未確定として記録し、コスト/ルールの推測変更をしない。次は同用途で中断→再開の連続製造同値を確認し、用途変更時は基準工数/元補正情報を保持する案を検討する。15assertions/警告エラー0/exit0、固定986406567A08C4B12911FA6B32B6F57A96787D21B98DFF30C88E0CD819D38956、製品/元保存/実ゲーム無変更。NAS archives/raw-materials/wip-purpose-audit-2026-11-21-0840、再生成tmp/delete-after-2026-11-21/raw-materials/wip-purpose-audit-0840。

2026-11-21 09:43 JST 実製造からの中断→廃品回収→保存→再開予約→確定を同一地点用途３種で回帰51項目PASS。PP完全体/頭胴結合・基礎工期12、Generalは６加工+再開６=12、Manufacturingは６+５=11、Harvestは６+７=13で通常EffectiveProductionPeriodと一致。全条件で既払+追加消費=PP24、入荷１個、保存/工数履歴維持。二重用途補正を入れない現状が同用途では正しいことを確認。用途変更や他工場移送後の補正の扱いは依然未確定で、今回変更せず。固定CPU/モデルDLL986406567A08C4B12911FA6B32B6F57A96787D21B98DFF30C88E0CD819D38956（描画のみF990は別）、ハーネス警告エラー0/exit0。NAS archives/raw-materials/wip-same-purpose-2026-11-21-0943、再生成tmp/delete-after-2026-11-21/raw-materials/wip-same-purpose-0943。製品/元保存/実ゲーム/公開物無変更。次はこの用途条件の反復をせず、仕掛品の工場移送/受取容量か既存入荷先変更と実作業員解除の未確認境界へ。

2026-11-21 10:49 JST 実製造作業員の解除予約と入荷先変更を２順序で隔離回帰12項目PASS。PP頭工期12の６加工後、入荷先変更→解除予約→保存→確定、または解除予約→保存→入荷先変更→確定。両方で実作業員FactoryWorker解除/レーン0、元地点の廃品へ仕掛品6/12・既払PP4が一度だけ残り、原料返却/架空入荷/二重廃品化なし。04:59のプラン停止fixtureとは異なる実ReserveRole解除。変更先は実隣接地点で全Owner0の診断配置、UI実クリックや完成品入荷待機の解除とは別。固定モデル/描画DLL F990425D0A2278E5A021924514BE26E493BD4DA1C6ECBC3FC216E972F5A66507、ハーネス警告エラー0/exit0。NAS archives/raw-materials/wip-worker-retarget-2026-11-21-1049、再生成tmp/delete-after-2026-11-21/raw-materials/wip-worker-retarget-1049。製品追加変更/元保存/実ゲーム/公開物操作なし。次はこの２順序を反復せず完成品待機と仕掛品廃品のUI区別か実受取容量の境界へ。

2026-11-21 11:45 JST 完成品出荷待機中の実製造作業員解除と入荷先変更を２順序で隔離検証14項目PASS。PP頭工期12を無効入荷先int.MaxValueで全12加工しWaitingを確認、入荷先変更→解除予約→保存復元→確定、または解除予約→保存復元→入荷先変更→確定。両方でレーン消去/作業員解除、元地点の廃品へ完成頭（IsWorkpiece=false/HeadWork=null）が一度だけ保存され、追加原料消費・返却・架空入荷・再回収重複なし。10:49途中6/12 fixtureと異なる完工境界。ただし満杯実地点/UI操作のテストではなく無効先による出荷拒否。固定F990425D0A2278E5A021924514BE26E493BD4DA1C6ECBC3FC216E972F5A66507、最終ビルド警告エラー0/exit0。NAS archives/raw-materials/completed-worker-release-2026-11-21-1145、再生成tmp/delete-after-2026-11-21/raw-materials/completed-worker-release-1145。最新の解体・スクラップ混在相談は製造挙動と区別し、未確定の提案を実装せず。製品/元保存/実ゲーム/公開物無変更。次はこの拒否条件を反復せず実受取容量またはUIの完成品/製造仕掛品表示を確認。


2026-11-21 12:50 JST 実在する隣接地点の満杯格納庫を用い、完成品出荷待機中の製造員解除＋入荷先変更を２順序で隔離検証20項目PASS/exit0。受取容量１を完全体１体で埋め、PP頭12tick完工後WaitingとIntakeFailure『入荷先の格納枠が不足しています』を確認。入荷先を元地点へ変更→解除予約、または解除予約→保存復元→入荷先変更の両順序で、確定後は完成頭が元地点カゴへ一度だけ残り、レーン消去、原料の返却/追加消費/架空入荷なし、受取側の既存完全体も不変。前11:45の無効先int.MaxValueと違う実容量境界。固定DLL775859D073069A4E069F1188383C45EEC6E3CFF1C188967A2B3F84E4606D619D、旧SetPlacement(3) fixtureなのでScrapRules有効な初期世界の検証ではない。人工完工fixtureで実UIクリック/メッセージ描画までは未検証。製品追加変更/実ゲーム/元保存/公開物操作なし。NAS archives/raw-materials/full-target-worker-release-2026-11-21-1250、再生成tmp/delete-after-2026-11-22/raw-materials/full-target-worker-release-1250。次は同じ２順序を反復せず、完成品待機と製造仕掛品のUI表示区別かScrapRules有効世界の容量共有境界へ。


2026-11-21 13:48 JST 新規通常世界由来のScrapRules有効な容量境界を配布v0.13.0 DLL A6A10CBA0EF44A9DFCA44F2697686A307D6440031A00268B80D600C05A34C73Eで隔離検証10項目PASS/exit0。世界42/cast8/rights492471852/fortune1544938375/2CPU/初期25・25でCompleteLand後、所有と２地点在庫・容量を制御fixture化。受取格納庫１つにスクラップ1/9だけを置き、PP頭12tickが完成しても専用格納庫ルールにより入荷拒否・『入荷先の格納枠が不足しています』を確認。保存復元後３tick待っても追加PP消費なし。実ReserveMapleShipment→ResolveMapleShipmentsでスクラップを元工場へ搬出すると、次のCompleteProductionで完成品１件を予約、ResolveProductionIntakesで頭を受取。原料二重消費なし・スクラップ量/ID維持。空き2/3があってもスクラップとロボットを混載しない仕様の実証。自然な在庫配置やCPUによる搬出選択、全フェーズ通し/UI描画の検証ではない。NAS archives/raw-materials/scrap-production-capacity-2026-11-21-1348、再生成tmp/delete-after-2026-11-22/raw-materials/scrap-production-capacity-1348。公開物/元保存/稼働ゲーム/製品ソース無変更、コミット・プッシュなし。次は必要なら工場の待機理由の表示区別か、スクラップ自動処分と同ターン製造入荷の順序を独立に検証する。


2026-11-21 14:48 JST スクラップ滞在満了と製造入荷の容量境界を配布v0.13.0固定A6A10CBA0EF44A9DFCA44F2697686A307D6440031A00268B80D600C05A34C73Eで隔離検証９項目PASS/exit0。13:48の新規世界由来制御fixtureを再利用し、受取側を最終処分場とし前全体ターン到着のScrapLot1単位を明示設定。PP頭完成待機→保存復元→３tick待機は追加原料消費なし。ProcessFinalDisposalで古いスクラップだけが消え、次のCompleteProduction→ResolveProductionIntakesで完成頭が１件入荷、原料再消費なし。現地の処分場属性/滞在履歴は注入fixtureで自然採取・自然配送の履歴ではない。各モデル処理を順に呼んだ境界検証で、全員内政終了から次産業までの自然フェーズ通しや同じBeginDelivery内の製造再開を証明しない。新着スクラップ・合算cohortの基本滞在検証は既存FinalDisposal108項目と区別。NAS archives/raw-materials/scrap-expiry-production-2026-11-21-1448、再生成tmp/delete-after-2026-11-22/raw-materials/scrap-expiry-production-1448。次は必要なら実BeginDelivery内の既存入荷キューと処分の順序を検証し、単なる完工待機との違いを残す。製品/公開版/実ゲーム/元保存/個人差分無変更、コミット・プッシュなし。


2026-11-21 15:50 JST 実BeginDeliveryで自動処分→既存製品入荷の順序を２条件13項目PASS。配布v0.13.0固定A6A10CBA0EF44A9DFCA44F2697686A307D6440031A00268B80D600C05A34C73E、新規世界42/cast8/rights492471852/fortune1544938375/2CPU/25・25/ScrapRules有効から所有・容量・処分場属性・滞在履歴を制御。PP頭を空き受取先への入荷キューに作成した後、スクラップで１格納庫を占有し保存復元。新着cohortは同じBeginDeliveryでも残り『入荷待機：格納枠不足』、前全体ターンcohortは先に除去され、そのBeginDelivery内で保留頭が１件入荷。再度同ターンBeginDeliveryしても新着の早期処分/完成頭重複/追加PP消費/全体ターン進行なし。前14:48のレーン完成待機とは異なり、既にキューにある製品を検証。初回fixtureは自然初期輸送を残し入荷後の滞在を仮定して失敗（古いscrap0/queue0だが受取robot0）。通常輸送をResetして処分と入荷順序へ隔離し、最終結果を得た。初回の後続配送の詳細は未追跡、製品消失や処分不具合と断定しない。人工cohort/属性のfixtureで自然フェーズ通しの全条件ではない。NAS archives/raw-materials/scrap-intake-phase-order-2026-11-21-1550（初回debug/最終13項目/fixture）、再生成tmp/delete-after-2026-11-22/raw-materials/scrap-intake-phase-order-1550。最終exit0/stderr空、製品/公開版/元保存/実ゲーム/個人差分無変更、コミット・プッシュなし。次はこの順序を再反復せず、必要なら入荷後通常輸送の荷ID系譜を独立に検証する。


2026-11-21 16:43 JST 入荷直後に受取格納庫が空になるケースを、通常輸送を明示したNAS隔離fixtureで追跡し７項目PASS。配布v0.13.0固定A6A10CBA0EF44A9DFCA44F2697686A307D6440031A00268B80D600C05A34C73E、新規世界42/cast8/rights492471852/fortune1544938375/2CPU/25・25/ScrapRules有効由来。前15:50の古いスクラップを処分→保留PP頭入荷のfixtureに、受取2→元0の頭100％通常輸送だけを追加。実BeginDeliveryでscrap0/受取robot0/入荷queue0となるが、LastTransfersに製品ID369/頭/2→0を確認し、実在庫でも地点0の同ID頭を確認。原材料の追加消費なし。この断面は消失ではなく同一配送フェーズ内の後続通常輸送で説明できる。前回自然初期輸送の失敗そのものと同一履歴を再現したものではなく、制御した代表例であり全輸送計画や合体先IDの検証ではない。NAS archives/raw-materials/intake-onward-transport-2026-11-21-1643、再生成tmp/delete-after-2026-11-22/raw-materials/intake-onward-1643。製品/公開物/元保存/実ゲーム/個人差分無変更、コミット・プッシュなし。次は必要な別境界へ進め、入荷地点に無いだけで消失と判定しない。


2026-11-21 17:47 JST スクラップで満杯の入荷待機から入荷先を変える境界11項目PASS。固定v0.13.0 A6A10CBA0EF44A9DFCA44F2697686A307D6440031A00268B80D600C05A34C73E、新規世界42/cast8/rights492471852/fortune1544938375/2CPU/25・25/ScrapRules有効由来の制御fixture。実BeginDeliveryでPP頭が『入荷待機：格納枠不足』となった後、SetIntakeTargetで元の工場へ変更→Status『入荷予約』へ更新→保存復元→実ResolveProductionIntakesで同じ製品IDが新入荷先へ１件到着、旧受取先の新着スクラップは不変、PP追加消費なし。UIはpending[0].Statusをそのまま描くコードでこの理由更新と対応、実画面操作/画素検証はしていない。NAS archives/raw-materials/blocked-intake-retarget-2026-11-21-1747、再生成tmp/delete-after-2026-11-22/raw-materials/blocked-retarget-1747。最終exit0、製品/公開物/実ゲーム/元保存/個人差分無変更、コミット・プッシュなし。次は未検証の具体的条件に進め、この待機変更を再反復しない。


2026-11-21 18:51 JST 原料合算の途中失敗で在庫が減増しないことを５原料30項目PASS。配布v0.13.0固定A6A10CBA0EF44A9DFCA44F2697686A307D6440031A00268B80D600C05A34C73E、１格納庫のモデルfixtureに15unitsの対象原料＋別２原料各16units。対象２units入荷は１unitだけ合算すれば残りが溢れる条件でTryReceiveMaple=false、元３荷record/合計47units不変を確認。対象１unitなら合算だけで受取成功、３荷のまま合計48units。楓/スチレン/軟質ビニール/PP/シリコーンで確認、スクラップ・実配送フェーズ・保存/UIは対象外。HasMapleSpace単独の拒否と、TryReceiveMapleによる合算成功は異なるため、CPU予測はPlanningCopyでTryReceiveMapleを使う方が正確という次の検討根拠。NAS archives/raw-materials/raw-merge-rollback-2026-11-21-1851、再生成tmp/delete-after-2026-11-22/raw-materials/raw-merge-rollback-1851。exit0、製品/公開物/実ゲーム/元保存/個人差分無変更、コミット・プッシュなし。


2026-11-21 19:49 JST 原料の出荷予約束と空き枠予約を区別する合算境界５原料30項目PASS。固定配布v0.13.0 A6A10CBA0EF44A9DFCA44F2697686A307D6440031A00268B80D600C05A34C73E、前18:51同様の１格納庫47unitsモデルfixture。合算先15units束がshippingなら１unit入荷を拒否し元record不変。一方、reserved=1でも未出荷の既存束へ全量合算できる１unitは受取成功・合計48、追加格納庫を消費しない。最初は予約枠が合算も拒否すると誤った期待で失敗、実装は新規荷を追加する場合だけHasMapleSpaceで予約枠を確認するため現行挙動として修正。途中のテスト置換ミスも直し最終30PASS/exit0。仕様変更・不具合修正ではなく現行モデル挙動の確認で、予約数量全体の妥当性/実配送/保存/UIは対象外。NAS archives/raw-materials/raw-merge-reserved-2026-11-21-1949、再生成tmp/delete-after-2026-11-22/raw-materials/raw-merge-reserved-1949。初期空き保証なし・有機能１枠維持の最新確定は保持し、按分未確定部分を実装せず、製品/公開物/実ゲーム/元保存/個人差分無変更、コミット・プッシュなし。


2026-11-21 20:42 JST 国旗別倉庫管理人ルールの原料受取予測に不具合を発見・修正。RobotStore.PlanningCopyが_flagWarehouseRulesを複製せず、同国旗１～２部位の管理人でも旧固定効果＋４/＋７として扱い、満杯の原料便を受取可能と過大予測していた。通常/中継×最大同旗部位１・２・３の６条件18検査で修正前４条件不一致（例：実３枠に予測５枠、実４枠に予測８枠）、フラグ複製追加後18PASS。ゲームルール変更なし。修正前DLLは直前の計略UI開発版、修正後SHA256 3B2AD06DB55D6B344677328C2392F8B01F81B609CDC5074251E373FE89E8F703。製品ビルド/FontCoverage成功、独立モデル検証（実配送・UI・収束は未検証）。NAS archives/raw-materials/flag-capacity-forecast-2026-11-21-2042 にコード・before/after.log、tmp/delete-after-2026-11-22/raw-materials/flag-capacity-forecast-2042 とflag-capacity-product-2042。初回テストは列挙名Polypropylene誤記でコンパイル失敗、PPへ修正してから修正前後を再検証。20:30のCPU候補は修正前PlanningCopyを使っているため、将来の同条件比較では予測誤差を含む候補として区別する。元保存/公開物/実ゲーム/個人差分無変更、コミット・プッシュなし。次は修正後の実配送予約または管理人交代と受取予測の境界を検証。


2026-11-21 21:43 JST 国旗別管理人の交代相当の容量変化と原料予測を通常/中継24項目PASS。固定DLL 3B2AD06DB55D6B344677328C2392F8B01F81B609CDC5074251E373FE89E8F703。外国の同旗３部位管理人で通常５/中継８枠、PPで残枠満杯時に２部位を明示未所属へ変えた管理人へのReplaceは実体/PlanningCopy双方拒否し原料・ロボットrecord不変。原料を移した条件では通常３/中継４枠へ縮小でき、コピー上だけ３部位へ戻すと増枠し入荷可能、元格納庫の容量・原料を変えない。予約UIや実交代命令/保存/実配送は未検証、モデルの原子的な容量保護と予測独立性の確認で製品修正なし。NAS archives/raw-materials/flag-keeper-swap-capacity-2026-11-21-2143（固定DLL/ソース/結果）、tmp/delete-after-2026-11-22/raw-materials/flag-keeper-swap-2143。exit0。最新UI既定は追加/原料約12.5％に変更済みで、25・25の既存シミュレーションとは別条件として維持。元保存/実ゲーム/公開物/個人差分無変更、コミット・プッシュなし。
