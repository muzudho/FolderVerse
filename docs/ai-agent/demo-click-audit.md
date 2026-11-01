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
