# 解析用ヘッドレスモード

`--headless` を付けると `Game1`、ウィンドウ、描画、音声を作らず、通常のモデルとCPU判断で新規９素材世界を進める。通常プレイ／自動操縦／エキシビジョンの演出には影響しない。

```powershell
dotnet FolderVerse.dll --headless --seed 42 --cast-seed 8 --players 20 --max-turns 400 --output Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-20\headless-example
```

`--headless --help` で全引数を表示する。既定は20CPU、運否天賦のロボット配置25／原材料25、解析上限400全体ターン。上限は1～400。上限未達で征服すれば成功、上限で未到達なら収束未判定として停止する。既存セーブの再開入口は持たない。

## CPUエキシビションとの比較

`tests/Headless/Headless.csproj` を検証用の出力先へビルドし、生成した `Headless.dll` に `--compare-exhibition <新規の検証出力先>` を渡す。画面を開けるWindowsセッションで実行する。4 CPU、世界42・人物8・領有権492471852と固定の運否天賦シードで、両経路を新規世界から2全体ターン進める。画面あり側は土地選択以降のゲーム処理を動かし、通常セーブと統計は無効化する。シードと初期在庫、産業後の地点所有者・ロボット在庫・産業結果・順位・征服条件を比較し、差があれば終了コード1を返す。検証出力の `world.fvs` と `exhibition-*.fvs` は差の調査に使える。

2026-11-20 JST、HEAD `fdc107c6` で実行。初期の運否天賦シード、人物、地点別の在庫集計は一致した。第1・第2ターンの産業後は、ターン番号・産業抽選結果・征服未達が一致し、地点所有者・ロボット在庫・暫定順位に差が出た。画面あり側の内政にある守備予約と組み立ては、ヘッドレス側の直接進行には含まれない。最初に状態が分岐する処理は次回の診断対象とする。勝者が出るまでの進行、手動プレイ、途中からの復元は未検証。

NASへのビルド中に接続が途切れたため、空き容量を確認して `artifacts/tmp/delete-after-2026-11-22/headless-parity/run-211859/` に出力した。ローカルで再ビルドし、画面ありの比較は終了コード1（差あり）。保管最終日は11月22日 JST。接続復旧時は参照とハッシュを確認した上で移す。

標準出力はJSONL。初期SEED一式、DLL SHA-256、版、登場人物、原料有効状態、地点別初期容量／在庫／管理人、20ターンごとの順位と最終結果を記録する。`--output` を指定すると同じログを `run.jsonl`、最終世界を `world.fvs` に保存する。既存の同名出力を上書きしない。未指定ならファイルは作らない。通常オートセーブとユーザーの統計ログは使用しない。

新しい解析出力はNASの作業別期限付きフォルダーへ置く。画面の演出時間を省くため、実プレイの所要時間比較にはそのまま使えない。

## Audit 2026-11-21 21:15:11.427025 JST

- HEAD079b3a77; AGENTS/ai-agent README and current differences checked. Existing dirty files are Demo/tutorial audit notes; no game edits or UI process launched. Previous fdc107c6 evidence remains historical, not a current run. Relative change includes CPU domestic ordering by DawnPhase/turn rotation (Game1.ComputerDomestic159), so prior runtime equivalence cannot be assumed.
- Static comparison: ExhibitionComparison uses 4CPU/world42/cast8/rights492471852/fortune Random123, separate new headless runs for1 and2 turns, then normal HandleInputCore exhibition with autosave/statistics disabled. Compares initial fortune/characters/stock and post-industry turn, owners, robot ID/parts, industry, ranking/conquest; does not compare every domestic/delivery/military boundary or full state. Saved snapshots are inspection outputs, not a tested resume path.
- HeadlessRunner51-61 directly PrepareDomestic ->FinishDomestic(automaticAll) ->BeginMilitary/Advance ->industry unless conquest, then clears trade replies; stops on conquest or finite turn limit with convergence undetermined. Screenless duration is not exhibition presentation duration. Existing documented domestic guard/assembly disparity is not reclassified as fixed. No new runtime difference identified this static review.
- Partial audit complete / real-screen comparison unverified: no new build or repeated old binary, no new validation output. Next current-source bounded comparison with isolated output and matching CPU settings/actions, then capture first differing phase before claiming parity; winner/convergence, restore-vs-continuous and ordinary OS input remain unverified. Prior OsTutorial capture timeout ended/collected in tutorial audit; no shared UI overlap. No commit/push/publication.

- Improvement reproduction planned 2026-11-21 21:30:18.954074 JST HEAD079b3a77: existing C fallback folderverse-audit retainedNov22 (prior NAS outage; no source deletion). Build tests/Headless limit600s/headless-current-build.log; current exhibition comparison limit300s/headless-current-compare.log, separate output headless-current-compare in same temporary root. No product edit before reproduction; saves/statistics disabled by independent entry-assembly guard and diagnostic.

## Diagnostic repair 2026-11-21 21:34:00.845360 JST

- HEAD079b3a77 (start21:29). Reproduced current comparison before product edits: build PID27984 21:30:34-45 limit600s exit0 warnings0/errors0, headless-current-build.log. Run PID26228 21:31:16-37 limit300s exit1/noTIMEOUT, headless-current-compare.log. Initial fortune/stock differed and diagnostic world stayed turn0 while actual screens advanced. This differs from historical fdc107c6 result and cannot be attributed directly to CPU behavior.
- Confirmed diagnostic stale reference: Game1.LandSetup101 replaces _setup with _fortuneStockPreview on normal Fortune confirmation. ExhibitionComparison cached old _setup before those calls. Changed only tests/Headless/ExhibitionComparison.cs: re-read actual _setup after confirmation and require WorldStatus. No CPU/gameplay/presentation change.
- Build command: dotnet build tests/Headless/Headless.csproj --artifacts-path C shared folderverse-audit -p:UseAppHost=false. Repaired build PID21820 21:32:48-54 limit600s exit0 warnings0/errors0, headless-rebind-build.log. Run: dotnet same/bin/Headless/debug/Headless.dll --compare-exhibition same/headless-rebind-compare (ALSOFT_DRIVERS=null), PID248 21:33:09-32 limit300s exit1/noTIMEOUT, headless-rebind-compare.log. All sessions ended/collected. Separate new result directories preserve prior evidence; existing C fallback retainedNov22.
- Repaired diagnostic now MATCH initial fortune/characters/stocks and ALL compared first-turn fields (turn/owners/robot stock/industry/ranking/conquest). First observed remaining difference is SECOND-turn post-industry owners/robot stock/ranking; turn/industry/conquest still MATCH. This does not locate the first internal phase divergence. Both normal screen-handler exhibition and headless model ran; no physical OS input or manual screenshot verification claimed.
- Diagnostic reference repair complete; gameplay parity remains UNRESOLVED, not a successful parity run. Next compare second-turn domestic reservation/assembly, delivery and military boundaries with exact actions/settings before a product fix. Full state, raw inventories/roles, winner/convergence and resume-vs-continuous remain unverified. git diff --check passes, HEAD unchanged; user notes/saves preserved, no commit/push/publication.

## Audit 2026-11-22T21:15:59.905372+09:00 JST

- HEADb3a5cb5cb5c2da14ef39d870adeea7dd16504a60; start21:15, instructions/current differences/latest salvage specification reviewed. Dirty changes only Demo/tutorial review notes, preserved. Source/change review complete only; no game/test edits or UI/build launch.
- Since prior079 comparison, salvage expeditions add real workers/pocket storage loans and are planned in shared ConquestCampaign.FinishDomestic; headless still calls that shared phase. Current headless defaults remain25/25 while normal fortune defaults changed, so matching explicit stock settings must be verified before interpreting any new discrepancy. Previous second-turn post-industry difference remains historical/unresolved, not reproduced on this HEAD.
- No current real-screen/headless comparison performed. Next isolated current-build comparison with explicit equal world/cast/rights/fortune/CPU/stock settings and phase-boundary snapshots; then identify first differing action before a product repair. Full state, winner/convergence, resume-versus-continuous and physical OS input remain unverified. No duplicate shared-UI run, commit/push/publication.

- Improvement review 2026-11-22T21:26:55.601882+09:00 JST (start21:26), HEADb3a5cb5c unchanged. Prior079 second-turn discrepancy remains unresolved; latest audit has no current-HEAD runtime reproduction and explicit fortune-stock settings still need alignment before attribution. No speculative CPU/product change or old-binary rerun; no build/UI launch. Next bounded isolated current-source comparison with equal settings and first differing phase, then minimal repair if reproduced. Existing docs changes protected; review complete only, gameplay parity not validated, no commit/push/publication.


## Audit 2026-11-23 current layout / HEAD8eb4b2e6

- Current Headless build succeeded; isolated --compare-exhibition run started21:16:27 JST and ended21:18:07 JST, limit300s/99.98s/exit1, no timeout. Output: NAS tmp/delete-after-2026-11-23/tutorial-current/headless-exhibition-8eb4b2e6. Build/run logs copied with SHA256 verification to NAS archives/diagnostics/headless-layout-8eb4b2e6-2026-11-23. No product/test edits in this comparison.
- Same world42/cast8/rights492471852/fortune Random123 and4CPU; field initializers both25/25, but effective preview settings differ (correction below). Initial fortune and characters MATCH, initial stocks DIFFER. Both first/second post-industry turns: number/industry/conquest MATCH, owners/robot stock/ranking DIFFER. Earliest observed discrepancy is now initial stocks, before turn1; this is not proof of a CPU decision defect. Diagnostic rebind remains present.
- Comparison completed with unresolved parity; inspect initial stock generation and exact settings before gameplay repair. Does not verify all phase boundaries, full state, resume parity, natural conquest, physical OS input or every layout. Existing user changes/saves protected; no commit/push/publication.


## Improvement reproduction 2026-11-23T21:37:37.613078+09:00 JST

- HEAD83d5835b after user merge. Build tests/Headless with NAS tutorial-current/entry-layout artifacts, UseAppHost=false/UseSharedCompilation=false/-m:1/-v:minimal: PID11516 21:34:23-21:35:01 limit600/37.95s exit0/warnings0/errors0. Run Headless.dll --compare-exhibition isolated headless-exhibition-83d5835b/null audio: PID36248 start21:35:23 limit300/exit1/noTIMEOUT; end/duration in headless-83-run.log, sessions1484/1281 collected. Initial fortune/characters MATCH, stocks DIFFER; both turns number/industry/conquest MATCH, owners/robot stock/ranking DIFFER.
- Comparison precondition mismatch found: EnsureFortuneStockPreview computes robot share from FixedRobotUnits plus up to12.5 percent, raw share from additional amount, then RebuildInitialStock. HeadlessRunner instead passes defaults25/25 to CompleteLand. Equal field initializers do NOT imply equal effective stock policies. This corrects earlier8eb note; later differences cannot yet be attributed to CPU behavior. Merge also changes keeper material/capacity.
- No game/test edit. Next align effective initial-stock policies and assert parameters/state before phase comparison. Full state/resume/winner/physical input unverified. Logs archived with hashes at NAS archives/diagnostics/headless-layout-83d5835b-2026-11-23; temporary output retainedNov23. No commit/push/publication.
