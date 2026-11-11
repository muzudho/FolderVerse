# Demo クリック位置の点検記録

## 初期状態（2026-10-31）

利用者から、UI変更後にDemoのクリックポイントがずれている箇所があるとの報告。定期点検は未実施。確認済みの個別不具合はまだない。

## 問題一覧

### 共通の確認条件（初回点検）

- 確認日：2026-10-31 08:56〜08:59 JST。HEAD：`a9f642b159cf09922e6b50635174346fd13b08e7`。
- 点検開始時のGit差分なし。ゲームコード・テストソースへの変更なし。
- 実描画：1920×1080、ウィンドウ、論理キャンバス1920×1080。固定ワールド種123・人物456・配置789、PlayerSlot=0。地図は初期の地星儀姿勢。格納庫テストの既存fixtureと同じ準備を使い、対象段階を個別に呼んで照合した。
- 診断ソース：`artifacts/demo-click-audit/Program.cs`、`DemoClickProbe.csproj`。ログ：`artifacts/demo-click-audit/probe-output.txt`。出力先は既存の`artifacts/corner-lesson`を再利用し、素材一式は追加コピーしていない。artifactsはGit管理対象外のローカル証拠。

### DEMO-0001：輸送計画を開くクリック先が、地点監督の休暇ボタンに重なる

- 状態：完了（2026-10-31修正回。詳細はdemo-click-fixes.md）。優先度：高。
- 対象：`FolderVerse/Game1.DemoDomestic.cs` 段階1、`Game1.NodeTransport.cs`、`Game1.NodeOffice.cs`。
- 再現：段階0で地点監督を開き、スライドを完了させる。段階1を1.8秒進める。
- 期待：表示された輸送長期計画への入口をクリックし、その計画画面を表示する。
- 実際：クリック位置は旧`NodeTransportTile`中心(371,917)。現画面では(84,916,380,48)の「地点監督に休暇を与える」に重なる。`DrawNodeTransportTile()`は空で、輸送ボタンは描かれない。段階1は入力を通さず`OpenNodeTransport()`を直呼びし、`_nodeOfficeOpen=True`と`_nodeTransportOpen=True`が共存する。`DrawNodeDialog()`はofficeを先に描くため、画面も地点監督のまま。
- 証拠：`probe-0.png`と`probe-1.png`（両方とも地点監督）、ログ`stage1 click={X:371 Y:917} office=True transport=True`。休暇ボタン位置の定義は`Game1.NodeAutomation.cs:15`。
- 修正の方向：現在の輸送長期計画プライマリーボタン等への実際の操作経路に合わせる。隠れた旧ボタンの座標だけを変更して終えない。

### DEMO-0002：閉じる演出が現在の×から外れている

- 状態：完了（2026-10-31修正回。詳細はdemo-click-fixes.md）。優先度：中。
- 対象：`Game1.DemoDomestic.cs`段階11、`Game1.DomesticView.cs`の`InspectorClose`。
- 再現：インスペクター表示・スライド完了後、段階11を進める。
- 期待：右側ハンドルの×を指して閉じる。
- 実際：Demoは(832,190)をクリックして`ResetInspector()`を直呼び。実際の×は矩形(912,464,48,48)、中心(936,488)。クリック位置は矩形外だが画面は閉じてしまう。
- 証拠：ログ`stage11 expectedClose={X:912 Y:464 Width:48 Height:48} actual=(832,190)`、`probe-0.png`の×と`probe-2.png`の閉じた状態。

### DEMO-0003：移動予約を開くとき「わたしの国」を経由せず、存在しない位置をクリックする

- 状態：完了（2026-10-31修正回。詳細はdemo-click-fixes.md）。優先度：中。
- 対象：`Game1.DemoDomestic.cs`段階12、`Game1.Movement.cs`、`Game1.MoveReservationMarkers.cs`。
- 再現：段階11の`ResetInspector()`後に段階12を進める。
- 期待：わたしの国を開いて、表示中の移動予約ボタンをクリックする。
- 実際：段階12直前は`_reignView=False`、`InspectorVisible=False`。Demoは`MovementButton`中心(377,938)を指して`OpenMovement()`を直呼びする。移動予約ボタンはわたしの国のインスペクターに表示され、通常入力でも`_reignView`が必要。この時点の画面に当該ボタンはない。
- 証拠：`probe-2.png`（主な操作は下段のプライマリーボタンのみ）、ログ`stage12 before reign=False inspector=False movementButton={X:282 Y:912 Width:190 Height:52}`。`probe-3.png`で直呼び後に移動パネルが開く。

### DEMO-0004：DemoUiが古い引数で停止し、巡回検証に到達しない

- 状態：完了（2026-10-31修正回。詳細はdemo-click-fixes.md）。優先度：高（検証の妨げ）。
- 対象：`tests/DemoUi/Program.cs`の`Tick`と開始時の期待値、`Game1.Demo.cs`の`UpdateDemo`。
- 再現：下記のビルド後に`dotnet artifacts/corner-lesson/DemoUi.dll`を実行する。
- 期待：Demoの開始・巡回・終了を検証する。
- 実際：`Tick`は５引数を渡すが、実装は`elapsed, active, click, escape, enter, now`の６引数。最初の`Tick(9)`で`System.Reflection.TargetParameterCountException: Parameter count mismatch`、`Program.cs:12`→`:17`で停止した。ビルド自体は成功。加えてテストは導入直後にRollingを期待するが、現在はPlayerCountへ進むので、引数だけ直して完了扱いにしない。
- 証拠：実行時スタックとソースを照合。全経路成功とは扱わない。

### DEMO-0005：人数確認のクリック演出が確認ボタンから外れ、次の人数確認も省略する

- 状態：完了（2026-10-31修正回。詳細はdemo-click-fixes.md）。優先度：中。
- 対象：`Game1.Demo.cs`の汎用aimと`case Screen.PlayerCount`、`Game1.PlayerCount.cs`。
- 再現：DemoをPlayerCount画面で進める。診断では段階待ち時間を短縮して該当分岐を実行した。
- 期待：確認ボタン（矩形1050,900,700,90、中心1400,945）を指し、現在の人数確認フローに沿って進む。
- 実際：PlayerCount専用のaimがなく、目標は汎用(1580,790)で確認ボタン外。診断の短縮実行でもクリック点(377,938)は矩形外のままRollingへ進んだ。通常の確認入力はComputerPlayerCountへ進むが、Demoは`BeginRolling()`を直呼びして省略する。
- 証拠：`probe-output.txt`の`playercount click=... confirm=... resultingScreen=Rolling`とソース。汎用aimへの補間が完了した自然再生のクリック画像は未取得。修正回は自然再生でも確認すること。

## 実行履歴

### 2026-10-31 初回点検

- 開始：08:56 JST。終了：08:59 JST。状態：完了（今回の点検回。全経路の確認は未完了）。
- ゲームコードの修正なし。現在の描画・入力矩形とDemoの直呼び先を照合し、上の５件を登録した。
- `dotnet build tests/DemoInspectorUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet`：成功（警告・エラー０）。`ALSOFT_DRIVERS=null`で同出力の`DemoInspectorUi.dll`実行：成功。これは地点監督の直呼び・描画検証であり、クリック位置の正しさを保証しない。
- 同じ出力先への`tests/DemoUi`ビルド：成功。`DemoUi.dll`実行：DEMO-0004の例外で停止。
- 診断用`DemoClickProbe`のビルドと実行：成功。各画面の実描画PNGと数値ログを確認した。
- 次回の続き：まずDEMO-0004の修正後に自然再生を巡回し、段階0の地点選択、段階2の行先、格納庫の段階3〜7、工場の段階8〜10、移動の段階13〜終端を検査する。段階14の固定(402,366)にも現画面との対応確認が必要（未確認のため個別不具合には未登録）。
- 未確認：直送便の退避復帰・左右インスペクターには現在のDemoソースに専用の巡回操作が見当たらない。別UIテストの成功でDemo確認済みとはしない。地図切替・継ぎ目移動・戦闘／輸送／説得／20分後の終了経路、540p等の別解像度、回転した展開図と地星儀、自然再生の待ち時間も次回以降に残る。

## 2026-11-01 01:00回の追加問題

共通条件：確認時刻2026-11-01 01:02～01:03 JST、HEAD `43b1b14aada2ca849ba9cca32a8a6a56925b4ade`。ゲーム・テストソースの未コミット差分なし。開始時の差分は定期時刻を更新したREADMEと利用者のチャット文書。全体statusはGit LFSフィルターの権限エラーになり、コマンド単位でLFSフィルターを無効にした読み取りではNo.52のPNG８件も変更候補として表示された（実ファイルとLFSポインターの比較の影響を除外できないため、利用者の実変更とは断定しない）。それらは触っていない。

再現用ソースは `artifacts/demo-click-audit/Probe20261101.cs.txt`、専用プロジェクトは同フォルダーの `DemoAudit20261101.csproj`。既存 `artifacts/corner-lesson` 出力を再利用。世界123・人物456・配置789、PlayerSlot=0、展開図・初期姿勢、対象地点26。診断は段階を直接呼ぶため、自然再生の全操作を確認したとはしない。

### DEMO-0006：工場プランを開くクリック位置が製造期間３で固定

- 状態：完了（2026-11-01 01:29 JST修正・検証。詳細はdemo-click-fixes.md）。優先度：中。
- 対象：`FolderVerse/Game1.DemoDomestic.cs:26,54`、`Game1.NodeFactory.cs:16,42,78`。関係する未コミット差分なし。
- 再現：上記fixtureで地点26の工場を開き、第１レーンの選択を「製造しない」（SelectPlan(3)）へ変更する。インスペクターを開き切ってからDemo段階９を1.8秒進める。
- 期待：描画と同じ現在の製造期間（製造しない場合は１）で得た製造品ボタンまたは矢印にカーソルを置き、通常の入力でプラン一覧を開く。
- 実際：表示中ボタンは(160,288,72,88)、中心(196,332)、矢印もその隣。Demoのクリックは期間３固定の(340,332)で、両矩形の外。それでも `_factoryPlanLine=0` の直代入で一覧が開く。製造期間１／２／４／５等でも同じ固定値の問題があるが、今回の実描画再現は「製造しない」で行った。
- 解像度：1920×1080と960×540、論理キャンバス1920×1080。双方とも論理クリック(340,332)、表示ボタン外を確認。
- 証拠：`artifacts/demo-click-audit/audit-20261101-output.txt` の `nonManufacturing=False actualButton={X:160 Y:288 Width:72 Height:88}` と `stage=9 click={X:340 Y:332} ... plan=0`。画像は `artifacts/corner-lesson/audit-20261101-1.png`、`audit-20261101-3.png`。1080p画像を目視確認した。
- 修正案：描画と同じ現在のレーン・期間から矩形を取得し、NodeDialogClick等の通常経路で開く。固定値だけを別の固定値へ置き換えない。

### DEMO-0007：工場プランをクリックした演出だけで実際のプランを選ばない

- 状態：完了（2026-11-01 01:29 JST修正・検証。詳細はdemo-click-fixes.md）。優先度：中。
- 対象：`Game1.DemoDomestic.cs:55`、`Game1.NodeFactory.cs:40`。関係する未コミット差分なし。
- 再現：DEMO-0006のfixtureで選択中プランを３にし、段階９で一覧を開いた後、段階10を1.8秒進める。
- 期待：FactoryPlanButton(0)の中心を押したので、通常操作と同じSelectPlan(0)が実行され、第１候補へ変わって一覧が閉じる。
- 実際：クリック位置(184,482)は候補０の矩形(94,410,180,144)内だが、段階10は `_factoryPlanLine=-1` にするだけ。SelectedPlanは３のままで、製造しない状態が残る。クリック座標自体ではなく、クリック先と遷移結果の不一致。
- 解像度：1920×1080。540pでの同段階の実描画は今回未実施。
- 証拠：同ログの `stage=10 click={X:184 Y:482} ... plan=-1` と `stage10 selected=3 expected=0 option={X:94 Y:410 Width:180 Height:144}`。`artifacts/corner-lesson/audit-20261101-1.png`（一覧、３が選択中）と `audit-20261101-2.png`（閉じた後も「製造しない」）を目視確認した。
- 修正案：通常の工場候補選択入力へ接続し、実際のSelectedPlanと完成予定品が変わることまで検証する。

### 実行履歴：2026-11-01 01:00回

- 開始：01:00:27 JST。終了：01:03:03 JST。状態：完了（今回の部分点検。全経路は未完了）。ゲームコード・既存テストコードの変更なし。新規登録はDEMO-0006／0007のみ。0001～0005の完了状態は変更しない。
- 競合確認：アプリのチャット一覧で同じ作業領域の別の実行中チャットなし（当該チャットのみactive、他はidle／notLoaded）。Get-ProcessではFolderVerse・dotnet・Demo等の実行中プロセスは検出されなかった。CIMによるコマンドライン取得は権限拒否。起動中プロセスの停止操作は行っていない。
- 共通環境：DOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null、既存出力先 `D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/`。
- `dotnet build tests/DemoUi --no-restore -p:OutputPath=（上記） -v quiet --disable-build-servers`、同じ引数のDemoInspectorUiビルド：成功、警告・エラー０。
- `dotnet build artifacts/demo-click-audit/DemoAudit20261101.csproj --source artifacts/nuget-feed -p:OutputPath=（上記） -v quiet --disable-build-servers`：成功、警告・エラー０。
- `dotnet artifacts/corner-lesson/DemoAudit20261101.dll`：成功。数値ログと実描画から上の２件を再現。ログは `artifacts/demo-click-audit/audit-20261101-output.txt`。
- `dotnet artifacts/corner-lesson/DemoInspectorUi.dll`：成功。閉じるハンドル、輸送長期計画プライマリー、わたしの国→移動、人数確認の矩形・遷移の既存チェック。ログは同フォルダー `inspector-20261101-output.txt`。
- `dotnet artifacts/corner-lesson/DemoUi.dll`：成功。自然に進めた開始・地図切替・18FPSの面接続・拒否後分岐・終了ループ等、既存チェック。ログは同 `demo-20261101-output.txt`。この成功は工場の各クリックが正しいことを意味せず、上記２件は既存チェックが見逃している。
- 優先範囲：プライマリーと×の既存Demo経路は上記テストで再確認した。直送便の退避復帰・左右インスペクターを巡回する専用経路はGame1.Demo*.csに見当たらず、Demoとしては未確認のまま。DirectDeliveryUiの過去の成功で代替していない。
- 次回修正回：この完了済み点検のDEMO-0006／0007を現在のコードで再現してから修正する。
- 次回点検の続き：段階０の固定(480,260)と実際に選択される地点26の地図上の位置を、開く直前のカメラ・HitNodeと照合する。今回は固定座標と直呼びを確認しただけで地点選択の誤りを断定せず、個別IDは未登録。次に格納庫段階３～７の組立クリックと実部位、段階13の移動先、直送便専用巡回の有無・未実施範囲を調べる。全体の540p自然再生、回転した地星儀、戦闘・輸送・説得の各クリック画像、タイミング全般は引き続き未確認。

## 2026-11-01 02:00回の追加問題

共通条件：2026-11-01 01:56:58～02:00:29 JST、HEAD `43b1b14aada2ca849ba9cca32a8a6a56925b4ade`。先行修正は01:29完了済み。開始時の関係差分はDEMO-0006／0007の修正（Game1.DemoDomestic.cs、Game1.NodeFactory.cs、tests/DemoInspectorUi/Program.cs）とその記録、定期設定のREADME。これらを保持し、今回ゲーム・既存テストコードは変更していない。全体statusはLFSをコマンド単位で無効にして読み取り、前回と同じNo.52のPNG８件候補と利用者のチャット文書も保持した。

診断は現在の検証用FolderVerse.dll（SHA256 `F20F2555BB61420BBA7922A6F84779DDFAD5B7D0E65B9479169AE623F945793B`）を参照し、世界123・人物456・配置789、PlayerSlot=0、地点26を使う。ソースは `artifacts/demo-click-audit/ProbeNext.cs.txt`、プロジェクトは同 `DemoAuditNext.csproj`。ログは同 `next-20261101-0200-output.txt`。描画は既存 `artifacts/corner-lesson/audit-next-0.png`～`7.png` を再利用する。

### DEMO-0008：地点を開くDemoのクリックが地図上の地点に一致しない

- 状態：完了（2026-11-01 02:31 JST修正・検証。詳細はdemo-click-fixes.md）。優先度：中。
- 対象：`FolderVerse/Game1.DemoDomestic.cs:21,36～39`、展開図のNodePosition／HitNode。関係差分は先行修正のみ（段階９・10の変更で、この段階は未変更）。確認時刻01:58～02:00 JST。
- 再現：インスペクターを閉じ、展開図Home・ScreenAngle=0で段階０の直前のNetPanelと地点26の投影位置を取得し、HitNodeを照合する。その後UpdateDemoDomestic(1.8)を実行する。90度回転と540pでも繰り返す。
- 期待：これから開く地点26の見えているマーカーへカーソルを合わせ、通常の地点選択と同じ地点を開く。見えていない場合には先に地図を合わせる。
- 実際：初期投影は論理座標(1026.1125,503.5175)、その位置のHitNodeは26。一方、Demoのクリックは固定(480,260)、HitNode=-1なのに、OpenNodeInspectorの直呼びで地点26が開く。90度回転時は地点26が(1008.4825,618.11255)へ動くが、Demoは同じ固定座標で同じ地点を開く。
- 解像度：1920×1080（０度／90度）、960×540（０度）。すべて論理キャンバス1920×1080。同じ不一致を数値で再現した。
- 証拠：上記ログの `expectedNode=26 projected=... hitAtFixedClick=-1 actualHitAtProjected=26` と直後の `stage=0 click={X:480 Y:260} resultingNode=26`。画像0は開く直前の展開図、1・2・3は開いた後。0を開いて地図の配置を目視確認した。クリック前後でインスペクターが地図を動かすため、判定は必ず開く直前の投影値を使う。
- 修正案：開く予定の地点を先に決め、現在の地図での投影位置と入力の当たり判定を共有する。地図の向き・視野外・表示レイヤーにも配慮する。

### DEMO-0009：格納庫の一度の胴移動演出で、脚まで含めて完全体になる

- 状態：完了（2026-11-01 02:31 JST修正・検証。詳細はdemo-click-fixes.md）。優先度：中。
- 対象：`Game1.DemoDomestic.cs:46～52`、`Game1.NodeAssembly.cs`の通常ドラッグ処理。関係差分は先行修正のみ（格納庫段階は未変更）。確認時刻01:59～02:00 JST。
- 再現：地点26の検証用格納庫に、自国の頭だけ・胴だけ・脚だけの３個体を別々に入れる。段階３で組立対象を選び、段階４→５を各1.8秒進める。別の同等fixtureで、同じ胴の位置を押下して頭の区画で解放する通常操作と比較する。
- 期待：通常操作と同じ部位移動を演出し、その部位だけが移る。３体から完全体を作るなら、脚を移す２回目の操作も見せる。
- 実際：段階３の選択はID332（頭）・333（胴）・334（脚）、source=1／dest=0。段階４のクリック(371,366)は胴だが、_hangarDragは空のまま。段階５のクリック(175,387)でAssembleが３体を一度に合体し、ID332:Completeができ、次段階は８になる。脚の移動操作はない。比較の通常入力は実際にドラッグID336を持ち、同じ区画へ解放すると、ID335:Body, HeadとID337:Legsの２体が残る。クリック・遷移結果が対応しない。
- 解像度：960×540、論理キャンバス1920×1080。1080pの３パーツ組立比較は今回未実施。
- 証拠：同ログの `assembly selection=332,333,334`、`dragAfterSource=`、`dragAfterDrop= robots=332:Complete`、`manualSourceDrag=336`、`sameManualDrop robots=337:Legs,335:Body, Head`。画像4～6がDemo、7が比較操作後。6を開いて完全体になった実描画を確認した。
- 修正案：通常の部位ドラッグ・ドロップへ接続し、必要な部位の回数分だけ演出する。２体合体・一体型など、移す部位の数が異なるケースも考慮する。内部Assembleだけを呼んで完了扱いにしない。

### 実行履歴：2026-11-01 02:00回（トリガー01:56）

- 開始：01:56:58 JST。終了：02:00:29 JST。状態：完了（今回の部分点検）。ゲームコード・既存テストの変更なし。DEMO-0008／0009を新規登録し、既報の状態は維持。
- アプリ一覧で同対象の実行中の別チャットなし（このチャットのみactive、他はidle／notLoaded）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、プロセスの停止はしていない。
- 優先範囲：先行修正の矩形共有・通常入力への接続差分を確認。Game1.Demo.cs／DemoDomestic.csと両既存テストを起点に、今回の継続位置である地点選択と格納庫組立を診断した。直送便の退避復帰・左右インスペクターへ入る専用Demo経路は引き続き見当たらず、そのDemo確認を完了扱いにしていない。
- 検証コマンド：`DOTNET_CLI_TELEMETRY_OPTOUT=1`、`dotnet build artifacts/demo-click-audit/DemoAuditNext.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers`：最終ビルド成功、警告・エラー０。`ALSOFT_DRIVERS=null`、`dotnet artifacts/corner-lesson/DemoAuditNext.dll`：最終実行成功、全７段階と比較操作の実描画・数値ログを取得。
- 診断準備中に、null値取得を誤った診断側のNullReferenceExceptionと、比較操作で前フレームのマウス押下状態を設定し忘れた問題があった。診断だけを訂正して再実行し、上記は最終の成功ログに基づく。ゲーム側の障害や不具合件数には含めない。
- 新たなソース変更をしていないため、既存DemoUi／DemoInspectorUi全体の繰り返し実行はしなかった。全経路成功とは判定しない。関係するゲーム・テスト差分の行数は開始時から不変。
- 次回修正回：完了した本点検のDEMO-0008／0009を再現してから修正する。先行修正0006／0007の差分を保護する。
- 次回点検の続き：段階13の移動先と確定操作、地点選択の地星儀／視野外／地形レイヤー、２体合体・一体型の格納庫操作、直送便のDemo専用経路の扱い。全体540p自然再生、戦闘・輸送・説得の各クリック画像は未確認のまま。

## 2026-11-01 03:00回の追加問題

### DEMO-0010：移動先クリックが地点より上へずれ、移動予約せずターンが進む

- 状態：完了（2026-11-01 03:34 JST修正・検証。詳細はdemo-click-fixes.md）。優先度：中。
- 確認日時：2026-11-01 03:04～03:06 JST。HEAD `43b1b14aada2ca849ba9cca32a8a6a56925b4ade`。関係差分は先行修正0006～0009（Game1.DemoDomestic.cs、Game1.DemoDomesticActions.cs、Game1.NodeFactory.cs、DemoInspectorUi、DemoActionsUi）で、今回それらは変更していない。
- 対象：`FolderVerse/Game1.DemoDomestic.cs:13,31,63～66`、`Game1.DestinationMap.cs:87～90`、`Game1.Movement.cs`のMovementClick。
- 再現：世界123・人物456・配置789、PlayerSlot=0で、わたしの国を開いて表示中のMovementButtonを通常処理で押し、スライドを完了する。Demo段階13を1.8秒進め、続く段階15を進める。展開図０度／90度と地星儀、それぞれ1080p／540pを確認。
- 期待：表示中の選択可能な地点にカーソルを合わせて移動先を選び、確定ボタンでその移動を予約する。選択に失敗した場合には、移動を予約したような演出のまま進行しない。
- 実際：候補は６地点あるが、DemoDestinationPointは古いDestinationBadge（地点の上に置く64×34の矩形）の中心を返す。通常のMovementClickは地点そのもののHitNodeを使い、Badgeの入力処理はない。たとえば展開図の地点89は(1186,450)で通常入力なら選択できるが、Demoは25px上の(1186,425)をクリックし、HitNode=-1・SelectedMoveNode=-1のまま段階15へ進む。続いて(707,938)へ確定のクリック演出を出し、選択がないためCampaign.Advance(-1,0)に落ち、予約０件でTransportへ移る。
- 回転展開図でもDemo(1468,434)に対し実地点(1468,459)、地星儀でもDemo(1389,222)に対し実地点(1389,247)。いずれも通常の実地点へのMovementClickでSelectedMoveNode=89になることを比較確認した（比較後は選択を元に戻してDemoの後続分岐を検査）。540pの地星儀ではDemo点(1898,222)がMapViewBounds（右端1880）の外になるケースもあり、同じ問題に含める。
- 解像度：1920×1080／960×540、論理キャンバス1920×1080。６条件で段階13の選択失敗→段階15の予約なし進行を再現。地星儀は初期カメラ等の内部状態の影響を受けるため、540pと1080pの投影値が一致するとは扱わない。
- 証拠：`artifacts/demo-click-audit/movement-0300-output.txt` の各scenarioのbefore・stage13・normalPoint・stage15。ソースは同 `ProbeMovement.cs.txt`、プロジェクトは `DemoAuditMovement.csproj`。画像は既存 `artifacts/corner-lesson/audit-movement-0.png`～17.png。１（展開図、候補を選べていないクリック）と２（予約なしでTransportへ）を開いて目視確認した。候補０件時の正常な待機ケースとの混同を避け、６件あることをログに残した。
- 修正案：移動先の現在の投影位置と通常の候補限定HitNodeを共有し、地図内・選択可能であることを確認してから指す。視野外なら先に地図を合わせる。段階13で選択した地点、段階15で実際に作成された予約の対象・内容を検証する。25pxの固定補正だけで済ませず、古い未描画Badgeへの依存をなくす。

### 実行履歴：2026-11-01 03:00回

- 開始：03:03:31 JST。終了：03:06:03 JST。状態：完了（今回の部分点検）。追加はDEMO-0010の１件。既報0001～0009の完了状態は維持。ゲーム・既存テストソースへの変更なし。
- AGENTS、README、点検・修正記録と関係差分を確認した。先行02:30修正は02:31:56完了済み。アプリ一覧では同対象の別実行なし（このチャットのみactive、他はidle／notLoaded）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、プロセスの停止なし。
- 全体statusはコマンド単位でLFSフィルターを無効にして読み取り。前回と同じNo.52のPNG８件候補、利用者のチャット文書、READMEと先行修正・記録を保持した。検証用FolderVerse.dllのSHA256は `26F576D35FDABC77F6B1DB32FF5E67BF14FD534FA285F8B96577A3C00B25E2D4`。既存ビルド出力を参照し、今回ゲームのビルドはしていない。
- 優先項目：プライマリー・直送便の退避復帰・左右インスペクターについて、Game1.Demo.cs／DemoDomestic.csと既存テストから経路を確認した。直送便専用の巡回は引き続き見当たらず、別UIテストの成功でDemo確認済みにはしない。今回は前回の続き位置の移動先選択と確定へ進んだ。
- `DOTNET_CLI_TELEMETRY_OPTOUT=1`、`dotnet build artifacts/demo-click-audit/DemoAuditMovement.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers`：最終ビルド成功、警告・エラー０。診断追記中にローカル変数名の衝突によるコンパイルエラーが一度あったが、診断ソースだけを直し、ビルド成功後の最終実行を証拠とした。
- `ALSOFT_DRIVERS=null`、`dotnet artifacts/corner-lesson/DemoAuditMovement.dll`：最終実行成功、６条件×選択前・選択後・確定後の18画面と通常入力比較の数値を取得。出力先は既存artifacts/corner-lessonを再利用。
- ゲームソースは変えていないため、既存の全Demo回帰テストを重複して実行していない。全経路成功とは判定しない。関係差分の行数は点検前後で不変。セーブの読み書き、コミット・プッシュ・公開なし。
- 次回修正回：DEMO-0010を現在のコードで再現してから修正する。先行0006～0009の未コミット差分を保持する。
- 次回点検の続き：移動修正の確認後、世界構築の人物・配置確認、征服者選択、戦闘・輸送・説得のクリック演出を順に照合する。直送便の専用Demo巡回、全体540p自然再生、画面外候補が多い世界での長時間確認は未確認のまま。

## 2026-11-01 04:00回の追加問題

### DEMO-0011：区画権利確認・征服者選択のクリックが汎用座標のまま

- 状態：完了（2026-11-01 04:40 JST修正・検証。詳細はdemo-click-fixes.md）。優先度：中。
- 確認日時：2026-11-01 04:03～04:04 JST。HEAD `43b1b14aada2ca849ba9cca32a8a6a56925b4ade`。関係差分は先行0006～0010のDemoDomestic・工場・Demoテストと記録。今回Game1.Demo.csと通常の世界構築UIに未コミット変更はなく、ゲームコードの変更なし。
- 対象：`FolderVerse/Game1.Demo.cs:92～96,119～122`、`Game1.cs`のCastAction・StatusProceed・HitSelection・DrawSelectionUi。
- 原因の共通性：人数確認・世界抽選・人物抽選には専用のaimがあるが、PlacementRolling／PlacementReview／PlayerSelect／PlayerReadyにはなく、汎用(1580,790)を指したまま直呼びで進む。この共通の欠落を１件として登録し、同じ原因の段階ごとに連番を増やしていない。
- 再現：人間１人・コンピューター19人、世界123・人物456・初期配置789を用意し、PlacementRollingからDemoを18FPS（1/18秒刻み）で各画面が変わるまで更新する。PlacementReview→PlayerSelect→PlayerReady→LandConquestまで同じ流れを進める。各遷移直前の実UI矩形と実際の_demoClickPointを照合する。
- 期待：区画権利抽選の停止・確定はCastActionの表示矩形、征服者の選択は実際に選ぶ人物のタイル、最後の確認はStatusProceedの矩形を指し、通常の入力と同じ対象を確定する。
- 実際（区画権利）：PlacementRolling／PlacementReviewの実ボタンは(518,1020,480,50)、中心(758,1045)。Demoは(1578,789)または(1579,789)でボタン外だが、画面の直接代入／BeginSelectionで次へ進む。
- 実際（征服者選択）：1080pのPlayerSelectではスロット０のタイルが(1537,112,258,180)。Demoは(1579,789)をクリックし、HitSelectionはスロット16を返すのに、SelectPlayer(0)でスロット０を選ぶ。540pではスロット０のタイルが(54,472,258,180)、同じクリック点のHitSelectionは14なのに０を選ぶ。タイル配置はランダムな領地数により変わるが、Demoは実際の選択対象に追従していない。
- 実際（選択後確認）：PlayerReadyのStatusProceedは(518,1020,480,50)。Demoは引き続き(1579,789)で矩形外だが、ConfirmHumanSelectionでLandConquestへ進む。
- 解像度：1920×1080／960×540、論理キャンバス1920×1080。各遷移は55フレームで発生。PlacementRollingで通常の再抽選があるため、最終配置Seedは1080pが1978627658、540pが2084923967だった（両方、世界123・人物456）。再現ソースとログに記録済み。配置の違いを解像度だけの影響とは扱わない。
- 証拠：`artifacts/demo-click-audit/setup-0400-output.txt` の８遷移のexpected／click／inside=Falseと、２件のselectionHitAtDemoClick。ソースは同 `ProbeSetup.cs.txt`、プロジェクトは `DemoAuditSetup.csproj`。画像は既存 `artifacts/corner-lesson/audit-setup-0.png`～9.png。２（1080pの選択画面でカーソルが別の顔）と３（同じ場所を指したまま別のスロット０に「あなた」表示）を開き目視した。
- 修正案：実際のCastAction・CharacterSelectionLayoutの選択タイル・StatusProceedを参照してaimを決め、クリックが当該矩形・HitSelectionに一致することと、確定した人物が一致することを検証する。人数確認と同様に最終クリック位置も対象へ揃える。複数の人間が参加する設定では既に選ばれた人物を再選択しない点にも留意する。

### 実行履歴：2026-11-01 04:00回

- 開始：04:02:01 JST。終了：04:04:23 JST。状態：完了（今回の部分点検）。追加はDEMO-0011の１件。既報0001～0010の完了状態を維持。ゲーム・既存テストコードの変更なし。
- AGENTS、README、点検・修正記録と現在の関係差分を確認した。先行03:30修正は03:34:21完了済み。アプリ一覧では同対象の別実行なし（このチャットのみactive、他はidle／notLoaded）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、プロセス停止なし。
- 全体statusはコマンド単位でLFSフィルターを無効にして読み取り、先行と同じNo.52のPNG８件候補、利用者のチャット文書、READMEと先行修正・記録を保持した。検証では先行修正済みの既存FolderVerse.dllを参照し、ゲーム本体を再ビルドしていない。
- 優先範囲：Game1.Demo.cs／DemoDomestic.csとDemoUi／DemoInspectorUiを起点に、直送便の退避復帰・左右インスペクターのDemo専用巡回が引き続き見当たらないことを確認。既存のUI単体検証でDemo全経路成功と扱わない。今回は前回記録の続き位置である世界構築と征服者選択へ進んだ。
- 検証：`DOTNET_CLI_TELEMETRY_OPTOUT=1`、`dotnet build artifacts/demo-click-audit/DemoAuditSetup.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers`：成功、警告・エラー０。`ALSOFT_DRIVERS=null`、`dotnet artifacts/corner-lesson/DemoAuditSetup.dll`：成功。18FPSの実際のUpdateDemoを進め、1080p／540pで８遷移と10画面を取得。出力先は既存artifacts/corner-lessonを再利用。
- タイトルからの全自然再生を新たに実施したわけではない。対象フェーズをfixtureで準備した後は、待ち時間を短縮せず自然な18FPS更新で進めた。ゲームソースに変更がないため、既存全回帰テストの重複実行はしていない。関係差分の行数は開始時から不変。セーブの読み書き、コミット・プッシュ・公開なし。
- 次回修正回：DEMO-0011を現在のコードで再現してから修正する。先行0006～0010の未コミット差分を保持する。
- 次回点検の続き：世界・人物抽選の停止／再抽選／確定の残りのクリック、戦闘・輸送・説得のクリック演出。直送便専用Demo巡回、全体540p自然再生、複数人間での自然再生は未確認のまま。

### 実行履歴：2026-11-01 05:00回（トリガー04:57）

- 開始：04:58:07 JST。終了：2026-11-01 05:00:38 JST。状態：完了（今回の部分点検）。新規不具合なし。既報0001～0011の完了状態を変更していない。
- 最初にAGENTS、README、点検・修正記録と関係差分を確認。先行04:30修正は04:40:06完了済み。HEADは43b1b14aada2ca849ba9cca32a8a6a56925b4ade。アプリ一覧で同対象の別実行なし（当該チャットのみactive、他はidle／notLoaded）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、プロセス停止なし。
- 全体差分はLFSフィルターをコマンド単位で無効にして読み取り。先行0006～0011のゲーム・テスト差分と新規ファイル、READMEと両記録、利用者のチャット文書、前回同様のNo.52のPNG８件候補を保持した。ゲーム・既存テストコードの変更なし。検証用FolderVerse.dllのSHA256は6C0EEB4AD2D813ADBDF5FBE6727B63829CC31C78CB21254083364D3C4B790090。
- Game1.Demo.cs、DemoDomestic.cs、DemoUi／DemoInspectorUiを起点に先行修正と現在の経路を照合。プライマリーの退避復帰・直送便左右インスペクターの専用Demo巡回は引き続き見当たらず、その範囲は未確認のまま。今回は前回の続き位置の世界・人物抽選の停止／再抽選／確定を進めた。
- 実施範囲：人間１人・コンピューター19人、通常のBeginRollingから、世界抽選３回（停止３回、再抽選２回、確定１回）→人物抽選３回（同）の合計12クリックを、1080p／540pで計24回確認した。各画面は18FPS（1/18秒刻み）で３秒の自然な待ち時間を進め、すべて55更新で遷移。実際のActionButton／RetryButton／CastAction／CastRetry矩形と_demoClickPointを照合し、24回すべて矩形内だった。区画権利抽選へ入るところまで確認した。
- 診断ソース：artifacts/demo-click-audit/ProbeRolling.cs.txt、プロジェクト：同DemoAuditRolling.csproj。ログ：同rolling-0500-output.txt。各遷移の対象矩形、クリック、次画面、更新回数、世界・人物Seedを記録した。抽選Seedは通常どおり変化し、固定値を結果として期待していない。
- 検証コマンド：DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditRolling.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditRolling.dll：成功、24件の矩形一致と遷移の検査を完走。既存ゲーム本体を参照し、ゲーム自体の再ビルドは行っていない。
- 画像は既存artifacts/corner-lesson/audit-rolling-0.png～25.png。８（1080p、人物の再抽選直後）と22（540p、人物の抽選停止直後）を開いて、操作位置と実画面の対応を目視確認した。全画像の目視確認を済ませたとはしない。関係するゲーム・既存テスト差分の行数は前後で不変。利用者のセーブの読み書き、コミット・プッシュ・公開なし。
- 修正回への引き継ぎ：本点検からの新規未解決項目はない。未解決項目がなければ修正しない。
- 次回点検の続き：戦闘・輸送・説得のクリック演出、直送便専用Demo経路の扱い。タイトルからの全体540p自然再生、全人数、任意FPSでの画像確認、起動中ゲームと併存する検証は未確認のまま。今回の成功を全経路正常とは扱わない。

### 実行履歴：2026-11-01 06:00回

- 開始：06:00:10 JST。終了：2026-11-01 06:02:50 JST。状態：完了（今回の部分点検）。新規不具合なし。既報0001～0011の完了状態を維持した。
- 最初にAGENTS、README、点検・修正記録と現在の差分を確認した。05:00点検は完了、05:30修正回は未解決０件のため無変更。HEADは43b1b14aada2ca849ba9cca32a8a6a56925b4ade。先行0006～0011のゲーム・テスト差分と新規ファイル、READMEと記録、利用者のチャット文書を保持した。
- アプリ一覧で同対象の別実行なし（当該チャットのみactive、他はidle／notLoaded）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、プロセス停止なし。ゲーム・既存テストコードへの変更なし。
- Game1.Demo.cs／DemoDomestic.cs、DemoUi／DemoInspectorUiと通常の処遇入力を起点に、前回の続きの戦後処遇のクリックと結果を点検した。４人格（3,9,11,13）×1080p／540pの８条件で、18FPS・３秒の待機後のクリックが実際のDispositionButtonの矩形内にあり、手下入りを拒否する場合は「逃がす」、承諾する場合は「手下にする」を選んで実際の状態へ反映されることを確認。全条件55更新で処理が成立した。
- 数値：逃がすは矩形(780,860,370,70)内の(967,895)、手下にするは矩形(380,860,370,70)内の(568,895)。前者はReleased=true・Superior=-1、後者はSuperior=0・Released=falseで、いずれもPending=0。クリック点と別の処理結果になる現象はこの８条件では見つからなかった。
- 検証用fixtureは人間１人・コンピューター19人、世界123・人物456・配置789。人格だけを対象の既存カタログ設定へ差し替え、捕虜１人を用意した。ソース：artifacts/demo-click-audit/ProbeDisposition.cs.txt、プロジェクト：同DemoAuditDisposition.csproj、ログ：同disposition-0600-output.txt。
- DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditDisposition.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditDisposition.dll：成功。既存ゲーム本体を参照し、ゲームの再ビルドはしていない。
- 実描画は既存artifacts/corner-lesson/audit-disposition-0.png～15.png。14（540p、承諾する人物の選択前）と15（選択後の挨拶）を開き、実際のボタンと手下入りの結果を目視確認。診断の挨拶画像はUpdateDispositionScene(1.3)を追加して取得したもので、その後８秒までの自然継続を確認したわけではない。
- ソース照合では、Demoの戦闘とTransportはUpdateDemo内の自動再生分岐で、通常のStartDemoClickへ届く前に戻る。内政のロボット説得はHandleInput側のUpdateHomePersuasionが先に時間進行する。したがって、これらの自動進行そのものを「見えない続行ボタンをクリックする不具合」とは扱わない。今回それぞれの全時間の実描画は未実施であり、正常と断定もしない。
- 優先項目のプライマリー・直送便の退避復帰・左右インスペクターについて、専用Demo巡回が引き続き見当たらないことを確認。別UIテストの成功でDemo確認済みにはしていない。関係するゲーム・既存テスト差分の行数は点検前後で不変。利用者のセーブの読み書き、コミット・プッシュ・公開なし。
- 修正回への引き継ぎ：新たな未解決項目なし。未解決項目がなければ変更しない。
- 次回点検の続き：戦闘導入・新聞・Transport・内政説得の自動遷移の実描画と、クリック演出が本当に出ていないかを時間経過で確認する。直送便専用Demo経路、全体540p自然再生、全人数、起動中ゲームとの併存検証は未確認のまま。

### 実行履歴：2026-11-01 07:00回（トリガー06:55）

- 開始：06:56:12 JST。終了：2026-11-01 06:58:07 JST。状態：完了（今回の部分点検）。新規不具合なし。既報0001～0011の完了状態を維持した。
- AGENTS、README、点検・修正記録と現在の差分を最初に確認。06:00点検は完了、06:30修正回は未解決０件のため無変更。HEADは43b1b14aada2ca849ba9cca32a8a6a56925b4ade。アプリ一覧では当該チャット以外に同対象の実行なし（他はidle／notLoaded）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、停止操作なし。
- Game1.Demo.cs／DemoDomestic.cs、DemoUi／DemoInspectorUiから、前回の続きである内政の説得の自動進行へ進んだ。通常のHandleInputを18FPSで実行し、Demoが有効な状態でも、工場タブを記憶していた地点で格納庫を開き、約８秒の説得表示後に通常のスライドで閉じることを1080p／540pで確認した。
- 検証用fixture：人間１人・コンピューター19人、世界123・人物456・配置789、PlayerSlot=0、所有する工場のある地点26。格納庫へ他国の完全体を加え、失敗のDomesticPersuasion表示イベントを１件用意した。成功確率や成功時の所属変更の再検証ではなく、表示とDemo入力の監査である。
- 全155更新／解像度にわたり、基準の_demoClickPoint=(123,456)が変化せず、_demoSwing=0のまま。開始0.111秒は格納庫表示・工場非表示、約４秒でも説得継続、8.0555秒で説得終了・閉じ始め、その後スライド０でインスペクターが消える。説得中に偽のDemoクリックを追加する現象は見つからなかった。通常入力の自動遷移であることを、UpdateDemo直呼びだけでなくHandleInput経由で確認した。
- ソース：artifacts/demo-click-audit/ProbePersuasion.cs.txt、プロジェクト：同DemoAuditPersuasion.csproj、ログ：同persuasion-0700-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPersuasion.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPersuasion.dll：成功。既存ゲーム本体を参照し、ゲーム本体の再ビルドなし。
- 実描画：既存artifacts/corner-lesson/audit-persuasion-0.png～7.png。１（1080p・約４秒）と５（540p・約４秒）を開き、格納庫と説得の吹き出し・人物が見えることを確認した。基準クリック点は診断の不変値であり、画像上でその座標を実際にクリックしたわけではない。
- プライマリー・直送便退避復帰・左右インスペクターについては、引き続き専用Demo巡回が見当たらず未確認。先行0006～0011のゲーム・テスト差分、新規ファイル、README・両記録、利用者の変更を保持した。関係するゲーム・既存テスト差分の行数は前後で不変。ゲームコード・既存テストへの変更、セーブの読み書き、コミット・プッシュ・公開なし。
- 修正回への引き継ぎ：新たな未解決項目なし。未解決項目がなければ変更しない。
- 次回点検の続き：Transport／戦闘導入／新聞の自動遷移の実描画と、クリック演出の有無を確認する。内政説得の成功時・複数イベントの表示、直送便専用Demo巡回、全体540p自然再生、全人数、起動中ゲームとの併存検証は未確認のまま。

### 実行履歴：2026-11-01 08:00回

- 開始：07:59:43 JST。終了：2026-11-01 08:02:11 JST。状態：完了（今回の部分点検）。新規不具合なし。既報0001～0011の完了状態を維持した。
- 最初にAGENTS、README、点検・修正記録と現在の差分を確認。07:00点検は完了、07:30修正回は未解決０件のため無変更。HEADは43b1b14aada2ca849ba9cca32a8a6a56925b4ade。アプリ一覧では同対象の別実行なし（当該チャットのみactive、他はidle／notLoaded）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、プロセス停止なし。
- Game1.Demo.cs／DemoDomestic.cs、DemoUi／DemoInspectorUiと通常のTransport・戦闘導入処理を起点に、前回の続きの自動遷移を調べた。優先項目の直送便退避復帰・左右インスペクターは専用Demo経路が引き続き見当たらず、その範囲は未確認のまま。
- 実施範囲は1080p／540p・18FPS、世界123・人物456・配置789、人間１人・コンピューター19人。Transportは転送経路なし・戦闘シーンなしのfixtureを作り、１秒では継続、２秒でWorldStatusへ進むことを確認した。経路のある配送・伝播移動の全演出を検証したわけではない。
- 戦闘はDemoUiと同じ形式の固定BattleStateを用意した。対象は自国の現在地点、守備０、１部隊・完全体ロボット１体・Chooseフレーム１件。FinishTransportでBattleへ入り、導入時計が０→１→6.5秒となり、クリックなしに導入を終えて盤面へ進むことを確認。実際の長い戦闘を決着まで進めたわけではない。
- 両解像度の全更新で基準_demoClickPoint=(123,456)が不変、_demoSwing=0のままだった。自動時間経過を進める分岐で、不適切なDemoのボタンクリックが新たに出る現象は見つからなかった。基準点は不変性の診断値であり、その場所を実際にクリックしてはいない。
- ソース：artifacts/demo-click-audit/ProbeBattleIntro.cs.txt、プロジェクト：同DemoAuditBattleIntro.csproj、ログ：同auto-battle-0800-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditBattleIntro.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、エラー０・警告１（診断ヘルパーTickがGame.Tickと同名というCS0108。ゲームコードの警告ではない）。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditBattleIntro.dll：成功。既存本体を参照し、ゲームの再ビルドなし。
- 実描画は既存artifacts/corner-lesson/audit-auto-battle-0.png～11.png。１（1080p・Transport１秒）と11（540p・戦闘導入終了）を開き、伝播の画面と戦場・新聞の表示を目視した。新聞を表示できることは確認したが、新聞の切替・全記事・戦闘結果・待ち時間による次画面は未確認であり、正常扱いしていない。
- 先行0006～0011のゲーム・テスト差分と新規ファイル、README・両記録、利用者の変更を保持した。関係するゲーム・既存テスト差分の行数は前後で不変。ゲームコード・既存テストへの変更、利用者のセーブの読み書き、コミット・プッシュ・公開なし。
- 修正回への引き継ぎ：新たな未解決項目なし。未解決項目がなければ変更しない。
- 次回点検の続き：実戦の新聞切替・結果・自動続行、経路のあるTransport。内政説得の成功時・複数表示、直送便専用Demo巡回、全体540p自然再生、全人数、起動中ゲームとの併存検証は未確認のまま。

### 実行履歴：2026-11-01 09:00回

- 開始：08:56:57 JST（heartbeat受信）。終了：2026-11-01 08:59:27 JST。状態：完了（部分点検）。新規不具合なし。DEMO-0001～0011の完了状態を維持。
- AGENTS、README、両記録、現在のGit差分を確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。アプリ一覧で同対象の別実行なし（当該チャットのみactive、他idle／notLoaded）、FolderVerse・dotnet・Demo・MSBuildプロセスなし。利用者の変更と既存の修正差分を保持し、停止・コミット・プッシュ・公開・セーブ読み書きなし。
- Game1.Demo.cs／DemoDomestic.cs、DemoUi／DemoInspectorUiを入口に、前回未確認の戦闘新聞・結果画面の自動続行を追跡。1080p／540p、18FPS、世界123・人物456・配置789、人間１人・CPU19人。既存の固定１部隊・１ロボット・Chooseフレーム１件のfixtureを使用した。
- 導入終了後の再生を一時停止したfixtureで約10秒後に新聞edition 0→1へ切り替わることを確認。再生再開後は５秒の結果待ちを経てsummaryへ進み、６秒ごとにsummaryReadPapers=1、2、3、edition=2、3、3となり、３回目でWorldStatusへ自動続行。全更新で基準_demoClickPoint=(123,456)、_demoSwing=0を維持し、時間経過をボタンクリックとして演出する現象なし。
- 診断ソース：artifacts/demo-click-audit/ProbeNewspaper.cs.txt、プロジェクト：DemoAuditNewspaper.csproj、ログ：newspaper-0900-output.txt（同フォルダー）。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditNewspaper.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditNewspaper.dll：成功。既存ゲームDLL参照のみで本体の再ビルドなし。
- 実描画：既存出力先のaudit-newspaper-0.png～19.png。４（1080p戦闘新聞）と17（540p結果新聞）を開き、盤面・新聞と選択中の新聞表示を確認。基準座標は入力していない不変性診断値。未完了の固定戦闘fixtureなので記事の「継戦」は想定どおりで、実戦の勝敗の正しさを検証したものではない。
- ゲームコード・既存テストは無変更。ゲーム・既存テストの差分行数は開始時と不変。診断用ファイルは既存artifacts配下を再利用し、点検記録だけ更新。新規未解決項目なし、修正回は未解決がなければ変更不要。
- 未確認／次回の続き：経路のあるTransport、実戦の勝敗確定を含む新聞記事と全７紙・周回境界、複数戦場。直送便の退避復帰・左右インスペクターの専用Demo巡回は引き続き見当たらず未確認。内政説得の成功時・複数表示、全体540p自然再生、全人数、利用者ゲームとの併存も未確認のまま。

### 実行履歴：2026-11-01 10:00回

- 開始：10:00:28 JST。終了：2026-11-01 10:01:18 JST。状態：完了（部分点検）。新規不具合なし、DEMO-0001～0011の完了状態を維持。
- 最初にAGENTS、README、両記録とGit差分を確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。先行点検は完了、09:30修正回は未解決なしで無変更。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。利用者の変更・既存修正を保持し、停止・セーブ操作・コミット・プッシュ・公開なし。
- Game1.Demo.cs／DemoDomestic.cs、DemoUi／DemoInspectorUiを入口に、前回の続きの「経路のあるTransport」を点検。直送便専用Demo操作はソース検索で引き続き見当たらず、退避復帰と左右インスペクターの専用巡回は未確認。今回のTransportは伝播フェーズであり、内政の直送便モードとは区別する。
- 1080p／540p・18FPS、人間１人＋CPU19人、世界1・人物2・配置3。PropagationCenterUiを参考に、展開図の未接続面をまたぐ実在隣接地点10→47（面1→4）のLastMarchesを１件用意し、通常のBeginPropagationとUpdateDemoを通した。配送予約の実行や戦闘結果生成を検証したものではない。
- 面接続中→カメラ中央合わせ→移動完了を確認。完了直後transportAge=0、その１秒後もTransport、２秒後にWorldStatusへ自動続行。全更新で基準_demoClickPoint=(123,456)、_demoSwing=0不変。時間経過中に不適切なクリック演出は出なかった。基準点で実際にクリックしたわけではない。
- 診断ソース：artifacts/demo-click-audit/ProbeRoute.cs.txt、プロジェクト：同DemoAuditRoute.csproj、ログ：同route-1000-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditRoute.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditRoute.dll：成功。既存ゲームDLL参照のみ、本体の再ビルドなし。
- 実描画：既存出力先audit-route-0.png～9.png。１（1080p中央合わせ）と８（540p移動完了後）を開き、展開図と進行バーを目視確認。ゲーム・既存テスト無変更、関係差分行数は開始時と不変。診断は既存artifacts配下、記録のみ追記。
- 修正回へ：新規未解決項目なし。未解決がなければ変更しない。
- 次回の続き／未確認：実戦の勝敗確定を含む新聞記事、全７紙の周回境界、複数戦場。配送・回収隊・女の子移動を含む複数伝播経路、内政説得の成功・複数表示、直送便専用Demo巡回、全体540p自然再生・全人数・利用者ゲームとの併存は未確認のまま。

### 実行履歴：2026-11-01 11:00回

- 開始：11:03:29 JST。終了：2026-11-01 11:04:12 JST。状態：完了（部分点検）。新規不具合なし。DEMO-0001～0011の完了状態を維持。
- 最初にAGENTS、README、点検・修正記録、Git差分を確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。10:00点検完了、10:30修正は未解決なしで無変更。同対象の別チャットはidle／notLoaded、FolderVerse・dotnet・Demo・MSBuildプロセスなし。既存差分と利用者変更を保持、停止・セーブ操作・コミット・プッシュ・公開なし。
- DemoのBattle分岐から前回未確認の新聞７紙の周回へ進んだ。1080p／540p・18FPS、人間１人＋CPU19人、世界123・人物456・配置789。固定１部隊・１体のBattleStateにFinished=true、Winner=0、Resultフレームを用意し、再生停止中にUpdateDemoを181更新ずつ通した。実戦を解決して勝者を生成した検証ではない。
- 各解像度でedition=0→1→2→3→4→5→6→0、profiles=7を確認。全更新で基準_demoClickPoint=(123,456)、_demoSwing=0を維持。約10秒の自動切替・周回で新しい偽クリックなし。09:00診断に残る期待値の%6は今回対象外だった部分で、今回の独立診断はNewspaperProfiles.All.Lengthを使用した。ゲームコードの不具合としては登録しない。
- 診断ソース：artifacts/demo-click-audit/ProbeNewspaperCycle.cs.txt、プロジェクト：同DemoAuditNewspaperCycle.csproj、ログ：同newspaper-cycle-1100-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditNewspaperCycle.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditNewspaperCycle.dll：成功。既存ゲーム本体参照、本体の再ビルドなし。
- 実描画：既存出力先audit-newspaper-cycle-0.png～15.png。６（1080p）と15（540p）を開き、勝利記事と新聞の表示を確認。撮影は切替直後なので旧紙がまだ見える（６は放送部、15はWANTED）。ログのeditionは切替先であり、７紙すべてのスライド完了後の目視確認と混同しない。全７紙の状態遷移は確認、全記事・写真の正しさは未確認。
- ゲームコード・既存テスト無変更、関連差分行数は開始時と不変。診断は既存artifacts配下を利用、点検記録だけ追記。修正回へ新規未解決項目なし。
- 次回の続き／未確認：複数戦場の自動切替と実戦からの結果、新聞スライド完了後の全紙描画。配送・回収隊・女の子移動を含む複数伝播経路、内政説得の成功・複数表示、直送便専用Demo巡回（専用経路が見当たらず未確認）、全体540p自然再生・全人数・利用者ゲームとの併存は未確認のまま。

### 実行履歴：2026-11-01 12:00回

- 開始：12:00:30 JST。終了：2026-11-01 12:01:07 JST。状態：完了（部分点検）。新規不具合なし、DEMO-0001～0011の完了状態を維持。
- AGENTS、README、両記録とGit差分を最初に確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。11:00点検完了、11:30修正は未解決なしで無変更。アプリ一覧で同対象の別実行なし（他idle／notLoaded）、ゲーム・dotnet・Demo・MSBuildプロセスなし。既存修正・利用者変更を保持、停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUiを入口に、前回の続きの複数戦場へ進んだ。1080p／540p、18FPS、人間１人＋CPU19人、世界123・人物456・配置789。固定BattleState２件（地点83と59、各１部隊１体）。第１戦場はturn0のChoose１フレーム、第２はturn0のChooseとturn1のResult。攻撃記録なし、移動のみとして扱われる再生fixtureで、実戦を解決したものではない。
- UpdateDemoを通して第１戦場・round0から、残っている第２戦場へwave=1／round=1として自動切替を確認。その後summaryへ進み、６秒ごとの新聞３回分でWorldStatusへ戻った。全更新で基準_demoClickPoint=(123,456)、_demoSwing=0不変、戦場切替や待ち時間に偽クリックは出なかった。
- ソース：artifacts/demo-click-audit/ProbeMultiBattle.cs.txt、プロジェクト：同DemoAuditMultiBattle.csproj、ログ：同multi-battle-1200-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditMultiBattle.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditMultiBattle.dll：成功。既存ゲームDLL参照、本体の再ビルドなし。
- 実描画：既存出力先audit-multi-battle-0.png～19.png。14（540p、第２戦場2/2・round2/2）と７（1080p、２戦場の結果一覧）を開いて確認。切替直後の新聞は旧戦場の記事がスライド中のため残る。未完了の固定fixtureなので「継戦」の表示は想定内。実戦の勝敗と記事内容は未確認。
- ゲーム・既存テスト無変更、関連差分行数は開始時と不変。診断は既存artifacts配下、点検記録のみ追記。新規未解決項目なし、修正回は対象がなければ変更しない。
- 次回の続き：攻撃を含む戦場間の５秒切替待ちと新聞スライド完了。実戦の勝敗生成、複数配送・回収隊・女の子の伝播、内政説得の成功・複数表示、直送便専用Demo巡回（専用経路が見当たらず退避復帰・左右は未確認）、全体540p自然再生・全人数・利用者ゲームとの併存も未確認のまま。

### 実行履歴：2026-11-01 13:00回

- 開始：13:01:31 JST。終了：2026-11-01 13:02:14 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録、現在差分を確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。先行点検完了、12:30修正は対象なしで無変更。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。既存差分と利用者変更を保持、停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に前回の複数戦場fixtureを拡張。1080p／540p・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。第１戦場のturn0へ短剣攻撃記録１件（対象なし）を足し、移動のみでない分岐の切替を調べた。実戦から攻撃・勝敗を生成した検証ではない。
- UpdateDemo経由でpendingFocus=1、wave=0となり、72更新後（約４秒）もwave=0、resultAge=3.9999999999999933を維持。その後５秒を満たすとwave=1／round=1へ切替。summaryに入り新聞３回分の時間経過でWorldStatusへ戻る。全更新で基準_demoClickPoint=(123,456)、_demoSwing=0不変。不適切なクリック演出なし。
- ソース：artifacts/demo-click-audit/ProbeAttackSwitch.cs.txt、プロジェクト：同DemoAuditAttackSwitch.csproj、ログ：同attack-switch-1300-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditAttackSwitch.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditAttackSwitch.dll：成功。既存ゲームDLL参照、本体の再ビルドなし。
- 実描画：既存出力先audit-attack-switch-0.png～19.png。14（540p、第２戦場）を開き盤面切替を確認。診断ログのsettledはDraw前の時計だけを判定しており、Drawが戦場変更を認識した時点で新聞スライドが始まる。画像には旧戦場の記事が残るので、新聞スライド完了の証拠として扱わない。この診断の限界を記録し、新しいゲーム不具合には登録しない。
- ゲーム・既存テスト無変更、関係差分行数は開始時と不変。既存artifacts配下を再利用し点検記録のみ追記。修正回へ新規未解決項目なし。
- 次回の続き：Drawで新聞切替を開始させてから更新・描画を交互に通し、スライド完了後の表示を確認。直送便の専用Demo経路が見当たらず退避復帰・左右インスペクターは未確認のまま。実戦の勝敗生成、複数配送・回収隊・女の子の伝播、内政説得の成功・複数表示、全体540p自然再生・全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 14:00回

- 開始：14:02:02 JST。終了：2026-11-01 14:02:48 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。先行点検完了、13:30修正は対象なしで無変更。アプリ一覧で同対象の別実行なし（他idle／notLoaded）、ゲーム・dotnet・Demo・MSBuildプロセスなし。利用者変更と既存修正を保持、停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に、前回未確認のDraw開始後の新聞スライドを点検。1080p／540p・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。13:00の２戦場・短剣攻撃記録１件の固定fixtureを使用。戦場切替後に一時停止し、Drawで第２戦場の新聞を作った後、UpdateDemo１回とDraw１回を18回交互に実行。
- 両解像度で_newspaperIssue.Battleが第２戦場の同一インスタンス、_newspaperSlideAge=1.0000000000000002（完了閾値.75秒以上）を確認。基準_demoClickPoint=(123,456)、_demoSwing=0不変。新聞の入替に不適切なクリック演出なし。
- ソース：artifacts/demo-click-audit/ProbePaperSettle.cs.txt、プロジェクト：同DemoAuditPaperSettle.csproj、ログ：同paper-settle-1400-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPaperSettle.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPaperSettle.dll：成功。既存ゲームDLL参照、本体の再ビルドなし。
- 実描画：既存出力先audit-paper-settle-0.png～47.png。23（1080p）と47（540p）を開き、盤面が第２戦場Point59で、新聞も#2陣営・パヴィトラ・緑の鍵となったことを目視確認。前回の旧戦場記事が残る画像とは区別し、今回はスライド完了後の証拠。全７紙・全記事・実戦の勝敗まで確認したわけではない。
- ゲームコード・既存テスト無変更、関係差分行数は開始時と不変。診断は既存artifacts配下、点検記録のみ追記。新規未解決項目なし、修正回は対象がなければ変更しない。
- 次回の続き：優先項目へ戻り、内政プライマリーと直送便の専用Demo経路・通常UI矩形の対応を再整理する（専用経路が見当たらず退避復帰・左右は未確認）。実戦の勝敗生成、複数配送・回収隊・女の子の伝播、内政説得の成功・複数表示、全体540p自然再生・全人数・利用者ゲームとの併存は未確認のまま。

### 実行履歴：2026-11-01 15:00回（トリガー14:56）

- 開始：14:56:03 JST。終了：2026-11-01 14:57:03 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と現在差分を最初に確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。先行点検完了、14:30修正は未解決なしで無変更。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。利用者変更・既存修正を保持、停止・コミット・プッシュ・公開なし。
- 優先範囲へ戻りGame1.Demo.cs／DemoDomestic.csとDemoUi／DemoInspectorUi、通常のMapOverlays・DirectDelivery・DirectPairMap／Transitionを照合。Demo段階12のTransportOverviewButtonはPrimaryActionBounds(4)で「長期輸送計画」、直送便はindex3（468,974,96,96）、モード解除×は(574,1002,40,40)。両者は別経路。直送便へ進む専用Demo段階は見当たらず、通常UIの検証とDemo巡回の網羅性を区別する。
- 通常の入力・描画は共通PrimaryActionBoundsを使い、index3以外はprimaryActionSlideによって下へ220px退避する。直送便中と復帰途中は他ボタンの旧位置・移動位置の入力を抑止。２地点画面の×はDirectPairBack=(932,110,56,48)、左右はAlignDirectPairInspectors、連続トラック選択はDirectPairTruckClick。ソース上の対応を整理し、固定座標を追加していない。
- 既存ゲームDLLのSHA256=6C0EEB4AD2D813ADBDF5FBE6727B63829CC31C78CB21254083364D3C4B790090を確認。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoInspectorUi.dll：成功（ログartifacts/demo-click-audit/primary-review-1500-output.txt）。現在の共通矩形で長期輸送計画・閉じる・わたしの国・移動と工場６プラン×２解像度を検証。
- 続けて既存dotnet artifacts/corner-lesson/DirectDeliveryUi.dll：成功（ログ同direct-review-1500-output.txt）。通常UIの退避復帰、移動中入力抑止、左右・連続トラッククリック、540p実画面からの変換、面接続・予約維持を確認。既存出力direct-17.pngを開き、540pの左右インスペクターと中央地図を目視。テストはメモリ内fixtureの保存復元も含むが利用者セーブへの書込なし。ゲーム本体・テストの再ビルドなし。
- ゲーム・既存テスト無変更、関連差分行数は開始時と不変。既存artifacts出力を再利用し、点検記録のみ追記。新規未解決項目なし、修正回は対象がなければ変更しない。
- 未確認／次回の続き：通常UIの直送便は確認したが専用Demo巡回は依然存在を確認できず、Demoでの退避復帰・左右・連続切替は未確認。次回は540pで自然再生する内政のクリック位置を段階ごとに追う。実戦勝敗生成、複数伝播、説得の成功・複数表示、全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 16:00回

- 開始：16:05:04 JST。終了：2026-11-01 16:06:11 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と現在差分を最初に確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。先行点検完了、15:30修正は対象なしで無変更。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。利用者変更・既存差分を保持、停止・セーブ書込・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUiを入口に、960×540、18FPS、人間１人＋CPU19人、世界123・人物456・配置789で内政段階０から進めた。UpdateDomesticSlideとUpdateDemoを１更新ずつDrawと交互に実行し、段階番号を途中で強制変更せず465更新・約25.8秒でTransportまで到達。タイトルから全経路を再生したわけではなく、通常HandleInput全体も通していない。
- クリック列（論理1920×1080座標）：段階0地点26=(960,552)、3格納庫=(773,230)、8工場=(377,230)、9製造=(196,332)、10プラン=(184,482)、11閉じる=(936,488)、12長期輸送計画=(622,1022)、16閉じる=(936,488)、17わたしの国=(102,1022)、18移動=(377,938)、13地点選択=(1620,273)、15予約=(707,938)、19ターン終了=(256,998)。これらは既存の共通矩形・ノード投影と対応し、ログのoffice／hangar／factory／transport／moveとTransportへの遷移を照合した。今回の初期配置では組立候補なしのため段階4・5は自然にスキップし未確認。
- ソース：artifacts/demo-click-audit/ProbeDomestic540.cs.txt、プロジェクト：同DemoAuditDomestic540.csproj、ログ：同domestic540-1600-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditDomestic540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditDomestic540.dll：正常終了、END frames=465 stage=0 screen=Transport。既存本体参照、本体の再ビルドなし。
- 描画はクリック発生時のみ既存出力先audit-domestic540-{frame}.pngへ13枚。245（長期輸送計画を開き始めた瞬間）と398（移動先選択直後）を開き、移動先Point90の実地点にカーソルが重なり、左に選択先と移動予約が出ることを確認。245はインスペクターがフレームインを始めた時点なので、完成後表示の証拠としない。
- ゲーム・既存テスト無変更、関連差分行数は開始時と不変。診断は既存artifacts配下、点検記録のみ追記。新規未解決項目なし、修正回は対象がなければ変更しない。
- 次回の続き：540pで組立可能な初期配置を用意し、同じ更新・描画交互の経路でパーツ移動を追う。直送便専用Demo巡回は見当たらず、Demoでの退避復帰・左右・連続切替は未確認。実戦勝敗、複数伝播、説得成功・複数表示、全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 17:00回

- 開始：17:04:35 JST。終了：2026-11-01 17:05:31 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。先行点検完了、16:30修正は対象なしで無変更。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。利用者変更・既存差分を保持、停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUi、DemoActionsUiを入口に前回スキップされた組立を点検。960×540、18FPS、世界123・人物456・配置789、人間１人＋CPU19人。メモリ内の自国地点26を頭・胴・脚の自国単独パーツ各１個に置換するfixtureを用意。段階０からUpdateDomesticSlide→UpdateDemo→Drawを１更新ずつ通し、途中で段階を強制変更せず206更新で工場直前の段階８まで進めた。
- 地点選択と格納庫を開いた後、段階4で胴(371,366)を押し、段階5で頭の枠(175,387)へ移動。状態は頭＋胴と単独脚となり、脚まで一度に組み上がらなかった。次に脚(567,404)から同じ枠へ移動し、２回目のドロップでComplete１体になった。いずれも論理1920×1080座標。実際の部位矩形と対象枠にカーソルが一致することを画像・ログで確認。
- ソース：artifacts/demo-click-audit/ProbeAssembly540.cs.txt、プロジェクト：同DemoAuditAssembly540.csproj、ログ：同assembly540-1700-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditAssembly540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditAssembly540.dll：正常終了、END frames=206 stage=8 screen=WorldStatus。既存ゲームDLL参照、本体の再ビルドなし。
- クリック時のみ既存出力先audit-assembly540-{frame}.pngへ６枚。106（胴を押す）、139（胴のみ移動後）、205（脚も移動後）を開き、部位と枠、最終完全体を目視確認。頭／胴／脚の通常分離ケースのみ。結合部位や損傷混在の自然再生、通常HandleInput全体、タイトルからの全経路は未確認。
- ゲーム・既存テスト無変更、関連差分行数は開始時と不変。診断は既存artifacts配下、点検記録のみ追記。新規未解決項目なし、修正回は対象がなければ変更しない。
- 次回の続き：組立の結合部位ケースを同じ更新・描画交互で確認。直送便専用Demo巡回は見当たらずDemoでの退避復帰・左右・連続切替は未確認。実戦勝敗、複数伝播、説得成功・複数表示、全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 18:00回（トリガー17:57）

- 開始：17:57:35 JST。終了：2026-11-01 17:58:28 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=43b1b14aada2ca849ba9cca32a8a6a56925b4ade。先行点検完了、17:30修正は対象なしで無変更。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。利用者変更・既存修正を保持、停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUi、DemoActionsUiを入口に結合部位ケースへ進んだ。960×540、18FPS、世界123・人物456・配置789、人間１人＋CPU19人。メモリ内自国地点26に頭１個とIntegratedParts=Body|Legsの結合パーツを置いたfixture。段階０からUpdateDomesticSlide→UpdateDemo→Drawを１更新ずつ通し、途中で段階を強制変更せず140更新で段階８まで進んだ。
- 地点選択・格納庫の後、段階4で胴の矩形(371,366)を押し、段階5で頭の枠(175,387)にドロップ。押す時点では頭と結合胴脚のまま、１回のドロップでComplete１体になり、分離脚の余分なクリックなし。実際の部位矩形と枠にカーソルが一致。前回の分離パーツの２回ドロップとの違いを確認した。
- ソース：artifacts/demo-click-audit/ProbeIntegrated540.cs.txt、プロジェクト：同DemoAuditIntegrated540.csproj、ログ：同integrated540-1800-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditIntegrated540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditIntegrated540.dll：正常終了、END frames=140 stage=8 screen=WorldStatus。既存ゲームDLL参照、本体の再ビルドなし。
- クリック時のみ既存出力先audit-integrated540-{frame}.pngへ４枚。106（結合胴脚を押す）と139（完全体へ移動後）を開き、結合の表示・部位・最終完全体を目視。論理1920×1080座標を540pへ描画した検証で、物理マウス入力の全経路ではない。
- ゲーム・既存テスト無変更、関連差分行数は開始時と不変。診断は既存artifacts配下、点検記録のみ追記。新規未解決項目なし、修正回は対象がなければ変更しない。
- 次回の続き：通常HandleInput経由の内政Demoで、インスペクター開閉とプライマリーの待ち時間を確認。直送便専用Demo巡回は見当たらずDemoでの退避復帰・左右・連続切替は未確認。損傷混在、実戦勝敗、複数伝播、説得成功・複数表示、タイトルからの全体540p再生・全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 19:00回

- 開始：19:02:36 JST。終了：2026-11-01 19:03:45 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と現在差分を最初に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。対象ゲーム・テスト・記録の未コミット差分なし。以前の修正は利用者側の385c59c4に取り込まれており、その後のHEADとの差分にゲーム・テスト変更なし。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUiを入口に、前回の続きの通常HandleInput経由を点検。960×540、18FPS、世界123・人物456・配置789、人間１人＋CPU19人、内政段階０から無入力のMouseState／KeyboardStateをHandleInputへ１更新ずつ渡しDrawと交互に実行。465更新でTransportへ到達し、インスペクター開閉とプライマリー操作の待ち時間で停止しなかった。タイトルからの全再生ではない。
- 16:00のUpdateDemo直呼びと同じ段階列・地点26→移動先90→予約→ターン終了を確認。工場の製造ボタンは今回は(340,332)で、直呼び時の(196,332)と異なるが、製造期間に対応する現在のFactoryProductionButtonへ追従しプランメニューが開いた。クリックは共通矩形・ノード投影に対応し、状態ログと遷移を照合。組立候補なしのため部位ドラッグは今回未確認。
- ソース：artifacts/demo-click-audit/ProbeNative540.cs.txt、プロジェクト：同DemoAuditNative540.csproj、ログ：同native540-1900-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditNative540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditNative540.dll：正常終了。既存ゲームDLL参照、本体の再ビルドなし。HEADのゲームソースは前回の検証対象と同じ。
- クリック時のみ既存出力先audit-native540-{frame}.pngへ13枚。139を開き、カーソルが工場の現在の製造ボタンを指し、プランが開いた状態を目視確認。ゲーム・既存テスト無変更、実行後も対象差分なし。点検記録のみ追記。新規未解決項目なし、修正回は対象がなければ変更しない。
- 次回の続き：通常HandleInput経由の組立可能fixtureで部位移動を確認。直送便専用Demo巡回は見当たらずDemoでの退避復帰・左右・連続切替は未確認。損傷混在、実戦勝敗、複数伝播、説得成功・複数表示、タイトルから全体540p再生・全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 20:00回

- 開始：20:00:07 JST。終了：2026-11-01 20:00:52 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と現在差分を最初に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は前回の点検記録10行のみで保持。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUi、DemoActionsUiを入口に、通常HandleInput経由の組立を確認。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。自国地点26に頭・胴・脚を各１個用意するメモリ内fixture。無入力MouseState／KeyboardStateをHandleInputへ渡し、更新とDrawを交互に通した。段階番号を途中で強制変更せず206更新で段階８へ到達。
- 段階０地点選択、３格納庫の後、胴(371,366)→頭の枠(175,387)、脚(567,404)→同じ枠の２回のドロップでComplete１体へ。最初のドロップ後は頭＋胴と単独脚のままで、２回目で完成。押下時の部位とドロップ後状態は17:00のUpdateDemo直呼びと同じだった。通常入力処理でドラッグや閉じる待ちが干渉する現象なし。
- ソース：artifacts/demo-click-audit/ProbeNativeAssembly540.cs.txt、プロジェクト：同DemoAuditNativeAssembly540.csproj、ログ：同native-assembly540-2000-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditNativeAssembly540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditNativeAssembly540.dll：正常終了。既存本体参照、本体再ビルドなし。
- クリック時のみ既存出力先audit-native-assembly540-{frame}.pngへ６枚。205を開き、頭の枠に完全体とカーソルが重なることを目視確認。ゲーム・既存テスト無変更、実行前後の差分は既存点検記録のみ。点検記録に追記、新規未解決項目なし。
- 次回の続き：通常HandleInput経由で地図切替を含む内政の２巡目を確認。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。結合部位の通常入力経由、損傷混在、実戦勝敗、複数伝播、説得成功・複数表示、タイトルから全体540p再生・全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 21:00回（トリガー20:57）

- 開始：20:57:38 JST。終了：2026-11-01 20:59:15 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録20行のみで保持。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に通常HandleInput経由で地図紹介から内政を再生。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人、demoMapVisits=0から開始。更新とDrawを交互に1350回、論理75秒進めた。途中の段階強制変更なし。地星儀／展開図切替と面接続→地点26→格納庫→工場→長期輸送計画→わたしの国→移動先90→予約→ターン終了→Transport→Battleまで進んだ。
- 地図クリックはframe37と190で(1766,160)、frame138で(1056,343)。その後の内政クリックは19:00と同じ実矩形に対応。frame756でTransport、frame1260でBattleを確認。予定した２巡目の内政までは今回の上限内に到達しなかったため未確認であり、停止や正常完了と断定しない。
- ソース：artifacts/demo-click-audit/ProbeMapNative540.cs.txt、プロジェクト：同DemoAuditMapNative540.csproj、ログ：同map-native540-2100-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditMapNative540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditMapNative540.dll：正常終了、END frames=1350 stage=0 screen=Battle。本体再ビルドなし。
- クリック時のみ既存出力先audit-map-native540-{frame}.pngへ保存。190を開き、地図切替ボタンにカーソルが重なることと展開図を目視。通常入力処理経由だがGame.Update全体やタイトルからの全再生ではない。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。新規未解決項目なし。
- 次回の続き：この診断の上限後のBattleから先を確認し、２巡目内政への到達を追う。Drawの時刻と診断入力の論理時計は別なので、時間評価には注意し一致させる。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。結合部位の通常入力、損傷混在、実戦勝敗、複数伝播、説得成功・複数表示、全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 22:00回

- 開始：22:03:09 JST。終了：2026-11-01 22:05:57 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録30行のみで保持。同対象の別チャットはidle／notLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に、前回の地図紹介からの診断を延長。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。入力のGameTimeとDrawのGameTimeを同じ論理時計へ揃え、通常HandleInputとDrawを１回ずつ交互に2700回（150秒）実行。途中の画面・段階強制変更なし。
- frame756で内政ターン終了、1260でBattle、1980でBattleResearch、2520でIndustry、2700終了時にWorldStatusへ帰還した。実際に生成された戦闘・戦訓・産業を通って内政画面へ戻ることを確認し、前回のBattleでの上限終了を停止不具合として登録しない。２巡目の内政操作はまだ段階０で、地点クリックまで確認できていない。戦闘中の全記事・勝敗内容の目視確認でもない。
- ソース：artifacts/demo-click-audit/ProbeLoopNative540.cs.txt、プロジェクト：同DemoAuditLoopNative540.csproj、ログ：同loop-native540-2200-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditLoopNative540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditLoopNative540.dll：正常終了、END frames=2700 stage=0 screen=WorldStatus。既存本体参照、本体再ビルドなし。
- 描画は毎更新実施、画像保存はクリック発生時のみ既存audit-loop-native540-{frame}.png。戦闘・戦訓・産業・帰還後はクリック発生がなくスクリーンショット保存対象外なので、今回はその画面の目視確認済みとはしない。状態遷移の証拠は上記ログ。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。新規未解決項目なし。
- 次回の続き：論理時計を揃えたこの経路を再利用し、2700以降の内政２巡目の地点選択まで進め、帰還時の画像も採取。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-01 23:00回（トリガー22:56）

- 開始：22:56:32 JST。終了：2026-11-01 22:58:55 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と差分を確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録40行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に、前回の通常HandleInput経由540p・18FPS・世界123／人物456／配置789・人間１人＋CPU19人の診断を延長。入力とDrawのGameTimeを同期し、毎更新描画。診断のIsFixedTimeStep=falseとしたが渡す論理elapsedは1/18秒固定。途中の画面・段階強制変更なし。
- 前回と同じ戦闘→戦訓→産業の後、frame2700はWorldStatus内の説得表示だった。画像を採取したことで、単なる内政帰還と説得中の内政画面を区別できた。説得表示を自動で終え、frame2804に２巡目の地点26（表示Point27）を(960,552)で選択、office=true／段階３へ進む。2805更新で正常終了。クリックは現在の地点投影に対応し、説得中に次の地点を誤ってクリックする現象なし。
- ソース：artifacts/demo-click-audit/ProbeReturnNative540.cs.txt、プロジェクト：同DemoAuditReturnNative540.csproj、ログ：同return-native540-2300-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditReturnNative540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditReturnNative540.dll：正常終了、END frames=2805 stage=3 screen=WorldStatus。既存本体参照、本体再ビルドなし。
- 既存出力先audit-return-native540-{frame}.pngへクリック時と2700を保存。2700を開き、他国地点の格納庫と説得の人物・吹き出しが見えることを確認。2804を開き２巡目の地点とカーソルを目視。地点インスペクターは開き始めなので完成状態の証拠ではない。説得成功率・色変更の正しさや全戦闘記事までは検証していない。
- ゲーム・既存テスト無変更、実行前後の差分は点検記録のみ。記録追記のみ、新規未解決項目なし。
- 次回の続き：２巡目で前回と異なる所在地・所有状況のインスペクターから、工場／長期輸送計画／移動の操作を追う。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、全人数・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 00:00回（トリガー11-01 23:58）

- 開始：2026-11-01 23:58:03 JST。終了：2026-11-02 00:00:51 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録51行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUiを入口に２巡目内政を点検。前回の540p・18FPS・世界123／人物456／配置789・人間１人＋CPU19人の経路を、同期したGameTimeで通常HandleInputとDrawを交互に実行。途中の画面・段階強制変更なし。１巡目、戦闘・戦訓・産業・説得を経て２巡目も工場→長期輸送計画→わたしの国→移動→予約→ターン終了まで進み、3262更新でTransportへ到達。
- ２巡目の製造クリックはframe2910で(484,332)、１巡目の(340,332)と異なる現在の製造期間の矩形へ追従。frame2943で実プラン選択、3016長期輸送計画(622,1022)、3056閉じる(936,488)、3096わたしの国(102,1022)、3136移動(377,938)、3195地点選択(1316,619)、3228予約(707,938)、3261ターン終了(256,998)。移動元がPoint90へ変わった後、選択先Point83の投影にカーソルが重なり、予約確認・次のTransportへ遷移した。
- ソース：artifacts/demo-click-audit/ProbeSecondNative540.cs.txt、プロジェクト：同DemoAuditSecondNative540.csproj、ログ：同second-native540-0000-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditSecondNative540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditSecondNative540.dll：正常終了、END frames=3262 stage=0 screen=Transport。既存本体参照、本体再ビルドなし。
- 画像は既存audit-second-native540-{frame}.pngへクリック時と2700を保存。2910（２巡目工場の製造ボタン）と3195（異なる移動元からPoint83選択）を開いて目視確認。ゲーム・既存テスト無変更、実行前後の差分は点検記録のみ。記録追記のみ、新規未解決項目なし。
- 次回の続き：人数・地図条件を変えた内政経路へ進む。同じ１人設定の繰返しだけで全人数の正常性は主張しない。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルからの全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 01:00回

- 開始：01:00:34 JST。終了：2026-11-02 01:02:01 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録61行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に人数条件を変更。960×540・18FPS、世界123・人物456・配置789、人間２人＋CPU18人。HumanPlayersを0、1に設定するメモリ内fixtureから、通常HandleInputとDrawを交互に実行。途中の画面・段階強制変更なし。人物選択画面から２人を選ぶ検証ではない。
- 第１人は地点26で従来の内政経路を進み、frame464ターン終了後もWorldStatusで次の人の内政へ。frame497に地点58を選び、第２人の工場・長期輸送計画・わたしの国・移動を進め、frame928の第２人ターン終了でTransportへ遷移した。929更新で正常終了。第１人(340,332)と第２人(268,332)で異なる製造期間のボタンに追従し、実プランメニューが開く。第２人の移動先は(1291,368)で表示Point62に対応、予約(707,938)とターン終了(256,998)も一致。
- ソース：artifacts/demo-click-audit/ProbeTwoHuman540.cs.txt、プロジェクト：同DemoAuditTwoHuman540.csproj、ログ：同twohuman540-0100-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditTwoHuman540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditTwoHuman540.dll：正常終了、END frames=929 stage=0 screen=Transport。既存本体参照、本体再ビルドなし。
- クリック時のみ既存audit-twohuman540-{frame}.pngへ保存。862を開き、第２人の緑色UI・現在地Point59・選択Point62と地点上のカーソルを目視確認。２人目の国へ切り替わっても前の人のクリック位置を使う現象なし。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。新規未解決項目なし。
- 次回の続き：別の世界Seedと回転した展開図の通常入力経路を確認。全人数は未確認。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルからの全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 02:00回

- 開始：02:03:35 JST。終了：2026-11-02 02:04:34 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録71行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUiを入口に地図条件を変更。960×540・18FPS、世界Seed42・人物456・配置789、人間１人＋CPU19人、展開図をScreenAngle=90度で開始。通常HandleInputとDrawを交互に通し、途中の段階強制変更なし。地点18選択(999,591)→格納庫→工場→長期輸送計画→わたしの国→移動→予約→ターン終了で、464更新後Transportへ到達。
- 製造ボタンは(268,332)へ追従しプランが開く。移動先選択(1495,415)は回転した展開図の表示Point87へ一致。予約(707,938)、ターン終了(256,998)も既存矩形に対応。90度回転を無視した旧座標へのクリックは見つからなかった。
- ソース：artifacts/demo-click-audit/ProbeRotated42.cs.txt、プロジェクト：同DemoAuditRotated42.csproj、ログ：同rotated42-0200-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditRotated42.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditRotated42.dll：正常終了、END frames=464 stage=0 screen=Transport。既存本体参照、本体再ビルドなし。
- クリック時のみ既存audit-rotated42-{frame}.pngへ13枚。397を開き、回転90度の表示とPoint87の選択・地点上カーソルを目視確認。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。記録追記のみ、新規未解決項目なし。
- 次回の続き：別Seedの地星儀で拡大・パンした初期状態から通常入力の地点選択を確認。全Seed・全人数は未確認。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 03:00回（トリガー02:56）

- 開始：02:56:36 JST。終了：2026-11-02 02:57:31 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と現在差分を確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録81行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に別Seedの地星儀を点検。960×540・18FPS、世界77・人物456・配置789、人間１人＋CPU19人。地星儀をZoom64、Pan(10,10)、任意回転Quaternion(1,2,3のYawPitchRoll)から開始するメモリ内fixture。通常HandleInputとDrawを交互に通し、途中の段階強制変更なし。Demoの視点調整後、地点5を(960,552)で選択し、格納庫→工場→長期輸送計画→わたしの国→移動→予約→ターン終了を経て465更新でTransportへ到達。
- 製造クリック(484,332)は現在の製造期間へ追従。移動先(1364,530)で表示Point33を選択、予約(707,938)とターン終了(256,998)へ進む。画面外の初期カメラをそのまま用いて空の地点をクリックする現象は見つからなかった。全拡大率・全方向の確認ではない。
- ソース：artifacts/demo-click-audit/ProbeGlobe77.cs.txt、プロジェクト：同DemoAuditGlobe77.csproj、ログ：同globe77-0300-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditGlobe77.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditGlobe77.dll：正常終了、END frames=465 stage=0 screen=Transport。既存本体参照、本体再ビルドなし。
- クリック時のみ既存audit-globe77-{frame}.pngへ13枚。398を開き、Point33選択と地点上カーソルを目視確認。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。記録追記のみ、新規未解決項目なし。
- 次回の続き：プライマリーの表示条件・初期タブ記憶など別条件を確認。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。全Seed・全人数、結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 04:00回

- 開始：04:00:07 JST。終了：2026-11-02 04:00:56 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と現在差分を最初に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録91行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に、初期タブ記憶を工場（_lastNodeTab=1）にした通常HandleInput経由の内政を点検。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。途中の段階強制変更なし。地点26を開くとDemoが監督タブを明示的に開き、格納庫・工場・長期輸送計画・わたしの国・移動・予約・ターン終了を進め465更新でTransportへ到達。工場記憶によってクリック対象が別タブへずれる現象なし。
- 段階３の(773,230)は格納庫タブ、段階９の(340,332)は現在製造期間の共通矩形、長期輸送計画(622,1022)と閉じる(936,488)、わたしの国(102,1022)、移動(377,938)が実遷移と対応。NodeFactoriesAllowedはTutorialStep!=1、TransportOverviewAllowedはNodeTransportAllowedかつTutorialStep!=2であることも静的に照合したが、制限付きチュートリアルをDemoが自然に通る検証ではない。
- ソース：artifacts/demo-click-audit/ProbeTab1Native540.cs.txt、プロジェクト：同DemoAuditTab1Native540.csproj、ログ：同tab1-native540-0400-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditTab1Native540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditTab1Native540.dll：正常終了、END frames=465 stage=0 screen=Transport。既存本体参照、本体再ビルドなし。
- クリック時のみ既存audit-tab1-native540-{frame}.pngへ13枚。73を開き格納庫タブ上のカーソルと格納庫表示を目視確認。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。新規未解決項目なし、記録追記のみ。
- 次回の続き：内政の初期インスペクターが閉じ途中などの待ち条件を点検。制限付きチュートリアル・全Seed／人数は未確認。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 05:00回

- 開始：05:02:38 JST。終了：2026-11-02 05:03:13 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と差分を最初に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録101行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に、初期インスペクターが閉じ途中の待ちを点検。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。自国地点の監督インスペクターを完全表示してCloseNodeInspectorを呼ぶfixtureから、通常HandleInputとDrawを交互に実行。
- frame0～6はclosing=true、slide=.8611113→.027778864、段階０のまま。frame7でclosing=false／slide=0となり、frame40に地点26を(960,552)で選んでoffice=true／段階３へ進んだ。41更新で終了。閉じ途中の早いクリックを検出する診断も成功。閉じる演出を待たずに背面地点をクリックする現象なし。
- ソース：artifacts/demo-click-audit/ProbeClosing540.cs.txt、プロジェクト：同DemoAuditClosing540.csproj、ログ：同closing540-0500-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditClosing540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditClosing540.dll：正常終了、END frames=41 stage=3 screen=WorldStatus。既存本体参照、本体再ビルドなし。
- 画像は既存audit-closing540-40.png。開いてPoint27の地点とカーソルを目視。クリック後のインスペクターは開き始めで完成表示の証拠ではない。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。新規未解決項目なし、記録追記のみ。
- 次回の続き：インスペクターが開き途中の待ち条件を同様に点検。制限付きチュートリアル・全Seed／人数、直送便専用Demo経路（見当たらず退避復帰・左右・連続切替は未確認）、結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 06:00回

- 開始：06:02:09 JST。終了：2026-11-02 06:02:43 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と差分を最初に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録111行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUiを入口に、インスペクターが開き途中の待ちを点検。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。地点26の監督を開いた直後・Demo段階３のfixtureから通常HandleInputとDrawを交互に実行。
- frame0～6はslide=.13888875→.97222114、段階３のままクリックなし。frame7でslide=1となり、frame39に格納庫タブ(773,230)をクリック、hangar=true／段階８へ進む。40更新で終了。開き途中に静止後の座標を早くクリックする現象なし。組立候補がないケースなので部位ドラッグは未確認。
- ソース：artifacts/demo-click-audit/ProbeOpening540.cs.txt、プロジェクト：同DemoAuditOpening540.csproj、ログ：同opening540-0600-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditOpening540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditOpening540.dll：正常終了。既存本体参照、本体再ビルドなし。
- 既存audit-opening540-39.pngを開き、格納庫タブとカーソル、格納庫表示を目視。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。新規未解決項目なし、記録追記のみ。
- 次回の続き：優先範囲の直送便通常UIとDemo非対応範囲を改めて照合し、残りの境界条件へ。直送便専用Demo経路は見当たらずDemoでの退避復帰・左右・連続切替は未確認。制限付きチュートリアル・全Seed／人数、結合部位の通常入力、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 07:00回（トリガー06:59）

- 開始：06:59:10 JST。終了：2026-11-02 06:59:34 JST。状態：完了（優先範囲の再照合）。新規不具合なし、既報0001～0011完了を維持。
- 最初にAGENTS、README、両記録と差分を確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録121行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiから優先範囲へ戻り、直送便と長期輸送計画を再照合。Demo段階12はPrimaryActionBounds(4)の長期輸送計画であり、直送便index3へ入る専用段階は引き続き見当たらない。直送便通常UIの確認結果を専用Demo巡回済みとは扱わない。
- 通常UIは入力・描画が同じPrimaryActionBoundsを参照。退避中の旧位置・移動位置、復帰中の開戦ボタン、完全退避後の表示と復帰後の元矩形をtests/DirectDeliveryUiと照合。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DirectDeliveryUi.dll：成功。ログartifacts/demo-click-audit/direct-priority-0700-output.txt。半分退避・復帰、非表示／移動中入力抑止、×とEscape、回転地図の左右、540pトラック連続切替・予約維持を実行。既存出力と本体を再利用、再ビルドなし。
- 既存direct-17.pngを開き、540pの左右インスペクターと中央地図・フェリーを目視確認。これらは通常UIのfixture。新規のDemoクリック経路を追加せず、ゲーム・既存テスト無変更。点検記録のみ追記、新規未解決項目なし。
- 次回の続き：未確認の結合部位を通常HandleInputで確認し、重複する成功条件の繰返しより未確認範囲を進める。直送便専用Demoは経路なしのため未確認のまま。制限付きチュートリアル・全Seed／人数、損傷混在、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 08:00回

- 開始：08:01:11 JST。終了：2026-11-02 08:01:46 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と差分を最初に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録130行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoUi／DemoInspectorUiを入口に、未確認だった結合部位の通常HandleInput経由を点検。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。自国地点26の頭とIntegratedParts=Body|Legsのfixtureから、無入力MouseState／KeyboardStateで通常入力処理とDrawを交互に実行。
- 地点選択→格納庫の後、frame106で胴(371,366)を押した時点では頭と結合胴脚のまま、frame139に頭の枠(175,387)へドロップしてComplete１体になった。１回のドロップ後に段階８へ進み、余分な脚クリックなし。140更新で正常終了。部位矩形・枠とカーソル、実パーツ状態を照合した。
- ソース：artifacts/demo-click-audit/ProbeNativeIntegrated540.cs.txt、プロジェクト：同DemoAuditNativeIntegrated540.csproj、ログ：同native-integrated540-0800-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditNativeIntegrated540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditNativeIntegrated540.dll：正常終了。既存本体参照、本体再ビルドなし。
- 画像はクリック時のみ既存audit-native-integrated540-{frame}.pngへ４枚。139を開き完全体と結合表示・枠上カーソルを目視。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。新規未解決項目なし、記録追記のみ。
- 次回の続き：損傷部位を含む組立候補の選択とクリックを確認。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。制限付きチュートリアル・全Seed／人数、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 09:00回（トリガー08:57）

- 開始：08:57:12 JST。終了：2026-11-02 08:57:54 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と現在差分を最初に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録140行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnet・Demo・MSBuildプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomestic、DemoUi／DemoInspectorUiを入口に損傷混在の組立を点検。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。地点26に損傷胴１個を先に置き、無傷の頭・胴・脚を追加するfixtureから通常HandleInputとDrawを交互に実行。
- frame106の(371,328)で頭を押し、139に最初の枠(175,387)へドロップ、172の(763,404)で脚を押し205に同じ枠へドロップ。Complete１体と単独胴１個を残し、206更新で段階８へ到達。画像上、完全体には損傷胴が残り、無傷の胴は別枠に残る。損傷品を避ける最適組合せの検証ではなく、損傷表示・枠順が変わっても実部位へクリックして組立が完了することを確認した。無傷自国パーツを含む条件と矛盾しないため、最適化の問題を新規不具合として登録しない。
- ソース：artifacts/demo-click-audit/ProbeDamaged540.cs.txt、プロジェクト：同DemoAuditDamaged540.csproj、ログ：同damaged540-0900-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditDamaged540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditDamaged540.dll：正常終了。既存本体参照、本体再ビルドなし。ログのfixture説明three disconnected partsは元診断の文字列が残るが、実際は損傷胴を含む４個体でありparts行とソースを証拠とする。
- クリック時のみ既存audit-damaged540-{frame}.pngへ６枚。205を開き、損傷表示付き完全体と残る胴、枠上カーソルを目視。ゲーム・既存テスト無変更、前後の差分は点検記録のみ。新規未解決項目なし、記録追記のみ。
- 次回の続き：組立不能（全部位損傷等）の候補なし経路を確認。直送便専用Demo経路は見当たらずDemoの退避復帰・左右・連続切替は未確認。制限付きチュートリアル・全Seed／人数、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 10:00回（トリガー09:56）

- 開始：09:56:13 JST。終了：2026-11-02 09:56:50 JST。状態：完了（部分点検）。新規不具合なし、既報0001～0011完了を維持。
- AGENTS、README、両記録と差分を最初に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存差分は点検記録150行のみで保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoInspectorUiを入口に、前回からの続きの全部位損傷・組立候補なし経路を点検。960×540・18FPS、世界123・人物456・配置789、人間１人＋CPU19人。地点26に損傷した頭・胴・脚を１個ずつ用意し、通常HandleInputとDrawを交互に実行。
- frame33で地点(960,552)、73で格納庫タブ(773,230)をクリック。全部位が単独のまま、段階３から８へ移り74更新で終了。実部位の押下／ドロップを偽装せず、候補なしとして工場へ進む段階に到達した。audit-all-damaged540-73.pngを開き、３部位の損傷表示と格納庫タブ上のカーソルを目視確認。
- ソース：artifacts/demo-click-audit/ProbeAllDamaged540.cs.txt、プロジェクト：同DemoAuditAllDamaged540.csproj、ログ：同all-damaged540-1000-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditAllDamaged540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditAllDamaged540.dll：正常終了。本体再ビルドなし、既存出力先利用。ゲーム・既存テスト無変更、記録のみ追記。
- 今回は格納庫の候補なし分岐まで。優先範囲のプライマリー・直送便通常UIは07:00回からコード差分なし、今回は再実行していない。直送便専用Demo経路がない点と、その退避復帰・左右・連続切替のDemoとしての確認は引き続き未確認。
- 次回の続き：制限付きチュートリアルで工場／輸送が制限された場合のDemo経路を静的照合し、可能なfixtureから実描画を確認する。全Seed／人数、戦闘結果内容・複数伝播・説得成功／複数表示、タイトルから全体再生・利用者ゲームとの併存も未確認。

### 実行履歴：2026-11-02 11:00回（トリガー10:57）

- 開始：10:57:44 JST。終了：2026-11-02 10:58:50 JST。状態：完了（部分点検）。新規不具合なし。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録160行の差分を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。ゲームコード・既存テスト・利用者セーブ無変更、停止・コミット・プッシュ・公開なし。
- 前回の続きとしてチュートリアル制限を静的照合。NodeFactoriesAllowedはTutorialStep!=1、輸送一覧はさらに!=2。BeginTutorialStepOneは_demoActive=falseにする。通常Demoはタイトルから人数設定・世界抽選へ進み、WorldSetup.SetWorldはTutorialStep=0へ戻すため、制限付きチュートリアルとDemoを同時に強制したfixtureを通常経路の不具合証拠とはしない。制限状態での実描画は今回未実施。
- 優先範囲は既存出力を再利用し、ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoInspectorUi.dllを実行、正常終了。ログartifacts/demo-click-audit/inspector-1100-output.txt。1080p／540pで工場６プランの実矩形・選択後manufacturing、地点監督・閉じる演出・長期輸送プライマリー・わたしの国経由の移動・人数確認を照合。生成されたdemo-office-1.pngを開き描画確認。存在しないdemo-office-73.pngの閲覧は失敗し、実在するファイルを選び直した。ゲーム本体再ビルドなし。
- 直送便専用Demo経路は引き続き見当たらず、通常直送便の退避復帰・左右をDemoとして検証済みとは扱わない。次回は説得成功時の表示とDemo再開経路へ進む。全Seed／人数、戦闘結果内容・複数伝播、タイトルから全体再生・利用者ゲームとの併存も未確認。記録追記のみ、既報0001～0011完了を維持。

### 実行履歴：2026-11-02 12:00回（トリガー12:04）

- 開始：12:04:15 JST。終了：2026-11-02 12:05:22 JST。状態：完了（部分点検）。AGENTS、README、両記録・現在差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録167行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。利用者ゲーム・セーブ操作、コミット・プッシュ・公開なし。
- Demo／通常HandleInputの説得割込みを点検。既存ProbePersuasionから成功結果の表示用fixtureを派生し、Beforeの他国ロボットとOwner=0のAfter、Success=trueを投入。成功抽選・パーツ色変更そのものの検証ではない。世界123・人物456・配置789、1080p／540p・18FPSで、製造工場を開いた状態から格納庫へ切り替わり、４秒後の成功表示、８秒後の自動閉鎖を確認。期間中Demoクリック点(123,456)とswing=0を維持し、余計なクリックなし。
- ソースartifacts/demo-click-audit/ProbePersuasionSuccess.cs.txt、同DemoAuditPersuasionSuccess.csproj、ログ同persuasion-success-1200-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPersuasionSuccess.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPersuasionSuccess.dll：正常終了。本体再ビルドなし。最初の70更新では浮動小数点で４秒未満だったため71にして表示境界を越えて再実行。画像audit-persuasion-success-5.pngを開き成功の吹き出しを確認。
- ゲーム・既存テスト無変更、記録のみ追記。新規不具合なし、既報0001～0011完了を維持。優先UIは前回からコード差分なし、今回は再実行なし。直送便専用Demo経路は依然未確認。今回の診断は閉鎖完了までで、次回は説得後のDemo地点クリック再開を確認。複数説得・全Seed／人数・戦闘結果内容・タイトル全体再生・利用者ゲーム併存も未確認。

### 実行履歴：2026-11-02 13:00回（トリガー12:59）

- 開始：12:59:17 JST。終了：2026-11-02 12:59:49 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録174行の差分を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticとDemoInspectorUiを入口に、前回の成功説得表示fixtureを閉鎖後まで延長。1080p／540p・18FPS、世界123・人物456・配置789。_demoMapVisits=1で内政経路を選び、説得８秒間のクリック抑止と閉鎖完了後の地点26クリック(960,552)、段階０→３への再開を通常HandleInputで確認した。閉鎖中のクリックはなく、最後のswing=.4は再開した実地点クリック。audit-persuasion-resume-7.pngを開き地図上のカーソルを確認。再開直後は新しいインスペクターが開く演出の開始時点であり、その後の格納庫までは今回未実施。
- ソースartifacts/demo-click-audit/ProbePersuasionResume.cs.txt、同DemoAuditPersuasionResume.csproj、ログ同persuasion-resume-1300-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPersuasionResume.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPersuasionResume.dll：正常終了。本体再ビルドなし。ログ末尾の既存PASS文は説得中の抑止を指し、RESUMED行がその後のクリック証拠。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。優先UIにコード差分なし、今回は再実行なし。次回は複数説得結果を同時表示する場合の割込み・閉鎖・再開を確認する。直送便専用Demo、全Seed／人数、戦闘結果内容・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-02 14:00回

- 開始：14:00:18 JST。終了：2026-11-02 14:00:42 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録181行の差分を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticの割込み経路の続きとして、同一地点26に成功１体・失敗１体の説得結果を投入した表示fixtureを実行。世界123・人物456・配置789、1080p／540p・18FPS。通常HandleInputで格納庫へ切替、４秒後に成功した個体だけ成功表示、８秒後閉鎖、閉鎖完了後に地点(960,552)クリックで段階３へ再開。説得中はクリック点(123,456)とswing=0を維持。画像audit-persuasion-mixed-5.pngを開き、失敗個体と成功表示の個体が別枠であることを確認。抽選確率・複数地点の説得は今回の検証対象外。
- ソースartifacts/demo-click-audit/ProbePersuasionMixed.cs.txt、同DemoAuditPersuasionMixed.csproj、ログ同persuasion-mixed-1400-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPersuasionMixed.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPersuasionMixed.dll：正常終了。本体再ビルドなし、既存出力先使用。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。優先プライマリー・直送便通常UIはコード差分なし、今回再実行なし。直送便専用Demo経路は未確認のまま。次回は複数地点の説得結果を扱う表示経路を静的照合する。全Seed／人数・戦闘結果内容・タイトル全体再生・利用者ゲーム併存も未確認。

### 実行履歴：2026-11-02 15:00回（トリガー14:58）

- 開始：14:58:49 JST。終了：2026-11-02 14:59:08 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録188行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- 前回の複数地点説得について静的照合。PersuadeAtHomeは各rulerのCurrent(ruler)１地点からイベントを生成し、PrepareHomePersuasionはPlayerSlotに一致するrulerだけを表示する。同一ターンの重複生成はDomesticPersuasionTurnsで抑止する。したがって異なる地点のイベントを同一rulerの表示リストへ人工投入する診断は通常生成経路と異なるため実行しない。複数rulerの順番をまたぐ表示は未確認として残す。
- 優先範囲へ戻りDemoDomestic段階12のTransportOverviewButton（長期輸送）と通常直送便の区別を再確認。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DirectDeliveryUi.dll：正常終了。ログartifacts/demo-click-audit/direct-priority-1500-output.txt。540pの回転面接続、退避／復帰中入力抑止、左右インスペクター、連続トラッククリック、予約保持、カメラ遷移を確認。direct-17.pngを開き左右と地図の描画を目視。本体再ビルドなし、既存出力利用。これは通常直送便UIのfixtureであり、専用Demo経路の巡回済みとは扱わない。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。次回は未確認の戦闘結果画面から内政へ戻るクリック／自動遷移の分岐を再確認する。直送便専用Demo、全Seed／人数、複数ruler説得、タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-02 16:00回（トリガー15:57）

- 開始：15:57:50 JST。終了：2026-11-02 15:58:17 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録195行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticと既存DemoUiを入口に戦闘後の分岐を静的照合。BeginRobotBattleの場面なし分岐は戦訓PendingがあればBattleResearch、なければIndustry。BeginBattleResearchは取得なし・通常ゲームなら即Industry、取得ありはページごとに新規７秒／既知0.4秒で進む。DemoはUpdateBattleResearch(elapsed,false)を呼び、ここではボタンをクリックしない。新聞巡回と戦訓の自動遷移を混同せず、今回は戦訓あり／なしの実描画fixtureは未実施とする。
- ALSОFT_DRIVERSではなく通常のALSOFT_DRIVERS=nullを設定し、dotnet artifacts/corner-lesson/DemoUi.dll：正常終了。ログartifacts/demo-click-audit/demo-1600-output.txt。開始待機、地星儀／展開図、18FPSの面接続、拒否分岐、終了ループの既存検証を再実行。本体再ビルドなし。今回は新しい画面画像の目視確認は行っていないため、戦闘後の実描画確認済みとはしない。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。優先UIにコード差分なし、直送便は前回確認から変更なし。次回は戦訓なし／ありの自動遷移を対象画面fixtureから実描画・通常HandleInputで確認する。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-02 17:00回（トリガー16:59）

- 開始：16:59:51 JST。終了：2026-11-02 17:00:38 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録202行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticと既存診断を入口に戦訓ありの自動遷移を確認。世界123・人物456・配置789、人間１＋CPU19、960×540・18FPS。Lessons.Pendingへ新規戦訓All[0]を１件入れBeginBattleResearchから通常HandleInputとDrawを実行。開始はBattleResearch。戦訓画面でクリックせず、後にWorldStatusへ戻り、frame159で地点(960,552)をクリック、199で格納庫タブ(773,230)をクリックし段階８へ到達した。audit-lessons540-20.pngを開き戦訓表示を目視。
- ソースartifacts/demo-click-audit/ProbeLessons540.cs.txt、同DemoAuditLessons540.csproj、ログ同lessons540-1700-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditLessons540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditLessons540.dll：正常終了。本体再ビルドなし、既存出力先使用。Industry終了条件を設定したが、通常入力内で即WorldStatusへ進み200更新上限で終了したため、Industryの描画自体・境界フレームは未確認。遷移先を想定して判定せず実ログを記録した。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。優先UIは直前までコード差分なし。次回は戦訓なしの分岐と、既取得戦訓の短時間表示を確認。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-02 18:00回（トリガー17:55）

- 開始：17:55:52 JST。終了：2026-11-02 17:56:27 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録209行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticを入口に戦訓なしの分岐を確認。世界123・人物456・配置789、人間１＋CPU19、960×540・18FPS。Pending空でBeginBattleResearchを呼ぶと、その呼出し内でWorldStatusまで進んだ。通常HandleInputとDrawを200更新実行し、frame32地点(960,552)、72格納庫タブ(773,230)、105工場タブ(377,230)、138製造ボタン(268,332)、171プラン(184,482)へ進むことを確認。audit-no-lessons540-20.pngを開き内政への復帰描画とプライマリー表示を目視。Industryは即時進行のため独立した描画は今回も未確認。
- ソースartifacts/demo-click-audit/ProbeNoLessons540.cs.txt、同DemoAuditNoLessons540.csproj、ログ同no-lessons540-1800-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditNoLessons540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditNoLessons540.dll：正常終了。本体再ビルドなし、既存出力先使用。
- 既取得戦訓はResearchが同じ所有者の既取得を除外するため、短時間表示を検証するなら他国で既知・受取国では新規という条件が必要。今回は未実施とし次回へ引き継ぐ。新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-02 19:00回

- 開始：19:00:23 JST。終了：2026-11-02 19:00:57 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録216行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticと前回診断から、他国で既知・受取国では新規の戦訓を確認。世界123・人物456・配置789、960×540・18FPS。Owned[1]にAll[0]、PendingにOwner0の同戦訓を設定しBeginBattleResearchから通常HandleInputとDrawを200更新。短時間の戦訓画面でクリックせずWorldStatusへ進み、工場の段階11まで再開した。audit-known-lessons540-2.pngを開き、通常の戦訓カード取得表示と次へボタンを目視確認。新規発見の７秒表示とは異なる分岐を確認した。
- ソースartifacts/demo-click-audit/ProbeKnownLessons540.cs.txt、同DemoAuditKnownLessons540.csproj、ログ同known-lessons540-1900-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditKnownLessons540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditKnownLessons540.dll：正常終了。本体再ビルドなし、既存出力先使用。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。優先UIにコード差分なし、今回は再実行なし。次回は複数戦訓ページで新規／既知が混在したときの自動ページ送りを確認する。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-02 20:00回（トリガー20:04）

- 開始：20:04:24 JST。終了：2026-11-02 20:04:55 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録223行を保持。同対象の別チャットはidle／notLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticと前回診断から、既知／新規の戦訓が２ページ混在する場合を確認。世界123・人物456・配置789、960×540・18FPS。Owned[1]にAll[0]、PendingにOwner0のAll[0]とAll[1]を設定し、通常HandleInputとDrawを200更新。画像audit-mixed-lessons540-20.pngを開き、自動で新規の戦訓#2へ移った表示を確認。戦訓中にDemoクリックせず、frame166に地点(960,552)クリックで内政段階３へ再開。新規のみの前回frame159より７更新遅れ、既知ページ分の待ちと整合する。
- ソースartifacts/demo-click-audit/ProbeMixedLessons540.cs.txt、同DemoAuditMixedLessons540.csproj、ログ同mixed-lessons540-2000-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditMixedLessons540.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditMixedLessons540.dll：正常終了。本体再ビルドなし、既存出力利用。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。優先UIはコード差分なし、今回は再実行なし。次回は優先のプライマリーとインスペクター経路へ戻り、未確認の解像度条件を検討する。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-02 21:00回

- 開始：20:59:55 JST。終了：2026-11-02 21:00:48 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=6efdb3dd01927c09d87c1df9e1b2f4cc133eb803。既存点検記録230行を保持。同対象の別チャットはidle／notLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo／DemoDomesticを入口に未確認だった1280×800（16:10）・18FPSで内政１巡を実行。世界123・人物456・配置789、人間１＋CPU19、通常HandleInputとDraw。地点・格納庫・工場・長期輸送一覧・閉じる・わたしの国・移動先・予約確定・ターン終了を経て465更新でTransportへ到達。frame398の移動先(1620,273)、431の確定(707,938)、464のターン終了(256,998)を論理座標で記録。audit-native800-464.pngを開き輸送フェーズへの遷移を目視。存在しない266の画像参照は失敗し、実在464を確認した。
- ソースartifacts/demo-click-audit/ProbeNative800.cs.txt、同DemoAuditNative800.csproj、ログ同native800-2100-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditNative800.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditNative800.dll：正常終了。本体再ビルドなし、既存出力先使用。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。16:10の物理マウス座標変換そのものは無入力Demo診断では未確認。直送便専用Demoも未確認。次回は同解像度で長期輸送プライマリーのクリック画像を実矩形と照合する。全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存も未確認。

### 実行履歴：2026-11-02 22:00回（トリガー21:57）

- 開始：21:57:26 JST。終了：2026-11-02 21:58:38 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6aへ更新済み、関係する未コミット差分なし。同対象の別チャットはidle／notLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- 前回HEADから通常入力・直送便・格納庫・描画と関連テストに変更があるため、旧DLLを更新後コードの証拠に流用せず再ビルドした。Demo／DemoDomestic自体の変更はなく、優先のDemoInspectorUiを現在の本体で確認。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoInspectorUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ログartifacts/demo-click-audit/rebuild-2200-output.txt。既存出力先を使用。
- ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoInspectorUi.dll：正常終了。ログ同inspector-2200-output.txt。1080p／540pで工場６プランの現矩形と選択結果、監督・閉じる演出・長期輸送プライマリー・わたしの国経由移動・人数確認・実描画を照合。demo-office-1.pngを開き更新後の監督インスペクター描画を確認。ゲーム・既存テスト無変更、終了時も関係差分なし、点検記録のみ追記。
- 新規不具合なし、既報0001～0011完了を維持。予定していた1280×800画像の追加照合は本体更新の再検証を優先し未実施。次回は更新されたDirectDeliveryUiを本体とともにビルド・実行し、退避復帰・左右を確認する。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-02 23:00回（トリガー22:55）

- 開始：22:55:27 JST。終了：2026-11-02 22:55:58 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録７行の差分を保持。同対象の別チャットはidle／notLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- 更新後の優先範囲としてDirectDeliveryUiを現ソースからビルド。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DirectDeliveryUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功。ログartifacts/demo-click-audit/direct-build-2300-output.txt。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DirectDeliveryUi.dll：正常終了。ログ同direct-2300-output.txt。既存出力先使用。
- ボタン比率、退避／復帰・移動中入力抑止、×／Escape、540pの回転面接続、左右インスペクター、連続トラッククリック、予約保持とカメラ遷移を確認。更新後のdirect-17.pngを開き左右と地図を目視。DemoDomestic段階12は引き続き長期輸送一覧で、通常直送便UIテストを専用Demo巡回とは扱わない。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。次回は更新後本体で1280×800の長期輸送プライマリー画像と実矩形の照合へ戻る。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 00:00回（トリガー00:04）

- 開始：00:04:58 JST。終了：2026-11-03 00:05:44 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録14行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- 更新後本体で1280×800・18FPSの内政Demoを再実行。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditNative800.dll：正常終了、465更新でTransport。ソースはartifacts/demo-click-audit/ProbeNative800.cs.txt、ログ同native800-0000-output.txt。診断と現在本体の既存出力を再利用し再ビルドなし。
- frame245の長期輸送プライマリー(622,1022)を、TransportOverviewButton=PrimaryActionBounds(4)と照合。audit-native800-245.pngを開き、画面下のカーソルを確認。一覧は開く演出開始直後でまだ画面左外にあり、この画像だけでは一覧内部の表示を確認できない。ログのtransport=Trueで開く状態を照合した。16:10では論理1920×1080を縮小し上下に余白を持つ描画であり、クリック演出の座標を照合した。通常HandleInput経由で一覧を開閉し、わたしの国・移動予約・ターン終了まで進んだ。物理マウス座標変換の検証とは区別する。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。直送便通常UIは前回検証からコード変更なし。次回は1280×800で実マウス相当の入力座標変換を既存検証から確認する。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 01:00回（トリガー00:55）

- 開始：00:55:59 JST。終了：2026-11-03 00:56:31 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録21行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・利用者セーブ操作・コミット・プッシュ・公開なし。
- Demo優先範囲の通常直送便入力について、現DirectDeliveryUiの診断コピーでframe6以降の解像度を1280×800へ変更。物理座標をCanvasBounds経由で作る既存Physical入力をそのまま用い、面接続・ペア側面接続・連続トラッククリック・予約維持・戻り遷移が成功。direct800-17.pngを開き上下40pxの余白、左右インスペクターと地図を確認。通常UIの検証であり専用Demo巡回ではない。前半のプライマリー退避テストは元の解像度のままで、1280×800で全テストをしたとは扱わない。
- ソースartifacts/demo-click-audit/ProbeDirect800.cs.txt、同DemoAuditDirect800.csproj、ログ同direct800-0100-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditDirect800.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditDirect800.dll：正常終了。本体再ビルドなし、既存出力先使用。ログの540pというPASS文は元診断の文字列が残るため、実解像度の証拠はソースと1280×800画像を参照。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。次回は16:10でプライマリー退避復帰の物理入力も確認する。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 02:00回（トリガー02:03）

- 開始：02:03:00 JST。終了：2026-11-03 02:03:50 JST。状態：中断（診断例外）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録28行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- Demo優先のプライマリーを通常UIの矩形と照合。1280×800の診断コピーに６ボタンの物理中心→CanvasPoint→元ヒット矩形内の往復確認を追加したが、診断側の反射呼出しで例外になり実行未完了。退避復帰テスト自体はMapOverlayClickを直接呼ぶ既存形式であり、物理クリックから退避開始までの完全な入力経路を検証済みとはしない。前回引継ぎの物理入力は座標変換までの部分確認とする。
- ソースartifacts/demo-click-audit/ProbePrimary800.cs.txt、同DemoAuditPrimary800.csproj、ログ同primary800-0200-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPrimary800.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPrimary800.dll：実行失敗（ログ参照）。本体再ビルドなし、既存出力先使用。新しい描画の目視確認は今回は行っていない。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。次回はプライマリーの物理押下と解放から退避・復帰を開始する通常HandleInput経路を確認する。直送便専用Demo・全Seed／人数・複数ruler説得・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 03:00回（トリガー02:56）

- 開始：02:56:01 JST。終了：2026-11-03 02:56:45 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録35行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- 前回の診断例外はstaticのPrimaryActionHomeBoundsをInstanceだけの反射で探したことが原因。ignored診断ProbePrimary800.cs.txtのBindingFlagsへStaticを追加し再実行。1280×800の６プライマリー中心を物理座標からCanvasPointへ戻すと全て対応する元矩形内に入り、既存の直送便遷移診断も正常終了した。ゲーム側の修正は不要。前回中断の履歴は保持し、この実行結果で座標変換の部分確認を補完する。
- DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPrimary800.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPrimary800.dll：正常終了。ログartifacts/demo-click-audit/primary800-0300-output.txt。終了コードを保存して実行成功を確認。本体再ビルドなし、既存出力先使用。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。新しい描画目視は今回未実施。次回は物理押下／解放から退避復帰を始める完全な通常HandleInput経路を確認する。今回の６中心の往復確認だけをその成功とは扱わない。直送便専用Demo・全Seed／人数・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 04:00回

- 開始：04:01:02 JST。終了：2026-11-03 04:01:45 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録42行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- 前回の続きとして1280×800の通常HandleInput経由の物理押下／解放を追加。直送便を閉じて復帰済みにしたfixtureから、PrimaryActionHomeBounds(3)中心をCanvasBoundsで物理座標へ変換し、解放→押下→解放で直送便が開くこと、0.3秒進めて他のプライマリーが画面外になること、×の物理クリックで閉じ、さらに0.3秒後_primaryActionSlide=0へ戻ることを確認。論理矩形への直接MapOverlayClickでこの追加確認を代用していない。これは通常UIの入力診断であり専用Demo経路ではない。
- ソースartifacts/demo-click-audit/ProbePrimaryInput800.cs.txt、同DemoAuditPrimaryInput800.csproj、ログ同primary-input800-0400-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPrimaryInput800.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPrimaryInput800.dll：正常終了、追加PASSと終了コード０を確認。本体再ビルドなし、既存出力先使用。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。追加した退避中の中間描画は今回画像確認していない。次回はその中間描画と復帰後表示を確認する。直送便専用Demo・全Seed／人数・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 05:00回

- 開始：05:02:03 JST。終了：2026-11-03 05:02:34 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録49行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- 前回の物理クリック診断へ、直送便を開いて0.125秒後のDrawと画像保存を追加。1280×800のprimary800-half.pngを開き、直送便ボタンと×が残り、他のプライマリーが下へ移動して画面端に切れる中間描画を確認。その後0.175秒で完全退避し、物理×クリックと0.3秒後の復帰状態のチェックも成功。復帰後の画像自体は今回目視していない。通常UI診断を専用Demo経路の確認とは扱わない。
- ソースartifacts/demo-click-audit/ProbePrimaryDraw800.cs.txt、同DemoAuditPrimaryDraw800.csproj、ログ同primary-draw800-0500-output.txt。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPrimaryDraw800.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoAuditPrimaryDraw800.dll：正常終了、終了コード０。本体再ビルドなし、既存出力先使用。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。次回は復帰後の画像を確認し、残るDemo経路の点検へ戻る。直送便専用Demo・全Seed／人数・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 06:00回（トリガー05:56）

- 開始：05:56:03 JST。終了：2026-11-03 05:56:15 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録56行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- 前回未確認だった復帰後の画像を確認。前回実行済みProbePrimaryDraw800.cs.txtのframe22は物理×クリックと0.3秒後の復帰状態チェック後にDrawしdirect800-22.pngへ保存する。ログprimary-draw800-0500-output.txtの成功と保存順を照合し、同画像を開いた。1280×800で６プライマリーが同じ下端へ戻り、直送便横の×が消え、通常地図に戻る描画を目視。前回のprimary800-half.pngと合わせ退避途中／復帰後を確認。今回新しい実行・ビルドはせず既存証拠を利用。
- 通常UI診断の範囲であり、専用Demo経路の巡回とはしない。新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。次回は更新後本体のDemoUiを再ビルドして開始・地図・終了経路を点検する。直送便専用Demo・全Seed／人数・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 07:00回

- 開始：07:00:35 JST。終了：2026-11-03 07:01:04 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=fdb1d7f8ebbdb0f24114ecd0d2fbbedf07e0ca6a。既存点検記録62行を保持。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- 更新後本体のDemoUiを再ビルドして開始・地図・終了経路を確認。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ログartifacts/demo-click-audit/demo-build-0700-output.txt。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoUi.dll：正常終了、終了コード０。ログ同demo-0700-nov3-output.txt。
- タイトル待機からの開始、地星儀／展開図切替、18FPSの面接続、拒否分岐、終了ループとDemoラベルの既存検証が成功。生成された1920×1080のdemo.pngを開きタイトルとDemoラベルを確認。全時間をタイトルから終了まで連続描画した検証ではなく、テスト内の対象段階からの再生を含む。優先プライマリー／直送便は前回までの確認からコード差分なし。
- 新規不具合なし、既報0001～0011完了を維持。ゲーム・既存テスト無変更、記録のみ追記。既存出力先使用。次回は更新後本体のDemoActionsUiで格納庫の実押下／ドロップ、移動予約のクリックと結果を再確認する。直送便専用Demo・全Seed／人数・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 08:00回（トリガー08:04）

- 開始：08:04:06 JST。終了：2026-11-03 08:05:16 JST。状態：完了（部分点検）。AGENTS、README、両記録・差分を先に確認。HEAD=35077bf66db9158017436278975e7baee65b1f02へ更新、関係未コミット差分なし。同対象の別チャットはnotLoaded、ゲーム・dotnetプロセスなし。停止・セーブ操作・コミット・プッシュ・公開なし。
- 格納庫・直送便・産業時間・製造プラン等の更新を確認し、旧本体を流用せずDemoActionsUiを再ビルド。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoActionsUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告０・エラー０。ログartifacts/demo-click-audit/actions-build-0800-output.txt。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoActionsUi.dll：正常終了、終了コード０。ログ同actions-0800-output.txt。
- 1080p／540pで実地点ヒット・地星儀／展開図・回転・非表示レイヤー／画面外カメラ、単独部位／２包み／結合部位の押下保持と実ドロップ、移動予約と独立したターン終了、再試行／行先なしを確認。demo-actions-1.pngを開き描画確認。ゲーム・既存テスト無変更、記録のみ追記、既存出力先使用。
- 新規不具合なし、既報0001～0011完了を維持。更新後の直送便・工場プラン・産業の再検証は今回未実施。次回は優先のDemoInspectorUiとDirectDeliveryUiを更新後本体でビルド・実行する。直送便専用Demo・全Seed／人数・タイトル全体再生・利用者ゲーム併存は未確認。

### 実行履歴：2026-11-03 09:00回（トリガー09:06）

- 開始：2026-11-03 09:06:37 JST（トリガー）。終了：2026-11-03 09:08:51 JST。状態：完了（部分点検）。AGENTS、README、両記録とGit差分を最初に確認。HEAD=e2a5b2eff6ac6ab8e94d9f0ea849d7b5b266b215。アプリ一覧は当該チャットのみactive、同対象の他チャットはnotLoaded。FolderVerse・Demo・dotnet・MSBuildプロセスなしを確認して開始。利用者のゲーム・セーブ操作、停止、コミット・プッシュ・公開なし。
- 関係する既存差分：Game1.CountrySettings／DomesticView／FolderTree／MapOverlays／PortraitMedallion／ReservationDirectory／Robots、未追跡Game1.CountryBorders、tests/CountrySettingsと個人用チャット文書。国模様の縁描画と設定画面の並び替えを保持した。Game1.Demo.cs／DemoDomestic.cs／既存Demoテストに未コミット差分なし。今回ゲームコード・テストは変更していない。
- Demo／DemoDomesticを入口に、DemoInspectorUi、DirectDeliveryUi、DemoUiを順に現ソースから再ビルド・実行。共通：DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/<対象> --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers、ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/<対象>.dll。全３ビルド成功（警告・エラー０）、全３実行成功。既存出力先を再使用。ログはartifacts/demo-click-audit/audit-latest-{build,inspector,direct-build,direct,demo-build,demo}.log（再利用する最新点検用ログ）。
- DemoInspectorUi：1920×1080／960×540、世界123・人物456・配置789。工場６プランのクリックと現矩形・実選択、監督、閉じる演出、輸送長期計画プライマリー、わたしの国→移動、人数確認を照合。段階12のクリックはTransportOverviewButton=PrimaryActionBounds(4)。demo-office-5.pngを開き長期輸送一覧の描画を確認した。
- DirectDeliveryUi：比率、他プライマリーの退避／復帰・移動中入力抑止、×／Escape、540pの回転面接続、左右インスペクター、連続トラッククリック、カメラ遷移と予約保持が成功。direct-17.pngを開き、左右の地点名・格納庫と中央地図の描画を確認した。通常直送便UIテストであり、直送便専用Demo巡回を完了したとは扱わない。
- DemoUi：タイトル待機開始、地星儀／展開図切替、18FPS面接続、拒否分岐、終了ループ、Demoラベルの検証成功。リリース準備時の「propagationへ到達せずWorldStatus stage=0」失敗は今回再現せず、原因・全Seedでの解消は未確定。成功した１回を恒久的な修正の証拠にはしない。
- 新規確認不具合なし。既報DEMO-0001～0011の完了状態を維持。未確認：全時間をタイトルから連続描画する経路、直送便専用Demo、全Seed／全人数、利用者ゲーム併存。次回はDemoActionsUiで現在の縁描画を含む格納庫押下／ドロップ・移動予約を再確認し、伝播到達失敗のSeed／再現条件が特定できるか調べる。

## 2026-11-03 10:00回の追加問題

### DEMO-0012：DemoUiの連続巡回検証が地図説明後の復帰判定で停止する

- 状態：完了（2026-11-03 10:39:25 JST修正・検証。固定Seed３条件と関連UIで確認。詳細はdemo-click-fixes.md）。優先度：中。
- 確認日時：2026-11-03 10:05～10:06 JST。HEAD=f0612ee5ecdf6e912e06982bb0e89a82c20f68a9。未コミット差分はGame1.csとGame1.CountrySettings.csの設定画面背景・背面描画抑止、個人用チャット文書。Demo本体・DemoUi／InspectorUi／DirectDeliveryUiは前回HEAD e2a5b2eから変更なし。今回ゲームコード・既存テストを変更していない。
- 対象：tests/DemoUi/Program.cs:44～57、FolderVerse/Game1.Demo.csのUpdateDemoMap／UpdateDemoDomesticへの移行。原因は未確定。
- 再現手順：現在の本体をtests/DemoActionsUiのビルドで更新後、既存のDemoUi.dllをALSOFT_DRIVERS=nullで起動する。テストはタイトル待機・人数確認・世界構築からTransport到達を確認後、WorldStatusに切り替えて_demoMapVisits／Stage／Ageを０にし、1/18秒×1200更新で地図説明と内政１巡を検査する。同コマンドを２回実行。
- 期待：地星儀／展開図／面接続の説明後に内政操作へ復帰し、_demoMapVisits>0となって後続の戦闘・拒否分岐・終了ループ検証へ進む。
- 実際：２回とも「Demo map explanation did not resume gameplay: stage=5 age=0 screen=WorldStatus step=0」でLoadContentの57行目から例外終了（終了コード非０）。３種類の地図説明を見たことの直前チェックは通過した。後続の戦闘・終了検証は未実施。テストの部分状態リセットや待ち時間が不適切である可能性も残り、ゲームのクリック不具合と断定しない。
- 解像度：標準1920×1080論理キャンバスのテストホスト。地図説明は18FPS刻み。世界・人物・配置Seedは既存テストがDemo中に生成し、今回例外ログには未収録。次回はSeedと国内段階・アニメーション待ち・地図位置を採取して固定再現を試す。
- 証拠：artifacts/demo-click-audit/audit-latest-demo.log、audit-latest-demo-recheck.logに同一の例外とスタック。テストはLoadContent内で停止したため、この失敗実行の画像は生成されていない。既存demo.pngを失敗地点の証拠には流用していない。
- 補足：前回09:00点検では同テストが成功した。以前のリリース準備時の「propagationへ到達せずWorldStatus stage=0」との因果関係は未確定。伝播未到達を今回再現したとは扱わない。

### 実行履歴：2026-11-03 10:00回（トリガー10:03）

- 開始：2026-11-03 10:03:37 JST（トリガー）。終了：2026-11-03 10:07:08 JST。状態：完了（部分点検、DEMO-0012追加）。AGENTS、README、両記録、Git差分を最初に確認。アプリ一覧は当該チャットのみactive、他はnotLoaded。開始時FolderVerse・Demo・dotnet・MSBuildプロセスなし。利用者の起動中ゲーム／セーブ／既存差分を保護し、停止・コミット・プッシュ・公開なし。
- Demo／DemoDomesticと既存テストの矩形・遷移チェックを確認。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoActionsUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功、警告・エラー０。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoActionsUi.dll：成功。ログaudit-latest-actions-build.log／audit-latest-actions.log（artifacts/demo-click-audit内の既存最新ログ名を再使用）。
- 現本体に対する既存DemoInspectorUi.dll／DirectDeliveryUi.dllも成功。前回から両テストのソース差分なしを確認し、現ゲームDLLに対して実行。ログaudit-latest-inspector.log／audit-latest-direct.log。プライマリー・直送便退避復帰・左右・連続トラック、工場６プラン・閉じる・人数確認を検査。1080p／540p。通常直送便UIの検証であり、直送便専用Demo巡回と混同しない。
- Actionsは世界123・人物456・配置789、実地点26の座標（展開図1026,503／回転1008,618／地星儀960,552）とヒット、押下保持・実ドロップ、部位／包み／結合部位、６条件の対象89への和平移動予約と独立したターン終了が成功。demo-actions-38.pngを開き「移動予約済」「予約１件／ターン終了で実行」と地図・プライマリー描画を確認。
- DemoUiは２回とも上記DEMO-0012で停止。今回は修正していない。既報DEMO-0001～0011の完了状態は維持。次回修正回は0012のテスト状態と実再生の切り分けから開始し、Seed・国内段階・待機条件を記録する。全Seed／全人数・直送便専用Demo・タイトルから終了までの連続描画・利用者ゲーム併存は未確認。ゲーム・テストは無変更、点検記録のみ追記。

- DEMO-0012修正回引き継ぎ：2026-11-03 10:39:25 JST完了。地図の投影中心と実表示範囲のずれ、およびクリック待機中の自動回転を修正。固定Seed０・１・２が元の1,200更新で次の伝播まで進み、Actions／Inspectorの実描画も確認。全条件の網羅ではない。

### 実行履歴：2026-11-03 11:00回

- 開始：2026-11-03 11:00:08 JST（トリガー）。終了：2026-11-03 11:02:44 JST。状態：完了（部分点検）。AGENTS、README（Norton起動待ち手順を含む）、両記録・Git差分を最初に確認。HEAD=6e9b0955ad4ce3769e909e114ba71e3c4066ab02、開始時未コミット差分なし。アプリ一覧は当該チャットのみactive、他はnotLoaded。ゲーム・Demo・dotnet・MSBuildなしを確認して開始。ゲーム・テスト無変更、点検記録のみ追記。セーブ操作・プロセス停止・コミット・プッシュ・公開なし。
- Demo／DemoDomesticの視点補正・内政中の自動回転抑止を確認。前回修正のSeed０～２に続き、FOLDERVERSE_DEMO_TEST_SEED=3を追加点検。dotnet build tests/DemoUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：成功（警告・エラー０）。要求11:00:40、終了11:01:05、約25秒の実行中を失敗扱いせず短くポーリングした。ログartifacts/demo-click-audit/audit-latest-demo-build.log。
- ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoUi.dll：要求11:01:37、終了11:01:43、終了コード０。ログaudit-latest-seed3.log。乱数３の世界／人物／配置は748827235／1836839534／462824652。元の1,200更新・18FPS刻みで地図説明後の内政１巡から全体ターン１→２と次のTransportへ進み、戦闘・拒否・終了ループも成功。固定Seed３を加えてもDEMO-0012は再発せず、完了状態を維持する。全Seedの解消とは扱わない。
- 現本体に対し、ソースに差分のない既存DirectDeliveryUi.dll（11:01:55～11:02:04）とDemoInspectorUi.dll（11:02:04～11:02:14）を順番に実行、どちらも終了コード０。ログaudit-latest-direct.log／audit-latest-inspector.log。直送便の退避復帰、×／Escape、左右インスペクター、540pの回転面接続・連続トラック・カメラ・予約保持、工場６プラン・閉じる・長期輸送プライマリー・わたしの国→移動を照合した。DirectDeliveryUiは通常UIテストであり専用Demo巡回ではない。
- 最新direct-17.png（960×540）とdemo-office-5.png（1920×1080）を開き、左右の地点名・格納庫・中央地図と、長期輸送一覧がプライマリーの操作から描画されることを確認。各画像・DLLは既存出力先を再使用。起動要求とテストSeed表示等のログを区別し、今回Nortonによる保留の直接証拠はなく原因にしていない。再試行・セキュリティ設定の変更なし。
- 新規確認不具合なし、DEMO-0001～0012の完了状態を維持。未確認：全Seed／人数、直送便専用Demo、固定Seedの全フレーム連続描画、利用者ゲーム併存。次回は必要に応じSeed４以降か1280×800条件を追加し、実クリック位置・遷移結果の範囲を広げる。

### 実行履歴：2026-11-03 12:00回（トリガー12:04）

- 開始：2026-11-03 12:04:09 JST（トリガー）。終了：2026-11-03 12:06:52 JST。状態：完了（部分点検）。AGENTS、README（Norton起動猶予）、両記録・Git差分を先に確認。HEAD=6e9b0955ad4ce3769e909e114ba71e3c4066ab02、既存の点検記録９行の差分を保持。他チャットはnotLoaded、ゲーム・Demo・dotnet・MSBuildプロセスなし。ゲーム／既存テスト無変更、セーブ操作・プロセス停止・コミット・プッシュ・公開なし。
- Demo／DemoDomesticの入力先が現在のプライマリー・インスペクター矩形とヒット可能な地点を使うことを再確認。前回から本体ソースに変更がなく、現本体を含む既存出力artifacts/corner-lessonを利用。再ビルドや出力先の新設なし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=4でdotnet artifacts/corner-lesson/DemoUi.dll：12:04:40要求、12:04:46終了、コード０。ログartifacts/demo-click-audit/audit-latest-seed4.log。世界／人物／配置=1014506826／1020794303／748161960。元の1,200更新・18FPS刻みで地図説明→内政１巡→全体ターン１から２→次のTransport、戦闘・拒否・終了ループの検査成功。DEMO-0012は追加Seed４でも再発せず。
- dotnet artifacts/corner-lesson/DemoAuditNative800.dll：12:04:59要求、12:05:31終了、コード０。既存ソースartifacts/demo-click-audit/ProbeNative800.cs.txtと既存診断DLLを現本体に対して使用。世界123・人物456・配置789、1280×800・18FPS、通常HandleInput＋毎更新Draw。監督・格納庫・工場・長期輸送・閉じる・わたしの国・移動選択・予約確定・ターン終了を465更新でTransportまで進めた。ログaudit-latest-native800.log。段階12のクリック(622,1022)、移動先(1203,263)、確定(707,938)、ターン終了(256,998)。audit-native800-398.pngを開き、クリック付近の選択先とPoint90の選択表示を確認した。座標は1920×1080論理値。
- dotnet artifacts/corner-lesson/DemoAuditPrimaryDraw800.dll：12:05:49要求、12:06:00終了、コード０。既存ソースProbePrimaryDraw800.cs.txt。1280×800の６プライマリー物理中心→論理矩形、直送便の退避途中・復帰、物理×クリックのチェック成功。primary800-half.pngを開き退避途中の切れ方を目視。さらに540p回転面接続・左右・連続トラック・カメラ・予約保持の既存検証も成功。ログaudit-latest-primary800.log。通常直送便UIの検査を専用Demo巡回と扱わない。
- 要求・終了時刻と起動後の進行ログを分け、短い待機で実行中を追跡した。今回Nortonの直接証拠なし、再試行・セキュリティ設定変更なし。新規確認不具合なし、DEMO-0001～0012の完了を維持。
- 未確認：全Seed／人数、直送便専用Demo、追加Seed全フレームの目視、利用者ゲーム併存。次回は必要に応じSeed５または未確認の戦闘・説得・世界構築Demo経路を巡回し、現矩形と実際の遷移を照合する。

### 実行履歴：2026-11-03 13:00回（トリガー12:55）

- 開始：2026-11-03 12:55:10 JST（トリガー）。終了：2026-11-03 12:56:48 JST。状態：完了（部分点検）。AGENTS、README、両記録、Git差分を最初に確認。HEAD=6e9b0955ad4ce3769e909e114ba71e3c4066ab02、既存の点検記録19行の差分を保持。同対象の他チャットはnotLoaded、ゲーム・Demo・dotnet・MSBuildプロセスなしを確認。ゲーム／テスト変更、利用者セーブ操作、プロセス停止、コミット・プッシュ・公開なし。
- Game1.Demo／DemoDomesticを入口に、区画権利はCastAction、人物確認はStatusProceed、人物選択は実タイル、内政はプライマリーとインスペクターの現矩形を参照することを確認。前回から本体・関連テストの変更なし、現本体を含む既存出力artifacts/corner-lessonを使用。再ビルド・出力先増設なし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=5、dotnet artifacts/corner-lesson/DemoUi.dll：12:55:39要求、12:55:44終了、コード０。ログartifacts/demo-click-audit/audit-latest-seed5.log。世界／人物／配置=1280186417／204749072／1033499268。地図説明から内政１巡、全体ターン１→２、次のTransport、戦闘・拒否・終了ループの検査成功。DEMO-0012は追加Seed５でも再発せず。
- dotnet artifacts/corner-lesson/DemoInspectorUi.dll：12:55:55要求、12:56:07終了、コード０。ログaudit-latest-inspector.log。工場６プランのクリック現在矩形・実選択（1080p／540p）、監督、閉じる演出、長期輸送プライマリー、わたしの国から移動、人数確認が成功。
- 残る世界構築経路の再点検としてdotnet artifacts/corner-lesson/DemoSetupUi.dll：12:56:07要求、12:56:19終了、コード０。ログaudit-latest-setup.log。世界123・人物456・初期配置789のfixture、18FPS・人間１／２人・1080p／540pで、停止／確定は(758,1045)の実矩形、人物は実タイルのHitSelectionと実選択、別人を選ぶ２人目、LandConquestへの遷移を照合した。通常の物理マウスによるスロット３選択と確認も成功。人物タイルの配置はその時点の領地数による。
- demo-setup-26.png（960×540）を開き、２人目の「あなた」表示とカーソルが同じ人物を指すことを目視。既存画像名・ログ名を再利用。直送便の退避復帰・左右・連続トラックは前回12:00点検以後コード変更がなく、今回は追加実行していない。専用直送便Demoの未確認状態は維持。
- 起動要求と終了時刻を記録し、短いポーリングで追跡した。Nortonの直接証拠、再試行、セキュリティ設定変更なし。新規確認不具合なし、DEMO-0001～0012の完了状態を維持。
- 未確認：全Seed／人数、専用直送便Demo、追加Seed全フレーム、利用者ゲーム併存。次回は必要に応じSeed６、または戦闘／説得など未確認のクリック付き経路を調べる。現在の成功を全経路の完了とは扱わない。

### 実行履歴：2026-11-03 14:00回（トリガー14:02）

- 開始：2026-11-03 14:02:41 JST（トリガー）。終了：2026-11-03 14:04:26 JST。状態：完了（部分点検）。AGENTS、README、両記録・Git差分を最初に確認。HEAD=6e9b0955ad4ce3769e909e114ba71e3c4066ab02、既存の点検記録30行の差分を保持。他チャットはnotLoaded、ゲーム・Demo・dotnet・MSBuildなし。ゲーム／テスト無変更、セーブ操作・停止・コミット・プッシュ・公開なし。
- Demo／DemoDomesticを入口に、戦闘と伝播はUpdateTransport／RobotBattleInputを自動進行させ、内政のクリックは現矩形と実ヒットを使うことを確認。本体・既存診断ソースに前回から変更なし、既存出力artifacts/corner-lessonを現本体のまま使用。再ビルド・出力先増設なし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=6、dotnet artifacts/corner-lesson/DemoUi.dll：14:03:10要求、14:03:14終了、コード０。ログartifacts/demo-click-audit/audit-latest-seed6.log。世界／人物／配置=1545866008／1536187488／1318836576。地図説明・内政１巡・全体ターン１→２・次のTransport、戦闘・拒否・終了ループの検査成功。DEMO-0012は追加Seed６でも再発せず。
- 優先直送便：dotnet artifacts/corner-lesson/DirectDeliveryUi.dll：14:03:39要求、14:03:50終了、コード０。ログaudit-latest-direct.log。プライマリー比率・退避復帰・移動中の入力抑止、×／Escape、540p回転面接続、左右、連続トラック、カメラ・予約保持を確認。direct-17.png（960×540）を開き左右の地点名・格納庫と中央地図を目視。通常UI検証であり専用直送便Demo巡回ではない。
- 残る説得経路：dotnet artifacts/corner-lesson/DemoAuditPersuasionResume.dll：14:03:50要求、14:03:55終了、コード０。既存ソースartifacts/demo-click-audit/ProbePersuasionResume.cs.txtを参照。世界123・人物456・配置789、1080p／540p・18FPSの通常HandleInput＋Draw。工場を開いたfixtureから説得演出で格納庫へ切り替わり、約８秒の待機・自動クローズ中にDemoクリックを発生させず、その後実地点クリック(960,552)でstage3へ復帰した。ログaudit-latest-persuasion.log。
- audit-persuasion-resume-5.png（960×540）を開き、説得中に工場ではなく格納庫が表示されることを確認。fixtureは説得結果を事前設定し、今回確率・パーツ変更の実計算や複数rulerの説得は検査していない。既存画像・ログ名を再利用。
- 起動要求と終了・Seed表示を区別して追跡し、今回Norton保留の直接証拠なし。再試行・セキュリティ設定変更なし。新規確認不具合なし、DEMO-0001～0012の完了状態を維持。
- 未確認：全Seed／人数、専用直送便Demo、追加Seed全フレーム、複数ruler説得、利用者ゲーム併存。次回は必要に応じSeed７か、未確認の戦闘・伝播の実描画／クリック有無を照合する。現在の部分検査を全経路完了とはしない。

### 実行履歴：2026-11-03 15:00回（トリガー14:57）

- 開始：2026-11-03 14:57:42 JST（トリガー）。終了：2026-11-03 14:58:55 東京 (標準時)。状態：完了（部分点検）。AGENTS、README、両記録・Git差分を最初に確認。HEAD=6e9b0955ad4ce3769e909e114ba71e3c4066ab02。既存の点検記録、README／docsの配布版案内、個人用チャットの差分を保持。アプリ一覧は当該チャットのみactive、他はnotLoaded。開始時FolderVerse／dotnetプロセスなし。ゲーム・テスト無変更、セーブ操作・停止・コミット・プッシュ・公開なし。
- Demo.cs／DemoDomestic.csとDemoUi／DemoInspectorUiを入口に、現矩形からのクリックと遷移チェックを確認。戦闘・伝播はUpdateTransport／RobotBattleInputの自動進行で、通常内政のクリック経路とは異なる。ゲーム／テストのGit差分なし、既存本体DLL（11:01更新）と出力artifacts/corner-lessonを再利用。再ビルド・出力先増設なし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=7、dotnet artifacts/corner-lesson/DemoUi.dll：14:58頃に要求・開始ログ・約７秒後終了、コード０。ログartifacts/demo-click-audit/audit-latest-seed7.log。世界／人物／配置=1811545599／720142257／1604173884。18FPS地図説明、内政１巡、全体ターン１→２と次のTransport、戦闘・拒否・終了ループ成功。DEMO-0012は追加Seed７でも再発せず。
- dotnet artifacts/corner-lesson/DemoInspectorUi.dll、続けてDirectDeliveryUi.dll：14:58:10頃要求、14:58:30までに両方コード０。ログaudit-latest-inspector.log／audit-latest-direct.logを再使用。工場６プランの現在矩形と実選択（1080p／540p）、監督、閉じる、長期輸送プライマリー、わたしの国から移動を確認。直送便はプライマリー退避復帰、移動中入力抑止、×／Escape、左右、連続トラック、540pの回転面接続と予約保持が成功。通常UI検証であり専用直送便Demoの完走とは扱わない。
- direct-17.png（960×540）を開き、左／右インスペクター、地点名、格納庫、中央地図とトラックの描画を確認。既存画像名を再使用。起動待ちをゲーム内停止と混同せず、今回Norton保留の直接証拠・再試行・セキュリティ設定変更なし。
- 新規確認不具合なし。DEMO-0001～0012の完了状態を維持。未確認：全Seed／人数、専用直送便Demo巡回、追加Seed全フレーム、複数ruler説得、利用者ゲーム併存。次回はSeed８か、未確認の戦闘／伝播の描画・クリック有無を照合する。部分検査を全経路完了とはしない。

### 実行履歴：2026-11-03 16:00回（トリガー15:57）

- 開始：2026-11-03 15:57:15 JST（トリガー）。終了：2026-11-03 15:58:00 JST。状態：中断（同一チェックアウトで別開発が実行中のため、現HEADの依存検証を次回へ送る）。AGENTS、README、両記録、現在のGit差分を最初に確認。HEAD=00ae8994d10f71453c6e74d98b1842ab15e042c4。開始時は個人用チャット文書のみ未コミット、FolderVerse／dotnet／MSBuildプロセスなし。
- アプリ一覧で別チャット「解説カットイン時にボタンを退避」が同じ作業ディレクトリーでactive。前回検証HEAD=6e9b095からゲーム／テスト21ファイルに変更があり、TutorialChrome、Game1、地図・向き操作等が更新されていることも検出。利用者の変更を保護し、本体の再ビルド・現HEADに対するUIテスト・重複変更を進めない。
- 並行状況と差分の取得と同じツール呼出しで、既存出力artifacts/corner-lesson/DemoUi.dllをALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=8で１回実行した。約５秒でコード０、ログaudit-latest-seed8.log。世界／人物／配置=2077225190／2051580673／1889511192、地図説明から内政・次のTransport・戦闘／拒否／終了の既存チェック成功。ただしDLLは前回本体（11:01更新）であり、今回のHEADを検証した証拠にはしない。Norton起動保留の直接証拠なし。
- Demo.cs／DemoDomestic.csの実地点ヒット・MapViewButton・自動戦闘／伝播の入口を読んだが、現HEADのプライマリー・直送便退避復帰・左右の実描画／遷移照合は未実施。新規確認不具合なし、DEMO-0001～0012の既存状態を変更しない。ゲーム／テスト・利用者セーブ無変更、プロセス停止・コミット・プッシュ・公開なし。
- 次回：先行開発の終了とゲーム／検証プロセスを確認し、その時点のHEADで既存の分離出力を更新してSeed８から再開。優先UIの回帰、全Seed／人数、専用直送便Demo、戦闘／伝播の描画、利用者ゲーム併存は未確認。今回を現コードの正常確認とは扱わない。

### 実行履歴：2026-11-03 17:00回（トリガー16:55）

- 開始：2026-11-03 16:55:16 JST。終了：2026-11-03 16:56:51 JST。状態：完了（部分点検）。AGENTS、README、両記録・Git差分を最初に確認。HEAD=34f433a803770fa5a50d5cc0188cc4e6531ccc70（終了時も同じ）。個人用チャット文書45行の既存差分を保持。別開発チャットはidle、開始時FolderVerse／dotnet／MSBuildなし。ゲーム／テスト無変更、利用者セーブ操作・停止・コミット・プッシュ・公開なし。
- 前回中断後の現HEADで既存出力artifacts/corner-lessonを更新。dotnet build tests/DemoUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：最初はCLIのTelemetry初期化例外で終了。DOTNET_CLI_TELEMETRY_OPTOUT=1を設定した実行は約15秒で成功、警告・エラー０。ログaudit-latest-build.log（成功実行）。これはNorton保留の証拠ではない。
- Demo.cs／DemoDomestic.csと既存テストを起点に、実地点ヒット、MapViewButtonと現在矩形による操作、戦闘／伝播は自動進行であることを照合。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=8、dotnet artifacts/corner-lesson/DemoUi.dll：16:56頃開始、コード０。世界／人物／配置=2077225190／2051580673／1889511192。18FPS地図説明・内政１巡・ターン１→２・次のTransport・戦闘／拒否／終了の検査成功。ログaudit-latest-seed8.log。今回は更新済み本体を使用し、前回の旧DLLでの成功と区別した。
- 続けてDemoInspectorUi.dll／DirectDeliveryUi.dllを現本体で実行し、両方コード０。16:56:22頃までに前者成功、直送便も後続ポーリングで終了確認。ログaudit-latest-inspector.log／audit-latest-direct.log。工場６プランの現在矩形・実選択、監督、閉じる、長期輸送プライマリー、わたしの国→移動（1080p／540p）、直送便の退避復帰・×／Escape・左右・連続トラック・回転面接続と予約保持が成功。direct-17.png（960×540）を開き左右の地点名・格納庫と中央地図の描画を目視。既存ログ・画像名を再利用。
- 新規確認不具合なし、DEMO-0001～0012の状態維持。Norton保留の直接証拠・セキュリティ設定変更なし。今回の直送便確認は通常UIテストであり専用Demo完走ではない。未確認：全Seed／人数、専用直送便Demo、各Seed全フレーム、戦闘／伝播全描画、複数ruler説得、利用者ゲーム併存。次回はSeed９か未確認経路を追加。部分点検を全経路完了とはしない。

### 実行履歴：2026-11-03 18:00回（トリガー17:56）

- 開始：2026-11-03 17:56:48 JST。終了：2026-11-03 17:58:08 JST。状態：完了（部分点検）。AGENTS、README、両記録、Git差分を最初に確認。HEAD=1701d0ce8db6d51c352d9a037c3aae3ad9fb62d2。別開発チャットはidle、開始時ゲーム／dotnet／MSBuildなし。既存のAutoSave・Tutorial・MapOverlays・OrientationToy・WorldSetupと関連テスト計12ファイル、および個人用チャット文書の差分を保持。終了前numstatも開始時と同じ。ゲーム／テスト無変更、セーブ操作・停止・コミット・プッシュ・公開なし。
- Demo.cs／DemoDomestic.csを入口にMapViewButton、実表示範囲と地点ヒット、伝播／戦闘の自動進行を照合。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：17:57頃要求、約８秒で成功、警告・エラー０。現在の未コミット差分を含む本体を既存分離出力へビルド。ログaudit-latest-build.log。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=9、dotnet artifacts/corner-lesson/DemoUi.dll：17:57頃開始、約５秒でコード０。世界／人物／配置=195421134／1235535442／27364853。18FPS地図説明・内政１巡・全体ターン１→２・次のTransport・戦闘／拒否／終了ループ成功。ログaudit-latest-seed9.log。
- 続けてDemoInspectorUi.dll、DirectDeliveryUi.dllを現本体で実行、両方コード０、17:57:50頃までに終了。工場６プランの現在矩形と実選択、監督、閉じる、長期輸送プライマリー、わたしの国から移動（1080p／540p）、直送便退避復帰・×／Escape・左右・連続トラック・回転面接続・予約保持が成功。ログaudit-latest-inspector.log／audit-latest-direct.log。direct-17.png（960×540）を開き左右の地点名と格納庫・中央地図を目視。既存画像／ログ名を再利用し出力先を増設していない。
- 新規確認不具合なし、DEMO-0001～0012の完了状態を維持。起動要求から終了を短く追跡し、今回Norton保留の直接証拠・再試行・設定変更なし。未確認：全Seed／人数、専用直送便Demo、各Seed全フレーム、戦闘／伝播全描画、複数ruler説得、利用者ゲーム併存。次回はSeed10または未確認経路へ。通常直送便UIテストを専用Demo巡回完了とは扱わない。

### 実行履歴：2026-11-03 19:00回

- 開始：2026-11-03 19:00:19 JST。終了：2026-11-03 19:00:40 JST。状態：中断（同一作業場所で別開発が実行中のため次回へ送る）。AGENTS、README、両記録とGit差分を最初に確認。HEAD=1701d0ce8db6d51c352d9a037c3aae3ad9fb62d2。別チャット「解説カットイン時にボタンを退避」がactive、移動・地図描画・選択ハイライト等にも前回から未コミット差分の追加あり。開始時ゲーム／dotnet／MSBuildは見つからなかったが、プロセスなしだけで先行作業の完了とは判断しない。
- 利用者の変更と既存点検記録を保持し、ビルド・UI起動・ゲーム／テスト変更・セーブ操作・プロセス停止・コミット・プッシュ・公開は行わない。新しい不具合を確認した実行はなく、DEMO連番や既報の状態は変更しない。
- 未確認：現差分でのプライマリー、直送便の退避復帰・左右、Seed10、残りのDemo経路。次回は先行開発の終了と競合なしを確認したうえで既存分離出力を更新し、Seed10から点検を再開する。今回の未実施を正常確認とは扱わない。

### 実行履歴：2026-11-03 20:00回（トリガー20:04）

- 開始：2026-11-03 20:04:50 JST。終了：2026-11-03 20:05:13 JST。状態：中断（同一作業場所で別開発がactiveのため重複実行を避け、次回へ送る）。AGENTS、README、両記録・Git差分を最初に確認。HEAD=e1ac2e6b5cc75072008a6bc9b22b470af933ab2f。開始時の未コミット差分は個人用チャット文書13行、ゲーム／dotnet／MSBuildプロセスなし。ただし別チャット「解説カットイン時にボタンを退避」のactiveを確認し、先行開発が完了したとは扱わない。
- ゲーム／テスト、利用者の変更・セーブ・起動中ゲームを保護。ビルド・Demo起動・プロセス停止・コミット・プッシュ・公開なし。新しい問題を再現した実行はなく、DEMO連番・既報の状態は変更しない。
- 未確認：現HEADでのプライマリー、直送便の退避復帰・左右、Seed10、残りのDemo経路。次回は先行作業終了と競合なしを確認し、既存分離出力を現コードで更新してSeed10から再開。今回の実行未確認を正常扱いしない。

### 実行履歴：2026-11-03 21:00回（トリガー20:59）

- 開始：2026-11-03 20:59:51 JST。終了：2026-11-03 21:00:56 JST。状態：完了（部分点検）。AGENTS、README、両記録とGit差分を最初に確認。HEAD=92a85d1fac0883cdd62f585c40538031a524749a（終了前も同じ）。別チャットはidle、開始時ゲーム／dotnet／MSBuildなし。既存の個人用チャット文書12行差分を保持。ゲーム／テスト無変更、利用者セーブ操作・プロセス停止・コミット・プッシュ・公開なし。
- 現HEADへ既存分離出力を更新。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：21:00頃要求、約９秒で成功、警告・エラー０。ログaudit-latest-build.log。Demo.cs／DemoDomestic.csのMapViewButton、実地点ヒット・行先、伝播／戦闘の自動進行を照合した。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=10、dotnet artifacts/corner-lesson/DemoUi.dll：21:00頃開始し約５秒でコード０。世界／人物／配置=461100725／419490211／312702161。18FPS地図説明、内政１巡、全体ターン１→２・次のTransport、戦闘／拒否／終了ループ成功。ログaudit-latest-seed10.log。
- 続けてDemoInspectorUi.dll／DirectDeliveryUi.dllを現本体で実行、両方コード０、21:00:40頃までに終了。工場６プランの現在矩形・実選択、監督、閉じる、長期輸送プライマリー、わたしの国→移動（1080p／540p）、直送便退避復帰・×／Escape・左右・連続トラック・回転面接続・予約保持が成功。ログaudit-latest-inspector.log／audit-latest-direct.log。direct-17.png（960×540）を開き左右の地点名・格納庫・中央地図を目視した。既存ログ／画像名と出力先を再利用。
- 新規確認不具合なし、DEMO-0001～0012の状態維持。Norton起動保留の直接証拠・再試行・設定変更なし。未確認：全Seed／人数、専用直送便Demo、各Seed全フレーム、戦闘／伝播全描画、複数ruler説得、利用者ゲーム併存。次回は同じ優先UIを必要に応じ確認しつつ、Seed11または未確認経路へ。通常直送便UIテストを専用Demo完走とは扱わない。

### 実行履歴：2026-11-03 22:00回（トリガー21:58）

- 開始：2026-11-03 21:58:22 JST。終了：2026-11-03 21:59:36 JST。状態：完了（部分点検）。AGENTS、README、両記録、Git差分（ステージ含む）を最初に確認。HEAD=973ceab60ea4309fc5aa20901b4eba69866fb8cc（終了前も同じ）。他チャットidle、開始時ゲーム／dotnet／MSBuildなし。UiFont・AutoSave・TutorialMessages／War・WorldSetup・TutorialWarGuidesUiの既存差分と個人用文書を保持。終了前のゲーム／テストnumstatは開始時と一致。ゲーム／テスト無変更、セーブ操作・停止・コミット・プッシュ・公開なし。
- DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：21:58頃要求、約12秒で成功、警告・エラー０。現未コミット差分を含む本体を既存出力へ更新。ログaudit-latest-build.log。Demo.cs／DemoDomestic.csの実地点ヒット・MapViewButton・戦闘と伝播の自動進行を照合。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=11、dotnet artifacts/corner-lesson/DemoUi.dll：21:58後半要求、21:59:11までに成功ログ・コード０。世界／人物／配置=726780316／1750928627／598039469。18FPS地図説明・内政１巡・ターン１→２・次のTransport・戦闘／拒否／終了ループ成功。ログaudit-latest-seed11.log。短いポーリングで開始ログと進行結果を区別し、無出力を即失敗とは扱わなかった。Norton保留の直接証拠なし。
- DemoInspectorUi.dll／DirectDeliveryUi.dllも現本体で成功、21:59:20頃までに両方コード０。工場６プランの現在矩形と実選択、監督、閉じる、長期輸送プライマリー、わたしの国→移動（1080p／540p）、直送便退避復帰・×／Escape・左右・連続トラック・回転面接続・予約保持を確認。ログaudit-latest-inspector.log／audit-latest-direct.log。direct-17.png（960×540）を開き左右の地点名・格納庫・中央地図を目視。既存ログ／画像名・出力先を再利用。
- 新規確認不具合なし、DEMO-0001～0012の完了状態維持。再試行・セキュリティ設定変更なし。未確認：全Seed／人数、専用直送便Demo、各Seed全フレーム、戦闘／伝播全描画、複数ruler説得、利用者ゲーム併存。次回はSeed12または残る経路。通常直送便UIテストを専用Demo完走とは扱わない。

### 修正回の追加経路確認：2026-11-03 22:30回

- 開始：2026-11-03 22:32:52 JST。終了：2026-11-03 22:33:22 JST。状態：中断（同一作業場所の別開発がactive）。AGENTS、README、両記録とGit差分を確認。HEAD=9e55c582ac8341148a5c90275e81ee66c3f54aeb。既存DEMO-0001～0012は完了、新たに点検済みの未解決IDなし。ゲーム／テスト無変更、ビルド・起動・コミット・プッシュ・公開なし。
- 利用者の追加指示を受け、緑／赤チェックのDemo実演を次回優先対象として確認した。現ソースのDemoDomestic段階にはNodeCheckButtonのクリックと開戦予約→赤チェックの手順が見当たらず、NodeChecksのNodeCheckPlayerは_demoActive中に-1となる。追加経路には実装不足の可能性がある。現段階は静的確認のみで、実描画・実入力・状態変化の再現確認前であり、新しい確定不具合IDとして登録していない。
- 別チャット「解説カットイン時にボタンを退避」がactiveのため依存実装を重複して進めない。次回点検は先行作業終了後の現コードで、実ハンドルから緑✅、地点選択→出撃確定（即予約）→赤✅→閉じる→ターン終了を優先して照合する。表示モード分離・赤チェックのスポットライト解除・チェック後のクリック・ターン末消去と、削除済み予約確定ボタンを使わないことも確認する。未実装が再現された場合はDEMO連番を付け、次の修正回で最小実装と描画検証へ進む。

### 利用者追加依頼の確認：2026-11-03夜、操作後の緑・赤チェック

- 開始時、Demoの定期点検・修正はACTIVE。既存の毎時00分・30分、対象チャットを維持して追加依頼を両automationへ反映した。22:30修正回が実行中だったため同じコードを同時編集せず、同回の終了を確認してから実装した。同回の「実装不足の可能性」という静的確認を、確定済みの旧不具合IDに読み替えていない。
- 利用者の依頼として、地点操作後の緑✅、開戦予約確定後の赤✅を追加。検証と手順はdemo-click-fixes.mdの同日の項目を参照。通常画面用のNodeCheckPlayerがDemo中に-1になる点も修正した。
- DemoChecksUiの地図／地星儀・1080p／540p４条件で、実ボタン内のクリック、実地点ヒット、出撃確定時の即予約、赤チェックによる強調除外、閉じた後の独立したターン終了、実進軍、両チェック消去を確認した。DemoUiは固定Seed12で自然巡回し、内政１巡からTransport、ターン２、戦闘・拒否・終了ループまで成功。DemoActionsUi・DemoInspectorUiの回帰も成功した。
- 実描画demo-checks-0-6.pngを開き、開戦予約済みの地点の赤チェック、ハンドルを指すDemoの手、予約保持を目視した。既存の点検結果・未確認範囲は保持し、全Seed／人数・専用直送便Demoまで確認済みとは扱わない。
- 完了：2026-11-03T22:40:28+09:00。利用者のゲームは停止していない。実セーブ・勲章の変更、コミット・プッシュ・追加リリースは行っていない。

### 実行履歴：2026-11-03 23:00回（トリガー22:59）

- 開始：2026-11-03 22:59:23 JST。終了：2026-11-03 23:00:51 JST。状態：完了（部分点検）。AGENTS、README、両記録・Git差分を最初に確認。HEAD=9e55c582ac8341148a5c90275e81ee66c3f54aeb。別チャットidle、開始時ゲーム／dotnet／MSBuildなし。利用者が22:40に追加したDemoChecks・DemoDomestic・NodeChecks・テスト・引継ぎ文書の差分を保持し、ゲーム／テスト無変更。実セーブ・勲章操作、停止、コミット・プッシュ・公開なし。
- 追加の緑／赤チェック経路を優先。DemoChecks.csで段階30～36の地点選択→出撃選択確定による即予約→赤チェック→閉じる→別段階19のターン終了と、NodeCheckButtonの実矩形使用を照合。旧開戦確定ボタンを使わない。前回の実装不足の可能性は利用者側の実装済みであり、新規未解決IDを追加していない。
- 最初に既存tests/DemoChecksUi/bin/DemoChecks/DemoChecksUi.dllを起動したがContent/Images/robot-assembly-icon.xnbの欠落によるContentLoadExceptionで終了。これは不完全な検証出力で、ゲーム内のクリック失敗やNorton保留ではない。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoChecksUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-serversを実行し、既存の素材付き分離出力を再利用。約８秒で警告・エラー０。
- ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoChecksUi.dll：23:00頃要求、約７秒でコード０。地星儀／展開図×1920×1080／960×540の４条件で、緑ハンドルのクリック、実開戦地点ヒット、出撃確定時の即予約、赤ハンドルのクリック、緑／赤メモ独立、赤地点の強調除外、閉じた後の独立したターン終了、実進軍、両チェック消去が成功。ログaudit-latest-checks.log／audit-latest-checks-build.log。demo-checks-0-6.pngを開き赤チェック・予約１件・ハンドルを指すカーソルと画面描画を確認。
- 続けてFOLDERVERSE_DEMO_TEST_SEED=12、dotnet artifacts/corner-lesson/DemoUi.dll：コード０、地図説明・自然内政巡回・次のTransport・戦闘／拒否／終了ループ成功（audit-latest-seed12.log）。現本体での優先追加経路の再確認であり全経路完走ではない。
- 新規確認不具合なし。未確認：チェック後の地点再クリックの独立再検査、全Seed／人数、専用直送便Demo、各Seed全フレーム、戦闘／伝播全描画、利用者ゲーム併存。直送便の退避復帰・左右は前回確認後の追加変更に対して今回未再実行。次回はチェック済み地点の実クリックと通常／開戦モード表示切替、直送便回帰、Seed13を優先。Norton設定変更なし。既存ログ・出力先を使用。

### 実行履歴：2026-11-04 00:00回（トリガー11月3日23:56）

- 開始：2026-11-03 23:56:24 JST。終了：2026-11-03 23:58:11 JST。状態：完了（部分点検）。AGENTS、README、両記録・Git差分を最初に確認。HEAD=9e55c582ac8341148a5c90275e81ee66c3f54aeb。別チャットidle、開始時ゲーム／dotnet／MSBuildなし。既存のDemoChecks関連コード・テスト・文書差分を保持。ゲーム／テスト無変更、実セーブ・勲章操作・プロセス停止・コミット・プッシュ・公開なし。
- 追加チェック経路を優先し、NodeChecksUiのソースで緑／赤モード別描画、赤チェック後もMovementTargetsに残すこと、解除時の強調復帰を確認。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/NodeChecksUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：23:56後半要求、約９秒で警告・エラー０。ALSOFT_DRIVERS=nullで同出力のNodeChecksUi.dllを実行、コード０。1080p／540pの非閉鎖トグル、モード別の赤／緑描画、ハンドル入力・地点変更後の保持、地星儀／展開図の赤強調除外と候補維持、ターン末消去が成功。ログaudit-latest-nodechecks-build.log／audit-latest-nodechecks.log。
- DemoChecksUi.dllも同出力で成功、４条件の実ハンドル・実地点・即予約→赤→閉じる→独立ターン終了・実進軍を確認。demo-checks-0-6.pngを開き赤チェック・ハンドルのカーソルと予約１件を目視。ログaudit-latest-checks.log。FOLDERVERSE_DEMO_TEST_SEED=13のDemoUi.dllもコード０（世界／人物／配置=1258139498／118838165／1168714085）、自然巡回・次のTransport・戦闘／拒否／終了チェック成功。ログaudit-latest-seed13.log。
- DirectDeliveryUiの古いDLLはFolderVerse Version=1.0.0.0参照のFileNotFoundExceptionで開始不能。現本体との参照版の不一致として切り分け、同プロジェクトを上記と同じ既存分離出力へ再ビルド（約８秒、警告・エラー０）後の実行はコード０。540pでプライマリー退避復帰・左右・連続トラック・面接続・予約保持のチェック成功。ログaudit-latest-direct-build.log／audit-latest-direct.log。再ビルド前の失敗をゲーム内クリック不具合とは扱わない。
- 新規確認不具合なし。未確認：赤チェック後の地点再クリックを実入力で独立検査する項目（今回は候補集合維持まで）、全Seed／人数、専用直送便Demo、全中間フレーム、利用者ゲーム併存。次回は実地点再クリックの検査、または残るDemo経路へ。既存ログ／画像・出力先を再利用。Norton保留の直接証拠・セキュリティ設定変更なし。

### DEMO-0013：DemoChecksUiが開戦プライマリー遷移の検査で停止する

- 状態：完了（2026-11-04 01:36:41 JST。内政／配送／軍事の分離にDemoと検証を追従。詳細はdemo-click-fixes.md）。確認日時：2026-11-04 01:05～01:06 JST。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584、開始・再現終了時Git差分なし。対象：tests/DemoChecksUi/Program.cs:28、Game1.DemoDomestic.cs／Game1.DemoChecks.csの段階30。
- 再現手順：DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoChecksUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-serversで現本体へ更新。ALSOFT_DRIVERS=null、dotnet artifacts/corner-lesson/DemoChecksUi.dllを２回実行。
- 期待：地点操作後の緑ハンドルをクリックし閉鎖後、実開戦ボタンのクリックでstage31・warPlanning=trueとなり、出撃即予約→赤チェック→閉じる→ターン終了の実描画検査へ進む。
- 実際：２回ともscenario0・step1の「Demo opens actual war primary action」で例外終了（コード-532462766）。直前の緑チェックとインスペクター閉鎖のチェックは通過した。失敗条件はstage／warPlanning／クリック矩形をまとめたassertであり、今回どの値が違うか未分離。ゲーム不具合かfixtureと現UIの不整合かは未確定。赤以降の４条件を今回成功扱いしない。
- 解像度／条件：最初の展開図1920×1080、PrepareCampaignDuel(123,456,789,12,0,1)、18FPSではなくテストの1.8秒刻みStepDemo。証拠：artifacts/demo-click-audit/audit-latest-checks.log、audit-latest-checks-recheck.logの同一例外とスタック。audit-latest-checks-build.logは成功（警告・エラー０、43.22秒）。demo-checks-0-0.pngは今回の緑段階までの描画。過去の赤段階画像を今回の成功証拠にしない。

### 実行履歴：2026-11-04 01:00回（トリガー01:04）

- 開始：2026-11-04 01:04:26 JST。終了：2026-11-04 01:06:40 JST。状態：完了（部分点検、DEMO-0013追加）。AGENTS、README、両記録・Git差分を最初に確認。HEAD上記、ゲーム／テスト差分なし。別チャットidle、開始時ゲーム／dotnet／MSBuildなし。セーブ・勲章操作、ゲーム／テスト変更、プロセス停止・コミット・プッシュ・公開なし。
- 緑／赤の追加経路を優先して現コードで再ビルドし、上記停止を再現。ビルド・起動待ちは短いポーリングで追跡し、Norton保留とは断定しない。実行開始後同じassertで２回停止したため起動遅延と区別する。既存分離出力と最新ログ名を再利用。
- 未確認：現HEADでの赤チェック以降、チェック済み地点の実再クリック、直送便退避復帰・左右、他Demo経路。次回修正回は0013のstage／warPlanning／実矩形・モード変更を切り分け、通常入力とfixtureの整合を確認する。全経路正常とは扱わない。

### 実行履歴：2026-11-04 02:00回（トリガー01:57）

- 開始：2026-11-04 01:57:57 JST。終了：2026-11-04 01:59:08 JST。状態：完了（部分点検）。AGENTS、README、両記録とGit差分を最初に確認。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。別チャットidle、開始時ゲーム／dotnet／MSBuildなし。先行修正回は01:36完了。DEMO-0013のゲーム／テスト差分を保持、終了前numstatも同じ。本回ゲーム／テスト無変更、実セーブ・勲章操作・停止・コミット・プッシュ・公開なし。
- 優先直送便を現本体で再ビルド。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DirectDeliveryUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：01:58頃要求、約８秒で警告・エラー０。ALSOFT_DRIVERS=null、同出力DirectDeliveryUi.dllはコード０。プライマリー退避復帰、移動中入力抑止、×／Escape、左右、連続トラック、540p回転面接続・予約保持が成功。ログaudit-latest-direct-build.log／audit-latest-direct.log。direct-17.png（960×540）を開き左右インスペクターと中央地図を目視。
- DemoChecksUi.dllは現本体でコード０。地星儀／展開図×1080p／540pの４条件で、緑チェック→閉鎖→内政終了／配送→軍事の開戦→地点→出撃確定即予約→赤→閉鎖→軍事終了、進軍とチェック消去を確認。ログaudit-latest-checks.log。削除済み最終予約ボタンを使わず、NodeCheckButton／WarButtonの実矩形と地点ヒットを検査する。DEMO-0013再発なし。
- FOLDERVERSE_DEMO_TEST_SEED=14、DemoUi.dll：コード０。世界／人物／配置=1523819089／1450276581／1454051393。自然内政・配送後の軍事巡回・出兵Transport・戦闘／拒否／終了ループ成功（audit-latest-seed14.log）。３実行は01:58:51までに終了、短いポーリングで追跡。Norton保留の直接証拠・設定変更なし。
- 新規確認不具合なし。未確認：チェック済み地点の独立した実再クリック、全Seed／人数、専用直送便Demo、全中間フレーム、利用者ゲーム併存。次回は既存４条件テストから独立した再クリックの診断を優先し、同じ成功範囲の反復だけで未確認を解消した扱いにしない。既存出力・ログ／画像名を再利用。

### 実行履歴：2026-11-04 03:00回

- 開始：2026-11-04 03:01:58 JST。終了：2026-11-04 03:03:19 JST。状態：完了（部分点検）。AGENTS、README、両記録・Git差分を最初に確認。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。別チャットidle、ゲーム／dotnet／MSBuildなし。先行修正と点検は終了済み。DEMO-0013のゲーム／テスト差分を保持し、本回ゲーム・既存テスト無変更（終了前numstat一致）。セーブ・勲章操作、停止・コミット・プッシュ・公開なし。
- 前回まで残っていた赤チェック済み地点の再クリックを優先。artifacts/demo-click-audit/ProbeCheckedClick.cs.txtと同名csprojを診断用に作成し、既存DemoChecksUiの４条件fixtureに赤チェック後の実地点再選択を追加。ResetInvasionTarget後に通常の投影位置OperationPositionから論理画面座標を取得し、通常のMovementClick入力処理へ渡す。選択値を直接成功に書き換えない。予約・赤メモ保持、選択地点一致、後続の閉鎖・ターン終了・実進軍まで検査。
- DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/ProbeCheckedClick.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：03:02頃要求、約10秒で警告・エラー０。ALSOFT_DRIVERS=null、同出力ProbeCheckedClick.dll：約８秒でコード０。展開図／地星儀×1920×1080／960×540の全４条件でtarget9の再選択成功、論理クリック座標(1440,552)。ログaudit-latest-reclick-build.log／audit-latest-reclick.log。
- 緑→閉鎖→内政終了・配送→軍事開戦→即予約→赤→閉鎖→独立ターン終了の元の４条件チェックも成功。赤の強調除外後も再クリックが成功し予約／メモが残ることを確認。demo-checks-2-6.png（960×540）を開き再選択後の赤チェック・ハンドル・予約１件を目視。診断は論理座標を通常入力処理に渡しており、OSの物理マウスからの再クリック検査とは区別する。
- 優先直送便退避復帰・左右は前回02:00の成功後ゲームコード変更がなく、今回は追加起動なし。新規確認不具合なし。未確認：物理マウスの再クリック、全Seed／人数、専用直送便Demo、全中間フレーム、利用者ゲーム併存。次回は残るDemo経路を広げるか物理座標の再クリックを追加する。既存分離出力・画像名を再利用。Norton保留の直接証拠・設定変更なし。

### DEMO-0014：DemoInspectorUiが長期輸送ボタンの検査に到達できない

- 状態：完了（2026-11-04 04:35:32 JST、修正記録参照）。2026-11-04 04:01 JST、HEAD=015d7ce960d7262ca70920f6d803fd38849d5584、DEMO-0013の修正差分あり。対象は tests/DemoInspectorUi/Program.cs の描画フレーム4～5。
- 再現は `dotnet build tests/DemoInspectorUi --no-restore -p:OutputPath=artifacts/corner-lesson/ -v quiet --disable-build-servers` 相当（実行時は絶対出力パス）でビルドし、ALSOFT_DRIVERS=nullで `dotnet artifacts/corner-lesson/DemoInspectorUi.dll` を起動。世界123／人物456／配置789、1920×1080論理画面。既存分離出力を使用。
- 期待は長期輸送の現在矩形・遷移を検証できること。実際は「Transport click misses primary action」で例外終了。テストはフレーム4の閉鎖後に次が段階12と仮定するが、現コードの内政では段階19に進む。通常プレイの矩形ずれは未確認。証拠は artifacts/demo-click-audit/audit-latest-inspector.log と audit-latest-inspector-build.log（警告・エラー０）。

### 実行履歴：2026-11-04 04:00回

- 開始 04:00:58 JST、終了 2026-11-04 04:02:02 JST。部分点検完了。事前にAGENTS・引き継ぎ・両記録とGit差分を確認。別チャットはidle、ゲーム／ビルドプロセスなし。ゲーム・既存テストの変更なし。
- 既存の現本体でSeed15のDemoUiはコード０。世界1789498680／人物634231350／配置1739388701。地図説明から内政・軍事・戦闘／拒否／終了の検査成功。証拠は audit-latest-seed15.log。DemoInspectorUiは上記の箇所で停止。
- 今回の未確認範囲は、停止以降の工場・長期輸送、直送便の退避復帰／左右の再実行、物理マウスによる赤チェック地点再選択。次回修正はDEMO-0014のテスト前提を実フェーズに合わせ、後続の矩形・遷移・描画まで検証する。

### 実行履歴：2026-11-04 05:00回

- 開始 05:00:29 JST、終了 2026-11-04 05:01:02 JST。部分点検完了。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584、DEMO-0013／0014の既存差分あり。AGENTS・引き継ぎ・両記録とGit差分を先に確認。別チャットidle、開始時にゲーム／ビルドプロセスなし。
- 現本体の既存分離出力でALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=16。dotnet artifacts/corner-lesson/DemoUi.dllはコード０。世界2055178271／人物1965669766／配置2024726009。地図切替、配送後の軍事巡回、戦闘・拒否・終了ループを確認。audit-latest-seed16.log。
- 同出力DirectDeliveryUi.dllはコード０。540pのプライマリー退避復帰、左右インスペクター、連続トラック操作、面接続・予約保持と描画が通過。audit-latest-direct.log。直送便の個別操作検証で、専用Demoの自然巡回は未確認。
- 新規不具合なし。緑／赤の４条件は前回の結果を保持し、今回は再実行なし。次回は物理座標からの赤チェック地点再選択、または未確認のDemo経路へ。ゲーム・既存テストに変更なし。

### 実行履歴：2026-11-04 06:00回

- 開始 05:59:00 JST、終了 2026-11-04 05:59:37 JST。部分点検完了。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。AGENTS、引き継ぎ、両記録とGit差分を確認。先行修正完了、別チャットidle、起動中のゲーム・検証プロセスなし。既存のDEMO-0013／0014差分を保持。
- Demo.cs、DemoDomestic.cs、DemoChecks.csでハンドルとプライマリーの現在矩形参照を照合。既存の分離出力でALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=17のDemoUi.dllを実行しコード０。世界173374215／人物1149624535／配置162579670。配送後の軍事巡回、地図切替と戦闘／拒否／終了の検査を通過。証拠は artifacts/demo-click-audit/audit-latest-seed17.log。
- 新規不具合なし。直送便の退避復帰と左右、長期輸送の個別操作は前回までに確認した範囲を維持し、今回は再起動していない。専用直送便Demo、全Seed／人数、赤チェック済み地点の物理再クリックは未確認。次回は物理座標の再クリックか、未確認のDemo経路から続ける。ゲーム・既存テストに変更なし。

### 実行履歴：2026-11-04 07:00回

- 開始 06:57:01 JST、終了 2026-11-04 06:58:06 JST。部分点検完了。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584、既存のDEMO-0013／0014差分を保持。AGENTS・引き継ぎ・両記録とGit差分を確認。先行点検完了、別チャットidle、開始時にゲーム／ビルドプロセスなし。
- 前回未確認だった赤チェック地点のウィンドウ座標入力を検査。既存の診断用 ProbeCheckedClick.cs.txt で、地点9の投影位置から CanvasBounds を使ってウィンドウ上の座標を算出し、通常の HandleInput に押下 MouseState を入力。再選択時だけDemoを無効にし、マウス入力でDemo自体を終了する分岐を回避した。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/ProbeCheckedClick.csproj --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー０。ALSOFT_DRIVERS=nullで既存出力の ProbeCheckedClick.dll はコード０。地星儀／展開図×1920×1080／960×540の４条件で座標(1440,552)／(720,276)から地点を再選択し、赤チェック・予約を保持。緑チェックから軍事終了・進軍・消去までの元の検査も通過。ログ audit-latest-reclick-build.log／audit-latest-reclick.log。
- 新規不具合なし。画面座標とゲームの通常入力経路を検証した。OSからの実マウス入力、専用直送便Demo、全Seed／人数、全中間描画は未確認。直送便の退避復帰・左右は05:00回から変更がないため再起動せず。次回は未点検のDemo経路から続ける。ゲーム・既存テストの変更なし。

### 実行履歴：2026-11-04 08:00回

- 開始 08:01:32 JST、終了 2026-11-04 08:02:29 JST。部分点検完了。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584、既存のDEMO-0013／0014差分を保持。AGENTS、引き継ぎ、両記録、Git差分を確認。別チャットidle、先行作業完了、ゲーム／ビルドプロセスなし。
- 残る初期設定経路を確認。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoSetupUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー０。ALSOFT_DRIVERS=nullで同出力のDemoSetupUi.dllはコード０。世界123／人物456／配置789、18FPS、1920×1080／960×540、1人／2人設定で配置停止・確定、実タイルの選択・確認とLandConquest遷移を検査。2人・540pでは(1698,719)と(1731,202)がそれぞれ実タイル矩形内、確認位置(758,1045)で確定した。証拠は audit-latest-setup-build.log／audit-latest-setup.log。
- 新規不具合なし。緑／赤とプライマリー・直送便の結果は前回までの検証範囲を維持し、今回は初期設定に絞った。未確認は専用直送便Demo、全人数／Seed、全中間フレーム、OS入力での赤チェック済み地点再選択。次回は残る自然巡回経路かコード変更後の優先UI回帰へ。ゲーム・既存テストの変更なし。

### 実行履歴：2026-11-04 09:00回

- 開始 09:01:32 JST、終了 2026-11-04 08:56:31 JST。部分点検完了。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584、既存のDEMO-0013／0014差分を保持。AGENTS、引き継ぎ、両記録とGit差分を確認。別チャットidle、ゲーム／ビルドプロセスなし。
- 既存分離出力でALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=18のDemoUi.dllを実行しコード０。世界439053806／人物333579304／配置447916978。18FPSの地星儀／展開図・面接続、配送後の軍事、戦闘・拒否・終了を検査。証拠は artifacts/demo-click-audit/audit-latest-seed18.log。
- 新規不具合なし。プライマリー・直送便・緑／赤は関係コードに前回から変更がなく、今回の個別再検査は未実施。専用直送便Demo、全Seed／人数、全中間描画、OS入力での赤チェック地点再選択は未確認。次回は未点検分岐か関係コード変更後の優先UI回帰から続ける。ゲーム・既存テストの変更なし。

### 実行履歴：2026-11-04 09:00枠（トリガー08:56:02 JST）

- 検証開始 09:33:24 JST、記録終了 09:33:40 JST（PC時計）。部分点検完了。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。AGENTS・引き継ぎ・両記録とGit差分を確認。別チャットidle、先行作業完了、ゲーム／ビルドプロセスなし。既存のDEMO-0013／0014差分を保持。
- 既存分離出力でALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=19のDemoUi.dllを実行しコード０。世界704733397／人物1665017720／配置733254286。18FPSの地図切替・面接続、配送後の軍事、戦闘・拒否・終了を検査。証拠は artifacts/demo-click-audit/audit-latest-seed19.log。
- 新規不具合なし。プライマリー・直送便・緑／赤の個別テストは既存の結果を参照。専用直送便Demo、全Seed／人数、全中間描画、OS入力での赤チェック地点再選択は未確認。次回は未点検分岐または関連コード変更後の回帰へ。ゲーム・既存テストの変更なし。

### 実行履歴：2026-11-04 10:00回

- トリガー 10:03:33 JST、記録終了 2026-11-04 10:04:43 JST（PC時計）。部分点検完了。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。AGENTS、引き継ぎ、両記録、Git差分を確認。別チャットidle、起動中のゲーム／検証プロセスなし。既存DEMO-0013／0014差分を保持。
- 残る地点選択・格納庫・和平移動を確認。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoActionsUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー０。ALSOFT_DRIVERS=nullで同出力のDemoActionsUi.dllはコード０。1080p／540pで実地点ヒット、地図／地星儀と回転・視野外からの復帰、部位のドラッグ＆ドロップ、和平予約と独立したターン終了を検証。和平6条件は地点89に予約、終了カーソル(707,938)。証拠 audit-latest-actions-build.log／audit-latest-actions.log。
- 新規不具合なし。優先プライマリー、直送便、緑／赤は関係コードに変更がなく既存の点検結果を参照。未確認は専用直送便Demo、全人数／Seed・中間フレーム、OSマウスからの赤チェック再選択。次回は未点検分岐または関係コードの変更後に優先UIを確認。ゲーム・既存テストの変更なし。

### 実行履歴：2026-11-04 11:00回

- 検証開始 11:02:29 JST、記録終了 2026-11-04 11:02:59 JST（PC時計）。部分点検完了。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。AGENTS・引き継ぎ・両記録とGit差分を確認。先行作業完了、別チャットidle、起動中のゲーム／ビルドなし。DEMO-0013／0014の差分を保持。
- 既存分離出力でALSOFT_DRIVERS=nullのDemoChecksUi.dllはコード０。展開図／地星儀×1080p／540pで緑・赤ハンドルのクリック、地点選択→出撃選択確定による即予約→赤→閉鎖→ターン終了、進軍とチェック消去を確認。DemoChecks.csの段階34はInvasionPickerDone、段階35はNodeCheckButtonを参照する。証拠 audit-latest-DemoChecksUi.log。
- 同出力DirectDeliveryUi.dllはコード０。540pでプライマリー退避復帰・左右・連続トラック・回転面接続・予約保持と描画を確認。証拠 audit-latest-DirectDeliveryUi.log。現Demoのソースに専用直送便経路は見当たらず、直送便の結果は個別UI検査の範囲。
- 新規不具合なし。全Seed／人数、全中間描画、OSマウス入力での赤地点再選択は未確認。次回はコード変更時の優先UI回帰、または未点検分岐の検査へ。ゲームと既存テストの変更なし。

### DEMO-0015：Seed20のDemoUiが初回配送前に停止する

- 状態：修正・検証完了（2026-11-04 12:29:58 JST）。詳細は demo-click-fixes.md の DEMO-0015。2026-11-04 12:01～12:02 JST（PC時計）。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584、DEMO-0013／0014の既存差分あり。標準1920×1080論理キャンバス。
- 再現は既存分離出力でALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=20を設定し、dotnet artifacts/corner-lesson/DemoUi.dll を実行。世界970412988／人物848972489／配置1018591594。初回巡回で配送Transportへ到達することを期待。
- 実際は tests/DemoUi/Program.cs:45 の「Demo did not enter propagation」で例外終了。WorldStatus、地図段階5、内政段階0、ターン0、visits=0、クリック(1766,160)。証拠は artifacts/demo-click-audit/audit-latest-seed20.log。DEMO-0012の初回Transport後の検査とは停止箇所が異なる。原因と実時間の通常Demoでの再現は未確認。

### 実行履歴：2026-11-04 12:00回

- 検証開始 12:01:54 JST、記録終了 2026-11-04 12:02:31 JST（PC時計）。部分点検完了。AGENTS、引き継ぎ、両記録、Git差分を確認。別チャットidle、先行点検完了、起動中のゲーム／ビルドなし。ゲーム・既存テストの変更なし。
- Seed20の自然巡回は上記で停止。直送便・緑／赤ハンドルは前回の個別検査を参照。今回のSeed20では内政操作・配送以降、全中間描画を確認できていない。次回修正は現コードで再現し、内政段階0の地点候補・投影・ヒットとテストの進行間隔を切り分ける。

### 実行履歴：2026-11-04 13:00回

- 開始 13:01:57 JST、終了 13:02:51 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83、開始時Git差分なし。先行修正の完了記録を確認。起動中のゲーム・検証プロセスなし。既存の分離出力を使用。
- ALSOFT_DRIVERS=nullで dotnet artifacts/corner-lesson/{DemoUi,DemoInspectorUi,DirectDeliveryUi,DemoChecksUi}.dll を順次実行し、すべて終了コード0。DemoUiはSeed21（世界1236092579／人物32927258／配置1303928902）で初回配送クリック(256,998)、配送開始・地図復帰、終了ループを確認。
- DemoInspectorUiは1080p／540pでプライマリー・インスペクター開閉と工場6プランの矩形・実遷移を照合。DirectDeliveryUiは540pで退避復帰・左右・連続トラック・予約保持と描画を確認。DemoChecksUiは地星儀／展開図×1080p／540pで緑／赤の実クリック、地点選択→出撃選択確定による即予約→赤→閉じる→ターン終了、表示分離・チェック消去を確認。証拠は artifacts/demo-click-audit/audit-latest-{DemoUi,DemoInspectorUi,DirectDeliveryUi,DemoChecksUi}.log。
- 新規不具合なし。専用直送便の自然Demo経路はソース上確認できず、今回も個別UIの検査範囲。赤チェック地点のOSマウスでの再選択、全Seed／人数と全中間フレームは未確認。次回は赤チェック地点の再選択または未点検Seed22から続ける。ゲームコード・既存テスト変更なし。

### 実行履歴：2026-11-04 14:00回

- 開始 14:01:46 JST、終了 14:02:07 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時差分は13:00回の点検記録のみ。先行修正回は対象なしで終了済み。開始時にゲーム・検証プロセスなし。
- 既存分離出力で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=22 として dotnet artifacts/corner-lesson/DemoUi.dll を実行。終了コード0。世界1501772170／人物1364365674／配置1589266210。論理1920×1080でクリック(256,998)、初回配送入り、地図復帰、地星儀／展開図切替、18FPSの端地点移動、拒否時の処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed22.log。
- 新規不具合なし。プライマリー、左右インスペクター、直送便、緑／赤の個別回帰は関係コードに差分がなく13:00回の結果を参照。専用直送便の自然Demo、赤チェック地点のOSマウス再選択、全Seed／人数と全中間フレームは未確認。次回はOS入力による再選択またはSeed23から続ける。ゲーム・テストの変更なし。

### 実行履歴：2026-11-04 15:00回

- 開始 14:58:46 JST、終了 14:59:07 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時の差分は点検記録のみ。先行修正回は対象なしで終了。起動中のゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=23 で dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。世界1767451761／人物548320443／配置1874603518。論理1920×1080でクリック(256,998)の初回配送入りと地図復帰、地星儀／展開図、18FPSでの端地点移動、拒否時の処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed23.log。
- 新規不具合なし。優先UIの個別回帰は関係コードの差分がないため13:00回の結果を参照。専用直送便の自然Demo、赤チェック地点のOS入力による再選択、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed24。ゲームとテストの変更なし。

### 実行履歴：2026-11-04 16:00回

- 開始 15:57:47 JST、終了 15:58:08 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時の差分は点検記録のみ。先行修正回は対象なしで終了。起動中のゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=24 で既存分離出力の dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。世界2033131352／人物1879758859／配置12457179。論理1920×1080でクリック(256,998)の初回配送入り・地図復帰、地星儀／展開図の切替、18FPSの端地点移動、拒否時の処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed24.log。
- 新規不具合なし。優先UIの個別回帰は関係コードに差分がないため13:00回を参照。専用直送便の自然Demo、赤チェック地点のOS入力による再選択、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed25から続ける。ゲーム・テストの変更なし。

### 実行履歴：2026-11-04 17:00回

- 開始 17:03:19 JST、終了 17:03:39 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時の差分は点検記録のみ。先行修正は対象なしで終了。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=25 で既存の dotnet artifacts/corner-lesson/DemoUi.dll を実行。終了コード0。世界151327296／人物1063713628／配置297794487。論理1920×1080で初回配送クリック(256,998)と地図復帰、地星儀／展開図切替、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed25.log。
- 新規不具合なし。優先UIの個別回帰は関係コードの差分がなく13:00回の結果を参照。専用直送便の自然Demo、赤チェック地点のOS入力での再選択、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed26。ゲーム・テストの変更なし。

### 実行履歴：2026-11-04 18:00回

- 開始 17:58:21 JST、終了 17:58:41 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時の差分は点検記録のみ。先行修正は対象なしで終了。起動中のゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=26 で既存の dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。世界417006887／人物247668397／配置583131795。論理1920×1080で初回配送クリック(256,998)と地図復帰、地星儀／展開図の切替、18FPSの端地点移動、拒否時の処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed26.log。
- 新規不具合なし。優先UIの個別回帰は関係コードに差分がないため13:00回を参照。赤チェック地点のOS入力での再選択、専用直送便の自然Demo、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed27。ゲーム・テストの変更なし。

### 実行履歴：2026-11-04 19:00回

- 開始 19:03:21 JST、終了 19:03:41 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時の差分は点検記録のみ。先行修正は対象なしで終了。起動中のゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=27 で既存の dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。世界682686478／人物1579106813／配置868469103。論理1920×1080で初回配送クリック(256,998)と地図復帰、地星儀／展開図の切替、18FPSの端地点移動、拒否時の処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed27.log。
- 新規不具合なし。優先UIの個別回帰は関係コードに差分がないため13:00回を参照。赤チェック地点のOS入力での再選択、専用直送便の自然Demo、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed28。ゲーム・テストの変更なし。

### 実行履歴：2026-11-04 20:00回

- 開始 19:59:52 JST、終了 20:00:13 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時差分は点検記録のみ。先行修正は対象なしで終了。起動中のゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=28 で既存の dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。世界948366069／人物763061582／配置1153806411。論理1920×1080で初回配送クリック(256,998)と地図復帰、地星儀／展開図切替、18FPSでの端地点移動、拒否時の処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed28.log。
- 新規不具合なし。優先UIの個別回帰は関係コードに差分がないため13:00回を参照。赤チェック地点のOS入力での再選択、専用直送便の自然Demo、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed29。ゲーム・テストの変更なし。

### 実行履歴：2026-11-04 21:00回

- 開始 21:01:52 JST、終了 21:02:12 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時の差分は点検記録のみ。先行修正は対象なしで終了。起動中のゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=29 で既存の dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。世界1214045660／人物2094499998／配置1439143719。論理1920×1080で初回配送クリック(256,998)と地図復帰、地星儀／展開図切替、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed29.log。
- 新規不具合なし。優先UIの個別回帰は関係コードに差分がないため13:00回を参照。赤チェック地点のOS入力による再選択、専用直送便の自然Demo、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed30。ゲーム・テストの変更なし。

### 実行履歴：2026-11-04 22:00回

- 開始 22:03:52 JST、終了 22:04:13 JST（PC時計）。部分点検完了。HEAD=fd82fa9f0d143a812e2b0441583c05354061cc83。開始時差分は点検記録のみ。先行修正は対象なしで終了。起動中のゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=30 で既存の dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。世界1479725251／人物1278454767／配置1724481027。論理1920×1080で初回配送クリック(256,998)と地図復帰、地星儀／展開図切替、18FPSでの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-seed30.log。
- 新規不具合なし。優先UIの個別回帰は関係コードに差分がないため13:00回を参照。赤チェック地点のOS入力での再選択、専用直送便の自然Demo、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed31。ゲーム・テストの変更なし。

### 実行履歴：2026-11-04 23:00回

- 開始 23:04:58 JST、終了 23:06:52 JST（PC時計）。部分点検完了。HEAD=5a82a870569f23ad9f5543acd79133b2e2fb6963。開始時にゲーム・検証プロセスなし。コンピューター内政・軍事表示とフェーズ処理の既存差分を保持。
- 既存分離出力 artifacts/corner-lesson に DemoUi、DemoChecksUi、DemoInspectorUi、DirectDeliveryUi を dotnet build tests/<対象> --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers で再ビルド。全て警告・エラー0。ALSOFT_DRIVERS=null で各DLLを実行し、全て終了コード0。証拠は artifacts/demo-click-audit/audit-latest-<対象>.log。
- DemoUi Seed31（世界1745404842／人物462409536／配置2009818335）でクリック(256,998)から初回配送と地図復帰、18FPSの端地点移動と終了ループを確認。DemoChecksUiは1080p／540p×地星儀／展開図で緑・赤の実クリック、即予約、閉じる、ターン終了と消去を検査。DemoInspectorUiは同解像度で工場・プライマリーと開閉、DirectDeliveryUiは540pで退避復帰・左右・予約保持・描画を検査。
- 新規不具合なし。専用直送便の自然Demo、赤チェック地点のOS入力による再選択、全Seed／人数と全中間フレームは未確認。次回は再選択またはSeed32。ゲーム・テストの変更なし。

### DEMO-0016：コンピューターの操作表示が通常の地点・出撃選択を通らない

- 状態：検証待ち（一部対応済み。ピッカー確定直後のカーソル残留は修正・実描画検証済み）。2026-11-05 00:04～00:06 JST。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f、開始時差分なし。960×540、論理1920×1080。
- 再現：コンピューターの［操作を表示］をオンにして軍事ターンを終える。自動検証は CountrySettingsUi のフレーム12。同画面でフィリアに切り替わった後、目標と出撃元の専用パネルが出て、手がその出撃行を叩く（論理429,356、実画面約215,178）。
- 期待：通常の地図・地点選択、出撃元と出撃選択の確定、予約成立、閉じる、ターン進行が順に見える。実際：Game1.ComputerMarches.cs は ComputerMarchOrder の中心に手を描き、通常の InvasionPickerDone や予約入力を経ず、表示後に元のプレイヤーへ戻して指示を一括解決する。画面切り替えと指示そのものは確認できた。
- 証拠：artifacts/corner-lesson/country-settings-12.png、artifacts/demo-click-audit/audit-latest-CountrySettings.log。既存のDemoクリック不具合とは別の自動操縦経路。

### 実行履歴：2026-11-05 00:00回

- 開始 00:04:31 JST、終了 00:05:34 JST。部分点検完了。先行修正は対象なしで終了、開始時にゲーム・検証プロセスなし。ゲーム・テストの変更なし。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で dotnet artifacts/corner-lesson/CountrySettings.dll --ui は終了コード0。軍事の実描画で専用表示との差を再現。内政・軍事とも征服者切り替え、Esc相当のスキップと人間への復帰をテストで確認。
- 内政は Game1.ComputerDomestic.cs のソースで、専用矩形上のカーソルと直接 Assemble を確認。通常の地点・格納庫操作との描画比較と組み立ての実結果は未確認。次回点検はこの比較と、未確認の赤チェック地点のOS再選択またはSeed32へ。Demo・優先UIの個別回帰は前回23:00回の結果を参照。

### 実行履歴：2026-11-05 01:00回

- 開始 00:58:58 JST、終了 00:59:37 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行修正終了とゲーム・検証プロセスなしを確認。DEMO-0016の既存差分を保持。
- ALSOFT_DRIVERS=null で既存分離出力 CountrySettings.dll --ui を再実行し終了コード0。540pでフィリアの通常出撃ピッカーと出撃3体を確認。確定で二重登録せず、スキップで人間へ復帰。証拠 artifacts/corner-lesson/country-settings-12.png と artifacts/demo-click-audit/audit-latest-CountrySettings.log。
- DEMO-0016の残項目：同画像ではピッカーを開いた直後、背面の出撃元ボタンを指したカーソルが前面のロボットカードに重なる。期待はピッカー表示後に次の操作先を指すこと。実際は Game1.ComputerMarches.cs の描画が最後のクリック位置を保持し、選択3体は個別クリックなしで設定済み。既報に追記し重複登録を避ける。
- 同出力 DemoUi.dll をSeed32で実行し終了コード0。論理1920×1080、世界2011084433／人物1793847952／配置147671996。初回配送クリック(256,998)、地図復帰、18FPSの端地点移動、終了ループを確認。証拠 audit-latest-DemoUi.log。優先UIの個別回帰は直近結果を参照。
- 内政の実描画、軍事の赤チェック、全指示のスキップなし完走、OS入力での赤地点再選択、専用直送便の自然Demoは未確認。次回は内政の実描画比較または軍事の残項目から続ける。ゲーム・テスト変更なし。

### DEMO-0017：CPU内政の組み立てが通常の格納庫操作を通らない

- 状態：検証待ち（一部対応済み。通常格納庫での結果表示を検証。修正記録参照）。2026-11-05 02:04 JST開始の点検。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。DEMO-0016の既存差分あり。960×540、論理1920×1080。
- 再現：CPUの［操作を表示］をオンにして人間の内政を終える。CountrySettingsUiのフレーム12に一時診断を挿入し、最初の対象であるフィリア（owner=1）の地点91に頭・胴・脚を補充。UpdateComputerDomesticを1.7秒進める。
- 期待：CPU視点の地図で地点を選び、通常の格納庫を開いて組み立ての操作と結果が見える。実際：CPU視点への切り替えと組み立ての成立は確認したが、手は専用パネルの「ロボットを組み立てました」行（論理429,356）を指す。Game1.ComputerDomestic.cs は格納庫入力を経ず Assemble を呼ぶ。軍事のDEMO-0016とは別経路。
- 証拠：artifacts/corner-lesson/audit-latest-computer-domestic.png、artifacts/demo-click-audit/audit-latest-computer-domestic.log。

### 実行履歴：2026-11-05 02:00回

- 開始 02:04:29 JST、終了 02:05:32 JST。部分点検完了。先行修正完了、開始時にゲーム・検証プロセスなし。ゲームコード無変更。一時診断を実行後、テストソースを元のバイト列に復元。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で同出力の CountrySettings.dll --ui は終了コード0。診断入りDLLは同じ検証出力を使用。CPU内政の描画・組み立てとスキップ後の人間復帰、既存の軍事ピッカー検査が通過。
- Demo・優先UIの回帰は既存結果を参照。全CPUのスキップなし完走、赤チェック地点のOS入力での再選択、専用直送便の自然Demoは未確認。次回はDEMO-0017の通常格納庫経路の検討、またはSeed33の巡回へ。

### 実行履歴：2026-11-05 03:00回

- 開始 03:01:02 JST、終了 03:01:41 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。DEMO-0016／0017の既存差分を保持。先行修正完了、開始時にゲーム・検証プロセスなし。ゲームとテストの変更なし。
- ALSOFT_DRIVERS=null で既存分離出力 CountrySettings.dll --ui、DemoUi.dll（Seed33）、DemoChecksUi.dll は終了コード0。証拠 artifacts/demo-click-audit/audit-latest-<対象>.log。CPU内政の地点・通常格納庫・組み立て、軍事の通常ピッカー確定とスキップ復帰を再確認。540pの画像は audit-latest-computer-domestic.png と country-settings-12.png。
- DemoUiは論理1920×1080、世界129280377／人物977802721／配置433009304。初回配送クリック(256,998)、地図復帰、18FPSの端地点移動、終了ループを確認。DemoChecksUiは1080p／540p×地星儀／展開図の緑・赤クリック、即予約、閉鎖、ターン終了と消去を確認。
- 新規不具合なし。直送便と左右インスペクターの個別回帰は直近結果を参照。CPUの個別ドラッグ・チェック、スキップなしの全CPU完走、OS入力による赤地点再選択、専用直送便の自然Demoは未確認。次回はスキップなし進行またはSeed34から続ける。

### 実行履歴：2026-11-05 04:00回

- 開始 03:56:30 JST、終了 03:57:12 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行修正終了、開始時ゲーム・検証プロセスなし。DEMO-0016／0017の差分を保持。
- CountrySettingsUiのスキップ呼び出し2か所を一時診断で0.5秒刻みの通常進行に置き換え、CPU内政全件から人間に復帰して配送へ進むこと、軍事の全指示から人間に復帰して次の内政に進むことを同じテスト世界で確認。通常格納庫の組み立て、出撃ピッカーの確定と二重予約防止の既存assertも通過。描画は960×540。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null の同出力 CountrySettings.dll --ui は終了コード0。証拠 artifacts/demo-click-audit/audit-latest-computer-no-skip.log。テストソースは元のバイト列に復元。分離出力のDLLには一時診断が残るため、標準のスキップ検証を再実施する前に再ビルドする。
- 新規不具合なし。画面ごとの中間描画や物理マウス入力、専用直送便の自然Demo、CPUの個別操作とチェックは未確認。優先UIの回帰は直近結果を参照。次回はSeed34またはCPUの個別操作の描画比較から続ける。ゲームコード無変更。

### 実行履歴：2026-11-05 05:00回

- 開始 05:03:03 JST、終了 05:03:53 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。DEMO-0016／0017の既存差分を保持。開始時にゲーム・検証プロセスなし。ゲーム・テスト無変更。
- ALSOFT_DRIVERS=null で既存分離出力の DemoUi.dll（Seed34）、DemoInspectorUi.dll、DirectDeliveryUi.dll を順次実行し終了コード0。証拠 artifacts/demo-click-audit/audit-latest-<対象>.log。論理1920×1080、Seed34は世界394959968／人物161757490／配置718346612。クリック(256,998)で初回配送入り、地図復帰、18FPSの端地点移動、終了ループを確認。
- プライマリー・工場6プラン・インスペクターの開閉を1080p／540pで照合。540pの直送便は退避復帰、左右、連続トラック、回転面接続、予約保持と描画を確認。
- 新規不具合なし。CPUの内政・軍事は04:00回の結果を参照。CPUの個別操作と緑／赤チェック、赤チェック地点のOSマウス再選択、専用直送便の自然Demoは未確認。次回はCPUの個別操作の描画比較またはSeed35から続ける。

### 実行履歴：2026-11-05 06:00回

- 開始 05:57:02 JST、終了 05:57:21 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行修正は新規対象なしで終了。開始時ゲーム・検証プロセスなし。DEMO-0016／0017の既存差分を保持。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=35 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界660639559／人物1493195906／配置1003683920。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSでの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPU内政・軍事の個別回帰は直近結果を参照。CPUの個別ドラッグ・チェック、赤地点のOS入力での再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed36。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 07:00回

- 開始 06:56:03 JST、終了 06:56:22 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行修正は新規対象なしで終了。開始時ゲーム・検証プロセスなし。DEMO-0016／0017の差分を保持。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=36 で既存分離出力の dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界926319150／人物677150675／配置1289021228。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPU内政・軍事は直近の個別検査を参照。CPUの個別ドラッグ・チェック、赤地点のOS入力での再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed37。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 08:00回

- 開始 08:01:04 JST、終了 08:01:26 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。DEMO-0016／0017の既存差分を保持。開始時にゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=37 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界1191998741／人物2008589091／配置1574358536。初回配送クリック(256,998)、地図復帰、18FPSの端地点移動、終了ループを確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- CPUの個別操作をソースで照合。軍事は出撃候補を一括選択して確定から閉鎖へ進む。内政は通常格納庫を開いた後にモデルを直接組み立てる。既報DEMO-0016／0017の残項目と一致。新規不具合なし。優先UIは直近の個別点検を参照。
- CPU個別操作とチェックの全中間描画、赤地点のOS入力での再選択、専用直送便の自然Demoは未確認。次回はCPU確定から閉鎖までのフレーム比較またはSeed38。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 09:00回

- 開始 08:56:35 JST、終了 08:57:27 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。DEMO-0016／0017の既存差分を保持。開始時にゲーム・検証プロセスなし。
- DEMO-0016に未解決の残項目を追加。CountrySettingsUiのフレーム13でCPUが出撃を確定した直後、次の閉鎖操作まで進める前に描画を取得。960×540ではピッカーが閉じて通常の開戦インスペクターに戻るが、手は消えた確定ボタンの位置（論理1415,822、実画面約708,411）に残る。期待は次の操作先である閉じるハンドルを指すか、次の操作まで手を表示しないこと。ピッカーを開いた直後に修正したずれとは逆方向の画面遷移。通常の予約操作は二重登録されず、既存の復帰assertも通過。
- 証拠 artifacts/corner-lesson/audit-latest-computer-confirm.png と artifacts/demo-click-audit/audit-latest-computer-confirm.log。dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0、ALSOFT_DRIVERS=null の CountrySettings.dll --ui は終了コード0。ゲームコード無変更。一時描画診断のテストソースは復元済み。DLL再利用時は診断入りであることに注意。
- 優先UIは直近の結果を参照。CPUのチェック、個別ドラッグ、物理マウス、専用直送便自然Demoは未確認。次回修正は確定直後のカーソル残留から。次の点検は閉鎖直後の描画またはSeed38。

### 実行履歴：2026-11-05 10:00回

- 開始 09:57:38 JST、終了 09:58:10 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行修正完了、起動中のゲーム・検証プロセスなし。DEMO-0016／0017の既存差分を保持。
- ALSOFT_DRIVERS=null で既存分離出力 DemoUi.dll（Seed38）と CountrySettings.dll --ui（前回の描画診断入り）を実行し、終了コード0。Seed38は論理1920×1080、世界1457678332／人物1192543860／配置1859695844。初回配送クリック(256,998)、地図復帰、18FPSの端地点移動、終了ループを確認。CPUは540pの通常格納庫・出撃選択・確定とスキップ後の復帰を再検査。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log／audit-latest-CountrySettings.log。
- 新規不具合なし。優先UIの個別回帰は直近結果を参照。閉鎖直後の手の非表示の実描画、CPUの個別操作・緑赤チェック、物理マウスによる赤地点再選択と専用直送便の自然Demoは未確認。次回は閉鎖直後の描画またはSeed39。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 11:00回

- 開始 10:57:07 JST、終了 10:57:30 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行修正は対象なしで終了。開始時にゲーム・検証プロセスなし。DEMO-0016／0017の差分を保持。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=39 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界1723357923／人物376498629／配置2145033152。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPU内政・軍事は直近の個別検査を参照。CPUの閉鎖直後の描画・個別操作・緑赤チェック、物理マウスによる赤地点再選択、専用直送便の自然Demoは未確認。次回は閉鎖直後の描画またはSeed40。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 12:00回

- 開始 12:02:47 JST、終了 12:03:10 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。開始時にゲーム・検証プロセスなし。DEMO-0016／0017の差分を保持。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=40 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界1989037514／人物1707937045／配置282886813。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPU内政・軍事は直近の個別検査を参照。CPUの閉鎖直後の描画・個別操作・緑赤チェック、物理マウスによる赤地点再選択、専用直送便の自然Demoは未確認。次回は閉鎖直後の描画またはSeed41。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 13:00回

- 開始 13:04:45 JST、終了 13:05:35 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。開始時にゲーム・検証プロセスなし。DEMO-0016／0017の既存差分を保持。
- CountrySettingsUiのフレーム13でCPUの出撃確定→閉鎖入力の直後を一時診断で検査。段階5と閉鎖アニメーション開始を確認し、960×540の実描画でインスペクターが退避を始める時点に手が残らないことを目視。確定時の二重予約防止とスキップ後の人間復帰も通過。DEMO-0016の閉鎖直後の描画を確認済みに追加。証拠 artifacts/corner-lesson/audit-latest-computer-close.png、artifacts/demo-click-audit/audit-latest-computer-close.log。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で同出力 CountrySettings.dll --ui は終了コード0。一時診断を加えたテストソースは元のバイト列へ復元。分離出力のDLLは診断入り。
- 新規不具合なし。優先UIとDemo巡回は直近の結果を参照。CPUの個別選択・ドラッグ、赤緑チェック、物理マウスによる赤地点再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed41。ゲームコード無変更。

### 実行履歴：2026-11-05 14:00回

- 開始 13:56:14 JST、終了 13:56:36 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行点検・修正終了、開始時にゲーム・検証プロセスなし。DEMO-0016／0017の既存差分を保持。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=41 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界107233458／人物891891814／配置568224121。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPUの通常格納庫・軍事確定・閉鎖は直近の個別検査を参照。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed42。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 15:00回

- 開始 14:58:46 JST、終了 14:59:09 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行点検・修正終了、開始時ゲーム・検証プロセスなし。DEMO-0016／0017の既存差分を保持。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=42 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界372913049／人物75846583／配置853561429。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPUの通常格納庫・軍事確定・閉鎖は直近の個別検査を参照。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed43。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 16:00回

- 開始 15:56:14 JST、終了 15:56:38 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行点検・修正終了、開始時ゲーム・検証プロセスなし。DEMO-0016／0017の既存差分を保持。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=43 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界638592640／人物1407284999／配置1138898737。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPUの通常格納庫・軍事確定・閉鎖は直近の個別検査を参照。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed44。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 17:00回

- 開始 17:01:22 JST、終了 17:01:51 JST。部分点検完了。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。先行点検・修正終了、開始時ゲーム・検証プロセスなし。DEMO-0016／0017の既存差分を保持。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=44 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界904272231／人物591239768／配置1424236045。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPUの通常格納庫・軍事確定・閉鎖は直近の個別検査を参照。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed45。ゲーム・テスト無変更。

### 実行履歴：2026-11-05 18:00回

- 開始 18:04:21 JST、終了 18:05:30 JST。中断。HEAD=1dfcb4ddf89a1f8ebdad0ae9231d6e9125195c6a。開始時にゲーム・検証プロセスなし。関係する軍事・移動・フェーズ・機能移転の18ファイルに既存差分。別チャットの実行は一覧で確認できず。既存変更を保持。
- 現行ソースの dotnet build tests/DemoUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は失敗。Game1.Invasion.cs の72、77、80行にマージ競合マーカーがあり CS8300。古いDLLを現行コードの検証結果として使用せず、実描画・Demo遷移・CPUの操作点検は未実施。
- ゲームコード無変更。次回は競合解消後に再ビルドし、プライマリー・出撃・緑赤チェックとCPU通常UIの回帰を優先。Seed45は未実施。既報DEMO-0016／0017は状態維持。

### 実行履歴：2026-11-05 19:00回

- 開始 19:02:47 JST、終了 19:02:58 JST。中断。Game1.Invasion.cs の72／77／80行に前回と同じ競合マーカー、Git状態UUを確認。既存の軍事・機能移転の差分を保持。再ビルド・実描画・Demo巡回は未実施。ゲーム・テスト無変更。
- 続きは競合解消後の現行コード再ビルドと優先UI／CPU通常UI回帰。Seed45は未実施、既報DEMO-0016／0017は状態維持。

### 実行履歴：2026-11-05 20:00回

- 開始 20:04:22 JST、終了 20:04:33 JST。中断。Game1.Invasion.cs の72／77／80行に同じ競合マーカーとGit状態UUを確認。既存差分を保持し、再ビルド・実描画・Demo巡回は未実施。ゲーム・テスト無変更。
- 競合解消後に現行コードを再ビルドし、優先UIとCPU通常UIの回帰から再開する。Seed45は未実施、DEMO-0016／0017は状態維持。

### 実行履歴：2026-11-05 21:00回

- 開始 20:56:48 JST、終了 20:56:56 JST。中断。Game1.Invasion.cs の72／77／80行に同じ競合マーカー、Git状態UUを確認。既存差分を保持。再ビルド・実描画・Demo巡回は未実施。ゲーム・テスト無変更。
- 続きは競合解消後の現行コード再ビルドと優先UI／CPU通常UI回帰。Seed45は未実施、DEMO-0016／0017は状態維持。

### 実行履歴：2026-11-05 22:00回

- 開始 21:55:20 JST、終了 21:55:29 JST。中断。Game1.Invasion.cs の72／77／80行に同じ競合マーカー、Git状態UUを確認。既存差分を保持。再ビルド・実描画・Demo巡回は未実施。ゲーム・テスト無変更。
- 続きは競合解消後の現行コード再ビルドと優先UI／CPU通常UI回帰。Seed45は未実施、DEMO-0016／0017は状態維持。

### 実行履歴：2026-11-05 23:00回

- 開始 22:59:25 JST、終了 23:00:44 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。差分・未解決競合なし、開始時にゲーム・検証プロセスなし。利用者依頼で競合解消済みの記録を確認し点検を再開。
- ALSOFT_DRIVERS=null で既存分離出力 DemoUi.dll（Seed46）と CountrySettings.dll --ui は終了コード0。論理1920×1080、世界1435631413／人物1106632953／配置1994910661。初回配送クリック(256,998)、地図復帰、18FPSの端地点移動、終了ループを確認。CPU通常格納庫の組み立て、通常出撃ピッカー、確定と二重予約防止、人間復帰の検査も通過。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log／audit-latest-CountrySettings.log。
- 新規不具合なし。優先UIの個別回帰は競合解消時と直近の検査結果を参照。CPU個別操作と緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed47。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 00:00回

- 開始 00:03:57 JST、終了 00:04:23 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時の差分は点検記録のみ、ゲーム・検証プロセスなし。先行点検・修正終了。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=47 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界1701311004／人物290587722／配置132764322。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPUの通常格納庫・軍事確定・閉鎖は直近の個別検査を参照。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed48。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 01:00回

- 開始 00:58:53 JST、終了 00:59:17 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時の差分は点検記録のみ、ゲーム・検証プロセスなし。先行点検・修正終了。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=48 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界1966990595／人物1622026138／配置418101630。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPUの通常格納庫・軍事確定・閉鎖は直近の個別検査を参照。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed49。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 02:00回

- 開始 02:01:27 JST、終了 02:01:52 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時の差分は点検記録のみ、ゲーム・検証プロセスなし。先行点検・修正終了。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=49 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界85186539／人物805980907／配置703438938。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPUの通常格納庫・軍事確定・閉鎖は直近の個別検査を参照。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed50。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 03:00回

- 開始 03:01:24 JST、終了 03:01:49 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時の差分は点検記録のみ、ゲーム・検証プロセスなし。先行点検・修正終了。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=50 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界350866130／人物2137419323／配置988776246。初回配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UIとCPUの通常格納庫・軍事確定・閉鎖は直近の個別検査を参照。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回は個別操作の描画比較またはSeed51。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 04:00回

- 開始 04:01:57 JST、終了 04:02:22 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検記録のみ。起動中のゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=51 で既存出力の dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界616545721／人物1321374092／配置1274113554。初回配送クリック(256,998)、地図復帰、地星儀／展開図切替、18FPSの端地点移動、終了ループを確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。プライマリー、直送便、左右インスペクター、緑赤チェックとCPU通常UIは直近の個別結果を参照。CPUの個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回はCPU個別操作の描画比較またはSeed52。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 05:00回

- 開始 04:57:25 JST、終了 04:57:48 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検記録のみ。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=52 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界882225312／人物505328861／配置1559450862。初回配送クリック(256,998)、地図復帰、地星儀／展開図切替、18FPSの端地点移動、終了ループを確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。優先UI・緑赤チェック・CPU通常UIは直近の個別検査を参照。CPUの個別選択・ドラッグと緑赤チェック、物理マウスでの赤地点再選択、専用直送便の自然Demoは未確認。次回はCPU個別操作の描画比較またはSeed53。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 06:00回

- 開始 05:59:04 JST、終了 05:59:09 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検記録のみ。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=53 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界1147904903／人物1836767277／配置1844788170。初回配送クリック(256,998)、地図復帰、地星儀／展開図切替、18FPSの端地点移動、拒否時処遇、終了ループ、Demoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。プライマリー、直送便の退避復帰、左右インスペクター、緑赤チェックとCPU通常UIは今回未再検証。CPU個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回はCPU個別操作の描画比較またはSeed54。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 07:00回

- 開始 06:57:30 JST、終了 06:57:36 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検記録のみ。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=54 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。論理1920×1080、世界1413584494／人物1020722046／配置2130125478。初回配送クリック(256,998)、地図復帰、地星儀／展開図切替、18FPSの端地点移動、拒否時処遇、終了ループ、Demoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。
- 新規不具合なし。プライマリー、直送便の退避復帰、左右インスペクター、緑赤チェックとCPU通常UIは今回未再検証。CPUの個別選択・ドラッグと緑赤チェック、赤地点の物理マウス再選択、専用直送便の自然Demoは未確認。次回はCPU個別操作の描画比較から。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 08:00回

- 開始 08:02:32 JST、終了 08:02:39 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検記録のみ。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存分離出力 dotnet artifacts/corner-lesson/CountrySettings.dll --ui を実行。設定入力、手動／自動遷移、軍事の操作表示の既存検査が成功。今回の実ウィンドウ解像度は未採取。
- DEMO-0016の残項目をコード照合。Game1.ComputerMarches.cs の AdvanceComputerWarPreview 段階2は通常の出撃ボタンを開いたあと _invasionDraft.UnionWith(ids) で選択済みにし、DrawComputerMarches はピッカー表示中に確定ボタンを指す。通常の操作で見える個別ロボット選択のクリックはこの経路にない。既報と一致するため重複登録なし。個別選択時の描画比較と専用表示への退避条件の実行再現は未確認。
- プライマリー、直送便の退避復帰、左右インスペクター、Demoの緑赤チェックとCPU内政は今回未再検証。次回は軍事ピッカーの個別選択の描画比較から。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 09:00回

- 開始 09:00:34 JST、終了 09:01:13 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時の差分は点検記録のみ。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存分離出力 artifacts/corner-lesson の DemoActionsUi.dll、DemoInspectorUi.dll、DemoChecksUi.dll を順次 dotnet で実行し、すべて終了コード0。1080p／540p。証拠は artifacts/demo-click-audit の audit-latest-DemoActionsUi.log、audit-latest-DemoInspectorUi.log、audit-latest-DemoChecksUi.log。
- 直送便のプライマリー、工場の実矩形（例 160,288,72,88 にクリック196,332）と製造開始、閉鎖、和平移動、緑赤チェック、出撃確定・即予約・赤チェック・閉鎖・ターン終了の順序とチェック消去を確認。新規不具合なし。
- 専用直送便の自然巡回での退避復帰、左右別の再照合、赤地点の物理マウス再選択、CPUの個別操作の実描画比較は今回未実施。次回はCPU軍事ピッカーの個別選択の比較から。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 10:00回

- 開始 10:04:36 JST、終了 10:04:42 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検記録のみ。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存分離出力の dotnet artifacts/corner-lesson/CountrySettings.dll --ui が成功。通常の出撃ピッカーへ進む軍事経路を既存検査で確認。証拠は実行ログの PASS と tests/CountrySettings/CountrySettingsUi.cs のピッカー確認。実ウィンドウ解像度は未採取。
- 新規不具合なし。個別ロボット選択クリック、専用表示へ退避する軍事経路、CPU内政と優先Demo経路は今回未確認。次回は軍事ピッカーの選択前後をフレームごとに比較する。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 11:00回

- 開始 10:56:35 JST、終了 10:57:26 JST。直送便点検は中断。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時の差分は点検記録のみ。起動中のゲーム・検証プロセスなし。
- 1280×800用の既存 DemoAuditDirect800.dll は依存アセンブリの読み込みで停止。DOTNET_CLI_TELEMETRY_OPTOUT=1 で既存の DemoAuditDirect800.csproj を分離出力へ再ビルドし警告・エラー0。ALSOFT_DRIVERS=null で再実行すると、旧診断の ProbeDirect800.cs.txt 37行目が現行ソースに存在しない DirectMapCloseButton を反射で参照して停止。証拠 artifacts/demo-click-audit/direct800-0100-output.txt。1280×800の描画は未確認。
- ゲームの不具合としては未登録。次回は現行の直送便終了入力を起点に、退避・復帰と左右の実矩形を照合する。緑赤チェックとCPU個別操作の描画比較は今回未実施。ゲーム・テストソース無変更。

### 実行履歴：2026-11-06 12:00回

- 開始 11:56:46 JST、終了 11:56:58 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検記録のみ。ゲーム・検証プロセスなし。
- 既存の非追跡診断 ProbeDirect800.cs.txt を現行の MapModeCloseButton(3) とプライマリー7個に合わせた。DOTNET_CLI_TELEMETRY_OPTOUT=1 で DemoAuditDirect800.csproj を既存分離出力へ再ビルドし警告・エラー0。ALSOFT_DRIVERS=null で DemoAuditDirect800.dll は終了コード0。証拠 artifacts/demo-click-audit/direct800-0100-output.txt。1280×800用の診断で、ログの540p表記は既存メッセージ。
- 直送便でプライマリー退避・復帰、移動中の入力抑止、閉じるボタンとEscape、回転時の左右割り当て、両格納庫と予約維持を確認。新規不具合なし。自然Demo巡回、CPU個別選択の実描画、赤地点の物理マウス再選択は未確認。次回はCPU軍事ピッカーの個別選択を照合。ゲームコード無変更。

### 実行履歴：2026-11-06 13:00回

- 開始 12:58:06 JST、終了 12:58:19 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検記録のみ。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で dotnet artifacts/corner-lesson/CountrySettings.dll --ui を実行し終了コード0。960×540の artifacts/corner-lesson/country-settings-12.png を目視。通常の出撃ピッカーの初回描画ですでに4体が選択済みで、カーソルは「選択を確定」の実ボタン内。期待は通常UIの個別選択操作が見えること。実際は選択済み状態から始まる。既存DEMO-0016の個別選択省略を実描画で再現。重複登録なし。既存テストは確定時の二重予約なしとスキップ後の人間への復帰も確認。
- 次回はDEMO-0016の個別選択に通常UIの矩形と入力を使えるか確認。専用表示に落ちる軍事経路、CPU内政、緑赤チェック、優先Demo経路は今回未再検証。ゲーム・テスト無変更。

- 2026-11-06 13:30修正回の追記。DEMO-0016は検証待ちを継続。個別選択の試行変更は取り消し。再ビルド後の軍事UI検査が出撃ピッカー到達前に失敗したため、詳細は修正記録を参照。

### 実行履歴：2026-11-06 14:00回

- 開始 14:03:12 JST、終了 14:04:17 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検・修正記録のみ。ゲーム・検証プロセスなし。
- 試行変更のない現行ソースから CountrySettings と DemoChecksUi を dotnet build tests/<対象> --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers で順次再ビルド。DOTNET_CLI_TELEMETRY_OPTOUT=1、両方とも警告・エラー0。
- ALSOFT_DRIVERS=null で CountrySettings.dll --ui と DemoChecksUi.dll を実行。前者は軍事操作開始前の征服者切替の検査、後者は軍事プライマリーを開く検査で失敗（ともに終了コード -532462766）。証拠は artifacts/demo-click-audit/audit-latest-CountrySettings.log と audit-latest-DemoChecksUi.log。前回の試行変更を含まない状態でも同じ検査が失敗することを確認。実ウィンドウ解像度は未採取。
- ゲーム側とテスト手順の切り分けが残るため新規IDは付与せず。次回は CountrySettingsUi の軍事開始後の操作順と DemoChecksUi の軍事ボタン入力前の状態を確認。個別出撃選択、緑赤チェック後半、直送便・左右インスペクターは今回未再検証。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 15:00回

- 開始 14:56:42 JST、終了 14:57:58 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は点検・修正記録のみ。ゲーム・検証プロセスなし。
- DEMO-0018（未解決）。緑チェック後にインスペクターを閉じ、現行の内政フェーズのままDemoの開戦段階を開始すると、開戦ボタン中央 (410,1022) を指し、段階31に進むが開戦モードは開かない。期待は地点を選べる開戦モードへの遷移。実際は WarPlanningAllowed が内政を許す一方で OpenWarPlanning が軍事限定で戻る。論理1920×1080、最初の1080p条件。後続の赤チェック・予約・閉鎖は未到達。
- 証拠 artifacts/demo-click-audit/audit-latest-planning-checks.log。DemoChecksUi を元にした非追跡の ProbePlanningChecks.cs.txt で、旧テストの軍事フェーズ強制遷移を外し、内政の開戦段階へ進めて再現。DemoAuditPlanningChecks.csproj を既存分離出力へビルドして警告・エラー0、ALSOFT_DRIVERS=null で DemoAuditPlanningChecks.dll を実行し該当アサーションで停止。自然巡回での到達、優先直送便・左右、CPU個別選択は今回未再検証。次回は開戦モードのフェーズ条件を関連入力と照合。ゲームコードと追跡テスト無変更。

- DEMO-0018は 15:34:48 JST に修正・検証完了。内政で開戦入力が通り、出撃確定から赤チェック・閉鎖・ターン終了まで4条件で検証。範囲とコマンドは修正記録を参照。

### 実行履歴：2026-11-06 16:00回

- 開始 15:59:08 JST、終了 15:59:22 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は DEMO-0018 の Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=55 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。世界1679264085／人物204676815／配置267979139。配送クリック(256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。実ウィンドウ解像度は未採取。
- 新規不具合なし。出撃予約後の配送・戦闘、CPU個別選択、左右インスペクターと直送便の退避復帰は今回未再検証。次回は自然巡回で予約から戦闘までを照合。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 17:00回

- 開始 16:58:09 JST、終了 16:58:44 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時の内容差分は DEMO-0018 の Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- 既存の非追跡 ProbePlanningChecks.cs.txt に、配送終了を呼び出して予約先と同じ地点への進軍を LastMarches で確認する検査を追加。既存分離出力へ DemoAuditPlanningChecks.csproj を再ビルドし警告・エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。証拠 artifacts/demo-click-audit/audit-latest-planning-checks.log。1080p／540p、展開図／地星儀で即予約・赤チェック・閉鎖・ターン終了・配送後の進軍を確認。
- 新規不具合なし。自然巡回での配送演出と戦闘の完走、CPUの個別選択、直送便・左右インスペクターは今回未再検証。次回はCPUの通常出撃ピッカーへの到達を調べる。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-06 18:00回

- 開始 18:01:39 JST、終了 18:02:20 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時の内容差分は DEMO-0018 の Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- CountrySettingsUi を元にした非追跡の DemoAuditCpuSortie.csproj を既存分離出力へビルド（警告・エラー0）。重複した EndPlannedTurn を省いた診断で OpenMilitaryPhase から開始。ALSOFT_DRIVERS=null で DemoAuditCpuSortie.dll を実行。征服者1への切替は確認したが、目標93の通常出撃経路は stage=-1、picker=-1、selected=-1 となり、ピッカー到達の検査で停止（-532462766）。証拠 artifacts/demo-click-audit/audit-latest-cpu-sortie.log。実解像度は未採取。
- 期待は対象の地点を選び通常のピッカーを開くこと。既存DEMO-0016に追記し、重複登録なし。次回は対象93のヒット判定と CanPlanRuler による候補除外を分けて確認。専用表示の実描画、CPU個別選択、優先Demo経路は今回未再検証。ゲームコード・追跡テスト無変更。

- 18:30修正回の追記。DEMO-0016の目標93で通常ピッカーに到達しない経路は修正・実描画検証済み。個別選択と専用表示の残項目は検証待ち。詳細は修正記録を参照。

### 実行履歴：2026-11-06 19:00回

- 開始 19:03:09 JST、終了 19:03:24 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.Invasion.cs の DEMO-0018 と DEMO-0016 修正、および点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存分離出力の dotnet artifacts/corner-lesson/DemoAuditCpuSortie.dll を実行し終了コード0。征服者1の表示切替、地点93選択と通常ピッカー到達、確定時の二重予約なしとスキップ後の人間への復帰を再確認。証拠 artifacts/demo-click-audit/audit-latest-cpu-sortie.log。実ウィンドウ解像度は今回未採取。
- 新規不具合なし。個別ロボット選択クリックの省略はDEMO-0016の残項目。次回はカードの描画・入力矩形とCPUの選択順を比較する。専用表示への退避、CPU内政、優先Demo経路、緑赤チェックは今回未再検証。ゲーム・テスト無変更。

- 19:30修正回の追記。DEMO-0016の通常ピッカーでの個別選択を修正し、960×540の描画と4体の選択を確認。専用表示への退避とCPU側の赤チェックは検証待ち。詳細は修正記録を参照。

### 実行履歴：2026-11-06 20:00回

- 開始 20:01:40 JST、終了 20:01:56 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存分離出力の dotnet artifacts/corner-lesson/DemoAuditCpuSortie.dll を実行し終了コード0。征服者1、地点93、通常ピッカーで個別4クリック、確定時の二重予約なし、スキップ復帰を再確認。証拠 artifacts/demo-click-audit/audit-latest-cpu-sortie.log。実解像度は今回未採取。
- 新規不具合なし。CPUの赤チェック、専用表示への退避、優先Demo経路は今回未再検証。次回はピッカーの対象外となる軍事指示で専用表示と状態遷移を確認する。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 21:00回

- 開始 20:58:12 JST、終了 20:58:39 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存分離出力の DemoAuditDirect800.dll と DemoAuditPlanningChecks.dll を dotnet で順次実行。1280×800用の直送便診断ではプライマリー退避・復帰と閉鎖が成功したが、右から左のドラッグで予約1件を期待する検査が失敗（-532462766、ProbeDirect800.cs.txt 70行目）。その時点の実予約件数は未採取。証拠 artifacts/demo-click-audit/audit-latest-DemoAuditDirect800.log。既存バイナリと共有アセンブリの組み合わせでの結果のため、ゲーム不具合としての登録は現行ソースからの再ビルドと入力照合まで保留。
- 開戦診断は終了コード0。展開図／地星儀、1080p／540pで緑赤チェックと出撃確定・閉鎖・ターン終了を再確認。証拠 artifacts/demo-click-audit/audit-latest-DemoAuditPlanningChecks.log。CPU専用表示と赤チェックは未確認。次回は直送便の右から左のドラッグを現行ビルドで再現し、実矩形と予約の向きを確認。ゲーム・テスト無変更。

### 実行履歴：2026-11-06 22:00回

- 開始 22:00:14 JST、終了 22:01:13 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- 1280×800用 DemoAuditDirect800 を現行コードで再ビルドし、旧予約一覧の検査失敗を再現。非追跡の ProbeDirect800.cs.txt を現行の PartPlanning 参照に変更して再実行。論理座標 (1434,392)→(470,544) の右から左の入力で地点3→0の予約1件、旧一覧0件を確認。プライマリー退避・復帰と閉鎖も成功。証拠 artifacts/demo-click-audit/audit-latest-DemoAuditDirect800.log。
- DOTNET_CLI_TELEMETRY_OPTOUT=1 で dotnet build artifacts/demo-click-audit/DemoAuditDirect800.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers。再診断は対象検査で終了するため到達不能の警告1、エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。前回の失敗は検査が旧一覧を参照していたためと判明し、不具合登録なし。
- ドラッグ中の画像と後続の左から右の検査、CPU専用表示・赤チェック、緑赤Demo経路は今回は未確認。次回は現行予約一覧で左から右の入力と描画を照合。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-06 23:00回

- 開始 23:03:16 JST、終了 23:03:38 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- 1280×800用の既存非追跡 ProbeDirect800 に左から右の予約方向の検査を追加し、DemoAuditDirect800.csproj を既存分離出力へ再ビルド。診断の早期終了による警告1、エラー0。ALSOFT_DRIVERS=null で DemoAuditDirect800.dll は終了コード0。論理座標 (1434,392)→(470,544) で地点3→0、(114,324)→(1442,378) で地点0→3。現行予約一覧に合計2件。プライマリー退避・復帰と閉鎖も成功。証拠 artifacts/demo-click-audit/audit-latest-DemoAuditDirect800.log。
- 新規不具合なし。ドラッグ中の描画、自然Demo巡回、緑赤チェック、CPUの専用表示と赤チェックは今回未再検証。次回は直送便ドラッグ中の描画を照合。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 00:00回

- 開始 23:56:13 JST、終了 23:56:34 JST（いずれも11月6日）。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=56 で既存分離出力 dotnet artifacts/corner-lesson/DemoUi.dll を実行し終了コード0。初回配送クリック (256,998)、地図復帰、地星儀／展開図切替、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠 artifacts/demo-click-audit/audit-latest-DemoUi.log。実ウィンドウ解像度は未採取。
- 新規不具合なし。直送便ドラッグ中の描画はカーソルを基準にパーツを描く実装まで確認し、実画面は未採取。次回はドラッグ中の描画を照合。プライマリー、左右インスペクター、緑赤チェックとCPU経路は今回未再検証。ゲーム・テスト無変更。

### 実行履歴：2026-11-07 01:00回

- 開始 00:57:46 JST、終了 00:58:11 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- 既存の非追跡 ProbeDirect800 に保持中の撮影を追加。DemoAuditDirect800.csproj を既存分離出力へ再ビルドし、早期終了による警告1、エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。退避・復帰と閉鎖、左右両方向の予約を再確認。証拠 artifacts/demo-click-audit/audit-latest-DemoAuditDirect800.log。
- 左格納庫から胴体を保持し、論理ポインター (960,600) に対応する胴体を左右インスペクター間に描くことを direct800-0.png で目視。診断名と異なり、この撮影時の実バックバッファは1920×1080。1280×800での保持中の描画、自然Demo巡回、CPU専用表示と赤チェックは未確認。次回は1280×800で保持中の描画を照合。新規不具合なし。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 02:00回

- 開始 02:02:47 JST、終了 02:03:06 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- 既存の非追跡 ProbeDirect800 を撮影前に1280×800へ切り替えるようにし、DemoAuditDirect800.csproj を既存分離出力へ再ビルド。診断の早期終了による警告1、エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。プライマリー退避・復帰、閉鎖、左右両方向の予約を再確認。
- 実バックバッファ1280×800の direct800-0.png で、論理ポインター (960,600) の位置に保持中の胴体が描かれることを目視。証拠 artifacts/demo-click-audit/audit-latest-DemoAuditDirect800.log と artifacts/corner-lesson/direct800-0.png。新規不具合なし。自然Demo巡回とCPU専用表示・赤チェックは未確認。次回はCPU軍事の専用表示への切り替えを照合。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 03:00回

- 開始 03:03:18 JST、終了 03:04:03 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- 既存の非追跡 DemoAuditCpuSortie を再ビルドし警告・エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。地点93で通常のピッカーと4回の個別選択を再確認。次に表示用データだけを和平移動へ差し替え、960×540の実描画で専用の町名・指示パネルとカーソルを確認。征服者1の表示維持とスキップ後の人間復帰を確認。証拠 audit-latest-cpu-sortie.log と audit-latest-computer-fallback.png。
- DEMO-0016の残項目として和平移動の専用表示を確認。実際の和平指示の生成と実行、赤チェック、対応可能な攻撃が失敗して専用表示に戻る経路は未確認。次回は和平移動を通常の移動UIの入力と照合。優先Demo経路・内政は今回未再検証。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 04:00回

- 開始 03:56:17 JST、終了 03:56:52 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- 既存の非追跡 DemoAuditCpuSortie.csproj を既存分離出力へ再ビルドし警告・エラー0。ALSOFT_DRIVERS=null で DemoAuditCpuSortie.dll は終了コード0。差し替え前のAI指示一覧52件に和平移動3件が含まれることを確認。通常出撃の個別4クリックとスキップ後の復帰を再確認。960×540の専用表示は引き続き差し替え用データでの確認。証拠 artifacts/demo-click-audit/audit-latest-cpu-sortie.log。
- 新規不具合なし。次回は差し替え前の和平指示を表示順に再生し、通常移動の地点判定と照合する。和平指示ごとの実行回数、CPU赤チェック、優先Demo経路は今回未検証。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 05:00回

- 開始 04:57:50 JST、終了 04:58:35 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs と点検・修正記録。ゲーム・検証プロセスなし。
- 既存の非追跡 DemoAuditCpuSortie を分離出力へ再ビルドし警告・エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。実際のAI指示を順に進め、征服者17の地点204への和平移動で通常ピッカーが開かず専用表示となる経路を960×540で採取。期待は通常の地点選択と移動確定が見えること。既存DEMO-0016の再現であり重複登録なし。証拠 artifacts/demo-click-audit/audit-latest-cpu-sortie.log と artifacts/corner-lesson/audit-latest-computer-fallback.png。
- スキップ後の人間復帰を確認。和平移動の実行回数、通常移動UIでの地点判定、CPU赤チェックと優先Demo経路は未確認。次回は征服者17の移動先204で通常の地点選択を照合。ゲームコード・追跡テスト無変更。

- 05:30修正回の追記。DEMO-0016の征服者17の和平移動は通常UIで実描画検証済み。個別の実行回数と残る専用表示は検証待ち。詳細は修正記録を参照。

### 実行履歴：2026-11-07 06:00回

- 開始 05:59:50 JST、終了 06:00:06 JST。部分点検完了。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。開始時差分は Game1.ComputerMarches.cs、Game1.Invasion.cs、Game1.Movement.cs と点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存分離出力 dotnet artifacts/corner-lesson/DemoAuditCpuSortie.dll を実行し終了コード0。地点93の通常ピッカーで4クリック、征服者17の地点204への通常移動選択、確定後の閉鎖と予約件数維持、スキップ後の人間復帰を再確認。960×540。証拠 artifacts/demo-click-audit/audit-latest-cpu-sortie.log。ログの Dedicated peaceful preview は旧文言。
- 新規不具合なし。和平指示の実行回数、CPU赤チェック、優先Demo経路は今回未再検証。次回は配送後の和平移動の結果と通常再生終了時の復帰を照合。ゲーム・テスト無変更。

### 実行履歴：2026-11-07 07:00回

- 開始 07:04:51 JST、終了 07:05:14 JST。部分点検完了。HEAD=6f8118c4。開始時に追跡差分なし。
- マージ時の分離ビルドから ALSOFT_DRIVERS=null で CountrySettings.dll --ui を実行。通常出撃ピッカーの確定後に閉じることと予約件数を検査する CountrySettingsUi.cs 78行目で失敗。実解像度・失敗時の予約件数は未採取。個別選択の追加に対し、既存検査は1ステップで確定を期待しているため、二重予約の有無は未確認。ゲーム不具合の新規登録は保留。
- 次回は現行HEADから再ビルドし、個別選択の各段階と確定前後の件数を照合。和平移動の実行回数、優先Demo経路、緑赤チェックは今回未検証。ゲーム・テスト無変更。

- 07:30修正回で出撃UI検査を個別選択の手順へ更新。選択と確定の全段階で二重予約なしを確認し、07:00回の検査失敗を解消。DEMO-0016のその他の未確認範囲は継続。

### 実行履歴：2026-11-07 08:00回

- 開始 07:56:55 JST、終了 07:57:26 JST。部分点検完了。HEAD=6f8118c4。開始時差分は CountrySettings のテストと点検・修正記録。ゲーム・検証プロセスなし。
- dotnet build tests/DemoUi --source artifacts/nuget-feed を既存の一時出力 artifacts/tmp/delete-after-2026-11-07/merge-check で実行し警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=57 で DemoUi.dll は終了コード0。初回配送クリック (256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠は同出力の audit-demo-seed57.log。実ウィンドウ解像度は未採取。
- 新規不具合なし。左右インスペクター、直送便の退避復帰、緑赤チェック、CPU和平移動の実行回数は今回未再検証。次回はマージ後の優先UIと緑赤チェックの個別診断を再実行する。ゲーム・テスト無変更。

### 実行履歴：2026-11-07 09:00回

- 開始 09:03:53 JST、終了 09:04:44 JST。部分点検完了。HEAD=6f8118c4。開始時差分は CountrySettings テストと点検・修正記録。ゲーム・検証プロセスなし。
- DemoInspectorUi と既存の DemoAuditPlanningChecks を現行コードから artifacts/tmp/delete-after-2026-11-07/merge-check へビルドし、両方とも警告・エラー0。ALSOFT_DRIVERS=null で順次実行。証拠は同出力の audit-DemoInspectorUi.log と audit-DemoAuditPlanningChecks.log。
- 開戦診断は終了コード0。展開図／地星儀、1080p／540pで緑赤チェック、出撃確定・即予約、閉鎖、ターン終了と配送後の進軍を確認。DemoInspectorUi は庁舎を開いて地点IDと担当者IDが一致することを期待する検査（Program.cs 20行目）で停止し、終了コード -532462766。停止時のID・解像度・描画は未採取。ゲーム不具合としての採番は保留。
- 次回は庁舎入力の実矩形、表示中の地点IDと担当者IDを照合して原因を切り分ける。直送便の退避復帰、左右インスペクター、CPU和平移動の実行回数は今回未確認。ゲームコード・テスト無変更。

- 09:30修正回で庁舎検査の初期巡回番号を修正し、実描画を伴う再実行が成功。ゲームの不具合としては登録せず、検査失敗を解消。

### 実行履歴：2026-11-07 10:00回

- 開始 10:01:54 JST、終了 10:02:11 JST。部分点検完了。HEAD=6f8118c4。開始時差分は CountrySettings・DemoInspectorUi のテストと点検・修正記録。ゲーム・検証プロセスなし。
- 既存 DemoAuditDirect800.csproj を一時出力 artifacts/tmp/delete-after-2026-11-07/merge-check へビルド。早期終了による警告1、エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。プライマリー退避・復帰・閉鎖、左右それぞれの予約、1280×800の描画で論理ポインター(960,600)付近のドラッグ中の胴体を確認。証拠は同出力の audit-direct800.log と direct800-0.png。
- 新規不具合なし。自然Demo巡回の直送便、緑赤チェックとCPU和平移動の実行回数は今回未再検証。次回はCPU和平移動の通常再生完了と復帰を照合。ゲーム・テスト無変更。

### 実行履歴：2026-11-07 11:00回

- 開始 11:01:56 JST、終了 11:03:09 JST。部分点検完了。HEAD=6f8118c4。開始時差分は CountrySettings・DemoInspectorUi のテストと点検・修正記録。ゲーム・検証プロセスなし。
- 非追跡の DemoAuditCpuSortie を現行の一時停止メニュー手順に合わせ、スキップなしで再生を進める診断を既存の一時出力へ再ビルド。警告・エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。960×540。征服者17の和平移動先204と終了後の現在地204が一致し、プレイヤー0への復帰を確認。証拠 artifacts/tmp/delete-after-2026-11-07/merge-check/audit-cpu-completion.log。
- 新規不具合なし。個々の指示の実行回数は未計測。CPU赤チェックと直送便の自然Demo巡回は未確認。次回はCPUの赤チェック経路を照合。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 12:00回

- 開始 11:56:57 JST、終了 11:57:12 JST。部分点検完了。HEAD=6f8118c4。開始時差分は CountrySettings・DemoInspectorUi のテストと点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存一時出力 artifacts/tmp/delete-after-2026-11-07/merge-check/CountrySettings.dll --ui を実行し終了コード0。個別選択、確定時の予約件数維持、スキップ復帰を再確認。証拠は同出力の audit-country-latest.log。実解像度は今回未採取。
- CPU赤チェックはコード照合のみ。Game1.ComputerMarches.cs は確定後に直接閉鎖し、NodeCheckClick を呼ばない。Game1.NodeChecks.cs の NodeCheckPlayer も人間の制御者がいないCPUでは -1 となるため、通常のハンドル入力を追加するだけではチェックを保持できない。既存DEMO-0016の未対応範囲として記録。実描画でチェック前後の状態は未確認。
- 次回はCPU出撃確定後のハンドル表示とチェック状態を実描画で照合。優先Demo経路と直送便自然巡回は今回未再検証。ゲーム・テスト無変更。

### 実行履歴：2026-11-07 13:00回

- 開始 13:03:28 JST、終了 13:04:13 JST。部分点検完了。HEAD=6f8118c4。開始時差分は CountrySettings・DemoInspectorUi のテストと点検・修正記録。ゲーム・検証プロセスなし。
- 既存の非追跡 DemoAuditCpuSortie に確定直後の撮影を追加し一時出力へビルド。警告・エラー0、ALSOFT_DRIVERS=null で実行し終了コード0。960×540。征服者1・地点93で個別4体の選択確定後、段階4となりカーソルが閉じるハンドルを指し、赤チェックは灰色のまま。NodeCheckPlayer=-1。期待は赤チェックの実入力を経て閉じること。既存DEMO-0016の未対応経路として実描画で再現、重複登録なし。
- 証拠 artifacts/demo-click-audit/audit-latest-cpu-red.png と artifacts/tmp/delete-after-2026-11-07/merge-check/audit-cpu-red.log。和平移動の通常完了と人間復帰も成功。次回はCPU表示中のチェック所有者と、チェック→閉鎖の段階を修正対象にする。直送便・優先Demo経路は今回未再検証。ゲームコード・追跡テスト無変更。

- 13:30修正回でDEMO-0016のCPU赤チェックを修正。確定後の実ハンドル入力、960×540の赤チェック描画と終了時消去を検証。その他の未確認範囲は修正記録参照。

### 実行履歴：2026-11-07 14:00回

- 開始 13:55:57 JST、終了 13:56:13 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU赤チェックのゲーム変更、CountrySettings・DemoInspectorUi のテストと点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で一時出力 artifacts/tmp/delete-after-2026-11-07/merge-check/CountrySettings.dll --ui を実行し終了コード0。内政で通常格納庫を開いて組み立てる経路と軍事の赤チェック・終了時消去を再確認。実解像度は今回未採取。
- CPU内政の緑チェックはコード上未対応。Game1.ComputerDomestic.cs は組み立て後に次の地点へ切り替え、NodeCheckClick を呼ばない。今回の実描画で緑チェック前後は未採取。既存DEMO-0017の残項目として記録し重複登録なし。
- 次回は組み立て後の格納庫ハンドルと緑チェック状態を実描画で照合。直送便・優先Demo経路は今回未再検証。ゲーム・テスト無変更。

### 実行履歴：2026-11-07 15:00回

- 開始 15:02:59 JST、終了 15:03:13 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU赤チェックのゲーム変更、CountrySettings・DemoInspectorUi テストと点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で一時出力の CountrySettings.dll --ui を実行し終了コード0。960×540の組み立て直後の格納庫描画を目視。組み立て成功メッセージが表示され、手は格納庫タブを指し、ハンドルのチェックは灰色のまま。期待は作業終了後に通常ハンドルから緑チェックを付けること。既存DEMO-0017の未対応範囲を実描画で再現。重複登録なし。
- 証拠 artifacts/demo-click-audit/audit-latest-computer-domestic.png（既存の保存先を再利用）。次回は組み立て後の緑チェックと閉鎖、内政終了時の消去を修正対象にする。直送便自然Demo・優先ボタンの再巡回は未実施。ゲームコード・テスト無変更。

- 15:30修正回でDEMO-0017のCPU緑チェックを修正。960×540で実描画検証済み。配送前消去は状態検査で確認。その他の未確認範囲は修正記録参照。

### 実行履歴：2026-11-07 16:00回

- 開始 15:59:30 JST、終了 16:00:07 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU緑赤チェックのゲーム変更、CountrySettings・DemoInspectorUi のテストと点検・修正記録。ゲーム・検証プロセスなし。
- 非追跡の DemoAuditCpuSortie を内政もスキップせず完了する診断へ更新し、既存の一時出力へビルド。警告・エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。960×540。内政の通常再生終了時に緑チェック一覧が空となり、人間へ復帰して配送へ進むことを確認。軍事の個別選択、赤チェック、和平移動先204への到着と通常再生終了も成功。証拠 artifacts/tmp/delete-after-2026-11-07/merge-check/audit-normal-domestic.log。
- 新規不具合なし。緑チェック消去直後の画像、画面外指示での専用表示への退避、直送便自然Demo巡回は未確認。次回は画面外のCPU内政で通常UIへの復帰を照合。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 17:00回

- 開始 17:01:31 JST、終了 17:01:45 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU緑赤チェックのゲーム変更、CountrySettings・DemoInspectorUi テストと点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=58 で既存一時出力 artifacts/tmp/delete-after-2026-11-07/merge-check/DemoUi.dll を実行し終了コード0。配送クリック (256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠は同出力の audit-demo-seed58.log。実解像度は今回未採取。
- 新規不具合なし。画面外CPU内政での専用表示への退避と復帰は今回実施できず未確認。直送便自然巡回とCPU緑赤チェックは今回未再検証。次回はCPU地点を画面外にした診断で退避・復帰を照合。ゲーム・テスト無変更。

### 実行履歴：2026-11-07 18:00回

- 開始 18:03:35 JST、終了 18:04:45 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU緑赤チェックのゲーム変更、CountrySettings・DemoInspectorUi テストと点検・修正記録。ゲーム・検証プロセスなし。
- 既存CPU診断から非追跡 DemoAuditOffscreenDomestic を作成。地図を Drag(100000,100000) で画面外へ移し、CPU内政が専用表示に戻る段階-1を確認。960×540の audit-offscreen-domestic.png を目視し、町名・指示の専用パネルを確認。スキップで人間へ復帰し配送へ進むことも成功。
- 一時出力へビルドし診断の早期終了警告1、エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。証拠 artifacts/tmp/delete-after-2026-11-07/merge-check/audit-offscreen.log と artifacts/demo-click-audit/audit-offscreen-domestic.png。初回は診断の読み取り専用プロパティ代入でコンパイル失敗し、公開Drag操作へ直して再試行。
- 既存DEMO-0017の専用表示残項目として記録。自然巡回での画面外条件、スキップせず次地点で通常UIへ戻る経路は未確認。次回は次地点での通常UI復帰を照合。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 19:00回

- 開始 19:03:03 JST、終了 19:03:37 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU緑赤チェックのゲーム変更、CountrySettings・DemoInspectorUi テストと点検・修正記録。ゲーム・検証プロセスなし。
- 既存の非追跡 DemoAuditOffscreenDomestic を画面外地点の専用表示から次地点までスキップせず進める診断へ更新。既存一時出力へビルドし、診断の早期終了警告1、エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。960×540。次の征服者3・地点102で通常格納庫に戻り、段階2となることを状態検査で確認。その後スキップで人間へ復帰し配送へ進むことも成功。
- 証拠 artifacts/tmp/delete-after-2026-11-07/merge-check/audit-offscreen-return.log。新規不具合なし。次地点での復帰画像、自然巡回で同じ画面外条件になる経路、直送便自然Demoは未確認。次回は優先Demo経路の別Seed巡回または復帰直後の描画を照合。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 20:00回

- 開始 20:03:05 JST、終了 20:03:41 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU緑赤チェックのゲーム変更、CountrySettings・DemoInspectorUi テストと点検・修正記録。ゲーム・検証プロセスなし。
- 既存非追跡 DemoAuditOffscreenDomestic に復帰後の撮影を追加し一時出力へビルド。早期終了の警告1、エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。960×540の artifacts/demo-click-audit/audit-offscreen-return.png で征服者3・地点102の通常格納庫と格納庫タブを指す手を目視。画面外地点の専用表示後、次地点では地図・地点・陣営色が切り替わって通常UIへ戻る。スキップによる人間復帰も成功。
- 証拠は同画像と artifacts/tmp/delete-after-2026-11-07/merge-check/audit-offscreen-return.log。新規不具合なし。自然巡回で画面外になる条件と直送便自然Demoは未確認。次回は別Seed巡回と優先UIの回帰。ゲームコード・追跡テスト無変更。

### 実行履歴：2026-11-07 21:00回

- 開始 20:57:35 JST、終了 20:57:46 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU緑赤チェックのゲーム変更、CountrySettings・DemoInspectorUi テストと点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=59 で既存一時出力 artifacts/tmp/delete-after-2026-11-07/merge-check/DemoUi.dll を実行し終了コード0。配送クリック (256,998)、地図復帰、地星儀／展開図、18FPSの端地点移動、拒否時処遇、終了ループとDemoラベル描画を確認。証拠は同出力の audit-demo-seed59.log。実解像度は未採取。
- 新規不具合なし。優先ボタンの個別矩形再照合、直送便自然Demo、CPU画面外条件の自然発生、緑赤チェックは今回未再検証。次回は優先UI個別検査の回帰を実施。ゲーム・テスト無変更。

### 実行履歴：2026-11-07 22:00回

- 開始 22:00:06 JST、終了 22:00:36 JST。部分点検完了。HEAD=6f8118c4。開始時差分はCPU緑赤チェックのゲーム変更、CountrySettings・DemoInspectorUi テストと点検・修正記録。ゲーム・検証プロセスなし。
- ALSOFT_DRIVERS=null で既存一時出力 artifacts/tmp/delete-after-2026-11-07/merge-check の DemoInspectorUi.dll と DemoAuditPlanningChecks.dll を実行し、両方とも終了コード0。1080p／540pで工場の現行矩形と製造、庁舎、閉鎖、直送便プライマリー、開戦の選択確定・即予約・赤チェック・閉鎖・ターン終了・配送後の進軍を再確認。証拠は同出力の audit-latest-DemoInspectorUi.log と audit-latest-DemoAuditPlanningChecks.log。
- 新規不具合なし。直送便自然DemoとCPU画面外条件の自然発生、物理マウスによるチェック地点再選択は未確認。次回は未確認経路の診断条件を照合する。ゲーム・テスト無変更。

### 実行履歴：2026-11-08 19:48回

- 開始 19:48:22 JST、終了 19:50:17 JST。部分点検完了。HEAD=c65db2134b43ace0239eb8ab5ed70477a6b324ae。FolderVerse・tests・docs/ai-agent・AGENTS.md の開始時差分なし。ゲーム・検証プロセスなし。全体のGit状態取得はブランド画像のgit-lfsフィルター起動で失敗したため、関係パスへ限定して差分を確認。他の画像は未確認。
- DemoInspectorUi を現行ソースから artifacts/tmp/delete-after-2026-11-08/demo-check へビルドし警告・エラー0。ALSOFT_DRIVERS=null で実行し終了コード0。1080p／540pで庁舎、閉鎖、直送便プライマリー、工場の6計画について現行矩形と製造開始、描画を確認。証拠は同出力の audit-inspector.log。例として矩形(160,288,72,88)内の(196,332)をクリックして製造開始。
- 新規不具合なし。直送便自然Demo、左右ペアの退避復帰、緑赤チェックとCPU経路は今回未再検証。次回は現行HEADで緑赤チェックとCPU通常UIの回帰から継続。期限切れの旧検証出力は今回削除していない。ゲーム・テスト無変更。

### 実行履歴：2026-11-08 19:55回

- 開始 19:55:59 JST、終了 19:56:55 JST。部分点検完了。HEAD=c65db2134b43ace0239eb8ab5ed70477a6b324ae。関係パスの開始時差分は点検記録のみ。ゲーム・検証プロセスなし。
- CountrySettings と DemoChecksUi を現行ソースから artifacts/tmp/delete-after-2026-11-08/demo-check へビルドし、両方とも警告・エラー0。ALSOFT_DRIVERS=null で順次実行。CountrySettings --ui は国59の入力変更の検査（CountrySettingsUi.cs 18行目）で停止し、CPU操作へ未到達。DemoChecksUi は実際にクリックできる開戦対象を見つける検査（Program.cs 29行目）で停止。ともに終了コード -532462766。
- 証拠は同出力の audit-CountrySettings.log と audit-DemoChecksUi.log。停止時の個別状態・解像度・実描画は未採取。現行UIと検査条件の差を切り分けるまでゲーム不具合の新規登録は保留。正常扱いしない。
- 次回は国設定の実矩形と入力後の各設定値、およびDemo開戦段階と対象候補を採取する。赤チェック以降の遷移、CPU操作、直送便自然Demoは今回未確認。ゲームコード・テスト無変更。

### 実行履歴：2026-11-08 21:00回

- 開始 21:00:37 JST、終了 21:01:32 JST。部分点検完了。HEAD=c65db213。関係パスの開始時差分は点検・修正記録のみ。ゲーム・検証プロセスなし。
- DemoChecksUi から非追跡の DemoAuditCurrentChecks 診断を作成し、既存一時出力へビルド。警告・エラー0、ALSOFT_DRIVERS=null で実行。失敗時は段階17、選択地点-1、Phase=Domestic、移動候補4、攻撃ハイライト0。終了コード -532462766。証拠 artifacts/tmp/delete-after-2026-11-08/demo-check/audit-current-checks.log。
- 検査は配送後に OpenMilitaryPhase を直接呼び、その後に攻撃対象が残ることを期待する。現行入口は即時Advanceするため、失敗時には内政へ進んでいた。ゲーム不具合の採番は保留し、次回は内政中の実予約手順を使う診断と分離して照合。停止時の実解像度・描画は未採取。
- 国設定の各設定値、赤チェック後半、CPU操作、直送便自然Demoは今回未確認。ゲームコード・追跡テスト無変更。

- 21:30修正回でDemoChecksUiの手順を現行の内政予約へ合わせ、4条件の実クリック・描画・配送後進軍の検証が成功。19:55回のDemo検査失敗は解消。国設定の失敗は継続。

### 実行履歴：2026-11-08 22:00回

- 開始 22:03:41 JST、終了 22:04:19 JST。部分点検完了。HEAD=c65db213。関係パスの開始時差分は DemoChecksUi テストと点検・修正記録。ゲーム・検証プロセスなし。
- CountrySettingsUi の非追跡診断 DemoAuditCurrentCountry を既存一時出力へビルドし警告・エラー0。ALSOFT_DRIVERS=null で実行し失敗時の値を採取。国59は人間=True、主色6、副色17、模様1。主色と同じ副色6の矩形への入力は受理されず、副色17を維持。現行UIは主色と同じ副色を除外するため、検査の副色6という期待と不一致。終了コード -532462766。ゲーム不具合の新規登録なし。
- 証拠 artifacts/tmp/delete-after-2026-11-08/demo-check/audit-country-values.log。実解像度・失敗時描画は未採取。次回は異なる副色を選ぶ入力と同色除外の検査へ修正し、CPU操作への後続経路を再検証。優先Demo経路・直送便は今回未再検証。ゲームコード・追跡テスト無変更。

- 22:28修正回で国設定検査を異なる副色7の選択と主色6と同じ副色入力の拒否へ修正。CountrySettings --ui は描画とCPU操作の後続検査を含め終了コード0（fix-CountrySettings.log）。19:55回の国設定検査失敗は解消。ゲームコード無変更。自然Demo・全Seed・専用表示への退避など既存の未確認範囲は継続。

### 実行履歴：2026-11-08 22:58回

- 開始 22:58:25 JST、終了 23:00:16 JST。部分点検・中断。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。関係パス（FolderVerse・tests・docs/ai-agent・AGENTS.md）の開始時差分なし、未解決マージなし、ゲーム・検証プロセスなし。前回修正後にHEADが更新されている。
- Game1.DemoDomestic.cs のプライマリー経路を照合し、直送便の退避復帰・左右インスペクターを優先して DirectDeliveryUi を現行ソースから既存出力 artifacts/tmp/delete-after-2026-11-08/demo-check/ へビルド。dotnet build tests/DirectDeliveryUi --source artifacts/nuget-feed -p:OutputPath=<同出力の絶対パス> -v quiet --disable-build-servers は警告・エラー0、約54秒。DOTNET_CLI_TELEMETRY_OPTOUT=1を使用。
- 22:59台に ALSOFT_DRIVERS=null で DirectDeliveryUi.dll を起動、23:00までに終了コード -1073741819。LoadContentのProgram.cs 37行でNullReferenceException。検査が反射取得する DirectMapCloseButton は現行ゲームソースに存在せず、古い検査との不一致。証拠は同出力の audit-direct.log。ゲームのクリック不具合としての新規採番は保留。
- 例外前に退避途中の入力抑止等のアサートを通過したが、Drawへ未到達のため実描画成功とは扱わない。解像度の実描画確認、左右ペア・退避復帰の後半、自然Demo、CPU操作は今回未確認。次回は現行の直送便終了操作と検査の旧ボタン参照を照合し、テスト更新後に左右・縮小表示を再点検。ゲーム・テスト無変更。

- 23:27修正回：旧ボタン参照を共有矩形MapModeCloseButton(3)へ更新し、終了・退避復帰の入力検査は成功。後続の右→左ドラッグ予約検査（DirectDeliveryUi 70行）で停止。状態は検証待ち。証拠fix-direct.log。Draw未到達で実描画未確認。次回はPartPlanningの予約と旧DirectDelivery.Orders期待値の差を切り分ける。

### 実行履歴：2026-11-09 00:02回

- 開始 00:01:56 JST、終了 00:02:30 JST。部分点検完了（静的照合のみ）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分は直送便テストの旧ボタン参照修正と両記録のみ。ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- 前回の右→左ドラッグ停止を現行入力と照合。Game1.DirectDelivery.cs 113行はPartPlanning.Reserveを呼び、RobotPartPlanning.cs 102行はPartPlanning.Ordersへ追加する。DirectDeliveryUi 70行は旧DirectDelivery.Ordersの件数と向きを期待し、後続にも旧予約の取消・実在庫の即時変更を期待する検査が続く。予約先変更だけでなく予約プレビュー・取消手順を含むテスト移行が必要と判断。クリック失敗を示す証拠にはならず、実際のPartPlanning予約値は未採取のため新規DEMO採番なし・検証待ちを継続。
- Game1.Demo.cs／DemoDomestic.csの入口、前回の関係差分を確認。今回はビルド・実行・実描画・解像度確認なし。直送便の左右・退避復帰後半、自然Demo、CPU軍事／内政は未確認。次回修正はドラッグ直後のPartPlanning.OrdersとPreviewResultsを採取し、通常予約UIに合う検査へ更新するところから。旧一時出力は未使用・未削除で、再利用時に保存期限の更新が必要。

- 2026-11-09 00:26修正回でDirectDeliveryUiを現行PartPlanning予約へ移行し、終了コード0。旧参照・右→左ドラッグで停止する検査不一致は解消。予約保持も現行予約を対象に検証。実描画540pの左右ペアを目視、証拠はartifacts/tmp/delete-after-2026-11-09/demo-check/fix-direct.logとdirect-6.png。ゲーム無変更。自然Demo・CPUの残りは未確認として継続。

### 実行履歴：2026-11-09 00:58回

- 開始 00:57:57 JST、終了 00:58:48 JST。部分点検・中断。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDirectDeliveryUiの前回修正と両記録。ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- Game1.DemoDomestic.csの共有プライマリー・閉じる・チェック矩形参照を照合後、残るDemo巡回を優先。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。
- 00:58台にALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12でDemoUi.dllを起動。開始ログあり、世界Seed=992459907/934883396/883376777。最初の配送と地図説明からの復帰は通過。その後Program.cs 91行の固定戦闘シーンへの差替え後に、UpdateSeaPresentation（Game1.PresentationAudio.cs 46行）の_robotScenes[_battleWave]でIndexOutOfRangeException。終了コード -532462766。証拠は同出力audit-demo.log。
- 自然巡回そのものの不具合と断定せず、固定シーン差替え時に波番号を初期化していない検査との関係を次回切り分ける。新規DEMO採番は保留。Drawへ未到達で解像度・描画確認なし、戦闘以降の遷移と自然Demo完走は未確認。CPU通常UIと直送便の再点検も今回未実施。次回は固定シーン導入直前の波番号と配列長を採取する。

- 2026-11-09 01:35修正回：固定シーン差替え時の旧波番号4／旧配列長16を採取。テスト側で波番号0を設定し配列例外を解消、戦闘導入検査は通過。ただし後続の勧誘拒否検査で停止し全体は検証待ち。fix-demo.log参照。次回はDispositionの待ち時間・実UIとPending残存を照合する。描画未到達、ゲーム無変更。

### 実行履歴：2026-11-09 01:55回

- 開始 01:55:28 JST、終了 01:56:00 JST。部分点検完了（静的照合）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUiの波番号初期化、DirectDeliveryUiの予約方式対応、両記録。ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- DemoUiの勧誘拒否検査をGame1.Demo.cs、Disposition.cs、Mercy.csと照合。UpdateDemoは3秒ごとにAutomaticDispositionPointをクリックする。拒否時に選ぶReleaseは現在、即解放ではなくBeginMercyInputによる交渉入力へ進むため、55フレーム（約3.06秒）でPending消去を期待する検査では確認クリックまで到達しない。現行ボタン中心を狙っていることはコード上確認、実描画上の一致は未確認。
- また通常Game1.Updateが進めるUpdateDispositionSceneをDemoUi.Tickは呼ばず、各性格の固定シーン間で残る処遇演出にも注意が必要。次回修正は交渉入力→確定→結果確認と演出時計を含む有限時間の検査へ合わせ、実際の選択・結果を検証する。停止原因はテスト条件との不一致が濃厚だが実行による確認前のため検証待ち継続、新規DEMO採番なし。
- 今回ビルド・実行・実解像度／描画確認なし。直送便優先経路は前回成功範囲を維持、自然Demo後半・CPU軍事／内政は未確認。続き位置はDemoUiの性格別処遇ループ。

- 2026-11-09 02:29修正回でDemoUiの処遇時計・交渉と結果確認の待ち条件を通常UIへ合わせ、Seed12の検査が描画・終了まで成功。00:58回からの固定シーン／処遇検査停止は解消。証拠artifacts/tmp/delete-after-2026-11-09/demo-check/fix-demo.logとdemo.png（1920×1080）。自然Demo全経路・処遇途中の実描画・CPU通常UIの残りは未確認として継続。ゲーム無変更。

### 実行履歴：2026-11-09 02:59回

- 開始 02:59:29 JST、終了 03:00:10 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiの検査修正と両記録、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- 直送便優先経路の前回成功を踏まえCPU通常UIの残りへ巡回。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=nullでCountrySettings.dll --uiを03:00まで実行し終了コード0。証拠audit-cpu.log。
- 表示オンオフ、征服者切替、内政格納庫・組立と緑チェック、配送前消去、人間復帰、軍事通常出撃選択、AI指定ロボットID、個別クリックの増分、確定時の二重予約なし、赤チェックと終了時消去、スキップ復帰を既存テストで確認。960×540 country-settings-12.pngで通常出撃選択ダイアログとカード上のカーソルを目視。新規不具合なし。
- 全Seed・画面外指示時の専用表示への退避／復帰、和平移動の実行回数、自然Demo全経路、各クリック前後すべての実描画は今回未確認。DEMO-0016／0017の残項目は継続。次回は専用表示へ退避する条件と通常UI復帰を優先。直送便とDemoInspectorUiは今回再実行していない。

### 実行履歴：2026-11-09 03:55回

- 開始 03:55:30 JST、終了 03:55:44 JST。部分点検完了（静的照合のみ）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiの既存検査修正と両記録。ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- DEMO-0016／0017の専用表示への退避条件を照合。内政はFocusComputerDomesticで各地点の段階を0へ戻し、OperationPosition・MapViewBounds・実ヒットIDが一致すれば通常インスペクターへ進む。不一致なら段階-1で専用表示を維持する。次地点で再び判定する設計は確認したが、実際の退避→通常UI復帰は未再現。
- 軍事は_computerWarStage>0で通常UI上の実操作先・チェック・閉じるを描き、段階0では町名／指示の専用パネルを描く。既存DEMO-0016の未対応・退避表示として扱い、重複登録なし。新たなクリックずれや状態破損は確認していない。
- 今回ビルド・実行・解像度・実描画確認なし。退避を自然発生させるSeed／画面条件と次地点復帰、和平移動の実行回数、自然Demo全経路は未確認。次回は既存CPU診断で退避の段階・地点IDを採取し、実描画と次地点への遷移を検証する。直送便・プライマリーは00:26回の成功範囲を維持。

### 実行履歴：2026-11-09 05:05回

- 開始 05:05:01 JST、終了 05:05:46 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 既存の非追跡診断ProbeOffscreenDomestic.cs.txtの旧副色期待値だけを前回修正済みCountrySettingsと同じ入力へ合わせて再利用。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditOffscreenDomestic.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers はエラー0、既存の早期return後の到達不能コード警告CS0162が1件。
- ALSOFT_DRIVERS=nullでDemoAuditOffscreenDomestic.dllを実行し終了コード0、audit-offscreen.logに保存。960×540で地図を意図的に100000px退避させ、内政段階-1の専用町名パネルを再現。その後スキップせず次CPU（owner=3、node=102）で段階2の通常格納庫へ復帰し、最後のスキップで人間と配送フェーズへ戻ることを確認。
- artifacts/demo-click-audit/audit-offscreen-domestic.png と audit-offscreen-return.png を再利用して描画を保存・目視。専用パネルと次CPUの通常格納庫・タブ上のカーソルが一致。DEMO-0017の退避後復帰は制御した画面外条件で確認済みへ更新。専用パネル自体は既存未対応として維持、自然発生条件や各実行回数・軍事の退避復帰・自然Demo全経路は未確認。新規不具合なし。次回は軍事側の退避条件と復帰を照合。

### 実行履歴：2026-11-09 06:03回

- 開始 06:03:32 JST、終了 06:03:46 JST。部分点検完了（静的照合）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- DEMO-0016の軍事専用表示を通常UI経路と照合。通常経路の対象は和平移動、またはQueen=false／PocketOnly=false／SourceNode>=0／Fighters>0。対象外は専用パネルを使用。通常経路でも地点位置・実ヒット不一致、出撃元なし、指定ロボットカードが画面下限外などで段階-1へ退避する。正常な次指示への移行時に段階0へ戻すコードを確認。
- 専用パネルはComputerMarchTarget／ComputerMarchOrder矩形の中心に手を動かす表示で、通常の地点・出撃確定を実入力で再演する経路ではない。既存の未対応範囲として保持し、新規採番なし。再生終了時は元のAI指示列をResolveへ渡す設計を確認したが、各指示の一回限り実行と退避→次通常指示の実描画は今回未検証。
- 既存ProbeCpuSortieは通常出撃と和平移動の巡回用で、軍事退避の段階を強制・検査する診断ではない。今回ビルド・実行・解像度確認なし。次回は対象外指示（Queen／PocketOnly）か画面外カードの再現条件を作り、専用表示・次指示復帰・予約件数を採取する。優先プライマリー・直送便の前回確認範囲は維持、全経路確認済みとはしない。

### 実行履歴：2026-11-09 07:05回

- 開始 07:05:03 JST、終了 07:06:09 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 既存非追跡ProbeCpuSortie.cs.txtに環境変数FOLDERVERSE_AUDIT_FALLBACK=1の場合のみ軍事段階-1を注入する診断を追加。旧副色検査は現行CountrySettingsと同じ入力へ更新。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。
- ALSOFT_DRIVERS=nullで同DLLを実行し終了コード0。実AI指示31件（和平1件）、表示征服者9で専用パネルを描画。2秒後に次指示index=1・段階0へ一度だけ進み、続く3更新で段階3の通常出撃選択へ復帰。PlannedOrdersは0→0（追加予約なし）。証拠audit-war-fallback.log、audit-war-fallback.png、audit-war-return.png。
- 960×540の両画像で町名専用パネルと通常の出撃選択・カード上カーソルを目視。DEMO-0016の退避状態から次通常UIへの復帰は制御した状態注入で確認。対象外指示や画面外カードによる自然な退避発生の再現ではなく、予約0件の不変確認だけで全指示の一回限り実行を証明しない。専用表示の未対応範囲は継続、新規不具合なし。次回は自然な退避入口とResolve後の実行結果を照合。優先直送便・プライマリーは前回確認範囲を維持。

### 実行履歴：2026-11-09 08:03回

- 開始 08:03:34 JST、終了 08:03:54 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 前回現行ソースからビルド済みのDemoAuditCpuSortie.dllを同じ出力で再利用。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_FALLBACK=0で実AI指示の通常巡回を実行。終了コード0。証拠artifacts/tmp/delete-after-2026-11-09/demo-check/audit-cpu-complete.log。今回は再ビルドなし。
- 表示指示31件／和平1件。征服者9の通常出撃選択（対象27、個別クリック2回）・赤チェックからスキップせず後続へ進行。征服者57の実際の和平指示（対象98）で通常移動UIの地点選択と確定、追加予約なしを確認。再生完了後の現在地98が指示先98と一致し、人間0と内政フェーズへ復帰。960×540 audit-latest-computer-fallback.pngは名前に反して現在は通常移動UIの画像であり、その内容を目視。
- 今回は退避段階注入なし。ただし各段階の退避ログやResolve呼出し回数を採取しておらず、全指示一回限り実行と自然退避入口は未証明。既存DEMO-0016の未確認範囲として維持、新規不具合なし。次回は自然な対象外指示またはカード下限外による退避入口の記録を追加。優先直送便・プライマリーは前回成功範囲を維持。

### 実行履歴：2026-11-09 09:01回

- 開始 09:01:05 JST、終了 09:01:48 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 既存非追跡ProbeCpuSortieの通常巡回ループへ、段階が負へ変わる退避入口と次指示indexのログだけを追加。DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojを既存artifacts/tmp/delete-after-2026-11-09/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_FALLBACK=0で実行し終了コード0。
- 証拠audit-cpu-natural.log。表示31指示でindex0→30が順次進み、記録範囲内の段階負への自然退避は0件。和平の指示先98と終了後現在地98、人間復帰も成功。これはこの固定世界・表示範囲での結果であり、対象外指示や全Seedでの退避不発を意味しない。
- 今回は新しい画像目視なし（診断は960×540の通常移動画像を再生成）。自然退避入口は再現できず要再確認を継続、新規不具合なし。次回は対象外指示Queen／PocketOnlyを含む別世界の選定か、画面外カードの条件を切り分ける。全指示のResolve実行回数と自然Demo全経路は未確認。優先UIの前回検証範囲は維持。

### 実行履歴：2026-11-09 10:00回

- 開始 10:00:06 JST、終了 10:00:54 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiの既存修正と両記録、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- 優先UIへ巡回し、DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoInspectorUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=nullでDemoInspectorUi.dllを実行し終了コード0。
- 庁舎、アニメーション付き閉鎖、直送便プライマリー、自国インスペクターから移動、人間／CPU人数確認のクリック矩形と実描画を検証。1920×1080／960×540で工場6計画は現行矩形(160,288,72,88)の中心(196,332)入力から製造開始まで成功。証拠同出力audit-inspector.log。今回は画像の追加目視なし。
- 新規不具合なし。左右直送便ペアは00:26回の確認範囲を維持。軍事の自然退避入口・全指示実行回数・自然Demo全経路は未確認で継続。次回は既存CPU診断の指示属性を採取し、対象外指示のある条件を特定する。全経路を正常扱いしない。

### 実行履歴：2026-11-09 11:01回

- 開始 11:01:37 JST、終了 11:02:29 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieへ表示指示の属性集計ログを追加して既存出力へ再ビルド。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_FALLBACK=0で実行し終了コード0。
- 証拠audit-cpu-attributes.log。31表示指示のQueen=1、PocketOnly=1、SourceNode欠落0、非和平のFighters<=0は0。集計は和平を除外していないため、この属性だけで通常UI対象外の非和平指示が存在すると判定できない。自然退避段階ログなし、通常巡回と和平指示先への到着は成功。ゲーム不具合の新規採番なし。
- 今回画像目視なし、自然退避入口の検証は未完了。次回はPeacefulとQueen／PocketOnlyの組合せを指示単位で採取し、通常UI対象外の非和平指示があるか区別する。Resolve一回限り実行、全Seed、自然Demo全経路は未確認。優先UIは前回検証範囲を維持。

### 実行履歴：2026-11-09 12:00回

- 開始 12:00:38 JST、終了 12:01:24 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieにQueen／PocketOnly指示の個別属性ログを追加。DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojを既存artifacts/tmp/delete-after-2026-11-09/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_FALLBACK=0で実行し終了コード0。
- audit-cpu-attributes.logを同名で更新。該当はindex30・ruler57・Peaceful=True・Queen=True・PocketOnly=True・source99・fighters0の1指示のみ。これは和平なので通常移動UIの対象であり、非和平の通常UI対象外指示はこの31件には含まれない。自然退避を再現できなかった条件を特定。出撃選択・赤チェック・和平移動・人間復帰の既存検査も成功。
- 新規不具合なし。今回は画像の追加目視なし（診断の実描画は960×540）。自然退避入口は別世界または画面外カード条件で要再確認、全指示の実行回数・自然Demo全経路は未確認。次回は別世界の指示選定を行い、通常経路対象外の非和平指示を先に確認してから描画検証する。優先UIの成功範囲は前回記録を維持。

### 実行履歴：2026-11-09 12:59回

- 開始 12:59:39 JST、終了 13:00:37 JST。部分点検・中断。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieの世界Seedを環境変数FOLDERVERSE_AUDIT_WORLDで選べるようにし、既定0を維持して別世界1を検査。Cast123／Placement456。DOTNET_CLI_TELEMETRY_OPTOUT=1で既存DemoAuditCpuSortie.csprojを既存期限付きdemo-check出力へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、警告・エラー0。
- ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_FALLBACK=0、FOLDERVERSE_AUDIT_WORLD=1で実行。表示15指示・和平0、Queen／PocketOnly／出撃元欠落／非和平の戦力0はいずれも0。征服者1の対象57、通常出撃選択の個別クリック4回、赤チェックまで成功。記録範囲内の自然退避ログなし。
- 後続の「和平指示まで進む」診断ループが和平0件を扱えず、全指示終了後に配列添字範囲外（ProbeCpuSortie.cs.txt132行）で停止。終了コード -532462766。証拠artifacts/tmp/delete-after-2026-11-09/demo-check/audit-cpu-world1.log。ゲーム例外ではなく診断の固定条件不一致として記録、新規DEMO採番なし。13:00までに実行開始・段階ログあり、起動待ちではない。
- 960×540で途中描画は実行したが今回画像目視なし。正常終了・人間復帰・和平移動は今回未確認。次回は非追跡診断で和平0件でも完了を観測できる境界条件を整え、別Seed選定を継続。ゲームコード修正対象とはしない。

- 2026-11-09 13:27修正回で非追跡CPU診断の和平0件条件を修正。世界1の15指示がスキップなしで完了、人間0と内政への復帰・描画を確認し終了コード0。fix-cpu-world1.log参照。12:59回の診断例外は解消。ゲーム無変更、自然退避と実行回数は未確認として継続。

### 実行履歴：2026-11-09 13:57回

- 開始 13:57:11 JST、終了 13:57:30 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 直前修正済みDemoAuditCpuSortie.dllを既存期限付き出力から再利用し、ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_FALLBACK=0、FOLDERVERSE_AUDIT_WORLD=2で別世界を点検。Cast123／Placement456。再ビルドなし。終了コード0、証拠artifacts/tmp/delete-after-2026-11-09/demo-check/audit-cpu-world2.log。
- 表示15指示／和平0、Queen／PocketOnly／出撃元欠落／非和平の戦力0はいずれも0。征服者9の対象27、通常出撃選択・個別4クリック・赤チェックの検査を経て、スキップなしで再生完了、人間0と内政へ復帰、描画も成功。診断表示は960×540、今回は画像目視なし。
- 新規不具合なし。世界2にも自然退避を狙う対象外指示がなく、通常UI対象外入口と全指示実行回数は未確認。次回は世界3以降の属性選定または制御した画面外カード条件で入口を特定。直送便・プライマリーと自然Demo全経路は今回未再検証、既存の確認／未確認範囲を保持。

### 実行履歴：2026-11-09 15:04回

- 開始 15:04:12 JST、終了 15:04:31 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 現行ビルド済みDemoAuditCpuSortie.dllを既存期限付き出力で再利用。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_FALLBACK=0、FOLDERVERSE_AUDIT_WORLD=3（Cast123／Placement456）で実行し終了コード0。証拠artifacts/tmp/delete-after-2026-11-09/demo-check/audit-cpu-world3.log。
- 征服者3の対象91、通常出撃選択・個別4クリック・赤チェックを検査。後続和平指示の対象93を通常移動UIで選択・確定し、再生完了後の現在地93と人間0への復帰を確認。自然退避の段階ログは今回の巡回範囲ではなし。
- 実描画は960×540、今回画像目視なし。新規不具合なし。自然退避入口・全指示実行回数・自然Demo全経路は未確認。次回はSeed巡回を広げる前に通常UIから退避するカード下限条件の診断を検討する。優先直送便・プライマリーは前回確認範囲を保持。

### 実行履歴：2026-11-09 16:04回

- 開始 16:04:43 JST、終了 16:04:56 JST。部分点検完了（静的照合）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- CPU軍事の画面外カード退避条件を通常出撃選択矩形と照合。InvasionRobotCard(i)は論理(350+i%6*200,320+i/6*170,185,155)、確定ボタン上端795。カード中心Yは0〜5番397、6〜11番567、12〜17番737、18番以降907以上。AdvanceComputerWarPreviewは指定カード中心Y>=795で通常経路を中止するため、退避入口の制御再現には少なくとも19番目（index18）のロボットをAI指示へ含む在庫が必要。
- 通常描画・入力も同じInvasionRobotCardを使うことを確認。今回のコード照合だけでは、カード全体の重なりや通常UI自体の画面外操作を不具合と断定しない。既存DEMO-0016の未確認条件として保持、新規採番なし。
- ビルド・実行・実描画・解像度確認は今回なし。次回は通常の在庫上限とAI出撃元の在庫順を確認し、19体条件が成立する固定場面で段階2→-1と次指示復帰を検証する。全指示実行回数・自然Demo全経路・優先UIの再検証は未実施。

### 実行履歴：2026-11-09 16:55回

- 開始 16:55:14 JST、終了 16:55:27 JST。部分点検完了（静的照合）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- カード下限退避の前提を通常在庫と照合。InvasionRobotsは出撃可能な在庫をID昇順に並べる。RobotStoreはStorageLimit=TotalCapacity-ReservedReturnsで追加を制限し、機能移転のプレビューでは容量を加算する。19体条件は既定の単一格納庫の在庫を単に増やす診断では成立せず、容量を含む合法な集約状態が必要。通常在庫の制約を無視する再現は採用しない。
- 今回ビルド・実行・実描画・解像度確認なし。19番目の指定カードによる段階2→-1は未再現で要再確認。新規採番なし。次回は格納庫容量を増やす通常機能移転の固定場面を用意できるか確認し、難しければ既存未対応のQueen／PocketOnly非和平指示を別条件で再現する。優先UIとCPU通常経路の既存成功範囲は維持、全経路正常とは扱わない。

### 実行履歴：2026-11-09 18:00回

- 開始 18:00:45 JST、終了 18:01:10 JST。部分点検完了（静的照合）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- 19体以上の出撃元を作る前提としてNodeFunctionTransfersの通常予約条件を確認。内政中、隣接する同盟機能地点、予約権限あり、戦闘・帰還予約なし、合計容量60以下が必要。Resolveは容量とロボット在庫をともに移転するため、12枠を2地点分集約した24枠は仕様上可能。直接容量フィールドを書き換える必要はない。
- 現時点では通常予約→配送解決で集約した在庫からAIが19番目以降を指定する固定場面を作成・実行していない。カード下限退避は要再確認を継続し、新規採番なし。今回ビルド・実描画・解像度確認なし。
- 次回はこの通常機能移転を使う診断場面で24枠の出撃元を作り、AI指示IDとカード順を照合して段階2の退避を検証する。優先UI・CPU通常経路は既存確認範囲を維持。全指示実行回数・自然Demo全経路は未確認。

### 実行履歴：2026-11-09 18:56回

- 開始 18:56:16 JST、終了 18:56:56 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- 集約在庫の前提は既存tests/FunctionTransferの通常Reserve→FinishDomestic検査を確認したが、CPU19体固定場面は今回作成していない。未確認のまま保持し、追加指示の緑赤チェック経路へ巡回。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoChecksUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=nullでDemoChecksUi.dllは終了コード0。証拠audit-checks.log。
- 展開図／地星儀、1920×1080／960×540の4条件で実ハンドルの緑チェック、地点選択→出撃選択→即予約→赤チェック→閉じる→ターン終了、緑赤表示分離と消去を検証。描画を実行、今回画像の追加目視なし。新規不具合なし。
- 自然Demo全経路、赤地点の物理再選択、CPU19体の退避入口と全指示実行回数は未確認。優先直送便の前回成功範囲は維持。次回は集約在庫のCPU固定場面を作成するか、通常UI未対応の非和平Queen指示の場面を限定して点検する。

### 実行履歴：2026-11-09 20:04回

- 開始 20:04:47 JST、終了 20:05:33 JST。部分点検完了。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 19体集約の場面は未作成のため、既存DEMO-0016の別の未対応入口である非和平Queenの表示を制御検証。非追跡ProbeCpuSortieの環境変数FOLDERVERSE_AUDIT_FALLBACK=2で表示用の最初の指示だけQueen=trueへ差替え、実Resolve用の元指示列は変更しない。AI自然生成条件の再現ではない。
- DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojを既存期限付きdemo-check出力へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=0、FOLDERVERSE_AUDIT_FALLBACK=2で実行し終了コード0。証拠audit-cpu-queen.log。
- 非和平Queenは通常地点・出撃選択を開かず専用パネルを描画。その後指示index1・段階3の通常出撃選択へ復帰、PlannedOrdersは0→0で追加予約なし。960×540のaudit-war-fallback.pngとaudit-war-return.pngを同名で再生成（今回は目視なし）。既報の未対応範囲として保持、新規採番なし。
- 自然AIで非和平Queenを選ぶ条件、19体カード下限、全指示の実行回数、自然Demo全経路は未確認。今回の結果だけでゲーム修正は行わない。次回はQueen用通常UIで元指示を保持したまま表示可能かを調べるか、集約在庫の制御場面を点検する。

### 実行履歴：2026-11-09 21:04回

- 開始 21:04:48 JST、終了 21:05:57 JST。部分点検完了。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6へ更新。関係パスの開始時差分なし、ゲーム・検証プロセスなし。ゲーム・テスト無変更。前回の未コミット検査修正・記録は現HEADに含まれる状態。
- 最初に既存DirectDeliveryUi.dllを実行し成功したが、旧HEAD出力のため現行検証には採用せず、現行ソースから再ビルド・再実行。DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DirectDeliveryUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=nullでDirectDeliveryUi.dllは終了コード0。
- 現行HEADでプライマリー退避復帰・移動中入力抑止、左右直送便、通常地図／地星儀と回転、540p物理座標入力、ペア連続切替、地図接続アニメーションと現行予約保持が成功。証拠同出力audit-direct.log、direct-0.png〜22.png。今回は追加画像目視なし。新規不具合なし。
- CPU非和平Queen／19体カードの未対応と未確認範囲は継続。現行HEADでのCPU診断・自然Demo全経路は今回未再検証。次回はHEAD更新後のCPU通常UI・緑赤チェックを優先してから未対応条件の点検へ戻る。

### 実行履歴：2026-11-09 22:00回

- 開始 22:00:19 JST、終了 22:00:57 JST。部分点検完了。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時の関係差分は点検記録のみ、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- HEAD更新後のCPU通常UIを優先し、DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=nullでCountrySettings.dll --uiは終了コード0。証拠同出力audit-cpu.log。
- 現行コードで表示オンオフ、征服者切替、通常格納庫・組立、緑チェックと配送前消去、人間復帰、通常出撃選択・指定ロボットの個別クリック・二重予約なし、赤チェック消去、スキップ復帰を検証。960×540を含む実描画を実行、今回は画像の追加目視なし。新規不具合なし。
- 非和平Queen専用表示と19体カード下限の未対応／未確認範囲は継続。現行HEADでの自然Demo全経路・全指示実行回数は未検証。次回は緑赤Demoの4条件を現行HEADで再検証後、合法な固定Queen指示か集約在庫の点検へ戻る。直送便は21:04回の成功範囲を維持。

### 実行履歴：2026-11-09 23:00回

- 開始 23:00:50 JST、終了 23:01:30 JST。部分点検完了。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時関係差分は点検記録のみ、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoChecksUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=nullでDemoChecksUi.dllは終了コード0、証拠同出力audit-checks.log。
- 現行HEADで展開図／地星儀、1920×1080／960×540の4条件が成功。実ハンドルの緑赤クリック、地点選択→出撃選択確定・即予約→赤チェック→閉鎖→ターン終了、緑赤表示分離・消去、配送後進軍まで検証。実描画あり、今回追加画像目視なし。新規不具合なし。
- 直送便は21:04回、CPU通常UIは22:00回の成功範囲を維持。非和平Queenの通常UI未対応、19体カード下限の退避入口、全指示実行回数、自然Demo全経路は未確認。次回は現行HEADの自然Demo検査を優先するか、合法な集約在庫の固定場面へ進む。検証出力は既存先を再利用し増設なし。

### 実行履歴：2026-11-09 23:56回

- 開始 23:56:21 JST、終了 23:56:51 JST。部分点検完了。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時の関係差分は点検記録のみ、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12でDemoUi.dllは終了コード0。証拠同出力audit-demo.log、demo.png。
- 現行HEADでタイトル待機とクリック復帰、人数確認、配送、地星儀／展開図切替・18FPS地図接続、固定戦闘導入、4性格処遇、20分終了経路・タイトル再開を検査。実描画まで成功、今回画像の追加目視なし。世界Seed992459907／Cast934883396／Placement883376777。新規不具合なし。
- 固定戦闘・処遇を注入する検査なので自然Demo全経路完走とは扱わない。非和平Queenの通常UI未対応、集約19体カードの退避入口、全AI指示実行回数は未確認。直送便・緑赤・CPU通常UIは直前3回の現行HEAD検証範囲を維持。次回は日付更新時に一時出力の期限を確認し、未対応条件の固定場面へ戻る。

### 実行履歴：2026-11-10 00:55回

- 開始 00:55:22 JST、終了 00:55:30 JST。部分点検完了（静的照合）。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時の関係差分は点検記録のみ、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- 非和平Queenの通常UI再利用の前提をBuildInvasionSources／InvasionRobotsと照合。Queen行はCanSortieを満たす征服者の現在地を出撃元として構成し、SelectableRobots(...,true)をID順に表示する。通常地上ロボット指示のQueenフラグだけを替える既存制御診断は、この適格性と指定IDの一致を保証しないため、そのまま通常UI対応の修正根拠にはしない。
- 合法なQueen指示でCanSortie・現在地・選択可能な同行ロボットIDを確認してから、通常行・ピッカー・征服者同行の確定を照合する必要がある。今回実行・実描画・解像度確認なし。既存DEMO-0016の未対応を保持、新規採番なし。
- 前日期限のdemo-check出力は今回使用・削除していない。次回ビルド時は期限更新または当日期限の既存出力を使用する。自然Demo全経路・19体カード退避・全指示実行回数は未確認。次回はQueen用合法指示を固定場面として準備するところから継続。

### 実行履歴：2026-11-10 01:58回

- 開始 01:58:52 JST、終了 01:59:15 JST。部分点検完了（静的照合）。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時関係差分は点検記録のみ、ゲーム・検証プロセスなし。ゲーム・テスト無変更。
- 合法なQueen指示の固定場面候補としてtests/InvasionPlanning/Program.csの現在地が敵所有のときの同地点開戦検査を確認。CanSortie=trueかつ和平CanMarch=false、実解決後LastQueenMarchesに同じ出発・到着地点が残る検査が既存にある。通常地上指示のフラグ差替えよりも、同地点Queen開戦の前提を揃える場面として再利用できる。
- 現行BuildInvasionSourcesは征服者用の行を作るが、CPU表示はQueen指示を通常経路の対象外にしている既報状態を維持。今回はこの固定場面のGame1への移植・実行・実描画・解像度確認なし。新規不具合採番なし。
- 次回は同地点敵所有場面で表示オンのCPUに合法なQueen指示を渡し、専用表示と通常行の有無を比較する。前日期限の検証出力は今回未使用・未削除。優先UI・通常CPUの前回確認範囲を維持し、自然Demo全経路・19体カード退避・全指示実行回数は未確認のまま。

### 実行履歴：2026-11-10 02:56回

- 開始 02:56:53 JST、終了 02:58:33 JST。部分点検完了。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時関係差分は点検記録のみ、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieへFOLDERVERSE_AUDIT_LEGAL_QUEEN=1の固定場面を追加。世界0／Cast123／Placement456、CPU1の現在地91を敵所有としCanSortie=trueを検査。同行ロボット0・Queen=true／PocketOnly=trueの同地点指示でShowComputerMarchesを開く。BuildInvasionSourcesに通常Queen行があることと、CPU表示は段階0・ピッカー閉鎖の専用表示のままであることを照合。
- DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojをartifacts/tmp/delete-after-2026-11-10/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、警告・エラー0。初回は表示オン設定漏れで開始検査に停止し、診断をToggleComputerOperationsで補正して再ビルド。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_LEGAL_QUEEN=1で最終実行は終了コード0。証拠同出力audit-legal-queen.log。
- 1920×1080 audit-legal-queen.pngでCPU1視点の専用町名・指示パネルとカーソルを目視。既存DEMO-0016のQueen未対応を合法な固定指示で再現、重複採番なし。期待は通常地点→Queen出撃選択→確定・赤チェック→閉鎖の実演。自然AI生成と指示解決・復帰は今回は未検証。
- 前日期限出力は使用・削除せず当日期限先を使用。次回修正はこの合法固定指示を使い、通常Queen行と同行0の確定・二重予約防止を最小限の対応範囲として検討可能。19体カード下限・自然Demo全経路・全指示実行回数は未確認。

- 2026-11-10 03:33修正回でDEMO-0016の同行0体Queen非和平指示を通常ピッカー→同行トグル→確定→赤チェック→閉鎖へ対応。合法固定場面で追加予約なし、スキップ人間復帰、元指示の同地点実行1件と1080p赤チェック描画を確認。通常CPU診断も回帰成功。fix-legal-queen.log／fix-queen-regression.log参照。同行ありQueen・PocketOnlyのみ・画面外カードの専用表示と未確認範囲は継続し、全体完了とはしない。

### 実行履歴：2026-11-10 04:02回

- 開始 04:02:54 JST、終了 04:03:40 JST。部分点検完了。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時差分は03:33回のGame1.ComputerMarches.csと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieの合法Queen診断にFOLDERVERSE_AUDIT_QUEEN540=1時の960×540設定とスキップせず再生完了を待つ最大20更新を追加。DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojを既存artifacts/tmp/delete-after-2026-11-10/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、警告・エラー0。
- ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_LEGAL_QUEEN=1、FOLDERVERSE_AUDIT_QUEEN540=1で終了コード0。通常Queenピッカー、実同行トグル、確定時追加予約なし、赤チェック、閉鎖、スキップなし終了・人間0復帰、LastQueenMarches同地点指示1件を確認。証拠audit-queen540.log。
- 同名更新したaudit-legal-queen.png（960×540）で通常インスペクターとハンドル／地点の赤チェックを目視。03:33修正の540p・通常完了の未確認範囲をこの固定場面では解消。新規不具合なし。自然AI生成・同行ありQueen／PocketOnlyのみ・19体カード・自然Demo全経路は未確認／未対応を継続。次回は同行ありの合法指示を対象に通常UIとの差を点検。

### 実行履歴：2026-11-10 05:04回

- 開始 05:04:25 JST、終了 05:05:11 JST。部分点検完了。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時差分は同行0体Queen対応のGame1.ComputerMarches.csと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieの合法同地点Queen固定場面に、FOLDERVERSE_AUDIT_QUEEN_ESCORTS=1でCPU1のポケットへ完成ロボット2体を追加し、そのIDを明示した指示を渡す診断を追加。CanSortieとSelectableRobotsに両IDが含まれること、通常Queen行があることを検査。
- DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojを既存artifacts/tmp/delete-after-2026-11-10/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、警告・エラー0。ALSOFT_DRIVERS=null、LEGAL_QUEEN=1／QUEEN_ESCORTS=1／QUEEN540=1の各FOLDERVERSE_AUDIT変数で実行し終了コード0。証拠audit-queen-escorts.log。
- 960×540 audit-queen-escorts.pngを目視。CPU1視点で専用町名・出撃2体パネルのまま、段階0・通常ピッカー閉鎖。期待は実カード2体の個別選択と同行トグル・確定・赤チェックの通常UI実演。既存DEMO-0016の同行ありQueen未対応として追記、重複採番なし。自然AI生成や解決・人間復帰は今回は未検証。
- 次回修正はこの合法な同行2体・明示IDの固定場面を使い、既存Queen行のカードクリックを再利用できるか最小限で検証。PocketOnlyのみ、19体カード、全指示実行回数、自然Demo全経路は未確認／未対応のまま。優先UIと同行0体の前回成功範囲を維持。

### 2026-11-10 05:26修正回からの状態更新

- 終了 05:29:54 JST。DEMO-0016の05:04既報（同行2体Queenの専用表示）は修正・固定場面検証済み。通常Queenトグル→指定2カードの個別クリック→確定（追加予約なし）→赤チェック→閉鎖→人間復帰へ変更。960×540の実描画を目視し、通常終了と1920×1080のスキップ終了で元Queen指示1件を確認。詳細・コマンド・証拠はdemo-click-fixes.mdの05:26回。
- 全体の状態は一部対応・検証待ちを継続。次回は自然AI生成の同行Queen、画面外カードと非Queen PocketOnlyの専用表示差を優先。未確認を正常扱いしない。

### 実行履歴：2026-11-10 05:59回

- 開始 05:59:56 JST、終了 06:01:04 JST。部分点検完了。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。開始時関係差分は同行Queen対応のGame1.ComputerMarches.csと両記録。先行検証・ゲームプロセスなし。ゲーム・テストコード無変更。
- Game1.DemoDomestic.csのプライマリー・閉鎖・チェックの共有矩形参照を静的照合。CPU軍事の残件も照合し、非Queen PocketOnlyは通常再生条件から除外、指定カード中心が確定ボタン上端以降なら専用表示へ退避する現状を確認。既存DEMO-0016の範囲で重複採番なし。今回これら残件の合法固定場面・実描画再現は未実施。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoInspectorUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。起動要求06:00:25頃、ALSOFT_DRIVERS=nullで同出力DemoInspectorUi.dllを実行し06:00:45までに終了コード0。証拠audit-inspector.log、demo-office-0～34.png。同じ既存期限出力先を再利用。
- 事務所の地点同期、アニメーション閉鎖、直送便プライマリー、わが国→移動、カーソル矩形と遷移、6工場計画の1080p／540p検査成功。960×540のdemo-office-34.pngを目視し工場インスペクター実描画を確認。新規不具合なし。
- 直送便の全退避復帰・左右全条件、自然AIの同行Queen、非Queen PocketOnly、19体カード、自然Demo全経路は今回は未確認。直前の成功記録を維持するが今回の確認済み範囲へ加算しない。次回はCPU残件の合法場面または直送便退避復帰の実描画から継続。コミット・プッシュなし。

- 2026-11-10 06:32修正回：DEMO-0016の残件は05:59回で静的照合に留まり、追加修正を裏付ける合法場面の実行再現がないためコード変更なし。一部対応・検証待ちを維持。次回点検は非Queen PocketOnlyの合法場面と実描画の確立から継続。詳細は修正記録06:32回。

### 実行履歴：2026-11-10 06:59回

- 開始06:59:27 JST、終了07:01:22 JST。部分点検完了。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175（前回後にHEAD更新、開始時関係差分なし）。先行検証・ゲームプロセスなし。ゲーム・追跡テストコード無変更。
- DEMO-0016残件の非Queen PocketOnlyを合法固定場面で再現。非追跡ProbeCpuSortie.cs.txtにFOLDERVERSE_AUDIT_POCKET_ONLY=1を追加。世界0／Cast123／Placement456、CPU1の現在地91を敵所有0へ設定、CanSortie=true、完成ポケットロボットID767・768、March(1,91,2,91,false,true,ids)。通常Queen属性のポケット出撃行に両IDがあることをInvasionRobotsで検査。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0、55.12秒）。07:00:50頃にALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_LEGAL_QUEEN／QUEEN_ESCORTS／QUEEN540／POCKET_ONLYすべて1で同出力DemoAuditCpuSortie.dllを起動、終了コード0。証拠audit-pocket-only.logとaudit-pocket-only.png。
- 960×540の実描画を目視。操作するCPU1へ先に切替済みだが、更新後も段階0・ピッカー閉鎖で専用町名／出撃2体パネルとカーソルを表示。期待は通常地点選択→ポケット出撃行→同行オフのまま指定2カード→確定→赤チェック→閉鎖。通常UIに適格行と両個体があるのに専用表示を使う既存DEMO-0016として追記し、重複採番なし。一部対応・検証待ちを維持。
- 今回は指示解決・実戦個体・人間復帰を未検証。次回修正はこの固定場面でQueen同行を追加せずポケット行とカードクリックを再利用できるか確認する。19体カード、自然AI生成、直送便全退避復帰と自然Demo全経路は未確認。優先UIの前回成功を今回実施済みとは扱わない。出力先再利用、コミット・プッシュなし。

- 2026-11-10 07:28修正回（完了07:33:46 JST）：DEMO-0016の06:59既報ポケット単独出撃を修正。通常ポケット行と指定2カード、Queen不参加の確定、赤チェック、閉鎖、人間復帰を再生。540p実描画を目視し、通常終了・1080pスキップで指定ID各1件の戦闘参加とQueen不参加、追加予約なしを検証。詳細とコマンド・証拠は修正記録07:28回。自然AI・画面外カード等は一部対応・検証待ちを継続。次回点検は19体カードの到達不能時の専用表示と次指示復帰、または直送便全退避復帰から継続。

### 実行履歴：2026-11-10 07:59回

- 開始07:59:58 JST、終了08:01:06 JST。部分点検完了。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時関係差分はポケット単独出撃対応Game1.ComputerMarches.csと両記録。先行検証・ゲームプロセスなし。ゲーム・テストコード無変更。
- 優先対象の直送便プライマリー退避復帰と左右インスペクターへ巡回。Game1.DemoDomestic.csのTransportOverviewButton／InspectorClose共有矩形参照を照合し、通常UI側の入力・遷移をDirectDeliveryUiで検査。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DirectDeliveryUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。08:00:35起動要求、ALSOFT_DRIVERS=nullで同出力DirectDeliveryUi.dllを実行し終了コード0。証拠audit-direct.log、direct-0～22.png。既存期限出力先を再利用。
- プライマリー退避中・画面外の入力抑止、閉鎖／Escapeと復帰、左右格納庫のパーツ予約・取り消し、地星儀／展開図での連続トラック選択、540p物理入力と回転継ぎ目接続、予約を保つ退避・復帰が成功。960×540 direct-22.pngで左右インスペクターと中央地図の実描画を目視。新規不具合なし。
- 固定場面・通常入力の検査であり専用直送便Demo自然巡回の完走とは扱わない。CPUの19体画面外カード、自然AI生成、自然Demo全経路は今回未確認。次回は19体カードの専用表示退避と次指示復帰の合法固定場面から継続。直前ポケット修正は今回再実行していない。コミット・プッシュなし。

### 実行履歴：2026-11-10 08:55回

- 開始08:55:29 JST、終了08:57:18 JST。部分点検中断（19体場面の構築条件不一致）。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時差分はポケット単独対応Game1.ComputerMarches.csと両記録、先行作業・ゲームプロセスなし。ゲーム・追跡テスト無変更。
- 19体カード退避を非追跡ProbeCpuSortieへ一時診断として追加し、CPU1のポケットへ20体を入れる場面を試した。DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojを既存artifacts/tmp/delete-after-2026-11-10/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）成功、警告・エラー0。
- ALSOFT_DRIVERS=null／FOLDERVERSE_AUDIT_CARD19=1で起動したが、RobotStore.Addの容量超過で終了コード-532462766。証拠audit-card19.log。RobotStore.Capacity=12のポケットへ19体を入れる設定は不適切であり、画面外カードのゲーム不具合の証拠にはしない。当該一時診断を除去し既存診断ソースへ戻した。除去後の再ビルド・実行は未実施、同出力DLLには試行分岐が残るため次回使用時は必ず再ビルドする。
- 実描画・カーソル・次指示復帰の検査へ未到達。既存DEMO-0016は要再確認範囲を継続、新規採番なし。次回はtests/InvasionPlanningの拡張地上格納庫場面を参考に、合法な地上出撃元と19体の指定IDを確立する。自然AI・自然Demo全経路は未確認。プライマリー／直送便は07:59回の成功範囲を維持するが今回再実行なし。コミット・プッシュなし。

- 2026-11-10 09:29修正回：08:55回で除去した一時診断の現行ソースを再ビルドし、古い試行分岐がDLLに残る制約を解消。ポケット2体の既存診断は終了コード0（fix-probe-refresh.log）。19体範囲は未再現・要再確認のまま、ゲーム追加変更なし。詳細は修正記録09:29回。

### 実行履歴：2026-11-10 09:56回

- 開始09:56:30 JST、終了09:57:44 JST。部分点検完了。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時関係差分はポケット対応Game1.ComputerMarches.csと両記録。先行検証・ゲームプロセスなし。ゲーム・追跡テスト無変更。
- 08:55回の不適切なポケット容量場面に代え、非追跡ProbeCpuSortieのFOLDERVERSE_AUDIT_CARD19=1で地上格納庫を使用。tests/InvasionPlanningと同じSetCapacity(60)による固定拡張格納庫。世界0／Cast123／Placement456、source0をCPU1所有、隣接target3を人間0所有として完成19体を配置。CanAttackFrom=trueを検査。後続はCPU3の合法な現在地Queen0体指示。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。ALSOFT_DRIVERS=null／FOLDERVERSE_AUDIT_CARD19=1で同出力DLLを起動、終了コード0。証拠audit-card19.log、audit-card19.png。出力先・ログ名再利用。
- 960×540実描画でCPU1の専用町名・出撃19体とカーソルを目視。段階-1、通常ピッカー閉鎖、追加予約0を確認。次指示で段階0へリセットしCPU3へ切替、通常ピッカーが開くことまで検証。既存DEMO-0016の画面外カードによる専用表示退避を固定拡張地上場面で再現、重複採番なし。期待は通常UIで指定個体の選択操作を学べることだが、現状は到達不能カードを誤クリックせず専用表示に退避する制約。一部対応・検証待ちを維持。
- 拡張は検証用SetCapacityによるため、自然AIがこの格納庫・指示を生成することは未確認。通常UIで19体をどう表示・選択するか、全指示実行と人間復帰、自然Demo全経路は今回未確認。次回は通常ピッカーの画面外カード到達手段を確認し、UI仕様をDemoのためだけに変えず再利用できるか判断する。プライマリー・直送便は前回成功範囲を維持し今回再実行なし。コミット・プッシュなし。

- 2026-11-10 10:26修正回：19体退避と次CPU3の通常ピッカー復帰を現行診断で再現（fix-card19-review.log、終了コード0）。通常ピッカーにカード用ページ／スクロールがなく、19枚目は確定ボタンより下へ達する。DemoだけのUI変更・直接選択を避け、専用表示の制約を維持。コード変更なし、19体通常操作は要再確認。次回は自然Demo・チェック実演の回帰へ戻り、表示容量制約を全体完了扱いしない。

### 実行履歴：2026-11-10 11:04回

- 開始11:04:31 JST、終了11:05:37 JST。部分点検完了。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時関係差分はポケット対応Game1.ComputerMarches.csと両記録、先行検証・ゲームプロセスなし。ゲーム・テスト無変更。
- 10:26回の引き継ぎに従い、実ハンドルの緑赤チェックと出撃予約のDemo操作へ回帰。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoChecksUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。11:05:05にALSOFT_DRIVERS=nullで同出力DemoChecksUi.dllを起動、終了コード0。証拠audit-checks.logとdemo-checks-0～3-0／6.png、既存出力先再利用。
- 展開図／地星儀×1920×1080／960×540の4条件で実緑赤クリック、地点→出撃選択→確定即予約→赤チェック→閉鎖→ターン終了、緑赤表示分離、チェック後の地点操作・赤スポットライト解除、両メモのターン末消去と予約進軍が成功。demo-checks-3-6.pngを目視し540p地星儀の通常開戦インスペクター・実赤チェック・カーソルを確認。削除済みの別確定ボタンは使用しない。新規不具合なし。
- 固定場面検査であり自然Demo全経路完走ではない。19体カードは既報の通常UI容量制約を継続、CPU全自然指示と専用直送便Demoの自然巡回は今回は未確認。直送便／左右インスペクターは07:59成功範囲を維持し今回再実行なし。次回はDemoUiのSeed12巡回・固定戦闘を含む既存回帰へ継続。コミット・プッシュなし。

### 実行履歴：2026-11-10 12:04回

- 開始12:04:32 JST、終了12:05:34 JST。部分点検完了。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時差分はポケット対応Game1.ComputerMarches.csと両記録。先行検証・ゲームなし。ゲーム・テスト変更なし。
- 引き継ぎどおりDemoUi Seed12巡回を実行。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。12:05:05にALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12で同出力DemoUi.dllを起動、終了コード0。証拠audit-demo.log、demo.png。既存出力先再利用。
- 配送への遷移、地星儀／展開図切替、18FPS接続、固定戦闘への置換と処遇の拒否時代替、エンディング経路、タイトル復帰とDemo再開の既存検査が成功。1920×1080 demo.pngでDemoラベルとタイトル実描画を目視。新規不具合なし。
- 固定戦闘・処遇の注入を含むため自然Demo全経路完走ではない。中間画面すべての実描画、全Seed、CPU自然全指示、19体カード通常操作は未確認／既存制約を継続。優先プライマリー・直送便・左右インスペクターと緑赤チェックは直前の個別成功範囲を維持し、今回再実行済みとは扱わない。次回はDemoInspectorUiの共有矩形・閉鎖待ちと格納庫／組立の巡回から継続。コミット・プッシュなし。

### 実行履歴：2026-11-10 13:01回

- 開始13:01:33 JST、終了13:02:44 JST。部分点検完了。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時関係差分はポケット対応Game1.ComputerMarches.csと両記録。先行検証・ゲームなし。ゲーム・テスト無変更。
- 引き継ぎのDemoInspectorUiを現行コードで検査。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoInspectorUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。13:02:12にALSOFT_DRIVERS=nullで同出力DemoInspectorUi.dll起動、終了コード0。audit-inspector.logとdemo-office-0～34.pngを同名再利用。
- 事務所の地点同期、通常アニメーション閉鎖、直送便プライマリー→わが国→移動、人数入力の実矩形と遷移、6工場計画の1080p／540pでの現描画矩形クリックと計画確定が成功。1920×1080 demo-office-3.pngを目視し格納庫／組み立て入口と実閉鎖ハンドルを確認。新規不具合なし。
- 格納庫を描画できたことだけで全組立操作・CPU自動操縦の個別操作が実演済みとは判定しない。CPU内政組立の既存専用処理制約、19体カード、全自然AI・全自然Demoは未確認／既報を維持。直送便左右・緑赤チェックは前回個別成功範囲を維持し今回再実行なし。次回はCPU内政の通常格納庫後の組み立て経路と既存DEMO-0017残件を照合する。コミット・プッシュなし。

### 実行履歴：2026-11-10 13:56回

- 開始13:56:34 JST、終了13:57:31 JST。部分点検完了。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時関係差分はポケット対応Game1.ComputerMarches.csと両記録。先行検証・ゲームなし。ゲーム・テスト・診断コード無変更。
- 既存DEMO-0017のCPU組立残件を現行診断で再現。13:56:59にALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_CARD19=0／LEGAL_QUEEN=0／FALLBACK=0／WORLD=0で既存出力DemoAuditCpuSortie.dllを起動、終了コード0。証拠audit-cpu-domestic.log、audit-latest-computer-domestic.png（同名再利用）。今回はビルドなし、当日09:56ビルド後のゲームコード変更なし。
- 世界0／Cast123／Placement456、EndDomesticPhaseで選ばれる最初のCPU地点へ頭・胴・脚の個別ロボットを追加し、0.8秒更新3回。地点選択・通常格納庫を開いた後、組立結果が出る一方で手は格納庫タブを指したまま。960×540画像で4体と組立成功メッセージを目視。期待は通常と同じ組立モードとパーツ操作の実演。Game1.ComputerDomestic.csはAssemblySelectionからRobots.Assembleを直接呼び、個別パーツ入力を経由しないことをコード照合。既存DEMO-0017の未対応範囲で重複採番なし。
- 通常側Game1.NodeAssembly.csのHangarAssemblyButton、HangarCell／HangarPartBounds、NodeAssemblyInputはモード切替とパーツドラッグを処理する。DemoDomesticActions側に通常入力を使う組立補助があるため、次回修正はAIが選んだIDを維持しつつ同じ描画矩形・入力を再利用できるか確認する。二重組立と予約重複を防ぐ必要がある。通常CPU診断のオンオフ・征服者切替・緑赤消去・和平到着・人間復帰は成功したが、組立の各実クリック・予約／実在庫の一致は未検証。
- 既存DEMO-0017は一部対応・検証待ちを継続。自然AI全指示・19体カード通常操作・全自然Demoは未確認。優先プライマリー・直送便左右は前回成功範囲を維持し今回再実行なし。コミット・プッシュなし。

- 2026-11-10 14:33修正回：DEMO-0017の通常入力は組替予約、CPU現行は実在庫即組立で確定時点が異なるため、二重予約・全予約の早期確定を避けコード変更なし。既存診断再現は成功（fix-domestic-review.log）。修正未完了・一部対応／検証待ちを維持。次回は表示オフ／オンでAI選択IDと配送確定後の完成個体・在庫を照合する固定場面から継続。詳細は修正記録14:33回。

### 実行履歴：2026-11-10 14:58回

- 開始14:58:35 JST、終了15:01:02 JST。部分点検完了（組立モデル比較のみ）。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時差分はポケット対応Game1.ComputerMarches.csと両記録。先行検証・ゲームなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieへFOLDERVERSE_AUDIT_ASSEMBLY_COMPARE=1の分岐を追加。世界0／Cast123／Placement456、個別のWorldSetup2個を用意し、自動組立オフの地点に頭・胴・脚ID1～3を配置。AIのAssemblySelectionが3IDを選ぶことを確認し、一方はAssemble即時、他方は同じ頭ID1へ胴・脚をPartPlanning.Reserveして予約プレビューと確定を比較。
- DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojを既存artifacts/tmp/delete-after-2026-11-10/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、最終警告・エラー0。初回はRelations.Reset後のLeader欠落で操作権限検査に失敗し、固定場面のLeader=0設定を補正。ALSOFT_DRIVERS=null／FOLDERVERSE_AUDIT_ASSEMBLY_COMPARE=1で最終終了コード0。証拠audit-assembly-compare.log。
- 予約時は実在庫3体のまま2件、プレビューは完成1体。検証用WorldSetup内だけPartPlanning.Resolveを呼ぶと予約0件となり、即組立と同じID1／Complete／owner0／1体になることを確認。利用者のWorldSetupや予約へ一括確定を行っていない。
- これは表示オンオフの実CPU比較や通常UIクリック検証ではない。配送・生産・機能移転を伴う結果、指示IDの役割・品質・損傷、スキップ途中の二重処理、実描画とカーソルは今回未確認。既存DEMO-0017は一部対応・検証待ちのまま。次回はこの最小場面の通常入力とAI選択IDの維持を検査し、既存利用者予約を保持した配送確定結果とスキップ差を確認する。優先UIは前回成功範囲を維持し今回再実行なし。出力先再利用、コミット・プッシュなし。

### 実行履歴：2026-11-10 16:03回

- 開始16:03:36 JST、終了16:04:53 JST。部分点検中断（検査初期条件不一致）。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時関係差分はポケット対応Game1.ComputerMarches.csと両記録。先行検証・ゲームなし。ゲーム・テスト無変更。
- CPU組立の通常入力比較の前提としてtests/DemoActionsUiを実行。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoActionsUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。16:04:11にALSOFT_DRIVERS=nullで同出力DemoActionsUi.dll起動、終了コード-532462766。証拠audit-actions.log。
- 最初のDraw、Program.cs:19の「Opened node is exactly the hit node」で失敗。LoadContentは_demoActive=trueだが_demoMapVisits未設定（初期0）。Game1.DemoDomestic.csは段階0かつ偶数訪問でWarPlanningAllowedなら段階30へ切り替えるため、内政のStage(0)で地点を開く期待と条件が一致しない。DemoInspectorUiでは既に訪問1を明示している。今回はテスト初期条件の不一致として記録し、ゲームクリック位置不具合の新規DEMO採番はしない。期待条件を補正して再検証する必要がある。
- 組立の予約前後・実クリック・配送確定比較へ未到達、画像保存前のため実描画・解像度別結果は未確認。次回修正はDemoActionsUiを内政巡回条件へ合わせ、続くTarget()が実在庫を参照する期待とPlanningStockの差も確認してから組立経路を再評価する。DEMO-0017は未完了を維持。前回成功した優先UI・緑赤・直送便検査を今回成功扱いへ置換しない。コミット・プッシュなし。

- 2026-11-10 16:25修正回：16:03回のDemoActionsUi初期条件不一致は修正・検証済み。内政訪問1、組立予定プレビューを使用し、6組立条件で通常press／drop、確定前実在庫不変、予約各1回、確定後完成1体を確認（fix-actions.log、終了コード0）。540p実描画を目視。ゲーム無変更、DEMO-0017のCPU実演は未完了。詳細と制約は修正記録16:25回。

### 実行履歴：2026-11-10 17:01回

- 開始17:01:37 JST、終了17:03:12 JST。部分点検完了（固定CPU内政のスキップ比較）。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時差分はポケット対応Game1.ComputerMarches.cs、DemoActionsUiと両記録。先行検証・ゲームなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieにFOLDERVERSE_AUDIT_DOMESTIC_SKIP=1を追加。世界0／Cast123／Placement456、CPU1の地点91のみCPU所有として頭胴脚ID1～3を配置、自動組立オフ。操作表示オンでShowComputerDomestic開始後0.8秒更新2回（組立前）から、通常完了とskip=trueを別々の初期化済み場面で比較。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。ALSOFT_DRIVERS=null／FOLDERVERSE_AUDIT_DOMESTIC_SKIP=1で同出力DLLは終了コード0。証拠audit-domestic-skip.log。既存出力先再利用。
- 両条件で地点91の完成個体ID1／Complete／owner1と在庫1体、PartPlanning予約0件、人間0復帰、Deliveryフェーズを確認。組立直前からのスキップはこの固定場面では重複組立を起こさない。今回画像保存・実描画目視なし。正常判定はこの状態比較に限定し、個別ドラッグを見せるCPU実装の対応済みとは扱わない。
- DEMO-0017は一部対応・検証待ち。表示オフとの比較、組立後に押すスキップ、利用者既存予約との共存、通常UI予約を使ったCPU経路と実配送順序は未確認。次回は既存予約を残す小場面でこの比較を拡張し、AI選択IDの入力再利用と二重処理防止の条件を確定する。優先UIと緑赤・直送便の前回成功範囲を維持し今回再実行なし。コミット・プッシュなし。

### 実行履歴：2026-11-10 17:56回

- 開始17:56:38 JST、終了17:57:50 JST。部分点検完了（既存予約共存・組立後スキップ）。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時関係差分はポケット対応Game1.ComputerMarches.cs、DemoActionsUiと両記録。先行作業・ゲームなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieのDOMESTIC_SKIP固定場面を3条件（通常完了、組立前スキップ、組立後スキップ）へ拡張。世界0／Cast123／Placement456、CPU1地点91に頭胴脚ID1～3、自動組立オフ。人間0の別地点にも胴ID4・脚ID5を置き、脚を胴へ結合する既存PartPlanning予約1件を先に作成。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。ALSOFT_DRIVERS=null／FOLDERVERSE_AUDIT_DOMESTIC_SKIP=1で同出力DLLは終了コード0。証拠audit-domestic-skip.log、既存出力先・ログ名再利用。
- CPU地点・格納庫を開いた時点では人間の予約レコードと実在庫2体が保持され、配送へ進むとID4の胴脚1体・予約0件になることを3条件で確認。CPU完成個体も3条件ともID1／Complete／owner1／1体、人間0復帰・Delivery一致。現行CPU直接組立について既存予約と共存し、組立後スキップでも二重処理しない固定場面を確認。
- 今回実描画・個別ドラッグ・表示オフとの比較なし。現行状態の比較であり、通常UI予約を用いるCPU改修の実証ではない。DEMO-0017は一部対応・検証待ちを維持。次回はこの場面を回帰条件として通常UIの組替予約とAI選択IDを同じ結果へ接続する実装可否を検討する。一般の配送・機能移転・品質／損傷・全自然AIとDemoは未確認。優先UI・緑赤・直送便は既存成功範囲を維持し今回再実行なし。コミット・プッシュなし。

### 実行履歴：2026-11-10 18:56回

- 開始18:56:09 JST、終了18:56:44 JST。部分点検完了（本番呼出順の静的照合）。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時関係差分はポケット対応Game1.ComputerMarches.cs、DemoActionsUiと両記録。先行作業・ゲームなし。ゲーム・テスト・診断無変更。
- ConquestCampaign.FinishDomesticの本番順序はComputerLogistics.Plan→ComputerFunctionTransfers.Plan→deferDelivery（ShowComputerDomestic）→BeginDelivery。17:56診断はShowComputerDomesticを直接呼ぶので、このAI計画前段を通らず、表示オフとの比較にそのまま利用できないことを明示。表示オンオフを比較する次の場面ではFinishDomesticの同じ入口・同じautomaticAll設定を使う必要がある。
- Game1.ComputerDomestic.csは表示待ち中2.35秒で実Assembleを呼び、通常入力のPartPlanning予約は通らない既存DEMO-0017を静的再確認。ComputerLogisticsは予約・PendingEditsのある地点を、ComputerFunctionTransfersはPartPlanningのある地点を候補から除外する参照があり、既存予約共存は先行計画を含む本番入口でも確認が必要。今回実行・ビルド・描画は行っていない。
- 新規不具合採番なし。一部対応・検証待ちを維持。次回は既存の予約共存診断をFinishDomestic入口へ合わせ、表示オンオフの完成個体と在庫・配送結果を比較する。前回の単独ShowComputerDomestic診断の成功を本番オンオフ成功へ拡張しない。優先プライマリー・直送便左右と緑赤は既存成功範囲を維持し今回再実行なし。全自然AI／Demoと19体通常操作は未確認を継続。コミット・プッシュなし。

### 実行履歴：2026-11-10 19:59回

- 開始19:59:10 JST、終了20:00:32 JST。部分点検完了。本番入口で表示オンオフによる組立結果差を確認。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時差分はポケット対応Game1.ComputerMarches.cs、DemoActionsUiと両記録。先行作業・ゲームなし。ゲーム・追跡テスト無変更。
- 非追跡ProbeCpuSortieのDOMESTIC_SKIP診断にFOLDERVERSE_AUDIT_DOMESTIC_ENTRY=1を追加。17:56の世界0／Cast123／Placement456、CPU1地点91の頭胴脚3体・自動組立オフ、人間の別地点の組替予約1件を維持。同じConquestCampaign.FinishDomestic(w,false,deferDelivery)で先行AI計画を含め、表示オフ／オン通常／組立前スキップ／組立後スキップを比較。
- DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojを既存artifacts/tmp/delete-after-2026-11-10/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）成功、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP=1／DOMESTIC_ENTRY=1で同出力DLLは終了コード0。証拠audit-domestic-entry.log。末尾PASSはオンのスキップ3条件の一致のみで、オフとの一致を意味しない。
- 実際：表示オフはDelivery時もCPU在庫3体（Head,Body,Legs）。表示オンの3条件はID1／Complete／1体。人間予約はオフでも解決され、オン3条件では既存予約保持・配送確定と人間復帰が成功。期待：操作表示は同じAI判断・指示を見せ、オンオフで実在庫を変えないこと。現行UpdateComputerDomesticが自動組立無効でも直接Assembleする差を確認。既存DEMO-0017の同じ直接組立根因として追記、重複採番なし。状態は未解決（一部UI対応済み）。
- 今回実描画・解像度別検証なし。結果差はモデル状態で確認し、手や個別ドラッグの改善とは別に修正が必要。次回修正は表示オフの本番結果を基準に、再生専用の追加組立を抑制し、自動組立の設定・AI判断を保つ最小条件を検討する。通常入力実演は引き続き未完了。自然AI全条件・生産／移転併用・19体通常操作は未確認。優先UIは前回成功範囲を維持し今回再実行なし。コミット・プッシュなし。

- 2026-11-10 20:28修正回：DEMO-0017の19:59既報「自動組立オフでも操作表示オンなら組立する」状態差は修正・検証済み。直接Assemble時にAutoAssemblyDisabledを尊重し、設定オン／オフ各々で表示オフ・オン通常・組立前後スキップの最終ID／構成／所有者が一致。既存予約の保持・配送1回確定と人間復帰、540pの3パーツ維持・実緑チェック描画を確認。詳細・コマンド・証拠は修正記録20:28回。個別パーツ実演は未対応のため全体は一部対応・検証待ちを継続。

### 実行履歴：2026-11-10 20:55回

- 開始20:55:41 JST、終了20:57:25 JST。部分点検完了。HEAD=90c02432296794b57a883060e75bb0f5afe93eb3へ更新、開始時関係差分なし。先行検証・ゲームなし。利用者のマージ済み変更を保護し、ゲーム・テスト無変更。前回AutoAssemblyDisabledガードが現行コードにもあることを確認。
- HEAD更新にRobotDirectDelivery／ComputerLogistics等の変更が含まれるため、優先のプライマリー退避復帰と直送便左右検査を再実行。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DirectDeliveryUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0、42.93秒）。20:56:55にALSOFT_DRIVERS=nullで同出力DirectDeliveryUi.dll起動、終了コード0。audit-direct.log、direct-0～22.pngを再利用。
- 退避中／画面外入力抑止、閉鎖／Escapeと復帰、左右予約・取消、地星儀／展開図の連続トラック入力、540p回転継ぎ目の物理クリックと予約保持が成功。960×540 direct-22.pngの左右格納庫・中央地図を目視。新規不具合なし。
- CPU本番入口オンオフ比較は新HEADで未実施。先行結果を新HEAD確認済みへ転用せず、次回は同診断を再ビルドしてAutoAssemblyDisabledオンオフ・表示オンオフ・既存予約保持とスキップを確認する。DEMO-0017の個別組立実演と19体通常操作、全自然Demoは未確認／既存制約を継続。コミット・プッシュなし。

### 実行履歴：2026-11-10 21:58回

- 開始21:58:42 JST、終了21:59:58 JST。部分点検完了。HEAD=90c02432296794b57a883060e75bb0f5afe93eb3。開始時関係差分は20:55点検記録のみ、先行作業・ゲームなし。ゲーム・テスト・診断無変更。
- 新HEADでCPU本番入口比較を再ビルド。DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP=1／DOMESTIC_ENTRY=1でAUTO_ENABLED=0と1を別実行し、両方終了コード0（audit-auto-disabled.log、audit-auto-enabled.log）。既存出力先再利用。
- 同じFinishDomestic入口で、自動組立無効は操作表示オフ／オン通常／組立前後スキップとも頭胴脚ID1～3、自動組立有効はID1／Complete／owner1／1体。人間の既存予約保持と配送時1回確定、人間0復帰・Deliveryを確認。960×540 audit-latest-computer-domestic.pngで無効時3パーツと実ハンドル／地点の緑チェック、カーソルを目視。20:28修正の範囲はこのHEADの固定場面でも回帰成功、新規不具合なし。
- 個別パーツ実演は既報DEMO-0017の未対応、配送・移転・品質／損傷全条件と自然AI／自然Demoは未確認。19体通常操作の制約を継続。プライマリー・直送便左右は直前20:55の新HEAD成功範囲を維持し今回再実行なし。次回は通常CPU診断を新HEADで回し、征服者切替・格納庫からの緑／軍事赤チェックと人間復帰を照合する。コミット・プッシュなし。

### 実行履歴：2026-11-10 22:59回

- 開始22:59:43 JST、終了23:00:39 JST。部分点検完了。HEAD=90c02432296794b57a883060e75bb0f5afe93eb3。開始時関係差分は点検記録のみ、先行作業・ゲームなし。ゲーム・テスト・診断無変更。
- 引き継ぎどおり現HEADで通常CPU診断を実行。21:58回で現HEADをビルド済み、以後ゲーム差分なしのため再ビルドなし。23:00:05にALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP／DOMESTIC_ENTRY／ASSEMBLY_COMPARE／CARD19／LEGAL_QUEEN／FALLBACK=0、WORLD=0で既存出力DemoAuditCpuSortie.dllを起動、終了コード0。証拠audit-cpu-current.logと既存audit-latest-computer-*.png。
- 操作表示設定の実入力、征服者切替、通常格納庫、緑チェック・消去、通常出撃ピッカー個別カード・確定・赤チェック・閉鎖、和平移動の到着先98と人間0復帰の状態検査が成功。新規の状態遷移不具合なし。
- 960×540 audit-latest-computer-confirm.pngを目視。通常ピッカーと個別カード上の手は確認できるが、中央の内政フェーズ演出がピッカーに重なり全ボタンを視認できない。診断はGameTimeを固定してbase.Drawへ渡す箇所があるため、実ゲームの演出待ち不具合か診断時計の問題かは未切分け。状態検査成功だけで全実描画正常とは扱わず、次回は演出の開始時計とDraw時刻をそろえて同画面を再採取する。未確定のため新規DEMO採番なし。
- DEMO-0017個別組立実演・19体通常操作・全自然AI／Demoは未確認／既存制約を継続。プライマリーと直送便左右は20:55の現HEAD成功範囲を維持し今回再実行なし。コミット・プッシュなし。
