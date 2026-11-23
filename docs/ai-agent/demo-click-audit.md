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

- 状態：一部対応済み（同一ページの複数組立と別ページ組立を固定ケースで検証済み。残る条件は22:11修正記録参照）。2026-11-05 02:04 JST開始の点検。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。DEMO-0016の既存差分あり。960×540、論理1920×1080。
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

### 実行履歴：2026-11-11 20:30回

- 開始20:30:01 JST、終了20:32:58 JST。部分点検完了。HEAD=6f02353570f4d67e3179faf97b02ac26f083febc。開始時関係差分なし、ゲーム・検証プロセスなし。90c02432以降のFolderVerseとDirectDeliveryUiの差分なし。ゲーム・追跡テスト無変更。
- 既存の再生成可能な検証出力は使用状況と絶対パスを確認し、期限を2026-11-11へ延長して同じdemo-checkを再利用。大容量出力の追加や資料移行はなし。
- 既存DirectDeliveryUi.dllの標準起動はMicrosoft.NETCore.App 9.0がなく終了コード-2147450730。ALSOFT_DRIVERS=null、DOTNET_ROLL_FORWARD=Majorで.NET 10に繰り上げた実行は終了コード0。退避復帰・左右格納庫・予約保持・540p物理入力を確認し、direct-22.pngで960×540の描画を目視。証拠audit-direct.log。対象コード同一の既存ビルドでの回帰。
- 軍事ピッカーの演出重なりは、非追跡ProbeCpuSortie.cs.txtで固定100秒と実時刻の混在を解消し、軍事入口の初回描画から2秒進めて採取する変更を準備。dotnet build DemoAuditCpuSortie.csproj（--source artifacts/nuget-feed、既存出力先）はMicrosoft.NETCore.App.Ref／WindowsDesktop.App.Ref不足でNU1101。SDK 10.0.302のMSBuildで一時的にTargetFramework=net10.0を指定した代替も参照プロジェクトのnet9.0資産でNETSDK1005。追跡設定の変更なし。診断変更の再実行・描画は未確認。
- 次回は.NET 9参照パックが使える環境で診断を再ビルドし、出撃選択画面を再採取する。演出重なりは未切分けでゲーム不具合として未採番。DEMO-0017個別組立実演、19体通常操作、自然Demo全経路は既存未確認のまま。

- 2026-11-11 20:34修正回（完了20:35:14 JST）。診断時計をそろえた再採取で、960×540のCPU軍事ピッカーのカード、手、取消・確定ボタンを視認できた。診断画像の重なりは対応済み。通常速度での演出との重なりは要再確認。詳細は修正記録20:34回、証拠fix-cpu-clock.log。ゲームコード変更なし。

### 実行履歴：2026-11-11 20:59回

- 開始20:59:02 JST、終了20:59:44 JST。部分点検完了。HEAD=6f02353570f4d67e3179faf97b02ac26f083febc。開始時関係差分は両記録。ゲーム・検証プロセスの競合なし。ゲーム・テスト・診断コードの変更なし。
- 既存出力artifacts/tmp/delete-after-2026-11-11/demo-checkのDemoChecksUi.dllを、ALSOFT_DRIVERS=null、DOTNET_ROLL_FORWARD=Majorで.NET 10上で実行。終了コード0。90c02432からゲームと当該検査の差分なし。証拠audit-checks.log。正規.NET 9の再ビルドは未実施。
- 展開図／地星儀×1080p／540pの4条件で、実ハンドルの緑赤、地点選択→出撃選択→確定即予約→赤チェック→閉鎖→ターン終了、表示分離、赤地点の再選択・スポットライト解除、両チェック消去を検査。960×540 demo-checks-3-6.pngで手が赤ハンドル上にあり、地図の赤チェックと予約1件が見えることを目視。新規不具合なし。
- 直送便と左右インスペクターは同日20:30の確認範囲を維持。通常速度のCPU軍事フェーズ演出との重なり、DEMO-0017個別組立、19体カードと全自然Demoは未確認。次回は通常速度のCPU軍事入口で演出と最初のクリックの時刻を照合する。

### DEMO-0018：CPU軍事の最初の操作がフェーズ演出中に始まる

- 2026-11-11 22:01:03〜22:03:29 JST。部分点検完了。HEAD=6f02353570f4d67e3179faf97b02ac26f083febc。関係差分は両記録、ゲーム・検証プロセスの競合なし。ゲームと追跡テストは変更なし。
- 再現手順。ProbeCpuSortie.cs.txtのWORLD=0・PHASE_TIMING=1でCPU操作表示を有効にし、配送画面を描画してOpenMilitaryPhaseへ進む。入口を描画してから実装のUpdateComputerMarchesを1/60秒刻みで進め、段階が変わった時点を採取する。既存FolderVerse.dllと.NET 10参照を使用して診断のみRoslynで再コンパイル。終了コード0、CS1701が3件。dotnet exec --fx-version 10.0.10と既存runtimeconfig／depsfileを使用し、最終実行は終了コード0。Major指定では復旧した.NET 9が選ばれ、診断のSystem.Runtime 10.0参照を読み込めなかった。
- 期待はフェーズ演出が消え、地図を見渡せる状態から最初の操作を見せること。実際は演出開始1.000秒で段階1となり、開戦プライマリーを内部座標(410,1022)で操作済み。960×540のaudit-cpu-phase-entry.pngではカーソルを確認できるが、1.8秒の演出が中央地図を覆う。2.000秒の段階2で地点27を選択できた。証拠は同じ検証出力内のaudit-cpu-phase-entry.logと画像。以前の固定時刻でのピッカー画像の重なりと異なり、今回は最初の開戦操作と演出の同時進行を確認。状態は未解決。
- 次の修正回は通常更新でCPU操作の開始を演出終了まで待てるかを検討し、スキップ・予約・人間復帰を検証する。固定入力と60Hz相当の診断であり、通常プレイの全条件、ピッカー以降の演出、個別組立実演、19体カードは今回未確認。直送便と緑赤の確認範囲は20:30・20:59回を参照。

- 2026-11-11 修正回（完了 22:31:21 JST）。DEMO-0018は修正済み。通常入力の60Hz相当診断で、最初の開戦操作は演出開始1.017秒から2.817秒へ移り、3.833秒で予定地点27を選択。960×540の描画で演出が消えた地図と手カーソルを目視。演出中0.250秒でEscを押すと人間に戻り、内政へ進行した。次回は別シードの軍事入口と未確認の個別組立実演・19体カードを確認する。

### 実行履歴：2026-11-11 22:55回

- 開始22:55:33 JST、終了22:57:12 JST。部分点検完了。HEAD=6f02353570f4d67e3179faf97b02ac26f083febc。関係差分はDEMO-0018修正のFolderVerse/Game1.cs、FolderVerse/Game1.ComputerMarches.csと両記録。開始時ゲーム・検証プロセスの競合なし。ゲーム・追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csを起点に、直送便のTransportOverviewButtonとInspectorCloseの共有矩形を確認。既存のdemo-checkで修正回のゲームDLLと診断を使用。ALSOFT_DRIVERS=nullでdotnet DirectDeliveryUi.dll（22:55:54〜22:56:03）、DemoInspectorUi.dll（22:56:03〜22:56:14）、DemoUi.dll（22:56:14〜22:56:22）を順に起動し、終了コードはすべて0。ログはaudit-late-各DLL名.log。
- 直送便プライマリーの退避復帰、動作中の入力抑止、左右格納庫・予約保持、540pの物理入力を検査。960×540のdirect-22.pngで左右インスペクターを目視。DemoInspectorUiは輸送プライマリー、事務所の開閉、工場6計画のカーソル座標と遷移を1080p／540pで検査。DemoUiは地図切替・18 FPS相当の縁移動、拒否時分岐とエンディングからの周回を検査。
- FOLDERVERSE_AUDIT_WORLD=1／PHASE_TIMING=1でDemoAuditCpuSortie.dll（22:56:22〜22:56:29、終了0）。60Hz相当の通常入力で演出開始2.817秒に開戦プライマリー内部座標(410,1022)、3.833秒に地点57を(1311,552)で選択。960×540のaudit-cpu-phase-entry.pngで演出の消えた地図と手を目視。audit-cpu-phase-seed1.log。DEMO-0018の修正済みを維持。
- WORLD=0／CARD19=1の固定拡張地上格納庫場面（22:56:40〜22:56:45、終了0）では、960×540で19体の専用表示に移り、追加予約なしで次の征服者3の通常ピッカーへ進む既知の制約を再確認。audit-card19.log／audit-card19.png。DEMO-0017の個別組立は静的確認のみ。Game1.ComputerDomestic.csの直接Assembleが残り、個別ドラッグ実演は引き続き未対応。新規採番なし。
- 次回は緑赤チェックの表示モード・ターン終了時の消去と、別配置の通常軍事ピッカーから続ける。今回、緑赤の一連の操作、自然Demo全周、19体の通常カード操作、CPU組立の個別ドラッグは未確認。既存出力先を再利用。

- 2026-11-11 23:28修正回（完了 23:31:54 JST）。DEMO-0016の19体で専用表示に退避する残件は修正・検証済み。960×540の拡張地上格納庫場面で通常ピッカーの2ページを使い、予定の19体を選択して閉じた。追加予約なし、次の征服者のピッカーは1ページ目、スキップで人間へ復帰。人間側の通常入力でも次・前ページ、19体目の選択、選択保持と取消を確認。別の専用表示分岐、自然AIでの大規模出撃、DEMO-0017個別組立実演は継続。

### 実行履歴：2026-11-11 23:56回

- 開始23:56:04 JST、終了23:57:54 JST。部分点検完了。HEAD=6f02353570f4d67e3179faf97b02ac26f083febc。開始時の関係差分はGame1.cs、Game1.ComputerMarches.cs、Game1.InvasionPicker.csと両記録。競合するゲーム・検証プロセスなし。ゲームと追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoUi／DemoInspectorUiを起点に輸送プライマリーと閉鎖の共有矩形を照合。既存出力でALSOFT_DRIVERS=nullの dotnet DirectDeliveryUi.dll（23:56:54〜23:57:04）、DemoInspectorUi.dll（23:57:04〜23:57:16）、DemoChecksUi.dll（23:57:16〜23:57:25）はすべて終了0。輸送プライマリー、退避復帰、左右格納庫と予約保持、工場6計画、展開図／地星儀の1080p／540pで緑赤の分離、赤地点の再選択とスポットライト解除、確定即予約→赤→閉鎖→ターン終了時消去を検査。960×540のdemo-checks-3-6.pngで赤ハンドル上の手、赤チェック、予約1件を目視。
- 非追跡ProbeCpuSortie.cs.txtの拡張格納庫診断に37体条件を追加。dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --no-restore（既存demo-check出力）は警告・エラー0。WORLD=0／CARD19=1／CARD37=1で dotnet DemoAuditCpuSortie.dll（23:57:25〜23:57:30、終了0）。960×540の通常ピッカーでCPUが3ページを送り指定37体を選び、確定で重複予約なし、次の征服者で通常ピッカーを1ページ目から開き、スキップで人間に復帰。audit-card37.pngで3/3、選択37体とカード上の手を目視。各実行の証拠は既存出力内のaudit-2356-各DLL名.log。
- DEMO-0016のページ切替は固定拡張場面で確認済み。自然AIの大規模出撃、他の専用表示分岐、DEMO-0017の個別組立実演と自然Demo全周は未確認または既報継続。新規採番なし。次回はCPU内政の通常格納庫と個別組立の照合、または自然Demo巡回から続ける。

- 2026-11-12 00:34修正回（完了 00:39:28 JST）。DEMO-0016の通常軍事ルートで最初のクリック前にも専用町名リストが出る挙動を現HEADで再現・修正。960×540で征服者切替後、通常地図と開戦プライマリー上の手を目視。地点27への遷移と演出中Escでの人間復帰を確認。専用表示への退避分岐とDEMO-0017の個別組立実演は継続。詳細は修正記録00:34回。

### 実行履歴：2026-11-12 01:03回

- 開始01:03:05 JST、終了01:05:00 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。開始時関係差分は00:34回のGame1.ComputerMarches.csと両記録。競合するゲーム・検証プロセスなし。ゲームと追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoUi／DemoInspectorUiの輸送プライマリー・閉鎖の共有矩形を照合。ALSOFT_DRIVERS=null、既存demo-checkの現行ゲームDLLでDemoInspectorUi.dll（01:03:31〜01:03:45）、DemoUi.dll（01:03:45〜01:03:53）は終了0。輸送、事務所・工場の位置と遷移、地図切替、低FPS相当の縁移動、拒否分岐、エンディング周回を確認。ログaudit-0103-各DLL名.log。
- DirectDeliveryUi.dll（01:03:21〜01:03:31）は退避復帰・入力抑止を通過し、Program.cs:79の区画ドラッグで終了-532462766。テストが使う旧余白(16,145)は現行DirectPartの脚矩形（セル相対X=6〜95、Y=128〜185）内。最新UIの入力が脚を選ぶのは矩形どおり。既存出力先に検証用コピーのみ作成し、余白を(110,145)、空区画を現ページの5へ更新。Roslynで.NET 9参照を使いコンパイル（CS1701が1件）、dotnet execで現行診断depsfileを用いて実行（01:04:18〜01:04:28、終了0）。左右格納庫、全体と個別のドラッグ、予約保持、540p物理入力を検査し、960×540のdirect-22.pngで現レイアウトを目視。証拠audit-0103-DirectDeliveryUi.log／audit-0103-DirectDeliveryUi-adjusted.log。追跡テストの旧座標は要更新、ゲーム不具合の新規DEMO採番なし。
- CPU内政はDOMESTIC_ENTRY=1／DOMESTIC_SKIP=1のDemoAuditCpuSortie.dllでAUTO_ENABLED=0（01:03:53〜01:03:57）と1（01:03:57〜01:04:01）、両方終了0。表示オフ・通常・組立前後スキップで各設定の最終個体IDと構成が一致し、人間の既存予約を配送で一度だけ実行、人間へ復帰。960×540で組立無効時の通常格納庫、3パーツと緑チェックを目視（audit-latest-computer-domestic.png、audit-0103-auto-0／1.log）。DEMO-0017の個別ドラッグ実演は既報継続。
- 次回は直送便の追跡テストの余白座標とページ選択を確認し、緑赤・別配置の通常軍事入口を巡回する。今回、軍事の専用表示、緑赤の全手順、CPU個別組立実演と自然Demo全周は未確認。

- 2026-11-12 01:25修正回（完了 01:27:28 JST）。01:03回で確認した直送便テストの旧座標を修正・検証済み。追跡テストは現行の区画・パーツ矩形から余白を求め、表示ページの空き区画を選ぶ。実行は終了0、960×540で左右格納庫を目視。ゲーム側のDEMO採番なし。詳細は修正記録01:25回。

### 実行履歴：2026-11-12 02:03回

- 開始02:03:36 JST、終了02:04:53 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。関係差分はGame1.ComputerMarches.cs、tests/DirectDeliveryUi/Program.csと両記録。競合するゲーム・検証プロセスなし。ゲーム・テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csの輸送プライマリーと閉鎖の共有矩形をDemoInspectorUiと照合。既存demo-checkでALSOFT_DRIVERS=null、dotnet DirectDeliveryUi.dll（02:03:52〜02:04:00）、DemoInspectorUi.dll（02:04:00〜02:04:13）、DemoChecksUi.dll（02:04:13〜02:04:22）はすべて終了0。直送便の退避復帰・左右格納庫・区画ドラッグ・予約保持、輸送と工場のクリック位置、緑赤分離、赤地点の再選択・スポットライト解除とターン末消去を検査。960×540のdemo-checks-3-6.pngで赤ハンドル上の手、赤チェック、予約1件を目視。
- WORLD=1／PHASE_TIMING=1のDemoAuditCpuSortie.dll（02:04:22〜02:04:30、終了0）。960×540のaudit-cpu-phase-leadin.pngでCPU視点へ切替後、専用町名リストのない通常地図と開戦プライマリー上の手を目視。60Hz相当の通常入力で演出開始2.817秒に(410,1022)をクリック、3.833秒に(1311,552)から地点57を選択。DEMO-0016の先頭待機表示とDEMO-0018の演出待ちの修正を別シードで確認。ログはaudit-0203-各DLL名.log。
- 新規採番なし。今回、CPU軍事は地点選択まで。別シードのピッカー以降・専用表示への退避分岐、CPU内政の個別組立実演と自然Demo全周は未確認または既報継続。次回は同シードで出撃選択の確定・赤・閉鎖・人間復帰から続ける。

### 実行履歴：2026-11-12 02:59回

- 開始02:59:07 JST、終了03:00:24 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。開始時の関係差分はGame1.ComputerMarches.cs、tests/DirectDeliveryUi/Program.csと両記録。競合するゲーム・検証プロセスなし。ゲームと追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiの輸送・閉鎖の共有矩形を照合。既存demo-checkでALSOFT_DRIVERS=null、dotnet DirectDeliveryUi.dll（02:59:23〜02:59:33）、DemoInspectorUi.dll（02:59:33〜02:59:48）は終了0。直送便の退避復帰、左右格納庫と予約保持、540pの物理入力、輸送プライマリー、工場・閉鎖を確認。
- WORLD=1の通常診断で dotnet DemoAuditCpuSortie.dll（02:59:48〜02:59:59、終了0）。CPU征服者1に切り替わり地点57の通常ピッカーを開く。960×540のaudit-latest-computer-confirm.pngでカード上の手と選択表示を目視。個別4クリック、確定後の重複予約なし、赤チェック、閉鎖、表示対象15指示の完了と人間復帰を検査。前回の60Hz相当診断は地点選択まで、今回は以降を段階駆動。
- dotnet DemoUi.dll（02:59:59〜03:00:05、終了0）。Seed0の固定戦闘を含む巡回で輸送→地図、低FPS相当の縁移動、拒否分岐とエンディング周回を検査。証拠は既存出力内のaudit-0259-各DLL名.log。
- 新規採番なし。緑赤の分離・赤地点の再選択・消去は02:03回の確認範囲を維持。今回、他の専用表示への退避条件、CPU個別組立実演、自然Demo全周は未確認または既報継続。次回はCPU内政の格納庫と組替予約の照合、または自然Demoの別シードから続ける。

### 実行履歴：2026-11-12 03:58回

- 開始03:58:08 JST、終了03:59:29 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。関係差分はGame1.ComputerMarches.cs、tests/DirectDeliveryUi/Program.csと両記録。競合プロセスなし。ゲームと追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiの輸送プライマリー・閉鎖の共有矩形を照合。ALSOFT_DRIVERS=null、既存demo-checkで dotnet DirectDeliveryUi.dll（03:58:23〜03:58:32）、DemoInspectorUi.dll（03:58:32〜03:58:45）、DemoChecksUi.dll（03:58:45〜03:58:54）はすべて終了0。退避復帰、左右格納庫と予約保持、輸送と工場のクリック、緑赤の分離、赤地点の再選択・スポットライト解除、ターン末消去を検査。960×540のdemo-checks-3-6.pngで赤ハンドル上の手、赤チェックと即時予約1件を目視。
- FOLDERVERSE_DEMO_TEST_SEED=33の dotnet DemoUi.dll（03:58:54〜03:58:59、終了0）。世界／配役／配置129280377／977802721／433009304。輸送→地図、18 FPS相当の縁移動、拒否時の分岐、固定戦闘を含むエンディングからの周回を確認。証拠は既存出力内のaudit-0358-各DLL名.log。
- 新規採番なし。CPU軍事のWORLD=1の確認範囲は02:59回を参照。今回はCPU軍事の別の退避条件と内政の個別組立実演、固定戦闘を含まない自然Demo全周を未確認。次回はCPU内政の格納庫と組替予約を通常入力と比較する。

### 実行履歴：2026-11-12 04:57回

- 開始04:57:08 JST、終了04:58:17 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。関係差分はGame1.ComputerMarches.cs、tests/DirectDeliveryUi/Program.csと両記録。競合プロセスなし。ゲームと追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiの輸送プライマリー・閉鎖の共有矩形を照合。ALSOFT_DRIVERS=null、既存demo-checkで dotnet DirectDeliveryUi.dll（04:57:25〜04:57:34）とDemoInspectorUi.dll（04:57:34〜04:57:47）は終了0。退避復帰、左右格納庫と予約保持、540pの物理入力、輸送・工場・閉鎖のクリックと描画を確認。
- WORLD=1／FALLBACK=1で dotnet DemoAuditCpuSortie.dll（04:57:47〜04:57:56、終了0）。CPU征服者1、表示対象15指示。最初の表示段階を診断で専用表示へ切り替え、960×540のaudit-war-fallback.pngで町名と手を目視。次の指示が通常ピッカーへ戻り、カード上の手、未選択状態、確定ボタンをaudit-war-return.pngで目視。追加予約0、次の指示index=1／stage=3。自然発生の退避条件は未確認。ログはaudit-0457-各DLL名.log。
- 新規採番なし。CPU個別組立実演はDEMO-0017継続。04:28修正記録の予定在庫照合により、組立前でも通常格納庫には完成予定個体が見える。今回はCPU内政と緑赤の全経路、自然Demo全周を再実行せず。次回はCPU内政の表示オンオフの在庫と実描画、または別シードのDemo巡回から続ける。

### 実行履歴：2026-11-12 07:04回

- 起動要求07:04:10 JST。検証機の時計で実行開始07:03:24、終了07:04:22 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。関係差分はGame1.ComputerMarches.cs、tests/DirectDeliveryUi/Program.csと両記録。競合プロセスなし。ゲームと追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csの輸送プライマリーと閉鎖の共有矩形を照合。ALSOFT_DRIVERS=null、既存demo-checkの dotnet DirectDeliveryUi.dll、DemoInspectorUi.dll、DemoChecksUi.dll は終了0。直送便の退避復帰、左右格納庫、予約保持と540pの入力、輸送プライマリーと閉鎖、緑赤の表示分離と開戦予約の順序、ターン末消去を検査。
- DOMESTIC_ENTRY=1／DOMESTIC_SKIP=1／AUTO_ENABLED=0の dotnet DemoAuditCpuSortie.dll は終了0。組立無効では通常格納庫の予定と実在庫に頭・胴・脚が別々に残り、操作表示オフ・オン・組立前後のスキップで個体IDと在庫が一致。人間の既存予約を配送で一度だけ処理し、人間へ復帰。960×540のaudit-latest-computer-domestic.pngで通常格納庫と緑チェックを目視。証拠は既存出力内audit-0704-各DLL名.log。
- 新規採番なし。今回は自動組立無効の固定条件。自動組立有効は04:28修正記録、CPU軍事の専用表示は04:57回を参照。個別ドラッグ実演はDEMO-0017継続、自然Demo全周は未確認。次回は別シードのDemo巡回または軍事のスキップなし遷移から続ける。

### 実行履歴：2026-11-12 08:03回

- 開始08:03:42 JST、終了08:04:51 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。関係差分はGame1.ComputerMarches.cs、tests/DirectDeliveryUi/Program.csと両記録。競合プロセスなし。ゲーム・追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiの輸送プライマリー・閉鎖の共有矩形を照合。ALSOFT_DRIVERS=nullで既存demo-checkの dotnet DirectDeliveryUi.dll（08:03:58〜08:04:04）、DemoInspectorUi.dll（08:04:04〜08:04:18）は終了0。退避復帰、左右格納庫と予約保持、540p入力、輸送と工場のクリック・閉鎖を確認。
- WORLD=2の通常診断で dotnet DemoAuditCpuSortie.dll（08:04:18〜08:04:31、終了0）。CPU征服者9へ切替後に予定地点27を通常ピッカーで開き、4体を個別選択。確定後の重複予約なし、赤チェック、閉鎖、表示対象15指示をスキップなしで完了して人間へ復帰。960×540のaudit-latest-computer-confirm.pngでカード上の手と選択表示を目視。ログは既存出力内のaudit-0803-各DLL名.log。
- 新規採番なし。緑赤の表示分離・赤地点の再選択・ターン末消去は07:04回以前を参照。今回の未確認は自然発生する軍事専用表示、DEMO-0017の個別組立実演、自然Demo全周。次回は別シードのDemo巡回または軍事の自然な専用表示条件の検査から続ける。

### 実行履歴：2026-11-12 08:56回

- 開始08:56:43 JST、終了08:57:51 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。関係差分はGame1.ComputerMarches.cs、tests/DirectDeliveryUi/Program.csと両記録。競合プロセスなし。ゲーム・追跡テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiの輸送・閉鎖の共有矩形を照合。ALSOFT_DRIVERS=null、既存demo-checkの dotnet DirectDeliveryUi.dll（08:56:58〜08:57:05）、DemoInspectorUi.dll（08:57:05〜08:57:19）は終了0。退避復帰、左右格納庫と予約保持、540pの入力、輸送プライマリーと工場・閉鎖を確認。
- FOLDERVERSE_DEMO_TEST_SEED=20の dotnet DemoUi.dll（08:57:19〜08:57:26、終了0）。世界／配役／配置970412988／848972489／1018591594。初回輸送と地図への復帰、18 FPS相当の縁移動、拒否分岐、固定戦闘を含むエンディングからの周回を検査。
- LEGAL_QUEEN=1／QUEEN540=1の dotnet DemoAuditCpuSortie.dll（08:57:26〜08:57:30、終了0）。固定の合法なクイーン指示で地点91、通常ピッカー、クイーン参加の切替、確定と赤チェックを確認。960×540のaudit-legal-queen.pngで赤チェックと閉鎖へ移った手を目視。証拠は既存出力内audit-0856-各DLL名.log。
- 新規採番なし。自然発生する軍事専用表示、CPU個別組立実演（DEMO-0017）、自然Demo全周は未確認または既報継続。緑赤全経路は07:04回を参照。次回は通常軍事で複数の出撃元を切り替える場面、またはCPU内政から続ける。

### 実行履歴：2026-11-13 00:10回

- 起動要求00:10:11、終了00:11:31 JST。部分点検完了。HEAD=3ad92296a925260b96175a39d3c8cc9683f9f667。関係差分はGame1.ComputerMarches.cs、tests/DirectDeliveryUi/Program.csと両記録。競合プロセスなし。ゲーム・追跡テスト無変更。既存demo-checkの保持期限を11月13日へ延長し再利用。
- ALSOFT_DRIVERS=nullで dotnet DirectDeliveryUi.dll（00:10:34〜00:10:44）、DemoInspectorUi.dll（00:10:44〜00:11:00）、DemoChecksUi.dll（00:11:00〜00:11:07）はすべて終了0。直送便の退避復帰、左右格納庫・予約保持、輸送プライマリー、事務所と工場のクリック・閉鎖、展開図／地星儀の1080p／540pで緑赤分離、赤地点の再選択とスポットライト解除、予約確定からターン終了時の消去までを確認。
- CARD19=1／CARD37=1の dotnet DemoAuditCpuSortie.dll（00:11:07〜00:11:10、終了0）。固定拡張格納庫で通常ピッカー3ページを通して37体を選択。重複予約なし、次の征服者の通常ピッカー、スキップによる人間復帰。960×540のaudit-card37.pngで3/3、選択37体とカード上の手を目視。ログは既存出力内のaudit-0010-各DLL名.log。
- 新規採番なし。自然発生の軍事専用表示、自然Demo全周、DEMO-0017個別組立実演は未確認または既報継続。次回はCPU内政の格納庫・組立と通常入力の照合、または自然Demoの別シードから続ける。

### 実行履歴：2026-11-13 00:22回

- 開始00:22:11 JST、終了00:24:48 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。開始時の関連ゲーム・テスト・記録に未コミット差分なし。競合プロセスなし。ゲームとテスト無変更。
- 前回HEADからの格納庫行切替・描画矩形変更を確認し、Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiを照合。dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --no-restore（既存demo-check出力、警告・エラー0）で現HEADのゲームDLLを更新。
- ALSOFT_DRIVERS=null、同出力で DirectDeliveryUi.dll、DemoInspectorUi.dll、DemoChecksUi.dll、DemoUi.dll、DemoActionsUi.dll を順次実行、すべて終了0。輸送プライマリー・退避復帰、左右格納庫と予約保持、工場6計画、緑赤の分離・赤の再選択と消去、Demoの組立6条件の実ドラッグと移動予約、Seed0の固定戦闘を含む巡回を検査。実行証拠はaudit-0022-各DLL名.log。
- WORLD=1の DemoAuditCpuSortie.dll（00:23:58〜00:24:07、終了0）。通常ピッカーで予定地点57と4体の個別選択、確定後の赤、閉鎖、人間復帰を検査。960×540のaudit-latest-computer-confirm.pngでカード上の手、選択表示と確定ボタンを目視。
- 新規採番なし。CPU専用表示の自然発生、DEMO-0017のCPU個別組立実演と自然Demo全周は未確認または既報継続。次回はCPU内政の格納庫と新しい行切替を照合する。

### 実行履歴：2026-11-13 00:41回

- 開始00:41:41 JST、終了00:42:52 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。開始時の関係差分は点検記録のみ。競合プロセスなし。ゲーム・テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiを起点に、既存demo-checkの現行DLLをALSOFT_DRIVERS=nullで実行。DirectDeliveryUi.dll（00:41:55〜00:42:02）、DemoInspectorUi.dll（00:42:02〜00:42:13）、DemoActionsUi.dll（00:42:13〜00:42:29）は終了0。退避復帰、左右格納庫、予約保持、輸送と工場のクリック、組立の実ドラッグと和平移動を確認。
- DOMESTIC_ENTRY=1／DOMESTIC_SKIP=1／AUTO_ENABLED=0でDemoAuditCpuSortie.dll（00:42:29〜00:42:33、終了0）。960×540のaudit-latest-computer-domestic.pngで新しい行切替表示、3パーツ、CPU視点と緑チェックを目視。表示オン・オフ・スキップで在庫一致、人間の予約を配送で一度だけ処理して復帰。証拠はaudit-0041-各DLL名.log。
- 新規採番なし。今回CPU軍事と緑赤全経路は再実行せず、00:22回を参照。CPU個別組立（DEMO-0017）、自然Demo全周、軍事の自然な専用表示は未確認または既報継続。次回は軍事のページをまたぐ選択と通常復帰を現行HEADで照合する。

### 実行履歴：2026-11-13 00:59回

- 開始00:59:12 JST、終了01:00:15 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。開始時の関係差分は点検記録のみ。競合プロセスなし。ゲーム・テスト無変更。
- 既存demo-checkの現行DLLをALSOFT_DRIVERS=nullで実行。DirectDeliveryUi.dll、DemoInspectorUi.dll、DemoChecksUi.dllはいずれも終了0。直送便の退避復帰、左右格納庫・予約保持、輸送プライマリーのクリック、緑赤分離、赤地点の再選択・スポットライト解除、予約確定からターン終了時の消去を確認。
- CARD19=1／CARD37=1のDemoAuditCpuSortie.dll（00:59:54〜00:59:57、終了0）。固定拡張格納庫で通常ピッカー3ページ、37体選択、重複予約なし、次の征服者の通常ピッカー、スキップで人間復帰を検査。960×540のaudit-card37.pngで3/3とカード上の手を目視。証拠はaudit-0059-各DLL名.log。
- 新規採番なし。CPU内政の個別組立実演、軍事の自然な専用表示、自然Demo全周は未確認または既報継続。次回は別シードのDemo巡回、または通常軍事の自然発生分岐から続ける。

### 実行履歴：2026-11-13 01:21回

- 開始01:21:12 JST、終了01:22:27 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は点検記録のみ。競合プロセスなし。ゲーム・テスト無変更。
- 既存demo-checkでALSOFT_DRIVERS=nullの dotnet DirectDeliveryUi.dll、DemoInspectorUi.dll、DemoUi.dll（SEED=12）は終了0。退避復帰、左右格納庫・予約保持、輸送プライマリーと閉鎖、工場、縁移動、固定戦闘を含むDemo巡回を検査。ログはaudit-0121-各DLL名.log。
- WORLD=3でDemoAuditCpuSortie.dll（01:21:52〜01:22:00、終了0）。CPU征服者3、通常ピッカーで地点91と4体の個別選択、確定後の赤と閉鎖、通常移動UIで次指示の和平地点93に到着し人間へ復帰。960×540のaudit-latest-computer-confirm.pngでカード上の手を目視。
- 新規採番なし。緑赤全経路は00:59回の確認範囲を維持。CPU個別組立実演、自然発生する専用表示、自然Demo全周は未確認または既報継続。次回はCPU内政の組立有効時の描画、または別シードの軍事分岐を確認する。

### 実行履歴：2026-11-13 01:39回

- 開始01:39:12 JST、終了01:40:21 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は点検記録のみ。競合プロセスなし。ゲームとテスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiの輸送プライマリー・閉鎖の共有矩形を照合。ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll、DemoInspectorUi.dll、DemoChecksUi.dllは終了0。退避復帰、左右格納庫、予約保持、工場と閉鎖、1080p／540pの緑赤分離・開戦予約の順序・消去を確認。
- WORLD=0のDemoAuditCpuSortie.dll（01:39:54〜01:40:03、終了0）で自動組立有効時の通常格納庫と軍事・和平・人間復帰を検査。960×540のaudit-latest-computer-domestic.pngで格納庫タブ上の手と完成予定の個体を目視。通常格納庫の表示だけでは個別ドラッグを確認できず、DEMO-0017継続。ログはaudit-0139-各DLL名.log。
- 新規採番なし。軍事の自然発生の専用表示、CPU個別組立実演、固定場面を含まないDemo全周は未確認。次回は軍事のクイーンと随伴機の通常ピッカーを現行HEADで確認する。

### 実行履歴：2026-11-13 02:00回

- 開始02:00:12 JST、終了02:01:32 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は点検記録のみ。競合プロセスなし。ゲーム・テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dllとDemoInspectorUi.dllは終了0。輸送プライマリーのクリック、直送便の退避復帰、左右格納庫と予約保持、工場と閉鎖を確認。
- LEGAL_QUEEN=1／QUEEN540=1のDemoAuditCpuSortie.dllを随伴なし・QUEEN_ESCORTS=1の2条件で実行、両方終了0。固定地点91で通常ピッカーのクイーン参加と随伴2体の個別選択、重複予約なし、赤、閉鎖、スキップなしの人間復帰を検査。960×540のaudit-legal-queen.pngでクイーン参加／2体と赤、閉鎖へ移った手を目視。証拠audit-0200-各DLL名.logとaudit-0200-escorts.log。
- 新規採番なし。緑赤の全経路は01:39回を参照。CPU個別組立、自然発生する専用表示、自然Demo全周は未確認または既報継続。次回はクイーンが参加しないポケット出撃と解決後の状態を照合する。

### 実行履歴：2026-11-13 02:18回

- 開始02:18:43 JST、終了02:19:57 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は点検記録のみ。競合プロセスなし。ゲーム・テスト無変更。
- Game1.Demo.cs／Game1.DemoDomestic.csとDemoInspectorUiの輸送・閉鎖を照合。ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll、DemoInspectorUi.dll、DemoChecksUi.dllは終了0。退避復帰、左右格納庫、予約保持、輸送と工場のクリック、緑赤分離と予約確定からターン末消去を確認。
- LEGAL_QUEEN=1／QUEEN540=1／QUEEN_ESCORTS=1／POCKET_ONLY=1でDemoAuditCpuSortie.dll（02:19:26〜02:19:31、終了0）。固定地点91で通常ピッカーから指定ID767・768を個別選択し、クイーン参加なし、重複予約なし、赤・閉鎖と人間復帰。解決後の戦闘で両IDが各1体、CPU側クイーンなし。960×540のaudit-legal-queen.pngでクイーン不参加／2体と赤を目視。ログはaudit-0218-各DLL名.log。
- 新規採番なし。CPU内政の個別組立実演、自然発生する専用表示、自然Demo全周は未確認または既報継続。次回は通常軍事の別シードか、内政で複数征服者へ切り替わる条件から続ける。

### 実行履歴：2026-11-13 02:41回

- 開始02:41:13 JST、終了02:42:25 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は点検記録のみ。競合プロセスなし。ゲーム・テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll、DemoInspectorUi.dll、DemoChecksUi.dllはすべて終了0。輸送プライマリーと閉鎖、直送便の退避復帰・左右格納庫・予約保持、緑赤分離・再選択・ターン末消去を確認。
- WORLD=4のDemoAuditCpuSortie.dll（02:41:54〜02:42:03、終了0）。CPU征服者9の視点から地点15を選び、通常ピッカーで4体を個別選択。確定後の赤・閉鎖、別の征服者49での通常UIによる和平地点83への到着、人間復帰を検査。960×540のaudit-latest-computer-confirm.pngでカード上の手と選択表示を目視。証拠はaudit-0241-各DLL名.log。
- 新規採番なし。今回の軍事経路で自然な専用表示への退避は観測せず。CPU個別組立実演と自然Demo全周は未確認または既報継続。次回は内政で複数征服者へ切り替わる条件から続ける。

### 実行履歴：2026-11-13 02:58回

- 開始02:58:43 JST、終了02:59:49 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は点検記録のみ。競合プロセスなし。ゲーム・テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（02:58:56〜02:59:05）とDemoInspectorUi.dll（02:59:05〜02:59:17）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫と予約保持、工場と閉鎖を検査。
- WORLD=5のDemoAuditCpuSortie.dll（02:59:17〜02:59:29、終了0）。予定地点20、通常ピッカーで4体を個別選択、確定後に赤、閉鎖と表示対象17指示の終了・人間復帰を検査。960×540のaudit-latest-computer-confirm.pngで選択カードに手が乗ることを目視。証拠audit-0258-各DLL名.log。
- 新規採番なし。緑赤全経路は02:41回を参照。今回はCPU内政の複数征服者切替と個別組立実演、自然発生する軍事専用表示、自然Demo全周を未確認。次回はCPU内政の複数征服者を固定条件で照合する。

### 実行履歴：2026-11-13 03:19回

- 開始03:19:14 JST、終了03:21:53 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。追跡対象の関連差分は点検記録のみ。競合プロセスなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=nullで既存demo-checkのDirectDeliveryUi.dll（03:20:38〜03:20:47）とDemoInspectorUi.dll（03:20:47〜03:20:59）は終了0。輸送プライマリー、直送便の退避復帰と予約保持、左右格納庫、工場のクリック矩形と閉鎖を検査。
- 非追跡診断DemoAuditCpuSortieに征服者・地点・操作段階の記録を追加し、既存demo-checkへdotnet build --no-restore --disable-build-servers（警告・エラー0）。WORLD=0で同DLLを実行（03:20:59〜03:21:14、終了0）。表示オンの30征服者で視点が先に切り替わり、地図から予定地点を選び、通常格納庫を開くことを確認。緑チェック・閉鎖の各段階を通り、全員終了後にチェック消去と人間0への復帰を確認。960×540のaudit-latest-computer-domestic-green.pngで先頭征服者の通常格納庫、緑チェック、閉じるボタン上の手を目視。証拠はaudit-0319-各DLL名.log。
- 同じ診断で通常出撃ピッカーの2体選択、確定後の赤と閉鎖、通常移動UIによる和平地点98への到着と人間復帰を確認。新規採番なし。CPUの個別パーツ操作はDEMO-0017継続。今回の内政は固定シード・段階送りで、自然Demo全周と自然発生する軍事専用表示は未確認。次回は自然Demoの輸送から復帰する区間を優先する。

- 03:31修正回でDEMO-0017の再現を現HEADでも確認。個別組立実演は未対応を継続。表示オン・オフ、組立前後のスキップで在庫と既存予約の結果が一致。詳細は修正記録03:31回。

### 実行履歴：2026-11-13 03:40回

- 開始03:40:14 JST、終了03:41:41 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は点検・修正記録のみ。競合プロセスなし。ゲームコードとテストは無変更。
- ALSOFT_DRIVERS=nullで既存demo-checkのDirectDeliveryUi.dll（03:40:26〜03:40:33）とDemoInspectorUi.dll（03:40:33〜03:40:47）は終了0。輸送プライマリーの矩形、直送便の退避復帰、左右格納庫と予約保持、工場・閉鎖を確認。
- FOLDERVERSE_DEMO_TEST_SEED=12のDemoUi.dll（03:40:47〜03:40:51、終了0）。タイトル待機から自然に初回輸送へ到達。復帰部分はテスト側でWorldStatusを設定し、展開図／地星儀・辺移動後に内政を再開、配送後に軍事を経て次の輸送へ進むことを確認。初回輸送の自然終了は未確認。
- DemoChecksUi.dll（03:40:51〜03:40:57、終了0）。1080p／540p・展開図／地星儀でハンドルによる緑赤、出撃確定時の即予約、閉鎖、ターン末消去を照合。960×540のdemo-checks-3-6.pngで赤ハンドル上の手・地図の赤と予約1件を目視。
- WORLD=2／PHASE_TIMING=1のDemoAuditCpuSortie.dll（03:41:09〜03:41:20、終了0）。内政で通常格納庫と征服者切替を通過。軍事の通常入力で演出後2.817秒に開戦プライマリー、3.833秒に地点27を選択。証拠はaudit-0340-各DLL名.log。新規採番なし。CPU個別組立はDEMO-0017継続、自然発生する軍事専用表示と自然Demo全周は未確認。次回は描画を挟んだ初回輸送の自然終了と復帰を追う。

- 03:48修正回。DEMO-0017の再現と表示オン・オフ／スキップの結果一致を現HEADで確認。CPUの個別組立実演は検証待ちを継続。予定在庫と実在庫の選択条件は修正記録03:48回を参照。

### 実行履歴：2026-11-13 04:00回

- 開始04:00:14 JST、終了04:02:56 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は点検・修正記録のみ。競合プロセスなし。ゲームコードと追跡テストは無変更。
- DemoUiから派生した一時診断NaturalTransportを既存demo-check内でビルド（dotnet build --no-restore --disable-build-servers、最終実行は警告・エラー0）。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12で dotnet NaturalTransport.dll（04:02:06〜04:02:16、終了0）。初回輸送から18Hz相当の更新と間引いた実描画を行い、画面・ターンの強制変更なしで327フレーム目に戦闘、戦闘研究・産業・処遇を経て3055フレーム目にターン1の内政へ復帰。1920×1080のaudit-natural-WorldStatus.pngで地図とDemo表示の復帰を目視。audit-0400-NaturalTransport.log参照。
- 同じ既存出力のDirectDeliveryUi.dll（04:02:16〜04:02:24）とDemoInspectorUi.dll（04:02:24〜04:02:36）は終了0。輸送プライマリー、直送便の退避復帰と左右格納庫・予約保持、工場・閉鎖を照合。証拠はaudit-0400-各DLL名.log。
- 新規採番なし。緑赤・CPU経路は03:40／03:19点検を参照。CPU個別組立、自然発生する軍事専用表示、全シードの自然Demo全周は未確認または既報継続。次回は復帰直後に最初に開く地点とそのクリック先を追う。

- 04:09修正回。同一格納庫の既存組替予約とCPU自動組立が競合せず、予定格納庫でCPU選択部品が完成済みとして表示されることを固定モデルで確認。DEMO-0017の個別操作実演は未対応を継続。詳細は修正記録04:09回。

### 実行履歴：2026-11-13 04:19回

- 開始04:19:45 JST、終了04:21:04 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は点検・修正記録のみ。競合プロセスなし。ゲームコードと追跡テストは無変更。
- 一時診断NaturalTransportを既存demo-checkで再ビルド（dotnet build --no-restore --disable-build-servers、警告・エラー0）。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12で同DLLを実行（04:20:15〜04:20:26、終了0）。18Hz相当の更新と間引いた実描画で、初回輸送を経て3055フレーム目にターン1の内政に復帰。3088フレーム目の地図クリック（960,552）で地点52を選び、地点役所を開く状態に遷移。1920×1080のaudit-natural-first-node.pngで選択点上の手とインスペクターの出現開始を目視。
- 既存DirectDeliveryUi.dll（04:20:26〜04:20:33）とDemoInspectorUi.dll（04:20:33〜04:20:46）は終了0。輸送プライマリー、直送便の退避復帰・左右格納庫・予約保持、工場と閉鎖を確認。証拠はaudit-0419-各DLL名.log。
- 新規採番なし。今回の復帰後は最初の地点選択まで。格納庫への自然遷移は次回の続き位置。緑赤とCPUの通常経路は03:40／03:19を参照。CPU個別組立と自然発生の軍事専用表示、全シードの自然Demo全周は未確認または既報継続。

- 04:28修正回。DEMO-0017の通常格納庫経路でクリック位置に手が残るよう変更。960×540で緑ハンドル上の手とチェック反映を確認。個別パーツドラッグ実演は検証待ちを継続。詳細は修正記録04:28回。

### 実行履歴：2026-11-13 04:40回

- 開始04:40:45 JST、終了04:42:04 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は04:28修正のGame1.ComputerDomestic.csと両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- 一時診断NaturalTransportを既存demo-checkへ再ビルド（dotnet build --no-restore --disable-build-servers、警告・エラー0）。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12で同DLLを実行（04:41:14〜04:41:26、終了0）。初回輸送後、ターン1で地点52を開き、3128フレーム目にスライド完了後の格納庫タブ矩形(678,210,190,40)内の(773,230)をクリック。格納庫が開き、1920×1080のaudit-natural-first-hangar.pngで手がタブに重なることを目視。
- 既存DirectDeliveryUi.dll（04:41:26〜04:41:33）とDemoInspectorUi.dll（04:41:33〜04:41:45）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫・予約保持、工場と閉鎖を確認。証拠はaudit-0440-各DLL名.log。
- 新規採番なし。緑赤とCPUは04:28修正時の確認を参照。今回は復帰後の格納庫まで。次回は同じ場面で格納庫から工場・緑チェック・閉鎖を追う。CPU個別組立と自然発生する軍事専用表示、自然Demo全周は未確認または既報継続。

- 04:48修正回。DEMO-0017の入荷予約を伴う条件で、予定格納庫と実在庫の選択を照合。既存予約の実施は1件。個別ドラッグ実演は未対応を継続。

### 実行履歴：2026-11-13 05:01回

- 開始05:01:15 JST、終了05:02:40 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はGame1.ComputerDomestic.csの04:28修正と両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- NaturalTransportを既存demo-checkへ再ビルド（dotnet build --no-restore --disable-build-servers、警告・エラー0）。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12で同DLLを実行（05:01:49〜05:02:01、終了0）。初回輸送後の地点52で格納庫から工場タブ(377,230)、製造操作(268,332)と計画選択(184,482)、緑ハンドル(936,544)、閉じる(936,488)へ自然に遷移。1920×1080のaudit-natural-domestic-20.pngで手が緑ハンドルに重なり、地図の緑と製造中の表示を目視。ログで閉鎖開始を確認。
- DirectDeliveryUi.dll（05:02:01〜05:02:07）とDemoInspectorUi.dll（05:02:07〜05:02:20）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫・予約保持、工場と閉鎖を照合。証拠はaudit-0501-各DLL名.log。
- 新規採番なし。赤チェックとCPUの回帰は04:28修正を参照。CPU個別組立実演と自然発生の軍事専用表示、全シードの自然Demo全周は未確認または既報継続。次回は同じ自然経路で閉鎖後のターン終了と緑チェック消去を確認する。

### DEMO-0019：CPUの操作表示設定で自動組立の完成個体が変わる

- 状態：完了（05:28修正回で固定再現・実描画を検証）。2026-11-13 05:12:47 JST確認。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はGame1.ComputerDomestic.csの04:28クリック演出修正と点検・修正記録。固定診断の在庫状態で確認。対象条件の画面解像度と実描画は未確認。
- 再現：世界0／Cast123／Placement456でCPU1の地点91に他国由来の頭ID1と自国の頭・胴・脚ID2〜4を置く。自動組立を有効にし、組立は自国部品2以上、出撃は自国部品3を必須にする。人間の内政終了後、CPUの［操作を表示］オフ、オン通常、オンで組立前後にスキップを比較する。
- 期待：同じ初期在庫・指示・設定で、操作表示の有無にかかわらず組立結果が一致する。実際：オフは完成ID1と頭ID2、オンの3経路は頭ID1と完成ID2。両方で既存の人間予約は1回だけ実施され、人間へ復帰。個別ドラッグが見えないDEMO-0017とは別に、ゲーム状態に差が出る。
- 証拠：既存demo-checkのfix-0511-policy.log。実行条件は05:11修正記録を参照。次の点検で実描画と他の調練条件を照合する。

### 実行履歴：2026-11-13 05:22回

- 開始05:22:16 JST、終了05:23:44 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はGame1.ComputerDomestic.csの04:28修正と両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- DEMO-0019の診断に実描画を追加し既存demo-checkへ再ビルド（警告・エラー0）。05:11修正回と同じ環境変数でDemoAuditCpuSortie.dll（05:22:50〜05:23:06、在庫一致のアサーションで終了-532462766）。表示オフは完成ID1、オン通常・前後スキップは完成ID2を再現。960×540のaudit-policy-before.png／audit-policy-after.pngで、通常格納庫の完成個体がID1からID2へ入れ替わることを目視。ログはaudit-0522-DemoAuditCpuSortie.log。DEMO-0019は未解決、追加採番なし。
- DirectDeliveryUi.dll（05:23:06〜05:23:14）、DemoInspectorUi.dll（05:23:14〜05:23:25）は終了0。輸送プライマリー、直送便の退避復帰と左右格納庫、予約保持、工場と閉鎖を確認。証拠audit-0522-各DLL名.log。
- 次回修正はDEMO-0019の表示オン・オフで部品選択と実行順序をそろえる。自然Demoの閉鎖後のターン終了・緑消去、CPU個別ドラッグ、自然発生する軍事専用表示は今回未確認または既報継続。自然Demoの続きは05:01回を参照。

- 05:28修正回でDEMO-0019を完了。表示オフ・オン通常・前後スキップで完成ID1・頭ID2が一致し、960×540で予定在庫表示を確認。DEMO-0017の個別操作は継続。

### 実行履歴：2026-11-13 05:41回

- 開始05:41:16 JST、終了05:42:47 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はGame1.ComputerDomestic.cs、CountrySettingsUi.csと両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、Seed=12の一時診断NaturalTransport.dll（05:41:51〜05:42:04、終了0）。1920×1080、18Hz相当で初回輸送から復帰し、地点52に緑を付けた後、3293フレーム目に閉鎖開始。3333フレーム目にターン終了ボタンの矩形(160,926,192,144)内の(256,998)をクリック。配送へ遷移し地点52の緑チェックが消えたことを状態で確認。audit-natural-domestic-20.png／11.pngとaudit-0541-NaturalTransport.log。
- DirectDeliveryUi.dll（05:42:04〜05:42:11）、DemoInspectorUi.dll（05:42:11〜05:42:24）、DemoChecksUi.dll（05:42:24〜05:42:32）は終了0。直送便の退避復帰、左右格納庫・予約保持、工場と閉鎖、展開図／地星儀・1080p／540pの緑赤分離、出撃確定と予約・消去を確認。証拠はaudit-0541-各DLL名.log。
- 新規採番なし。CPUの表示オン・オフは05:28修正を参照。CPU個別組立、自然発生する軍事専用表示、全シードの自然Demo全周は未確認または既報継続。次回は同じ自然経路の配送後に軍事側の最初の操作を追う。

- 05:49修正回。DEMO-0017の予定在庫から既存予約に触れず組立順を読めることを確認。個別実演は未対応を継続。DEMO-0019の在庫一致は回帰なし。

### 実行履歴：2026-11-13 06:01回

- 開始06:01:46 JST、終了06:03:12 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はGame1.ComputerDomestic.cs、CountrySettingsUi.csと両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- 既存demo-checkへNaturalTransportを再ビルド（警告・エラー0）。ALSOFT_DRIVERS=null、Seed=12で実行（06:02:22〜06:02:35、終了0）。1920×1080で内政の緑チェック・閉鎖・ターン終了を通り、3333フレーム目の配送から3431フレーム目にターン2の出兵輸送へ進んだ。軍事処理後の進軍16件をログで確認。この区間では軍事の手動操作画面を経由しなかった。証拠audit-0601-NaturalTransport.log、audit-natural-after-delivery.png。
- DirectDeliveryUi.dll（06:02:35〜06:02:42）、DemoInspectorUi.dll（06:02:42〜06:02:56）は終了0。輸送プライマリー、直送便の退避復帰・左右格納庫・予約保持、工場・閉鎖を確認。証拠audit-0601-各DLL名.log。
- 新規採番なし。次回は次の内政復帰後、巡回回数が偶数の開戦実演を追う。赤チェックの固定検査は05:41、CPU在庫一致は05:49を参照。CPUの個別組立、自然発生する専用表示、自然Demo全周は未確認または既報継続。

- 06:11修正回。DEMO-0016の強制退避から通常ピッカーへ戻ることを確認。自然退避の発生条件は要再確認。ゲームコードの追加変更なし。

### 実行履歴：2026-11-13 06:18回

- 開始06:18:17 JST、終了06:19:42 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はGame1.ComputerDomestic.cs、CountrySettingsUi.csと両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- 既存demo-checkへNaturalTransportを再ビルド（警告・エラー0）。ALSOFT_DRIVERS=null、Seed=12で実行（06:18:47〜06:19:03、終了0）。ターン2の内政復帰後に開戦プライマリー(410,1022)、地点49(1140,632)、出撃元・個体選択、確定(1415,822)で予約1件、赤ハンドル(936,544)、閉鎖(936,488)を順に確認。1920×1080のaudit-natural-domestic-35.pngで赤ハンドルに重なる手、地図の赤、予約1件を目視。証拠audit-0618-NaturalTransport.log。
- DirectDeliveryUi.dll（06:19:03〜06:19:10）、DemoInspectorUi.dll（06:19:10〜06:19:21）は終了0。輸送プライマリー、直送便の退避復帰と左右格納庫・予約保持、工場と閉鎖を再確認。証拠audit-0618-各DLL名.log。
- 新規採番なし。今回は自然開戦の閉鎖開始まで。次回は閉鎖後のターン終了、赤消去と予約が実行される地点を追う。CPUの自然退避、個別組立実演、自然Demo全周は未確認または既報継続。

- 06:31修正回。DEMO-0016の自然退避はSeed 6／7の最初の和平指示まででは再現せず、他の条件を要再確認。詳細は修正記録06:31回。

### 実行履歴：2026-11-13 06:39回

- 開始06:39:47 JST、終了06:41:12 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はGame1.ComputerDomestic.cs、CountrySettingsUi.csと両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- 既存demo-checkの一時診断NaturalTransportを再ビルド（診断のnullable注釈の警告1、エラー0）。ALSOFT_DRIVERS=null、Seed=12で実行（06:40:21〜06:40:37、終了0）。1920×1080で地点49の確定・即予約、赤ハンドル、閉鎖から5939フレーム目のターン終了へ自然に進み、配送開始時に赤が消えた。6092フレーム目に同じ征服者・地点49の進軍が1件だけ実行された。証拠audit-0639-NaturalTransport.logとaudit-natural-domestic-35.png／36.png。
- DirectDeliveryUi.dll（06:40:37〜06:40:44）、DemoInspectorUi.dll（06:40:44〜06:40:56）は終了0。輸送プライマリー、直送便の退避復帰と左右格納庫・予約保持、工場と閉鎖を再確認。証拠audit-0639-各DLL名.log。
- 新規採番なし。自然な赤チェック後の再選択は今回未実施（固定検査は05:41）。次回は別シードで同じ経路、またはCPUの和平後の専用表示を検査する。CPU個別組立・自然Demo全周は未確認または既報継続。

### 実行履歴：2026-11-13 06:51回

- 開始06:51:47 JST、終了06:54:02 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はGame1.ComputerDomestic.cs、CountrySettingsUi.csと両記録。競合プロセスなしで開始。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null。DirectDeliveryUi.dll（06:52:01〜06:52:10）、DemoInspectorUi.dll（06:52:10〜06:52:21）、Seed=20のDemoUi.dll（06:52:21〜06:52:28）は終了0。直送便の退避復帰・左右格納庫・予約保持、工場・閉鎖、輸送プライマリー、初回輸送と地図解説を確認。証拠audit-0651-各DLL名.log。
- Seed=20のNaturalTransport.dllで1920×1080の自然巡回を検査。地点71を選択して確定直後に予約1件、赤ハンドル(936,544)、閉鎖(936,488)、ターン終了で赤消去。配送後の進軍件数は0。初回診断は進軍1件を前提としたアサーションで停止（06:52:28〜06:53:26、終了-532462766）。診断を再ビルドして取消理由を記録した再実行（06:53:27〜06:53:44、終了0）では、出撃元72・個体ID18の予約が1件取り消され、理由は予定メンバーが出撃できなかったこと。証拠audit-0651-NaturalTransport-reason.log、audit-natural-domestic-35.png／36.png。
- 今回確認したクリック順に新規不具合なし。ID18が出撃条件を失った理由は未確認で、次回は配送前後の役割・予約状態を照合する。CPU個別組立と自然な専用表示、自然Demo全周は未確認または既報継続。

### DEMO-0020：Demoの出撃予約と自動守備が同じ個体を使う

- 状態：完了（07:11修正回でSeed 20と守備回帰を検証）。2026-11-13 07:00:38 JST確認。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はGame1.ComputerDomestic.cs、CountrySettingsUi.csと両記録。1920×1080、18Hz相当の更新と間引いた実描画。
- 再現：NaturalTransportをSeed=20で実行。ターン2の開戦実演で地点71を選び、出撃元72からID18を確定、赤チェック、閉鎖、ターン終了を待つ。期待は、配送完了時に出撃可能な予約個体を自動守備へ回さず進軍すること。
- 実際：配送開始時までID18は出撃元72にあり、完成体・出撃可能・役割なし。軍事処理直後は同地点・同個体で守備専用かつ出撃不可となり、地点71への進軍は0件、予約は1件取消。ComputerJointAttacks.Planは予約個体をusedに集めるが、BattleLessons.ReserveHomeGuardsへ渡さず、DemoのautomaticAllでは人間側も守備割当の対象になる。自動守備呼び出し前後の単独切り分けは次回修正時に行う。
- 証拠：audit-0659-NaturalTransport.log、audit-natural-domestic-35.png／36.png。06:51回の同じ取消を原因側から追跡したもので、追加採番は本件のみ。

### 実行履歴：2026-11-13 06:59回

- 開始06:59:47 JST、終了07:01:27 JST。部分点検完了。上記HEAD・差分。競合プロセスなし。ゲームコードと追跡テストは無変更。
- 既存demo-checkのNaturalTransportを再ビルド（警告・エラー0）。ALSOFT_DRIVERS=nullで実行（07:00:22〜07:00:38、終了0）。診断は進軍・明示取消の双方を記録する。結果はDEMO-0020を参照。
- DirectDeliveryUi.dll（07:00:38〜07:00:46）とDemoInspectorUi.dll（07:00:46〜07:00:57）は終了0。直送便の退避復帰・左右格納庫・予約保持、プライマリー、工場と閉鎖を確認。証拠audit-0659-各DLL名.log。
- 次回はDEMO-0020を優先。CPU個別組立・自然な専用表示・全シードの自然Demo全周は今回未確認または既報継続。

- 07:11修正回でDEMO-0020完了。地点72の出撃予約ID18を守備へ割り当てず、地点71への地上出撃1件と取消0件を確認。詳細は修正記録07:11回。

### 実行履歴：2026-11-13 07:20回

- 開始07:20:48 JST、終了07:21:48 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はGame1.ComputerDomestic.cs、ComputerJointAttacks.cs、BattleLessons.EmptyHome.cs、CountrySettingsUi.cs、LongDelivery/Program.csと両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（07:21:01〜07:21:09）とDemoInspectorUi.dll（07:21:09〜07:21:21）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫と予約保持、工場・閉鎖を確認。
- FOLDERVERSE_AUDIT_WORLD=8のDemoAuditCpuSortie.dll（07:21:21〜07:21:33、終了0）。内政の征服者切替、軍事の通常ピッカーで征服者15が地点170へ4体を個別選択し確定、赤チェック、25件の表示終了と人間復帰を確認。和平0件の経路で実描画。証拠audit-0720-各DLL名.log。
- 新規採番なし。DEMO-0020のSeed 20自然巡回は07:11修正回を参照。自然発生する軍事専用表示、CPU個別組立、全シードの自然Demo全周は未確認または既報継続。次回は和平を含む指示列の後半を検査する。

- 07:29修正回。Seed 6／7で和平後から指示列終端まで自然退避を検出せず。DEMO-0016の他条件は要再確認を継続。

### 実行履歴：2026-11-13 07:38回

- 開始07:38:18 JST、終了07:39:37 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（07:38:46〜07:38:55）、DemoInspectorUi.dll（07:38:55〜07:39:06）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫・予約保持、工場・閉鎖を確認。
- 一時診断NaturalTransportを960×540に変更して再ビルド（警告・エラー0）、Seed=20で実行（07:39:06〜07:39:20、終了0）。自然巡回の地点71への出撃確定・即予約、赤ハンドル、閉鎖、ターン終了時の赤消去、配送後のID18の到着と地上出撃1件・取消なしを確認。audit-natural-540-domestic-35.pngで手と赤チェック、予約1件を目視。証拠audit-0738-各DLL名.log。
- 新規採番なし。CPUの和平後の専用表示は07:29回を参照。CPU個別組立・他の自然退避条件・自然Demo全周は未確認または既報継続。次回は540pの別シードで閉鎖・ターン終了を照合する。

- 07:52修正回。DEMO-0016の自然退避はSeed 9／10で再現せず、別条件を要再確認。通常出撃・和平・人間復帰は検査済み。

### 実行履歴：2026-11-13 07:59回

- 開始07:59:48 JST、終了08:00:48 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（08:00:02〜08:00:11）とDemoInspectorUi.dll（08:00:11〜08:00:23）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫・予約保持、工場・閉鎖を照合。
- Seed=12でNaturalTransport.dll（08:00:23〜08:00:34、終了0）。960×540で地点49への出撃確定と予約1件、赤チェック、閉鎖、ターン末の赤消去、配送後の地点52→49の地上出撃1件・ID34の到着・取消なしを確認。証拠audit-0759-各DLL名.log、audit-natural-540-domestic-35.png／36.png。
- 新規採番なし。CPU経路は07:20と07:52を参照。次回は自然Demoの別シードまたはCPU軍事のスキップ前後を比較。CPU個別組立、自然発生する軍事専用表示、全シードの自然Demo全周は未確認または既報継続。

- 08:11修正回。DEMO-0016の自然退避はSeed 11／12で再現せず、対象外指示と別配置は要再確認。詳細は修正記録08:11回。

### 実行履歴：2026-11-13 08:21回

- 開始08:21:49 JST、終了08:23:01 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（08:22:03〜08:22:12）とDemoInspectorUi.dll（08:22:12〜08:22:24）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫・予約保持、工場・閉鎖を照合。
- WORLD=2／PHASE_TIMING=1のDemoAuditCpuSortie.dll（08:22:24〜08:22:35、終了0）で960×540の演出待ちと通常入力を確認。演出後2.817秒に開戦プライマリー(410,1022)、3.833秒に地点27(1201,552)を選択。PHASE_SKIP=1（08:22:35〜08:22:44、終了0）では演出0.250秒のEscで人間0・内政へ復帰。証拠audit-0821-各条件.log、audit-cpu-phase-leadin.png／entry.png。
- 新規採番なし。今回の通常側は地点選択まで、スキップ側は演出中。次回は出撃確定直後のスキップと完走の進軍結果を比較する。CPU個別組立・自然退避・全シードの自然Demo全周は未確認または既報継続。

- 08:29修正回。CPU軍事で出撃確定直後のEscと完走の進軍・取消・人間復帰が一致。DEMO-0016の自然退避とDEMO-0017の個別組立は継続。

### 実行履歴：2026-11-13 08:40回

- 開始08:40:19 JST、終了08:41:54 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（08:40:53〜08:41:02）とDemoInspectorUi.dll（08:41:02〜08:41:14）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫・予約保持、工場・閉鎖を確認。
- WORLD=9のDemoAuditCpuSortie.dllをCONFIRM_COMPLETION=normal／skipで比較（08:41:14〜08:41:35、両方終了0）。960×540の通常ピッカーで確定後に完走・Escへ分岐し、進軍38件の征服者・出撃元・目標と取消0件が一致。未処理予約0、人間0へ復帰。証拠audit-0840-各条件.log。
- 新規採番なし。参加個体の同一性、CPU個別組立、自然発生の軍事専用表示、全シードの自然Demo全周は未確認または既報継続。次回は別シードの自然Demoか、CPU軍事で戦闘参加個体まで比較する。

- 08:49修正回。Seed 9のCPU軍事で確定後スキップと完走の戦闘参加個体を比較し、27場面で一致。自然退避と個別組立は引き続き未確認または既報継続。

### 実行履歴：2026-11-13 08:59回

- 開始08:59:49 JST、終了09:01:53 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（09:00:10〜09:00:19）とDemoInspectorUi.dll（09:00:19〜09:00:30）は終了0。輸送プライマリー、直送便の退避復帰と左右格納庫・予約保持、工場と閉鎖を確認。
- Seed=33のNaturalTransport.dllで960×540の自然巡回。地点51への即時予約、赤ハンドル、閉鎖とターン末の赤消去、出撃元53からの進軍1件・取消なし。初回（09:00:30〜09:00:56）は目的地格納庫だけを確認する診断が戦闘参加中の個体を見落として失敗。診断に戦闘の盤上・待機個体を追加し再ビルド（警告・エラー0）。再実行（09:01:25〜09:01:37、終了0）で予約ID62の地点51での戦闘参加を確認。証拠audit-0859-各DLL名.log、audit-0859-NaturalTransport-confirm.logとaudit-natural-540-domestic-35.png／36.png。
- 新規採番なし。CPU戦闘参加個体の比較は08:49を参照。CPU個別組立、自然発生する軍事専用表示、全シードの自然Demo全周は未確認または既報継続。次回はSeed 33の戦闘後の復帰か別配置の軍事専用表示を追う。

- 09:10修正回。DEMO-0016の自然退避は和平が各2件あるSeed 13／14でも再現せず、他条件を要再確認。詳細は修正記録09:10回。

### 実行履歴：2026-11-13 09:20回

- 開始09:20:50 JST、終了09:22:07 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（09:21:11〜09:21:20）とDemoInspectorUi.dll（09:21:20〜09:21:32）は終了0。輸送プライマリー、直送便の退避復帰と左右格納庫・予約保持、工場と閉鎖を再検査。
- WORLD=10／CONFIRM_COMPLETION=normal・skipのDemoAuditCpuSortie.dll（09:21:32〜09:21:52、両方終了0）。960×540の確定直後を描画し、完走とEscで進軍34件、戦闘24場面の盤上・待機の個体ID、取消0件、人間復帰が一致。証拠audit-0920-各条件.log。
- 新規採番なし。次回はSeed 33の戦闘後の自然復帰を追う。CPU個別組立・自然発生する軍事専用表示・全シードの自然Demo全周は未確認または既報継続。

- 09:28修正回。DEMO-0016の自然退避はSeed 15／16で再現せず。Seed 17は和平なしの完走のみ確認。残る条件は要再確認。

### 実行履歴：2026-11-13 09:40回

- 開始09:40:50 JST、終了09:42:19 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（09:41:24〜09:41:33）とDemoInspectorUi.dll（09:41:33〜09:41:45）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫・予約保持、工場と閉鎖を確認。
- 一時診断NaturalTransportを再ビルド（警告・エラー0）、Seed=33で実行（09:41:45〜09:41:58、終了0）。960×540の自然巡回で地点51へ予約ID62が1回出撃、取消なし。6423フレーム目の戦闘、戦闘研究、産業を経て7639フレーム目にターン3の征服者0の内政へ自然復帰。証拠audit-0940-各DLL名.log、audit-natural-540-post-war.png。
- 新規採番なし。次回は戦闘後復帰時の最初の地点クリックか、CPUの別配置で軍事専用表示を確認。CPU個別組立・自然発生する軍事専用表示・全シードの自然Demo全周は未確認または既報継続。

- 09:50修正回。Seed 17の和平なし指示列で初回出撃後から終端まで自然退避0。DEMO-0016の別配置は要再確認。

### 実行履歴：2026-11-13 09:59回

- 開始09:59:20 JST、終了10:00:38 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（09:59:46〜09:59:53）とDemoInspectorUi.dll（09:59:53〜10:00:06）は終了0。輸送プライマリー、直送便の退避復帰、左右格納庫・予約保持、工場・閉鎖を確認。
- 一時診断NaturalTransportを再ビルド（警告・エラー0）、Seed=33で実行（10:00:06〜10:00:18、終了0）。戦闘後7639フレーム目に内政へ復帰し、8642フレーム目の地図クリック（論理960,552）で地点11を選び役所を開くことを確認。960×540のaudit-natural-540-post-war-office.pngで選択地点上の手とインスペクターの出現開始を目視。証拠audit-0959-各DLL名.log。
- 新規採番なし。次回は同じ復帰後に格納庫か工場を選ぶ段階を照合。CPU個別組立・軍事の自然退避・全シードの自然Demo全周は未確認または既報継続。

- 10:09修正回。DEMO-0019の表示オン・オフ／スキップで在庫・予約の一致を再確認。DEMO-0017の個別ドラッグ実演は未対応を継続。

### 実行履歴：2026-11-13 10:20回

- 開始10:20:50 JST、終了10:22:21 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は07:11までのゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkのDirectDeliveryUi.dll（10:21:26〜10:21:35）とDemoInspectorUi.dll（10:21:35〜10:21:47）は終了0。輸送プライマリー、直送便の退避復帰と左右格納庫・予約保持、工場・閉鎖を確認。
- 一時診断NaturalTransportを再ビルド（警告・エラー0）、Seed=33で実行（10:21:47〜10:22:01、終了0）。戦闘後の内政復帰から地点11を選び、8682フレーム目に格納庫タブ矩形(678,210,190,40)内の(773,230)をクリック。通常格納庫が開き、960×540のaudit-natural-540-post-war-hangar.pngでタブ上の手と空き2区画を目視。証拠audit-1020-各DLL名.log。
- 新規採番なし。次回は同じ地点の工場タブが無効な場合に、Demoが緑チェック・閉鎖へ進むか確認。CPU個別組立、軍事の自然退避、全シードの自然Demo全周は未確認または既報継続。
- 10:31修正回。DEMO-0016の自然退避は配置18の初回出撃確認後から終端、配置19の最初の和平まで再現せず。配置19の和平以降と別配置は要再確認。

### DEMO-0021：工場のない地点でDemoが表示されていない工場ボタンをクリックする

- 状態は未解決。2026-11-13 10:41:59〜10:42:13 JST。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は BattleLessons.EmptyHome.cs、ComputerJointAttacks.cs、Game1.ComputerDomestic.cs と関連テスト・両記録の既存変更。DemoDomesticと工場UIに差分なし。実描画960×540、以下の座標は論理1920×1080。
- 再現は ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=33 で既存demo-checkの NaturalTransport.dll を実行。戦闘後の内政復帰から地点11を開き、格納庫を経て工場段階8に到達する。この地点は製造ライン0本。
- 期待は無効な工場をクリックせず、地点の緑チェックと閉鎖へ進むこと。実際は8715フレーム目に無効な工場タブ矩形(282,210,190,40)内の(377,230)をクリックし、工場が開かないまま段階9へ進む。8748フレーム目に(0,0)、8781フレーム目に(184,482)へクリックを表示した。工場は両方とも閉じていた。Game1.DemoDomestic.cs の段階8〜10。
- 証拠は artifacts/tmp/delete-after-2026-11-13/demo-check の audit-1040-NaturalTransport.log と audit-natural-540-post-war-factory.png。8814フレーム目に緑チェックが付与され、8847フレーム目に閉鎖が始まることは各実矩形と描画で確認した。画像は同じ出力先の post-war-green／close。

### 実行履歴：2026-11-13 10:40回

- 開始10:40:21、終了10:42:43 JST。部分点検完了。開始時にゲーム・dotnetプロセスなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null で既存の DirectDeliveryUi.dll（10:40:50〜10:40:59）、DemoInspectorUi.dll（10:40:59〜10:41:10）、DemoChecksUi.dll（10:41:10〜10:41:18）を実行し、全て終了0。直送便の退避復帰、左右と予約保持、通常工場、輸送プライマリー、緑赤のチェックとターン末消去を確認。証拠は audit-1040-各DLL名.log。NaturalTransportの診断を延長し再ビルド（警告・エラー0）、Seed33は終了0。
- 次回修正回はDEMO-0021の再現から。次回点検は閉鎖後の移動プライマリーへ進む。CPU個別組立、軍事の自然退避、全シードの自然Demo全周は未確認または既報継続。

- DEMO-0021 状態更新は完了（2026-11-13 10:52:50 JST）。Seed33の960×540実描画で工場クリックを省略し、実ハンドルの緑チェックと閉鎖へ進むことを確認。通常工場と緑赤の回帰検査も成功。詳細は修正記録。

### 実行履歴：2026-11-13 11:01回

- 開始11:01:51、終了11:05:05 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は前回までのゲーム・テストと両記録（DEMO-0021を含む）。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkで DirectDeliveryUi.dll（11:02:02〜11:02:11）、DemoInspectorUi.dll（11:02:11〜11:02:22）、DemoChecksUi.dll（11:02:22〜11:02:31）は終了0。輸送プライマリー、直送便の退避復帰と左右・予約保持、工場、緑赤の実入力・表示分離・消去を確認。
- NaturalTransportを再ビルドしSeed33で自然再生。戦闘後の地点11は格納庫→緑チェック→閉鎖の後、移動プライマリーの段階18を経由せず19へ進んだ。この時点は戦略計画中で、8698フレーム目に論理(256,998)が終了ボタン矩形(160,926,192,144)に収まり、配送画面へ遷移した。960×540で描画。最初の診断は段階18を待ち続けて次巡のターン比較で停止し、次の診断は矩形取得のリフレクションで停止。診断を直して11:04:35〜11:04:48に終了0を確認。
- 証拠は同じ出力先の audit-1101-*.log と audit-natural-540-post-war-turn-end.png。新規採番なし。次回は移動プライマリーが利用できる条件の段階18から目的地選択を確認。CPU個別組立、軍事の自然退避、全Seedの自然巡回は未確認または既報継続。

- 11:08修正回。DEMO-0016は配置20の初回出撃確認後から終端まで自然退避を再現せず。別配置と和平後は要再確認。

### 実行履歴：2026-11-13 11:21回

- 開始11:21:21、終了11:22:56 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は前回までのゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkで DirectDeliveryUi.dll（11:21:39〜11:21:47）、DemoInspectorUi.dll（11:21:47〜11:21:59）、DemoActionsUi.dll（11:21:59〜11:22:18）は終了0。直送便退避復帰、左右格納庫と予約保持、輸送プライマリー、わたしの国から移動ボタンの実矩形と画面遷移を確認。
- DemoActionsUiの6条件（1080p／540p、展開図／回転展開図／地星儀）で実地点89のヒット、論理(707,938)の予約操作から和平予約1件、別操作のターン終了を確認。対象なしの場合は架空のクリックを出さず終了ボタンへ進んだ。自然巡回では未確認。
- FOLDERVERSE_AUDIT_WORLD=19 で DemoAuditCpuSortie.dll（11:22:23〜11:22:36、終了0）。通常出撃後、征服者19の和平は960×540の既報の専用表示を通り地点54に到着。DEMO-0016へ重複登録なし。証拠は audit-1121-*.log。
- 新規採番なし。次回は自然巡回で和平予約が使える配置を探す。CPU個別組立と別配置での軍事の自然退避、全Seedの自然巡回は未確認または既報継続。

- 11:29修正回。DEMO-0016は配置21の初回出撃確認後から終端まで自然退避を再現せず。配置22は既知の和平専用経路を確認。通常出撃の自然退避は要再確認。

### 実行履歴：2026-11-13 11:40回

- 開始11:40:22、終了11:41:59 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021を含む前回までのゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（11:40:34〜11:40:43）、DemoInspectorUi.dll（11:40:43〜11:40:55）、FOLDERVERSE_DEMO_TEST_SEED=34のDemoUi.dll（11:40:55〜11:41:04）は終了0。直送便退避復帰・左右格納庫・予約保持、各プライマリーと既存の地図切替を確認。
- 同SeedのNaturalTransport.dll（11:41:21〜11:41:44、終了0）は960×540。戦闘後の地点68で実工場タブを開き、緑チェック・閉鎖の実矩形と状態を確認。段階11→19から、終了ボタン矩形内の論理(256,998)で配送へ遷移した。段階18と和平予約はこの経路では未到達。証拠は同出力先の audit-1140-*.log と audit-natural-540-post-war-turn-end.png。
- 新規採番なし。次回は自然再生中に内政／軍事フェーズと段階11・17の関係を追い、和平予約への到達条件を切り分ける。CPU個別組立・軍事の自然退避・全Seedの自然巡回は未確認または既報継続。

- 11:50修正回。DEMO-0016は再ビルドした診断で配置19／22の最初の和平から指示列終端まで自然退避を検出せず。別配置と後続和平の個別経路は要再確認。

### DEMO-0022：内政の地点操作後にDemoが和平移動を飛ばす

- 状態は未解決。2026-11-13 11:59:37〜12:00:01 JST。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分は既存のDEMO-0021等。Game1.DemoDomestic.csの段階11と末尾の加算はHEADと同じ。
- 再現は ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=34 で既存demo-checkの NaturalTransport.dll を実行。960×540の自然再生で内政の地点133、戦闘後は地点68の緑チェックを経て閉鎖する。
- 期待は内政で地点操作を閉じた後、わたしの国から移動を開いて選択可能な目的地を探し、該当する場合は予約を実演すること。実際は両地点でフェーズDomesticにもかかわらず段階11→19となる。段階11が18を代入した後、共通の加算で19に進むため、段階17・18を通らない。地点68では8875フレーム目に閉鎖、8915フレーム目に論理(256,998)で終了ボタンをクリックして配送へ進んだ。自然再生のこの2地点での目的地候補数は未検査。移動の目的地・予約処理自体は11:21回の固定条件で確認済み。
- 証拠は既存demo-checkの audit-1158-NaturalTransport.log と audit-natural-540-post-war-close.png／turn-end.png。

### 実行履歴：2026-11-13 11:58回

- 開始11:58:52、終了12:00:33 JST。部分点検完了。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テスト無変更。
- DirectDeliveryUi.dll（11:59:17〜11:59:26）とDemoInspectorUi.dll（11:59:26〜11:59:37）は終了0。直送便退避復帰、左右格納庫と予約保持、輸送プライマリーと固定条件の移動入口を確認。NaturalTransportを再ビルド（警告・エラー0）、Seed34で終了0。開戦後の段階36→19も観測。
- 次回修正回はDEMO-0022の再現から。次回点検は内政の閉鎖から移動入口・目的地選択へ進む自然再生を照合。CPU個別組立・別配置の自然退避・全Seedは未確認または既報継続。

- DEMO-0022は完了（2026-11-13 12:12:32 JST）。Seed34の自然再生を960×540で確認し、地点閉鎖から「わたしの国」、移動、地点68の和平予約1件まで到達。詳細は修正記録。

### 実行履歴：2026-11-13 12:18回

- 開始12:18:52、終了12:20:21 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存のゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（12:19:33〜12:19:42）、DemoInspectorUi.dll（12:19:42〜12:19:53）は終了0。輸送プライマリー、直送便退避復帰、左右格納庫、予約保持、移動入口を確認。
- NaturalTransport診断を和平予約後まで延長し dotnet build --no-restore --disable-build-servers（警告・エラー0）。FOLDERVERSE_DEMO_TEST_SEED=34で実行（12:19:53〜12:20:07、終了0）。960×540の自然再生で地点68への和平予約後、2704フレーム目の別クリック(256,998)がターン終了矩形(160,926,192,144)内。地点133の緑が消え、配送へ進むことを確認。証拠 audit-1218-*.log と同出力先の audit-natural-540-post-war-turn-end.png（今回は初回和平後の終了画面）。
- 新規採番なし。次回は配送後の和平予約の解決と到着を照合。CPU個別組立、別配置での軍事の自然退避、全Seedの自然巡回は未確認または既報継続。

- 12:31修正回。CPU組立の表示オン・オフとスキップで完成個体・在庫が一致。DEMO-0017の個別ドラッグ実演は未対応を継続。

### 実行履歴：2026-11-13 12:41回

- 開始12:41:53、終了12:43:17 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存のゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（12:42:04〜12:42:13）、DemoInspectorUi.dll（12:42:13〜12:42:25）、DemoChecksUi.dll（12:42:25〜12:42:33）は終了0。直送便の退避復帰・左右格納庫・予約保持、プライマリー、緑赤の実クリックとターン末消去を確認。
- NaturalTransportを到着まで延長し dotnet build --no-restore --disable-build-servers（診断の未使用変数警告1、エラー0）。FOLDERVERSE_DEMO_TEST_SEED=34で実行（12:42:47〜12:43:02、終了0）。960×540。地点68への和平予約と別クリックのターン終了を経て、2911フレーム目に征服者0が地点63から予約先68に到着。キャンペーンは内政・ターン2、描画は輸送画面。証拠 audit-1241-*.log、audit-natural-540-post-war-peaceful-arrival.png。
- 新規採番なし。次回は到着後に輸送画面から地図へ戻り、地点68を起点に次の操作へ進むことを照合。CPU個別組立、別配置の自然退避、全Seedは未確認または既報継続。

- 12:48修正回。DEMO-0016は配置23の初回出撃確認後から終端まで自然退避を再現せず。別配置は要再確認。

### 実行履歴：2026-11-13 13:01回

- 開始13:01:53、終了13:03:10 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存のゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（13:02:19〜13:02:28）、DemoInspectorUi.dll（13:02:28〜13:02:40）は終了0。直送便の退避復帰、左右格納庫・予約保持、輸送と移動のプライマリーを確認。
- NaturalTransportを地図復帰まで延長し dotnet build --no-restore --disable-build-servers（警告・エラー0）。FOLDERVERSE_DEMO_TEST_SEED=34で実行（13:02:40〜13:02:56、終了0）。和平移動後、戦闘研究と産業画面を経て5196フレーム目にターン2の地図へ自然復帰。征服者0の現在地は予約先68。960×540で描画。証拠 audit-1301-*.log と audit-natural-540-post-war-peaceful-map-return.png。
- 新規採番なし。次回は地図復帰後の最初のプライマリーと地点選択を照合。CPU個別組立・別配置での軍事の自然退避・全Seedの自然巡回は未確認または既報継続。

- 13:10修正回。DEMO-0016は配置24の初回出撃確認後から終端まで自然退避を再現せず。別配置は要再確認。

### 実行履歴：2026-11-13 13:18回

- 開始13:18:23、終了13:20:06 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存のゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（13:19:11〜13:19:20）、DemoInspectorUi.dll（13:19:20〜13:19:32）は終了0。直送便の退避復帰と左右格納庫・予約保持、輸送プライマリーを確認。
- NaturalTransportを再ビルド。最初のdotnet起動はレジストリ読み取り拒否、再実行で警告・エラー0。FOLDERVERSE_DEMO_TEST_SEED=34で実行（13:19:32〜13:19:51、終了0）。和平後の地図復帰から5910フレーム目に開戦プライマリー矩形(362,974,96,96)内の論理(410,1022)をクリックし、開戦選択へ遷移。5950フレーム目に論理(1223,456)で地点84を選び、地図のヒットも84。960×540で描画。証拠 audit-1318-*.log と audit-natural-540-post-war-peaceful-next-target.png。
- 新規採番なし。次回は地点84の出撃元選択・確定・赤チェックを自然再生で照合。CPU個別組立、別配置での自然退避、全Seedの自然巡回は未確認または既報継続。

- 13:30修正回。DEMO-0016は配置25の初回出撃確認後から終端まで自然退避を再現せず。別配置は要再確認。

### 実行履歴：2026-11-13 13:40回

- 開始13:40:54、終了13:42:40 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存のゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（13:41:45〜13:41:54）、DemoInspectorUi.dll（13:41:54〜13:42:06）は終了0。直送便退避復帰、左右格納庫と予約保持、輸送・移動プライマリーを確認。
- NaturalTransportを出撃確定と赤まで延長。診断のローカル名衝突を直し dotnet build --no-restore --disable-build-servers（警告・エラー0）。Seed34で実行（13:42:06〜13:42:25、終了0）。960×540の自然再生で地点84選択後に出撃元の操作(464,338)から出撃選択が開き、6049フレーム目に確定(1415,822)で80→84の予約1件。6082フレーム目は論理(936,544)が赤ハンドル矩形(912,520,48,48)内で、地点84の赤を確認。証拠 audit-1340-*.log と audit-natural-540-post-war-peaceful-next-red.png。
- 新規採番なし。次回はこの赤チェック後のスポットライトと閉鎖、ターン終了、予約の解決を自然再生で照合。CPU個別組立、別配置での自然退避、全Seedの自然巡回は未確認または既報継続。

- 13:51修正回。DEMO-0016は配置26の初回出撃確認後から指示列終端まで自然退避を検出せず。別配置・未対応操作は要再確認。

### 実行履歴：2026-11-13 14:00回

- 開始14:00:54、終了14:02:26 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（14:01:30〜14:01:39）とDemoInspectorUi.dll（14:01:39〜14:01:50）は終了0。直送便退避復帰、左右格納庫と予約保持、輸送・移動プライマリーを再確認。
- NaturalTransport診断を出撃解決まで延長し dotnet build --no-restore --disable-build-servers（警告・エラー0）。Seed34で実行（14:01:50〜14:02:11、終了0）。960×540の自然再生で地点84の赤チェック後にスポットライトが外れ、閉じるクリックがハンドル矩形内で閉鎖開始。6155フレーム目に配送へ進んで赤が消えた。6362フレーム目に予約個体67が80から84へ一度だけ出撃し、取消なし。証拠 audit-1400-*.log、audit-natural-540-post-war-peaceful-next-red.png／sortie-resolved.png。
- 新規採番なし。次回は赤チェック済み地点の再選択とスポットライト解除の両立を確認。CPU個別組立、別配置での自然退避、全Seedの自然巡回は未確認または既報継続。

- 14:08修正回。DEMO-0016は配置27の初回出撃確認後から指示列終端まで自然退避を検出せず。別配置・未対応操作は要再確認。

### 実行履歴：2026-11-13 14:19回

- 開始14:19:24、終了14:20:49 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（14:20:04〜14:20:13）とDemoInspectorUi.dll（14:20:13〜14:20:24）は終了0。直送便の退避復帰、左右格納庫と予約保持、プライマリーを確認。
- 既存の一時診断ProbeCheckedClickを現行の内政遷移に合わせて再ビルド（警告・エラー0、ローカルNuGetキャッシュ）。dotnet ProbeCheckedClick.dll（14:20:24〜14:20:31、終了0）。展開図／地星儀×1920×1080／960×540で、赤チェック後にスポットライトが外れた地点9へ通常入力で再クリックし、選択・赤チェック・予約を確認。緑との表示分離、閉鎖、ターン終了後の消去も確認。証拠 audit-1419-*.log と demo-checks-各条件-6.png。
- 新規採番なし。再クリックは固定条件での検証。次回は自然再生の地点84で同操作を照合。CPU個別組立、別配置での自然退避、全Seedの自然巡回は未確認または既報継続。

- 14:31修正回。DEMO-0016は配置28の初回出撃確認後から終端まで自然退避を再現せず。別配置・未対応操作は要再確認。

### 実行履歴：2026-11-13 14:41回

- 開始14:41:55、終了14:46:21 JST。優先項目の部分点検完了、自然再生後の再クリック照合は中断。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（14:42:31〜14:42:40）とDemoInspectorUi.dll（14:42:40〜14:42:52）は終了0。直送便退避復帰、左右格納庫・予約保持、輸送と移動のプライマリーを確認。
- NaturalTransportを警告・エラー0でビルドしSeed34を960×540で実行。地点84の赤チェックとスポットライト解除までは再確認。直後に診断側で選択を解除し、Demoフラグを一時オフにして通常入力を注入すると、開戦選択が閉じて地点68のインスペクターが開いた。診断側で対象を画面内に寄せた再試行でも同じ結果。再試行は論理(1440,552)、実ピクセル(720,276)。既存の通常入力固定検査は14:19回で成功しており、自然再生から通常入力へ移す診断の条件と通常プレイでの挙動の切り分けが未完了。証拠は既存demo-checkの audit-1441-NaturalTransport.log／audit-1441-reclick-focus.log。
- 新規採番なし。次回は再クリック直前の入力座標変換、地図の候補ヒット、通常入力の分岐を記録し、診断の切替なしで同条件を作って照合。CPU個別組立、別配置での自然退避、全Seedの自然巡回は未確認または既報継続。

- 14:50修正回。DEMO-0016は配置29の初回出撃確認後から終端まで自然退避を再現せず。別配置・未対応操作は要再確認。

### 実行履歴：2026-11-13 15:01回

- 開始15:01:55、終了15:03:45 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分は既存のDEMO-0021／0022を含むゲーム・テストと両記録。競合プロセスなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（15:02:41〜15:02:50）とDemoInspectorUi.dll（15:02:50〜15:03:02）は終了0。直送便退避復帰、左右格納庫・予約保持、輸送と移動プライマリーを確認。
- NaturalTransportを再ビルド（警告・エラー0）。Seed34、960×540で赤チェック直後の入力を切り分けた。入力直前に未表示の地元説得4件を確認。通常入力が先に処理する説得を診断内だけで除いた場合、論理(1440,552)、実ピクセル(720,276)で地点84を再選択し、赤と予約1件を保持。閉鎖、ターン終了で赤消去、出撃1回まで確認（15:03:02〜15:03:23、終了0）。証拠 audit-1501-*.log と audit-natural-540-post-war-red-reclick.png。
- 新規採番なし。14:41の失敗は説得演出の割り込みで説明できる。自然再生の途中で通常入力を注入した診断なので、説得の自然終了後に行う再選択は未確認。次回はこの条件か他の自然巡回へ。CPU個別組立、別配置の自然退避、全Seedは未確認または既報継続。

- 15:10修正回。DEMO-0016は配置30の初回出撃確認後から終端まで自然退避を再現せず。別配置・未対応操作は要再確認。

### 実行履歴：2026-11-13 15:20回

- 開始15:20:55、終了15:22:11 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（15:21:10〜15:21:19）、DemoInspectorUi.dll（15:21:19〜15:21:31）、DemoChecksUi.dll（15:21:31〜15:21:39）は終了0。直送便退避復帰、左右格納庫と予約保持、プライマリー、緑赤の表示分離とターン末消去を確認。
- NaturalTransport.dll（15:21:39〜15:21:57、終了0）、Seed34、960×540。前回と同じ診断条件で未表示の地元説得4件を除き、地点84を実ピクセル(720,276)から再選択。赤と予約1件を保持し、配送後の出撃1回を確認。証拠 audit-1520-*.log。
- 新規採番なし。今回も説得の自然終了後の再選択は未確認。次回は通常の説得完了から開戦画面を開く経路を照合。CPU個別組立、別配置での自然退避、全Seedは未確認または既報継続。

- 15:28修正回。DEMO-0016は配置31の初回出撃確認後から終端まで自然退避を再現せず。別配置・未対応操作は要再確認。

### 実行履歴：2026-11-13 15:40回

- 開始15:40:26、終了15:42:39 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- 一時診断の古い参照（0.2.0）が検証出力に混入し、優先テスト2件が起動時に失敗。現行0.7.0の出力を再ビルドして復旧し、診断も現行プロジェクト参照へ変更。最終ビルドは警告・エラー0。ALSOFT_DRIVERS=null、DirectDeliveryUi.dll（15:41:29〜15:41:38）とDemoInspectorUi.dll（15:41:38〜15:41:49）は終了0。直送便退避復帰、左右格納庫と予約保持、プライマリーを確認。
- 現行ビルドのDemoAuditPersuasionResume.dll（15:42:14〜15:42:23、終了0）。1920×1080／960×540の固定条件で、地元説得が格納庫を開き、約8秒で自動閉鎖。演出中にDemoクリックは発生せず、閉鎖後は論理(960,552)で地点26の操作へ復帰。証拠は audit-1540-*.log と audit-persuasion-resume-*.png。
- 新規採番なし。説得後の通常開戦と赤地点の再選択は未確認。次回は固定条件で説得終了後の開戦プライマリーと選択を照合。CPU個別組立、別配置の自然退避、全Seedは未確認または既報継続。

- 15:48修正回。DEMO-0016は配置32の初回出撃確認後から終端まで自然退避を再現せず。別配置・未対応操作は要再確認。

### 実行履歴：2026-11-13 15:58回

- 開始15:58:56、終了16:00:20 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（15:59:37〜15:59:46）、DemoInspectorUi.dll（15:59:46〜15:59:56）は終了0。直送便の退避復帰、左右格納庫と予約保持、プライマリーを確認。
- DemoAuditPersuasionResumeを同出力先へ再ビルド（警告・エラー0）。同DLL実行（15:59:56〜16:00:05、終了0）。1080p／540pの固定条件で地元説得の自動終了と閉鎖を待ち、通常入力で開戦プライマリー矩形(362,974,96,96)の中心をクリック。開戦選択が開き、論理(1296,450)で地点28を選択。説得中のDemoクリックなし。証拠 audit-1558-*.log、audit-persuasion-resume-3.png／7.png。
- 新規採番なし。説得後の出撃確定・赤とその再選択は今回未確認。次回はこの固定条件から続ける。CPU個別組立、別配置の自然退避、全Seedは未確認または既報継続。

- 16:10修正回。DEMO-0016は配置33の初回出撃確認後から終端まで自然退避を再現せず。2件目の和平と別配置は要再確認。

### 実行履歴：2026-11-13 16:22回

- 開始16:22:26、終了16:24:40 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関連差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（16:23:09〜16:23:18）とDemoInspectorUi.dll（16:23:18〜16:23:29）は終了0。直送便の退避復帰、左右格納庫と予約保持、プライマリーを確認。
- DemoAuditPersuasionResumeを出撃確定と赤まで延長し同出力へ再ビルド（警告・エラー0）。地元説得の終了と閉鎖を待ち、通常入力で開戦・地点28選択、続いてDemo段階32〜35を検査。1080p／540pとも、個体選択後は予約0件、確定(1415,822)で予約1件、論理(936,544)が赤ハンドル矩形内で赤が付いた。初回の540pは同一診断インスタンスに前ケースの赤が残り、クリックを省略。ケース初期化にチェック消去を加えた再実行は終了0（16:24:24）。証拠 audit-1622-*.log と audit-persuasion-resume-3.png／7.png。
- 新規採番なし。次回は説得後の赤チェック済み地点に通常入力で再選択し、予約が維持されるか照合。CPU個別組立、別配置の自然退避、全Seedは未確認または既報継続。

- 16:29修正回。DEMO-0016は配置34の初回出撃確認後から終端まで自然退避を再現せず。別配置と未対応操作は要再確認。

### 実行履歴：2026-11-13 16:41回

- 開始16:41:57、終了16:43:13 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（16:42:29〜16:42:38）とDemoInspectorUi.dll（16:42:38〜16:42:50）は終了0。直送便の退避復帰、左右格納庫・予約保持、プライマリーを確認。
- DemoAuditPersuasionResumeを同出力へ再ビルド（警告・エラー0）。実行16:42:50〜16:42:58、終了0。1080p／540pの固定条件で、地元説得の自動終了から開戦・確定・赤まで進めた。赤地点28のスポットライト解除後、通常入力で再選択し、赤と出撃予約1件を保持。証拠 audit-1641-*.log と audit-persuasion-resume-3.png／7.png。
- 新規採番なし。説得後の再選択は固定条件で確認済み。自然巡回での説得終了後、CPU個別組立、別配置の自然退避、全Seedは未確認または既報継続。次回は地図・輸送以外の自然経路かCPUの未確認条件へ進む。

- 16:51修正回。DEMO-0016は配置35の初回出撃確認後から終端まで自然退避を再現せず。別配置と未対応操作は要再確認。

### 実行履歴：2026-11-13 17:00回

- 開始17:00:57、終了17:02:57 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコード・追跡テスト無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（17:01:11〜17:01:20）とDemoInspectorUi.dll（17:01:20〜17:01:31）は終了0。直送便退避復帰、左右格納庫と予約保持、プライマリーを確認。
- FOLDERVERSE_AUDIT_WORLD=36で DemoAuditCpuSortie.dll（17:01:31〜17:01:42、終了0）。960×540でCPU視点への切替、通常出撃、和平地点125の選択・到着、人間復帰を確認。専用表示へ退避した状態を固定するFOLDERVERSE_AUDIT_FALLBACK=1（17:02:29〜17:02:38、終了0）では、次の指示の段階3に到達、追加予約0件。FALLBACK=2はQueen属性だけで退避するという旧診断の前提で失敗。現行コードではQueenの通常経路があり、この失敗は実際の退避として扱わない。証拠 audit-1700-*.log。
- 新規採番なし。自然発生する退避条件・CPU個別組立・全Seedの自然巡回は未確認または既報継続。次回は実際の退避条件を作り、専用表示から通常UIへ復帰する経路を照合。

- 17:09修正回。DEMO-0016は配置37の初回出撃確認後から終端まで自然退避を再現せず。別配置・未対応操作は要再確認。

### 実行履歴：2026-11-13 17:19回

- 開始17:19:57、終了17:21:08 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。関係差分はDEMO-0021／0022を含む既存ゲーム・テストと両記録。開始時にゲーム・dotnet・MSBuildなし。ゲームコードと追跡テストは無変更。
- ALSOFT_DRIVERS=null、既存demo-checkの DirectDeliveryUi.dll（17:20:25〜17:20:34）とDemoInspectorUi.dll（17:20:34〜17:20:46）は終了0。直送便退避復帰、左右格納庫・予約保持、プライマリーを確認。
- FOLDERVERSE_AUDIT_CARD19=1、FOLDERVERSE_AUDIT_CARD37=1で DemoAuditCpuSortie.dll（17:20:46〜17:20:51、終了0）。960×540の固定拡張格納庫で、地点0→3の37体を通常の出撃選択3ページで指定IDどおり選択。追加予約なし。次の征服者の出撃選択は1ページ目、スキップで人間に復帰。証拠 audit-1719-*.log と audit-card37.png。
- 新規採番なし。多ページの通常再生を確認した範囲では退避せず。実条件での退避、CPU個別組立、全Seedは未確認または既報継続。次回は合法な出撃で表示開始後に対象が操作不能になる条件と次指示への復帰を検討。

- 17:30修正回。DEMO-0016は配置38で通常出撃の4体選択・確定後の赤チェックと最初の和平地点103を確認。通常出撃の退避は要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 17:38回

- 開始17:38:57、終了17:40:15 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、ComputerJointAttacks.cs、DemoInspectorUi、DemoChecksUi。ゲームコード・追跡テスト無変更。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（17:39:13〜17:39:22、終了0）は960×540で直送便の退避・復帰、アニメーション中の入力抑止、左右格納庫と予約保持を確認。dotnet ./DemoInspectorUi.dll（17:39:22〜17:39:34、終了0）は1920×1080／960×540でプライマリー、工場6計画、閉じる・移動と実クリック矩形を確認。例として (196,332) は矩形 (160,288,72,88) 内で製造開始。dotnet ./DemoChecksUi.dll（17:39:34〜17:39:42、終了0）は地図／地星儀×2解像度で、緑・赤の実クリック、地点選択→出撃確定・即予約→赤チェック→閉じる→ターン終了、チェックの分離とターン末消去を確認。
- 960×540で FOLDERVERSE_AUDIT_WORLD=38、FOLDERVERSE_AUDIT_FALLBACK=1 を設定し dotnet ./DemoAuditCpuSortie.dll（17:39:47〜17:39:56、終了0）。退避段階を注入した後、指示1の和平専用表示（段階6）へ進み、予約0→0。証拠は実行出力の「Fallback next index=1 stage=6 ruler=1 reservations=0/0」。退避を開始する実条件と、和平に続く通常出撃への復帰はこの実行では未確認。
- 新規採番なし。次回は通常出撃再生中に対象が操作不能になる条件と通常UIへの復帰を照合。CPU個別組立、全Seed、今回の赤チェック後の再クリック・スポットライト解除の個別検証は未確認。

- 17:48修正回。配置39の通常出撃で4体の選択、確定後の赤チェック、人間への復帰を確認。DEMO-0016の自然退避は0件で要再確認を継続。詳細は修正記録。

### 実行履歴：2026-11-13 17:58回

- 開始17:58:58、終了17:59:59 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正は完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、ComputerJointAttacks.cs、DemoInspectorUi、DemoChecksUi。ゲームコードと追跡テスト無変更。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（17:59:10〜17:59:16、終了0）は960×540で直送便の退避・復帰、移動中の入力抑止、左右格納庫と予約保持を確認。dotnet ./DemoInspectorUi.dll（17:59:16〜17:59:30、終了0）は1920×1080／960×540でプライマリー、工場・閉じる・移動の矩形と実描画を確認。
- dotnet ./ProbeCheckedClick.dll（17:59:30〜17:59:37、終了0）は地図／地星儀と2解像度で赤チェック地点を再クリック。実行出力では内部点 (1440,552)、540pで画面点 (720,276)。緑と赤の実クリック、出撃確定→予約→赤チェック→閉じる→ターン終了、表示モード分離とターン末消去も通過。スポットライト解除の単独描画は今回未確認。
- dotnet ./DemoUi.dll（17:59:37〜17:59:41、終了0）はテストSeed0で待機開始、直送便から地図への復帰、地図／地星儀、18 FPSで辺移動、拒否時分岐、終了ループとラベル描画を確認。証拠は各実行のPASSおよび最初の直送便／地図復帰の状態出力。
- 新規採番なし。CPU軍事の通常出撃中に退避を発生させる条件、退避から通常出撃へ戻る遷移、CPU個別組立、全Seedは今回未確認。次回はCPU軍事の実条件を優先。

- 18:10修正回。配置40で通常出撃から赤チェックへの遷移と最初の和平地点40を確認。DEMO-0016の通常出撃中の退避条件は要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 18:20回

- 開始18:20:28、終了18:21:26 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正は完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.ComputerDomestic.cs、ComputerJointAttacks.cs、Game1.DemoDomestic.cs、DemoInspectorUi。ゲームコード・追跡テスト無変更。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=39、FOLDERVERSE_AUDIT_FALLBACK=1。dotnet ./DemoAuditCpuSortie.dll（18:20:39〜18:20:49、終了0）は960×540で退避段階を注入し、征服者17から次の征服者19の通常出撃選択（段階3）へ復帰。予約0→0。証拠は「Fallback next index=1 stage=3 ruler=19 reservations=0/0」。18:20回は退避後の復帰を確認し、退避を起こす実条件は未確認。
- dotnet ./DirectDeliveryUi.dll（18:20:49〜18:20:57、終了0）は960×540で直送便の退避・復帰、左右格納庫・予約保持と移動中の入力抑止を確認。dotnet ./DemoInspectorUi.dll（18:20:57〜18:21:11、終了0）は1920×1080／960×540でプライマリー、工場・閉じる・移動のクリックと描画を確認。例として (196,332) は矩形 (160,288,72,88) 内で製造開始。
- 新規採番なし。次回は通常出撃の再生中に対象が操作不能になる条件を照合。CPU個別組立とスポットライト解除の単独描画、全Seedは今回未確認。

- 18:30修正回。配置41で後続指示への遷移と最初の和平地点105を確認。DEMO-0016の通常出撃中の退避条件は要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 18:41回

- 開始18:41:58、終了18:42:59 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、ComputerJointAttacks.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（18:42:13〜18:42:22）、dotnet ./DemoInspectorUi.dll（18:42:22〜18:42:33）、dotnet ./DemoChecksUi.dll（18:42:33〜18:42:42）はすべて終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー、工場計画の実矩形内クリックと製造開始を確認。1920×1080／960×540の地図・地星儀で緑と赤の実クリック、出撃確定・即予約→赤チェック→閉じる→ターン終了、モード分離、ターン末消去を確認。証拠は各実行のPASS。
- DEMO-0016の退避開始箇所を Game1.ComputerMarches.cs の AdvanceComputerWarPreview で静的照合。通常出撃の段階1は実地点の投影・入力範囲・地点ヒットが一致しなければ退避。段階2は出撃元の行が消えるか、指示内の個体IDが現在の候補と一致しなければ退避し、CloseMovement後に段階-1へ入る。次回は通常出撃の地点選択後、段階2に入る直前に診断上で指示の個体を候補から外し、退避表示、二重予約なし、次の通常出撃への復帰を確認する。
- 新規採番なし。上記の退避開始条件の実行、CPU個別組立、赤チェックのスポットライト解除の単独描画、全Seedは未確認。

- 18:51修正回。DEMO-0016は通常の地点クリック後に表示用指示の個体IDだけを差し替えると、候補照合から専用表示へ退避し、追加予約0件で次の通常出撃選択へ復帰。自然発生は要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 19:00回

- 開始19:00:59、終了19:02:08 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。既存の関係差分は Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、ComputerJointAttacks.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=39、FOLDERVERSE_AUDIT_FALLBACK=3。dotnet ./DemoAuditCpuSortie.dll（19:01:11〜19:01:21、終了0）。960×540で通常の地点クリック後に表示用個体IDを差し替え、段階-1の専用表示を描画。征服者17から19へ切替後、通常出撃選択（段階3）へ復帰し予約0→0。証拠は「Candidate mismatch entered stage -1 through the normal preview」「Fallback next index=1 stage=3 ruler=19 reservations=0/0」と既存の audit-war-fallback.png／audit-war-return.png。
- dotnet ./DirectDeliveryUi.dll（19:01:21〜19:01:29）、dotnet ./DemoInspectorUi.dll（19:01:29〜19:01:41）、dotnet ./DemoChecksUi.dll（19:01:41〜19:01:48）はすべて終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー、工場計画の矩形内クリックと製造開始を確認。地図／地星儀の1920×1080／960×540で緑・赤の実クリック、出撃確定・即予約→赤チェック→閉じる→ターン終了、モード分離とターン末消去を確認。
- 新規採番なし。自然に生じる軍事候補不一致、CPU個別組立、赤チェックのスポットライト解除の単独描画、全Seedは未確認。次回はCPU内政の個別組立と手の位置を既報DEMO-0017に照合する。

- 19:09修正回。DEMO-0017は操作表示とスキップによる最終在庫の差なし。格納庫を開いた時点で組立オンの物理在庫は3部品、予測は完成品で、個別ドラッグ実演は未対応を継続。詳細は修正記録。

### 実行履歴：2026-11-13 19:18回

- 開始19:18:59、終了19:19:56 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.ComputerDomestic.cs、Game1.DemoDomestic.cs、DemoInspectorUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP=1、FOLDERVERSE_AUDIT_DOMESTIC_ENTRY=1、FOLDERVERSE_AUDIT_AUTO_ENABLED=1、FOLDERVERSE_AUDIT_ASSEMBLY_POLICY=1、FOLDERVERSE_AUDIT_ASSEMBLY_RECIPE=1。dotnet ./DemoAuditCpuSortie.dll（19:19:12〜19:19:18、終了0）。960×540の通常格納庫で、期待する組立実演は胴ID3→頭ID1、脚ID4→頭胴ID1。実際は開いた時点から完成ID1と頭ID2の予測を表示し、組立前後の診断フレームで物理在庫は4部品のまま。個別ドラッグは既報DEMO-0017の未対応範囲。証拠は「Forecast assembly 1」「before/after 960x540」と既存 fix-policy-before.png／fix-policy-after.png。表示オフ・通常・スキップの結果は全て完成ID1・頭ID2。既存予約は配送で一度反映し、人間へ復帰。
- dotnet ./DirectDeliveryUi.dll（19:19:18〜19:19:24）、dotnet ./DemoInspectorUi.dll（19:19:24〜19:19:37）は両方終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー、工場計画の矩形内クリックと製造開始を確認。例として (196,332) は矩形 (160,288,72,88) 内。証拠は各実行のPASS。
- 新規採番なし。CPUの部品ごとの押下・解放座標と状態更新、自然発生する軍事退避、スポットライト解除の単独描画、全Seedは未確認。次回は予測組立前の部品IDを通常格納庫スロットへ対応付ける条件を照合する。

- 19:28修正回。DEMO-0017の入荷・既存予約を含む組立後在庫を診断で確認。個別ドラッグ実演は未対応。詳細は修正記録。

### 実行履歴：2026-11-13 19:40回

- 開始19:40:29、終了19:41:34 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（19:40:41〜19:40:47）、dotnet ./DemoInspectorUi.dll（19:40:47〜19:41:02）、dotnet ./ProbeCheckedClick.dll（19:41:02〜19:41:09）はすべて終了0。直送便の退避・復帰と左右格納庫の予約保持、プライマリーと工場の矩形・製造遷移、緑／赤の実クリック、出撃確定・即予約→赤チェック→閉じる→ターン終了、表示分離とターン末消去を確認。1920×1080／960×540の地図・地星儀で赤チェック後の再クリックが通過。証拠は実行のPASS、540pの画面点 (720,276)。
- スポットライト解除は静的照合。NodeCheckClick は赤チェック変更時に RefreshWarTargetHighlights を呼び、赤チェック地点を AttackHighlightTargets から外す。クリック先の MovementTargets は保持され、上記の再クリックと整合。解除の単独描画は未確認。
- DEMO-0017の格納庫スロットを静的照合。SyncHangarSlots は PlanningStock に残るIDの位置を保持し、新規IDは空きへ入れる。VisibleAssemblySlots と AssemblyPartBounds は現在ページに従う。組立実演の次回検証は、予約反映後・組立前の在庫で描画したページから出発し、部品IDが各矩形と一致することを確認する。個別ドラッグの実行、自然発生する軍事退避、全Seedは今回未確認。新規採番なし。

- 19:50修正回。DEMO-0017は隔離した3部品の直接組立と組替予約で最終個体が一致。CPU格納庫での個別ドラッグ実演は未対応。詳細は修正記録。

### 実行履歴：2026-11-13 19:58回

- 開始19:58:30、終了19:59:46 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（19:58:41〜19:58:50）と dotnet ./DemoInspectorUi.dll（19:58:50〜19:59:01）は終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー・工場計画の実矩形と製造開始を確認。
- dotnet ./DemoActionsUi.dll（19:59:01〜19:59:20、終了0）。1920×1080／960×540で通常Demoの個別部品、2種類の部品セットと一体型の押下・ドラッグ・解放、和平移動予約と別操作のターン終了、地図／地星儀の投影と地点ヒットを確認。証拠は最終PASSと和平地点89・カーソル (707,938) の出力。CPU格納庫への同じ操作の適用は未確認。
- dotnet ./ProbeCheckedClick.dll（19:59:20〜19:59:28、終了0）。2解像度・両地図で緑／赤の実クリック、出撃確定・即予約→赤チェック→閉じる→ターン終了、モード分離・ターン末消去、チェック後の再クリックを確認。証拠は最終PASSと540pの画面点 (720,276)。スポットライト解除の単独描画は未確認。
- 新規採番なし。次回は通常Demoのドラッグを基準に、DEMO-0017のCPU格納庫を予約反映後・自動組立前の在庫で描画した場合の部品IDと実矩形を照合する。CPU個別ドラッグ、自然発生する軍事退避、全Seedは今回未確認。

- 20:08修正回。DEMO-0017は固定ケースの組立前在庫で通常格納庫の頭・胴・脚の矩形を照合。既存予約を保持し最終在庫一致。個別押下・解放と実演は未対応。詳細は修正記録。

### 実行履歴：2026-11-13 20:18回

- 開始20:18:30、終了20:19:37 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。既存の関係差分は Game1.ComputerDomestic.cs、ComputerJointAttacks.cs、Game1.DemoDomestic.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=39、FOLDERVERSE_AUDIT_FALLBACK=3。dotnet ./DemoAuditCpuSortie.dll（20:18:42〜20:18:52、終了0）。960×540で地点選択後の表示用候補不一致から専用表示を描画し、次の征服者19の通常出撃選択へ復帰。予約0→0。証拠は「Candidate mismatch entered stage -1 through the normal preview」「Fallback next index=1 stage=3 ruler=19 reservations=0/0」と既存 audit-war-fallback.png／audit-war-return.png。
- dotnet ./DirectDeliveryUi.dll（20:18:52〜20:18:59）、dotnet ./DemoInspectorUi.dll（20:18:59〜20:19:12）、dotnet ./ProbeCheckedClick.dll（20:19:12〜20:19:19）はすべて終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリーと工場矩形、緑／赤の実クリック、出撃確定・即予約→赤チェック→閉じる→ターン終了、表示分離・ターン末消去を確認。地図／地星儀・1920×1080／960×540で赤チェック後の再クリックが通過。証拠は各PASSと540pの画面点 (720,276)。
- 新規採番なし。自然発生する軍事退避、スポットライト解除の単独描画、全Seedは未確認。DEMO-0017は個別押下・解放と段階ごとの表示更新が未対応。次回は20:08回の組立前スロット診断から、実予約を追加しない個別操作の検証へ進む。

- 20:30修正回。DEMO-0017は隔離ケースで通常入力が胴ID3と脚ID4を掴み、画面外解放で実予約を増やさず取り消すことを確認。受け側への解放とCPU実演は未対応。詳細は修正記録。

### 実行履歴：2026-11-13 20:39回

- 開始20:39:00、終了20:40:17 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（20:39:11〜20:39:20）と dotnet ./DemoInspectorUi.dll（20:39:20〜20:39:32）は終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリーと工場の実矩形・製造開始を確認。証拠は各PASS、例としてクリック (196,332) と矩形 (160,288,72,88)。
- dotnet ./DemoActionsUi.dll（20:39:32〜20:39:52、終了0）。1920×1080／960×540で通常Demoの個別部品・部品セット・一体型の実ドロップと、和平予約と別操作のターン終了を確認。dotnet ./ProbeCheckedClick.dll（20:39:52〜20:40:00、終了0）は同解像度・地図／地星儀で実ハンドルの緑・赤、出撃確定・即予約→赤チェック→閉じる→ターン終了、モード分離・消去・赤チェック後の再クリックを確認。証拠は各PASSと内部再クリック点 (1440,552)。
- 新規採番なし。DEMO-0017は通常Demoのドロップを比較基準とし、CPU側で受け側へのドロップと中間状態を実予約から切り離して再生する経路は未対応。次回は受け側の区画と予約判定を照合。自然発生する軍事退避、スポットライト解除の単独描画、全Seedは今回未確認。

- 20:50修正回。DEMO-0017は隔離診断で通常格納庫の胴・脚を頭の区画へドロップし、中間状態の描画と診断用予約の取り消しを確認。実予約へ書き込まない連続実演は未対応。詳細は修正記録。

### 実行履歴：2026-11-13 20:59回

- 開始20:59:30、終了21:00:39 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。既存の関係差分は Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、ComputerJointAttacks.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=39、FOLDERVERSE_AUDIT_FALLBACK=3。dotnet ./DemoAuditCpuSortie.dll（20:59:43〜20:59:52、終了0）。960×540で通常の地点選択後に表示用の個体候補を不一致にし、専用表示へ退避。次の征服者19の通常出撃選択に復帰し予約0→0。証拠は「Candidate mismatch entered stage -1 through the normal preview」「Fallback next index=1 stage=3 ruler=19 reservations=0/0」。
- dotnet ./DirectDeliveryUi.dll（20:59:52〜20:59:59）、dotnet ./DemoInspectorUi.dll（20:59:59〜21:00:13）、dotnet ./ProbeCheckedClick.dll（21:00:13〜21:00:21）は全て終了0。直送便退避・復帰、左右格納庫と予約保持、プライマリー、工場の実矩形と製造開始を確認。1920×1080／960×540の地図／地星儀で緑・赤の実クリック、出撃確定・即予約→赤チェック→閉じる→ターン終了、モード分離・ターン末消去と赤チェック後の再クリックを確認。証拠は各PASS、工場クリック (196,332) と矩形 (160,288,72,88)、540p再クリック画面点 (720,276)。
- 新規採番なし。自然発生するCPU軍事退避、スポットライト解除の単独描画、全Seedは未確認。DEMO-0017は20:50回の単発ドロップを確認済み範囲とし、実予約に書き込まない頭胴→完成の連続再生を次回の続き位置とする。

- 21:12修正回。DEMO-0017は複製した予測在庫で頭胴→完成を通常格納庫に順次描画し、既存予約1件を保持。CPUの押下・解放と連動する実演は未対応。詳細は修正記録。

### 実行履歴：2026-11-13 21:21回

- 開始21:21:31、終了21:22:26 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（21:21:43〜21:21:52）、dotnet ./DemoInspectorUi.dll（21:21:52〜21:22:03）、dotnet ./ProbeCheckedClick.dll（21:22:03〜21:22:11）は全て終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー、工場計画の矩形と製造開始を確認。1920×1080／960×540の地図・地星儀で緑／赤の実クリック、出撃確定・即予約→赤チェック→閉じる→ターン終了、モード分離・消去・赤チェック後の再クリックを確認。証拠は各PASSと工場クリック (196,332)・矩形 (160,288,72,88)。
- 新規採番なし。21:12回で確認した複製在庫の頭胴→完成の描画に対し、CPUの押下・解放と連動する経路は未対応。次回は通常のドロップ判定を使い、実予約を増やさず複製在庫が更新されることを照合する。自然発生する軍事退避、スポットライト解除の単独描画、全Seedは今回未確認。

- 21:31修正回。DEMO-0017は同一ページの最初のCPU自動組立を通常格納庫で実演できるよう修正。960×540で頭胴→完成と実予約の保持を検証済み。多ページ・複数組立の実演は未対応、ドラッグ中スキップは要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 21:40回

- 開始21:40:31、終了21:42:01 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は21:31修正回の Game1.ComputerDomestic.cs、Game1.NodeAssembly.cs、Game1.PlannedHangars.cs、RobotPartPlanning.cs。変更は点検記録と既存の非追跡診断 ProbeCpuSortie.cs.txt。ゲームコード無変更。
- 既存 demo-check に dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --no-restore --disable-build-servers -p:OutputPath=既存demo-check -v quiet（警告・エラー0）。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP／DOMESTIC_ENTRY／ASSEMBLY_LIVE／ASSEMBLY_SKIP_HELD／AUTO_ENABLED=1。dotnet ./DemoAuditCpuSortie.dll（21:41:07〜21:41:15、終了0）。960×540で胴ID2を掴んだ状態（内部点474,405）を描画してからスキップ入力の分岐へ。保持部品と表示用在庫が消え、既存予約は配送時に一度だけ反映、人間に復帰。通常再生の頭胴→完成と同じ完成ID1。証拠は「Skipping held CPU part 2」と最終PASS。
- dotnet ./DirectDeliveryUi.dll（21:41:15〜21:41:21）、dotnet ./DemoInspectorUi.dll（21:41:21〜21:41:34）、dotnet ./ProbeCheckedClick.dll（21:41:34〜21:41:40）は各終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー、工場矩形を確認。1920×1080／960×540の地図／地星儀で緑赤の実クリック、出撃確定・即予約→赤チェック→閉じる→ターン終了、分離・消去・赤チェック地点の再クリックを確認。証拠は各PASS。
- 新規採番なし。DEMO-0017の掴んだ状態からのスキップは固定ケースで確認済み。ドラッグ中間位置での物理Esc、多ページ・複数組立、自然発生する軍事退避、スポットライト解除の単独描画は未確認。次回は出発側・受け側が同一ページに収まらない組立を照合する。

- 21:50修正回。DEMO-0017は同一ページで2体の組立を連続実演し、全4操作で実予約を保持。表示オフ・通常・スキップの最終在庫が一致。複数ページにまたがる実演は未対応。詳細は修正記録。

### 実行履歴：2026-11-13 21:59回

- 開始21:59:01、終了22:00:23 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は Game1.ComputerDomestic.cs、Game1.NodeAssembly.cs、Game1.PlannedHangars.cs、RobotPartPlanning.cs。変更は点検記録と既存非追跡診断。ゲームコード無変更。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --no-restore --disable-build-servers -p:OutputPath=既存demo-check -v quiet は警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP／DOMESTIC_ENTRY／AUTO_ENABLED／ASSEMBLY_PAGE_LIMIT=1 で dotnet ./DemoAuditCpuSortie.dll（21:59:37〜21:59:44、終了0）。960×540で出発側の胴を診断上スロット6へ置き、受け側を先頭ページに残した。期待は通常格納庫でページ移動とドロップを実演。実際は部品を掴まず通常格納庫の結果表示へ戻る。既存予約1件を保持し、配送で一度反映、人間へ復帰。証拠は「Off-page CPU assembly: source slot 6, result display, reservations=1」。既報DEMO-0017の未対応条件を再現し、新規採番なし。
- dotnet ./DirectDeliveryUi.dll（21:59:44〜21:59:51）と dotnet ./DemoInspectorUi.dll（21:59:51〜22:00:04）は終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリーと工場計画の実矩形・製造遷移を確認。証拠は各PASSとクリック (196,332)・矩形 (160,288,72,88)。
- 次回は通常格納庫のスクロール操作とドラッグ保持中のページ移動を照合する。自然配置の複数ページ組立、軍事の自然退避、スポットライト解除の単独描画、全Seedは今回未確認。

- 22:11修正回。DEMO-0017の別ページ組立を修正し、スロット6から先頭行へ保持スクロールしてドロップする実描画と予約保持を確認。自然配置と逆方向のスクロール中スキップは要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 22:21回

- 開始22:21:02、終了22:22:35 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了。開始時にゲーム・dotnet・MSBuildなし。関係差分は Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeAssembly.cs、Game1.PlannedHangars.cs、RobotPartPlanning.cs。変更は点検記録と既存非追跡診断。ゲームコード無変更。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --no-restore --disable-build-servers -p:OutputPath=既存demo-check -v quiet（警告・エラー0）。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP／DOMESTIC_ENTRY／AUTO_ENABLED／ASSEMBLY_PAGE_LIMIT／ASSEMBLY_REVERSE_SKIP=1。dotnet ./DemoAuditCpuSortie.dll（22:21:40〜22:21:48、終了0）。960×540で前回の上方向ドロップを再確認。逆方向は受け側をスロット6へ置き、胴を保持したまま下ボタン中心の内部点 (474,845) で次行へ移動。ドロップ前にスキップ分岐を実行し、保持・表示在庫を解除、実予約は配送で一度反映、人間へ復帰。最終完成ID1が表示オフ・通常と一致。証拠は「Skip during downward held scroll: row=1 cursor={X:474 Y:845}」と最終PASS。
- dotnet ./DirectDeliveryUi.dll（22:21:48〜22:21:55）、dotnet ./DemoInspectorUi.dll（22:21:55〜22:22:08）、dotnet ./ProbeCheckedClick.dll（22:22:08〜22:22:15）は各終了0。直送便・左右格納庫・プライマリー、工場計画の実矩形、1920×1080／960×540の地図／地星儀で緑赤とターン末消去・赤チェック後の再クリックを確認。証拠は各PASS。
- 新規採番なし。DEMO-0017の下方向スクロールからのスキップは固定ケースで確認済み。下方向のドロップ完走と自然配置、物理Esc、自然発生する軍事退避、スポットライト解除の単独描画は未確認。次回は下方向のドロップ完走を照合する。

- 22:30修正回。DEMO-0017は固定ケースで下方向の保持スクロールからドロップまで描画し、予約保持と最終在庫一致を確認。自然配置と物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 22:40回

- 開始22:40:02、終了22:41:14 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（22:40:19〜22:40:26）、dotnet ./DemoInspectorUi.dll（22:40:26〜22:40:40）は各終了0。直送便退避・復帰、左右格納庫と予約保持、プライマリーと工場計画の実矩形・製造開始を確認。クリック (196,332)・矩形 (160,288,72,88)。
- dotnet ./ProbeCheckedClick.dll（22:40:40〜22:40:47、終了0）は1920×1080／960×540の地図・地星儀で緑赤、即予約→赤チェック→閉じる→ターン終了、モード分離・消去、赤チェック後の再クリックを確認。診断の段階6は赤チェック地点が AttackHighlightTargets から除外されることも実行時に検査。証拠は各シナリオPASS、540p再クリック点 (720,276)。スポットライト解除前後のピクセル単独比較は未実施。
- dotnet ./DemoUi.dll（22:40:47〜22:40:52、終了0）はテストSeed0で待機から直送便、地図復帰、地図／地星儀、18 FPSで辺移動、拒否時分岐、終了ループとDemoラベルを確認。証拠は first transport／map follow-up と最終PASS。
- 新規採番なし。DEMO-0017の通常在庫順から複数ページにまたがるケース、物理Esc、CPU軍事の自然退避、全Seedは未確認。次回はスロットを入れ替えず、部品と既存在庫の追加順でCPU格納庫を複数ページにする。

- 22:52修正回。DEMO-0017は頭と胴の間に完成品5体がある固定在庫で通常のスロット割付を使用し、ページをまたぐ胴のドロップと予約保持を確認。自然プレイと2回目のドロップの個別描画は要再確認。詳細は修正記録。

- 23:00修正回。DEMO-0017は通常割付の固定在庫で胴・脚の両ドロップを描画し、頭胴→完成、既存予約と最終在庫一致を確認。自然プレイと物理Escは要再確認。詳細は修正記録。

- 23:09修正回。DEMO-0017は下方向の保持スクロール中に通常入力経由でEscapeを渡し、保持解除・予約の一度だけの反映・人間復帰を確認。自然プレイとOSの物理キー操作は要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 23:18回

- 開始23:18:33、終了23:19:43 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeAssembly.cs、RobotPartPlanning.cs、Game1.DemoDomestic.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=42。dotnet ./DemoAuditCpuSortie.dll（23:18:45〜23:18:57、終了0）。960×540でCPU征服者9の視点、地点20の通常出撃選択、個体4体のクリック、確定後の段階5と赤チェック所有者9を確認。表示18件・和平0件。自然退避0件で人間へ復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=9」「No-peaceful batch fallbacks=0」と最終PASS。
- dotnet ./DirectDeliveryUi.dll（23:18:57〜23:19:04）、dotnet ./DemoInspectorUi.dll（23:19:04〜23:19:16）、dotnet ./ProbeCheckedClick.dll（23:19:16〜23:19:24）は各終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー、工場の矩形内クリックと製造開始。1920×1080／960×540の地図・地星儀で緑赤の実クリック、即予約→赤チェック→閉じる→ターン終了、モード分離、スポットライト対象からの除外、再クリックと消去を確認。証拠は各PASSと540pの再クリック画面点 (720,276)。
- 新規採番なし。CPU内政の自然在庫による組立を今回は確認できていない。次回はCPUが部品を生産した配置から通常の内政再生に入り、組立の部品IDと入力を照合する。物理Esc、自然発生の軍事退避、スポットライト解除のピクセル単独比較、全Seedは未確認。

- 23:28修正回。DEMO-0017はCPU地点の工場で生産した頭・胴・脚を通常内政で組み立て、実描画と予約保持を確認。計画は診断で設定し、CPU自身が計画を選ぶ自然プレイは要再確認。詳細は修正記録。

### 実行履歴：2026-11-13 23:38回

- 開始23:38:33、終了23:39:35 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeAssembly.cs、RobotPartPlanning.cs、Game1.DemoDomestic.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（23:38:45〜23:38:54）、dotnet ./DemoInspectorUi.dll（23:38:54〜23:39:06）、dotnet ./ProbeCheckedClick.dll（23:39:06〜23:39:15）は各終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリーと工場実矩形・製造開始を確認。地図・地星儀の1920×1080／960×540で緑赤、即予約→赤チェック→閉じる→ターン終了、表示分離、スポットライト対象からの除外、再クリックと消去を確認。証拠は各PASS、540pの再クリック画面点 (720,276)。
- DEMO-0017の自動工場計画の入口を RobotWorld.Automation.cs で静的照合。PrepareDomestic は AdjustNode を呼び、手動以外の方針では格納済み・入荷予約済みの自国部品数を比較して不足を補う計画を選ぶ。次回はこの入口と AdvanceIndustry を通し、組立候補が出た地点の通常格納庫で部品IDとドロップを照合する。前回の診断による工場計画設定からの生産結果は維持。
- 新規採番なし。自動で選んだ計画からの生産・CPU実演、物理Esc、自然発生する軍事退避、スポットライト解除の単独ピクセル比較、全Seedは今回未確認。

- 23:48修正回。DEMO-0017は自動選択した工場計画から入荷候補を確認。組立実演は診断の単独3部品という前提が成立せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 00:02回

- 開始00:02:03、終了00:03:42 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeAssembly.cs、RobotPartPlanning.cs、Game1.DemoDomestic.cs、DemoInspectorUi、DemoChecksUi。点検記録と既存非追跡診断 ProbeCpuSortie.cs.txt を更新。ゲームコード無変更。
- 既存 demo-check に dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --no-restore --disable-build-servers -p:OutputPath=既存demo-check -v quiet（警告・エラー0）。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP／DOMESTIC_ENTRY／ASSEMBLY_LIVE／ASSEMBLY_AUTO_PLAN／AUTO_ENABLED=1 で dotnet ./DemoAuditCpuSortie.dll（00:02:49〜00:02:54、終了0）。DEMO-0017の診断を単独3部品の条件から通常の組立予測に変更。地点91の自動工場は120ステップで頭胴・胴脚の複合部品を各5体入荷予約し、同じ胴が重複するため完成候補は生じなかった。証拠は「No legal auto-factory recipe in 120 steps」。この試行でCPU組立の描画・入力は未確認。
- dotnet ./DirectDeliveryUi.dll（00:02:54〜00:03:01）、dotnet ./DemoInspectorUi.dll（00:03:01〜00:03:12）、dotnet ./ProbeCheckedClick.dll（00:03:12〜00:03:21）は各終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー、工場の実矩形と製造遷移を確認。1920×1080／960×540の地図・地星儀で緑赤、即予約→赤チェック→閉じる→ターン終了、モード分離、スポットライト対象からの除外、再クリックと消去を確認。証拠は各PASS、工場点 (196,332)・矩形 (160,288,72,88)、540p再クリック点 (720,276)。
- 新規採番なし。自動工場由来の組立は、次回は異なる地点・工場ライン構成で、部位が重ならない候補が生じる配置から通常格納庫を照合する。物理Esc、自然発生の軍事退避、スポットライト解除のピクセル単独比較、全Seedは未確認。

- 00:11修正回。DEMO-0017は既存の脚とCPU自動工場が生産した頭胴を通常格納庫で組み立て、完成描画・予約保持・人間復帰を確認。初期在庫を指定しない自然プレイは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 00:19回

- 開始00:19:34、終了00:20:42 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeAssembly.cs、RobotPartPlanning.cs、Game1.DemoDomestic.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=43。dotnet ./DemoAuditCpuSortie.dll（00:19:46〜00:19:56、終了0）。960×540でCPU征服者15の視点、地点73の通常出撃選択、個体4体のクリック、確定後の段階5・赤チェック所有者15。19指示・和平なしの指示列を退避0件で終え、人間に復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=15」「No-peaceful batch fallbacks=0」と最終PASS。
- dotnet ./DirectDeliveryUi.dll（00:19:56〜00:20:04）、dotnet ./DemoInspectorUi.dll（00:20:04〜00:20:15）、dotnet ./ProbeCheckedClick.dll（00:20:15〜00:20:24）は各終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー・工場実矩形。1920×1080／960×540の地図・地星儀で緑赤の実クリック、即予約→赤チェック→閉じる→ターン終了、表示分離、スポットライト対象からの除外、再クリックと消去を確認。証拠は各PASSと540pの再クリック画面点 (720,276)。
- 新規採番なし。DEMO-0017の初期在庫を指定しない組立、物理Esc、軍事の自然退避、スポットライトの単独ピクセル比較、全Seedは今回未確認。次回はCPUの初期在庫と自動工場計画が組立可能な組み合わせになる配置を探し、格納庫を照合する。

- 00:30修正回。DEMO-0017は配置43〜45の空在庫・単独工場で合法な組立候補が発生せず、実演は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 00:40回

- 開始00:40:04、終了00:41:05 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeAssembly.cs、RobotPartPlanning.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null。dotnet ./DirectDeliveryUi.dll（00:40:16〜00:40:24）と dotnet ./DemoInspectorUi.dll（00:40:24〜00:40:36）は終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリー、工場計画の矩形内クリックと製造開始を確認。例としてクリック (196,332) と矩形 (160,288,72,88)。証拠は各PASS。
- dotnet ./ProbeCheckedClick.dll（00:40:36〜00:40:44、終了0）は1920×1080／960×540の地図・地星儀で緑赤の実クリック、即予約→赤チェック→閉じる→ターン終了、表示分離、スポットライト対象からの除外、赤チェック後の再クリック、ターン末消去を確認。dotnet ./DemoUi.dll（00:40:44〜00:40:48、終了0）はテストSeed0で直送便から地図復帰、地図・地星儀の切替、18 FPSで辺移動、拒否時分岐、終了ループとラベル描画を確認。
- 新規採番なし。CPU軍事の自然退避、複数工場・通常初期在庫でのCPU組立、物理Esc、全Seedは未確認。次回はDEMO-0017の複数工場条件で部品が同じ格納庫に揃うケースを照合する。

- 00:49修正回。DEMO-0017は複数工場の入荷予定から通常格納庫で完成を描画し、予約保持・人間復帰を確認。脚の製造と入荷先は診断で設定したため、自然プレイは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 00:59回

- 開始00:59:04、終了01:00:14 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeAssembly.cs、RobotPartPlanning.cs、DemoInspectorUi、DemoChecksUi。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=39、FOLDERVERSE_AUDIT_FALLBACK=3。dotnet ./DemoAuditCpuSortie.dll（00:59:19〜00:59:29、終了0）。960×540で通常の地点クリック後に候補不一致を発生させ、専用表示を描画。次の征服者19の通常出撃選択（段階3）へ復帰し予約0→0。証拠は「Candidate mismatch entered stage -1 through the normal preview」「Fallback next index=1 stage=3 ruler=19 reservations=0/0」。自然発生条件は未確認。
- dotnet ./DirectDeliveryUi.dll（00:59:29〜00:59:35）、dotnet ./DemoInspectorUi.dll（00:59:35〜00:59:48）、dotnet ./ProbeCheckedClick.dll（00:59:48〜00:59:55）は各終了0。直送便の退避・復帰と左右格納庫・予約保持、プライマリー、工場計画の実矩形と製造開始。地図／地星儀の1920×1080／960×540で緑赤の実クリック、即予約→赤チェック→閉じる→ターン終了、表示分離、スポットライト対象からの除外、赤チェック後の再クリックと消去を確認。証拠は各PASSとクリック (196,332)・矩形 (160,288,72,88)。
- 新規採番なし。DEMO-0017は00:49回の制御した複数工場で確認済み。CPUが両工場の計画・入荷先を選ぶ自然プレイ、物理Esc、全Seedは今回未確認。次回は実設定で既存の入荷先を持つCPU拠点から自然な組立候補を照合する。

- 01:11修正回。DEMO-0017は設定済み入荷先への部品で組立・描画・予約保持を再確認。CPU工場自動化は既存の入荷先を参照する。自然な既存在庫での組立と物理Escは要再確認。詳細は修正記録。
### 実行履歴：2026-11-14 01:19回

- 開始01:19:35、終了01:20:59 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeAssembly.cs、RobotPartPlanning.cs と関連診断。点検記録のみ更新。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=44。dotnet ./DemoAuditCpuSortie.dll（01:20:01〜01:20:13、終了0）。軍事再生の診断が通過し、960×540の専用表示で和平指示の征服者55・地点73と反映後の地点73が一致。証拠は「Dedicated peaceful preview 960x540」「Peaceful ruler=55 expected=73 current=73」と最終PASS。和平の専用表示は既報。
- dotnet ./DirectDeliveryUi.dll（01:20:13〜01:20:19）、dotnet ./DemoInspectorUi.dll（01:20:19〜01:20:33）、dotnet ./ProbeCheckedClick.dll（01:20:33〜01:20:40）は各終了0。直送便の退避・復帰、左右格納庫と予約保持、プライマリーと工場製造への遷移を確認。工場のクリック (196,332) は矩形 (160,288,72,88) 内。1920×1080／960×540の地図・地星儀で緑赤の実クリック、即予約→赤チェック→閉じる→ターン終了、表示分離、赤チェック地点の再クリックとターン終了時の消去を確認。証拠は各PASSと540pの再クリック画面点 (720,276)。
- 新規採番なし。自然な既存在庫でのCPU組立、軍事の候補不一致による自然な退避、物理Esc、スポットライトの単独ピクセル比較、全Seedは今回未確認。次回は既存の入荷先を持つCPU拠点で、自然に合法な組立候補が揃う経路から再開する。

- 01:30修正回（終了01:30:59 JST）。DEMO-0017は配置49で、空在庫から単独CPU工場が自動で生産した部品による通常格納庫の組立・描画、予約保持、表示オフ・スキップとの結果一致を確認。通常プレイ全体の自然発生と物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 01:39回

- 開始01:39:35、終了01:40:51 JST。部分点検完了。HEAD=9f134f8847be7c0e365ee4e98632ac8622b27640。先行修正完了、開始時にゲーム・dotnet・MSBuildなし。関係差分は既存の Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、Game1.NodeAssembly.cs、RobotPartPlanning.cs と関連診断。変更は点検記録のみ。
- 既存 demo-check で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=45。dotnet ./DemoAuditCpuSortie.dll（01:39:49〜01:39:59、終了0）。960×540で通常の出撃選択の個体クリック2回、確定後の段階5と赤チェック所有者3を確認。和平は既報の専用表示で征服者17・目標74、反映先74。証拠は「Individual robot clicks: 2」「after confirm: stage=5 red-owner=3」「Peaceful ruler=17 expected=74 current=74」と最終PASS。
- dotnet ./DirectDeliveryUi.dll（01:39:59〜01:40:08）、dotnet ./DemoInspectorUi.dll（01:40:08〜01:40:19）、dotnet ./ProbeCheckedClick.dll（01:40:19〜01:40:27）は各終了0。直送便の退避・復帰、左右格納庫、予約保持、プライマリー、工場6プランの実矩形と製造への遷移を確認。例はクリック (292,332) と矩形 (256,288,72,88)。1920×1080／960×540の地図・地星儀で緑赤の実クリック、即予約→赤チェック→閉じる→ターン終了、表示分離、赤チェック地点の再クリックと消去を確認。証拠は各PASSと再クリック画面点 (1440,552)／(720,276)。
- dotnet ./DemoUi.dll（01:40:27〜01:40:32、終了0）。テストSeed0で直送便から地図に復帰し、地図・地星儀の切替、18 FPSの辺移動、拒否時の分岐と終了ループを確認。証拠は「first transport」「map follow-up」と最終PASS。
- 新規採番なし。次回は配置49で確認済みのCPU組立を通常のターン進行から照合する。軍事の自然退避、物理Esc、スポットライトの単独ピクセル比較、全Seedは未確認。

- 01:51修正回（終了01:52:13 JST）。DEMO-0017は配置49の自動生産から格納庫の描画と通常入力経由のEscapeによるドラッグ解除まで確認。通常ターンの連続進行と物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 01:59回

- 開始01:59:05、終了02:05:37 JST。マージ後の部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。開始時に競合・ゲームプロセス・未コミット差分なし。比較元205a1940から Game1.DemoDomestic.cs、Game1.ComputerDomestic.cs、Game1.FunctionPages.cs、Game1.NodeFactory.cs が変更。点検記録のみ追記。
- NASの tmp/delete-after-2026-11-16/demo-audit を再生成可能な出力先として使用。dotnet build tests/<対象>/<対象>.csproj --artifacts-path <同出力先> -v quiet で DemoInspectorUi、DirectDeliveryUi、DemoChecksUi は警告・エラー0。ALSOFT_DRIVERS=null、各DLLの出力先で dotnet <対象DLL> を順次実行。
- DemoInspectorUi（02:01:01〜02:01:37、終了0）は1080p／540pで工場6プランの実矩形と製造遷移、プライマリー、閉じる描画を確認。例は論理クリック (268,332) と矩形 (232,288,72,88)。DirectDeliveryUi（02:01:37〜02:02:04、終了0）は540pで直送便の退避・復帰、左右格納庫、予約保持と描画を確認。証拠は各PASS。
- DemoChecksUi（02:02:04〜02:02:26、終了0）は1080p／540pの地図・地星儀でハンドルの緑赤クリック、即予約→赤チェック→閉じる→ターン終了、表示分離、赤チェック地点がスポットライト対象から外れることとターン末消去を確認。証拠は4条件のPASS。
- DemoUiのNASビルドは02:01:52開始後に進行出力がなく、02:05:18に今回のビルドプロセスのみ終了。巡回は未実施。新規採番なし。次回は同じNAS出力先でDemoUiのビルド段階を切り分け、マージ後のCPU軍事・格納庫の縦横移動とドラッグ中スキップ、赤チェック後の地点再クリックを照合する。

- 02:08修正回はNAS確認待ちで検証を中断。DEMO-0017のマージ後の格納庫再生は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 02:19回

- 開始02:19:05、終了02:21:40 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。関係する未コミット差分は点検・修正記録のみ。先行修正は中断済み。ゲームコード無変更。
- NASは通常・権限付きの確認とも応答待ち。Cドライブの空き約200 GBを確認し、必要なビルドを artifacts/tmp/delete-after-2026-11-16/demo-audit-local に出力。接続復旧後に参照とハッシュを確認して移す。既存資料は保持。
- 通常のNuGet復元はNU1801／NU1101で終了1。既存キャッシュを使った dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path artifacts/tmp/delete-after-2026-11-16/demo-audit-local --disable-build-servers -p:NuGetAudit=false --source artifacts/tmp/delete-after-2026-11-13/demo-check/.nuget/packages -v quiet は警告・エラー0。
- ALSOFT_DRIVERS=null で dotnet DemoUi.dll。マージ後のSeed0で直送便→地図復帰（論理クリック (256,998)）、地図・地星儀の切替、18 FPSの辺移動、拒否時分岐、終了ループとDemoラベルの描画を確認。証拠は first transport、map follow-up、最終PASS。画面解像度は今回の出力から未確認。
- 新規採番なし。プライマリー・左右格納庫・緑赤の詳細は同じHEADでの01:59回を参照。次回はCPU軍事の通常・専用表示、格納庫の縦横移動とドラッグ中スキップ、赤チェック後の再クリックを照合する。

- 02:30修正回。DEMO-0017はマージ後の格納庫導入アニメーションとCPU押下の競合を修正。画面外からのドロップ2回と保持中Escapeを960×540で検証済み。通常プレイ全体での自然発生と物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 02:40回

- 開始02:40:36、終了02:41:35 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係するゲーム差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。今回の変更は既存診断 ProbeCpuSortie.cs.txt と点検記録。
- 初回診断はゲームの更新を直接呼び、描画側のアニメーション時間が停止したまま格納庫導入待ちに達した。診断の時間を更新間隔と同じだけ進めて再実行。既存 demo-audit-local で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path artifacts/tmp/delete-after-2026-11-16/demo-audit-local --disable-build-servers --no-restore -v quiet は警告・エラー0。
- ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=45 で dotnet DemoAuditCpuSortie.dll は終了0。CPU内政で征服者の画面切替→地点→格納庫→緑チェック→閉じると人間への復帰を確認。軍事は征服者15の地点72で個体4回のクリックと確定後の段階5・赤チェックを確認。征服者17の和平は既報の960×540専用表示で目標74と反映先74が一致。証拠は段階ログ、「Individual robot clicks: 4」「Peaceful ruler=17 expected=74 current=74」と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫の実描画は01:59回の記録を参照し、今回の再実行範囲には含めない。次回は赤チェック後の地点再クリック、CPU軍事の自然退避、物理Escを照合する。

- 02:49修正回。DEMO-0017の導入待ち修正後、配置49でCPU工場の自動生産から通常格納庫の組み立てまで再確認。通常プレイ全体と物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 02:58回

- 開始02:58:06、終了02:58:29 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関連差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 既存の期限付き出力 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=39、FOLDERVERSE_AUDIT_FALLBACK=3 により dotnet DemoAuditCpuSortie.dll を実行し、終了0。960×540のCPU軍事で、通常の地点選択後に候補不一致を注入。専用表示の段階−1から次の征服者19の通常出撃選択の段階3に復帰し、予約件数は0→0。証拠は「Candidate mismatch entered stage -1 through the normal preview」「Fallback next index=1 stage=3 ruler=19 reservations=0/0」。
- 新規採番なし。候補不一致の自然発生、赤チェック後の再クリックと物理Escは今回未確認。プライマリー・直送便・左右格納庫は01:59回の実描画を参照。次回は赤チェック後の地点再クリックから再開する。

- 03:10修正回。DEMO-0016は配置39で候補不一致を注入しない場合、通常出撃から人間復帰まで完了。自然な候補不一致は再現せず要再確認。和平の専用表示は既報のまま。詳細は修正記録。

### 実行履歴：2026-11-14 03:20回

- 開始03:20:06、終了03:21:15 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 期限付き出力 demo-audit-local と既存NuGetキャッシュを再利用。dotnet build tests/DemoChecksUi/DemoChecksUi.csproj と dotnet build artifacts/demo-click-audit/ProbeCheckedClick.csproj に同じ --artifacts-path、--disable-build-servers、-p:NuGetAudit=false、--source を指定し、両方とも警告・エラー0。ALSOFT_DRIVERS=null で各DLLを実行し終了0。
- 1920×1080／960×540の地図と地星儀で緑赤のハンドルクリック、即予約→赤チェック→閉じる→ターン終了、表示分離、スポットライト対象からの除外とターン末消去を確認。赤チェック済みの地点9を画面点 (1440,552)／(720,276) で再クリックできた。証拠は4条件の「red checked target re-click」と各最終PASS。
- 新規採番なし。直送便・プライマリー・左右格納庫の詳細は01:59回、CPU軍事は03:10回を参照。自然な軍事退避と物理Escは今回未確認。次回は配置を変えた軍事指示列の退避条件から再開する。

- 03:29修正回。DEMO-0016は配置43の通常出撃から人間復帰まで確認。自然な候補不一致は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 03:41回

- 開始03:41:37、終了03:42:03 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 期限付き出力 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=44 により dotnet DemoAuditCpuSortie.dll を実行し、終了0。征服者13の地点91で通常出撃選択から個体4体をクリックし、確定後の段階5と赤チェックを確認。続く征服者55の女の子・ポケットのみの和平指示は、960×540の既報の専用表示を経て、目標73と反映先73が一致。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=13」「Peaceful ruler=55 expected=73 current=73」と最終PASS。
- 新規採番なし。直送便・左右格納庫・プライマリーは01:59回、赤チェック後の再クリックは03:20回を参照。今回の軍事指示列では候補不一致による自然退避を確認せず。次回は和平以外の退避条件と格納庫ドラッグ中の物理Escを照合する。

- 03:48修正回。DEMO-0016は配置46の通常出撃から人間復帰まで確認し、自然な候補不一致は再現せず。03:41回の和平は診断コードを再照合すると通常移動UIで、「Dedicated peaceful preview」はキャプチャのログ名。和平の実際の退避条件は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 04:00回

- 開始04:00:07、終了04:00:35 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 期限付き既存出力 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=47 を指定し dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540で征服者15の地点109への通常出撃選択、個体4体のクリックと確定後の段階5・赤チェックを確認。征服者19の和平は通常移動UIで地点219を選び、確定で重複予約を作らず、反映先219と人間への復帰を確認。証拠は「Actual peaceful order ruler=19 target=219」「Peaceful ruler=19 expected=219 current=219」と最終PASS。
- 新規採番なし。自然な専用表示への退避は今回の診断ログに出ず、発生条件は未確認。直送便・左右格納庫・プライマリーは01:59回、赤チェック後の再クリックは03:20回を参照。次回は格納庫ドラッグ中の物理Escと別配置の退避条件から再開する。

- 04:11修正回。DEMO-0016は配置48の通常出撃から人間復帰まで確認。自然な候補不一致は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 04:20回

- 開始04:20:07、終了04:20:33 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=49 により dotnet DemoAuditCpuSortie.dll を実行、終了0。CPU内政の征服者切替・地点選択・通常格納庫を経て軍事へ進み、征服者1の地点97の通常出撃選択で個体2体をクリック。確定後の赤チェック、和平なし16指示の描画と人間への復帰を確認。証拠は「Individual robot clicks: 2」「after confirm: stage=5 red-owner=1」「No-peaceful batch fallbacks=0」と最終PASS。画面は診断設定の960×540。
- 新規採番なし。自然退避はこの指示列では未発生。物理Escは未実施。プライマリー・直送便・左右格納庫は01:59回、赤チェック後の再クリックは03:20回を参照。次回は物理EscとCPUが実際に専用表示へ退避する条件を照合する。

- 04:28修正回。DEMO-0016は配置50の通常出撃から人間復帰まで確認。自然な候補不一致は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 04:40回

- 開始04:40:08、終了04:40:31 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 期限付き既存出力 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=51 により dotnet DemoAuditCpuSortie.dll を実行し、終了0。960×540でCPUの視点切替・格納庫を経て、征服者7が地点32を選択し通常出撃で個体4体をクリック、確定後に赤チェック。和平なしの4指示を描画し、人間に復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=7」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。直送便・左右格納庫・プライマリーは01:59回、赤チェック後の再クリックは03:20回を参照。自然な軍事退避と物理Escは未確認。次回は格納庫ドラッグ中のスキップを再照合し、物理Escと自然退避を区別して記録する。

- 04:52修正回。DEMO-0017は画面外の胴・脚を組み立て、保持中の通常入力経由Escapeと人間復帰を再確認。物理Escと通常プレイ全体は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 04:58回

- 開始04:58:38、終了04:59:02 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP／DOMESTIC_ENTRY／AUTO_ENABLED／ASSEMBLY_LIVE／ASSEMBLY_INVENTORY_PAGE／ASSEMBLY_SKIP_HELD=1 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540で通常格納庫をスクロールし、胴と脚を順に組み立てて既存予約1件を保持。別試行では部品ID7を保持した状態でスキップし、完成6体の在庫が表示オフ・通常再生・早期スキップと一致し、人間へ復帰。証拠は「Skipping held CPU part 7 at {X:738 Y:675}」と最終PASS。
- 新規採番なし。物理Escと軍事の自然退避は未確認。プライマリー・直送便・左右格納庫は01:59回、緑赤と再クリックは03:20回、軍事は04:40回を参照。次回は物理Escまたは自然な軍事退避を再現できる条件を確認する。

- 05:09修正回。DEMO-0016は配置52の通常出撃から人間復帰まで確認。候補不一致は自然発生せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 05:20回

- 開始05:20:38、終了05:21:04 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係する既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=53 により dotnet DemoAuditCpuSortie.dll を実行し、終了0。960×540でCPU内政の視点切替、地点選択、格納庫、緑チェック、閉じるを経て軍事へ遷移。征服者17が地点202の通常出撃選択で3体をクリックし、確定後に赤チェック。和平なし15指示を退避0件で完了して人間に復帰。証拠は「Individual robot clicks: 3」「after confirm: stage=5 red-owner=17」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は01:59回、緑赤と地点の再クリックは03:20回の実描画を参照。物理Escと自然退避は今回未確認。次回は再現条件を限定して格納庫の物理Escか専用表示への退避を照合する。

- 05:30修正回。DEMO-0016は配置54の通常出撃から人間復帰まで確認。候補不一致は自然発生せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 05:39回

- 開始05:39:39、終了05:40:04 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=55 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540で征服者切替・通常格納庫を経て、征服者7が地点26の通常出撃選択で個体4体をクリック、確定後に赤チェック。和平なし12指示を退避0件で描画し、人間に復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=7」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は01:59回、緑赤と再クリックは03:20回を参照。物理Escと自然退避は今回未確認。次回は通常出撃の配置追加より、実キーボード入力または退避条件の照合を優先する。

- 05:52修正回。DEMO-0016は配置56で通常出撃と通常UIでの和平、予約件数と人間復帰を確認。自然退避は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 06:02回

- 開始06:02:09、終了06:03:18 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 期限付き既存出力 demo-audit-local と既存NuGetキャッシュを再利用。dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj と dotnet build tests/DirectDeliveryUi/DirectDeliveryUi.csproj に --artifacts-path artifacts/tmp/delete-after-2026-11-16/demo-audit-local、--disable-build-servers、-p:NuGetAudit=false、--source artifacts/tmp/delete-after-2026-11-13/demo-check/.nuget/packages、-v quiet を指定。両方とも警告・エラー0。ALSOFT_DRIVERS=null で各DLLを実行し終了0。
- DemoInspectorUiは1080p／540pでプライマリー、工場6プラン、インスペクター閉じる描画と製造遷移を確認。例は論理クリック (220,332) と矩形 (184,288,72,88)、製造開始後 manufacturing=True。DirectDeliveryUiは540pで直送便の退避・復帰、地図／地星儀の連続トラッククリック、移動中の入力抑止、左右格納庫と予約保持を確認。証拠は各PASS。
- 新規採番なし。緑赤と再クリックは03:20回、CPU軍事と和平は05:52回、保持中のスキップは04:58回を参照。今回のEscapeは診断入力。次回は物理Escか専用表示への自然退避を再現できる条件を確認する。

- 06:09修正回。DEMO-0018は軍事フェーズ演出中の通常入力経由Escapeで人間の内政へ復帰することを再確認。物理Escとスキップなしの開始時刻は今回未確認。詳細は修正記録。

### 実行履歴：2026-11-14 06:21回

- 開始06:21:09、終了06:21:35 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=56、FOLDERVERSE_AUDIT_PHASE_TIMING=1 により dotnet DemoAuditCpuSortie.dll を実行、終了0。DEMO-0018のスキップなし条件を960×540で確認。1.8秒の演出後、2.817秒で開戦プライマリーを論理点 (410,1022) で操作し、3.833秒で論理点 (1271,552) の地点74を選択。証拠は段階1・2のログと既存の audit-cpu-phase-entry.png。演出中の先行操作は再現せず。
- 新規採番なし。今回の診断は地点選択まで。直送便・プライマリー・左右格納庫は06:02回、出撃確定以降と和平は05:52回、緑赤と再クリックは03:20回を参照。次回は物理Escと自然な軍事退避を照合する。

- 06:30修正回。DEMO-0018は配置47でも軍事演出終了後に開戦ボタンと地点を操作。演出中の先行操作は再現せず。物理Escと他の演出時間・実FPS条件は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 06:41回

- 開始06:41:40、終了06:42:05 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 期限付き既存出力 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=57 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540で征服者7が地点150の通常出撃選択で個体2体をクリックし、確定後に赤チェック。13件の軍事指示を経て、征服者57が通常移動UIで地点103を選び、確定時の予約数を維持。反映先103と人間への復帰を確認。証拠は「Individual robot clicks: 2」「after confirm: stage=5 red-owner=7」「Peaceful ruler=57 expected=103 current=103」と最終PASS。
- 新規採番なし。自然退避は今回発生せず、物理Escは未実施。直送便・左右格納庫・プライマリーは06:02回、緑赤と再クリックは03:20回を参照。次回は通常移動に入れない条件か実キーボード入力を検証する。

- 06:51修正回。DEMO-0016は配置58で通常出撃から人間復帰まで確認。候補不一致の自然発生は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 07:00回

- 開始07:00:10、終了07:00:36 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 期限付き既存出力 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=59 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540でCPUの視点切替と通常格納庫を経て、征服者15が地点68の通常出撃選択で4体をクリック。確定後に赤チェックを付け、和平なしの12指示を退避0件で描画し、人間へ復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=15」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は06:02回、緑赤と再クリックは03:20回を参照。自然退避と物理Escは今回未確認。次回は自然退避の候補条件か格納庫の実キーボード入力を照合する。

- 07:09修正回。DEMO-0016は配置60で通常出撃と通常UIの和平を経て人間に復帰。自然な候補不一致は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 07:22回

- 開始07:22:10、終了07:22:36 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 期限付き既存出力 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=61 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540でCPU内政の視点切替と通常格納庫、軍事で征服者1が地点128を選び通常出撃の個体4体をクリック、確定後に赤チェック。23指示中3件が和平。最初の和平は征服者3が通常移動UIで地点73を選び、確定時の予約件数を維持して実行後に地点73に到着。証拠は「after confirm: stage=5 red-owner=1」「Peaceful ruler=3 expected=73 current=73」と人間復帰を含む最終PASS。
- 新規採番なし。自然退避はログに出ず発生条件は未確認。物理Escも未実施。直送便・左右格納庫・プライマリーは06:02回、緑赤と再クリックは03:20回を参照。次回は物理Escと専用表示への退避条件を照合する。

- 07:32修正回。DEMO-0016の診断で先頭指示が和平の場合の誤判定を修正。配置62で通常移動UIの地点選択、重複予約のない確定、到着と人間復帰を確認。ゲームコード変更なし。詳細は修正記録。

### 実行履歴：2026-11-14 07:40回

- 開始07:40:11、終了07:40:37 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=63 により dotnet DemoAuditCpuSortie.dll を実行、終了0。07:32の診断変更後も、先頭が出撃の場合に960×540で征服者23が地点77を選び、通常の出撃選択で個体2体をクリックし、確定後に赤チェックを付けることを確認。和平なし10指示を退避0件で描画し、人間に復帰。証拠は「Individual robot clicks: 2」「after confirm: stage=5 red-owner=23」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。自然な退避と物理Escは未確認。プライマリー・直送便・左右格納庫は06:02回、緑赤と再クリックは03:20回を参照。次回は実キーボード入力と自然退避の条件を照合する。

- 07:51修正回。DEMO-0016は配置64で同一征服者の出撃から和平への切替と人間復帰を確認。自然な退避は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 08:00回

- 開始08:00:11、終了08:00:29 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null により dotnet ProbeCheckedClick.dll を実行、終了0。地図・地星儀の1080p／540pで緑赤の実クリック、即予約→赤チェック→閉じる→ターン終了、表示分離とターン末消去を確認。赤チェック済みの地点9は画面点 (1440,552)／(720,276) で再クリックできた。証拠は「red checked target re-click」4条件と最終PASS。
- 新規採番なし。直送便・左右格納庫・プライマリーは06:02回、CPUの通常出撃から和平への切替は07:51回を参照。自然退避と物理Escは今回未確認。次回はCPU軍事で退避が起きる指示条件か、格納庫ドラッグ中の実キーボード入力を照合する。

- 08:09修正回。DEMO-0016は配置64の出撃確定直後から通常再生とEscapeによるスキップを比較し、進軍・取消・戦闘参加個体・人間復帰が一致。物理Escと自然退避は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 08:21回

- 開始08:21:41、終了08:22:08 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=65 により dotnet DemoAuditCpuSortie.dll を実行し終了0。960×540で征服者7が地点152の通常出撃選択から4体をクリック、確定後に赤チェック。次の同じ征服者の和平は通常移動UIで地点161を選び、重複予約なしで到着。22指示の再生後に人間へ復帰。証拠は「Individual robot clicks: 4」「Peaceful ruler=7 expected=161 current=161」と最終PASS。
- 新規採番なし。自然な専用表示への退避と物理Escは未確認。プライマリー・直送便・左右格納庫は06:02回、緑赤と再クリックは08:00回を参照。次回は物理Escか退避発生条件を確認する。

- 08:29修正回。DEMO-0016は配置66で通常出撃から人間復帰まで確認。自然な候補不一致は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 08:39回

- 開始08:39:42、終了08:40:03 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=67 により dotnet DemoAuditCpuSortie.dll を実行し、終了0。960×540でCPU内政の地点・格納庫から軍事へ移行し、征服者7が地点46を選択。通常の出撃選択で4体をクリックし、確定後の赤チェックを確認。和平なし8指示を退避0件で描画し、人間へ復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=7」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は06:02回、緑赤と再クリックは08:00回を参照。自然退避と物理Escは未確認。次回は物理Escか候補不一致の発生条件を照合する。

- 08:51修正回。DEMO-0016は配置68で通常出撃と地点ID 0への和平移動を確認。自然な候補不一致は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 09:01回

- 開始09:01:12、終了09:01:54 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。開始時にゲーム・同一診断のプロセスなし。関係差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=69 により dotnet DemoAuditCpuSortie.dll を実行し、終了0。960×540で征服者19が地点92の通常出撃選択で個体4体をクリックし、確定後に赤チェック。和平なし13指示を退避0件で描画し、人間へ復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=19」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。直送便・左右格納庫・プライマリーは06:02回、緑赤と再クリックは08:00回を参照。自然退避と物理Escは未確認。次回は退避の発生条件を限定した診断か、格納庫ドラッグ中の実キーボード入力を照合する。

- 09:08修正回。DEMO-0016は配置70で通常出撃から人間復帰まで確認。候補不一致は自然発生せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 09:18回

- 開始09:18:42、終了09:19:24 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null により dotnet DirectDeliveryUi.dll と dotnet DemoInspectorUi.dll を順次実行し、各終了0。540pで直送便の退避・復帰、左右格納庫、地図／地星儀の連続クリック、移動中の入力抑止と予約保持を確認。1080p／540pでプライマリー、工場6プラン、閉じる描画と製造遷移を確認。例は論理クリック (268,332) と矩形 (232,288,72,88)、クリック後 manufacturing=True。証拠は各PASS。
- 新規採番なし。緑赤と再クリックは08:00回、CPU軍事は09:08回を参照。物理Escと専用表示への自然退避は今回未確認。次回はこの2条件の確認を優先する。

- 09:30修正回。DEMO-0016は配置71で通常出撃と和平移動を確認。自然な退避は再現せず、物理Escとともに要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 09:41回

- 開始09:41:13、終了09:41:38 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=72 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540でCPU内政の地点選択と格納庫を経て、征服者17が地点182の通常出撃選択で個体3体をクリック。確定後に赤チェック、和平なし17指示を退避0件で描画、人間へ復帰。証拠は「Individual robot clicks: 3」「after confirm: stage=5 red-owner=17」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。直送便・左右格納庫・プライマリーは09:18回、緑赤と再クリックは08:00回を参照。自然退避と物理Escは今回未確認。次回はこれらの再現条件の確認を優先する。

- 09:50修正回。DEMO-0016は配置73で通常出撃と和平移動、予約件数の維持と人間復帰を確認。自然な退避は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 09:59回

- 開始09:59:43、終了10:00:08 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。関連差分は既存の Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=74 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540で征服者1が地点142の通常出撃選択で個体2体をクリックし、確定後に赤チェック。和平なしの17指示を退避0件で描画し、人間に復帰。証拠は「Individual robot clicks: 2」「after confirm: stage=5 red-owner=1」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は09:18回、緑赤と再クリックは08:00回を参照。物理Escと専用表示への自然退避は未確認。次回は同じ通常出撃診断の配置追加より、退避条件と実キーボード入力の照合を優先する。

- 10:12修正回。DEMO-0016は配置74で候補不一致を注入し、専用表示から同じ征服者の次指示へ復帰して予約数を維持することを確認。自然発生は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 10:18回

- 開始10:18:43、終了10:19:03 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null により dotnet DemoUi.dll を実行、終了0。Seed 0で直送便から地図へ復帰し、地図と地星儀の切替、18 FPSの辺移動、拒否時の分岐、終了ループとラベル描画を確認。証拠は「first transport」「map follow-up」の論理クリック (256,998) と最終PASS。実解像度は今回のログから未確認。
- 新規採番なし。プライマリー・左右格納庫は09:18回、緑赤と再クリックは08:00回、CPU軍事の候補不一致を注入した結果は10:12回を参照。自然な退避と物理Escは未確認。次回は自然な退避条件または格納庫の実キーボード入力を照合する。

- 10:31修正回。DEMO-0016は配置74で出撃確定直後の通常入力経由Escapeを確認し、人間復帰と予約残数0を確認。自然な退避と物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 10:39回

- 開始10:39:14、終了10:39:40 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=74、FOLDERVERSE_AUDIT_CONFIRM_COMPLETION=normal により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540で個体2体をクリックして通常出撃を確定し、再生継続後に人間の内政へ復帰。10:31回のスキップと予約残数0および復帰先が一致。証拠は「Individual robot clicks: 2」「Completion skip=False ruler=0 phase=Domestic planned=0」。この回は進軍・戦闘個体の逐一比較までは未実施。
- 新規採番なし。直送便・左右格納庫・プライマリーは09:18回、緑赤と再クリックは08:00回を参照。物理Escと自然退避は未確認。次回は物理Escか退避発生条件の照合を優先する。

- 10:51修正回。DEMO-0016は配置74で出撃確定直後の通常再生とEscapeを比較し、進軍・取消・戦闘参加個体の詳細まで一致。物理Escと自然退避は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 10:59回

- 開始10:59:44、終了11:00:07 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12 により dotnet DemoUi.dll を実行、終了0。直送便から地図への復帰、地図・地星儀切替、18 FPSの辺移動、拒否時分岐、終了ループとラベル描画を確認。証拠は「first transport」「map follow-up」の論理クリック (256,998) と最終PASS。解像度は実行ログから未確認。
- 新規採番なし。プライマリー・左右格納庫は09:18回、緑赤と再クリックは08:00回、CPU軍事の出撃確定後は10:51回を参照。自然な退避と物理Escは未確認。次回はその発生条件または通常UIでの実キーボード入力から再開する。

- 11:09修正回。DEMO-0016は配置75で通常出撃、同じ征服者の和平移動、人間復帰を確認。自然退避は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 11:19回

- 開始11:19:14、終了11:19:38 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=20 により dotnet DemoUi.dll を実行、終了0。DEMO-0015の既報Seedで初回直送便から地図への復帰を確認。地図・地星儀切替、18 FPSの辺移動、拒否時分岐、終了ループとラベル描画も通過。証拠は「first transport」「map follow-up」の論理クリック (256,998) と最終PASS。実解像度は今回のログから未確認。
- 新規採番なし。プライマリー・左右格納庫は09:18回、緑赤と再クリックは08:00回、CPUの和平移動は11:09回を参照。自然退避と物理Escは未確認。次回は候補不一致の発生条件か実キーボード入力を確認する。

- 11:31修正回。DEMO-0016は配置76で通常出撃・赤チェックから別の征服者の和平を経て人間に復帰。自然退避は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 11:40回

- 開始11:40:15、終了11:40:40 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=1 により dotnet DemoUi.dll を実行し、終了0。初回直送便から地図への復帰、地図・地星儀の切替、18 FPSの辺移動、拒否時分岐、終了ループとラベル描画を確認。証拠は「first transport」「map follow-up」の論理クリック (256,998) と最終PASS。実解像度はログから未確認。
- 新規採番なし。プライマリー・左右格納庫は09:18回、緑赤と再クリックは08:00回、CPU出撃・和平は11:31回を参照。物理Escと自然退避は今回未確認。次回は通常格納庫の実キーボード入力またはCPU退避条件から再開する。

- 11:49修正回。DEMO-0016は配置76で候補不一致を注入し、専用表示から別の征服者の通常出撃選択への復帰と予約数の維持を確認。自然発生は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 12:01回

- 開始12:01:45、終了12:02:06 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null により dotnet ProbeCheckedClick.dll を実行し終了0。地図・地星儀の1920×1080／960×540で緑・赤ハンドルのクリック、出撃確定と即予約→赤チェック→閉じる→ターン終了、表示分離とターン末消去を確認。赤チェック後の地点9を画面点 (1440,552)／(720,276) で再クリックできた。証拠は4条件の「red checked target re-click」と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は09:18回、CPU軍事の制御した退避は11:49回を参照。自然退避と物理Escは未確認。次回はこの2条件から再開する。

- 12:08修正回。DEMO-0016は配置77で通常出撃・赤チェック、同じ征服者の和平移動と人間復帰を確認。自然退避は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 12:20回

- 開始12:20:45、終了12:21:13 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。点検記録のみ更新。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null により dotnet DemoInspectorUi.dll を実行し、終了0。1080p／540pで輸送プライマリー、地点監督、閉じるアニメーション、工場6プランの実矩形と製造開始を確認。証拠はクリック (292,332) と矩形 (256,288,72,88)、クリック後の manufacturing=True、および最終PASS。
- 新規採番なし。直送便の退避・復帰と左右格納庫は09:18回、緑赤と再クリックは12:01回、CPUの出撃・和平は12:08回を参照。物理Escと自然な軍事退避は今回未確認。次回はこの2条件を優先する。

- 12:30修正回。DEMO-0016は配置77で出撃確定直後の通常入力経由Escapeと人間復帰、予約残数0を確認。物理Escと自然退避は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 12:39回

- 開始12:39:16、終了12:39:45 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=78 により dotnet DemoAuditCpuSortie.dll を実行し、終了0。960×540でCPUの視点切替と通常格納庫を経て、征服者17が地点70への通常出撃で個体4体をクリック。確定後に赤チェックを付け、和平なし15指示を退避0件で描画して人間に復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=17」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。直送便・左右格納庫は09:18回、プライマリーは12:20回、緑赤と再クリックは12:01回を参照。自然退避と物理Escは未確認。次回は通常出撃の配置追加より、この2条件の照合を優先する。

- 12:50修正回。DEMO-0016は配置78の候補不一致注入で、専用表示から次の征服者の通常出撃選択へ復帰し、予約数を維持。自然発生と物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 12:59回

- 開始12:59:48、終了13:00:16 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=79 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540でCPU内政の地点選択・格納庫を経て、征服者17が地点77への通常出撃で個体4体をクリックし、確定後に赤チェック。和平なし11指示を退避0件で描画して人間に復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=17」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。直送便・左右格納庫は09:18回、プライマリーは12:20回、緑赤と再クリックは12:01回を参照。自然退避と物理Escは今回未確認。次回は実キーボード入力か自然退避の発生条件を確認する。

- 13:09修正回。DEMO-0016は配置79で候補不一致を注入し、専用表示から次の征服者の通常出撃選択に戻り、予約が増えないことを確認。自然発生は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 13:19回

- 開始13:19:18、終了13:19:40 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null により dotnet DirectDeliveryUi.dll を実行、終了0。540pで直送便の退避・復帰、移動中の入力抑止、左右格納庫、地図と地星儀の連続トラッククリック、予約保持と描画を確認。証拠は「animated main/pair/return cameras」「two hangars, reservations and rendering」を含む4件のPASS。
- 新規採番なし。プライマリーは12:20回、緑赤と再クリックは12:01回、軍事の候補不一致を注入した場合は13:09回を参照。今回のEscapeは診断入力で、物理Escと自然退避は未確認。次回は通常格納庫の実キーボード入力か自然退避の発生条件から再開する。

- 13:29修正回。DEMO-0016は配置80で通常出撃・赤チェック・人間復帰を確認。自然退避は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 13:41回

- 開始13:41:48、終了13:42:16 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=81 により dotnet DemoAuditCpuSortie.dll を実行、終了0。960×540で征服者1が地点84の通常出撃選択で個体4体をクリックし、確定後に赤チェック。征服者19の和平は通常移動UIで地点152を選び、予約を増やさず到着し、人間へ復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=1」「Peaceful ruler=19 expected=152 current=152」と最終PASS。
- 新規採番なし。直送便・左右格納庫は13:19回、プライマリーは12:20回、緑赤と再クリックは12:01回を参照。物理Escと自然退避は今回未確認。次回は候補不一致が自然発生する条件または通常格納庫の実キーボード入力を照合する。

- 13:48修正回。DEMO-0016は配置82で通常出撃・赤チェック、同じ征服者の和平、人間復帰を確認。自然退避は再現せず要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 13:59回

- 開始13:59:48、終了14:00:12 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null により dotnet ProbeCheckedClick.dll を実行、終了0。地図・地星儀の1080p／540pで緑赤の実クリック、即予約→赤チェック→閉じる→ターン終了、表示分離とターン末消去を確認。赤チェック済み地点9を画面点 (1440,552)／(720,276) で再クリックできた。証拠は4条件の「red checked target re-click」と最終PASS。
- 新規採番なし。プライマリーは12:20回、直送便と左右格納庫は13:19回、CPU軍事は13:48回を参照。物理Escと自然退避は今回未確認。次回は通常格納庫の実キーボード入力かCPUの候補不一致条件を照合する。

- 14:11修正回。DEMO-0017は画面外部品の組み立てと保持中の通常入力経由Escapeで、予約保持・在庫一致・人間復帰を再確認。物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 14:18回

- 開始14:18:49、終了14:19:12 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=83 により dotnet DemoAuditCpuSortie.dll を実行し、終了0。960×540で征服者17が地点91の通常出撃選択で4体をクリックし、確定後に赤チェック。和平なし14指示を退避0件で描画し、人間へ復帰。証拠は「Individual robot clicks: 4」「after confirm: stage=5 red-owner=17」「No-peaceful batch fallbacks=0」と最終PASS。
- 新規採番なし。プライマリーは12:20回、直送便・左右格納庫は13:19回、緑赤と再クリックは13:59回を参照。物理Escと自然退避は未確認。次回は通常格納庫の実キーボード入力または候補不一致の自然発生条件を照合する。

- 14:29修正回。DEMO-0016は配置83で出撃確定後の通常再生と通常入力経由Escapeを比較し、進軍・取消・戦闘参加個体と人間への復帰が一致。物理Escと自然退避は要再確認。詳細は修正記録。

### 実行履歴：2026-11-14 14:38回

- 開始14:38:19、終了14:38:44 JST。部分点検完了。HEAD=be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa。先行修正完了。既存ゲーム差分は Game1.ComputerDomestic.cs の格納庫導入待ち。変更は点検記録のみ。
- 既存 demo-audit-local で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=34 により dotnet DemoUi.dll を実行、終了0。初回直送便から地図への復帰、地図・地星儀切替、18 FPSの辺移動、拒否時分岐、終了ループとラベル描画を確認。証拠は「first transport」「map follow-up」の論理クリック (256,998) と最終PASS。実解像度は今回のログから未確認。
- 新規採番なし。プライマリーは12:20回、左右格納庫は13:19回、緑赤と再クリックは13:59回、CPUの出撃確定後のスキップ比較は14:29回を参照。物理Escと自然退避は今回未確認。次回はこれらの再現条件を優先する。
### 実行履歴 2026-11-14 14:49 修正回

- 開始 14:49 JST、終了 14:52 JST。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。関係する既存差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。
- CPU軍事の候補不一致を seed 83 の既存描画診断で確認。通常UIで地点を選択後、候補IDを差し替えるモード3で代替表示へ入り、次の指示へ1件だけ進んだ。予約件数は 0→0、終了コード0。自然発生条件と物理Escは引き続き未確認。
- モード2は女王を設定するだけで即座に代替表示へ進む前提の検証で、指示番号のアサーションに失敗した。ゲーム不具合としては再現未確認。次回は女王の通常経路と、自然に候補不一致が起きる条件を確認する。

### 実行履歴：2026-11-14 15:00回

- 開始15:00:50、終了15:02 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。ゲーム側の既存差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。今回の変更は本記録のみ。
- 15:01:43に既存の `DemoAuditCpuSortie.exe` を起動。`ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_LEGAL_QUEEN=1`、`FOLDERVERSE_AUDIT_QUEEN540=1`、`FOLDERVERSE_AUDIT_QUEEN_ESCORTS=1`。960×540の固定場面でCPU1の敵所有地点91から通常の出撃選択、女王の切替、確定、実ハンドルの赤チェック、閉鎖と人間への復帰を確認。女王の実戦参加は1回。終了コード0。証拠は `PASS: legal queen target=91 owner=0, normal picker, queen toggle, confirmation and red check succeeded` と同出力先の `audit-legal-queen.png`。
- DEMO-0016の既報範囲で新規採番なし。プライマリー、直送便、左右インスペクターと緑赤の表示分離は今回未実施。次回は自然発生の候補不一致と物理Escから再開する。

- 15:11修正回。DEMO-0016は世界84の13指示で通常出撃と人間への復帰を確認。自然退避は再現せず、物理Escは未実施のため要再確認を維持。詳細は修正記録。

### 実行履歴：2026-11-14 15:21回

- 開始15:21:20、終了15:22:32 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。ゲーム側の既存差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 既存 `demo-audit-local` で `ALSOFT_DRIVERS=null` により `DemoInspectorUi.exe`（15:21:35起動、終了0）を実行。1080pと540pで直送便プライマリーのクリックと遷移、閉鎖アニメーション、工場6プランの実矩形と製造開始を確認。証拠は論理クリック (268,332) と矩形 (232,288,72,88)、製造開始ログ、最終PASS。
- 同じ出力先の `DirectDeliveryUi.exe`（15:22:10起動、終了0）で540pの退避・復帰、移動中の入力抑止、地図・地星儀のトラック連続操作、左右格納庫、予約保持と描画を確認。証拠は `PASS: edge trucks, hidden ordinary-inspector markers, selected overlay opening and return, two hangars, reservations and rendering.` を含む4件のPASS。
- 新規採番なし。今回の未確認は緑赤のチェックとCPU経路、自然退避、物理Esc。次回は `DemoChecksUi` の開戦確定から赤チェック、閉鎖とターン末消去を照合する。

### 実行履歴：2026-11-14 15:41回

- 開始15:41:50、終了15:42:28 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 既存の `DemoChecksUi.exe` を15:42:06に `ALSOFT_DRIVERS=null` で起動し、終了0。地図・地星儀の1920×1080／960×540で、実ハンドル矩形内の緑赤クリック、地点9の選択→出撃選択確定と即予約→赤チェック→閉鎖→ターン終了、緑赤の表示分離、赤のスポットライト解除、ターン末消去、配送後の予約実行を照合。証拠は4シナリオのPASSと最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は15:21回で確認。今回は赤チェック後の地点再クリック、CPU経路、自然退避と物理Escが未実施。次回は赤チェック後の再クリックから確認する。

### 実行履歴：2026-11-14 15:59回

- 開始15:59:20、終了16:00:01 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。変更は本記録のみ。
- 15:59:43に既存の `ProbeCheckedClick.exe` を `ALSOFT_DRIVERS=null` で起動し、終了0。地図・地星儀の1920×1080／960×540の4条件で、赤チェック後にスポットライトが外れた地点9を実ピクセル(1440,552)／(720,276)で再選択し、赤チェックと出撃予約を保持した。固定場面でDemo入力を一時解除して通常の入力処理にマウス状態を渡した結果。証拠は4件の `PASS: red checked target re-click` と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は15:21回を参照。自然再生からの再クリック、CPUの自然退避と物理Escは今回未実施。次回はCPUの自然退避条件と物理Escを確認する。

### 実行履歴：2026-11-14 16:21回

- 開始16:21:51、終了16:22:23 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 16:22:07に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=85` で起動、終了0。CPU1の通常出撃で4体をクリックし、確定後に赤チェック。11指示のうち和平1件はCPU37視点へ切替後、地点19を通常の移動UIで選択。960×540で描画し、確定で画面が閉じ、予約が重複せず、到着地点19と人間復帰を確認。証拠は `Individual robot clicks: 4`、`Actual peaceful order ruler=37 target=19`、`Peaceful ruler=37 expected=19 current=19` と最終PASS。
- 新規採番なし。自然退避は今回のログに現れず、物理Escは未実施。次回は物理Esc、または自然に候補不一致となる条件を確認する。プライマリー・直送便・左右格納庫は15:21回を参照。

### 実行履歴：2026-11-14 16:41回

- 開始16:41:51、終了16:42 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 16:42:07に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_` 接頭辞の `DOMESTIC_SKIP`、`DOMESTIC_ENTRY`、`AUTO_ENABLED`、`ASSEMBLY_LIVE`、`ASSEMBLY_INVENTORY_PAGE`、`ASSEMBLY_ESC_INPUT` を各1にして起動、終了0。960×540の固定場面で格納庫の頭胴→完成を論理点(210,430)で描画。予約1件を維持し、通常再生とスキップで完成6体と人間復帰が一致した。証拠は `Live CPU step 1`、`Live CPU step 2`、`Normal Escape input cleared CPU drag` と最終PASS。
- 新規採番なし。Escapeは通常入力へのキー状態注入で、物理キーは未確認。次回は物理EscまたはCPU軍事の自然退避から再開する。直送便などは15:21回、赤チェック再選択は15:59回を参照。

### 実行履歴：2026-11-14 17:00回

- 開始17:00:51 JST、終了17:02 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。本記録のみ追記。
- 17:01:17に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=86` で起動。960×540でCPU3の出撃確定後に赤チェック、CPU23視点で地点36を通常移動UIから選択し、人間へ復帰。証拠は9指示・和平1件、`after confirm: stage=5 red-owner=3`、`Actual peaceful order ruler=23 target=36` と最終PASS。自然退避のログなし。
- 新規採番なし。`Game1.ComputerMarches.cs` の段階1で、対象を投影できない場合や地図のヒット先が一致しない場合に代替表示へ移る経路を確認。次回はこの条件の実行時再現と物理Escから再開する。優先UIは15:21回と15:59回を参照。

### 実行履歴：2026-11-14 17:19回

- 開始17:19:22、終了17:19:55 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 17:19:38に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=87` で起動、終了0。960×540の描画診断で和平指示まで12件進み、CPU37が通常移動UIから地点4を選択して到着、人間へ復帰。証拠は `Next instruction 11->12 stage=0`、`Peaceful ruler=37 expected=4 current=4` と最終PASS。
- 新規採番なし。自然退避の再現と物理Escは未確認。次回は地点の投影またはヒット判定に失敗する条件を再現し、代替表示への切替を照合する。プライマリー・直送便・左右格納庫は15:21回を参照。

### 実行履歴：2026-11-14 17:40回

- 開始17:40:52、終了17:41:30 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。本記録のみ追記。
- 17:41:07に既存 `DemoUi.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_DEMO_TEST_SEED=35` で起動、終了0。初回直送便から地図への復帰、地図・地星儀切替、18 FPSの辺移動、拒否時分岐と終了ループを確認。証拠は `first transport` と `map follow-up` の論理クリック(256,998)、および描画検査の最終PASS。実解像度はログに出ず未確認。
- 新規採番なし。プライマリー・左右格納庫は15:21回、赤チェック後の再クリックは15:59回を参照。自然退避と物理Escは今回未実施。次回はCPUの地点投影・ヒット判定による代替表示を確認する。

### 実行履歴：2026-11-14 17:58回

- 開始17:58:22、終了17:58:55 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。変更は点検記録のみ。
- 17:58:38に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=88` で起動、終了0。960×540の描画診断でCPU21が通常UIから4体を選び、確定後に赤チェック。CPU43が通常移動UIから地点95を選び、到着後に人間へ復帰。証拠は `Individual robot clicks: 4`、`after confirm: stage=5 red-owner=21`、`Peaceful ruler=43 expected=95 current=95` と最終PASS。
- 新規採番なし。自然退避は再現せず、物理Escは未実施。次回は地点投影または地図ヒット不一致を起こす条件を検証する。プライマリー・左右格納庫は15:21回、赤チェック後の再選択は15:59回を参照。

### 実行履歴：2026-11-14 18:20回

- 開始18:20:53、終了18:21:24 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 18:21:08に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=89` で起動、終了0。960×540の診断でCPU19が地点143を通常UIで選び、4体をクリックし、確定後に赤チェック。和平なしの13指示を描画し、人間へ復帰。証拠は `stage=3 picker=0 selected=143 target=143`、`Individual robot clicks: 4`、`No-peaceful batch fallbacks=0` と最終PASS。
- 新規採番なし。世界89では自然退避を再現せず、物理Escは未実施。次回は地点投影または地図ヒット不一致が発生する場面を絞る。優先UIは15:21回、赤チェック後の再選択は15:59回を参照。

### 実行履歴：2026-11-14 18:41回

- 開始18:41:53、終了18:42:26 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 18:42:11に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=90` で起動、終了0。960×540でCPU35視点へ切替後、通常移動UIで地点186を選び、到着と人間への復帰を確認。証拠は `Actual peaceful order ruler=35 target=186`、`Peaceful ruler=35 expected=186 current=186` と最終PASS。
- 新規採番なし。地点投影・ヒット不一致による自然退避は再現を確認できず、物理Escは未実施。次回は地図の向きと対象位置を変えた固定場面でヒット判定を照合する。優先UIは15:21回、赤チェック後の再選択は15:59回を参照。

### 実行履歴：2026-11-14 19:02回

- 開始19:02:23、終了19:02:54 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 19:02:40に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=91` で起動、終了0。4指示の先頭が和平移動となる場面で、CPU11へ先に視点が切り替わり、地点128の選択・描画・確定、人間への復帰を確認。証拠は `CPU view=11` と `PASS: first peaceful order ruler=11 target=128, normal movement and human return`。この分岐は実解像度をログに出さない。
- 新規採番なし。自然退避、地図の向きを変えた固定場面と物理Escは未実施。次回はその固定場面のヒット判定から再開する。優先UIは15:21回、赤チェック後の再選択は15:59回を参照。

### 実行履歴：2026-11-14 19:18回

- 開始19:18:24、終了19:18:58 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 19:18:41に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=92` で起動、終了0。960×540でCPU5視点へ先に切り替わり、地点14を選択、通常の出撃選択で4体をクリックし、確定後に赤チェック。和平なし13指示で退避0件、人間へ復帰。証拠は `stage=3 picker=0 selected=14 target=14`、`after confirm: stage=5 red-owner=5` と最終PASS。
- 新規採番なし。地図の向きを変更した固定場面、自然退避と物理Escは未実施。次回は段階1の地図ヒットが対象と一致しない固定場面から確認する。優先UIは15:21回、赤チェック後の再選択は15:59回を参照。

### 実行履歴：2026-11-14 19:40回

- 開始19:40:24、終了19:41:10 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 既存 `demo-audit-local` で `ALSOFT_DRIVERS=null`、`DirectDeliveryUi.exe`（19:40:40起動、終了0）。960×540で直送便の退避復帰、回転した辺の文字とトラックの位置、展開図・地星儀の連続操作、左右格納庫と予約保持を確認。証拠は4件のPASS。
- 続けて `DemoInspectorUi.exe`（19:40:52起動、終了0）。1920×1080／960×540で直送便プライマリー、インスペクターの閉鎖、工場6プランの実矩形と製造遷移を確認。証拠は論理クリック(268,332)と矩形(232,288,72,88)、製造開始ログ、最終PASS。
- 新規採番なし。赤チェック後の再選択は15:59回を参照。CPU軍事の地図ヒット不一致と自然退避、物理Escは今回未実施。次回はCPU軍事の地図ヒット判定から再開する。

### 実行履歴：2026-11-14 20:01回

- 開始20:01:24、終了20:01:56 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。本記録のみ追記。
- 20:01:41に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=93` で起動、終了0。960×540でCPU3が通常UIから地点69を選択し、出撃選択で4体をクリック、確定後に赤チェック。和平なし9指示で退避0件、人間に復帰。証拠は `selected=69 target=69`、`after confirm: stage=5 red-owner=3` と最終PASS。
- 新規採番なし。地図の向きを変更した固定場面、自然退避、物理Escは未確認。次回は段階1の地図ヒットが対象と一致しない固定場面で、代替表示と次の指示への遷移を照合する。優先UIは19:40回を参照。

### 実行履歴：2026-11-14 20:21回

- 開始20:21:25、終了20:21:58 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 20:21:42に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=94` で起動、終了0。960×540でCPU5が通常UIで地点86を選び、2体をクリックして確定後に赤チェック。和平なし14指示で退避0件、人間へ復帰。証拠は `selected=86 target=86`、`Individual robot clicks: 2`、`after confirm: stage=5 red-owner=5` と最終PASS。
- 新規採番なし。地図の向きを変える固定場面、自然退避、物理Escは未実施。次回は段階1のヒット不一致を固定場面で再現し、代替表示と次の指示への遷移を確認する。優先UIは19:40回を参照。

### 実行履歴：2026-11-14 20:39回

- 開始20:39:55、終了20:40:29 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 20:40:14に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=95` で起動、終了0。960×540でCPU11が地点29の通常出撃選択で4体をクリックし、確定後に赤チェック。CPU19へ切り替わり、通常移動で地点156に到着後、人間へ復帰。証拠は `selected=29 target=29`、`after confirm: stage=5 red-owner=11`、`Peaceful ruler=19 expected=156 current=156` と最終PASS。
- 新規採番なし。地図の向きを変える固定場面、自然退避と物理Escは未確認。次回は地図ヒット不一致の固定場面を準備する。優先UIは19:40回、赤チェック後の再選択は15:59回を参照。

### 実行履歴：2026-11-14 20:58回

- 開始20:58:55、終了20:59:26 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 20:59:14に既存 `DemoChecksUi.exe` を `ALSOFT_DRIVERS=null` で起動、終了0。地図・地星儀の1920×1080／960×540で、実ハンドルの緑赤クリック、地点9の選択から出撃確定・即予約・赤チェック・閉鎖・ターン終了、表示分離と赤チェック地点のスポットライト解除、ターン末消去を確認。証拠は4シナリオのPASSと最終PASS。
- 新規採番なし。赤チェック後の再選択は15:59回、優先UIは19:40回を参照。CPU軍事のヒット不一致による退避と物理Escは今回未実施。次回はCPUの地点選択段階から再開する。

### 実行履歴：2026-11-14 21:20回

- 開始21:20:55、終了21:21:37 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 21:21:16に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=94`、`FOLDERVERSE_AUDIT_FALLBACK=3` で起動、終了0。通常UIで地点を選択した後にロボット候補IDを意図的に不一致にすると、専用表示へ移り、次の指示の出撃選択まで復帰。予約0件を維持した。証拠は `Candidate mismatch entered stage -1 through the normal preview`、`Fallback next index=1 stage=3 ruler=5 reservations=0/0` と既存出力先の `audit-war-fallback.png`、`audit-war-return.png`。
- DEMO-0016の既報範囲で新規採番なし。今回の条件は出撃候補IDの不一致。地点投影・地図ヒット不一致、自然退避、物理Escは未確認。次回は地点選択段階の固定場面を優先する。プライマリー・直送便・左右格納庫は19:40回を参照。

### 実行履歴：2026-11-14 21:39回

- 開始21:39:56、終了21:40:27 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 21:40:18に既存 `ProbeCheckedClick.exe` を `ALSOFT_DRIVERS=null` で起動、終了0。展開図・地星儀の1920×1080／960×540で、赤チェック後に地点9を通常入力へ渡したウィンドウ座標(1440,552)／(720,276)から再選択し、赤と出撃予約を保持。証拠は4件の `PASS: red checked target re-click`、開戦順序とターン末消去の最終PASS。
- 新規採番なし。CPUの地図ヒット不一致は `Game1.ComputerMarches.cs` の段階1で対象投影と候補ヒットを比較することをコードで確認した。今回の実行はDemoの再クリック。CPU側の固定場面、自然退避と物理Escは未実施。次回はCPU側の段階1から再開する。優先UIは19:40回を参照。

### 実行履歴：2026-11-14 21:59回

- 開始21:59:26、終了22:00:00 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 21:59:44に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=96` で起動、終了0。960×540のCPU軍事診断で先行8指示から和平指示へ進み、CPU35が通常移動UIから地点197を選んで到着し、人間へ復帰した。証拠は `Next instruction 7->8 stage=0`、`Peaceful ruler=35 expected=197 current=197` と最終PASS。
- 新規採番なし。地図の向きを変えた固定場面、自然退避、物理Escは未実施。次回は段階1で投影点と地図ヒットの差を検証する。優先UIは19:40回、赤チェック後の再選択は21:39回を参照。

### 実行履歴：2026-11-14 22:18回

- 開始22:18:56、終了22:19:30 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 22:19:13に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=96`、`FOLDERVERSE_AUDIT_PHASE_TIMING=1`、`FOLDERVERSE_AUDIT_PHASE_SKIP=1` で起動、終了0。CPU視点で軍事カットインを描画後、開始0.250秒に通常入力へEscapeキー状態を渡し、再生を終了して人間の内政に復帰。証拠は `Escape at age=.250 restored human=0 phase=Domestic`。実解像度はこの分岐のログで未確認。
- 新規採番なし。物理Esc、地点投影・地図ヒット不一致と自然退避は未実施。次回は段階1の地点投影とヒット判定を固定場面で照合する。優先UIは19:40回、赤チェック後の再選択は21:39回を参照。

### 実行履歴：2026-11-14 22:40回

- 開始22:40:27、終了22:40:59 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 22:40:43に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=97` で起動、終了0。960×540でCPU17へ視点切替後、地点190を通常UIで選択。3体をクリックして確定後に赤チェックを付け、和平なしの13指示を退避0件で描画し人間に復帰した。証拠は `selected=190 target=190`、`after confirm: stage=5 red-owner=17`、`No-peaceful batch fallbacks=0` と最終PASS。
- 新規採番なし。地図の向きを変えた固定場面、地図ヒット不一致による自然退避、物理Escは未実施。次回は対象地点と地図ヒットが異なる固定場面から再開する。優先UIは19:40回、赤チェック後の再選択は21:39回を参照。

### 実行履歴：2026-11-14 23:00回

- 開始23:00:27、終了23:00:55 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 23:00:43に既存 `DemoUi.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_DEMO_TEST_SEED=36` で起動、終了0。初回直送便から地図への復帰、地球儀・展開図の切替、18 FPSの辺移動、拒否時分岐、終了ループとラベル描画を確認。証拠は `first transport` と `map follow-up` の論理クリック(256,998)および最終PASS。実解像度は実行ログで未確認。
- 新規採番なし。プライマリー・左右格納庫は19:40回、赤チェック後の再選択は21:39回を参照。CPU軍事の地図ヒット不一致、自然退避と物理Escは未実施。次回はCPU地点選択の段階1を確認する。

### 実行履歴：2026-11-14 23:19回

- 開始23:19:57、終了23:20:31 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 23:20:15に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=98` で起動、終了0。960×540でCPU7が通常UIから地点37を選び、4体をクリックして確定後に赤チェック。CPU15へ切替後、和平移動で地点172に到着し、人間へ復帰。証拠は `selected=37 target=37`、`after confirm: stage=5 red-owner=7`、`Peaceful ruler=15 expected=172 current=172` と最終PASS。
- 新規採番なし。CPU地点選択の正常経路を照合。地図ヒット不一致の固定場面、自然退避と物理Escは未確認。次回は固定場面を用意して段階1から代替表示への切替を検証する。優先UIは19:40回、赤チェック後の再選択は21:39回を参照。

### 実行履歴：2026-11-14 23:38回

- 開始23:38:28、終了23:39 JST。コード照合まで完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。変更は点検記録のみ。
- CPU軍事段階1では `OperationPosition` の投影点をクリックに使う。地星儀では、同メソッド（`Game1.DestinationMap.cs:82`）は経路座標をそのまま投影し、`HitGlobeNode`（`Game1.StatusGlobe.cs:77`）は地形の起伏を足した座標を投影して最寄りの地点を判定する。双方の比較箇所は `Game1.ComputerMarches.cs:100`。投影座標の差はコードで確認したが、クリックの誤判定に至るかは未確認。新規採番なし。
- 実描画と解像度、自然退避、物理Escは今回未確認。次回は地星儀で対象と近接地点を含む固定場面を作り、両投影のピクセル差、ヒット先、代替表示の有無を測る。優先UIは19:40回、赤チェック後の再選択は21:39回を参照。
### 実行履歴：2026-11-15 00:01回

- 開始00:01:58、終了00:02 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。本記録のみ追記。
- `ALSOFT_DRIVERS=null` で既存の `ProbeCheckedClick.exe` を実行し、終了0。展開図・地星儀の1920×1080／960×540で、赤チェック後に地点9をウィンドウ座標(1440,552)／(720,276)から再選択し、赤チェックと出撃予約を保持。証拠は4件の再クリックPASSと最終PASS。
- 新規採番なし。今回の固定場面は人間側の通常入力。CPU軍事の投影差が誤判定を起こす場面と物理Escは未確認。次回は地星儀で起伏のある近接地点を使い、段階1のクリック点とヒット先を測る。プライマリー・直送便・左右格納庫は11月14日19:40回を参照。
### 実行履歴：2026-11-15 00:18回

- 開始00:18:58、終了00:19:30 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 00:19:15に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=99` で起動、終了0。960×540でCPU17が地点61の通常出撃選択で4体をクリックし、確定後に赤チェック。和平なし14指示で退避0件、人間へ復帰。証拠は `selected=61 target=61`、`after confirm: stage=5 red-owner=17`、`No-peaceful batch fallbacks=0` と最終PASS。
- 新規採番なし。プライマリー・直送便・左右格納庫は11月14日19:40回、赤チェック後の再選択は00:01回を参照。地星儀の投影差による誤判定と物理Escは未確認。次回は起伏のある近接地点でクリック点とヒット先を比較する。

### 実行履歴：2026-11-15 00:38回

- 開始00:38:28、終了00:39 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。
- 00:38:47に既存 `DirectDeliveryUi.exe` を `ALSOFT_DRIVERS=null` で起動、終了0。960×540で直送便の退避・復帰、隠れた操作や移動中の入力抑止、地星儀と展開図のトラック連続クリック、左右格納庫、予約保持と描画を確認。証拠は4件のPASS。
- 新規採番なし。今回は直送便のみ。プライマリーは11月14日19:40回を参照。CPU軍事の投影差による誤判定と物理Escは未確認。次回は起伏のある地点の投影点とヒット先の比較から再開する。

### 実行履歴：2026-11-15 01:01回

- 開始01:01:29、終了01:02 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 01:01:49に既存 `DemoInspectorUi.exe` を `ALSOFT_DRIVERS=null` で起動、終了0。1920×1080／960×540で、直送便プライマリーの実矩形と遷移、インスペクター閉鎖、工場6プランの選択と製造開始を照合。証拠は論理クリック(268,332)、矩形(232,288,72,88)、`manufacturing=True` と最終PASS。
- 新規採番なし。直送便の退避復帰・左右格納庫は00:38回を参照。CPU軍事の起伏による投影差・誤判定と物理Escは未確認。次回は地星儀上でクリック点とヒット先を照合する。

### 実行履歴：2026-11-15 01:19回

- 開始01:19:29、終了01:20:01 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。本記録のみ追記。
- 01:19:45に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=100` で起動、終了0。960×540でCPU45の視点に切り替わり、地点38の通常出撃選択で4体をクリック。確定後に赤チェックし、和平なし2指示を退避0件で描画して人間に復帰。証拠は `selected=38 target=38`、`after confirm: stage=5 red-owner=45`、`No-peaceful batch fallbacks=0` と最終PASS。
- 新規採番なし。プライマリーは01:01回、直送便と左右格納庫は00:38回を参照。地星儀の投影差による誤判定と物理Escは未確認。次回は起伏のある地点の投影位置と地星儀のヒット先を同じ画面で比較する。

### 実行履歴：2026-11-15 01:38回

- 開始01:38:29、終了01:38:59 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。本記録のみ追記。
- 01:38:47に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=101` で起動、終了0。960×540でCPU5へ先に視点を切り替え、地点118を通常UIで選択し、2体をクリックして確定後に赤チェック。和平なし7指示を退避0件で描画して人間に復帰。証拠は `selected=118 target=118`、`Individual robot clicks: 2`、`after confirm: stage=5 red-owner=5` と最終PASS。
- 新規採番なし。直送便と左右格納庫は00:38回、プライマリーは01:01回を参照。起伏のある地星儀の投影差によるヒット不一致と物理Escは未実施。次回は対象地点と近接地点を含む地星儀の固定場面を優先する。

### 実行履歴：2026-11-15 01:59回

- 開始01:59:00、終了01:59 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- 01:59:16に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=102` で起動、終了0。960×540でCPU5視点から地点28を通常UIで選択し、2体をクリックして確定後に赤チェック。10指示を退避0件で描画して人間へ復帰。証拠は `selected=28 target=28`、`after confirm: stage=5 red-owner=5`、`No-peaceful batch fallbacks=0` と最終PASS。
- 新規採番なし。プライマリーは01:01回、直送便・左右格納庫は00:38回を参照。地星儀の起伏による投影差と物理Escは今回未確認。次回は地星儀上でカーソルの投影点と地形を含むヒット位置を比較する。

### 実行履歴：2026-11-15 02:18回

- 開始02:18:30、終了02:19 JST。部分点検完了（コード照合）。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。点検記録のみ追記。
- CPU軍事段階1の再現条件を絞った。段階0で `FocusDemoNode` が地星儀の手動回転を解除し、ズームを1へ戻す。さらに `FocusGlobePosition` が対象の経路座標をアンカーにし、`GlobeOrientation` が回転後のアンカーのX・Yを差し引く。根拠は `Game1.ComputerMarches.cs:98`、`Game1.DemoDomesticActions.cs:27`、`Game1.StatusGlobe.cs:106`、`WorldPreview.RobotPins.cs:24`。段階0より前の回転・拡大だけでは段階1の条件として残らない。
- 新規採番なし。実描画・解像度、CPUのヒット不一致と物理Escは未確認。次回は段階0の直後に同じ視点で、起伏のある対象と近接地点の投影点・ヒット先を比較する。プライマリーは01:01回、直送便と左右格納庫は00:38回を参照。

### 実行履歴：2026-11-15 02:40回

- 開始02:40:00、終了02:40 JST。部分点検完了。HEAD `be2f63aa76d079d0f44ba1b49a9f7e5acb086cfa`。既存ゲーム差分は `Game1.ComputerDomestic.cs` の格納庫導入待機。本記録のみ追記。
- 02:40:17に既存 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=103` で起動、終了0。960×540でCPU5視点から地点91を通常UIで選択し、2体をクリックして確定後に赤チェック。16指示を退避0件で描画し、人間へ復帰。証拠は `selected=91 target=91`、`Individual robot clicks: 2`、`No-peaceful batch fallbacks=0` と最終PASS。
- 新規採番なし。プライマリーは01:01回、直送便・左右格納庫は00:38回を参照。今回の実行で起伏による投影差と物理Escは未確認。次回は地星儀の段階0直後に同一カメラで投影点と当たり判定を比較する。

### 実行履歴：2026-11-15 02:58回

- 開始02:58:31、終了03:00:54 JST。マージ後の部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。開始時にゲームC#・テスト・両記録の未コミット差分と未解決の競合なし。Git LFSの起動エラーにより画像差分は未確認。変更は本記録のみ。
- 指定NASの接続を確認し、`dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-16/demo-audit --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -v quiet` は警告・エラー0。03:00:11に同出力の `DemoInspectorUi.exe` を `ALSOFT_DRIVERS=null` で起動し、終了0。
- 1920×1080／960×540で、直送便プライマリー、インスペクターの閉鎖、工場6プランの実矩形と製造開始を確認。例は論理クリック(327,428)と矩形(284,406,86,44)、`manufacturing=True` と最終PASS。新規採番なし。
- 次回は同じNAS出力先で `DirectDeliveryUi` と `DemoChecksUi` を現HEADからビルドし、直送便の退避復帰・左右格納庫と赤チェックの手順を確認する。これらとCPU軍事の地星儀の投影差、物理Escはマージ後の実行未確認。

### 実行履歴：2026-11-15 03:20回

- 開始03:20:01、終了03:21:54 JST。マージ後の部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。開始時の関係差分は前回の点検記録のみ。未解決のGit競合なし。
- 既存NAS出力 `Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-16/demo-audit` へ `dotnet build` で `DirectDeliveryUi` と `DemoChecksUi` を順にビルドし、双方とも警告・エラー0。出力先に `--artifacts-path`、復元元に `artifacts/nuget-feed` と `C:/Users/muzud/.nuget/packages`、`--disable-build-servers -v quiet` を指定。
- 03:20:58から `ALSOFT_DRIVERS=null` で両検証を順に実行。直送便は960×540で退避復帰、移動中の入力抑止、左右格納庫、トラック連続操作、予約保持を確認し4件のPASS。緑赤チェックは地図・地星儀の1920×1080／960×540で、地点9を選択して出撃確定・即予約・赤チェック・閉鎖・ターン終了へ進み、スポットライト解除と表示分離・消去を確認。証拠は4シナリオと最終PASS、連続実行の終了コード0。
- 新規採番なし。プライマリーは02:58回を参照。マージ後の赤チェック後の地点再選択、CPU軍事の投影差と物理Escは未確認。次回は `ProbeCheckedClick` の再選択から再開する。

### 実行履歴：2026-11-15 03:38回

- 開始03:38:31、終了03:40:02 JST。マージ後の部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。開始時の関係差分は過去2回の点検記録のみ。未解決のGit競合なし。変更は本記録のみ。
- 既存NAS出力 `Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-16/demo-audit` へ `dotnet build artifacts/demo-click-audit/ProbeCheckedClick.csproj --artifacts-path <同出力先> --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -v quiet`。警告・エラー0。03:39:34に同出力の `ProbeCheckedClick.exe` を `ALSOFT_DRIVERS=null` で起動、終了0。
- 展開図・地星儀の1920×1080／960×540で、赤チェック後の地点9を実ピクセル(1440,552)／(720,276)から標準入力処理で再選択し、赤と出撃予約を保持。証拠は4件の `PASS: red checked target re-click` と、開戦順序・表示分離・ターン末消去の最終PASS。
- 新規採番なし。プライマリーは02:58回、直送便の退避復帰と左右格納庫は03:20回。CPU軍事の地星儀投影差と物理Escはマージ後未確認。次回はCPU検証を同一出力先でビルドし、地点選択と視点切替から再開する。

### 実行履歴：2026-11-15 03:58回

- 開始03:58:32、終了04:00:07 JST。マージ後の部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。開始時の関係差分は点検記録のみ。ゲームコード変更なし。
- 既存NAS出力 `Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-16/demo-audit` へ `dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path <同出力先> --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -v quiet`。警告・エラー0。03:59:29に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=103` で同出力の `DemoAuditCpuSortie.exe` を起動し、終了0。
- 960×540でCPU5視点に切り替わり、地点91で通常の出撃選択から2体をクリックし、確定後に赤チェック。和平なし16指示で退避0件、人間に復帰。証拠は `selected=91 target=91`、`after confirm: stage=5 red-owner=5`、`No-peaceful batch fallbacks=0` と最終PASS。
- 新規採番なし。CPU地星儀の投影差、格納庫での組み立てと物理Escはマージ後未確認。次回は組み立ての通常再生とスキップ比較から再開する。優先UIは02:58回と03:20回、赤チェック後の再選択は03:38回を参照。

### 実行履歴：2026-11-15 04:19回

- 開始04:19:32、終了04:20:33 JST。CPU格納庫の点検はアサーションで中断。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。開始時の関係差分は点検記録のみ。ゲームコード変更なし。
- 04:19:51に03:58回のNAS出力 `DemoAuditCpuSortie.exe` を `ALSOFT_DRIVERS=null` と `FOLDERVERSE_AUDIT_` 接頭辞の `DOMESTIC_SKIP`、`DOMESTIC_ENTRY`、`AUTO_ENABLED`、`ASSEMBLY_LIVE`、`ASSEMBLY_INVENTORY_PAGE`、`ASSEMBLY_ESC_INPUT` を各1にして起動。960×540の固定場面。期待は組み立て表示中に既存の人間側部品予約1件を保持すること。実際は開始前に `orders=1`、最初の組み立て完了ログより前に `Scroll preserves real reservations`（`ProbeCpuSortie.cs.txt:141`）で停止。終了コード -532462766。
- 予約の件数・内容と再生段階を失敗時に出す診断が必要。スクロールによる消失と、再生の早期終了に伴う予約解決を切り分けられていないため、DEMO-0017の要再確認として扱い、新規採番なし。次回は失敗時の予約とフェーズを照合する。組み立て完了・スキップ比較・物理Escと地星儀の投影差は未確認。優先UIは02:58回と03:20回を参照。

- 04:30修正回（04:32 JST終了）。DEMO-0017の04:19回と同条件で、診断の装備モードを組み立て可能な `Materials` に指定すると、960×540で部品予約1件を維持して頭胴→完成まで描画し、通常再生・早期スキップ・Escキー状態注入後の在庫と人間への復帰が一致。04:19回のアサーション失敗は旧モードを使った固定場面の問題として扱う。物理Escと通常プレイからの自然開始は要再確認。詳細は修正記録。

### 実行履歴：2026-11-15 04:41回

- 開始04:41:32、終了04:43 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。関連差分は既存の診断設定と両記録。今回の変更は本記録のみ。
- NASの既存 `DemoAuditCpuSortie.dll` を `dotnet` で実行。`ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=49`、同接頭辞の `DOMESTIC_SKIP`、`DOMESTIC_ENTRY`、`AUTO_ENABLED`、`ASSEMBLY_LIVE`、`ASSEMBLY_AUTO_PLAN`、`ASSEMBLY_ESC_INPUT` を各1。04:41:54の実行は120ステップで組み立て可能な組合せが揃わず、診断が終了0で早期終了。自然開始は未確認。
- 04:42:24に `FOLDERVERSE_AUDIT_ASSEMBLY_AUTO_SEED_LEG=1` を追加した対照条件で終了0。960×540で自動生産の頭胴を脚に組み付け、論理点(210,430)で完成を描画。人間側予約1件を保持し、表示オフ・通常・スキップの在庫と人間復帰が一致。証拠は `Auto factory recipe node=100 tick=4`、`Live CPU step 1: Complete`、最終PASS。
- 新規採番なし。次回は補助部品なしで現行の生産条件から組み立て候補が揃う場面を探す。物理Escと地星儀の投影差は未確認。優先UIは02:58回、03:20回と03:38回を参照。

### 実行履歴：2026-11-15 05:01回

- 開始05:01:03、終了05:02 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存差分は診断設定と両記録。本記録のみ追記。
- 既存NASの `DemoAuditCpuSortie.dll` を `dotnet` で再利用。`ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=46`、同接頭辞の `DOMESTIC_SKIP`、`DOMESTIC_ENTRY`、`AUTO_ENABLED`、`ASSEMBLY_LIVE`、`ASSEMBLY_AUTO_PLAN` を各1。単独工場の05:01:20実行では120ステップで組み立て可能な組合せが揃わず終了0。組み立て開始は未確認。
- 05:01:50に `ASSEMBLY_TWO_FACTORIES=1` を追加。隣接工場76から地点75へ脚の入荷予約が入り、地点75で自動生産した頭胴と組み立てた。部品を在庫へ直接加えず、960×540で論理点(210,430)に完成を描画。人間側予約1件を保持し、表示オフ・通常・スキップで完成1体と人間復帰が一致。終了0。証拠は `Second factory 76 reserved legs at 75`、`Live CPU step 1: Complete` と最終PASS。
- 新規採番なし。工場と入荷経路は診断で設定。通常プレイからの自然開始、物理Escと地星儀の投影差は未確認。次回は工場を自動設定する通常ターンからの開始を確認する。優先UIは02:58回、03:20回と03:38回を参照。

### 実行履歴：2026-11-15 05:21回

- 開始05:21:03、終了05:22:21 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存の追跡差分は両記録のみ。変更は本記録のみ。
- 既存のNAS出力先 `Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-16/demo-audit` に `dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path <同出力先> --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -v quiet`。警告・エラー0。05:21:52に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_DEMO_TEST_SEED=36` で `DemoUi.exe` を起動、終了0。
- 初回直送便と地図への復帰で論理クリック(256,998)。地星儀・展開図切替、18 FPSの辺移動、拒否時分岐、終了ループとラベル描画を確認。証拠は `first transport`、`map follow-up` と最終PASS。実解像度は実行ログに出ず未確認。
- 新規採番なし。プライマリーは02:58回、退避復帰と左右格納庫は03:20回、赤チェック後の地点再選択は03:38回を参照。CPU格納庫の通常ターンからの開始、地星儀の投影差、物理Escは未実施。次回は素材モードの通常ターンからCPUの組み立てが始まる条件を確認する。

### 実行履歴：2026-11-15 05:40回

- 開始05:40:33、終了05:41:25 JST。部分点検完了。HEAD 5fdc97aced7388d121cfc468df4531a5282fa284。追跡差分のうち C#・プロジェクトファイルに変更なし、点検・修正記録に既存差分あり。全体の差分取得は Git LFS の起動制限で失敗。
- 05:40:58に ALSOFT_DRIVERS=null で既存ローカル出力 dotnet artifacts/tmp/delete-after-2026-11-16/demo-audit-local/bin/DemoInspectorUi/debug/DemoInspectorUi.dll を実行し、終了0。1080p・540pで6種類の製造期間と工場プランの矩形内クリック、プラン切り替えと製造開始、直送便プライマリー、地点監督、閉じる演出、わたしの国からの移動を確認。証拠は click={X:268 Y:332} bounds={X:232 Y:288 Width:72 Height:88}、factory-plan-after=0 manufacturing=True、最終PASS。
- 新規採番なし。既存の優先経路は03:20回と05:21回を参照。CPU格納庫の通常ターンからの開始、CPU地星儀の投影差と物理Escは未確認。次回は素材モードの通常ターンから組み立てが始まる条件の点検に戻る。ゲームコード変更なし。

### 実行履歴：2026-11-15 05:58回

- 開始05:58:34、終了05:59:26 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。C#・プロジェクトファイルの追跡差分なし。既存の両記録と、LFSフィルターを無効にしたstatusで差分候補となる素材は保持した。
- 05:58:51と05:59:00に `ALSOFT_DRIVERS=null`、既存ローカル出力 `artifacts/tmp/delete-after-2026-11-16/demo-audit-local/bin` の `DemoChecksUi/debug/DemoChecksUi.dll` と `DirectDeliveryUi/debug/DirectDeliveryUi.dll` を順に `dotnet` で実行。両方終了0。新規ビルドなし。
- DemoChecksUiは1080p・540pの展開図と地星儀で地点9へのクリック、出撃確定と即予約、赤チェック、閉鎖、終了、進軍を確認。緑と赤のハンドル操作、モード別保持、ターン終了での消去も確認。証拠は各scenarioのPASSと最終PASS。
- DirectDeliveryUiは操作ボタンの退避・復帰、退避・移動中の入力抑制、閉じる操作、左右格納庫、予約保持と描画を確認。540pの展開図・地星儀連続操作は `PASS: animated main/pair/return cameras`、左右格納庫は `PASS: edge trucks` の行を参照。
- 新規採番なし。プライマリーと工場は05:40回を参照。CPU組み立ての通常ターンからの開始、CPU軍事の地星儀での位置合わせ、物理Escは未確認。次回は素材モードの通常ターンから組み立てが始まる条件に戻る。ゲームコード変更なし。

### 実行履歴：2026-11-15 06:19回

- 開始06:19:34、終了06:20:18 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。C#・プロジェクトファイルの追跡差分なし。既存の両記録を保持し、本記録のみ追記。
- 06:19:54に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_DEMO_TEST_SEED=34` で既存ローカル出力 `dotnet artifacts/tmp/delete-after-2026-11-16/demo-audit-local/bin/DemoUi/debug/DemoUi.dll` を実行し、終了0。論理(256,998)で初回直送便と地図への復帰、展開図・地星儀切替、18 FPSでの辺移動、拒否時分岐と終了ループの描画を確認。証拠は first transport、map follow-up、最終PASS。実解像度はログに出ず未確認。
- CPU組み立ての既存診断を調べた。自動生産の診断は所有者・工場設定を先に与えるため、通常ターンからの開始を立証できない。今回NASの既存診断出力は通常権限から参照できず、ローカル診断はマージ前の出力だったため、CPU経路の再検証に使っていない。
- 新規採番なし。優先UIの描画・遷移は05:40回と05:58回を参照。次回は通常ターンから素材モードのCPUが工場を設定し、組み立てへ進む場面を確認する。CPU軍事の地星儀での位置合わせと物理Escも未確認。

### 実行履歴：2026-11-15 06:39回

- 開始06:39:04、終了06:40:33 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。コードとテストの追跡差分なし。既存の両記録とLFS素材の差分候補を保持。
- 権限付きの読み取りでNASの既存出力を参照できた。06:39:49に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=104` で `dotnet Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-16/demo-audit/bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll` を起動。終了0。新規ビルドなし。
- 960×540の軍事固定場面でCPU17視点に切り替わり、地点82の通常出撃選択で4体をクリック、確定後に赤チェックを付け、人間へ復帰。証拠は `selected=82 target=82`、`Individual robot clicks: 4`、`after confirm: stage=5 red-owner=17` と最終PASS。和平なしの16指示で町名リストへの退避0件。内政の地点選択・格納庫・閉鎖の段階もログに出た。通常プレイからの自動組み立て開始はこの固定場面では未確認。
- 新規採番なし。優先UIは05:40回と05:58回を参照。次回は素材モードの通常ターンで工場設定と組み立て開始をつなぐ。CPU軍事の地星儀と物理Escも未確認。ゲームコード変更なし。

### 実行履歴：2026-11-15 07:01回

- 開始07:01:35、終了07:02:31 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。コード・テストに追跡差分なし。既存の両記録とLFS素材の差分候補を保持し、本記録のみ追記。
- 07:02:01に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=104`、同接頭辞の `DOMESTIC_SKIP`、`DOMESTIC_ENTRY`、`AUTO_ENABLED`、`ASSEMBLY_LIVE`、`ASSEMBLY_AUTO_PLAN` を各1にして、06:39回と同じNASの `DemoAuditCpuSortie.dll` を `dotnet` で起動。終了0。新規ビルドなし。
- 補助部品を追加しない単独工場の診断では、120ステップで「頭胴」と「胴脚」の入荷予約が交互に積まれ、完成可能な組み合わせが揃わず早期終了した。証拠は `No legal auto-factory recipe in 120 steps` と `pending=Body, Head;Legs, Body`。組み立ての表示・クリックには未到達のため、その解像度での確認結果なし。
- 新規採番なし。次回は単独工場で入荷予約が解決した後も部品構成が続くか、通常の内政・配送を通して確認する。CPU軍事の地星儀と物理Escは未確認。優先UIは05:40回と05:58回を参照。

### 実行履歴：2026-11-15 07:21回

- 開始07:21:35、終了07:22:28 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。追跡差分は両記録とLFS素材の差分候補。今回の変更は本記録のみ。
- 07:21:56に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_ASSEMBLY_ARRIVAL=1` で06:39回と同じNASの `DemoAuditCpuSortie.dll` を `dotnet` で実行。終了0。地点3から地点0への頭部の入荷予約を保ったまま、地点0の既存在庫2・3・4で組み立て、解決後の在庫は頭部1と完成品2。証拠は `live selection=2,3,4` と `PASS: final stock=1:Head,2:Complete`。予約解決1件をアサーションで確認。
- 既存在庫を設定して予約解決を直接呼ぶ対照診断。通常の内政・配送を通した単独工場の組み立て開始、クリックと描画、実解像度はこの診断では未確認。次回は素材モードで単独工場の生産から配送後の在庫を追う。CPU地星儀と物理Escも未確認。優先UIは05:40回・05:58回を参照。新規採番なし。

### 実行履歴：2026-11-15 07:38回

- 開始07:38:35、終了07:39:14 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。C#・テストの追跡差分なし。既存の両記録とLFS素材の差分候補を保持。
- 07:38:53に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_DEMO_TEST_SEED=37` で06:19回と同じローカルの `DemoUi.dll` を `dotnet` で実行し、終了0。初回直送便と地図復帰時の論理クリック(256,998)、展開図・地星儀切替、18 FPSの辺移動、拒否分岐、終了ループの描画を確認。証拠は first transport、map follow-up と最終PASS。実解像度はログに出ず未確認。
- 単独工場の続きをコードで照合した。通常の内政終了では `FinishDomestic` がCPU操作表示を呼び、表示後の `BeginDelivery` が製造入荷を解決してから部品計画を解決する。07:01回の予約だけを積む診断では、次の内政に入った時点での在庫を確認できない。次回はこの順序で実行し、配送後の在庫と次ターンの格納庫での操作を追う。
- 新規採番なし。通常ターンからのCPU組み立て開始、CPU軍事の地星儀と物理Escは未確認。優先UIは05:40回・05:58回を参照。ゲームコード変更なし。

### 実行履歴：2026-11-15 08:00回

- 開始08:00:35、終了08:02:16 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコードと追跡テストは変更なし。既存の非追跡診断 `artifacts/demo-click-audit/ProbeCpuSortie.cs.txt` に、3ステップごとに製造入荷を解決する任意の条件を追加。
- 既存NAS出力先に `dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path <06:39回と同じ出力先> --no-restore --disable-build-servers -p:UseAppHost=false -v quiet`。成功、警告1（旧EXEの削除拒否）、エラー0。08:01:38に07:01回と同じ環境変数に `FOLDERVERSE_AUDIT_ASSEMBLY_RESOLVE_INTAKES=1` を追加し、診断DLLを `dotnet` で実行。終了0。
- 世界104の単独工場では、入荷処理後の実在庫でも頭胴・胴脚が交互に並んだ。証拠は `Resolved intakes after tick=11 stock=Body, Head;Legs, Body` と `No legal auto-factory recipe in 120 steps`。組み立てに未到達のため、クリック・描画とその解像度は未確認。今回は製造入荷の解決のみを実行しており、通常のターン全体は未実施。
- 新規採番なし。次回は通常の内政・配送・軍事を通過する場面で、単一部品が入荷する条件と次ターンの組み立てを追う。CPU軍事の地星儀と物理Escも未確認。優先UIの実描画は05:40回と05:58回を参照。

### 実行履歴：2026-11-15 08:19回

- 開始08:19:06、終了08:19:51 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。追跡差分は両記録とLFS素材の差分候補。本記録のみ追記。
- 08:19:21に08:00回と同じNASの診断DLL、環境変数、単独工場条件で `FOLDERVERSE_AUDIT_WORLD=46` に変更して実行。終了0。入荷後は `stock=Body, Head;Legs, Body`、120ステップで組み立て候補なし。世界104と同じ部品構成で、組み立てのクリック・描画と解像度は未確認。
- 新規採番なし。次回は世界違いの同一診断を繰り返す前に、通常の内政・配送・軍事を通す再現条件を作る。CPU軍事の地星儀と物理Escも未確認。優先UIは05:40回と05:58回を参照。

### 実行履歴：2026-11-15 08:39回

- 開始08:39:06、終了08:40:43 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコードと追跡テストは変更なし。既存の非追跡診断 `artifacts/demo-click-audit/ProbeCpuSortie.cs.txt` に内政・配送・軍事・産業の順で進める条件を追加。既存の記録差分とLFS素材を保持。
- 08:00回と同じNAS出力先・コマンドで再ビルド。警告1（旧EXEの削除拒否）、エラー0。08:40:10に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=46`、同接頭辞の `DOMESTIC_SKIP`、`AUTO_ENABLED`、`ASSEMBLY_AUTO_PLAN`、`ASSEMBLY_CAMPAIGN` を各1にして同じ診断DLLを実行、終了0。
- 単独工場を設定した固定場面で、`FinishDomestic` →配送→`BeginMilitary` →`Advance` →産業を12回実行。敵軍事命令は無効。地点75には頭胴だけが入荷し、組み立て候補は空。証拠は `Delivery round=11 owner=1 stock=Body, Head` から始まる在庫行と `Campaign assembly candidate at node=75:`。組み立てのクリック・描画と解像度は未確認。
- 新規採番なし。次回は同じフェーズ進行で隣接工場から脚が入荷する条件を当て、次の内政に組み立て候補が出たら通常の格納庫表示まで追う。CPU地星儀と物理Escは未確認。優先UIは05:40回と05:58回を参照。

### 実行履歴：2026-11-15 08:59回

- 開始08:59:06、終了08:59:58 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコード・追跡テストの差分なし。既存の両記録とLFS素材の差分候補を保持し、本記録のみ追記。
- 08:59:22に08:39回と同じ環境・NASの診断DLLで `FOLDERVERSE_AUDIT_ASSEMBLY_TWO_FACTORIES=1` を追加して実行。終了0。世界46の二工場固定場面で、工場76から地点75へ脚が入り、配送1・3・4・5・7回目の在庫に完成品が現れた（回番号は0から）。証拠は `Second factory 76 reserved legs at 75`、`Delivery round=1 owner=1 stock=Legs;Legs;Legs;Legs;Legs;Legs;Complete`。
- 配送前に実在庫から探した組み立て候補は空。今回の診断は操作表示を呼ばず配送後に在庫を観測したため、格納庫のクリック・描画・解像度は未確認。次回は配送1回目の直前に操作表示を有効にし、製造入荷予定を含む組み立て表示と配送後の在庫を照合する。新規採番なし。CPU軍事の地星儀と物理Escも未確認。優先UIは05:40回・05:58回を参照。

### 実行履歴：2026-11-15 09:20回

- 開始09:20:37、終了09:23:43 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコード・追跡テストの差分なし。既存の非追跡診断に配送前のCPU操作表示と描画を追加。本記録以外の追跡ファイルは変更なし。
- 08:00回と同じNAS出力先・コマンドでビルド2回。各警告1（旧EXEの削除拒否）、エラー0。08:59回の条件に `FOLDERVERSE_AUDIT_ASSEMBLY_CAMPAIGN_SHOW=1` を追加し、09:21:30に起動。診断が格納庫のアニメーション時計を進めておらず、配送・人間復帰アサーションで停止。時計を0.25秒ずつ進めるようにした再実行は09:22:57起動、終了0。
- 世界46の二工場固定場面では、通常の内政終了からCPU操作表示に入り、960×540で組み立て用在庫を使った描画を5フレーム実行。人間への復帰と配送遷移をアサーションで確認。配送後の在庫は表示オフの08:59回と同じ脚6個と完成品1体。証拠は `Campaign playback assembly frames=5 viewport=960x540`、`Delivery round=1 owner=1 stock=Legs;Legs;Legs;Legs;Legs;Legs;Complete`。
- 新規採番なし。次回はこの条件でドラッグ・ドロップ座標と仮在庫の完成を照合し、通常の新規プレイからの開始も引き続き確認する。CPU軍事の地星儀と物理Escは未確認。優先UIは05:40回・05:58回を参照。

### 実行履歴：2026-11-15 09:39回

- 開始09:39:07、終了09:40:34 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコード・追跡テストの差分なし。既存の非追跡診断にドロップ先矩形と仮在庫を照合するアサーションを追加。本記録以外の追跡ファイルは変更なし。
- 08:00回と同じNAS出力先・コマンドで再ビルド。警告1（旧EXEの削除拒否）、エラー0。09:39:57に09:20回と同条件の世界46・二工場・操作表示オンで再実行、終了0。
- 960×540で5フレーム描画。論理始点(474,611)、終点(210,430)は格納庫セル(84,300,252,260)内。通常入力のドロップ後に仮在庫の対象が完成し、人間へ復帰して配送の実在庫は脚6個と完成品1体。証拠は `Campaign drag={X:474 Y:611} drop={X:210 Y:430} bounds={X:84 Y:300 Width:252 Height:260} result=Complete`。始点の部品矩形は独立に照合していない。
- 新規採番なし。次回は始点の部品矩形と表示オン・スキップの一致を同じ場面で照合。新規プレイからの自然開始、CPU軍事の地星儀、物理Escは未確認。優先UIは05:40回・05:58回を参照。

### 実行履歴：2026-11-15 09:59回

- 開始09:59:07、終了10:00:40 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。コードと追跡テストに差分なし。既存の非追跡診断にドラッグ始点の照合を追加し、既存の点検・修正記録とLFS素材を保持。
- 08:00回と同じNAS出力先・コマンドでビルド。警告1（旧EXEの削除拒否）、エラー0。09:59:59に09:20回と同じ世界46・二工場・操作表示オンの条件で起動、終了0。
- 960×540の描画で、論理始点(474,611)は部品矩形(423,580,102,62)内。通常入力が個体6を保持。終点(210,430)は格納庫セル(84,300,252,260)内で、仮在庫の対象が完成。人間へ復帰し、配送後の実在庫は脚6個と完成品1体。証拠は `Campaign press={X:474 Y:611} bounds={X:423 Y:580 Width:102 Height:62} robot=6` と `result=Complete`。
- 新規採番なし。次回は同じ二工場場面でスキップ後の在庫と人間復帰を比較。新規プレイからの自然開始、CPU軍事の地星儀、物理Escは未確認。優先UIは05:40回と05:58回を参照。

### 実行履歴：2026-11-15 10:21回

- 開始10:21:38、終了10:23:10 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコード・追跡テストに差分なし。既存の非追跡診断に、部品をつかんだ直後のEsc入力を追加。
- 08:00回と同じNAS出力先・コマンドでビルド。警告1（旧EXEの削除拒否）、エラー0。10:22:32に09:59回の条件に `FOLDERVERSE_AUDIT_ASSEMBLY_CAMPAIGN_ESCAPE=1` を追加し、診断DLLを実行、終了0。
- 960×540でCPUが論理(474,611)で個体6をつかんだ直後、通常入力処理にEscのキー状態を渡した。ドラッグとCPU操作表示が終了し、人間に復帰して配送へ遷移。配送後は脚6個と完成品1体で、09:59回の通常再生、08:59回の表示オフと一致。証拠は `Campaign Escape cleared the held part`、`viewport=960x540`、`Delivery round=1` の在庫行。
- 新規採番なし。新規プレイからの自然開始、CPU軍事の地星儀での位置合わせ、物理Escは未確認。次回はCPU軍事の地星儀で投影された地点と実際の選択を照合。優先UIは05:40回と05:58回を参照。

### 実行履歴：2026-11-15 10:42回

- 開始10:42:08、終了10:44:26 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存の非追跡診断にCPU軍事の地星儀ヒットと実選択の照合を追加。ゲームコード・追跡テストの差分なし。
- 08:00回と同じNAS出力先・コマンドでビルド。警告1（旧EXEの削除拒否）、エラー0。10:43:41に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=104`、`FOLDERVERSE_AUDIT_GLOBE_MATCH=1` で既存NASの診断DLLを実行し、終了0。
- 960×540でCPU17の視点に切り替わり、地星儀上の論理(1271,552)をクリック。投影後のヒット、選択先、指示先はいずれも地点82。通常の出撃選択、確定後の赤チェック、人間への復帰を確認。和平なし16指示で町名リストへの退避0件。証拠は `CPU globe click={X:1271 Y:552} hit=82 target=82 viewport=960x540`、`after confirm: stage=5 red-owner=17`、最終PASS。
- 新規採番なし。CPU軍事の別配置・解像度での地星儀、町名リストへの自然退避、新規プレイからの組み立て開始、物理Escは未確認。次回は地星儀の別配置と画面拡縮を確認する。優先UIは05:40回・05:58回を参照。

### 実行履歴：2026-11-15 11:01回

- 開始11:01:08、終了11:02:09 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。コード・追跡テストに差分なし。既存の両記録とLFS素材の差分候補を保持し、本記録のみ追記。
- 11:01:26に10:42回と同じNASの診断DLLを `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_GLOBE_MATCH=1`、`FOLDERVERSE_AUDIT_WORLD=103` で起動。終了0。新規ビルドなし。
- 960×540でCPU5の視点に切り替わり、地星儀上の論理(1194,552)のヒットと実選択が指示先の地点91に一致。通常の出撃選択、確定後の赤チェック、人間への復帰を確認。町名リストへの退避0件。証拠は `hit=91 target=91 viewport=960x540`、`after confirm: stage=5 red-owner=5`、最終PASS。
- 新規採番なし。次回はCPU軍事の地星儀を別解像度で照合。自然退避、新規プレイからのCPU組み立て開始、物理Escは未確認。優先UIは05:40回・05:58回を参照。

### 実行履歴：2026-11-15 11:18回

- 開始11:18:39、終了11:20:18 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存の非追跡診断に軍事開始直前の1920×1080設定を追加。ゲームコード・追跡テストの差分なし。本記録のみ追記。
- 08:00回と同じNAS出力先・コマンドでビルド。警告1（旧EXEの削除拒否）、エラー0。11:19:29に11:01回の世界103の条件に `FOLDERVERSE_AUDIT_GLOBE_1080=1` を追加して診断DLLを起動。終了0。
- 1920×1080で、CPU5の視点に切り替わり、地星儀の論理(1194,552)のヒットと実選択が指示先の地点91に一致。通常の出撃選択、確定後の赤チェック、人間への復帰を確認。960×540の11:01回と同じ結果。証拠は `hit=91 target=91 viewport=1920x1080`、`after confirm: stage=5 red-owner=5`、最終PASS。
- 新規採番なし。今回の町名リストへの退避は0件で、自然退避は未確認。次回は通常UIを外れる指示の自然発生条件を追う。新規プレイからのCPU組み立て開始、物理Escも未確認。優先UIは05:40回・05:58回を参照。

### 実行履歴：2026-11-15 11:41回

- 開始11:41:39、終了11:42:46 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコード・追跡テストに差分なし。既存の両記録とLFS素材の差分候補を保持。本記録のみ追記。
- 11:41:56に10:42回と同じNASの診断DLLを `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=105` で起動。終了0。診断既定の960×540。CPU1へ切り替わり、指示地点234が通常出撃選択に渡り、人間へ復帰。証拠は `selected=234 target=234` と最終PASS。
- 表示対象12指示に和平・女王・ポケット限定・出撃元欠落・戦力なしは各0件。町名リストへの自然退避は0件。既存の候補不一致診断は個体IDを差し替える固定条件で、今回の自然発生の証拠には含めていない。新規採番なし。
- 次回は配置の変更だけに頼らず、通常のAI判断を維持して対象や出撃元が変化するターンを追う。新規プレイからの組み立て開始と物理Escも未確認。優先UIは05:40回・05:58回、地星儀の両解像度は11:01回・11:18回を参照。

### 実行履歴：2026-11-15 12:00回

- 開始12:00:09、終了12:01:14 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコード・追跡テストに差分なし。既存の両記録とLFS素材の差分候補を保持し、本記録のみ追記。
- 12:00:28に11:41回と同じNASの診断DLLを `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=105`、`FOLDERVERSE_AUDIT_FALLBACK=3` で実行。終了0。診断の960×540固定場面。最初の通常地点選択後、再生用の指示の候補IDを差し替え、町名リストへの退避を再現。移動画面が閉じ、次の指示でCPU17の通常出撃選択に進み、予約件数は0のまま。証拠は `Candidate mismatch entered stage -1 through the normal preview`、`Fallback next index=1 stage=3 ruler=17 reservations=0/0`。描画は既存NAS出力先の `audit-war-fallback.png` と `audit-war-return.png`。
- 新規採番なし。固定診断は次のCPUの出撃選択で終了するため、この退避を含む指示列の人間への復帰・最終解決は未確認。候補不一致の自然発生、新規プレイからの組み立て開始と物理Escも未確認。次回は通常AIの指示を複数ターン進めて自然退避を追う。優先UIは05:40回と05:58回を参照。

### 実行履歴：2026-11-15 12:18回

- 開始12:18:40、終了12:20:19 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコードと追跡テストの差分なし。既存の非追跡診断に退避を含む指示列の完了まで追う条件を追加。
- 08:00回と同じNAS出力先・コマンドでビルド。警告1（旧EXEの削除拒否）、エラー0。12:19:31に12:00回と同じ世界105・候補差し替え条件に `FOLDERVERSE_AUDIT_FALLBACK_FINISH=1` を加えて診断DLLを実行。終了0。
- 960×540で、候補不一致時の町名リストへの退避、次のCPU17での通常出撃選択、全指示後の人間への復帰と内政遷移を確認。予約は0件のまま。証拠は `Fallback next index=1 stage=3 ruler=17 reservations=0/0`、`Fallback finished phase=Domestic player=0 reservations=0 viewport=960x540`。指示ごとの最終戦闘結果は照合していない。
- 新規採番なし。自然退避は未確認。次回は出撃候補を書き換えない条件で、複数ターンのCPU判断と実行を追う。新規プレイからの組み立て開始と物理Escも未確認。優先UIは05:40回・05:58回を参照。

### 実行履歴：2026-11-15 12:39回

- 開始12:39:10、終了12:40:52 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存の非追跡診断に2回目の軍事を追う条件を追加。ゲームコード・追跡テストは変更なし。
- 08:00回と同じNAS出力先・コマンドでビルド。警告1（旧EXEの削除拒否）、エラー0。12:40:03に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=105`、`FOLDERVERSE_AUDIT_SECOND_MILITARY=1` で実行し、終了0。
- 960×540の固定初期場面から、AIの指示・候補を差し替えず1回目の軍事、産業、次の内政・配送、2回目の軍事を進めた。表示対象は12件と17件。いずれも町名リストへの自然退避0件で、2回目も人間の内政に戻った。証拠は `Second military visible=17 fallbacks=0 player=0 phase=Domestic` と最終PASS。2回目はクリックごとの矩形を記録していない。
- 新規採番なし。自然退避は未確認。次回は和平指示を含む別配置の連続ターンを追う。新規プレイからのCPU組み立て開始・物理Escも未確認。優先UIは05:40回と05:58回を参照。

### 実行履歴：2026-11-15 13:01回

- 開始13:01:10、終了13:02:15 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存差分は両記録とLFS素材候補。本記録のみ追記。
- 13:01:28に12:39回と同じNASの診断DLLを `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=61` で実行。終了0。診断既定の960×540で通常出撃確定後にCPU1の赤チェック、和平移動でCPU39が指示先48へ到着、人間への復帰を確認。表示対象22件、和平2件。証拠は `after confirm: stage=5 red-owner=1`、`Peaceful ruler=39 expected=48 current=48` と最終PASS。
- 新規採番なし。和平を含むこの診断は1回目の軍事まで。自然退避、和平を含む連続ターン、CPU組み立ての自然開始と物理Escは未確認。次回は和平ありの診断を2回目の軍事まで延長し、変更していないAI指示と退避の有無を追う。優先UIは05:40回・05:58回を参照。ゲームコード変更なし。

### 実行履歴：2026-11-15 13:22回

- 開始13:22:10、終了13:23:53 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存の非追跡診断から2回目の軍事の検査を再利用し、和平を含む分岐でも呼ぶようにした。ゲームコード・追跡テストは変更なし。
- 08:00回と同じNAS出力先・ビルドコマンド。警告1（旧EXEの削除拒否）、エラー0。13:23:03に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=61`、`FOLDERVERSE_AUDIT_SECOND_MILITARY=1` で診断DLLを実行。終了0。
- 960×540で第1軍事は表示対象22件、和平2件。CPU39が指示先48へ到着。AIの指示を差し替えず次の産業・内政・配送を進め、第2軍事の表示対象16件で町名リストへの退避0件、人間の内政に復帰。証拠は `Peaceful ruler=39 expected=48 current=48`、`Second military visible=16 fallbacks=0 player=0 phase=Domestic` と最終PASS。第2軍事の全クリック矩形と和平件数は未記録。
- 新規採番なし。町名リストへの自然退避、新規プレイからのCPU組み立て開始、物理Escは未確認。次回は新規プレイの素材モードで、CPUが組み立てを始める在庫とターンを追う。優先UIは05:40回・05:58回を参照。

### DEMO-0023：素材モードの新規領土診断で軍事解決中に格納庫例外が発生

- 状態は未解決。2026-11-15 13:38:11～13:40:02 JST、HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。ゲームコード・追跡テストの差分なし。既存の非追跡診断 `artifacts/demo-click-audit/ProbeCpuSortie.cs.txt` に新規領土の条件を追加。
- 再現手順は08:00回と同じNAS出力先・コマンドで既存診断をビルドし、`ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=46`、`FOLDERVERSE_AUDIT_NEW_LAND_ASSEMBLY=1` で診断DLLを実行。素材モード、人間1・CPU59、領土シード456、運勢789。空き区画を番号順に割り当てて `CompleteLand` を呼び、内政・配送・軍事をモデルの入口から進める。診断が在庫や出撃指示を差し替える処理はない。描画前の例外につき実解像度は未確認。
- 期待は軍事を解決して次の内政に進むこと。実際は13:39:13起動の実行が終了コード -532462766。証拠は `InvalidOperationException: Robot store is full or ID is duplicated.` と `RobotStore.Add (RobotState.cs:160) → RobotCampaign.PlaceWinner (RobotCampaign.cs:531) → ReturnUnits → Complete → ConquestCampaign.Advance`。満杯とID重複のどちらが原因か、例外が発生したターン、通常UIから同じ場面へ到達するかは未確認。

### 実行履歴：2026-11-15 13:38回

- 開始13:38:11、終了13:40:02 JST。CPU組み立て点検は軍事解決の例外で中断。既存NASへのビルドは警告1（旧EXEの削除拒否）、エラー0。ゲームコードは変更なし。組み立て開始・クリック・描画、自然退避と物理Escは未確認。
- 次回はDEMO-0023を同条件で再現し、格納庫容量、ロボットID、帰還元・勝利地点を確認する。その後、CPU組み立て点検を再開。優先UIは05:40回と05:58回を参照。

- DEMO-0023は固定診断の停止を修正・検証済み（13:52:34 JST）。同条件で軍事解決を含む8周を通過し、RobotCampaign回帰と別配置の実描画も成功。元配置への通常UI経由の到達と組み立ての自然開始は要再確認。詳細は修正記録。

### 実行履歴：2026-11-15 14:00回

- 開始14:00:11、終了14:01:03 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。関係差分は既存のDEMO-0023修正（RobotCampaign.cs）と両記録。ゲームコード・テストは変更せず、本記録のみ追記。
- `ALSOFT_DRIVERS=null` で05:40回・05:58回と同じローカルの `DemoInspectorUi.dll`（14:00:24）と `DirectDeliveryUi.dll`（14:00:38）を順に `dotnet` で実行し、両方終了0。既存出力を使い、DEMO-0023差分を含む再ビルドは行っていない。
- 1080p・540pで工場の実矩形をクリックしてプラン変更・製造開始、地点監督・閉じる・輸送プライマリー・移動を確認。直送便では退避復帰、移動中の入力抑制、左右格納庫と予約保持、540pの展開図・地星儀の連続操作を確認。証拠は `factory-plan-after=0 manufacturing=True` と両診断の最終PASS。
- 新規採番なし。DEMO-0023の修正後のCPU軍事回帰は13:48修正記録を参照。元の配置への通常UI経由の到達、CPU組み立ての自然開始、軍事の自然退避、物理Escは未確認。次回は修正済みNAS出力で素材モードの領土・運勢条件を変え、組み立て候補を追う。

### 実行履歴：2026-11-15 14:18回

- 開始14:18:41、終了14:19:41 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。関係差分は既存のDEMO-0023修正（RobotCampaign.cs）と両記録。今回の変更は本記録のみ。
- 14:19:01に13:48修正回のNAS診断DLLを `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_WORLD=61`、`FOLDERVERSE_AUDIT_NEW_LAND_ASSEMBLY=1` で実行。終了0。素材モード、領土456・運勢789の別世界で内政・配送・軍事・産業を8周通過。証拠は `No new land assembly candidate in eight rounds`。新規ビルドなし。
- 組み立て候補が現れず、格納庫のクリック・描画・実解像度は未確認。新規採番なし。次回は世界シードに加えて領土・運勢を変え、標準の新規領土処理で生成された在庫から候補を探す。軍事の自然退避、元配置への通常UI経由の到達、物理Escも未確認。優先UIは14:00回と05:58回を参照。

### 実行履歴：2026-11-15 14:38回

- 開始14:38:12、終了14:39:50 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存のDEMO-0023修正、両記録と素材差分を保持。既存の非追跡診断に領土・運勢シード指定と自動組み立て予定の照合を追加。ゲームコード変更なし。
- 08:00回と同じNAS出力先・コマンドでビルド。警告1（旧EXEの削除拒否）、エラー0。14:39:15に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_AUDIT_` 接頭辞の `WORLD=61`、`LAND_SEED=457`、`FORTUNE_SEED=790`、`NEW_LAND_ASSEMBLY=1`、`NEW_LAND_PREVIEW=1` で診断DLLを実行し、終了0。
- 標準の領土確定で生成した在庫の8周で、実在庫の組み立て候補と入荷予定を含む自動組み立て計画の双方を探索。どちらにも候補が現れなかった。証拠は `No new land assembly candidate in eight rounds`。格納庫クリック・描画・解像度は未確認。
- 新規採番なし。次回は通常の運勢設定で選べる初期ロボット配置率も変えて候補を探す。元配置への通常UI経由の到達、軍事の自然退避、物理Escは未確認。優先UIは14:00回と05:58回を参照。

### 実行履歴：2026-11-15 14:59回

- 開始14:59:42、終了15:01:03 JST。部分点検完了。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存のDEMO-0023修正と両記録・素材差分を保持。非追跡診断に初期ロボット配置率の指定を追加。ゲームコード変更なし。
- 08:00回と同じNAS出力先・コマンドでビルド。警告1（旧EXEの削除拒否）、エラー0。15:00:27に14:38回と同じ世界61・領土457・運勢790に `FOLDERVERSE_AUDIT_SCATTER=100` を追加して診断DLLを実行。終了0。
- 標準の領土確定で生成した初期在庫から、最初の内政でCPU30の地点1に自動組み立て予定を確認。証拠は `New land planned assembly round=0 owner=30 node=1 stock=Complete;Complete;Legs, Head;Body;Head`。予定を検出した時点で診断を終えたため、CPU操作表示、クリック矩形、描画・解像度、配送後の実在庫は未確認。
- 新規採番なし。次回はこの配置でCPU30の操作表示をオンにし、通常の内政終了から選ばれる地点と格納庫の操作・配送後在庫を照合。軍事の自然退避、元配置への通常UI経由の到達、物理Escは未確認。優先UIは14:00回と05:58回を参照。

### DEMO-0024：新規領土のCPU操作表示が組み立て予定の地点を開かない

- 未解決。2026-11-15 15:18:42～15:20:26 JST。HEAD `5fdc97aced7388d121cfc468df4531a5282fa284`。既存のゲームコード差分はDEMO-0023のRobotCampaign.cs。今回のゲームコード変更なし。対象は `Game1.ComputerDomestic.cs` の `ShowComputerDomestic` における地点選択。
- 再現は非追跡診断 `artifacts/demo-click-audit/ProbeCpuSortie.cs.txt` を08:00回と同じNAS出力先にビルドし、14:59回と同じ世界61・領土457・運勢790・初期配置率100%条件に `FOLDERVERSE_AUDIT_NEW_LAND_SHOW=1` を追加。CPU30の操作表示をオンにし、新規領土の最初の内政を `FinishDomestic` から進める。描画960×540。
- 期待は、CPUの自動組み立て予定がある地点1の格納庫を開き、カーソルと組み立て結果を見せること。実際はCPU30の視点へ切り替わったが表示対象は地点0のみ。組み立て再生0フレーム・0ドロップのまま人間に復帰し、配送後に地点1の在庫は `Complete;Complete;Head`。証拠は `New land playback player=30 nodes=0 planned=1` と `frames=0 drops=0 viewport=960x540`。地点選択で参照する実在庫の候補、機能移転の優先、配送予定を含む計画のどれが差を生んだかは要切り分け。DEMO-0017で確認済みの格納庫ドラッグとは別に、表示地点の選択で再現した問題。

### 実行履歴：2026-11-15 15:18回

- 開始15:18:42、終了15:20:26 JST。部分点検完了。既存診断に新規領土からの操作表示・描画と人間復帰の確認を追加。既存NASへのビルドは警告1（旧EXEの削除拒否）、エラー0。15:19:33起動、終了0。新規採番はDEMO-0024。
- 次回修正回は同条件で再現し、機能移転予定と工場入荷を含む組み立て候補で地点選択が一致するかを確認する。この配置でのドラッグ矩形、DEMO-0023の元配置への通常UI経由の到達、自然退避、物理Escは未確認。優先UIは14:00回・05:58回を参照。

### 実行履歴：2026-11-15 15:38回

- 開始15:38:43、終了15:39:20 JST。部分点検完了。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。開始時の追跡差分はLFSフィルターを無効にした差分で表示された素材1件。ゲームコードと素材は変更せず、本記録のみ追記。
- 15:38:56に `ALSOFT_DRIVERS=null` で05:58回と同じローカルの `DemoChecksUi.dll` を `dotnet` で実行、終了0。1080p・540pの展開図と地星儀で緑・赤の実クリック、表示モード別保持、出撃確定による即予約、赤チェックから閉鎖・ターン終了・進軍までの順序とチェックの消去を確認。4条件のPASSが証拠。マージ前の既存バイナリのため、新HEADの回帰結果は未確認。直送便・左右格納庫は14:00回を参照。
- DEMO-0024は要再確認。15:30修正回の追加診断では、CPU30・地点1の組み立て予定は内政提出前にはあったが、AI計画後の一覧にはCPU30の地点がなかった。CPU22の対照ではAI計画後も地点50に予定があり、地点50を選択して960×540で組み立て用在庫を1フレーム表示したが、ドロップは0回だった。地点選択の修正候補は現HEADに含まれる。前回の診断結果を記録し、今回は同条件を再実行していない。
- 新規採番なし。次回は現HEADでCPU22の診断を再実行し、組み立てが始まる地点でドラッグを受け付ける条件と配送後の在庫を照合。元配置への通常UI経由の到達、軍事の自然退避、物理Escは引き続き未確認。

- DEMO-0024追記（15:59:23 JST）。CPU22・地点50で、CPU管理権判定により編集可能な部品をつかめない問題を再現し修正。960×540でドロップ1回と人間への復帰、通常・スキップ・表示オフで同一の配送後在庫を確認。元のCPU30はAI計画後に候補が消えるため地点選択は要再確認。詳細は修正記録。

### 実行履歴：2026-11-15 16:01回

- 開始16:01:13、終了16:02:25 JST。部分点検完了。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は前回修正の `Game1.NodeFactory.cs` と両記録。素材差分を保持し、ゲームコード変更なし。
- `dotnet build tests/DemoChecksUi/DemoChecksUi.csproj --artifacts-path <08:00回と同じNAS出力先> --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:UseAppHost=false -v quiet`。警告・エラー0。16:01:48に `ALSOFT_DRIVERS=null` で再ビルドした `bin/DemoChecksUi/debug/DemoChecksUi.dll` を `dotnet` で起動。終了0。
- 1920×1080と960×540の展開図・地星儀で、緑・赤チェックのハンドル操作、地点9の選択、出撃確定で即予約、赤チェック後に閉鎖・ターン終了・進軍、表示モード分離とターン終了の消去を確認。証拠は4条件の `PASS: check/war sequence` と最終PASS。15:38回に未確認だった新HEADのこの経路を確認できた。
- 新規採番なし。次回は現HEADのプライマリー、直送便の退避復帰、左右格納庫を確認。CPU22の組み立て修正は15:51修正記録を参照。CPU30の地点選択、軍事の自然退避、物理Escは引き続き未確認。

### 実行履歴：2026-11-15 16:18回

- 開始16:18:43、終了16:20:42 JST。部分点検完了。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は前回修正の `Game1.NodeFactory.cs` と両記録。本記録のみ追記し、素材差分を保持。
- `DemoInspectorUi` と `DirectDeliveryUi` の各csprojを16:01回と同じNAS出力先・オプションでビルドし、どちらも警告・エラー0。`ALSOFT_DRIVERS=null` で各DLLを `dotnet` で順に起動（16:19:16、16:19:59）、両方終了0。
- 1080p・540pで工場の実矩形とクリックの一致、プラン変更と製造開始、輸送プライマリー、地点監督・閉じる・移動を確認。例は `click={X:327 Y:428} bounds={X:284 Y:406 Width:86 Height:44}`、`manufacturing=True`。直送便は退避復帰、移動中の入力抑制、左右格納庫と予約保持、540pで展開図・地星儀を連続操作。証拠は両診断の最終PASS。14:00回に残った現HEADでの優先UIを確認。
- 新規採番なし。次回は現HEADでDemo自然巡回の初回直送便から地図へ復帰する経路を点検。CPU30の地点選択、軍事の自然退避、物理Escは未確認。緑・赤の経路は16:01回を参照。

### 実行履歴：2026-11-15 16:40回

- 開始16:40:14、終了16:41:29 JST。部分点検完了。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。素材差分は保持し、本記録のみ追記。
- `dotnet build tests/DemoUi/DemoUi.csproj` を16:01回と同じNAS出力先・オプションで実行し、警告・エラー0。16:40:50に `ALSOFT_DRIVERS=null`、`FOLDERVERSE_DEMO_TEST_SEED=34` で再ビルドした `bin/DemoUi/debug/DemoUi.dll` を `dotnet` 起動。終了0。
- 自然巡回で論理クリック(256,998)の初回直送便と地図復帰、展開図・地星儀の切替、18 FPSでの辺移動、拒否分岐と終了ループの描画を確認。証拠は `first transport`、`map follow-up`、最終PASS。実解像度は今回のログでは未確認。
- 新規採番なし。次回は別Seedで初回配送後の内政と開戦巡回を追う。CPU30の地点選択、軍事の自然退避、物理Escは未確認。緑・赤の経路と優先UIは16:01回・16:18回を参照。ゲームコード変更なし。

### 実行履歴：2026-11-15 16:58回

- 開始16:58:44、終了16:59:36 JST。部分点検完了。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。開始時の関係差分は既存の `Game1.NodeFactory.cs` と両記録。素材差分を保持し、本記録のみ追記。
- 16:58:59に16:40回と同じNASの `DemoUi.dll` を `ALSOFT_DRIVERS=null`、`FOLDERVERSE_DEMO_TEST_SEED=37` で実行。終了0。新規ビルドなし。
- 初回直送便と地図への復帰で論理クリック(256,998)を確認。展開図と地星儀の切り替え、18 FPSでの辺移動、拒否分岐、終了ループとラベル描画の検査はPASS。証拠は `first transport`、`map follow-up` と最終PASS。実解像度および初回配送後の内政・開戦の各クリックは今回の巡回ログでは未確認。
- 新規採番なし。次回は配送後の内政・開戦に絞り、緑・赤チェックと即予約の前後で実UI矩形と状態を記録する。CPU30の地点選択、軍事の自然退避、物理Escは未確認。優先UIと固定チェック経路は16:18回・16:01回を参照。

### 実行履歴：2026-11-15 17:19回

- 開始17:19:14、終了17:23:14 JST。自然巡回の詳細点検はビルド待ちで中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。本記録のみ追記し、素材差分を保持。
- 17:19:52に既存の `artifacts/tmp/delete-after-2026-11-13/demo-check/natural-transport/NaturalTransport.csproj` を16:01回と同じNAS出力先・オプションでビルド。dotnet PID 23080は起動したが約3分間出力がなかった。コマンド行で対象を確認し、17:22:51に今回のビルドプロセスへ終了を指示。ビルド成功とDLLの起動は確認できず、原因未確定。
- 新規採番なし。配送後の緑・赤チェックの自然巡回、クリック矩形、実解像度は未実施。次回は残存する同一ビルドがないことを確認し、診断の復元・ビルドを別々に実行。緑・赤の固定条件、優先UIは16:01回・16:18回を参照。ゲームコード変更なし。

### 実行履歴：2026-11-15 17:40回

- 開始17:40:15、終了17:40:59 JST。先行ビルドが残っているため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。素材差分を保持し、本記録のみ追記。
- 権限付きのプロセス照会で、17:19回に開始した `dotnet.exe` PID 23080 が依然として `build .../NaturalTransport.csproj --artifacts-path Z:/.../demo-audit` のコマンド行で残っていることを確認。前回は終了を指示したが、終了は確認できていない。復元・ビルド・診断を重複起動していない。
- 新規採番なし。配送後の緑・赤チェック、実UI矩形、描画・解像度は未実施。次回は同一プロセスの終了を権限付きで照会してから、復元とビルドを分けて再開。優先UIと固定チェックは16:18回・16:01回を参照。ゲームコード変更なし。

### 実行履歴：2026-11-15 17:59回

- 開始17:59:45、終了18:00:20 JST。先行ビルドの残存で中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。ゲームコード・素材は変更せず、本記録のみ追記。
- 権限付きの `tasklist` で前回確認した `dotnet.exe` PID 23080 の残存を確認。復元・ビルド・診断の重複起動は行わなかった。
- 新規採番なし。配送後の緑・赤チェック、実UI矩形・描画・解像度は未実施。次回は先行プロセスの終了を確認し、復元とビルドを分けて再開する。優先UIと固定チェックは16:18回・16:01回を参照。

### 実行履歴：2026-11-15 18:19回

- 開始18:19:45、終了18:20:07 JST。先行ビルドの残存により中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。ゲームコード・素材の変更なし。
- 権限付き `tasklist` で `dotnet.exe` PID 23080 の残存を再確認。17:19回のビルド開始から約1時間経過し、17:22回に終了を指示した後も残っている。今回も同じ出力先へ復元・ビルド・診断を重複起動していない。
- 新規採番なし。配送後の緑・赤チェックとクリック矩形・実描画・解像度は未実施。次回は当該プロセスの終了を確認し、復元とビルドを分けて再開する。

### 実行履歴：2026-11-15 18:41回

- 開始18:41:16、終了18:41:32 JST。先行ビルドが残っているため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。追記は本記録のみ。
- 権限付き `tasklist` で `dotnet.exe` PID 23080 の残存を再確認。同じ診断を重複起動せず、新規採番なし。配送後の実UI矩形・描画・解像度は未実施。次回は終了を確認してからNaturalTransportの復元・ビルドを分けて再開する。

### 実行履歴：2026-11-15 19:01回

- 開始19:01:46、終了19:02:04 JST。先行ビルド残存のため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。今回の変更は本記録のみ。
- 権限付き `tasklist` で `dotnet.exe` PID 23080 が引き続き残っていることを確認。重複実行なし、新規採番なし。配送後のクリック矩形・描画・解像度は未実施。次回は先行ビルドの終了後、NaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 19:20回

- 開始19:20:16、終了19:20:32 JST。先行ビルド残存のため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。今回の変更は本記録のみ。
- 権限付き `tasklist` で `dotnet.exe` PID 23080 の残存を確認。重複実行なし、新規採番なし。配送後のクリック矩形・描画・解像度は未実施。次回は先行ビルドの終了後、NaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 19:41回

- 開始19:41:46、終了19:42 JST。先行ビルド残存のため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。今回の変更は本記録のみ。
- 権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 の残存を確認。重複起動なし、新規採番なし。配送後の緑・赤チェック、実UI矩形・描画・解像度は未確認。次回は先行ビルドの終了を確認し、NaturalTransportの復元から再開する。優先UIと固定チェックは16:18回・16:01回を参照。

### 実行履歴：2026-11-15 20:00回

- 開始20:00:47、終了20:01 JST。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。権限付き `tasklist.exe | findstr 23080` で先行ビルドの `dotnet.exe` PID 23080 が残存しているため中断。今回の変更は本記録のみ。
- 重複起動なし、新規採番なし。配送後の緑・赤チェックと実UI矩形・描画・解像度は未確認。次回は先行プロセスの終了を確認し、NaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 20:21回

- 開始20:21:17、終了20:21 JST。先行ビルド残存のため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 を確認。本記録のみ追記。
- 重複起動なし、新規採番なし。配送後の緑・赤チェック、実UI矩形・描画・解像度は未確認。次回は先行ビルドの終了確認後にNaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 20:40回

- 開始20:40:17、終了20:40 JST。先行ビルドの残存で中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 を再確認。本記録のみ追記。
- 重複起動なし、新規採番なし。配送後の緑・赤チェック、実UI矩形・描画・解像度は未確認。次回は先行ビルドの終了を確認し、NaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 21:01回

- 開始21:01:18、終了21:01 JST。先行ビルド残存のため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。本記録のみ追記。
- 権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 を再確認。重複起動・新規採番なし。配送後の緑・赤チェック、実UI矩形・描画・解像度は未確認。次回は当該ビルドの終了を確認し、NaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 21:20回

- 開始21:20:48、終了21:21 JST。先行ビルド残存のため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。本記録のみ追記。
- 権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 を確認。重複起動・新規採番なし。配送後の緑・赤チェック、クリック矩形、実描画・解像度は未確認。次回は先行ビルドの終了確認後にNaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 21:39回

- 開始21:39:48、終了21:40 JST。先行ビルド残存により中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。本記録のみ追記。
- 権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 を確認。重複起動・新規採番なし。配送後の緑・赤チェック、クリック矩形、実描画・解像度は未確認。次回はPIDの終了を確認し、NaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 22:01回

- 開始22:01:19、終了22:01 JST。先行ビルド残存のため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。本記録のみ追記。
- 権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 を確認。重複起動・新規採番なし。配送後の緑・赤チェック、実UI矩形・実描画・解像度は未確認。次回は先行ビルドの終了確認後にNaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 22:21回

- 開始22:21:19、終了22:21 JST。先行ビルドが残存しているため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。本記録のみ追記。
- 権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 を確認。重複起動・新規採番なし。配送後の緑・赤チェック、実UI矩形・実描画・解像度は今回未確認。次回は先行ビルドの終了を確認し、NaturalTransportの復元から再開する。

### 実行履歴：2026-11-15 22:40回

- 開始22:40:49、終了22:41 JST。先行ビルド残存のため中断。HEAD `2f82f8ba15f67d37b6cd85e6d077e9834f474640`。関係差分は既存の `Game1.NodeFactory.cs` と両記録。本記録のみ追記。
- 権限付き `tasklist.exe | findstr 23080` で `dotnet.exe` PID 23080 を確認。重複起動・新規採番なし。配送後の緑・赤チェック、実UI矩形・実描画・解像度は未確認。次回は先行ビルドの終了を確認し、NaturalTransportの復元から再開する。


### 実行履歴：2026-11-16 20:39回

- 開始20:39:55、終了20:42 JST。HEAD `a611e1986591a2aef280a1ae84c91ea2c81ec88a`。利用者メモの差分を保持。PID 23080の終了を確認し再開。NASの既存出力先が使えず、Cドライブ空き203 GBを確認して既存の期限付きローカル出力に復元・ビルド。復元終了0、ビルド警告・エラー0。
- NaturalTransport.dllの実測Seed 0、実描画960×540。配送後に地点17の緑チェックとターン終了時の消去を確認。和平移動後の開戦プライマリーが矩形内、地点11の地図ヒット一致、出撃確定で予約1件、赤ハンドル(936,544)が矩形(912,520,48,48)内で赤チェックとスポットライト解除を確認。証拠はログと同出力先の audit-natural-540-post-war-*.png。
- 診断が開戦対象をリセットして再クリックすると地点11のインスペクターが開いた。移動UIの選択保持を要求するアサーションで中断。ゲーム不具合か診断条件か未確定で採番なし。次回は赤チェック・予約の保持を個別に確認し、自然経路で閉じる→ターン終了→進軍を追う。CPU30、軍事の自然退避、物理Escは未確認。ゲームコード変更なし。


### 実行履歴：2026-11-16 20:43回

- 開始20:43:55、終了20:44 JST。先行診断のプロセスが残っているため中断。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は本記録の20:39回のみ。ゲームコード変更なし。
- `Get-CimInstance Win32_Process` で PID 17060 のコマンド行が前回の `NaturalTransport.dll` の実行であることを確認。診断のアサーション出力後も終了は未確認。重複起動・新規採番なし。
- 今回は描画・クリック未実施。次回は PID 17060 の終了と現HEADの関係差分を確認し、地点11の再クリック後の赤チェック・予約保持を個別に照合。続いて自然経路で閉じる→ターン終了→進軍を確認する。

### 実行履歴：2026-11-16 20:58回

- 開始20:58:25、終了20:58 JST。先行診断の残存で中断。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の関係差分は本記録の20:39回と20:43回。ゲームコード変更なし。
- `Get-CimInstance Win32_Process` で PID 17060 のコマンド行が先行の `NaturalTransport.dll` と一致することを再確認。重複起動・新規採番なし。今回はクリック・実描画・解像度の検証なし。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェック・予約保持と、その後の閉鎖・ターン終了・進軍から再開する。


### 実行履歴：2026-11-16 21:18回

- 開始21:18:25、終了21:18 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は本記録のみ。`Get-CimInstance Win32_Process` で先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。ゲームコード変更・重複起動・新規採番なし。
- 今回はクリック・描画・解像度を検証していない。次回は当該診断の終了を確認し、地点11の再クリック後の赤チェック・予約保持、閉鎖からターン終了・進軍までを確認する。

### 実行履歴：2026-11-16 21:41回

- 開始21:41:26、終了21:41 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は本記録のみ。`Get-CimInstance Win32_Process` で先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認して、地点11の再クリック後の赤チェックと予約の保持、閉鎖からターン終了・進軍までを確認する。

### 実行履歴：2026-11-16 22:00回

- 開始22:00:56、終了22:01 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は本記録のみ。`Get-CimInstance Win32_Process` で先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。ゲームコード変更・重複起動・新規採番なし。
- 今回はクリック・実描画・解像度が未検証。次回は当該診断の終了を確認し、地点11の再クリック後の赤チェック・出撃予約の保持と、閉じる→ターン終了→進軍を照合する。

### 実行履歴：2026-11-16 22:21回

- 開始22:21:26、終了22:21 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の関係差分は既存の点検記録のみ。`Get-CimInstance Win32_Process` で先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。重複起動・新規採番なし。ゲームコード変更なし。
- クリック・描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェック・出撃予約の保持から、インスペクターを閉じてターン終了・進軍するまでを照合する。

### 実行履歴：2026-11-16 22:41回

- 開始22:41:57、終了22:42 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。`Get-CimInstance Win32_Process` で先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-16 23:01回

- 開始23:01:57、終了23:02 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。`Get-CimInstance Win32_Process` で先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェック・出撃予約の保持と、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-16 23:20回

- 開始23:20:57、終了23:21 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。`Get-CimInstance Win32_Process` で先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェック・出撃予約の保持と、閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-16 23:40回

- 開始23:40:57、終了23:41 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。プロセスのコマンド行から先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェック・出撃予約の保持と、閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 00:00回

- 開始00:00:28、終了00:00 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。`Get-CimInstance Win32_Process` で PID 17060 のコマンド行が先行の `NaturalTransport.dll` と一致することを確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェック・出撃予約の保持と、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 00:19回

- 開始00:19:58、終了00:20 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）をコマンド行で確認し、中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認後、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 00:38回

- 開始00:38:58、終了00:39 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 00:59回

- 開始00:59:59、終了01:00 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 01:20回

- 開始01:20:59、終了01:21 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は当該診断の終了後、地点11の再クリック後の赤チェックと出撃予約の保持、閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 01:38回

- 開始01:38:31、終了01:38 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 02:00回

- 開始02:00:02、終了02:00 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）が残っていることをコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- 今回はクリック・実描画・解像度を検証していない。次回は先行診断の終了後、地点11の再クリックで赤チェックと出撃予約が保持されるかを確認し、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 02:18回

- 開始02:18:32、終了02:18 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は当該診断の終了を確認後、地点11の再クリックで赤チェックと出撃予約が保持されるか、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 02:41回

- 開始02:41:32、終了02:41 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認後、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 02:59回

- 開始02:59:03、終了02:59 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認後、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 03:21回

- 開始03:21:33、終了03:21 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェック・出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 03:40回

- 開始03:40:03、終了03:40 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認後、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 04:00回

- 開始04:00:33、終了04:00 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 04:20回

- 開始04:20:03、終了04:20 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 04:41回

- 開始04:41:04、終了04:41 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 05:01回

- 開始05:01:04、終了05:01 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 05:21回

- 開始05:21:35、終了05:21 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了確認後、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 05:39回

- 開始05:39:35、終了05:39 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 05:59回

- 開始05:59:05、終了05:59 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 06:22回

- 開始06:22:06、終了06:22 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）が残存しているため中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了後、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 06:40回

- 開始06:40:36、終了06:40 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は当該診断の終了後、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 06:59回

- 開始06:59:06、終了06:59 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。ゲームコード変更・重複起動・新規採番なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 07:19回

- 開始07:19:37、終了07:19 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。`Get-CimInstance Win32_Process` で先行の `NaturalTransport.dll`（PID 17060）の残存を確認し中断。重複起動・新規採番・ゲームコード変更なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 07:41回

- 開始07:41:07、終了07:41 JST。HEAD `d623412c25dd3961a170e963520fa0d71420f1e0`。開始時の差分は点検記録のみ。先行の `NaturalTransport.dll`（PID 17060）の残存をコマンド行で確認し中断。重複起動・新規採番・ゲームコード変更なし。
- クリック・実描画・解像度は未検証。次回は先行診断の終了を確認し、地点11の再クリック後の赤チェックと出撃予約の保持、インスペクター閉鎖からターン終了・進軍までを照合する。

### 実行履歴：2026-11-17 08:02回

- 開始08:02:07 JST。HEAD `4620e675987fcde833f0c35a1dc02051ad2409a4`。開始時に未コミット差分なし。先行の NaturalTransport（PID 17060）の終了とNAS接続を確認し再開。前回ビルドから内政・直送便・地図に関係するコミット差分があるため現HEADで検証を開始した。
- `dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v quiet` を実行。PID 21964、終了時点でビルド結果未取得のため点検を中断。DirectDeliveryUi、DemoChecksUi、クリック・実描画・解像度は未確認。新規採番・ゲームコード変更なし。
- 次回は当該ビルドと後続ビルドの終了を確認し、成功した検証DLLから再開。地点11の赤チェック後の再選択と予約保持、閉鎖から進軍までの自然経路は未確認。
- 点検終了 08:06:45 JST。

### 実行履歴：2026-11-17 08:21回

- 開始08:21:50、終了08:24 JST。部分点検完了。HEAD `c2a8113c61cf724993429c4f4c62db87a4185d4f`。作業ツリーは既存の非追跡チュートリアル記録のみ。08:02回のビルド結果を回収し、3件とも警告・エラー0。ビルド時のHEADからゲーム・テストソース差分なし。
- 既存NAS出力の各DLLを `ALSOFT_DRIVERS=null` で順次実行。各上限5分、同出力の `<対象>-0821.stdout.log` と `<対象>-0821.stderr.log` に記録。起動・作成時刻／PID／終了は DemoInspectorUi 08:22:27／28072／08:22:54・0、DirectDeliveryUi 08:22:55／16040／08:23:22・-532462766、DemoChecksUi 08:23:22／30404／08:23:42・0。
- 1080p・540pで工場プランの実矩形、製造、地点監督・閉じる・輸送プライマリーを確認。展開図・地星儀で緑・赤クリック、即予約、閉鎖・ターン終了、表示モード別保持・消去を確認。直送便は退避復帰と移動中の入力抑制、閉じる・EscまでPASS。その後は下記の診断失敗で中断。
- 次回は直送便診断が現行メッセージを検査するよう整え、左右格納庫の残りと実描画を再確認する。自然経路の地点11再クリック・予約保持・進軍、CPU30と軍事の自然退避・物理Escは未確認。ゲームコード変更なし。

### DEMO-0025 直送便の空き区画テストが旧文言を要求

- 2026-11-17 08:23:22 JST、上記HEAD・差分。状態は完了（08:28 JSTの修正記録を参照）。対象は `tests/DirectDeliveryUi/Program.cs:98` と `Game1.DirectDelivery.cs` の空き区画案内。
- 再現は上記のDirectDeliveryUiを実行し、左格納庫の見えている空き区画の余白をクリックする。期待は現行の案内と描画可能な文字を検査して後続へ進むこと。実際はテストが旧文言「パーツ単位」を要求する。現行コードの空き区画案内は「その区画は一括移動できません」で、アサーション `Empty-slot guidance has every glyph required for rendering` が発生する。
- 証拠は `DirectDeliveryUi-0821.stderr.log` と現行の文言分岐。初期化中に停止したため、この案内の実画面・実解像度は未確認。ゲーム側のクリックずれは未確定。

### 実行履歴：2026-11-17 08:41回

- 開始08:41:20、終了08:42 JST。部分点検完了。HEAD `c2a8113c61cf724993429c4f4c62db87a4185d4f`。開始時の差分は直送便テスト、Demo両記録、非追跡のチュートリアル記録。先行検証との競合なし。ゲームコード変更・新規採番なし。
- 20:39回の地点11再クリックを静的に照合。NaturalTransport診断は開戦対象をリセットした後、通常入力を送り、移動UIの `_selectedMoveNode` に地点が残ることを要求していた。通常の地点インスペクターを開く経路は `CloseMovement` でこの値を -1 にする。前回ログの「インスペクターが開き、移動選択が -1」と整合するため、このアサーションだけでは赤チェックや予約の消失を判定できない。
- 次回は再クリック診断でインスペクターの地点、赤チェック、予約件数を個別に確認し、状態を差し替えない自然経路で赤チェック→閉鎖→陣営ターン終了→進軍を追う。今回の実描画・解像度・CPU経路は未確認。優先UIの実行結果は08:21回およびDEMO-0025修正記録を参照。

### 実行履歴：2026-11-17 09:02トリガー

- 実作業開始08:56ごろ、終了 09:01:30 JST（トリガー時刻は09:02:07 JST、実時刻はホスト時計）。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。開始時の差分は直送便テスト、Demo両記録、非追跡チュートリアル記録。先行Demo検証なし。点検記録と既存の検証専用 natural-transport/Program.cs を更新。ゲームコード変更・新規採番なし。
- 既存の NaturalTransport.csproj を NAS の delete-after-2026-11-17/demo-audit へ dotnet build。ローカルの2ソースで復元し、--disable-build-servers -p:UseAppHost=false を指定。再検証は --no-restore。08:57:27 PID 6048、09:00:16 PID 29300、ともに終了0・警告/エラー0。上限600秒。DLLは ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=0 で実行。上限各300秒、実描画960×540。世界/配役/配置Seed 2099272109/2137491580/1754296375。
- 自然巡回（08:59:19起動 PID 23472、08:59:51終了0）は地点71を地図で選択し、地点10からの機体28を出撃確定で即予約。赤ハンドル(936,544)が矩形(912,520,48,48)内にあり、赤チェック後はスポットライト解除。閉じる(936,488)も矩形内。陣営のターン終了で赤チェックが消去され、対応する機体の進軍1回・取消0件・地点71到着を確認。
- 再クリックの初回（08:58:28起動 PID 17756、08:59:19終了 -532462766）は通常入力が地点71を開戦対象に再選択した際、インスペクターだけを期待した診断で失敗。開戦での再選択と通常インスペクターのどちらも地点IDで照合し、赤チェックと予約件数を個別に判定した。再検証（09:00:28起動 PID 27256、09:01:03終了0）は対象をリセットした後に通常入力へ画面座標(720,276)を送り、開戦対象71、赤チェック保持、予約1件を確認。
- 証拠は同NAS出力の NaturalTransport-*-0902.{stdout,stderr}.log と bin/NaturalTransport/debug/audit-natural-540-post-war-*.png。今回の種では地点11に至らず、再クリックは地点71の開戦モードで確認。直送便・左右インスペクターは08:21回とDEMO-0025の検証を参照。次回は地点11の通常インスペクター経由の再クリック、CPU30の内政・軍事の自然退避と物理Escを続ける。

### 実行履歴：2026-11-17 09:01回

- 開始09:02:02、終了 09:04:45 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。開始時差分はDemo両記録、直送便テスト、非追跡チュートリアル記録。先行同対象プロセスなし。ゲームコード変更・新規採番なし。
- DEMO-0024を再確認。既存の artifacts/demo-click-audit/DemoAuditCpuSortie.csproj を NAS の delete-after-2026-11-17/demo-audit へ dotnet build（--disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false、ローカル２ソースで復元）。09:02:46起動・作成、PID 28300、上限600秒、09:03:57終了0、警告・エラー0。
- 同出力の bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll を dotnet 実行。09:03:57起動・作成、PID 4136、上限300秒、09:04:21終了0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_ 接頭辞で WORLD=61、LAND_SEED=457、FORTUNE_SEED=790、SCATTER=100、NEW_LAND_ASSEMBLY=1、NEW_LAND_PREVIEW=1、NEW_LAND_SHOW=1、NEW_LAND_RULER=30。
- 960×540でCPU30に表示切替後、人間に復帰して配送へ進むことを確認。提出前にCPU30地点1の組み立て候補があり、AI計画後はCPU30の地点に組み立て候補が残らず、再生対象は地点0、組み立てフレーム・ドロップとも0。CPU30の地点1選択・格納庫操作は要再確認のまま。証拠は同出力の cpu30-{build,run}-0901.{stdout,stderr}.log。
- 軍事のソース照合では征服者切替後の通常出撃選択と、対象投影・地図ヒット・出撃候補照合に失敗した場合の専用表示を確認した。軍事の自然退避、物理Esc、地点11の通常インスペクター経由の再クリックは今回未検証。次回はCPU30のAI計画後に組み立てが残る条件の探索、または軍事の自然退避から再開する。優先UIの実描画は直前の09:02トリガーと08:21回・DEMO-0025を参照。

### 実行履歴：2026-11-17 09:22回

- 開始09:23:04、終了 09:25:20 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。開始時の関係差分はDemo両記録、直送便テスト。チュートリアルの作業差分は保持。競合なし。ゲームコード変更なし。
- 既存の非追跡診断 ProbeCpuSortie.cs.txt で通常オートセーブを無効にし、赤チェック画像の出力を検証DLLと同じNASへ変更。dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。09:23:33起動・作成、PID 17476、上限600秒、09:24:16終了0、警告・エラー0。
- 同出力の bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=84、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 で dotnet 実行。09:24:16起動・作成、PID 7324、上限300秒、09:24:51終了0。配役123・配置456。証拠は同出力の DemoAuditCpuSortie-{build,run}-0923.{stdout,stderr}.log、bin/DemoAuditCpuSortie/debug/audit-no-peace-fallback.png（960×540）。
- DEMO-0016の専用表示への自然退避を再現したため、この条件の地点選択は未解決として追記する。CPU1は先に画面が切り替わり、通常出撃選択で地点89、機体3体、確定後に赤チェックへ進んだ。可視39指示中、CPU7の出撃元169・目標105だけが地図の地点選択（段階1）で専用表示へ退避。通常UIで地点を選択できることが期待値。候補不一致の注入なし。第二軍事の30指示は自然退避0、人間に復帰。重複採番なし。
- 次回は同条件のCPU7で投影可否・矩形・実ヒット・入力後の地点IDを採取し、段階1のどの判定で外れたか特定する。物理Escと地点11の通常インスペクター再クリックは未確認。直送便・左右インスペクターとプライマリーは08:21回およびDEMO-0025を参照。

- 09:26修正回（09:40:09 JST完了）。DEMO-0016の世界84・CPU7地点105で、地星儀の表示中心が隣接地点106に当たることを再現。近傍の通常ヒット座標(1314,548)から地点105を選択する修正を実描画で確認し、この条件は完了。第一軍事39指示・第二軍事30指示とも自然退避0、人間に復帰。ほかの配置の自然退避、表示オフ・スキップ比較と物理Escは引き続き要再確認。詳細は修正記録09:26回。

- 09:43回、DEMO-0016の世界84で通常再生とEscape入力を比較し、CPU7の対象進軍1回、同じ取消・戦闘参加個体、人間への復帰、予約残数0を確認。表示オフ・物理Escは要再確認。詳細は修正記録。

### 実行履歴：2026-11-17 09:49回

- 開始09:49:33、終了 09:51:26 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分はGame1.ComputerMarches.cs、Game1.StatusGlobe.cs、DirectDeliveryUiとDemo両記録。チュートリアルの既存差分を保持。競合・ゲームコード変更・新規採番なし。
- 既存NAS出力 delete-after-2026-11-17/demo-audit に dotnet build tests/<対象>/<対象>.csproj --artifacts-path <同出力先> --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。DemoChecksUi は09:49:44 PID 4484→09:49:56、DirectDeliveryUi は09:50:21 PID 11588→09:50:34。上限各600秒、いずれも終了0、警告・エラー0。
- 同出力の各DLLを ALSOFT_DRIVERS=null で dotnet 実行。DemoChecksUi は09:49:56 PID 10208→09:50:21、DirectDeliveryUi は09:50:34 PID 19724→09:51:02。上限各300秒、終了0。証拠は <対象>-{build,run}-0949.{stdout,stderr}.log。
- DemoChecksUiは1080p・540pの展開図と地星儀で開戦プライマリー、緑・赤のハンドル、即予約、赤チェック地点のスポットライト解除、閉鎖・ターン終了とモード別の保持・消去を確認。DirectDeliveryUiは540pで退避復帰、移動中の入力抑制、左右格納庫、連続クリックと予約保持を描画付きで確認。
- 次回はCPU軍事の表示オフ比較、地点11の通常インスペクター再クリック、CPU30の組み立て候補が残る条件、物理Escを継続。今回の検査は固定場面を含み、自然巡回の追加実行はなし。

### 実行履歴：2026-11-17 10:01回

- 開始10:01:40、終了 10:03:00 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。既存のチュートリアル差分を保持。競合なし。ゲームコード変更・新規採番なし。
- dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。10:01:51起動・作成 PID 31464、上限600秒、10:02:03終了0、警告・エラー0。
- 同出力の bin/DemoInspectorUi/debug/DemoInspectorUi.dll を ALSOFT_DRIVERS=null で dotnet 実行。10:02:03起動・作成 PID 26408、上限300秒、10:02:40終了0。1080pと540pで6種類の工場プランを実矩形から開いて製造状態へ遷移。地点監督、アニメーション付き閉鎖、輸送プライマリー、わたしの国から移動、人数確認の矩形と描画を確認。証拠は DemoInspectorUi-{build,run}-1001.{stdout,stderr}.log および demo-office-*.png。
- 直送便の退避復帰と左右格納庫・緑赤の操作は09:49回を参照。今回は固定場面の実描画。次回はCPU軍事の表示オフ比較、地点11の通常インスペクター再クリック、CPU30で組み立て候補が残る条件、物理Escを継続。

### 実行履歴：2026-11-17 10:19回

- 開始10:20:04、終了 10:22:53 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事・直送便テスト・Demo両記録。既存のチュートリアル差分を保持。ゲームコード変更・競合・新規採番なし。
- DEMO-0016の表示オフ比較。既存の非追跡診断 ProbeCpuSortie.cs.txt に、同じ内政処理後に軍事の操作表示だけを全CPUでオフにする検証分岐を追加。dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。10:20:31起動・作成 PID 4324、上限600秒、10:21:57終了0、警告・エラー0。
- 同出力の bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=84、FOLDERVERSE_AUDIT_CONFIRM_COMPLETION=off で dotnet 実行。10:21:57起動・作成 PID 21884、上限300秒、10:22:31終了0。960×540の描画付きで軍事の操作表示に入らず人間に復帰し、予約残数0。09:43回のオンと進軍、取消、戦闘参加個体がすべて一致し、CPU7の169→105は1件。証拠は DemoAuditCpuSortie-build-1020 と Cpu7-off-1020 の標準出力・標準エラー、および比較元 Cpu7-normal-0944.stdout.log。
- プライマリー、直送便、左右格納庫と緑赤は09:49・10:01回を参照。次回は地点11の通常インスペクター再クリック、CPU30の組み立て候補が残る条件、物理Escを継続。世界84の表示オン・オフ比較は確認済み。

### 実行履歴：2026-11-17 10:40回

- 開始10:40:41、終了 10:44:02 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、既存直送便テスト・Demo両記録。チュートリアル差分は保持。競合、ゲームコード変更、新規採番なし。
- 既存の NaturalTransport 診断に開戦インスペクター閉鎖後の通常地点再クリック条件を追加。dotnet build artifacts/tmp/delete-after-2026-11-13/demo-check/natural-transport/NaturalTransport.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。初回10:41:10 PID 22972→10:41:26、最終10:42:47 PID 20632→10:42:59。上限各600秒、終了0・警告／エラー0。
- 同出力の bin/NaturalTransport/debug/NaturalTransport.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=0、FOLDERVERSE_AUDIT_RECLICK=inspector で実行。初回10:41:26 PID 9268→10:42:20、終了 -532462766。診断がマウス押下しか送らず、地星儀の通常地点選択に必要な解放がなかった。押下・解放で再試行（10:42:59 PID 6496→10:43:37、終了0。上限各300秒）。
- 自然巡回の赤チェック後に閉鎖を完了し、960×540の地星儀で地点71を画面座標(480,276)から通常入力で再クリック。地点71のインスペクターが開き、赤チェックと出撃予約1件を保持。証拠は NaturalTransport-*-1041/1043 の各ログと audit-natural-540-post-war-red-reclick.png。地点11の元配置は今回未再現。直送便・左右格納庫・プライマリーは09:49と10:01回を参照。次回はCPU30で組み立て候補が残る条件、物理Esc、地点11の元配置から継続。

### 実行履歴：2026-11-17 11:01回

- 開始11:01:36、終了 11:03:08 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト・Demo両記録。チュートリアル差分は保持。競合、ゲームコード・診断コードの変更、新規採番なし。
- DEMO-0024のCPU30配置を比較。10:19回のNAS出力 bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll を再利用。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_ 接頭辞で WORLD=62/63、LAND_SEED=457、FORTUNE_SEED=790、SCATTER=100、NEW_LAND_ASSEMBLY=1、NEW_LAND_PREVIEW=1、NEW_LAND_SHOW=1、NEW_LAND_RULER=30。世界62は11:01:54 PID 9576→11:02:17、世界63は11:02:17 PID 26084→11:02:37。上限各300秒、終了0。証拠は Cpu30-world{62,63}-run-1101.{stdout,stderr}.log。
- 世界62ではAI計画後もCPU30の候補が残り、CPU30の画面へ切り替わって地点3の通常格納庫を開いた。960×540の描画付きで部品14の胴を機体13へドロップ1回、人間へ復帰し配送へ進んだ。配送後の地点3は機体13がComplete。世界63ではAI計画後にCPU30の候補が残らず、組み立ては未検証。世界61の地点1は引き続き要再確認。
- 次回は世界62の表示オン・オフ／スキップで配送後の地点3在庫を比較する。物理Escと地点11の元配置は未確認。直送便・左右格納庫・緑赤・プライマリーは09:49、10:01、10:40回を参照。

- 11:20修正回（11:24:03 JST）。DEMO-0024の世界62・CPU30地点3は通常再生、表示オフ、ドラッグ中スキップで配送時の在庫と人間復帰が一致。世界61の地点1はAI計画後に候補が残らず要再確認、物理Escは未実施。詳細は修正記録。

### 実行履歴：2026-11-17 11:38回

- 開始11:39:11、終了 11:41:12 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。既存のチュートリアル差分を保持。ゲーム・テストコード変更、競合、新規採番なし。
- dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v minimal。11:39:23 PID 29356→11:40:22、上限600秒、終了0、警告・エラー0。
- 同出力の bin/DemoUi/debug/DemoUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12 で dotnet 実行。11:40:22 PID 21184→11:40:44、上限300秒、終了0。自然巡回で初回配送から地図へ復帰。展開図／地星儀、18 FPSの辺クリック、拒否時の退避、固定戦闘差し替え、エンディングからの再巡回、Demoラベル描画を確認。証拠は DemoUi-{build,run}-1139.{stdout,stderr}.log と demo.png（1920×1080）。
- プライマリー・直送便・左右格納庫・緑赤は09:49・10:01・10:40回、世界62のCPU内政は11:20回を参照。次回は地点11と世界61・地点1の元配置、物理Esc、未巡回の別Seedを継続する。専用直送便の自然巡回は今回未確認。

### 実行履歴：2026-11-17 12:01回

- 開始12:01:37、終了 12:02:53 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。チュートリアルの既存差分を保持。競合・ゲーム／テストコード変更・新規採番なし。
- 11:38回のNAS出力 bin/DemoUi/debug/DemoUi.dll を再利用し、ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=20/21 で順に dotnet 実行。Seed20は12:01:50 PID 2356→12:02:11、Seed21は12:02:12 PID 3956→12:02:34。上限各300秒、終了0。証拠は DemoUi-seed{20,21}-run-1201.{stdout,stderr}.log と再利用出力 demo.png（1920×1080、最後のSeed21）。
- 既報DEMO-0015のSeed20、別配置Seed21とも初回配送と地図復帰、地星儀・展開図、18 FPSの辺クリック、拒否時の分岐、固定戦闘の差し替え、終了後の再巡回、Demoラベル描画を確認。プライマリー・直送便・左右格納庫・緑赤は09:49・10:01・10:40回、CPU内政比較は11:20回参照。地点11、世界61のCPU30地点1、物理Esc、専用直送便の自然経路は今回未確認。次回は上記未確認の元条件を優先。

### 実行履歴：2026-11-17 12:11追試

- 開始12:10:44、記録終了 12:12:04 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。ゲーム・テストコード変更、新規採番なし。
- 既存NAS出力の bin/DemoInspectorUi/debug で ALSOFT_DRIVERS=null を設定して dotnet DemoInspectorUi.dll を実行。12:11:13起動・作成 PID 31292、上限300秒、12:11:46終了0。
- 1920×1080と960×540で工場の現行矩形から開く操作と6種類の生産プラン選択を確認。事務所、閉鎖アニメーション、輸送のプライマリー、自国の移動、人数選択の矩形と描画を含む診断がPASS。証拠は既存NAS出力の DemoInspectorUi-run-121113.{stdout,stderr}.log と同DLL隣の demo-office-*.png。
- 直送便の退避復帰・左右格納庫・緑赤は09:49・10:01・10:40回、CPU内政は11:20回を参照。自然Demoの地点11、世界61・CPU30地点1、物理Esc、専用直送便の自然経路は今回未確認。次回は元条件での再現を優先。

### 実行履歴：2026-11-17 12:19回

- 開始12:19:06、終了 12:20:06 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。ゲーム・テストコード変更、競合、新規採番なし。
- DEMO-0024の元条件で既存NAS出力の bin/DemoAuditCpuSortie/debug から dotnet DemoAuditCpuSortie.dll を実行。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_ 接頭辞で WORLD=61、LAND_SEED=457、FORTUNE_SEED=790、SCATTER=100、NEW_LAND_ASSEMBLY=1、NEW_LAND_PREVIEW=1、NEW_LAND_SHOW=1、NEW_LAND_RULER=30。起動・作成12:19:23、PID 23620、上限300秒、12:19:47終了0。証拠は Cpu30-world61-run-121923.{stdout,stderr}.log。
- 計画前にはCPU30の地点1に候補がある。AI計画後にはCPU30の組み立て対象が0件で、960×540の診断で再生フレーム・ドロップとも0。元配置で格納庫操作が始まった場合のカーソルと入力は要再確認。世界62の通常再生とオフ／スキップ比較は11:01・11:20回。
- プライマリー、直送便退避復帰、左右格納庫、緑赤は09:49・10:01・10:40・12:11回を参照。地点11再クリック、物理Esc、専用直送便自然巡回は今回未確認。次回は地点11の元Seedと到達条件の特定を優先。

### 実行履歴：2026-11-17 12:39回

- 開始12:39:10、終了 12:40:40 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。ゲーム・テストコード変更、競合、新規採番なし。
- 地点11の元記録を照合。11月13日の内政での到達はSeed33、11月16日20:39の赤チェック後の到達はSeed0。既存NAS出力の bin/NaturalTransport/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=33、FOLDERVERSE_AUDIT_RECLICK=inspector として dotnet NaturalTransport.dll を実行。12:39:46起動・作成 PID 22204、上限300秒、12:40:19終了0。
- 現在のSeed33では和平移動後の開戦目標は地点54。プライマリーと赤ハンドルのクリックは矩形内にあり、出撃確定直後の予約1件を確認。960×540の地星儀で地点54の画面座標(480,276)に通常入力の押下・解放を送り、同地点のインスペクターが開き、赤チェックと予約1件を保持。証拠は NaturalTransport-reclick-seed33-1239.{stdout,stderr}.log と audit-natural-540-post-war-red-reclick.png。
- 地点11の同条件、再クリック後の閉鎖から進軍まで、物理Esc、専用直送便の自然経路は未確認。次回は11月16日Seed0の実測ログと現在のSeed0の世界・配役・領土Seedを比較する。左右格納庫・直送便退避復帰は09:49・10:01回、CPU操作表示は11:20・12:19回参照。

### 実行履歴：2026-11-17 13:01回

- 開始13:01:38、終了 13:03:20 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。ゲーム・テストコード変更、競合、新規採番なし。
- 既存NAS出力の bin/NaturalTransport/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=0、再クリック分岐なしで dotnet NaturalTransport.dll を実行。13:02:27起動・作成 PID 19572、上限300秒、13:03:00終了0。世界・配役・領土の診断出力は2099272109/2137491580/1754296375で10:43の地点71再クリックと一致。11月16日20:39の地点11は監査記載で確認し、検索した既存出力から元ログを回収できなかった。
- 960×540の自然経路で地点71の選択、即時予約、赤チェック、閉じるボタンの矩形内クリック、ターン終了で赤消去、配送を経た予約機体28の地点71への進軍を確認。予約機体28の実行1件に加え、同経路の女の子の進軍1件。証拠は NaturalTransport-sortie-1301.{stdout,stderr}.log と audit-natural-540-post-war-sortie-resolved.png。通常入力の地点71再クリックは10:43追試を参照。
- 元の地点11、再クリックを挟んだ同一セッションの進軍、物理Esc、専用直送便の自然経路は未確認。次回は再クリック後に閉鎖からターン終了まで追う。左右格納庫・退避復帰は09:49・10:01回、CPU表示比較は11:20回参照。

### 実行履歴：2026-11-17 13:22回

- 開始13:22:07、終了 13:23:57 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の地星儀・CPU軍事、直送便テスト等の差分を保持。既存の非追跡NaturalTransport診断に再クリック後も続ける分岐を追加。ゲームコード変更、競合、新規採番なし。
- dotnet build artifacts/tmp/delete-after-2026-11-13/demo-check/natural-transport/NaturalTransport.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。13:22:46起動・作成 PID 17692、上限600秒、13:22:59終了0、警告・エラー0。同出力の bin/NaturalTransport/debug/NaturalTransport.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=0、FOLDERVERSE_AUDIT_RECLICK=continue で実行。13:22:59起動・作成 PID 16256、上限300秒、13:23:34終了0。
- 960×540、地点71。出撃確定で即予約し、赤チェック後にスポットライト解除を確認。診断が閉鎖と地図への焦点合わせを行い、地図座標(480,276)へ通常入力で押下・解放。同地点のインスペクターが開き、赤と予約1件を保持。Demoを再開し、閉じるボタンの矩形内クリック、陣営ターン終了時の赤消去、配送後に予約機体28の進軍1件と取消0を同一セッションで確認。証拠は NaturalTransport-reclick-continue-{build,run}-1322.{stdout,stderr}.log と audit-natural-540-post-war-red-reclick.png／audit-natural-540-post-war-sortie-resolved.png。
- 地点11の元配置、同経路の物理マウス操作、物理Esc、専用直送便の自然巡回は未確認。次回は専用直送便の自然起動候補から再開。左右格納庫・退避復帰は09:49・10:01回、CPU操作表示の比較は11:20・12:19回参照。

### 実行履歴：2026-11-17 13:41回

- 開始13:41:07、終了 13:42:33 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。変更は本記録のみ。競合、新規採番なし。
- 既存NAS出力の bin/DirectDeliveryUi/debug で ALSOFT_DRIVERS=null を設定して dotnet DirectDeliveryUi.dll を実行。13:41:46起動・作成 PID 28696、上限300秒、13:42:10終了0。プライマリーの比率、退避復帰、移動中の入力抑止、閉じるとEscape、左右格納庫、960×540での実描画・連続クリック・予約保持がPASS。証拠は DirectDelivery-run-1341.{stdout,stderr}.log と同DLL隣の direct-*.png。
- 専用直送便の自然巡回はソースを照合。Game1.DemoDomestic.csの段階12は TransportOverviewButton（プライマリー4、ロボット輸送長期計画）を使う。通常の直送便地図はGame1.MapOverlays.csのプライマリー3から OpenDirectMap を呼ぶ。確認したDemo巡回にこの直送便ボタンを押す段階はなく、Seedを変えるだけでは同経路の実演に到達する根拠が得られなかった。専用直送便の実演は未確認、通常UI診断に合格。
- 再クリック後の進軍は13:22回、緑赤・CPU比較は10:40・11:20回参照。地点11の元配置、物理マウス・物理Escは未確認。次回は物理操作と競合しない検証環境を確認するか、CPU軍事の未検証配置へ進む。

### 実行履歴：2026-11-17 14:01回

- 開始14:01:08、終了 14:02:18 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。変更は本記録のみ。競合、新規採番なし。
- 既存NAS出力の bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=85、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll を実行。14:01:24起動・作成 PID 5124、上限300秒、14:01:57終了0。証拠は CpuMilitary-world85-run-1401.{stdout,stderr}.log。
- CPU9へ画面を切り替え、通常の地点94、出撃選択の機体2体、確定後の赤チェックを描画付きで確認。世界85は第一軍事12指示と第二軍事15指示で専用表示への自然退避0。人間0に復帰し内政へ進行。専用表示が必要になる対照例は世界84の修正前09:22回、表示オフ・スキップ比較は09:43・10:19回参照。
- 優先プライマリー、直送便退避・復帰と左右格納庫は12:11・13:41回、赤チェック後の再クリックから進軍は13:22回。地点11の元配置、物理Esc、専用直送便の自然実演は未確認。次回は別配置で専用表示への自然退避が残る操作を探す。

### 実行履歴：2026-11-17 14:40回

- 開始14:40:10、終了 14:41:17 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。変更は本記録のみ。競合、新規採番なし。
- 既存NAS出力の bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=86、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll を実行。14:40:27要求、14:40:28作成 PID 6736、上限300秒、14:40:58終了0。証拠は CpuMilitary-world86-run-1440.{stdout,stderr}.log と既存NASの audit-latest-computer-confirm.png、audit-latest-cpu-red.png（960×540）。
- CPU3へ画面切り替え後、通常地図で地点93、出撃選択の機体4体、確定後の赤チェックを診断で確認。第一軍事12指示・第二軍事13指示で専用表示への退避0。人間0に復帰し内政へ進行。直送便と左右格納庫は13:41回、プライマリーは12:11回、赤チェック後の再クリック・進軍は13:22回参照。
- 地点11の元配置、物理Esc、専用直送便の自然実演は未確認。次回は退避が残り得る軍事条件か、同じ地点でも見える矩形が変わる別解像度を点検する。

### 実行履歴：2026-11-17 14:59回

- 開始14:59:09、終了 15:00:26 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事、直送便テスト、Demo両記録。変更は本記録のみ。競合、新規採番なし。
- 既存NAS出力の bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=84、FOLDERVERSE_AUDIT_GLOBE_1080=1、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll を実行。14:59:24要求・作成 PID 14784、上限300秒、15:00:02終了0。証拠は CpuMilitary-world84-1080-run-1459.{stdout,stderr}.log と cpu7-target105.png。
- DEMO-0016の世界84・CPU7地点105。地星儀で中心が隣接地点106に当たる条件に対し、近傍(1314,548)から地点105を選択し、通常操作の段階2へ進んだ。CPU1は地点89の出撃画面で3体を選び赤チェックを反映。第一軍事39指示、第二軍事30指示で専用表示への自然退避0。人間0に復帰し内政へ進行。
- 直送便退避・復帰と左右格納庫は13:41回、赤チェック後の再クリック・進軍は13:22回。地点11の元配置、物理Esc、専用直送便の自然実演は未確認。次回は地星儀と展開図でのCPU通常操作の対照、または未巡回配置を点検する。

### 実行履歴：2026-11-17 15:19回

- 開始15:19:40、終了 15:21:56 JST。展開図の部分点検は中断。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の地星儀・CPU軍事、直送便テスト等の差分を保持。変更は非追跡診断 artifacts/demo-click-audit/ProbeCpuSortie.cs.txt と本記録。ゲームコード変更・競合・新規採番なし。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。15:20:21起動・作成 PID 26300、上限600秒、15:20:35終了0、警告・エラー0。同出力の bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=84、FOLDERVERSE_AUDIT_SECOND_MILITARY=1、FOLDERVERSE_AUDIT_MILITARY_NET=1 で実行。15:20:35起動・作成 PID 5952、上限300秒、15:21:19終了-532462766。証拠は CpuMilitary-net-{build,run}-1519.{stdout,stderr}.log。
- 960×540の診断でCPU1へ切り替えた直後に内部状態で展開図を指定。期待は地点89を選び通常出撃画面を開くこと。実際は段階-1、選択地点-1、出撃画面-1で停止。DEMO-0016関連の要再確認。通常の地図切り替えと同じ状態だったか、どの段階で専用表示へ移ったかは未取得。次回は通常の展開図切替とカメラ更新を経た再現および投影・ヒット・退避段階の採取から再開。
- 直送便・左右格納庫は13:41回、赤チェックから再クリック・進軍は13:22回、世界84の地星儀は14:59回参照。地点11の元配置、物理Esc、専用直送便の自然実演は未確認。

- 15:24修正回で通常の地図遷移処理を呼ぶ診断でも地点89の症状を再現。退避直前の投影・ヒットは未取得のため要再確認。詳細は修正記録。

### 実行履歴：2026-11-17 15:41回

- 開始15:41:09、終了 15:44:28 JST。部分点検完了。DEMO-0016の展開図条件を再現し未解決。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。ゲームコード差分は既存の地星儀・CPU軍事等。今回の変更は非追跡 ProbeCpuSortie.cs.txt と本記録。
- 世界84のCPU1・地点89で地図の通常遷移処理を経て960×540の展開図を描画。開戦直後は論理座標(960,552)で候補・通常ヒットとも地点89。次のCPU更新後は段階-1、投影座標(1920,552)、地図のX範囲1000以上1880未満。期待は表示された地点89を通常選択して出撃画面へ進むこと。実際は選択-1・出撃画面-1で専用表示へ移る。CPU更新はクリック前にインスペクターのスライドを進める。段階0のカメラ調整と段階1の矩形確定の順序を次回修正候補とする。通常の物理入力からCPUの展開図保存状態を作る経路は未確認。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。初回15:41:36 PID 5564→15:42:02、診断内の描画ヘルパーの範囲誤りで終了1。最終15:42:14 PID 22520→15:42:57、終了0・警告とエラー0。上限各600秒。
- 同出力の bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=84、FOLDERVERSE_AUDIT_SECOND_MILITARY=1、FOLDERVERSE_AUDIT_MILITARY_NET=1 で実行。15:42:57起動・作成 PID 19052、上限300秒、15:43:42終了-532462766。証拠は CpuMilitary-net-hit-{build,run}-1541/1542.{stdout,stderr}.log。
- 優先プライマリー・直送便と左右格納庫は12:11・13:41回、赤チェック再選択と進軍は13:22回。地点11の元配置、物理Esc、専用直送便の実演は未確認。

- DEMO-0016の展開図・世界84・CPU1地点89は 15:49:56 JSTに修正完了。960×540で出撃3体と赤チェック、通常・スキップ・表示オフの実行結果一致、人間復帰を確認。詳細は15:45修正記録。

### 実行履歴：2026-11-17 15:59回

- 開始15:59:15、終了 16:00:23 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分はDEMO-0016の地星儀・展開図、直送便テスト、Demo両記録。今回の変更は本記録のみ。競合・新規採番なし。
- 15:45修正回の既存NAS出力 bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=84、FOLDERVERSE_AUDIT_SECOND_MILITARY=1、FOLDERVERSE_AUDIT_MILITARY_NET=1、FOLDERVERSE_AUDIT_GLOBE_1080=1 で dotnet DemoAuditCpuSortie.dll を実行。15:59:27起動・作成 PID 29952、上限300秒、16:00:02終了0。
- 1920×1080の展開図でCPU1・地点89を(1440,552)に再投影し、通常出撃画面で3体を選択、確定後の赤チェックを確認。地星儀のCPU7・地点105も通常選択。第一軍事39指示・第二軍事30指示で専用表示への退避0、人間へ復帰。証拠は CpuMilitary-net-1080-run-1559.{stdout,stderr}.log と audit-latest-computer-confirm.png／audit-latest-cpu-red.png。
- プライマリーと直送便退避復帰・左右格納庫は12:11・13:41回、赤チェック後の再クリック・進軍は13:22回。地点11の元配置、物理入力、専用直送便の実演は未確認。次回は世界84以外の展開図か、CPUの通常入力の残る分岐を続ける。

### 実行履歴：2026-11-17 16:19回

- 開始16:19:08、終了 16:20:06 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分はDEMO-0016の地星儀・展開図、直送便テスト、Demo両記録。変更は本記録のみ。競合・新規採番なし。
- 既存NAS出力の bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=85、FOLDERVERSE_AUDIT_SECOND_MILITARY=1、FOLDERVERSE_AUDIT_MILITARY_NET=1 で dotnet DemoAuditCpuSortie.dll を実行。16:19:18起動・作成 PID 5068、上限300秒、16:19:46終了0。
- 960×540の展開図でCPU9に切り替わり、インスペクターを開いた後に論理(1440,552)で地点94を選択。通常出撃画面で2体の選択と確定、赤チェックを実描画付きで確認。第一軍事12指示、第二軍事15指示で退避0、人間に復帰。証拠は CpuMilitary-net-world85-run-1619.{stdout,stderr}.log と audit-latest-computer-confirm.png／audit-latest-cpu-red.png。
- 直送便・プライマリー・左右は12:11・13:41回、赤チェック後の再クリック・進軍は13:22回。物理入力、地点11の元配置、専用直送便実演は未確認。次回は展開図のクイーン・ポケット出撃など、通常の格納庫出撃以外を確認する。

### 実行履歴：2026-11-17 16:38回

- 開始16:38:43、終了 16:40:38 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の地星儀・展開図修正、直送便テスト等を保持。今回の変更は非追跡 ProbeCpuSortie.cs.txt と本記録のみ。ゲームコード変更・競合・新規採番なし。
- CPU1のポケットのみの出撃を合法固定場面で検査。通常の地図切替処理から展開図へ移り、960×540で地点91の通常出撃画面を開く。機体767・768を個別に選択して確定、赤チェックと通常の閉鎖、人間への復帰を確認。予約件数の増加なし。戦闘では指定機体が各1回参加し、クイーン参加なし。証拠は audit-legal-queen.png と下記ログ。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。16:39:09起動・作成 PID 23984、上限600秒、16:39:51終了0、警告・エラー0。同出力の bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_ 接頭辞の MILITARY_NET=1、LEGAL_QUEEN=1、QUEEN540=1、QUEEN_ESCORTS=1、POCKET_ONLY=1 で dotnet 実行。16:39:51要求・16:39:52作成 PID 3604、上限300秒、16:40:14終了0。証拠は CpuMilitary-net-pocket-{build,run}-1638.{stdout,stderr}.log。
- 次回は同じ展開図でクイーン参加ありを確認する。固定場面のため自然AIがポケット出撃を選ぶ条件、物理入力、地点11の元配置、専用直送便の実演は未確認。直送便・左右格納庫は13:41回、赤チェック後再クリック・進軍は13:22回参照。

### 実行履歴：2026-11-17 16:59回

- 開始16:59:41、終了 17:00:34 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分はDEMO-0016、直送便テストとDemo両記録。変更は本記録のみ。ゲームコード変更・競合・新規採番なし。
- 既存NAS出力の bin/DemoAuditCpuSortie/debug から dotnet DemoAuditCpuSortie.dll を実行。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_ 接頭辞の MILITARY_NET=1、LEGAL_QUEEN=1、QUEEN540=1、QUEEN_ESCORTS=1。16:59:54起動・作成 PID 30736、上限300秒、17:00:13終了0。証拠は CpuMilitary-net-queen-run-1659.{stdout,stderr}.log と audit-legal-queen.png。
- 960×540の展開図、CPU1・地点91の固定場面で通常出撃画面を開き、クイーン参加の切替と機体2体の個別選択、確定・赤チェック・閉鎖を確認。再生中の予約件数の増加なし。元のクイーン進軍1件と人間0への復帰を確認。ポケットのみは16:38回。
- 自然AIで同じ指示が生成される条件、地点11の元配置、物理入力、専用直送便の実演は未確認。次回は自然AIでポケット・クイーンを含む指示が生成される配置か、別の未巡回経路を点検する。直送便・左右は13:41回、緑赤と再クリック・進軍は13:22回等を参照。

### 実行履歴：2026-11-17 17:21回

- 開始17:21:12、終了 17:22:22 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の地星儀・展開図修正、直送便テストと両記録の差分を保持。変更は本記録のみ。ゲームコード変更・競合・新規採番なし。
- 既存NASの bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=87、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll を実行。17:21:24起動・作成 PID 13688、上限300秒、17:22:01終了0。証拠は CpuMilitary-world87-run-1721.{stdout,stderr}.log と audit-latest-computer-confirm.png／audit-latest-cpu-red.png。
- 960×540の通常UIでCPU5へ切り替え、地点93・機体4体の選択、確定後の赤チェック、人間への復帰を確認。第一軍事30指示と第二軍事22指示で専用表示への退避0。第一軍事ではポケットのみ・クイーン参加は0件、第二軍事の属性内訳は未採取。固定場面は16:38・16:59回。
- 次回はポケット・クイーンを自然に含み得る後半のAI計画か、他の未巡回経路を点検する。地点11の元配置、物理入力、専用直送便の自然実演は未確認。プライマリー・直送便・左右は12:11・13:41回、赤チェック後再クリック・進軍は13:22回参照。

### 実行履歴：2026-11-17 17:26開始回

- 開始17:26:38、終了 17:28:00 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は DEMO-0016、直送便テストとDemo両記録。変更は本記録のみ。新規採番なし。
- 既存NAS出力の bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=89、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll を実行。17:27:12起動・作成 PID 20224、上限300秒、17:27:41終了0。証拠は同ディレクトリの CpuMilitary-world89-run-172712.{stdout,stderr}.log と診断の描画・状態検査。
- 960×540の通常UIで内政再生を通過後、CPU7・地点23で2体を選択し、確定後に赤チェック。第一軍事30指示と第二軍事27指示で専用表示への退避0。人間0の内政へ復帰。第一軍事ではポケットのみ・クイーン参加は0件、第二軍事の属性内訳は未採取。
- 次回は第二軍事以降の指示属性を採取し、自然にポケット・クイーンを含む指示を優先する。固定場面は16:38・16:59回。物理入力、地点11の元配置、専用直送便の自然実演は未確認。

### 実行履歴：2026-11-17 17:40回

- 開始17:40 JST、終了 17:41:42 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の tests/DirectDeliveryUi/Program.cs と DEMO-0016。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/DirectDeliveryUi/debug で ALSOFT_DRIVERS=null、dotnet DirectDeliveryUi.dll。17:40:59要求、17:41:00作成 PID 28596、上限300秒、17:41:26終了0。証拠は DirectDelivery-run-174059.{stdout,stderr}.log と同出力の direct-*.png。再ビルドなし。
- 960×540の描画でプライマリーボタンの比率、直送便の退避・復帰、移動中の入力抑止、地星儀と展開図での連続トラッククリック、左右格納庫と予約保持を確認。診断の閉じる・Escapeも通過。
- 次回は自然Demoの専用直送便か、第二軍事以降の指示属性を点検する。物理入力、地点11の元配置、自然AIでのポケット・クイーン出撃は未確認。CPU通常UIの前回は17:26回。

### 実行履歴：2026-11-17 17:59回

- 開始17:59 JST、終了 18:00:29 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関連する既存差分は DEMO-0016 と直送便テスト。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/DemoUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=22 として dotnet DemoUi.dll を実行。17:59:45起動・作成 PID 11020、上限300秒、18:00:11終了0。証拠は DemoUi-seed22-run-175945.{stdout,stderr}.log と同出力の demo.png。再ビルドなし。
- 世界・配役・領土は1501772170/1364365674/1589266210。初回配送の論理クリック (256,998) と地図復帰を確認。地星儀・展開図切替、18 FPSの端地点移動、拒否時の退避、戦闘差し替え、エンディングからの再巡回、Demoラベル描画がPASS。解像度は論理1920×1080。
- 専用直送便の自然実演、地点11の元配置、物理入力、自然AIのポケット・クイーン出撃は未確認。次回は第二軍事以降の指示属性を採取するか、専用直送便の入口を点検する。個別の直送便退避・復帰と左右格納庫は17:40回参照。

### 実行履歴：2026-11-17 18:19回

- 開始18:19 JST、終了 18:21:28 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は DEMO-0016 と直送便テスト。変更は既存診断 ProbeCpuSortie.cs.txt の第二軍事属性ログと本記録。ゲームコード変更・新規採番なし。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。18:19:46起動・作成 PID 14740、上限600秒、18:20:29終了0、警告・エラー0。同出力 bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=89、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。18:20:29起動・作成 PID 7880、上限300秒、18:21:04終了0。証拠は NAS出力の CpuMilitary-second-attributes-build-181946 と CpuMilitary-second-attributes-world89-182029 の標準出力・標準エラー。
- 960×540の診断。第一軍事はCPU7・地点23で2体選択と確定後の赤チェック。第二軍事は自然生成27指示のうち Queen フラグ3、PocketOnly フラグ2、和平2（属性ごとの集計）。第二軍事の専用表示への退避0、人間0の内政に復帰。フラグ付き個別指示のクリック矩形・描画・戦闘参加内訳は未採取。
- 次回は同じ世界89の第二軍事で該当指示の地点と矩形、確定・赤チェック・戦闘参加を個別に追う。物理入力、地点11の元配置、専用直送便の自然実演は未確認。

### 実行履歴：2026-11-17 18:40回

- 開始18:40:12、終了 18:41:58 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は DEMO-0016。変更は既存診断 ProbeCpuSortie.cs.txt と本記録のみ。新規採番なし。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。18:40:45要求・18:40:46作成 PID 9332、上限600秒、18:40:59終了0、警告・エラー0。同出力の bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=89、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。18:41:00要求・作成 PID 28756、上限300秒、18:41:34終了0。証拠は NAS出力の CpuMilitary-featured-build-184045 と CpuMilitary-featured-world89-184100 の標準出力・標準エラー。
- 960×540の診断で、第二軍事の PocketOnly 付き2件はいずれも和平移動と判明。CPU19・地点141とCPU23・地点61が地図で選択され、通常の移動確定後に選択解除。自然生成のポケットのみの攻撃は今回の配置に現れなかった。
- CPU39のクイーン参加攻撃は、CPU39視点で地点123選択 (1255,551)、出撃選択、確定 (1415,822)、赤チェック (936,544)、閉じる (936,488) の順を確認。座標は論理値。確定と赤チェック後に描画処理を実行し、第二軍事27指示は退避0で人間0の内政へ復帰。矩形の個別採取、戦闘でのクイーン参加、物理入力、地点11の元配置、専用直送便の自然実演は未確認。次回は地点123の戦闘参加と予約の重複有無を照合する。

### 実行履歴：2026-11-17 19:00回

- 開始19:00:41、終了 19:02:56 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は DEMO-0016。変更は既存診断 ProbeCpuSortie.cs.txt と本記録。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。19:01:15起動 PID 27292、上限600秒、19:01:57終了0、警告・エラー0。同出力の bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=89、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。19:01:57要求・19:01:58作成 PID 29460、上限300秒、19:02:30終了0。証拠は NAS出力の CpuMilitary-queen-result-build-190115 と CpuMilitary-queen-result-world89-190157 の両ログ。
- 960×540の診断。地点123でCPU39の通常の出撃確定、赤チェック、閉鎖を再確認。計画予約の開始・最大・終了件数は0/0/0、記録上のクイーン進軍は1件。地点123の戦闘フレームからCPU39のクイーン機体は取得できず、実戦参加は未確認、採番保留。
- 次回は地点123に関連する遭遇・迎撃と待機兵を照合して、クイーンが登場する戦闘を特定する。物理入力、地点11の元配置、専用直送便の自然実演は未確認。

### 実行履歴：2026-11-17 19:21回

- 開始19:21:44、終了 19:23:53 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は DEMO-0016。変更は既存診断 ProbeCpuSortie.cs.txt と本記録。新規採番なし。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。19:22:15作成 PID 7624、上限600秒、19:22:57終了0、警告・エラー0。同出力の bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=89、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。19:22:57作成 PID 24880、上限300秒、19:23:31終了0。証拠は NAS出力の CpuMilitary-queen-encounters-build-192214 と CpuMilitary-queen-encounters-world89-192257 の両ログ。
- 960×540でCPU39が地点123へ出撃を確定し、赤チェック・閉鎖後に人間へ復帰。クイーン進軍1件、再生の予約件数0/0/0。前回未確認のクイーンは地点123と124の間の終了済み辺戦闘に機体ID -40 として参加し、戦闘フレームは地点124として記録された。進軍後のCPU39の現在地は124、地点123は無所属。
- 次回は自然生成のポケットのみの攻撃か専用直送便の入口を点検する。今回の辺戦闘の画面操作、物理入力、地点11の元配置、専用直送便の自然実演は未確認。

### 実行履歴：2026-11-17 19:40回

- 開始19:40:50、終了 19:42:16 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は DEMO-0016。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=94、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。19:41:11起動・作成 PID 16800、上限300秒、19:41:46終了0。証拠は CpuMilitary-second-world94-194111.{stdout,stderr}.log。
- 960×540。第一軍事39指示に続き、第二軍事35指示で専用表示への退避0、人間の内政へ復帰。クイーン参加攻撃4件では各1件の進軍と戦闘参加を照合し、予約件数0/0/0。第二軍事にポケットのみの攻撃は現れなかった。
- 次回は自然生成のポケットのみの攻撃を別配置で探すか、専用直送便の入口を点検する。物理入力、地点11の元配置、専用直送便の自然実演は未確認。

### 実行履歴：2026-11-17 19:58回

- 開始19:58:42、終了 19:59:53 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は DEMO-0016。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=93、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。19:59:00要求・作成 PID 15164、上限300秒、19:59:33終了0。証拠は CpuMilitary-world93-195900.{stdout,stderr}.log。
- 960×540。第一軍事33指示、第二軍事31指示で専用表示への退避なし、人間0の内政へ復帰。第二軍事でポケット属性付きの1件はCPU3の和平移動。CPU3視点で地点35を選択 (1300,552) し、移動確定 (707,938) 後に選択解除。座標は論理値。
- 次回は自然生成のポケットのみの攻撃を別配置で探すか、専用直送便の入口を点検する。今回の和平移動の到着確認、物理入力、地点11の元配置、専用直送便の自然実演は未確認。

### 実行履歴：2026-11-17 20:21回

- 開始20:21:14、終了 20:22:41 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の直送便テストと DEMO-0016。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/DemoInspectorUi/debug で ALSOFT_DRIVERS=null として dotnet DemoInspectorUi.dll。20:21:36要求、20:21:37作成 PID 3736、上限300秒、20:22:10終了0。証拠は DemoInspectorUi-run-202136.{stdout,stderr}.log。1080p/540pで工場6プランの矩形と実選択、インスペクター閉鎖、輸送プライマリー、通常の移動入口を描画付きで確認。
- Game1.DemoDomestic.cs の段階12は TransportOverviewButton（PrimaryActionBounds(4)）を MapOverlayClick に渡し、通常の輸送概要を開く。巡回入口の静的照合では、専用直送便のペア表示へ入る明示的な操作は確認できなかった。直送便の通常UIでの退避復帰・左右格納庫は17:40回を参照。自然実演の有無は引き続き未確認。
- 次回は専用直送便への入力分岐を通常UI側から照合する。物理入力、地点11の元配置、自然生成のポケットのみの攻撃、世界93での和平移動の到着は未確認。

### 実行履歴：2026-11-17 20:40回

- 開始20:40:42、終了 20:41:51 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は直送便テストと DEMO-0016。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/DirectDeliveryUi/debug で ALSOFT_DRIVERS=null として dotnet DirectDeliveryUi.dll。20:41:03起動・作成 PID 31272、上限300秒、20:41:32終了0。証拠は DirectDelivery-pair-run-204103.{stdout,stderr}.log と direct-*.png。960×540でプライマリーの比率、退避復帰、左右格納庫、地星儀・展開図の連続トラッククリック、予約保持を確認。
- 通常UIは Game1.MapOverlays.cs のプライマリー第4ボタン（0始まり3）から OpenDirectMap、Game1.DirectDelivery.cs の DirectMapClick で描画済みトラック矩形から OpenDirectPair へ進む。Demoの段階12は第5ボタンの輸送概要。今回の通常UI診断では自然Demoからペア画面へ入る条件は未確認。
- 次回は自然生成されたポケットのみの攻撃か、自然Demoの輸送概要後の遷移を追う。物理入力、地点11の元配置、世界93での和平移動の到着は未確認。

### 実行履歴：2026-11-17 21:01回

- 開始21:01:45、終了 21:03:24 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の直送便テストと DEMO-0016。変更は tests/DemoUi/Program.cs の状態ログと本記録。新規採番なし。
- dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。21:02:22作成 PID 30416、上限600秒、21:02:36終了0、警告・エラー0。同出力 bin/DemoUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=22 として dotnet DemoUi.dll。21:02:36作成 PID 18568、上限300秒、21:03:02終了0。証拠は DemoUi-natural-delivery-build-210222 と DemoUi-natural-delivery-seed22-210236 の両ログ、demo.png。
- 初回配送までの自然巡回を1秒刻みで観測。世界・配役・領土は1501772170/1364365674/1589266210。輸送概要、直送便地図・ペア画面のフラグは観測時すべてFalse。初回配送に到達し、通常のDemo回帰も終了0。カーソルは論理1920×1080で (256,998)。状態ログは初回配送までで、後続の配送には固定場面を含む。
- 次回は輸送概要が自然に開く内政の配置を1秒未満で追う。自然Demoの直送便ペア、物理入力、地点11の元配置、自然生成のポケットのみの攻撃は引き続き未確認。

### 実行履歴：2026-11-17 21:19回

- 開始21:19:16、終了 21:20:38 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の直送便テストと DEMO-0016。変更は tests/DemoUi/Program.cs の追跡ログと本記録。新規採番なし。
- dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。21:19:43起動 PID 2672、上限600秒、21:19:56終了0、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=22 で同出力 bin/DemoUi/debug の dotnet DemoUi.dll。21:19:56起動 PID 28200、上限300秒、21:20:17終了0。証拠は DemoUi-followup-build-211943 と DemoUi-followup-seed22-211956 の両ログ、demo.png。
- 初回配送後にテストが地図へ戻した場面から、地星儀・展開図切替と内政再開を18 FPS相当で観測。輸送概要、直送便地図、ペア画面のフラグはいずれもFalse。論理1920×1080。続く軍事・戦闘等の回帰はPASS。自然に配送から地図へ戻る操作そのものは今回の観測外。
- 次回は段階12に到達する内政の前提を確認する。専用直送便の自然実演、自然生成のポケットのみの攻撃、物理入力、地点11の元配置は未確認。

### 実行履歴：2026-11-17 21:39回

- 開始21:39:46、終了 21:40:23 JST。部分点検完了（コード照合）。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は DEMO-0016、DemoUi と直送便診断。変更は本記録のみ。新規採番なし。
- 輸送概要を狙う段階12への入口を照合。Game1.DemoDomestic.cs の段階18は移動ボタン処理後に12を指定し、段階11は内政以外で12へ分岐する。Game1.DemoDomesticActions.cs の DemoNodePoint も自陣営の戦闘ロック外地点がなければ12を指定する。段階12の実入力は内政フェーズとチュートリアル条件で有効な場合に輸送概要を開く。工場計画を選んだ段階10や出撃予約に成功した段階36は19のターン終了へ進む。
- 新規実行なし。通常輸送概要のボタンと描画は20:21回、直送便ペアは20:40回の通常UI診断を参照。21:19回の論理1920×1080の観測ではいずれも開かなかった。次回は有効な内政状態で段階18から12へ進んだ直後の矩形・画面を採取する。専用直送便の自然実演、物理入力、自然生成のポケットのみの攻撃は未確認。

### 実行履歴：2026-11-17 21:59回

- 開始21:59:45、終了 22:02:27 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/DemoInspectorUi/Program.cs の状態ログと本記録。ゲームコード変更・新規採番なし。
- dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/demo-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal 後、同出力の bin/DemoInspectorUi/debug で ALSOFT_DRIVERS=null、dotnet DemoInspectorUi.dll。初回ビルド21:00ではなく22:00:13 PID 26184→22:00:26終了0、実行22:00:27 PID 8020→22:00:57終了 -532462766。診断が次段階を12と仮定して停止。ログ DemoInspectorUi-transport-followup-{build,run}。
- 診断の仮定を取り除き、再ビルド22:01:22 PID 2760→22:01:34終了0、警告・エラー0。再実行22:01:34 PID 23088→22:02:05終了0。上限は各ビルド600秒・各診断300秒。証拠は DemoInspectorUi-stage-build-220122 と DemoInspectorUi-stage-run-220134 の両ログ。
- 21:39回の入口説明を訂正。段階18は内部で12を代入した後、共通の末尾加算により実際は13の移動先選択へ進む。固定場面の実描画診断で stage=13、overview=False、movement=True。輸送概要の独立検証、工場6プランと閉鎖等は1080p/540pの既存診断でPASS。自然Demoの直送便ペアは未確認。次回は他の未確認経路（自然生成のポケットのみの攻撃など）を点検する。

### 実行履歴：2026-11-17 22:20回

- 開始22:20:45、終了 22:21:50 JST。部分点検完了。HEAD 0a14fa16c243eee52032c9a7a1e1840c28cca7ae。開始時の差分はチュートリアル診断・記録と利用者メモ。今回の変更は本記録のみ。新規採番なし。
- 既存NAS出力 bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=92、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。22:21:02作成 PID 29296、上限300秒、22:21:32終了0。ログは CpuMilitary-world92-222102.{stdout,stderr}.log。解像度960×540。診断は既存ビルドを使用。
- 第一軍事44件、第二軍事32件で専用画面への退避なし。ポケット属性付きの1件はCPU9の和平移動で、通常UIで地点14選択 (1275,552) と移動確定 (707,938) を通過し、選択解除後に人間0の内政へ復帰。座標は論理値。
- 次回は自然生成されたポケットのみの攻撃を別配置で探す。今回の対象地点への到着、物理入力、自然Demoの直送便ペアは未確認。

### 実行履歴：2026-11-17 22:39回

- 開始22:39:16、終了 22:40:25 JST。部分点検完了。HEAD d5d2c35f658bc05c81d5444bc77509a35b91df90。開始時の差分はチュートリアル点検記録のみ。今回の変更は本記録のみ。新規採番なし。
- 既存NAS出力 bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=95、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。22:39:38作成 PID 11020、上限300秒、22:40:07終了0。証拠は CpuMilitary-world95-223937.{stdout,stderr}.log。解像度960×540。
- 第一軍事19件、第二軍事31件で専用画面への退避なし。第二軍事のクイーン付き攻撃6件で各進軍1件・戦闘参加・再生中の予約増加なしを確認。人間0の内政へ復帰。ポケットのみの攻撃は発生せず、物理入力と自然Demoの直送便ペアも未確認。
- 次回は自然生成のポケットのみの攻撃を別配置で探すか、未確認の直送便経路を点検する。

### 実行履歴：2026-11-17 22:58回

- 開始22:58:45、終了 22:59:53 JST。部分点検完了。HEAD 9e4d089af14f8203205f85cdc46f93aeaa3ba95a。開始時差分は点検記録2件と利用者メモ。今回の変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/DemoAuditCpuSortie/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=96、FOLDERVERSE_AUDIT_SECOND_MILITARY=1 として dotnet DemoAuditCpuSortie.dll。22:59:03作成 PID 6964、上限300秒、22:59:35終了0。証拠は CpuMilitary-world96-225903.{stdout,stderr}.log。診断の設定は960×540、再ビルドなし。
- 第一軍事35件、第二軍事27件で専用画面への退避なし。クイーン付き攻撃2件の各進軍1件・戦闘参加と、再生中の予約増加なし、人間0の内政復帰を確認。
- 自然生成のポケットのみの攻撃、自然Demoの直送便ペア画面、物理入力は未確認。次回は直送便の自然巡回か別配置でのポケット攻撃を調べる。

### 実行履歴：2026-11-17 23:30回

- 開始23:30:36、終了 23:32:11 JST。部分点検完了。HEAD 9e4d089af14f8203205f85cdc46f93aeaa3ba95a。開始時の関係差分はプリマプラナ実装の共有地図・入力と点検記録。今回の変更は本記録のみ。新規採番なし。
- Game1.DemoDomestic.cs の段階11・16の閉じる、段階12の輸送概要、段階19のターン終了、段階20の緑チェック、Game1.DemoChecks.cs の段階35・36の赤チェックと閉じるが既存UI矩形の中心を参照することを静的照合。現行差分の地図切替と自動出撃はプリマプラナ条件付き。
- 既存NAS出力 demo-audit/bin/DirectDeliveryUi/debug で ALSOFT_DRIVERS=null、dotnet DirectDeliveryUi.dll。23:31:24作成 PID 30204、上限300秒、23:31:48終了0。DirectDeliveryUi-followup-233124.{stdout,stderr}.log。960×540の確認を含み、ボタン退避復帰、移動中入力抑止、ペアへの入退場、左右格納庫、予約維持と描画がPASS。現行コードでの再ビルドなし。
- 次回は現行差分込みのビルドで自然Demoの直送便ペア画面か、別配置のポケットのみの攻撃を点検する。今回の自然巡回、物理マウス入力、チェックの実描画は未確認。


### 2026-11-18 00:00 JST Demo audit

- HEAD f5f8109d. Start 00:00:37 JST; end 00:01:11. Partial static check. Initial working changes were the two audit logs and user notes. This round only appended this entry.
- Compared the merged CubeNet.Routes.cs change with DemoDomestic stages 11/12/16 and DemoChecks stage 36. The merged hunk adds water coloring, while the Demo stages still use InspectorClose and TransportOverviewButton rectangles. No new defect reproduced.
- The prior NAS-bound dotnet PIDs 25828 and 18852 were still listed. The preceding tutorial audit had no result from its NAS probe. No new build or UI run this round. Merged render, synthetic and physical inputs, natural direct-delivery pair and pocket-only attack remain unverified. Resume on merged build with a responsive NAS or an isolated local output after checking prior process/output status.


### 2026-11-18 00:21点検回

- 開始00:21:37、終了 00:22:42 JST。部分点検完了。HEAD f5f8109d。開始時差分は両点検記録と利用者メモ。変更は本記録のみ。新規採番なし。
- 前回NAS待ちが続いていたため、チュートリアル点検が使用済みの期限11月18日の分離出力 artifacts/tmp/delete-after-2026-11-18/tutorial-audit-local を共有ビルドの再利用先として使用。dotnet build tests/DirectDeliveryUi/DirectDeliveryUi.csproj --artifacts-path <同出力> --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v:minimal。00:22:07 PID 15100、上限600秒、00:22:14終了0・警告とエラー0。
- 同出力 bin/DirectDeliveryUi/debug で ALSOFT_DRIVERS=null、dotnet DirectDeliveryUi.dll。00:22:14 PID 33388、上限300秒、00:22:23終了0。DirectDelivery-build-002207 と DirectDelivery-run-002214 の両ログ。960×540を含む描画と入力で、プライマリーボタンの退避復帰、移動中入力の抑止、左右格納庫、連続トラック選択、閉じる／Escapeと予約維持を確認。
- 次回はマージ後の自然Demoの直送便ペア画面、または別配置のCPUによるポケットのみの攻撃を追う。今回の試験では自然巡回、物理マウス操作、緑赤チェックの描画を未確認。


### 2026-11-18 00:40 JST Demo audit

- Start 00:40:38 JST; end 00:41:38. HEAD f5f8109d. Initial changes were the two audit logs and user notes. Partial audit complete, only this log changed.
- Reused the local tutorial-audit-local output under artifacts/tmp/delete-after-2026-11-18 because NAS output had stalled. dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path <same output> --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v:minimal. PID 6344 00:41:04 to 00:41:10, limit 600s, exit 0, no warnings or errors. dotnet DemoUi.dll, ALSOFT_DRIVERS=null and FOLDERVERSE_DEMO_TEST_SEED=25, PID 31872 00:41:10 to 00:41:22, limit 300s, exit 0. Evidence: DemoUi-build-004104 and DemoUi-seed25-004110 out/err, demo.png.
- Natural first transport entered and map returned; overview, directMap and directPair flags were false on both observations. Globe/net, edge relocation, ending loop and label drawing passed. No new defect reproduced. Next: find a naturally reached direct-delivery pair or a different CPU pocket-only attack. Those paths, physical input and check logic were not verified in this run.


### 2026-11-18 00:59 JST Demo audit

- Start 00:59:38, end 01:00:31. HEAD f5f8109d. Initial working changes were audit logs and user notes. Only this log changed. No new defect.
- Reused the 00:40 merged DemoUi build in artifacts/tmp/delete-after-2026-11-18/tutorial-audit-local/bin/DemoUi/debug; no rebuild. ALSOFT_DRIVERS=null, FOLDERVERSE_DEMO_TEST_SEED=26, dotnet DemoUi.dll. PID 740, 01:00:05 to 01:00:14, limit 300s, exit 0. Logs DemoUi-seed26-010005 out/err and demo.png.
- First transport and subsequent map return passed. overview/directMap/directPair were false at both observations. Globe/net and edge relocation at 18 FPS, fallback, ending loop and demo label passed. Natural direct pair, CPU pocket-only attack, check modes and physical input remain unverified. Next: inspect the Demo route into direct delivery or CPU pocket attack. D free 1300271104 bytes before run; reuse outputs and avoid additional copied build trees.


### 2026-11-18 01:19 JST Demo audit

- Start 01:19:08, end 01:19:37. HEAD f5f8109d. Partial static inspection, game code unchanged. Initial differences were audit logs and user notes; only this log appended.
- Followed DemoDomestic stage 12 into MapOverlayClick and primary action 4. This opens the transport overview. The examined natural Demo path has no explicit OpenDirectPair call, so earlier false directPair observations do not establish correct pair-entry behavior. Inspector close stages continue using the actual InspectorClose rectangle. No new defect reproduced; no duplicate issue ID.
- No UI run this round. Next audit: trace later overview and direct-delivery input transitions or use the existing CPU sortie diagnostic for pocket-only attacks. Natural pair entry, physical input and full on/off/skip behavior remain unverified.


### 2026-11-18 01:40 JST Demo audit

- Start 01:40:38, end 01:41:12. HEAD f5f8109d. Partial static check complete. Initial differences: audit logs, user notes and the tutorial diagnostic turn-count option. Game code unchanged, only this log appended.
- Traced TransportOverviewClick: route/node clicks select a transport source and instruction editor. DirectDeliveryClick opens OpenDirectPair only from a truck hit in the separate direct-map overlay. Thus the examined stage-12 transport overview path does not provide evidence for a natural direct-pair entry. No new reproduced click defect, no issue ID.
- No UI run. Next audit should inspect ordinary direct-map primary action and Demo stage coverage together; natural direct-pair entry, CPU pocket-only attack, physical input and on/off/skip comparison remain unverified. Existing DirectDeliveryUi coverage remains limited to its explicit test scenario.


### 2026-11-18 01:59 JST Demo audit

- Start 01:59:08, end 01:59:24. HEAD f5f8109d. Partial static inspection complete. Working changes: audit logs, user notes and existing tutorial diagnostic. Game code unchanged; only this log appended.
- Compared primary action 3 (OpenDirectMap) with natural Demo stage 12 (TransportOverviewButton/primary action 4). The examined stages have no explicit primary-action-3 click before truck selection. DirectDeliveryClick needs an active direct-map truck hit to open the pair. This narrows the missing coverage to the direct-map entry/selection stages; no reproduced coordinate defect or issue ID.
- No UI run. Next inspect Demo stage coverage for direct-map action and a visible truck, then verify pair arrival/return. Natural pair, CPU pocket-only attacks, physical input and on/off/skip remain unverified. Existing explicit DirectDeliveryUi tests passed at 00:21.


### 2026-11-18 02:19 JST Demo audit

- Start 02:19:09, end 02:19:25. HEAD f5f8109d. Partial static check; game code unchanged. Initial working differences: audit records, user notes and the tutorial diagnostic. Only this record appended.
- Rechecked natural Demo transport stages 11..19 against primary action 3 and 4: stage 12 explicitly selects transport overview, while no explicit direct-map/truck stage exists in this examined branch. Earlier short natural runs therefore did not exercise the two-inspector direct-pair screen. This is an unimplemented or unverified demonstration route, rather than evidence that its click positions work. No newly reproduced coordinate bug or new ID.
- No UI run. Resume with an actual naturally reached truck selection or inspect other Demo/computer-playback branches for such an entry. Existing manual DirectDeliveryUi path passed, but natural pair, pocket-only attack, physical input and on/off/skip coverage remain unverified.


### 2026-11-18 02:39 JST Demo audit

- Start 02:39:09, end 02:39:35. HEAD f5f8109d. Partial static check complete. Initial changes: audit logs, user notes and the tutorial diagnostic. Game code unchanged; this record only.
- Expanded direct-map/pair entry search to all Game1.Computer*.cs and Game1.Demo*.cs. No explicit OpenDirectMap/OpenDirectPair calls found. Calls through normal input may still reach those methods; therefore this search does not establish total absence of the feature. The known natural stage-12 path selects the separate transport overview.
- No new coordinate defect; no UI run or new build. D free 1219756032 bytes, so retain existing output reuse. Next audit should use existing CPU operation diagnostics to capture normal-input direct-map or pocket-only paths. Natural pair and on/off/skip/physical coverage remain unverified.


### 2026-11-18 02:59 JST Demo audit

- Start 02:59:10, end 03:00:05. HEAD f5f8109d. Partial audit complete. Initial differences: audit records, user notes and tutorial diagnostic. Only this log changed, no product change or new defect.
- Reused existing DirectDeliveryUi build from local isolated tutorial-audit-local. ALSOFT_DRIVERS=null, dotnet DirectDeliveryUi.dll. PID 34264 02:59:35 to 02:59:48, 300s limit, exit 0. Evidence DirectDelivery-followup-025935.out/err and existing direct-*.png captures. No rebuild due to existing valid merged output and D free 1.19GB.
- Explicit normal-input scenario passed button frame-out/in, hidden/moving click suppression, left/right hangars, truck pair entry/return, Escape and reservation preservation, including 960x540 rendering. Natural Demo reachability, CPU pocket-only attacks and actual physical mouse/keyboard remain unverified. Next use existing CPU operation diagnostic for the latter paths rather than treating this explicit pair test as natural Demo coverage.


### 2026-11-18 03:18 JST Demo audit

- Start 03:18:10, end 03:18:26. HEAD f5f8109d. Partial static check only. Initial working differences: audit logs, user notes and tutorial diagnostic. Game code unchanged; this record only.
- Reconfirmed green check/close in DemoDomestic and red check/close in DemoChecks use NodeCheckButton.Center and InspectorClose.Center. Static references establish shared rectangle use, not rendered cursor alignment or resulting checks. Existing CPU sortie probe project presence was checked for a later isolated run.
- No new defect or UI execution this round. Next use CPU sortie/check diagnostic on existing merged output with capacity-aware reuse. Natural direct-pair entry, pocket-only attack, check modes after clicking, turn-end cleanup and physical input remain unverified in this round.


### 2026-11-18 03:41 JST Demo audit

- Start 03:41:11, end 03:41:37. HEAD f5f8109d. Partial static check. Initial differences: audit logs, user notes and tutorial diagnostic. Game unchanged; this log only.
- Located existing DemoAuditCpuSortie project and probe options for future world/second-military conditions. The project currently references the game and copies runtime content per output. D free 1131741184 bytes, so did not create another content copy this round. Existing NAS output still needs connection/result recovery before reuse.
- No UI execution or new defect. Next: run the CPU probe with shared or existing valid output and trace pocket-only attack plus human return. Natural direct-pair, physical input and on/off/skip coverage remain unverified. Existing DirectDeliveryUi passing scenario cannot substitute for these CPU paths.


### 2026-11-18 04:01:13.436621 JST Demo audit

- Start 2026-11-18 04:01:03.779216, end 2026-11-18 04:01:13.436621. HEAD f5f8109d. Reused isolated DemoUi build, seed27, ALSOFT_DRIVERS=null. PID 28760, limit300s, exit 0. Logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\DemoUi-seed27-latest.out and D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\DemoUi-seed27-latest.err.
- Partial audit completed. Game unchanged. Natural transport/map return, globe/net and ending assertions exercised by existing diagnostic. Natural direct pair, CPU pocket-only attack, on/off/skip and physical input remain unverified. Next: CPU probe with existing shared content.


### 2026-11-18 04:20:09.927200 JST Demo audit

- Start/end 2026-11-18 04:20:09.927200 JST. HEAD f5f8109d. Static partial audit completed, game unchanged; audit logs, user notes and tutorial diagnostic differences preserved.
- CPU probe already supports LEGAL_QUEEN, POCKET_ONLY and QUEEN540 environment options with FOLDERVERSE_AUDIT prefix. This is a constructed legal-queen scenario, so it cannot establish natural pocket-only reachability. Content resolves relative to executable; next isolated probe can reuse an existing valid runtime Content tree instead of copying another tree. No build/UI run or new defect this round.
- Next: prepare probe output with shared existing Content, run legal pocket-only control then distinguish natural AI orders and human return. Natural direct-pair, physical input and full on/off/skip remain unverified.


### 2026-11-18 04:42:45.573057 JST Demo audit

- Start/end 2026-11-18 04:42:45.573057 JST. HEAD f5f8109d. Static partial audit complete. Game unchanged, existing audit/user-note/tutorial diagnostic differences retained.
- Important probe precondition: POCKET_ONLY alone leaves escortIds empty unless QUEEN_ESCORTS=1 is also supplied. A zero-robot fixture cannot validate real pocket robot clicks or single deployment. Next fixed control must combine LEGAL_QUEEN=1, POCKET_ONLY=1, QUEEN_ESCORTS=1, QUEEN540=1 (FOLDERVERSE_AUDIT prefix) and check each of the two IDs exactly once, no queen, red check and human return.
- No UI run/new defect. Natural AI pocket-only reachability, direct-pair Demo, on/off/skip and physical input remain unverified. Next prepare minimal probe assembly using existing runtime Content rather than a new copied tree.


### 2026-11-18 05:02:33.741773 JST Demo audit

- HEAD f5f8109d. Game unchanged. Reuse existing DemoUi runtime Content/output for CPU probe; BuildProjectReferences=false, existing merged game assembly. C free206GB, D free1.07GB before run; no separate Content tree. Fixed pocket-only control LEGAL_QUEEN/POCKET_ONLY/QUEEN_ESCORTS/QUEEN540=1.
- build 2026-11-18 05:02:23.326146 to 2026-11-18 05:02:26.348579 PID 30444 limit 600s exit 0; command ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:OutputPath=D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '-v:minimal']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pocket-shared-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pocket-shared-build.err
- run 2026-11-18 05:02:26.349680 to 2026-11-18 05:02:33.741773 PID 4352 limit 300s exit 0; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pocket-shared-run.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pocket-shared-run.err
- Partial audit; natural pocket-only reachability, natural direct-pair, on/off/skip and physical input remain unverified. Review logs before classifying any assertion as product defect.


### 2026-11-18 05:20:46.429919 JST Demo audit

- Start 2026-11-18 05:20:43.285197, end 2026-11-18 05:20:46.429919. HEAD f5f8109d. Game unchanged; reused shared CPU probe without build. dotnet bin/DemoUi/debug/DemoAuditCpuSortie.dll under existing tutorial-audit-local; ALSOFT_DRIVERS=null; LEGAL_QUEEN/POCKET_ONLY/QUEEN_ESCORTS/QUEEN540/MILITARY_NET=1 with FOLDERVERSE_AUDIT prefix. PID 18888, limit300s, exit 0, TIMEOUT=False. Logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pocket-net-run.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pocket-net-run.err.
- Fixed 960x540 pocket-only unfold control; natural reachability and physical input remain separate/unverified. On/off/skip and natural direct-pair still unverified. Partial audit completed; inspect any failed assertion before assigning a defect.


### 2026-11-18 05:39:26.963702 JST Demo audit

- Start 2026-11-18 05:39:15.130424, end 2026-11-18 05:39:26.963702. HEAD f5f8109d. Game unchanged. Existing shared CPU probe reused without build: dotnet bin/DemoUi/debug/DemoAuditCpuSortie.dll in tutorial-audit-local. ALSOFT_DRIVERS=null; WORLD84/SECOND_MILITARY1/MILITARY_NET1/CONFIRM_COMPLETION normal (FOLDERVERSE_AUDIT prefix). PID 9652 limit300s exit 0 TIMEOUT=False. Logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu84-net-normal.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu84-net-normal.err.
- Partial merged-HEAD regression of ordinary CPU military map/picker/check/return. Next compare skip/off with same output; this world does not establish natural pocket-only coverage. Natural direct pair and physical input remain unverified.


### 2026-11-18 06:01:43.230268 JST Demo audit

- HEAD f5f8109d. Game unchanged; shared existing CPU probe/output reused, no build. World84, second military, normal unfold transition. Compared CONFIRM_COMPLETION skip/off to prior normal, with FOLDERVERSE_AUDIT prefix; ALSOFT_DRIVERS=null, dotnet existing bin/DemoUi/debug/DemoAuditCpuSortie.dll.
- skip: 2026-11-18 06:01:20.666801 to 2026-11-18 06:01:32.930177, PID 29784, limit300s, exit0, TIMEOUT=False; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu84-net-skip.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu84-net-skip.err
- off: 2026-11-18 06:01:32.932327 to 2026-11-18 06:01:43.228365, PID 16572, limit300s, exit0, TIMEOUT=False; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu84-net-off.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu84-net-off.err
- Marches/cancellations/battle participant line equality to normal: {'skip': True, 'off': True}. Review completion lines for human return/planned0. Partial audit complete. Natural pocket-only, natural direct pair and physical input remain unverified. Next target natural pocket-only AI order or check mode/turn-end cleanup using shared runtime.


### 2026-11-18 06:21:30.484470 JST Demo audit

- HEAD f5f8109d. Game unchanged; DemoChecksUi built into existing DemoUi shared runtime, BuildProjectReferences=false, existing merged assembly; no new Content tree. ALSOFT_DRIVERS=null.
- build 2026-11-18 06:21:17.237259 to 2026-11-18 06:21:20.268869 PID 2388 limit600s exit0 TIMEOUT=False; ['dotnet', 'build', 'tests/DemoChecksUi/DemoChecksUi.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:OutputPath=D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '-v:minimal']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\checks-shared-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\checks-shared-build.err
- run 2026-11-18 06:21:20.269428 to 2026-11-18 06:21:30.483857 PID 9204 limit300s exit0 TIMEOUT=False; ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoChecksUi.dll']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\checks-shared-run.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\checks-shared-run.err
- Partial check-mode/input regression; natural direct pair, natural pocket-only and physical input remain unverified. Review checks-shared-run logs before classifying any failure.


### 2026-11-18 06:42:41.113224 JST Demo audit

- HEAD f5f8109d. Game unchanged; DemoInspectorUi built with BuildProjectReferences=false into shared DemoUi runtime, no new Content tree. Commands: dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path tutorial-audit-local --disable-build-servers -p:UseAppHost=false -p:BuildProjectReferences=false -p:OutputPath=<existing DemoUi/debug> --source artifacts/nuget-feed --source local NuGet cache; dotnet corresponding DLL, ALSOFT_DRIVERS=null.
- build 2026-11-18 06:42:22.401781 to 2026-11-18 06:42:25.093425 PID33080 limit600s exit0 TIMEOUT=False; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\inspector-shared-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\inspector-shared-build.err
- run 2026-11-18 06:42:25.093846 to 2026-11-18 06:42:41.112419 PID34424 limit300s exit0 TIMEOUT=False; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\inspector-shared-run.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\inspector-shared-run.err
- Scope: factory plans/office/animated close/transport primary/my-country movement/counts,1080p/540p real rendering. Partial audit; natural direct pair/pocket-only and physical input remain unverified. Next use natural CPU pocket order or direct-pair reachability rather than repeat these fixtures.


### 2026-11-18 07:01:30.923743 JST Demo audit

- Start 2026-11-18 07:01:17.773611, end 2026-11-18 07:01:30.923743. HEAD f5f8109d. Game unchanged; reused shared CPU diagnostic without build. WORLD103/SECOND_MILITARY1/CONFIRM_COMPLETION normal, FOLDERVERSE_AUDIT prefix; ALSOFT_DRIVERS=null; dotnet existing tutorial-audit-local/bin/DemoUi/debug/DemoAuditCpuSortie.dll. PID14496 limit300s exit0 TIMEOUT=False. Logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-normal.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-normal.err.
- Partial alternate-world CPU map/picker regression. Natural pocket-only coverage requires an actual pocket order; review Unsupported attributes count, not just PASS. Natural direct pair and physical input remain unverified. Next follow any pocket-bearing AI order or inspect source conditions if absent.


### 2026-11-18 07:19:37 JST Demo static audit

- HEAD f5f8109d; start 07:18, end 2026-11-18 07:19:37. Game unchanged, no build/UI process launched. Initial instructions, both records and Git differences reviewed; user changes retained. Partial static audit completed.
- Reviewed cpu103-normal.out: visible50, queen0/pocket0, sourceMissing0/noFighters0; target137 selected, picker1, three individual card clicks, human0 Domestic planned0. This is ordinary military coverage only; no natural pocket order occurred.
- ComputerJointAttacks.cs:59-72 jointly plans pocket escorts when at least two eligible faction members can meet required force; 80-94 mixes insufficient ground and pocket forces; 103-114 plans individual retinue assault or friendly frontward movement. These paths emit Queen=true/PocketOnly=true; they do not establish the Queen=false pocket-only case tested by the fixed fixture. Game1.ComputerMarches supports both forms. No defect confirmed.
- Next audit: follow naturally planned retinue orders across later turns and distinguish queen participation from pocket-only escort selection; avoid repeated first-turn seeds without such orders. Natural direct-pair route, full remaining Demo traversal and physical input remain unverified. No new DEMO ID; prior fixed tests do not close these gaps.


### 2026-11-18 07:40:56.295885 JST Demo audit

- Start 2026-11-18 07:40:42.447208, end 2026-11-18 07:40:56.295885 JST; HEAD f5f8109d. Game unchanged; shared existing CPU probe reused, no build. WORLD103/SECOND_MILITARY1, no CONFIRM_COMPLETION early exit; ALSOFT_DRIVERS=null. Command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; PID34592 limit300s exit0 TIMEOUT=False; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-second.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-second.err.
- Partial later-military audit; inspect actual Queen/PocketOnly attributes before claiming coverage. Natural direct-pair and physical input remain unverified.

- 2026-11-18 07:41:19.251444 JST outcome: second military visible38, queen2/pocket1/peaceful1, fallbacks0; player0 Domestic. Natural CPU29 pocket+queen peaceful move to154 selected target at(1244,553), then ordinary movement confirm(707,938). Natural CPU31 queen assault35 selected at(1308,552), picker1, confirm(1415,822), red check(936,544), close(936,488); one queen march, queen unit-32 in battle35, reservations0/0/0. This covers natural queen assault and peaceful pocket movement, not Queen=false pocket-only combat. Actual battle targetOwner8: no claim of conquest success. Next compare these later-turn orders with skip/off and inspect natural combat pocket-only if emitted. Partial audit completed; physical input and natural direct-pair remain unverified.


### 2026-11-18 08:02:20.926534 JST Demo static audit

- Start08:01 end2026-11-18 08:02:20.926534 JST; HEAD f5f8109d. Initial instructions/records/differences checked, product unchanged; no UI run/build. Partial static audit complete.
- Direct-pair entry trace: Game1.DemoDomestic stage12 aims at TransportOverviewButton.Center then MapOverlayClick, stage15 uses MovementClick. Normal OpenDirectPair entry is a truck hit in Game1.DirectDelivery.cs:63, pair-map hit in DirectPairMap.cs:90 or long-delivery hit in LongDelivery.cs:45. No dedicated truck/pair selection was found in the inspected Demo files. This is route coverage evidence, not a confirmed inaccessible UI or click defect.
- Next natural Demo inspection must observe whether transport overview stage transitions ever open direct-map/truck and select a pair; fixed DirectDeliveryUi passing does not establish a natural teaching path. Prior second-military run completed and exited0; later-turn skip/off comparison still requires a diagnostic path that does not early-exit at first confirmation. No new DEMO ID. Natural combat pocket-only, direct-pair and physical input remain unverified.


### 2026-11-18 08:21:59.565350 JST Demo audit

- Start 2026-11-18 08:21:45.358116 end 2026-11-18 08:21:59.565350 JST HEAD f5f8109d. Game unchanged; existing shared probe reused. WORLD103 SECOND_MILITARY1 MILITARY_NET1, ALSOFT_DRIVERS=null; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; PID3200 limit300s exit0 TIMEOUT=False; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-second-net.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-second-net.err.
- Partial unfolded-map later-military audit; inspect actual order attributes before claiming pocket combat coverage. Natural direct-pair and physical input remain unverified.

- 2026-11-18 08:22:20.326180 JST outcome: exit0, second military38 queen2/pocket1/peaceful1, fallbacks0, player0 Domestic. Same CPU29 target154 movement and CPU31 target35 queen assault/red/close, queen march1, reservations0/0/0 as prior control. MILITARY_NET flag establishes initial ordinary map transition only; identical second-stage coordinates do not prove second batch retained unfolded view. Next log _statusGlobe at later featured orders and use ordinary transition there before claiming later unfolded coverage. Skip/off remains unverified for second batch; natural direct-pair and physical input unchanged. Partial audit complete.

- CPU103 mode diagnostic build 2026-11-18 08:41:49.563994 to 2026-11-18 08:41:52.366976 JST PID33708 limit600s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:OutputPath=D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '-v:minimal']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-mode-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-mode-build.err.

- CPU103 mode diagnostic run 2026-11-18 08:41:52.367375 to 2026-11-18 08:42:09.279109 JST PID15340 limit300s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-mode-run.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\cpu103-mode-run.err.


### 2026-11-18 08:42:29.472457 JST Demo mode audit outcome

- Start08:41 end2026-11-18 08:42:29.472474 JST HEAD f5f8109d. Game unchanged; existing untracked CPU probe adds globe flag to featured-order logs. Shared runtime used, no Content copy. Build/run results above; no TIMEOUT. WORLD103 SECOND_MILITARY1 MILITARY_NET1.
- Evidence: Second featured order 21: ruler=29 viewed=29 globe=True target=154 queen=True pocket=True peaceful=True stage=1->2 click={X:1244 Y:553} selected=154 picker=-1 red=False | Second featured order 21: ruler=29 viewed=29 globe=True target=154 queen=True pocket=True peaceful=True stage=2->6 click={X:707 Y:938} selected=-1 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=1->2 click={X:1308 Y:552} selected=35 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=2->3 click={X:640 Y:732} selected=35 picker=1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=3->3 click={X:640 Y:732} selected=35 picker=1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=3->4 click={X:1415 Y:822} selected=35 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=4->5 click={X:936 Y:544} selected=35 picker=-1 red=True | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=5->6 click={X:936 Y:488} selected=35 picker=-1 red=True | Second military visible=38 fallbacks=0 player=0 phase=Domestic.
- Next use ordinary map transition after second batch begins if globe=True; initial MILITARY_NET does not guarantee persistence. Later-turn skip/off, natural pocket-only combat and direct-pair still unverified; physical input unverified. Partial audit completed, no new DEMO ID.

- Second-net diagnostic build 2026-11-18 09:06:34.433909 to 2026-11-18 09:06:37.441456 JST PID4812 limit600s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:OutputPath=D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '-v:minimal']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-net-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-net-build.err.

- Second-net diagnostic run 2026-11-18 09:06:37.441946 to 2026-11-18 09:06:52.608256 JST PID29184 limit300s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-net-run.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-net-run.err.


### 2026-11-18 09:07:13.799245 JST Demo audit outcome

- Start09:05 end2026-11-18 09:07:13.799261 JST HEAD f5f8109d. Game unchanged; existing diagnostic adds SECOND_NET option using normal map transition after second military begins; runtime Content reused. WORLD103 SECOND_MILITARY1 SECOND_NET1; build/run results above, no TIMEOUT.
- Evidence: Second featured order 21: ruler=29 viewed=29 globe=True target=154 queen=True pocket=True peaceful=True stage=1->2 click={X:1244 Y:553} selected=154 picker=-1 red=False | Second featured order 21: ruler=29 viewed=29 globe=True target=154 queen=True pocket=True peaceful=True stage=2->6 click={X:707 Y:938} selected=-1 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=1->2 click={X:1308 Y:552} selected=35 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=2->3 click={X:640 Y:732} selected=35 picker=1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=3->3 click={X:640 Y:732} selected=35 picker=1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=3->4 click={X:1415 Y:822} selected=35 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=4->5 click={X:936 Y:544} selected=35 picker=-1 red=True | Second featured order 23: ruler=31 viewed=31 globe=True target=35 queen=True pocket=False peaceful=False stage=5->6 click={X:936 Y:488} selected=35 picker=-1 red=True | Second queen result: ruler=31 target=35 marches=1 queens=-32 reservations=0/0/0 | Second military visible=38 fallbacks=0 player=0 phase=Domestic.
- Partial audit completed. These natural queen assault and peaceful pocket orders now have explicit map-mode evidence; combat pocket-only, second-batch skip/off, natural direct-pair and physical input remain unverified. Next compare later-batch normal/skip/off without first-batch early exit. No new DEMO ID.

- 2026-11-18 09:07:29.515973 JST qualification: featured orders still globe=True despite successful second-batch initial unfold; ruler switching restores globe before CPU29/31. Therefore this run does not cover their unfolded-map execution. Next apply normal transition at the featured ruler/order stage0 after view switch, then verify globe=False at actual selection. Previous paragraph map-mode evidence means observed globe mode only.


### 2026-11-18 09:19:15.980750 JST Demo static audit

- Start09:18 end2026-11-18 09:19:15.980750 JST HEAD f5f8109d; specified instructions/records/differences checked, product unchanged, no build/UI run. Partial static audit completed.
- Game1.ComputerMarches.cs:67-71 changes viewed ruler with ShowComputerView as the instruction advances, before next stage0. Thus a single SECOND_NET transition at batch start cannot guarantee later rulers remain unfolded, consistent with measured globe=True atCPU29/31. Stage0 opens movement/war inspector and focuses target; next audit should transition once at each featured order stage1 before target click, after ruler selection and inspector opening. Log globe=False at actual stage1-to2.
- No new defect or DEMO ID. Second-batch unfolded queen/pocket execution, skip/off, natural combat pocket-only, direct-pair and physical input remain unverified. Existing successful globe result remains valid.

- Order-net diagnostic build 2026-11-18 09:41:10.758903 to 2026-11-18 09:41:13.682757 JST PID25376 limit600s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:OutputPath=D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '-v:minimal']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\order-net-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\order-net-build.err.

- Order-net diagnostic run 2026-11-18 09:41:13.683179 to 2026-11-18 09:41:30.606747 JST PID28640 limit300s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\order-net-run.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\order-net-run.err.


### 2026-11-18 09:41:47.452734 JST Demo order-net audit

- Start09:40 end2026-11-18 09:41:47.452750 JST HEAD f5f8109d; product unchanged, existing diagnostic only adds SECOND_ORDER_NET transition at featured-order stage1 after ruler/inspector selection. Shared output reused, no Content copy. WORLD103 SECOND_MILITARY1 SECOND_ORDER_NET1; commands/PIDs/limits/results above, noTIMEOUT.
- Evidence: Second featured order 21: ruler=29 viewed=29 globe=False target=154 queen=True pocket=True peaceful=True stage=1->2 click={X:1440 Y:552} selected=154 picker=-1 red=False | Second featured order 21: ruler=29 viewed=29 globe=False target=154 queen=True pocket=True peaceful=True stage=2->6 click={X:707 Y:938} selected=-1 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=False target=35 queen=True pocket=False peaceful=False stage=1->2 click={X:1440 Y:552} selected=35 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=False target=35 queen=True pocket=False peaceful=False stage=2->3 click={X:640 Y:732} selected=35 picker=1 red=False | Second featured order 23: ruler=31 viewed=31 globe=False target=35 queen=True pocket=False peaceful=False stage=3->3 click={X:640 Y:732} selected=35 picker=1 red=False | Second featured order 23: ruler=31 viewed=31 globe=False target=35 queen=True pocket=False peaceful=False stage=3->4 click={X:1415 Y:822} selected=35 picker=-1 red=False | Second featured order 23: ruler=31 viewed=31 globe=False target=35 queen=True pocket=False peaceful=False stage=4->5 click={X:936 Y:544} selected=35 picker=-1 red=True | Second featured order 23: ruler=31 viewed=31 globe=False target=35 queen=True pocket=False peaceful=False stage=5->6 click={X:936 Y:488} selected=35 picker=-1 red=True | Second queen result: ruler=31 target=35 marches=1 queens=-32 reservations=0/0/0 | Second military visible=38 fallbacks=0 player=0 phase=Domestic.
- Partial audit completed; natural combat pocket-only, later-turn skip/off, natural direct-pair and physical input remain unverified. No new DEMO ID; next compare actual featured orders and fallback counts against globe control.


### 2026-11-18 10:01:51.564316 JST Demo evidence comparison

- Start10:01 end2026-11-18 10:01:51.564316 JST HEAD f5f8109d. Initial instructions/current differences/records checked. Game unchanged; no repeated UI run/build. Compared cpu103-mode-run.out versus order-net-run.out from completed tests.
- Both: second military38 instructions, fallback0, human0 Domestic; CPU31 target35 queen marches1/unit-32, frame target35, current37/targetOwner8, reservations0/0/0. Actual view differs (globeTrue vsFalse); map/picker/check/close evidence in prior entries. Equality covers these recorded queen outcomes, not entire robot participants or all world state.
- Next add optional later-order skip/off and full outcome snapshot to existing diagnostic before comparison; existing CONFIRM_COMPLETION exits first batch and cannot supply this. Natural combat pocket-only, direct-pair and physical input still unverified. Partial static audit completed; no new DEMO ID.

- Second comparison build 2026-11-18 10:22:09.418281 to 2026-11-18 10:22:12.200738 JST PID15584 limit600s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:OutputPath=D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '-v:minimal']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-compare-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-compare-build.err.

- Second comparison normal 2026-11-18 10:22:12.201220 to 2026-11-18 10:22:29.661623 JST PID13836 limit300s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-compare-normal.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-compare-normal.err.

- Second comparison skip 2026-11-18 10:22:29.662084 to 2026-11-18 10:22:44.353937 JST PID17960 limit300s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-compare-skip.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-compare-skip.err.


### 2026-11-18 10:23:02.788284 JST Demo second-order skip audit

- Start10:21 end2026-11-18 10:23:02.788303 JST HEAD f5f8109d, game unchanged; existing diagnostic adds SECOND_SKIP at actual second-batch Queen picker stage3 and outcome snapshot. WORLD103 SECOND_MILITARY1 SECOND_ORDER_NET1; skip additionallySECOND_SKIP1. Commands/PIDs/results above, all exit0/noTIMEOUT, shared runtime reused.
- Normal versus skip second-outcome line equality=True (LastMarches sorted, all node owners, LastQueenMarches sorted). Both restore human0 Domestic. This covers supplied snapshot only, not all state or robot participant IDs; skip uses UpdateComputerMarches skip flag, not physical Escape.
- Partial audit completed; later-turn display off, natural combat pocket-only/direct-pair and physical input remain unverified. Next capture participant IDs/cancellations if extending equality and test display off from same second batch. No new DEMO ID.

- Second display-off build 2026-11-18 10:46:22.257780 to 2026-11-18 10:46:25.234213 JST PID10948 limit600s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:OutputPath=D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '-v:minimal']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-off-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-off-build.err.

- Second display-off run 2026-11-18 10:46:25.234731 to 2026-11-18 10:46:38.820687 JST PID31712 limit300s exit0 TIMEOUT=False HEAD f5f8109d; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoAuditCpuSortie.dll']; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-off-run.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\second-off-run.err.


### 2026-11-18 10:46:57.874826 JST Demo second display-off outcome

- Start10:40 end2026-11-18 10:46:57.874826 JST HEAD f5f8109d; game unchanged. Existing diagnostic adds SECOND_OFF to toggle computer operations off only before second military. Shared runtime reused; WORLD103 SECOND_MILITARY1 SECOND_OFF1. Commands/PIDs/limits/results above, exit0/noTIMEOUT.
- Normal/skip/off Second outcome equality=True (sorted LastMarches, all node owners, sorted LastQueenMarches). Display-off batch restores human0 Domestic without preview. Equality still excludes full participants/cancellations and all other world fields. Toggle uses same settings method, not physical UI.
- Partial audit complete. Natural combat pocket-only/direct-pair, physical input and full participant snapshot remain unverified. Next extend participant/cancellation equality only if needed, otherwise pursue remaining natural route coverage. No new DEMO ID.


### 2026-11-18 11:00:50.045917 JST natural Demo audit

- Start2026-11-18 11:00:40.572629 end2026-11-18 11:00:50.045917 JST HEAD f5f8109d; game unchanged, existing DemoUi shared runtime reused. Seed28, ALSOFT_DRIVERS=null; command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoUi.dll'] PID31860 limit300s exit0 TIMEOUT=False; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\natural28.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\natural28.err.
- Partial natural title-to-first-transport and fixture continuation audit. Inspect delivery flags before claiming direct-pair reachability; later stages include diagnostic state resets. No full physical traversal.

- 2026-11-18 11:01:07.109104 JST outcome: Natural delivery flags before first transport: overview=False, directMap=False, directPair=False | Follow-up delivery flags: overview=False, directMap=False, directPair=False | PASS: idle loop, globe/net switching, edge relocation at 18 FPS, refusal fallback, ending loop and demo label rendering.. Natural prefix ended at first transport; direct-pair remains unverified. Next pursue longer natural transport traversal rather than infer from fixture continuation. Partial audit complete, no new DEMO ID.


### 2026-11-18 11:20 JST Demo static continuation audit

- Start11:20; end 2026-11-18T11:26:21.017870+09:00; partial static audit completed. HEAD f5f8109d; current diff: audit records, user note, TutorialWarGuidesUi diagnostic only. Game code unchanged. No UI process launched, no resolution or new rendering evidence this round.
- Rechecked Game1.Demo.cs, Game1.DemoDomestic.cs and tests/DemoUi/Program.cs. Natural prefix in DemoUi stops on first Transport at lines44-56; subsequent regression clears pending, forces WorldStatus and resets map stages at lines59-72. Its follow-up flags cannot establish natural delivery/direct-pair coverage.
- Game1.Demo.cs advances Transport through UpdateTransport(elapsed,false,now) and resets step age; Game1.DemoDomestic.cs stage12 uses TransportOverviewButton.Center via MapOverlayClick, followed by MovementClick stages15/13. DirectDelivery.cs truck selection line63 opens pair through actual truck Bounds. Static inspection alone does not verify cursor alignment or transitions after first Transport.
- No new confirmed defect or DEMO ID. Remaining natural post-Transport traversal, direct-pair reachability, pocket-only combat, physical input remain unverified. Next: extend diagnostic natural prefix through delivery/military before fixture mutations, record screen/phase transitions and delivery flags; retain existing runtime to avoid duplicate outputs.

- Planned natural-continue-build limit600s; HEAD f5f8109d; existing local fallback reused, NAS previously unavailable; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\natural-continue-build.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\natural-continue-build.err.
- natural-continue-build 2026-11-18 11:41:54.762089+09:00 to 2026-11-18 11:41:57.422363+09:00 PID28496 exit0 TIMEOUT=False command ['dotnet', 'build', 'tests/DemoUi/DemoUi.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal']

- Planned natural-continue28 limit300s; HEAD f5f8109d; existing local fallback reused, NAS previously unavailable; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\natural-continue28.out, D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\natural-continue28.err.
- natural-continue28 2026-11-18 11:41:57.423358+09:00 to 2026-11-18 11:42:03.718947+09:00 PID24580 exit3762504530 TIMEOUT=False command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoUi.dll']

- Runtime correction: first run failed ContentLoadException missing robot-assembly-icon.xnb before Demo start. Copied only 770 missing compiled Content files (452627189 bytes) from existing FolderVerse/Content/bin/DesktopGL/Content, no originals modified. Retry 2026-11-18 11:42:38.215379+09:00 to 2026-11-18 11:42:54.400629+09:00 PID19712 limit300s exit0 TIMEOUT=False; logs natural-continue28-retry.out/.err in same output base.


### 2026-11-18 11:40 JST natural post-Transport audit outcome

- Start11:40 end 2026-11-18T11:43:15.893742+09:00; partial audit completed. HEAD f5f8109d; game unchanged. tests/DemoUi/Program.cs adds opt-in FOLDERVERSE_DEMO_NATURAL_CONTINUE=1 before fixture resets; ticks normal Demo update at0.25s and draws on screen/phase/turn transitions. Seed28; no forced world/screen/queue changes in this branch. Commands, PID, finite limits and content correction recorded above. Retry exit0, noTIMEOUT. Final PNG resolution (1920, 1080); actual desktop input not tested.
- Natural progression observed Transport Delivery0 -> Transport Domestic1 -> Battle -> BattleResearch -> Industry -> WorldStatus -> Transport Delivery1 -> turn2 -> Battle -> BattleResearch -> Industry -> WorldStatus -> Transport Delivery2, two subsequent transport entries. No natural progression stall in this bounded sample. Evidence natural-continue28-retry.out/.err and demo.png in existing shared output.
- overview=False/directMap=False/directPair=False throughout. This seed did not demonstrate direct delivery/pair teaching path; absence of flags is a coverage limitation, not proof of broken UI. No new DEMO ID. Remaining: inspect domestic stage transitions and ordinary truck entry to determine dedicated Demo teaching coverage; pocket-only combat/physical input remain unverified. Next use this opt-in branch to sample stage/button rectangles without fixture mutations. Shared runtime content now supplemented from existing compiled Content; avoid further Content copies.


### 2026-11-18 22:16 JST updated-HEAD static audit

- Start22:16 end 2026-11-18T22:17:34.937133+09:00; HEAD ed3c02d5, Git diff/status clean. Previous f5f8109d results are historical and are not current-HEAD runtime validation. No dotnet processes returned by tasklist; no game stopped or duplicate validation launched. Game code unchanged this round; partial static audit complete.
- Compared f5f8109d..HEAD: Demo.cs/DemoDomestic.cs unchanged, but44 related source/test files changed including primary overlays, camera restoration, computer policy, production intakes and trade. PrimaryActionCount grows8->9 with TradeAction8; PrimaryActionHomeBounds formula for prior indexes unchanged, TransportOverviewButton stage12 still reuses primary index4. Drawing/input both iterate shared count/bounds. This establishes static sharing, not rendered/input correctness.
- New RestoreDomesticCamera sets diplomacy pending for DueTrade or replies; previous natural turn loop cannot establish current trade-modal behavior. Factory pending-intake button and ResetInspector pending-intakes cleanup also require current-runtime regression. No confirmed new defect/DEMO ID, no screenshot/resolution this round.
- Next build current ed3c02d5 Game1 plus DemoUi into existing bounded output (do not reuse stale assembly as current evidence), run opt-in natural continuation with stage/button samples, then DemoInspectorUi for new pending-intake interaction. Natural direct-pair teaching/pocket-only combat and physical input remain unverified. No commit/push/publication.

- Planned current-demo-build limit600s HEAD ed3c02d5 existing local fallback; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\current-demo-build.out/.err
- current-demo-build 2026-11-18 22:21:22.533075+09:00 to 2026-11-18 22:21:25.242200+09:00 PID30184 exit0 TIMEOUT=False command ['dotnet', 'build', 'tests/DemoUi/DemoUi.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal']

- Planned current-demo28 limit300s HEAD ed3c02d5 existing local fallback; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\current-demo28.out/.err
- current-demo28 2026-11-18 22:21:25.242711+09:00 to 2026-11-18 22:21:39.475768+09:00 PID34012 exit0 TIMEOUT=False command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoUi\\debug\\DemoUi.dll']


### 2026-11-18 22:20 JST current Demo continuation outcome

- Start22:20 end 2026-11-18T22:21:58.399686+09:00; HEAD ed3c02d5; partial audit completed, game unchanged. Diagnostic only adds stage/primary4 sampling. Used game assembly built in preceding current tutorial regression; FolderVerse/debug and DemoUi/debug DLL SHA256 equal e3524c53dc94fa1184b2798bda416175472482f687e1c4d662dadd1956d3196e. Current build/run exit0, noTIMEOUT, stderr empty. No extra Content copies.
- Natural Seed28 reached Delivery2 after two rounds of Battle/BattleResearch/Industry/WorldStatus; overview/directMap/directPair remainFalse. Primary4 bounds observed(680,974,96,96); war stages30..36 followed by19, confirm click(1415,822), red(936,544), close(936,488), turn end(256,998). State transitions and1920x1080 drawing recorded, but stage logging by itself is not full rectangle/state assertion for every click.
- No new DEMO ID. Dedicated direct-pair route, natural pocket-only combat, physical input remain unverified. Next inspect first domestic-stage samples and run current DemoInspectorUi/DirectDelivery regression for actual close/retreat/restore bounds. Evidence current-demo28.out/.err and shared demo.png; diagnostic update tests/DemoUi/Program.cs only.

- Planned current-DemoInspectorUi-build limit600s HEAD ed3c02d5; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\current-DemoInspectorUi-build.out/.err; existing local fallback reused, Content absolute override.
- current-DemoInspectorUi-build 2026-11-18 22:42:13.445216+09:00 to 2026-11-18 22:42:16.361519+09:00 PID24764 exit0 TIMEOUT=False command ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal']

- Planned current-DemoInspectorUi-run limit300s HEAD ed3c02d5; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\current-DemoInspectorUi-run.out/.err; existing local fallback reused, Content absolute override.
- current-DemoInspectorUi-run 2026-11-18 22:42:16.361879+09:00 to 2026-11-18 22:42:30.478504+09:00 PID2244 exit0 TIMEOUT=False command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll']

- Planned current-DirectDeliveryUi-build limit600s HEAD ed3c02d5; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\current-DirectDeliveryUi-build.out/.err; existing local fallback reused, Content absolute override.
- current-DirectDeliveryUi-build 2026-11-18 22:42:30.479443+09:00 to 2026-11-18 22:42:33.423365+09:00 PID14240 exit0 TIMEOUT=False command ['dotnet', 'build', 'tests/DirectDeliveryUi/DirectDeliveryUi.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal']

- Planned current-DirectDeliveryUi-run limit300s HEAD ed3c02d5; logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\current-DirectDeliveryUi-run.out/.err; existing local fallback reused, Content absolute override.
- current-DirectDeliveryUi-run 2026-11-18 22:42:33.423715+09:00 to 2026-11-18 22:42:41.886080+09:00 PID11148 exit0 TIMEOUT=False command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DirectDeliveryUi\\debug\\DirectDeliveryUi.dll']


### 2026-11-18 22:41 JST inspector/direct-delivery regression outcome

- Start22:41 end 2026-11-18T22:43:01.529609+09:00 HEAD ed3c02d5; partial audit completed, game unchanged. Only diagnostic Content override added to tests/DemoInspectorUi and tests/DirectDeliveryUi; FOLDERVERSE_TEST_CONTENT points at existing FolderVerse/Content/bin/DesktopGL/Content, ALSOFT_DRIVERS=null. No Content copies. Current game assembly SHA equality {'DemoInspectorUi': True, 'DirectDeliveryUi': True} versus e3524c53dc94fa1184b2798bda416175472482f687e1c4d662dadd1956d3196e. Commands/PIDs/time limits/logs above, both builds and runs exit0/noTIMEOUT, stderr empty.
- DemoInspectorUi passed office IDs/rendering, animated close/reset, primary transport hit and my-country/movement follow-up, six factory-plan rectangles and actual plan state at1080p/540p, count-confirm hits. DirectDeliveryUi passed main/pair camera transitions, input suppression while controls move, close/Escape, globe/net truck selection540p, left/right hangars, selected overlay opening/return and reservation preservation. These are fixture/normal-input-handler tests, not physical mouse/Esc or natural Demo dedicated direct-pair teaching coverage.
- No new DEMO ID. New factory pending-intake panel not explicitly covered by existing factory test. Remaining natural pocket-only combat, dedicated direct-pair teaching and physical input unchanged. Next inspect pending-intake panel coverage or resume natural stage/CPU path sampling; avoid repeating these passing unchanged regressions.


### 2026-11-18 23:01 JST pending-intake coverage audit

- Start23:01 end 2026-11-18T23:01:46.781205+09:00 HEAD ed3c02d5; partial static audit complete, product unchanged. Git differences remain prior audit records and diagnostic changes. No new build/UI run, screenshot or resolution evidence this round.
- ProductionIntakes.cs shared FactoryPendingIntakesButton=(90,926,550,38) is drawn at NodeFactory.cs255 and tested at150 before opening list; list draw/input share IntakeRow and IntakeClose/Previous/Next. Game1.cs190 routes modal input before later UI and751 draws picker; ResetInspector clears _pendingIntakesOpen. Static sharing only, not rendered-hit proof.
- Found pre-existing tests/ProductionIntakeWorkerCancel/Program.cs78-119 covering footer opening,10-item second-page identity, page bounds, Escape/close/background capture, detached-line target selection. It constructs an uninitialized Game1 shell and invokes handlers; no GraphicsDevice/real rendering coverage there. Not executed this round, so not counted as current pass. Existing DemoInspectorUi pass does not exercise this modal.
- No new confirmed DEMO defect. Next reuse ProductionIntakeWorkerCancel fixture to obtain a pending-only factory state and add bounded real-render/handler verification if pursuing modal; preserve natural direct-pair/pocket-only/physical-input gaps. Avoid duplicate product changes or rerun of already passing broad regressions.

- Planned pending-handler-build limit600s HEAD ed3c02d5 logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pending-handler-build.out/.err. Existing local fallback reused, no new UI process.
- pending-handler-build 2026-11-18 23:21:43.202844+09:00 to 2026-11-18 23:21:46.353702+09:00 PID27624 exit0 TIMEOUT=False command ['dotnet', 'build', 'tests/ProductionIntakeWorkerCancel/ProductionIntakeWorkerCancel.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '-v:minimal']

- Planned pending-handler-run limit300s HEAD ed3c02d5 logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pending-handler-run.out/.err. Existing local fallback reused, no new UI process.
- pending-handler-run 2026-11-18 23:21:46.354246+09:00 to 2026-11-18 23:21:54.443520+09:00 PID6304 exit0 TIMEOUT=False command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\ProductionIntakeWorkerCancel\\debug\\ProductionIntakeWorkerCancel.dll']


### 2026-11-18 23:21 JST pending-intake handler audit outcome

- Start23:21 end 2026-11-18T23:22:11.736721+09:00 HEAD ed3c02d5; partial audit complete, no game/test source change this round. Existing ProductionIntakeWorkerCancel built/run exit0, zero warnings/errors, noTIMEOUT, stderr empty. FOLDERVERSE_PROBE_CONTENT_BASE=FolderVerse/Content/bin/DesktopGL absolute path, no save argument supplied; no normal save modified. Commands/PIDs/logs above; no Content copies.
- Passed10 pending-products second-page identity/page bounds, background capture, close/Escape, pending-only factory availability, row-to-line and destination selection. Both before/after worker cancellation preserve detached intake across serialization, full recipient waits and exact-once arrival; raw material/progress assertions pass.
- This is reflection-based uninitialized Game1 input/state verification, no actual rendering/resolution or natural Demo cursor verification. No new DEMO defect. Next render pending-only factory/modal and verify physical logical-to-canvas hit conversion if pursuing this path; existing natural direct-pair/pocket-only/physical-input gaps remain.

- Planned pending-render-build limit600s HEAD ed3c02d5 logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pending-render-build.out/.err; existing output/Content reused.
- pending-render-build 2026-11-18 23:39:32.851069+09:00 to 2026-11-18 23:39:35.150740+09:00 PID15556 exit0 TIMEOUT=False command ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal']

- Planned pending-render-run limit300s HEAD ed3c02d5 logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pending-render-run.out/.err; existing output/Content reused.
- pending-render-run 2026-11-18 23:39:35.151092+09:00 to 2026-11-18 23:39:41.293255+09:00 PID7624 exit0 TIMEOUT=False command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll']


### 2026-11-18 23:38 JST pending-intake rendered audit outcome

- Start23:38 end 2026-11-18T23:40:06.575619+09:00 HEAD ed3c02d5; partial audit complete, game unchanged. tests/DemoInspectorUi/Program.cs adds optional FOLDERVERSE_PENDING_RENDER=1 branch with synthetic10 pending heads. It does not model real worker cancellation; preceding ProductionIntakeWorkerCancel covers that state path. Existing compiled Content override reused, no Content copies. Build/run exit0, noTIMEOUT, stderr empty; commands/PIDs/logs above.
- Rendered first/second pending pages and destination picker at1920x1080; next-page handler moves to1, second row selects pending index9 by reference, Escape clears picker, ResetInspector clears list. Visually reviewed second-page IDs920008/920009, heading/status text and previous/next/close controls inside modal. Saved D:\github.com\muzudho\FolderVerse\artifacts\demo-click-audit\pending-render-current.png SHA256 45ecf50d407134f66322db846ef81fc57327a45dda847192f07fe53b491b8b77. Other frames pending-render-0..3.png in shared DemoInspectorUi output.
- New branch opens list by fixture field assignment and clicks handlers at shared logical rectangle centers; footer opening and full physical/canvas input are not newly verified here. No new DEMO defect. Next if needed combine pending-only saved-world fixture with actual HandleInput at540p; otherwise pursue remaining natural direct-pair/pocket-only route coverage. Physical desktop approval issue unchanged.

- Planned pending-canvas-build limit600s HEAD ed3c02d5 logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pending-canvas-build.out/.err; existing output/Content reused.
- pending-canvas-build 2026-11-19 00:00:10.287098+09:00 to 2026-11-19 00:00:12.566423+09:00 PID30892 exit0 TIMEOUT=False command ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal']

- Planned pending-canvas-run limit300s HEAD ed3c02d5 logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pending-canvas-run.out/.err; existing output/Content reused.
- pending-canvas-run 2026-11-19 00:00:12.566759+09:00 to 2026-11-19 00:00:25.757958+09:00 PID6912 exit3762504530 TIMEOUT=False command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll']


### 2026-11-19 00:00 JST pending-intake canvas audit interrupted

- Start2026-11-18 23:59 end 2026-11-19T00:00:51.663423+09:00 HEAD ed3c02d5; game unchanged. tests/DemoInspectorUi/Program.cs adds optional FOLDERVERSE_PENDING_CANVAS=1:960x540, logical-to-CanvasBounds mouse release/press/release via HandleInput, starting from synthetic pending products. Build exit0 but run failed Canvas footer click failed in first Draw frame before screenshot; noTIMEOUT, process exited. Commands/PIDs/logs pending-canvas-build/run above.
- This is an unresolved diagnostic failure, NOT a confirmed product defect and NOT a passing540p test. Earlier1920x1080 direct-handler/render pass stands only for that scope. Initial layout/input readiness and cut-in/inspector timing were not yet excluded; do not assign DEMO ID until normal reachable state reproduces.
- Interrupted canvas extension; next instrument screen, cut-in, inspector/slide, pointer/canvas and footer bounds before/after HandleInput, render before clicking, compare NodeFactoryClick versus normal input. Preserve existing pending-render-current.png; no additional run after date rollover. Physical input and natural Demo coverage unchanged.

- Planned pending-state-build limit600s HEAD ed3c02d5 logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pending-state-build.out/.err; existing runtime retention extended through2026-11-19JST with marker, no Content copies.
- pending-state-build 2026-11-19 00:22:37.009478+09:00 to 2026-11-19 00:22:39.519020+09:00 PID9472 exit0 TIMEOUT=False command ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal']

- Planned pending-state-run limit300s HEAD ed3c02d5 logs D:\github.com\muzudho\FolderVerse\artifacts\tmp\delete-after-2026-11-18\tutorial-audit-local\pending-state-run.out/.err; existing runtime retention extended through2026-11-19JST with marker, no Content copies.
- pending-state-run 2026-11-19 00:22:39.519392+09:00 to 2026-11-19 00:22:53.193474+09:00 PID11684 exit3762504530 TIMEOUT=False command ['dotnet', 'D:\\github.com\\muzudho\\FolderVerse\\artifacts\\tmp\\delete-after-2026-11-18\\tutorial-audit-local\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll']


### 2026-11-19 00:21 JST pending-canvas diagnostic state

- Start00:21 end 2026-11-19T00:24:04.515608+09:00 HEAD ed3c02d5; game unchanged, diagnostic logs only added to PendingClick. Build exit0/run exception exited, noTIMEOUT; commands/PIDs/logs pending-state-build/run above. Existing runtime retention extended throughNov19JST using RETAIN-THROUGH-2026-11-19.txt. No Content copies.
- Before logical footer(365,945), canvas(0,0,960,540), physical(182,472), WorldStatus/factoryTrue/plan-1/slide1/persuasionFalse/chrome0; cut-in phase matches. After pointer(364,944) correctly inside footer but pendingFalse/factoryFalse. Thus simple2x coordinate conversion mismatch is not supported by this sample; input path closes/hides factory before list opening. Initial render readiness or other consuming handlers not yet excluded.
- NodeDialogClick adjusts NodeInspectorOffset (zero at slide1); SupervisorNodePanel currentlyfalse, so right-panel translation not cause in source. Need log populationNode/cell, owner/player, auto/computer handlers, factory/office flags after each release/press event, render initial frame before input and compare direct NodeFactoryClick. No confirmed DEMO ID; diagnostic failure remains unresolved, not normal. Partial investigation complete; next continue first-event interception.

### 2026-11-19 06:19 JST source-update audit

- Start06:19 end 
2026-11-19T06:19:57.5580071+09:00
. Initial HEAD eec8fd62 advanced during read-only inspection to 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7 (user merge); worktree clean before this record. No game/test code changed, no build or game process started. No dotnet/FolderVerse process returned by Win32_Process query. Instructions, audit/fixes and current diff reviewed.
- Prior runtime passes used ed3c02d5; current changes include Game1.cs input and Trade, so those passes are historical evidence and do not establish current-HEAD regression success. Demo.cs/DemoDomestic.cs unchanged against ed3c02d5. Resolution: no new rendered sample; prior pending-canvas sample960x540 remains unresolved diagnostic failure, no confirmed DEMO ID.
- Static follow-up: ComputerDomestic ShowComputerView/EndComputerView call ResetInspector; NodeFactoryClick clears factory on disallowed tutorial or invalid population node. These are candidate interceptors, not a reproduced cause. First-event input instrumentation and current-HEAD rendering remain unverified. Partial static inspection complete; runtime extension deferred during source update. Next confirm stable HEAD and existing output freshness, then log each Released/Pressed event and initial rendered state before pending-footer input. Desktop approval remains previously reported blocker; no retry this round.

- Static audit follow-up2026-11-19 start06:23 end 2026-11-19T06:23:51.8054238+09:00 HEAD53c9ed61 unchanged. AGENTS/README/audit/fixes/current differences reviewed; only records and user notebook differ. Demo.cs/DemoDomestic.cs inspected: shared factory/transport/check/close rectangle targeting remains present; no game code changed. Current Game1.cs introduces TryBeginTurnNewspaper before UpdateDemo (lines169/236); newspaper input returns before scripted cursor updates. This makes newspaper entry/automatic8-second return a specific current-HEAD regression target, not a confirmed defect. Earlier pending-canvas failure predates this change and cannot be attributed to it. No new rendering/input run, screenshot, or DEMO ID. Static portion complete; runtime coverage remains unverified. Next isolate current-HEAD natural Demo through newspaper and subsequent planning, then resume per-event pending-footer diagnostic. Do not repeat desktop approval request absent a permission change.

- Planned inspector-current-build 2026-11-19 06:40:59.543136+09:00 HEAD 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7 limit600s command ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:NuGetAudit=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\inspector-current-build.out/.err; NAS existing current game/content reused.
- inspector-current-build start 2026-11-19 06:40:59.543136+09:00 end 2026-11-19 06:41:16.114805+09:00 PID 34780 exit 0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned inspector-current-run 2026-11-19 06:41:16.173859+09:00 HEAD 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7 limit300s command ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\inspector-current-run.out/.err; NAS existing current game/content reused.
- inspector-current-run start 2026-11-19 06:41:16.173859+09:00 end 2026-11-19 06:41:44.714559+09:00 PID 31712 exit 0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Rendered regression2026-11-19 start06:40 end 2026-11-19T06:42:33.7509564+09:00 HEAD53c9ed61 stable. No game/test source changes. DemoInspectorUi build/run exit0 without TIMEOUT; limits600/300s, commands/PIDs/logs inspector-current-build/run above in existing NAS tutorial-audit output. Referenced FolderVerse.dll equals current tutorial build SHA25610787799ECE9898D578CB00581DCB50F6F4A093908D5B078277430904FDEFE16. - Passed six factory plans at1920x1080/960x540 with shared-rectangle Demo click bounds and actual selection/manufacturing update; synchronized office IDs, animated close/reset, transport primary, my-country movement, human/computer count confirmations and rendering. Visually inspected540p demo-office-34.png; factory panel, close/check controls and selected plan remain rendered. Preserved outside tmp at Z:/muzudho-with-ai-agent/FolderVerse/demo-click-audit/inspector-540-53c9ed61.png. - Partial regression complete; no new DEMO ID. Fixture/helper coverage does not prove physical input, natural newspaper transition, pending-footer canvas case or direct-pair-only teaching route. Next natural Demo regression must use HandleInput because existing DemoUi Tick calls UpdateDemo directly and bypasses newspaper entry. Do not count direct UpdateDemo continuation as newspaper validation. Desktop approval issue unchanged.

- Planned demo-normal-build 2026-11-19 07:03:11.333780+09:00 HEAD 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7 limit600s command ['dotnet', 'build', 'tests/DemoUi/DemoUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-p:NuGetAudit=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\demo-normal-build.out/.err; seed28 natural continuation with FOLDERVERSE_DEMO_NORMAL_INPUT=1 and FOLDERVERSE_DEMO_NATURAL_CONTINUE=1.
- demo-normal-build start 2026-11-19 07:03:11.333780+09:00 end 2026-11-19 07:03:25.722668+09:00 PID 24524 exit 0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned demo-normal-run 2026-11-19 07:03:25.773633+09:00 HEAD 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7 limit300s command ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoUi\\debug\\DemoUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\demo-normal-run.out/.err; seed28 natural continuation with FOLDERVERSE_DEMO_NORMAL_INPUT=1 and FOLDERVERSE_DEMO_NATURAL_CONTINUE=1.
- demo-normal-run start 2026-11-19 07:03:25.773633+09:00 end 2026-11-19 07:03:34.153410+09:00 PID 34596 exit 3762504530 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned demo-normal-build2 2026-11-19 07:04:18.500078+09:00 HEAD53c9ed61 limit600s command ['dotnet', 'build', 'tests/DemoUi/DemoUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\demo-normal-build2.out/.err; same seed28 normal-input env plus FOLDERVERSE_TEST_CONTENT=current tutorial output Content; previous failure missing assembly icon before gameplay.
- demo-normal-build2 start 2026-11-19 07:04:18.500078+09:00 end 2026-11-19 07:04:29.253913+09:00 PID 27516 exit 0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned demo-normal-run2 2026-11-19 07:04:29.312539+09:00 HEAD53c9ed61 limit300s command ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoUi\\debug\\DemoUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\demo-normal-run2.out/.err; same seed28 normal-input env plus FOLDERVERSE_TEST_CONTENT=current tutorial output Content; previous failure missing assembly icon before gameplay.
- demo-normal-run2 start 2026-11-19 07:04:29.312539+09:00 end 2026-11-19 07:05:33.633703+09:00 PID 17576 exit 3762504530 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

### DEMO-0026 Factory album consumes normal input before Demo can select a plan
- Status: 完了. Confirmed2026-11-19 2026-11-19T07:06:28.6284348+09:00 HEAD53c9ed61; audit started07:02, partial audit complete. Game unchanged; tests/DemoUi/Program.cs adds opt-in FOLDERVERSE_DEMO_NORMAL_INPUT=1 and shared Content override. Existing test/record/user notebook differences preserved. - Reproduce: build DemoUi against current NAS game; seed28, NATURAL_CONTINUE=1 and NORMAL_INPUT=1. Initial scripted setup reaches first Transport; subsequent ticks call normal HandleInput with released mouse at(-1,-1) and no keys. Natural newspaper appears169.25 simulated seconds and returns177.25, then factory stages8/9/10 at185/187/189. Continue through600 simulated seconds: WorldStatus, turn1, stage10, age0, node2 and click(327,389) remain; two subsequent transports never occur. Initial game setup is diagnostic/helper, not physical title playthrough. Rendering1920x1080 occurs on screen transitions; no screenshot of final stuck album was captured this run. - Expected: after opening factory plan album, Demo waits1.8s, clicks shared candidate rectangle, updates selected plan, then proceeds to checks/close/turn end and delivery. Actual: HandleInputCore early FactoryAlbumOpen block (Game1.cs140ff) returns on every frame before UpdateDemo at236, so Demo stage10 cannot advance. Existing direct UpdateDemo tests bypass this interceptor and passed falsely for normal-input continuity. No coordinate displacement established; this is click/state progression interception. Separate from historical DEMO-0007 plan-selection implementation issue. - Evidence: NAS tmp/delete-after-2026-11-19/tutorial-audit/demo-normal-run2.out/.err, PID17576 exit3762504530, noTIMEOUT; finite600 simulated-second diagnostic assertion. First attempt PID34596 failed missing Content before gameplay, resolved with explicit existing Content path; not a product defect. Build/run commands/PIDs/start/end/limits recorded above. New current game SHA10787799ECE9898D578CB00581DCB50F6F4A093908D5B078277430904FDEFE16 was previously verified. Next repair reproduce at album with rendered screenshot, let Demo advance while preserving normal-user album modal input, then validate plan choice, subsequent delivery and manual album clicks/Escape. Newspaper8-second return itself passed. Pending-footer canvas and physical/CPU/direct-pair routes remain unverified.

### DEMO-0027 Sortie picker intercepts normal-input Demo at stage33
- Status: 完了. Confirmed2026-11-19 07:10JST during DEMO-0026 repair validation, HEAD53c9ed61 plus factory-album fix. Seed28 world948366069/cast763061582/placement1153806411,1920x1080, normal HandleInput continuation after helper initial setup. Newspaper and first factory/delivery pass complete; second planning reaches stage33 at369.75 simulated seconds, selected target22, click(464,338), movement/war true, node-1, slide1. Remains stage33/age0 through600s, no second subsequent delivery. - Expected: Demo chooses sortie robots, confirms immediate reservation, red check, closes inspector and ends faction turn. Actual: Game1.cs invasion-picker early return (around197) calls only InvasionPickerInput, before UpdateDemo, so stage33 never advances. Evidence NAS tutorial-audit/demo26-run.out/.err, PID16660 exited3762504530, noTIMEOUT, full command/timing in fixes record. No final-picker screenshot captured; album screenshot is evidence only for DEMO-0026 and not this screen. - New issue recorded separately from DEMO-0026, no duplicate known item. No sortie-picker product fix this round. Next reproduce picker with rendered capture, preserve normal human input and prevent double execution when allowing scripted update, verify reservation/redcheck/close/turn end. DEMO-0026 now検証待ち: its factory stop is removed, but independent human-modal diagnostic and full continuation still unresolved. Repair investigation ended07:18JST; physical/CPU/direct-pair and pending-footer canvas scope unchanged.

- Planned album-state-build 2026-11-19 07:21:05.883168+09:00 HEAD53c9ed61+pendingDemo26fix limit600s cmd ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\album-state-build.out/.err; FACTORY_MODAL_INPUT=1 with per-event state logging, same Content/audio env.
- End 2026-11-19 07:21:10.783544+09:00 PID25452 exit0 TIMEOUT=False

- Planned album-state-run 2026-11-19 07:21:10.801503+09:00 HEAD53c9ed61+pendingDemo26fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\album-state-run.out/.err; FACTORY_MODAL_INPUT=1 with per-event state logging, same Content/audio env.
- End 2026-11-19 07:21:33.590450+09:00 PID19740 exit3762504530 TIMEOUT=False

- Planned album-manual-build 2026-11-19 07:22:50.964139+09:00 HEAD53c9ed61+pendingDemo26fix limit600s cmd ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\album-manual-build.out/.err; same per-event fixture with workshop.Automation=Manual.
- End 2026-11-19 07:22:55.579942+09:00 PID33836 exit0 TIMEOUT=False

- Planned album-manual-run 2026-11-19 07:22:55.596310+09:00 HEAD53c9ed61+pendingDemo26fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\album-manual-run.out/.err; same per-event fixture with workshop.Automation=Manual.
- End 2026-11-19 07:23:12.008237+09:00 PID7572 exit0 TIMEOUT=False

- Verification audit2026-11-19 start07:20 end 2026-11-19T07:23:53.9686418+09:00 HEAD53c9ed61+existingDemo26fix; no game code changed this audit. Per-event album-state-run proved real human Pressed selects0 (base/screen5->0), next Released changes0->7. Source RobotWorld.PrepareDomestic/AdjustNode explains first normal-frame automatic supervisor adjustment; fixture was not manual. Set workshop.Automation=Manual in test only; album-manual-run exits0 and retains0 after release, normal Escape closes album. Exact commands/PIDs/limits600/300s/logs above, noTIMEOUT. New diagnostic evidence excludes a click failure; no additional product defect assigned. - DEMO-0026 now完了 for factory modal Demo progression and human manual click/Escape: prior rendered1920x1080 album and natural stage10->19->delivery1 plus this normal-input pass. Product fix remains existing Game1.cs five-line change, no coordinate/UI spec change. Existing broad continuation still fails separately at DEMO-0027 sortie picker; do not report whole Demo as passed. CPU/direct-pair/pending-footer and physical desktop coverage unchanged. Next repair uses recorded DEMO-0027; next audit capture picker and verify reservation/redcheck/close/turn end after its repair.

- DEMO-0027 verified complete2026-11-19 2026-11-19T07:32:10.4703890+09:00 HEAD53c9ed61+Demo26/27 modal fixes. demo27-run passed normal input through selected robot->immediate reservation(count1)->redcheck->inspector close->faction turnend->second subsequent delivery. demo27-human-run passed normal tutorial sortie confirmation and followup.1920x1080 selected-picker/redcheck images visually reviewed and preserved NAS demo-click-audit/demo27-sortie-selected.png, demo27-red-check.png. Details/commands/times/PIDs/limits in fixes record. No new problem; next audit resumes remaining pending-footer canvas/direct-pair or CPU current-HEAD coverage. Physical input and all-seed/all-resolution scope still unverified.

- Planned pending-current-build 2026-11-19 07:41:20.788622+09:00 HEAD53c9ed61+Demo26/27fix limit600s cmd ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\pending-current-build.out/.err; PENDING_RENDER/PENDING_CANVAS=1 with own-node/manual/human/paper-read fixture; shared Content/audio=null.
- End 2026-11-19 07:41:25.845498+09:00 PID12216 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned pending-current-run 2026-11-19 07:41:25.893137+09:00 HEAD53c9ed61+Demo26/27fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\pending-current-run.out/.err; PENDING_RENDER/PENDING_CANVAS=1 with own-node/manual/human/paper-read fixture; shared Content/audio=null.
- End 2026-11-19 07:41:49.610416+09:00 PID29828 exit3762504530 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned pending-event-build 2026-11-19 07:43:07.315891+09:00 limit600s cmd ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\pending-event-build.out/.err; same pending fixture/env, added per-event factory/node/office/movement flags.
- End 2026-11-19 07:43:11.991434+09:00 PID31320 exit0 TIMEOUT=False

- Planned pending-event-run 2026-11-19 07:43:12.014770+09:00 limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\pending-event-run.out/.err; same pending fixture/env, added per-event factory/node/office/movement flags.
- End 2026-11-19 07:43:36.340150+09:00 PID18752 exit3762504530 TIMEOUT=False

- Pending-footer audit2026-11-19 start07:40 end 2026-11-19T07:45:10.0951224+09:00 HEAD53c9ed61+Demo26/27 fixes stable; no game code changed. tests/DemoInspectorUi pending540 fixture now explicitly uses current human player, own node82/cell37, manual workshop, newspaper read. Per-event logging added. Builds exit0; pending-current/event runs exited3762504530 at Canvas footer click failed, noTIMEOUT. Exact commands/PIDs/start/end/600s build and300s run limits/logs above in existing NAS tutorial-audit output. - New evidence: initial Released preserves factoryTrue, population82/37, officeFalse/movementFalse; Pressed closes factoryFalse without changing population; following Released keepsFalse. ManageTrue, screenWorldStatus and pointer(364,944) from physical(182,472) on960x540 remain correct. This excludes initial release-only shutdown and current newspaper/ownership/automatic-management confounds. FactoryPendingIntakesButton center365,945 lies inside nominal NodeInspectorBounds(54,150,850,825), NodeFactoryTile is(282,210,190,40). Still no confirmed product defect/DEMO ID: synthetic pending records, actual pending count after ReconcileDestinations, first real drawing/layout and exact consuming input handler remain unverified. - Partial diagnostic investigation complete; canvas case remains unresolved, not passing. No new screenshot because first-frame assertion precedes capture; existing1080 direct-handler picture is not540p input proof. Next render initialized factory before clicking, log pending records/source after each event and inspector offset/bounds/other modal flags, compare direct NodeFactoryClick result under identical state. Use normal reachable saved pending-world if synthetic records are reconciled away. Do not change game based on fixture alone. Physical desktop, natural direct-pair and current CPU on/off/skip remain pending; passed Demo26/27 continuation not repeated.

- Planned pending-draw-build 2026-11-19 07:59:34.703104+09:00 limit600s cmd ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\pending-draw-build.out/.err; same current pending fixture/env, added initial Draw/screenshot and pending-record counts.
- End 2026-11-19 07:59:39.031126+09:00 PID29524 exit0 TIMEOUT=False

- Planned pending-draw-run 2026-11-19 07:59:39.046810+09:00 limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\pending-draw-run.out/.err; same current pending fixture/env, added initial Draw/screenshot and pending-record counts.
- End 2026-11-19 08:00:04.329067+09:00 PID31620 exit3762504530 TIMEOUT=False

- Planned pending-compare-build 2026-11-19 08:01:35.329273+09:00 limit600s cmd ['dotnet', 'build', 'tests/DemoInspectorUi/DemoInspectorUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\pending-compare-build.out/.err; same pending fixture/env, direct-handler comparison after failed normal input, original failure retained.
- End 2026-11-19 08:01:39.673153+09:00 PID30208 exit0 TIMEOUT=False

- Planned pending-compare-run 2026-11-19 08:01:39.688748+09:00 limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoInspectorUi\\debug\\DemoInspectorUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\pending-compare-run.out/.err; same pending fixture/env, direct-handler comparison after failed normal input, original failure retained.
- End 2026-11-19 08:02:07.847078+09:00 PID25668 exit3762504530 TIMEOUT=False

### DEMO-0028 Hidden node-transport tile intercepts pending-product footer
- Status: 完了. Audit start2026-11-19 07:58 end 2026-11-19T08:02:54.1589889+09:00 HEAD53c9ed61+existingDemo26/27 fixes stable; no game changes this audit. tests/DemoInspectorUi adds initial render, pending counts, per-event other-tab flags and direct-handler comparison. No duplicate known issue. Current gameDLL D6D4CC76B59C0B0ACAAC8E1A2AD533B8C197152BE56CB64E89C5EC96E24755C7. - Reproduce960x540: own human node82/cell37, manual workshop, newspaper read;10 synthetic pending products from82, current factory fully rendered. FactoryPendingIntakesButton=(90,926,550,38); click logical365,945 -> physical182,472 -> pointer364,944 via release/press/release HandleInput. All10 source records remain after each event. Before click factoryTrue/transportFalse; Pressed changes factoryFalse/transportTrue without opening pending list. Screen/node unchanged. Direct NodeFactoryClick at same364,944 opens pendingTrue, so fixture record removal, missing initial draw, permission and simple2x conversion are excluded. - Expected rendered completed-products-waiting footer opens pending list. Actual hidden NodeTransportTile=(280,875,182,84) contains364,944; NodeDialogClick checks this rectangle before calling NodeFactoryClick, opens transport and hides factory. The screenshot visibly shows pending10 footer and no transport tile at that position. This establishes product input interception even though pending items are synthetic; natural worker-cancellation creation is not newly validated. Static overlap also exists at1080p logical coordinates, but this round runtime is540p only. - Evidence existing NAS tmp/delete-after-2026-11-19/tutorial-audit/pending-draw-run and pending-compare-run.out/.err; finalPID25668 exit3762504530/noTIMEOUT, assertion intentionally retains failed normal result even after successful direct comparison. Exact commands/PIDs/start/end/limits600/300s above. Initial1920logical/960x540physical rendering visually reviewed; preserved outside tmp at NAS demo-click-audit/demo28-pending-footer-before.png (hash printed by tool). - Partial audit complete, confirmed0028 replaces prior unresolved generic diagnostic for this footer. Next repair remove hidden-transport hit interception for factory footer while retaining visible transport/other-tab inputs and current normal UI; reuse shared footer rectangle, verify actual normal-input list open, paging/index9 identity, destination picker and Escape at540p plus1080p if needed. CPU/direct-pair and physical desktop scope remain unverified. No game fix in audit round.

- DEMO-0028 verified complete2026-11-19 2026-11-19T08:13:04.7411826+09:00 HEAD53c9ed61+Demo26/27/28 fixes. Shared factory footer now receives normal Canvas input before hidden transport rectangle.960x540 and1920x1080 list opening/page1/index9 identity/destination Escape/reset passed with actual rendering; related default DemoInspectorUi passed. Evidence NAS demo-click-audit/demo28-fixed-540.png, demo28-fixed-1080.png; commands/results/limits/PIDs in fixes record. Existing generic pending-footer diagnostic failure is resolved for this input route. Next audit resumes remaining natural direct-pair/CPU paths or natural worker-release pending state; physical input remains unverified.

- Planned cpu-current-build 2026-11-19 08:22:24.654462+09:00 HEAD53c9ed61+Demo26/27/28fix limit600s cmd ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '--disable-build-servers', '-p:NuGetAudit=false', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-current-build.out/.err; AUDIT_WORLD84 SECOND_MILITARY1 shared Content/audio=null; autosave disabled, generated evidence in NAS test output.
- End 2026-11-19 08:22:40.468685+09:00 PID22296 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-current-run 2026-11-19 08:22:40.512218+09:00 HEAD53c9ed61+Demo26/27/28fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-current-run.out/.err; AUDIT_WORLD84 SECOND_MILITARY1 shared Content/audio=null; autosave disabled, generated evidence in NAS test output.
- End 2026-11-19 08:23:07.642220+09:00 PID16672 exit3762504530 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-current-build2 2026-11-19 08:24:15.568578+09:00 limit600s cmd ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-current-build2.out/.err; same world84/second-military/env, initial fixture newspaper marked read; first run failed settings entrance before CPU.
- End 2026-11-19 08:24:20.132017+09:00 PID26816 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-current-run2 2026-11-19 08:24:20.175650+09:00 limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-current-run2.out/.err; same world84/second-military/env, initial fixture newspaper marked read; first run failed settings entrance before CPU.
- End 2026-11-19 08:24:55.265279+09:00 PID9172 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Current CPU audit2026-11-19 start08:20 end 2026-11-19T08:25:50.7309198+09:00 HEAD53c9ed61+Demo26/27/28fix stable. No game code changed. Existing untracked ProbeCpuSortie.cs.txt now accepts shared Content and starts fixture after current-turn newspaper. Initial cpu-current-run failed settings entrance before CPU: old fixture did not account for newly inserted newspaper; not a confirmed product defect. Updated fixture cpu-current-build2/run2 both exit0/noTIMEOUT, commands/PIDs/start/end/limits600/300s/logs above, NAS current output reused. - World84/cast123/placement456,960x540 rendered CPU normal UI: settings on/off/manual transitions, computer domestic hangar/check, acting conqueror view, normal military map/target/picker, individual3 robot card clicks matching AI IDs, confirmation without duplicate planned orders, real redcheck and inspector close, human return passed. First batch39 orders/peaceful0/fallback0, second35 orders/queen1/pocketOnly0/peaceful0/fallback0/player0Domestic. New count35 is current observed AI behavior, not forced back to historical30. CPU7 overlap target105 path passed through existing corrected hit handling. Second queen result preserved exact queen participation and no reservation peak growth. - Visually reviewed current CPU redcheck screenshot: reserved-target check and handle click drawn in correct CPU color/inspector at960x540. Preserved outside tmp NAS demo-click-audit/cpu-current-red-53c9ed61.png and cpu-current-green-53c9ed61.png. This is helper/scripted AI tick rendering, not physical desktop input. No new DEMO defect; partial CPU audit complete. Current normal/off/skip end-result equivalence, unfold variant and pocket-only execution not newly verified; next compare existing CONFIRM_COMPLETION normal/skip/off or second-military modes on same DLL, inspect actual branch reachability first. Avoid re-running already passed broad baseline without new need.

- Planned cpu-compare-normal 2026-11-19 08:43:01.697264+09:00 HEAD53c9ed61+Demo26/27/28fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-compare-normal.out/.err; AUDIT_WORLD84 CONFIRM_COMPLETION=normal, same DLL/Content/audio=null, no rebuild.
- End 2026-11-19 08:43:29.023954+09:00 PID31828 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-compare-skip 2026-11-19 08:43:29.063222+09:00 HEAD53c9ed61+Demo26/27/28fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-compare-skip.out/.err; AUDIT_WORLD84 CONFIRM_COMPLETION=skip, same DLL/Content/audio=null, no rebuild.
- End 2026-11-19 08:43:57.735349+09:00 PID14792 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-compare-off 2026-11-19 08:43:57.783451+09:00 HEAD53c9ed61+Demo26/27/28fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-compare-off.out/.err; AUDIT_WORLD84 CONFIRM_COMPLETION=off, same DLL/Content/audio=null, no rebuild.
- End 2026-11-19 08:44:26.018100+09:00 PID33032 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- CPU mode equivalence audit2026-11-19 start08:42 end 2026-11-19T08:44:59.7589172+09:00 HEAD53c9ed61+Demo26/27/28fix stable. No game/diagnostic code changed or rebuild; reused current CPU DLL. World84/cast123/placement456,960x540: CONFIRM_COMPLETION normal, skip and off all exit0/noTIMEOUT. Exact commands/PIDs/times and300s limits above; logs cpu-compare-{normal,skip,off}.out/.err in existing NAS tutorial-audit. - Parsed Marches, Cancellations and Battle participants lines: identical across allthree modes, no missing result fields. Normal/skip assert restored human0 Domestic with no preview; off asserts no preview and zero pending orders. Normal confirmation selects AI IDs without adding duplicate planned orders; skip injects Escape through HandleInput immediately after confirmation, not physical key input. Display-off resolves same AI batch without preview. Source fixture starts newspaper already read; no real saves written. - Partial mode comparison complete, no new DEMO defect. Summary with full three result sets preserved outside tmp NAS demo-click-audit/cpu-equivalence-53c9ed61.json; prior CPU rendered evidence remains current. Remaining: unfold variant, skipping other stages, second-batch/pocket-only equivalence, all seeds, physical desktop and natural dedicated direct-pair teaching route not newly verified. Next targeted uncovered case or changed-code regression; avoid repeating unchanged globe comparison.

- Planned cpu-net-normal 2026-11-19 09:01:29.502781+09:00 HEAD53c9ed61+Demo26/27/28fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-net-normal.out/.err; WORLD84 CONFIRM_COMPLETION=normal MILITARY_NET1, same DLL/shared Content/audio=null.
- End 2026-11-19 09:02:01.970513+09:00 PID12324 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-net-skip 2026-11-19 09:02:02.010479+09:00 HEAD53c9ed61+Demo26/27/28fix limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-net-skip.out/.err; WORLD84 CONFIRM_COMPLETION=skip MILITARY_NET1, same DLL/shared Content/audio=null.
- End 2026-11-19 09:02:32.318832+09:00 PID3104 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- CPU unfold audit2026-11-19 start09:00 end 2026-11-19T09:03:11.2249317+09:00 HEAD53c9ed61+Demo26/27/28fix stable. Initial instructions/audit/fixes/diff/processes reviewed; no game/test changes or rebuild. Existing current CPU DLL with WORLD84 MILITARY_NET1 CONFIRM_COMPLETION normal/skip: both exit0/noTIMEOUT,300s limits and exact commands/PIDs/times/logs cpu-net-normal/skip above. Normal transition uses StartMapTransition/UpdateMapTransition from diagnostic, not physical map button. - Verified net branch reached: CPU1 view, target89, globeFalse, after inspector camera adjustment logical1440,552 hits89 and selects89, normal picker opensstage3,3 individual AI-ID clicks, confirmation without duplicate reservation. Both normal and Escape-after-confirm restore human0Domestic. Parsed Marches/Cancellations/Battle participants match each other and previous same-DLL display-off result exactly; no fresh off run because map display does not alter that earlier off batch. No new DEMO defect. - Visually inspected960x540 normal net picker image: selected robot card and actual confirmation/cancel controls, unfolded map behind modal. Preserved outside tmp NAS demo-click-audit/cpu-net-selection-53c9ed61.png and cpu-net-equivalence-53c9ed61.json. Partial variant audit complete. Remaining physical inputs, skipping before/during robot selection or after redcheck, pocket-only/second-batch equivalence, other seeds and natural dedicated direct-pair teaching route unverified. Next targeted uncovered case or changed-code regression; do not repeat passed net/globe first-batch mode comparisons.

- Planned cpu-early-phase-skip 2026-11-19T09:21:04.921752+09:00 HEAD53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7+Demo26/27/28 fixes; existing DLL no rebuild; WORLD84 PHASE_TIMING1 PHASE_SKIP1, synthetic Escape during cut-in before map selection. Limit300s inclusive startup. Command ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll']; logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-early-phase-skip.out/.err; shared Content/audio=null.
- Started 2026-11-19T09:21:04.945830+09:00 PID27892 (Popen process creation).
- End 2026-11-19T09:21:33.722246+09:00 PID27892 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Early-phase skip audit2026-11-19 start09:20 end 2026-11-19T09:21:52.645750+09:00 HEAD53c9ed61+Demo26/27/28 stable; gameDLL SHA256 DF5F6026CF73698ED446B95ED164437AFFFAC86259B6A88E45C00A48949C8D7C. Instructions/records/diff/processes reviewed; no concurrent game/validation. Existing CPU diagnostic WORLD84 PHASE_TIMING1 PHASE_SKIP1 exited0/noTIMEOUT. Actual normal HandleInput receives synthetic Escape at cut-in age0.250 with stage0, computer batch active and CPU view already switched; preview completes and restores human0/Domestic. No new defect; no game/test change or rebuild. Exact command/PID/start/end/300s deadline and NAS cpu-early-phase-skip.out/.err above. Partial audit complete, no fresh screenshot because this branch checks immediate skip rather than cursor placement. This is not physical Escape and does not compare final marches/participants against display-off for this early phase. Previous comparisons cover after-confirm skip only. Remaining picker-selection/after-red-check skip, pocket-only/second-batch equivalence, physical input and dedicated natural direct-pair route unverified. Next those uncovered cases or relevant changed-code regression; prior primary/inspector/green-red results retained without repeating. No commit/push/publication.

- Planned cpu-late-skip-build 2026-11-19T09:41:39.326694+09:00 HEAD53c9ed61+Demo26/27/28; limit600s; command ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-late-skip-build.out/.err; WORLD84 CONFIRM_COMPLETION=skip SKIP_AFTER_RED=1, shared Content/audio=null. Diagnostic-only extension.
- Started 2026-11-19T09:41:39.353213+09:00 PID19608 Popen creation.
- End 2026-11-19T09:41:44.163434+09:00 PID19608 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-late-skip-run 2026-11-19T09:41:44.191739+09:00 HEAD53c9ed61+Demo26/27/28; limit300s; command ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-late-skip-run.out/.err; WORLD84 CONFIRM_COMPLETION=skip SKIP_AFTER_RED=1, shared Content/audio=null. Diagnostic-only extension.
- Started 2026-11-19T09:41:44.204843+09:00 PID2348 Popen creation.
- End 2026-11-19T09:42:16.232306+09:00 PID2348 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- After-red-check skip audit2026-11-19 start09:40 end 2026-11-19T09:42:37.247034+09:00 HEAD53c9ed61+Demo26/27/28 stable; same gameDLL SHA256 DF5F6026CF73698ED446B95ED164437AFFFAC86259B6A88E45C00A48949C8D7C. No game code changed. Existing untracked ProbeCpuSortie diagnostic adds optional SKIP_AFTER_RED condition only; build reference-only and run exit0/noTIMEOUT. Exact commands/PIDs/times/limits600/300s/logs cpu-late-skip-build/run above. WORLD84,960x540, CPU normal picker individual selection and confirmation precede stage4; advance existing UpdateComputerMarches once to stage5 after red handle check, before inspector close, then inject Escape through normal HandleInput. Restores human0Domestic, no preview remaining/no duplicate order assertion passed. Parsed Marches/Cancellations/Battle participants exactly equal previous same-game-DLL cpu-compare-normal and cpu-compare-off outputs. No broad rerun needed. No new DEMO defect. Partial state-transition audit complete; no new screenshot or physical Escape claim. Prior09:03 visuals still layout evidence only. Remaining skip during individual card selection, pocket-only/second-batch equivalence, physical input and dedicated direct-pair natural route unverified. Next targeted uncovered condition or changed-code regression. User work retained, no commit/push/publication.

- Planned cpu-mid-skip-build 2026-11-19T10:00:10.265413+09:00 HEAD53c9ed61+Demo26/27/28; limit600s command ['dotnet', 'build', 'artifacts/demo-click-audit/DemoAuditCpuSortie.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal']; logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-mid-skip-build.out/.err; WORLD84 SKIP_DURING_SELECTION1 shared Content/audio=null, diagnostic-only extension.
- Started 2026-11-19T10:00:10.287946+09:00 PID24716 Popen creation.
- End 2026-11-19T10:00:14.973147+09:00 PID24716 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-mid-skip-run 2026-11-19T10:00:15.012290+09:00 HEAD53c9ed61+Demo26/27/28; limit300s command ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll']; logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-mid-skip-run.out/.err; WORLD84 SKIP_DURING_SELECTION1 shared Content/audio=null, diagnostic-only extension.
- Started 2026-11-19T10:00:15.026859+09:00 PID34744 Popen creation.
- End 2026-11-19T10:00:43.311289+09:00 PID34744 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Mid-selection skip audit2026-11-19 start09:59 end 2026-11-19T10:01:07.486161+09:00 HEAD53c9ed61+Demo26/27/28 stable, gameDLL SHA256 DF5F6026CF73698ED446B95ED164437AFFFAC86259B6A88E45C00A48949C8D7C. Initial instructions/records/diff/processes checked; no competing run, no game-code changes. Existing untracked ProbeCpuSortie adds opt-in SKIP_DURING_SELECTION only. Reference-only build/run cpu-mid-skip-build/run exit0/noTIMEOUT; exact command/PID/start/end and600/300s limits above; existing NAS output/shared Content reused. WORLD84 at960x540: normal CPU map/picker selects first of3 AI robots then synthetic Escape through HandleInput while remaining2 unselected. Preview resolves once, pending orders0 and human0Domestic restored. Parsed Marches/Cancellations/Battle participants exactly equal previous same-game-DLL normal, display-off and after-red-skip outputs. No new defect. Partial state-transition audit complete; physical Escape/cursor alignment not established by this synthetic input comparison. No additional image retained; existing diagnostic capture may be overwritten as designed. Remaining pocket-only/second-batch outcome equivalence, physical desktop and natural dedicated direct-pair route unverified; next targeted remaining case or changed-code regression. User edits preserved, no commit/push/publication.

- Planned cpu-pocket-current 2026-11-19T10:22:41.331143+09:00 HEAD53c9ed61+Demo26/27/28; existingDLL/no rebuild; limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-pocket-current.out/.err; WORLD84 LEGAL_QUEEN1 POCKET_ONLY1 QUEEN5401, shared Content/audio=null. Fixed legal pocket state, not natural AI batch.
- Started 2026-11-19T10:22:41.353917+09:00 PID8024 Popen creation.
- End 2026-11-19T10:23:07.587613+09:00 PID8024 exit3762504530 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Pocket fixed-state audit2026-11-19 start10:21 end 2026-11-19T10:23:40.444272+09:00 HEAD53c9ed61+Demo26/27/28 stable; instructions/records/diff/processes checked, no game/test change or rebuild. Existing current DLL WORLD84 LEGAL_QUEEN1 POCKET_ONLY1 QUEEN5401 run exited3762504530/noTIMEOUT at diagnostic assertion Legal queen opens normal picker, ProbeCpuSortie line645 after three UpdateComputerMarches calls. cpu-pocket-current.out logs initial newspaper-read fixture only; .err full stack preserved on existing NAS output, exact command/PID/times/300s limit above. This case is FAILED/UNVERIFIED, not a passing pocket playback or new confirmed product defect: fixture uses a constructed legal order/current-node hostile ownership and calls playback without normal phase entry; current stage/cut-in age/target hit/fallback consumer are not logged, so failure source remains unclassified. Do not alter game based on assertion alone or mark old pocket coverage current. - Partial audit interrupted at picker opening; no new DEMO number, screenshot or repair. Next add diagnostic stage/age/view/target/management trace around the three updates, compare legal queen without pocket and normal phase setup, then reproduce through reachable UI before any product repair. Existing normal/mid/late-skip outcome comparisons remain valid for their different fixture. Physical/pocket-only equivalence/second-batch/direct-pair scope remains unverified. User work protected, no commits/push/publication.

- Pocket fixture clarification2026-11-19 start10:24 end 2026-11-19T10:25:41.376811+09:00 HEAD53c9ed61+Demo26/27/28 stable. Static CanPreviewComputerMarch excludes PocketOnly with Fighters0. Previous10:21 diagnostic left QUEEN_ESCORTS unset, created zero fighters/QueenFalse, so its picker assertion did not test a supported pocket sortie. Corrected invocation only: existing QUEEN_ESCORTS1 creates2 actual pocket robots744/745; same DLL no rebuild/game/test edits. cpu-pocket-two-current exit0/noTIMEOUT; exact commands/PID/300s limit/logs above in fixes. Both individual IDs selected, queen participation remains off, confirmation adds no duplicate reservation, real redcheck/inspector close and normal completion restore human. Resolved encounter contains expected744/745 and retinue count2. Screenshot960x540 visually reviewed and preserved outside tmp NAS demo-click-audit/cpu-pocket-two-53c9ed61.png: red checked target and normal inspector on globe. Header remains Domestic because fixture directly invokes playback rather than entering military normally; not natural phase teaching proof. No confirmed game defect/DEMO ID or product repair. Diagnostic failure classified as invalid zero-fighter fixture; supported fixed-state pocket input passes, while normal/off/skip final-outcome equivalence, second military batch and physical input remain unverified. Review complete; next natural phase/pocket mode comparison rather than repeating zero-fighter case. No commit/push/publication.

- Planned cpu-second-normal 2026-11-19T10:41:07.462441+09:00 HEAD53c9ed61+Demo26/27/28; existingDLL/no rebuild limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-second-normal.out/.err; WORLD84 SECOND_MILITARY1 SECOND_SKIP0, shared Content/audio=null.
- Started 2026-11-19T10:41:07.478505+09:00 PID4980 Popen creation.
- End 2026-11-19T10:41:38.322731+09:00 PID4980 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned cpu-second-skip 2026-11-19T10:41:38.356866+09:00 HEAD53c9ed61+Demo26/27/28; existingDLL/no rebuild limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-second-skip.out/.err; WORLD84 SECOND_MILITARY1 SECOND_SKIP1, shared Content/audio=null.
- Started 2026-11-19T10:41:38.373208+09:00 PID12324 Popen creation.
- End 2026-11-19T10:42:10.685861+09:00 PID12324 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Second-military audit2026-11-19 start10:40 end 2026-11-19T10:42:36.398358+09:00 HEAD53c9ed61+Demo26/27/28 stable. Initial instructions/records/diff/processes reviewed; no game/test edits or rebuild. Existing current CPU DLL WORLD84 SECOND_MILITARY1 with SECOND_SKIP0/1 runs both exit0/noTIMEOUT. Commands/PIDs/times/300s deadlines/logs cpu-second-normal/skip above in existing NAS output. Second outcome lines (sorted marches, all node owners and queen marches) match exactly between normal and skip at featured queen stage3; both restore human0Domestic. This is existing UpdateComputerMarches(skip=true) diagnostic injection, not physical or HandleInput Escape. First batch is run normally in both conditions; previous first-batch comparisons not newly repeated as separate cases. No new defect. Partial outcome audit complete, no new screenshots retained. Normal featured queen redcheck/close and no-fallback count are in logs; all battle participants and display-off second batch are not compared by Second outcome signature. Remaining pocket-only mode equivalence, second-batch display-off/participant equality, physical desktop and natural dedicated direct-pair route unverified. Next remaining specific case or relevant new code/spec change. No commit/push/publication; user edits preserved.

- Planned cpu-second-off 2026-11-19T11:02:55.055531+09:00 HEAD53c9ed61+Demo26/27/28; existingDLL no rebuild limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll']; logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-second-off.out/.err; WORLD84 SECOND_MILITARY1 SECOND_OFF1, shared Content/audio=null.
- Started 2026-11-19T11:02:55.087946+09:00 PID31548 Popen creation.
- End 2026-11-19T11:03:29.682332+09:00 PID31548 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Second-military display-off audit2026-11-19 start11:02 end 2026-11-19T11:03:50.148014+09:00 HEAD53c9ed61+Demo26/27/28 stable; instructions/audit/fixes/diff/processes reviewed, no concurrent validation/game. No game/test edits or rebuild. Existing diagnostic WORLD84 SECOND_MILITARY1 SECOND_OFF1 exit0/noTIMEOUT; exact command/PID/start/end/300s limit and cpu-second-off.out/.err above. Initial military runs normally; before second OpenMilitaryPhase all CPU operation displays are disabled. Second preview absent (visible0), human0Domestic restored. Parsed Second outcome signature exactly equals prior same-DLL cpu-second-normal and cpu-second-skip: ground marches, node owners and queen marches. No new defect. Partial outcome audit complete. Signature does not include complete battle participant IDs or pocket-only mode equivalence; no claim those passed. Physical input and natural dedicated direct-pair remain unverified. No new screenshot/output copy needed, NAS log retained. Next targeted pocket comparison/participant signature or changed-code regression; do not repeat matched second-outcome runs without new reason. User changes protected; no commit/push/publication.

- Direct-delivery coverage audit2026-11-19 start11:22 end 2026-11-19T11:23:52.400491+09:00 HEAD53c9ed61+Demo26/27/28 stable. Required instructions/audit/fixes/diff/processes reviewed; no running game/validation, no game/test edits or new build/run. Static review Game1.Demo.cs/DemoDomestic.cs/DemoDomesticActions.cs/DemoChecks.cs and DirectDelivery/DirectPairTransition plus DemoUi/Inspector fixtures: natural continuation records directMap/directPair flags but only asserts newspaper/turn+2/transports>=2. Therefore its existing PASS never establishes direct-map/pair entry; keep dedicated natural path unverified. No direct-pair opening call found in Demo partial files by rg OpenDirectMap/_directPairOpen/UpdateDemoDirect; normal DirectDelivery opening is gated by DomesticOperationsAllowed and uses separate handlers, so this is a coverage/teaching-path question requiring reachable scenario, not yet a click-location defect or new repair ID. Existing DirectDeliveryUi and ProbeDirect800 force fixture/direct mode and test shared rectangles/transition, not natural demonstration entry. - Static partial audit complete. Priority primary retreat/return and left/right direct inspectors retain prior specific tested coverage; no broad repeat justified by unchanged code. Next use a reachable domestic direct-delivery scenario and observe whether Demo enters/avoids it intentionally before proposing missing-path implementation; physical desktop approval and complete natural direct-pair route remain unverified. Remaining pocket mode/participant equivalence unchanged. No commit/push/publication; user changes preserved.

- Primary/direct-inspector static audit2026-11-19 start11:39 end 2026-11-19T11:41:17.405507+09:00 HEAD53c9ed61+Demo26/27/28 stable; initial instructions/records/diff/process checks complete, no conflicting run. No game/test edits or rebuild. Game1.MapOverlays.cs current PrimaryActionCount9, DirectMapAction3/LongMapAction7, FunctionTransfer6 and Trade8; shared PrimaryActionBounds supplies220px animated displacement for every nonpinned action and MapOverlayClick rejects both home/current bounds when inactive via Enumerable.Range(0,9). Static rectangle reuse and all-nine suppression are consistent. tests/DirectDeliveryUi still uses i<6 in baseline/retreat loops; it does not establish retreat/return for newer6..8. Concrete coverage gap, not runtime proof of misclick; no new DEMO product ID. Direct-pair tests construct mode directly as recorded11:23; physical/natural path unchanged. Static partial audit complete. Next extend existing direct-delivery diagnostic to derive PrimaryActionCount, check added actions at animated halfway/full retreat/return and rendered left/right inspectors against current DLL. Do not change UI spec or assert passing runtime for untested6..8. Pocket mode/participant equivalence and physical desktop remain unverified. No commit/push/publication; user work preserved.

- Planned direct-nine-build 2026-11-19T12:01:35.302374+09:00 HEAD53c9ed61+Demo26/27/28; limit600s cmd ['dotnet', 'build', 'tests/DirectDeliveryUi/DirectDeliveryUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--source', 'artifacts/nuget-feed', '--source', 'C:/Users/muzud/.nuget/packages', '--disable-build-servers', '-p:NuGetAudit=false', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\direct-nine-build.out/.err; shared Content/audio=null. DirectDeliveryUi diagnostic-only nine-primary extension; no game code change.
- Started 2026-11-19T12:01:35.325847+09:00 PID16248 Popen creation.
- End 2026-11-19T12:01:51.180644+09:00 PID16248 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned direct-nine-run 2026-11-19T12:01:51.220174+09:00 HEAD53c9ed61+Demo26/27/28; limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DirectDeliveryUi\\debug\\DirectDeliveryUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\direct-nine-run.out/.err; shared Content/audio=null. DirectDeliveryUi diagnostic-only nine-primary extension; no game code change.
- Started 2026-11-19T12:01:51.236917+09:00 PID4840 Popen creation.
- End 2026-11-19T12:02:26.942265+09:00 PID4840 exit3762504530 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned direct-nine-build2 2026-11-19T12:03:18.287747+09:00 HEAD53c9ed61+Demo26/27/28; limit600s cmd ['dotnet', 'build', 'tests/DirectDeliveryUi/DirectDeliveryUi.csproj', '--artifacts-path', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit', '--no-restore', '--disable-build-servers', '-p:UseAppHost=false', '-p:BuildProjectReferences=false', '-v:minimal'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\direct-nine-build2.out/.err; sharedContent/audio=null. First run passed nine-primary checks then frame18 HandleInput seam click failed; diagnostic now explicit newspaper read/autoSave false. No game change.
- Started 2026-11-19T12:03:18.311657+09:00 PID32452 Popen creation.
- End 2026-11-19T12:03:22.669829+09:00 PID32452 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Planned direct-nine-run2 2026-11-19T12:03:22.716650+09:00 HEAD53c9ed61+Demo26/27/28; limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DirectDeliveryUi\\debug\\DirectDeliveryUi.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\direct-nine-run2.out/.err; sharedContent/audio=null. First run passed nine-primary checks then frame18 HandleInput seam click failed; diagnostic now explicit newspaper read/autoSave false. No game change.
- Started 2026-11-19T12:03:22.732444+09:00 PID21100 Popen creation.
- End 2026-11-19T12:03:49.856616+09:00 PID21100 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Direct nine-primary/input audit2026-11-19 start12:00 end 2026-11-19T12:04:19.751164+09:00 HEAD53c9ed61+Demo26/27/28 stable. Initial instructions/records/diff/processes reviewed; no game changes/concurrent validation. tests/DirectDeliveryUi/Program.cs derives PrimaryActionCount from current constant instead of two old i<6 loops, checks all9 baseline/no-overlap/full retreat and all inactive home clicks at half-slide. Added explicit newspaper-read fixture state and autoSave disabled. Initial direct-nine-run passed primary checks then failed frame18 alphabet click through HandleInput; no product defect inferred. Newspaper-read setup corrected that diagnostic premise: direct-nine-build2/run2 exit0/noTIMEOUT. Exact commands/PIDs/times/build600s/UI300s limits/NAS logs above. No repeated fullgame build, current reference reused. - Both added controls6..8 and prior0..5 satisfy expected rectangle/retreat checks. Full DirectDeliveryUi now passes shared primary retreat/return/input suppression, globe/rotated-net left/right inspectors, pair transition/continuous switches,960x540 Canvas HandleInput including seam attachment, drag suppression and reservations/save-restore. Underlying fixture opens direct mode/handlers; not natural Demo entry or physical hardware input. Final960x540 direct22 drawing visually reviewed: left/right hangars and central map with close/green-check handle. Preserved outside tmp NAS demo-click-audit/direct-nine-inspectors-53c9ed61.png. Dense all-node routes are fixture layout, no new click defect. - Partial audit complete. No new DEMO ID or game repair. Remaining natural dedicated Demo direct-pair entry, physical desktop, pocket mode/complete participant equivalence unchanged. Next targeted remaining route or relevant specification change, avoid unchanged broad reruns. User changes protected, no commit/push/publication.

- Planned cpu-pocket-close-skip 2026-11-19T12:22:25.469451+09:00 HEAD53c9ed61+Demo26/27/28; existingDLL no rebuild limit300s cmd ['dotnet', 'Z:\\muzudho-with-ai-agent\\FolderVerse\\tmp\\delete-after-2026-11-19\\tutorial-audit\\bin\\DemoAuditCpuSortie\\debug\\DemoAuditCpuSortie.dll'] logs Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit\cpu-pocket-close-skip.out/.err; WORLD84 LEGAL_QUEEN1 POCKET_ONLY1 QUEEN_ESCORTS1, QUEEN540 unset uses existing direct skip after close. Shared Content/audio=null.
- Started 2026-11-19T12:22:25.488975+09:00 PID22704 Popen creation.
- End 2026-11-19T12:22:47.591398+09:00 PID22704 exit0 TIMEOUT=False HEAD-after 53c9ed6122a09b0b68ff1cbd5e364d53fb2e58b7

- Pocket close-skip audit2026-11-19 start12:21 end 2026-11-19T12:23:06.420162+09:00 HEAD53c9ed61+Demo26/27/28 stable. Initial instructions/records/diff/processes reviewed; no game/test changes or build. Existing fixed legal2-pocket-robot fixture uses default QUEEN540 unset: normal picker selections/confirmation/redcheck/close, then UpdateComputerMarches(0,true), rather than normal540 completion. cpu-pocket-close-skip exit0/noTIMEOUT; exact command/PID/start/end/300s limit/logs above. Participant line exactly matches prior same-game-DLL normal540: actual307,308,309,744,745 expected744,745 encounters1 retinue2. Both assert no queen role, each selected pocket ID exactly once and human return. No new defect. Partial comparison complete: resolution differs by existing diagnostic branch and skip is direct handler injection, not physical Escape; no all-world-state/off equivalence claim. Existing preserved540 redcheck image remains visual evidence for normal path only. Remaining pocket display-off/all-state equality, second-batch complete participant signature, natural dedicated Demo direct-pair and physical desktop unverified. Next specific remaining condition or relevant changed-code regression. No commit/push/publication; user changes protected.

- Unchanged Demo audit follow-ups: first2026-11-19 12:39, latest 2026-11-19T20:21:48.7880488+09:00, count24. HEAD53c9ed61+Demo26/27/28 stable; initial AGENTS/README/audit/fixes/diff/processes checked, no concurrent game/validation. Static DemoChecks still uses shared WarButton, visible invasion row plus inspector offset, robot/queen picker rectangles, InvasionPickerDone, NodeCheckButton and InspectorClose; no obsolete separate reservation-confirm click introduced. No new defect or relevant product change, no game/test edits/build/repeated runtime. Review complete. Latest direct-nine12:04 and pocket close-skip12:23 results retain their documented limits; natural dedicated direct-pair, pocket display-off/all-state equality, full second-batch participant signature and physical desktop remain unverified. Next targeted remaining condition or relevant code/spec change; no commit/push/publication. Aggregate unchanged future reviews here.

### 2026-11-19 20:52 Demo点検：案内発光追加との差分照合

- 開始20:52:55 JST、終了2026-11-19T20:53:45.3301179+09:00、静的点検完了。HEAD2200ceb9df0487b3515d6c3416dc0872984c109b。AGENTS/README/両記録/現行差分とプロセスを確認。前回53c9ed61から既存DEMO-0026/27/28がコミット・マージに入り、ProductionIntakeInputの優先順・同地点ラベルの新仕様が加わったため、その変更を維持した。今回のゲームコード変更なし。
- 利用者指定の次操作発光は未コミットのGame1.TutorialActionGlow.csとGame1.csの描画呼出し。入口で_demoActive/_autoPlayを除外し、Demoへ人間向けの発光を出さない。直前のTutorialPlanetMenuUi最終実描画検証20:47–20:48でDemo時の対象なしを確認済み。同じ条件の再実行はしない。掲示板・割合・工場ランプの差分は時間依存の色を定常値へ置き換えたもので、矩形・入力・AI判断は変更されていない。
- DemoDomesticとDemoChecksの共有矩形参照を照合。緑チェック・赤チェック・出撃確定・閉じる・ターン終了は引き続き実ボタン中心を参照し、削除済みの別予約確定ボタンを追加していない。今回の差分から新たなクリック位置不具合は確認していない。新DEMO連番なし。
- 制約：今回の実施範囲は現行差分と共有矩形・発光除外条件の静的照合。Demoの実描画・自然巡回は再実行していない。旧HEADの成功はその条件の証拠として保持し、現HEADで全経路が正常とは扱わない。自然な直送便ペアDemo、CPUポケット表示オフの全状態比較、2回目軍事の全参加ID、OS実マウスの通し操作は引き続き未確認。次回は現HEADのDemoInspectorUi/DirectDeliveryUiで左右矩形と退避復帰を優先。利用者の差分とセーブを保持、コミット・プッシュ・公開なし。

### 2026-11-19 21:01 Demo点検：現HEADのインスペクター実描画回帰

- 開始21:01:25 JST、終了2026-11-19T21:03:33.5735079+09:00、実施範囲の点検完了。HEAD2200ceb9と案内発光の未コミット差分。AGENTS/README/点検・修正記録/差分/実行プロセスを確認。ゲーム・テストコードの変更なし。
- NAS既存出力で dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit -p:UseAppHost=false --no-restore。PID8112、21:01:55–21:02:19、上限600秒、exit0、警告0・エラー0。続いて同出力/bin/DemoInspectorUi/debug/DemoInspectorUi.dllをdotnetで実行。PID18852、21:02:19–21:02:47、上限300秒、exit0、TIMEOUTなし。Content=同出力/bin/DemoUi/debug/Content、ALSOFT_DRIVERS=null。ログdemo-glow-inspector-build.log / demo-glow-inspector-run.log。
- world123/cast456/placement789。Demoの地点・監督同期、地点事務所、閉じるアニメーション、輸送プライマリー、わたしの国から移動、人数設定、実クリック矩形をPASS。工場6プランについて1080p/540pで現行製造ボタン矩形→プラン選択→製造再開をPASS。960×540のプラン選択画面を目視しNAS demo-click-audit/demo-factory-540-2200ceb9.pngへ保存。新たな不具合なし、DEMO連番追加なし。
- 制約：診断の固定状態・ハンドラー呼出しと実描画の範囲で、OS実マウスや全Demo自然巡回ではない。左右配置・直送便の退避復帰はこのテストだけで全面確認したとしない。次回は現HEADのDirectDeliveryUiで9プライマリー退避復帰・左右ペア矩形を確認。自然な直送便ペアDemo、CPUポケット表示オフの全状態比較、2回目軍事の全参加ID、実デスクトップは未確認を保持。利用者の差分と保存データを保護、コミット・プッシュ・公開なし。

### 2026-11-19 21:18 Demo点検：現HEADの直送便・左右回帰

- 開始21:18:55 JST、終了2026-11-19T21:20:56.2036794+09:00、実施範囲の点検完了。HEAD2200ceb9と利用者指定の案内発光差分。AGENTS/README/両記録/差分/プロセス確認、ゲーム・テストコード変更なし。
- dotnet build tests/DirectDeliveryUi/DirectDeliveryUi.csproj --artifacts-path Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-19\tutorial-audit -p:UseAppHost=false --no-restore。PID16796、21:19:28–21:19:51、上限600秒、exit0、警告0・エラー0。dotnet 同出力/bin/DirectDeliveryUi/debug/DirectDeliveryUi.dll。PID4840、21:19:51–21:20:14、上限300秒、exit0、TIMEOUTなし。Content=同出力/bin/DemoUi/debug/Content、ALSOFT_DRIVERS=null。ログdemo-glow-direct-build.log / demo-glow-direct-run.log。
- 9プライマリーの比率・退避・復帰・移動中/非表示ボタンの入力抑制、閉じる/Escape、通常インスペクターのマーカー非表示、左右格納庫、直送便の予約・ドラッグ・保存復元、地図/地星儀と左右ペアの遷移、540pで回転した境界文字の面付替え・トラック連続クリック・予約維持をPASS。実描画direct-22.pngの左右インスペクターと閉じる/チェック位置を目視しNAS demo-click-audit/direct-nine-540-2200ceb9.pngへ保存。新たな不具合なし、新DEMO番号なし。
- 制約：固定fixtureの実描画・HandleInput/直接ハンドラー混在。テスト名のphysicalはMouseStateを入力へ渡す診断でありOSの実マウスではない。地図の密集表示はfixtureの全地点配置によるもので、新不具合とは登録しない。自然な専用直送便ペアDemo、全Seed/解像度、CPUポケット表示オフの全状態比較、2回目軍事全参加ID、OS実入力は未確認を保持。次回は未確認の具体的条件または新仕様の関係差分を優先し、同一成功回帰は反復しない。コミット・プッシュ・公開なし。

- 現HEAD変更なし点検集約：初回2026-11-19 21:40:55、直近2026-11-19T23:42:26.0152418+09:00、回数7。HEAD2200ceb9と案内発光の関係差分は21:18点検から不変（後続はチュートリアル診断・記録のみ）。AGENTS/README/両記録/差分/プロセス確認、競合するゲームや診断なし。DemoDomestic/Checksの共有ボタン参照も維持。新たな不具合なし、ゲームコード変更・ビルド・同一成功回帰の再実行なし。今回の静的確認は完了、自然な直送便ペアDemo・CPUポケット表示オフ全状態・2回目軍事全参加ID・OS実入力は未確認を保持。次回は具体的未確認条件または関係仕様変更。コミット・プッシュ・公開なし。

### 2026-11-20 00:00 点検：計略カード追加後のDemoインスペクター

- 開始00:00:58 JST、終了2026-11-20T00:03:50.0745431+09:00、実施範囲完了。HEAD9b233002195d9617104924c1b7345860857c3f55、検証前後不変。AGENTS/README/両記録/差分/プロセス確認。ゲーム・テストコード変更なし、利用者チャット差分保護。2200ceb9から計略カード・共有PrimaryActionBoundsと内政終了の変更あり、旧HEADの成功を流用せずDemoInspectorUiを現HEADで再実行。StratagemInterfaceはDemo/自動プレイを除外する条件を静的確認。
- dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-20\demo-audit -p:UseAppHost=false、PID30008、00:01:27–00:02:54、上限600秒、exit0、警告/エラー0。dotnet 同出力/bin/DemoInspectorUi/debug/DemoInspectorUi.dll、00:02:54–00:03:20、上限300秒、exit0、TIMEOUTなし。実行PIDはdemo-inspector-9b233002-run.log先頭。両ログは同NAS出力のdemo-inspector-9b233002-build.log / demo-inspector-9b233002-run.log。Contentは既存11月19日tutorial-audit/bin/DemoUi/debug/Contentを再利用、この素材参照は本検証の制約で計略画像の描画検証には用いない。
- 1080p/540pで工場の現行矩形と6種計画、実入力の状態更新、事務所、閉じるアニメーション、輸送プライマリー、わたしの国の移動、人数欄の既存アサーションPASS。描画付き診断の成功でありOS実マウス通し操作ではない。新DEMOなし。直送便左右・自然巡回・CPU表示オン/オフ/スキップの全状態・計略カード自体の操作は新HEADでは未確認、次回は直送便と計略UIを優先。コミット・プッシュ・公開なし。

### 2026-11-20 00:19 点検：新HEAD直送便診断の期待値不整合

- 開始00:19:28 JST、終了2026-11-20T00:21:38.2790621+09:00、診断中断・後続は要再確認。HEAD9b233002195d9617104924c1b7345860857c3f55は前後不変。AGENTS/README/両記録/差分/プロセス確認、競合なし、ゲーム・テスト変更なし、利用者差分保護。dotnet build tests/DirectDeliveryUi/DirectDeliveryUi.csproj --artifacts-path Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-20\demo-audit -p:UseAppHost=false、PID6884、00:19:56–00:20:34、上限600秒、exit0。dotnet 同出力/bin/DirectDeliveryUi/debug/DirectDeliveryUi.dll、PID27252、00:20:34–00:21:03、上限300秒、exit3762504530、終了回収済み、TIMEOUTなし。ログ同NAS出力demo-direct-9b233002-build.log / demo-direct-9b233002-run.log、Content同出力/bin/DemoInspectorUi/debug/Content、ALSOFT_DRIVERS=null。
- LoadContentのProgram.cs:32で「End turn uses 2:1.5 units」が失敗し、直送便操作前で停止。現行PrimaryActionBoundsは計略UIの手動操作でターン終了を192×96へ変更するが、診断は通常プレイの配置で旧192×144を無条件に期待。まず診断期待値を現行仕様とDemo状態に分ける必要があり、ゲームのクリックずれと断定せず新DEMO番号は付けない。旧HEADへUIを戻さない。次回はこの診断前提を最小更新して通常/手動/実Demo条件を分けて再実行し、9プライマリー退避復帰・左右格納庫・直送便予約とカメラ遷移を続行。これら後続経路と計略UI本体、CPU全状態、OS入力は新HEADでは未確認。コミット・プッシュ・公開なし。

- 00:19診断中断の追記（2026-11-20T00:26:49.1177933+09:00）：旧寸法と画面内途中位置の期待値を診断側で現行仕様へ更新、00:26のDirectDeliveryUiは後続の退避/復帰・左右・カメラ・予約維持までPASS。詳細はdemo-click-fixes.mdの00:23修正回。ゲームのクリック不具合は再現せず、新DEMO番号なし。自然Demo・計略カード本体・CPU全状態・OS入力の未確認は維持。

- 2026-11-20 00:40 静的点検：開始00:40:28 JST、終了2026-11-20T00:40:52.9657355+09:00、確認範囲完了。AGENTS/README/両記録/差分/プロセス確認。HEAD9b233002と製品コードは00:26成功から不変、実行競合なし。計略UIはTutorialStep==0・Demo/自動プレイ以外の条件を維持し、Demoの旧寸法と通常手動UIの新寸法を混同しない診断差分を保持。新規不具合なし、ゲーム/テスト変更と同条件の成功回帰再実行なし。00:26の左右・退避復帰成功を自然Demo巡回やCPU全状態の確認済みとは扱わない。次回は計略UIの実操作診断またはCPUポケット表示オン/オフ/スキップの具体的条件を優先。OS実マウスは未確認。コミット・プッシュ・公開なし。

- 2026-11-20 01:00 静的点検：開始01:00:00 JST、終了2026-11-20T06:01:32.6631138+09:00、確認範囲完了。AGENTS/README/両記録/差分/実行競合確認。HEAD9b233002と製品差分不変、競合なし。DemoDomesticの閉じる/輸送/切替は実UI共有矩形参照を維持。00:50チュートリアル点検の計略UI隔離診断は選択/予約/取消/手動復帰と描画PASS、新不具合なし。01:11計略UI診断では閉じる/再表示/Escapeの予約保持もPASS。01:29計略モデルの確定/競合拒否もPASSで新規不具合なし。02:09の現HEAD生成fixtureでも計略UI診断PASS。03:10計略の通常ターン終了競合拒否も隔離UIでPASS。01:00/01:19/01:41/01:59/02:20/02:39/03:01/03:19/04:01/04:21/05:41/06:00の静的確認を集約。04:01回は段階20の緑チェックと段階30–36の軍事経路分岐、共有NodeCheck/PopulationTurn参照も静的照合。新仕様の既存診断入口tests/OutpostRelief、tests/OffshorePPFacilityを確認したが今回は実行していない。04:21回はDemoChecks段階35/36のNodeCheckButton/InspectorClose中心とDomesticViewClick、予約済みなら段階19へ進む分岐を静的照合。04:11迎撃モデル診断の現HEAD成功はtutorial-audit.md参照、Demo実操作の代用にはしない。これはDemo自動操縦の計略利用や実ターン終了結果の検証ではない。05:41回はリポジトリtests配下を検索し、旧記録のDemoAuditCpuSortie専用診断ソースが通常tests一覧にないことを確認。旧NASのDLLだけを新HEAD証拠として再実行せず、再ビルド可能な元ソースを確認してからCPU回帰へ進む。06:00回は旧記録のartifacts/demo-click-audit/DemoAuditCpuSortie.csprojとProbeCpuSortie.cs.txt（90922bytes）、Entryを現存確認。通常base.LoadContent/セーブ無効、WORLD84/SECOND_MILITARY条件と2回目終了時の人間復帰検査を読み取り確認。新HEAD再ビルド/実行は次回、旧DLLを現行証拠にはしない。ゲーム・テスト変更と同条件回帰反復なし。次回は自然Demo/CPU表示モードと計略の閉じる・ターン終了結果など具体的未確認条件を優先。OS実マウスは未確認、利用者差分保護、コミット・プッシュ・公開なし。

### 2026-11-20 04:40 点検：現HEADの緑赤チェック実演経路

- 開始04:40:04 JST、終了2026-11-20T04:42:28.3486115+09:00、隔離診断範囲完了。AGENTS/README/両記録/差分/競合確認、HEAD9b233002前後不変、製品・テスト編集なし。dotnet build tests/DemoChecksUi/DemoChecksUi.csproj --artifacts-path Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-20\demo-audit -p:UseAppHost=false --no-restore はPID19736、04:40:40–41、上限600秒、exit1（診断用assets未生成）。同コマンドの--no-restoreを外しPID9624、04:40:58–04:41:41、上限600秒、exit0警告/エラー0。dotnet 同出力/bin/DemoChecksUi/debug/DemoChecksUi.dll、PID26568、04:41:41–04:42:06、上限300秒、exit0、TIMEOUTなし。ログ同出力demo-checks-current-build.log、demo-checks-restored-build.log、demo-checks-restored-run.log。ALSOFT_DRIVERS=null、FOLDERVERSE_TEST_CONTENT=同出力/bin/DemoInspectorUi/debug/Content、通常セーブ書込無効。
- 地図/地星儀それぞれ1920×1080と960×540の4条件で、実ハンドル緑チェック→開戦地点選択→出撃選択→確定即予約→実ハンドル赤チェック→閉じる→ターン終了→配送後進軍がPASS。赤チェック後のスポットライト除去と緑赤別管理、ターン終了時消去、削除済み最終確定ボタンに依存しないこともPASS。画面出力は同出力/bin/DemoChecksUi/debug/demo-checks-{scenario}-{step}.png。描画を伴う固定fixture/通常入力関数の診断で、今回は画像目視やOS実マウス通しではない。新DEMOなし。自然Demo・直送便ペア自然巡回・CPU表示オン/オフ/スキップの全状態・新仕様通常UIは未確認として維持。次回は具体的な未確認条件を優先し、同成功回帰を反復しない。通常セーブと利用者差分保護、コミット・プッシュ・公開なし。

- 2026-11-20 05:00 描画証拠確認：開始05:00:34 JST、終了2026-11-20T05:22:32.7622332+09:00、既存出力の目視範囲完了。HEAD9b233002と製品差分不変、AGENTS/README/両記録/差分/競合確認、実行競合なし。04:40診断のdemo-checks-3-6.png（地星儀960×540）を目視。左の出撃元リスト、予約済み表示、インスペクターハンドルの赤チェックと閉じる位置、地図上の赤チェックを確認。NAS demo-click-audit/checks-globe-540-9b233002.pngへ証拠を保存。新規不具合なし、ゲーム/テスト変更・ビルド・診断再実行なし。05:22回はdemo-checks-0-6.png（展開図1920×1080）も目視し、1機選択・予約1件・地図上赤チェックと実ハンドルのクリック位置を照合、NAS demo-click-audit/checks-net-1080-9b233002.pngへ保存。05:00/05:22の既存証拠確認を集約。他の画像目視・自然Demo・CPU全状態・OS入力は未確認として保持。次回は未確認条件または新しい関係差分、同成功回帰は反復しない。利用者差分と通常セーブ保護、コミット・プッシュ・公開なし。

### 2026-11-20 06:21 点検：現HEADのCPU通常UI・2回目軍事

- 開始06:21:05 JST、終了2026-11-20T06:23:20.1656350+09:00、固定fixture診断範囲完了。既読AGENTS/READMEハッシュ不変、両記録/差分/競合確認、HEAD9b233002前後不変、ゲーム/テスト編集なし。dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-20\demo-audit -p:UseAppHost=false、PID31972、06:21:33–06:22:13、上限600秒、exit0警告/エラー0。dotnet 同出力/bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll、06:22:13–06:22:51、上限300秒、exit0、TIMEOUTなし。正確な実行PIDは同出力cpu-second-current-run.log先頭。ログcpu-second-current-build.log / cpu-second-current-run.log。FOLDERVERSE_TEST_CONTENT=同出力/bin/DemoInspectorUi/debug/Content、ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_WORLD=84、FOLDERVERSE_AUDIT_SECOND_MILITARY=1、通常セーブ無効。
- CPUの通常UI経路・描画・人間復帰、2回目軍事35可視指示/専用退避0件/人間player0・Domestic復帰がPASS。征服者3のクイーンを含む指示ではviewed3を先に表示し、選択135→赤チェック→閉じるの段階遷移とクリック座標を出力。対象135でクイーンID-4、進軍1件、予約数0/0/0を確認。到着後の現在地や領有は勝敗の影響を受け、旧HEADとの一致を要求しない。新DEMOなし。これは独立世界の既存診断で自然ゲーム/OS実入力ではない。新HEADのスキップ/操作表示オフとの最終状態比較、全参加ID追跡、専用直送便自然Demoは未確認。次回は同DLLのスキップ/オフ比較を必要な範囲で行い、同一成功条件を反復しない。利用者差分・通常セーブ保護、コミット・プッシュ・公開なし。

- 2026-11-20 06:39 CPU表示比較点検：開始06:39:36 JST、終了2026-11-20T06:41:30.7759513+09:00、固定fixture比較範囲完了。既読AGENTS/README不変、両記録/差分/競合確認、HEAD9b233002前後不変、製品/テスト変更・再ビルドなし。06:21成功と同一現HEADのDemoAuditCpuSortie.dll、WORLD84/SECOND_MILITARY1を使用し、SECOND_SKIP1とSECOND_OFF1を別プロセスで順に実行。各上限300秒、両exit0、TIMEOUTなし、スキップ終了06:40:35、オフ終了06:41:07。正確な開始/PID/コマンドは同NAS demo-audit/cpu-second-{skip,off}-current.log先頭、素材/audio/セーブ抑止は06:21と同じ。
- 2回目軍事の通常/クイーン段階3スキップ/操作表示オフでSecond outcome行（全LastMarches・全地点owner・LastQueenMarches）が完全一致。人間player0・Domestic復帰もPASS、新DEMOなし。比較対象はこの固定世界と2回目軍事の結果署名であり、全ロボット個別ID・全状態・ポケット専用・OS実入力・自然Demoは未確認として保持。次回はそれらの具体的条件か新関係差分を優先し、同一比較を反復しない。利用者変更と通常セーブ保護、コミット・プッシュ・公開なし。

- 2026-11-20 06:59 マージ後静的点検：開始06:59:36 JST、終了2026-11-20T16:02:31.6582459+09:00、静的範囲完了。既読AGENTS/READMEハッシュ不変、両記録/差分/競合確認、HEADebdda80495c6f7b0880fcb029bb137f1ab1f280a。旧9b233002とのFolderVerse製品コード差分なし、未コミットはチュートリアル記録4行のみ、実行競合なし。DemoDomestic/Checksの閉じる・緑赤チェック・ターン終了は共有UI矩形中心の参照を維持。マージ済み過去診断の結果は元HEAD/条件に属する証拠として保持し、全経路の確認済みへ拡張しない。06:59/08:01/08:20/08:40/09:00/09:19/09:40/09:58/10:22/10:40/11:00/11:20/11:40/12:00/12:19/12:39/13:01/13:18/13:39/13:59/14:20/14:42/15:02/15:20/15:39/16:02の変更なし静的確認を集約。09:58回も指示ハッシュ/HEAD/既存5ファイル差分不変、実行競合なし。新たな実描画・自然遷移の検証なし、未確認経路の状態を変更しない。09:40回もHEAD/製品差分不変、実行競合なし。既存成功条件の再実行はせず、実描画/自然遷移の追加確認とは扱わない。09:19回はDemo.csのTransportでUpdateTransportへ委譲する分岐とDemoUiの自然巡回Transport回数検査を静的確認。画面Transport到達だけでは専用直送便の退避復帰ペアまで検証した証拠にならず、自然直送便の未確認を維持。09:00回は記録/計略診断5ファイルの既存差分のみ、製品変更・実行競合なし。後続07:20ポケット個別選択/人間復帰と07:41描画目視は別記の条件に限定。新DEMOなし、ゲーム/テスト変更・再ビルド・同条件反復なし。自然直送便Demo・全参加ID/全状態のCPU比較・ポケット専用・OS実入力は未確認。次回はそれらの具体的条件か関係仕様変更を優先。利用者のマージと起動中ゲーム/通常セーブ保護、コミット・プッシュ・公開なし。

- 2026-11-20 07:20 ポケットCPU出撃点検：開始07:20:06 JST、終了2026-11-20T07:21:39.6429265+09:00、固定fixture診断範囲完了。既読AGENTS/README不変、両記録/差分/競合確認、HEADebdda804前後不変。旧9b233002と製品/診断ソース差分なしを確認済みのため06:21ビルドの同一DLLを再利用、ゲーム/テスト変更・再ビルドなし。dotnet NAS demo-audit/bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll、PID33216、07:20:37–56、上限300秒、exit0、TIMEOUTなし。ログ同出力cpu-pocket-current.log。WORLD84、LEGAL_QUEEN1、POCKET_ONLY1、QUEEN_ESCORTS1、QUEEN5401、素材/audioは06:21と同じ、通常セーブ無効。
- 960×540で実ポケット兵744/745の個別カード選択、女の子参加オフ、確定後二重予約なし、実赤チェック・インスペクター閉じる・通常完走で人間player0復帰がPASS。戦闘に各指定IDが1回ずつ含まれクイーン未出撃を確認。他の戦闘参加兵307/308/309も存在するため、全参加ID集合が2機だけとは主張しない。画像は同出力/bin/DemoAuditCpuSortie/debug/audit-legal-queen.png、今回は目視未実施。新DEMOなし。直接ShowComputerMarchesを開始する固定fixtureで自然軍事フェーズ入口ではない。ポケット専用の表示オフ/スキップ全状態比較、自然直送便Demo、全Seed/OS入力は未確認。次回はこれら具体的条件、新関係差分を優先。同成功条件は反復しない。利用者差分・通常セーブ保護、コミット・プッシュ・公開なし。

- 2026-11-20 07:41 ポケットCPU描画証拠確認：開始07:41:06 JST、終了2026-11-20T07:41:56.8209801+09:00、既存出力の目視範囲完了。既読AGENTS/README不変、両記録/差分/競合確認、HEADebdda804製品不変、競合なし。07:20のaudit-legal-queen.png（960×540）を目視し、操作国の色/征服者、ポケット出撃2体・女の子参加オフ、赤チェックと閉じるハンドル、対象地図チェックの表示を確認。NAS demo-click-audit/cpu-pocket-540-ebdda804.pngへ証拠保存。上部が内政フェーズなのは診断が直接ShowComputerMarchesを呼ぶfixtureであり、自然軍事フェーズの案内整合性を確認済みにしない。新DEMOなし、ゲーム/テスト変更・ビルド・診断再実行なし。ポケットの表示オフ/スキップ全状態比較・自然直送便Demo・OS入力は未確認を保持。次回はそれら具体的条件または新関係差分。利用者差分・通常セーブ保護、コミット・プッシュ・公開なし。

- 2026-11-20 16:41/16:59 点検回：開始16:41:38 JST、直近終了2026-11-20T17:06:22.7570868+09:00、静的範囲完了。AGENTS/READMEハッシュ・HEAD ebdda804・既存5ファイル差分不変、実行競合なし。DemoDomestic段階12はTransportOverviewButton.Center→MapOverlayClick、段階11/16はInspectorClose.Center→DomesticViewClick、段階19はPopulationTurnButton.Center→EndPlannedTurnを参照することを確認。16:59回はDemo.cs:106のUpdateTransport委譲とDirectDeliveryUiのモード閉じる/Escape・ペアカメラ検査を静的再確認。DemoUiの自然巡回はoverview/directMap/directPairの観測フラグとTransport到達回数を保持するが、今回それらを実行しておらず直送便退避復帰ペアの成功証拠にはしない。ゲーム/テスト変更・ビルド・実描画/OS操作なし、新DEMOなし。次回は自然直送便で退避前後の状態と矩形を具体的に採取するかCPUポケットのスキップ/オフ比較を優先。通常セーブ・利用者変更保護、コミット・プッシュ・公開なし。

- 2026-11-20 17:24 点検回：開始17:24:09 JST、終了2026-11-20T17:31:34.7863407+09:00、固定CPUポケット終端スキップ診断範囲完了。AGENTS/README/HEAD ebdda804不変、既存6ファイル差分保護、実行競合なし。ゲーム/テスト変更・ビルドなし。既存NAS demo-audit/bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dllを実行、WORLD84/LEGAL_QUEEN1/POCKET_ONLY1/QUEEN_ESCORTS1、QUEEN540未設定で1080pの閉じる後UpdateComputerMarches(0,true)経路を検証。Content同出力/bin/DemoInspectorUi/debug/Content、ALSOFT_DRIVERS=null、通常セーブ無効。上限300秒、PID32632、17:31:12終了exit0、TIMEOUTなし・終了回収済み。正確な開始時刻は同出力cpu-pocket-end-skip.log先頭。
- 実ポケット兵744/745の個別選択・女の子参加オフ・即確定後二重予約なし・実赤チェック・閉じる・終端スキップ後player0復帰がPASS。参加者307/308/309/744/745、指定744/745は各1回、クイーン未出撃。通常完走540pの07:20と同じ指定ID検査結果だが、全状態署名比較/表示オフ/選択途中スキップは未確認。自然直送便退避復帰・OS入力も未確認保持。次回はそれら具体的条件か新関係差分。同成功条件の反復なし。利用者差分/通常セーブ保護、コミット・プッシュ・公開なし。

- 2026-11-20 17:42/18:00 点検回：開始17:42:10 JST、直近終了2026-11-20T18:00:41.3505112+09:00、静的範囲完了。AGENTS/README/HEAD ebdda804・既存6ファイル差分不変、実行競合なし。DemoChecks.cs段階34はInvasionPickerDone.Center、35は予約済み/表示/既チェックの条件を経てNodeCheckButton.Center、36はInspectorClose.Centerを参照する現行経路を再確認。段階19のターン終了はDemoDomesticに分離。18:00回も指示/HEAD/既存6ファイル差分不変、実行競合なし。追加の動的検証は行わず既存の未確認状態を維持。追加の実描画・自然遷移は未実施、新DEMOなし、ゲーム/テスト変更・ビルド・同条件診断反復なし。自然直送便退避復帰、CPU全状態/表示オフ/選択途中スキップ、OS入力は未確認を維持。次回はこれら具体的条件または新関係差分を優先。通常セーブ・利用者変更保護、コミット・プッシュ・公開なし。

- 2026-11-20 18:22 点検回：開始18:22:10 JST、終了2026-11-20T18:22:44.4508999+09:00、静的範囲完了・自然巡回は未実施。AGENTS/README/HEAD ebdda804・既存6ファイル差分不変、実行競合なし。DemoUiの自然継続は2ターン/2回Transport到達を終了条件とし、直送便overview/directMap/directPairは観測ログであって成立アサーションではないことを確認。NAS demo-audit/bin/DemoUi/debug/DemoUi.dllは存在せず、そのパスで再利用実行せず。既存の別診断成功で自然直送便の退避復帰を正常扱いしない。新DEMOなし、ゲーム/テスト変更・ビルドなし。次回は自然直送便の適切なfixture/観測点を決めて有限上限でビルド/実行し、退避前後の矩形と状態を採取する。CPU全状態・OS入力も未確認保持。通常セーブ・利用者変更保護、コミット・プッシュ・公開なし。

- 2026-11-20 18:41 点検回：開始18:41:41 JST、終了2026-11-20T18:45:02.2513065+09:00、自然Demo固定seed診断範囲完了。AGENTS/README/HEAD ebdda804前後不変、既存6ファイル差分保護、競合なし、ゲーム/テスト変更なし。dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path NAS demo-audit -p:UseAppHost=false、PID18892、18:43:23終了、上限600秒exit0。dotnet 同出力/bin/DemoUi/debug/DemoUi.dll、PID9908、18:44:35終了、上限300秒exit0、TIMEOUTなし・終了回収済み。正確な開始時刻は同出力demo-natural-build.log/run.log先頭。環境FOLDERVERSE_DEMO_TEST_SEED12/NATURAL_CONTINUE1/NORMAL_INPUT1、Content同出力/bin/DemoInspectorUi/debug/Content、ALSOFT_DRIVERS=null。
- 自然導入から新聞・後続計画/配送を2ターン進行しTransport2回到達PASS。overview=False/directMap=False/directPair=Falseのため、このseed・期間では直送便専用モードに未到達。直送便退避復帰の確認済みとはせず、現行Demoがその操作を見本として示す条件の追加調査が必要。通常入力関数診断でOS入力ではない。新DEMOなし。次回は段階12の実行条件/モード入口と自然巡回での到達可能性を調べ、専用fixtureの退避復帰とは区別する。利用者差分・通常セーブ保護、コミット・プッシュ・公開なし。

- 2026-11-20 19:00 点検回：開始19:00:11 JST、終了2026-11-20T19:00:46.6871798+09:00、入口切り分けの静的範囲完了。指示/HEAD ebdda804/既存6ファイル差分不変、残存検証なし。MapOverlays.cs:56の直送便入口はPrimaryActionBounds(3)→OpenDirectMap、DemoDomestic段階12のTransportOverviewButtonは同(4)であり別操作。18:41診断ログの観測フラグがfalseであることだけで直送便ボタンのクリックずれとは判定できない。現行Demoに直送便の見本経路が必要か、到達条件/専用fixtureと合わせて要確認。追加の実描画/自然巡回なし、新DEMOなし、製品/テスト変更・ビルドなし。次回は入口3の自然Demo実演の有無と退避復帰の到達条件を明示し、CPU全状態/OS入力の未確認も維持。通常セーブ・利用者差分保護、コミット・プッシュ・公開なし。

- 2026-11-20 19:20/19:39/19:59/20:21/20:39/21:00 点検回：開始19:20:41 JST、直近終了2026-11-20T21:00:39.6485746+09:00、静的範囲完了。指示/HEAD ebdda804/既存6ファイル差分不変、残存検証なし。19:39/19:59/20:21/20:39/21:00回も指示/HEAD/既存6ファイル差分不変・残存検証なし、追加実操作なし。19:59回の全ソース参照検索ではOpenDirectMapはMapOverlays入口3とLongDelivery経由のみ。Demoの直接OpenDirectPair参照も未検出。diff --check成功。Game1.Demo*.csをOpenDirectMap/_directMap/PrimaryActionBounds(3)で検索し直接参照なし。DemoDomesticの段階12/18は輸送概要/移動を使うため、現行の自然Demoが直送便専用の退避復帰を実演する経路は今回の読み取りでは確認できない。18:41の2ターン観測でも専用モード未到達。専用UIの描画/入力診断と自然Demo実演の有無を別の未確認事項として保持し、検索不一致だけで新DEMO登録・ゲーム変更はしない。追加の実描画/実操作・ビルドなし。次回は間接呼出しも含む自然実演の有無またはCPU未確認条件を確認。通常セーブ・利用者差分保護、コミット・プッシュ・公開なし。

- 2026-11-20 21:41 点検回：開始21:41:43 JST、終了2026-11-20 12:43:29 UTC UTC、読み取り再開。PowerShellの無出力が21:16以降継続したが、cmd.exeでは時刻に依存しないver/git/Python読み取りが応答した。自分の無応答確認セッションはすべて中断終了。AGENTS/READMEハッシュ不変、HEADfdc107c6。未コミットは両記録各2行、ヘッドレス仕様8行・診断8行、個人資料14行。ヘッドレス比較の利用者変更を保護し、MSBuild実行も観測したため新規ビルド/画面操作は行わない。新DEMOなし、実描画/自然直送便/OS入力の未確認は維持。今後は応答するシェルで必須確認し、同対象の実行競合を対象/PID/ログで照合してから再開。コミット・プッシュ・公開なし。

- 2026-11-20 21:59/22:19/22:39/22:59 点検回（22:19・22:39・22:59は指示/HEAD/関係差分の不変確認のみ、追加実操作なし）：開始21:59:44 JST、完了（読み取り範囲）。cmd経由で指示ハッシュ/HEADfdc107c6/差分/両記録を確認。利用者のヘッドレス比較関連差分は継続中。追加の実描画・回帰実行は未実施、新DEMOなし、製品/テスト変更・ビルドなし。新HEADの征服概要AUTO回帰・自然直送便・OS入力は未確認保持。次回は関係する現行回帰を優先し、利用者変更と通常セーブを保護する。コミット・プッシュ・公開なし。

- 2026-11-20 23:19 点検回：開始23:19:45 JST、静的範囲完了。指示ハッシュ・HEADfdc107c6不変、差分は先行チュートリアル記録2行追加のみで利用者のHeadless比較差分を保護。Game1.ConquestOverview.csの通常AUTO許可、TutorialStep1/2/3/4除外、同一ターン抑止、FinishConquestOverviewで概要フラグ解除を再読。tests/ConquestOverviewの--auto-onlyはAUTO/同一ターン/明示無効/Exhibition描画用であり、自然Demo後続の再開確認とは区別する。新規ビルド・実画面操作なし、新DEMOなし。新HEADの実行回帰・自然Demo復帰・直送便退避復帰・CPU未確認条件は未確認を保持。次回はこの未実行回帰を優先する。製品変更・コミット・プッシュ・公開なし。終了JST：2026-11-20 23:20:20.145166

- 2026-11-20 23:42 点検回：開始23:42:16 JST、準備段階で中断。AGENTS/READMEハッシュ・HEADfdc107c6・利用者差分不変。tasklistではdotnet/FolderVerse未検出、MSBuild23464は他作業の可能性があり存在だけで競合確定せず停止しない。C空き209454522368 bytesを独立読取で確認。NAS既存demo-auditを子プロセス15秒上限で読取確認するコマンドも無出力のまま継続したため、自分のセッション8881をCtrl+C終了exit1回収。内部タイムアウト実行/子終了を出力で確認できず、NAS接続回復とは扱わない。23:28の同種中断と併せて継続2回。ビルド/UI診断は未起動、製品変更・新DEMOなし。次回はNASアクセスを含まないローカル既存出力の調査から再開し、現HEAD実描画回帰と自然Demo復帰は未確認維持。通常ゲーム・セーブ・利用者差分を保護。終了JST：2026-11-20 23:43:38.967538

- 2026-11-20 23:58 点検開始：HEADfdc107c6、既読指示/差分不変。NAS応答不可の既報によりC空き209454522368 bytes確認済み。出力増殖を避け既存artifacts/tmp/delete-after-2026-11-18/tutorial-audit-localを現HEAD回帰に再利用し、当該出力の保持期限を2026-11-22 JSTまで延長（旧日付の自動削除候補から外す）。他作業headless-parityは変更しない。DemoUiの現HEAD再ビルド上限600秒、ログ同出力demo-current-build.log。後続UI診断は別コマンド300秒上限で実行し、ビルドのみでは完了判定しない。

- 2026-11-21 00:01 結果回収：23:58開始回は現HEAD回帰の診断範囲完了。dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path artifacts/tmp/delete-after-2026-11-18/tutorial-audit-local -p:UseAppHost=false、PID20500、2026-11-20 23:59:08–27 JST、上限600秒exit0/警告0/エラー0。続いてdotnet 同出力/bin/DemoUi/debug/DemoUi.dll、PID27984、2026-11-21 00:00:09–24 JST、上限300秒exit0、全TIMEOUTなし・終了回収済み。ログ同出力demo-current-build.log/demo-current-run.log。seed12/NATURAL_CONTINUE1/NORMAL_INPUT1、Contentは同出力FolderVerse/debug/Content、ALSOFT_DRIVERS=null。
- 通常入力処理経由で新聞後の内政/配送へ復帰、turn0→2・Transport2回、出撃確定の即予約1件・赤チェック後のインスペクター閉じるをアサーションPASS。1920×1080実描画normal-stage-36.pngを目視し、赤チェック・予約1件・閉じるボタンとDemoカーソルを確認。証拠artifacts/demo-click-audit/demo-red-check-fdc107c6-20261121.png、SHA256 e4f703baa5f54579b8d0984a90db9447b31f37e8003e8552cf1ef7ea17dc1c1b（原出力と一致）。NAS復旧後は参照/ハッシュ確認して移行する。
- HEAD前後fdc107c603e216ecb8889cc0ad8222ea10503c21不変、ゲーム/テスト変更なし、新DEMOなし。観測overview/directMap/directPairはfalseのままで専用直送便退避復帰を検証したとは扱わない。ConquestOverviewの個別AUTO表示回帰・CPU全条件・OS実入力通しも未確認を維持。NASへ再アクセスせず既存ローカル出力で実行検証を再開できた。終了JST：2026-11-21 00:01:16.564764

- 2026-11-21 00:22 点検回：開始00:22:16 JST、読み取り範囲完了、実行検証は保留。指示ハッシュ・HEADfdc107c6・製品差分不変、00:01自然Demo回帰結果を確認。00:12チュートリアル記録の容量障害はD空き402939904 bytesで継続し、D上の追加ビルド/画像出力は行わない。同成功条件の再実行なし。新DEMO・ゲーム変更なし。次回はNASまたは十分な空きのある出力先を確保して個別AUTO回帰/直送便退避復帰等を再開する。OS入力・CPU未確認条件も保持。利用者差分/通常セーブ保護、コミット・プッシュ・公開なし。終了JST：2026-11-21 00:22:43.970076

- 2026-11-21 00:39 点検開始：指示ハッシュ/HEADfdc107c6/製品差分不変。NAS無応答・D容量不足により、許可済みC作業領域の C:/Users/muzud/.codex/visualizations/2026/10/30/01a18ce8-f326-7083-b8e7-ba69c77ea956/artifacts/tmp/delete-after-2026-11-22/folderverse-audit に検証出力を分離する。C空き209839808512 bytes確認。保持期限11/22 JST、NAS復旧後は参照/ハッシュ照合して移行。ConquestOverview現HEAD回帰をビルド600秒・実描画診断300秒上限、ログconquest-build.log/conquest-run.log。製品コード変更なし。

- 2026-11-21 00:41 結果：C分離出力での現HEAD ConquestOverview回帰完了。ビルドPID13380、00:39:54–00:40:15 JST、上限600秒exit0/警告0/エラー0。dotnet C:/Users/muzud/.codex/visualizations/2026/10/30/01a18ce8-f326-7083-b8e7-ba69c77ea956/artifacts/tmp/delete-after-2026-11-22/folderverse-audit/bin/ConquestOverview/debug/ConquestOverview.dll --auto-only、PID26560、00:40:38–43、上限300秒exit0、TIMEOUTなし。両ログは上記C出力conquest-build.log/conquest-run.log。通常AUTOの概要表示・同一ターン抑止・明示無効と60人Exhibitionのグループ表示がPASS。1920×1080の生成画像を目視し、脱落24名/手下35名の集約欄と肖像の表示を確認。証拠C:/Users/muzud/.codex/visualizations/2026/10/30/01a18ce8-f326-7083-b8e7-ba69c77ea956/artifacts/demo-click-audit/conquest-auto-fdc107c6.png、SHA256 f67fceb3045991e9b00ffa5e43507a6c95b8845d85efadf9f830dcd79f5167e6、原出力一致。NAS復旧後に参照/ハッシュを確認して移行する。
- HEAD前後fdc107c6不変、新DEMO・ゲーム/テスト変更なし。通常AUTOの個別表示回帰は今回確認できたが、OS入力・自然Demoとの組合せ全分岐・専用直送便・CPU全条件は未確認を保持。D容量不足は解消したと扱わず、以後このC出力を共用して増殖を避ける。終了JST：2026-11-21 00:41:25.536458

- 2026-11-21 01:00 点検開始：指示ハッシュ/HEADfdc107c6・関係製品差分不変。先行チュートリアル診断終了済み。C既存folderverse-audit出力を共用し、DirectDeliveryUiでプライマリー矩形・退避復帰・左右インスペクター・540p入力/カメラの現HEAD回帰を確認する。ビルド600秒、診断300秒、ログdirect-build.log/direct-run.log。固定fixture回帰であり自然Demo専用直送便の実演有無とは区別する。製品変更なし。

- 2026-11-21 01:02 結果：DirectDeliveryUi現HEAD回帰完了。ビルドPID14036、01:01:27–34 JST、上限600秒exit0/警告0/エラー0。dotnet C共用出力/bin/DirectDeliveryUi/debug/DirectDeliveryUi.dll、PID4140、01:01:46–58、上限300秒exit0、TIMEOUTなし・終了回収済み。ログC共用folderverse-audit/direct-build.log/direct-run.log。プライマリー比率・画面外退避/復帰アニメーション・移動中/非表示入力抑止・閉じる/Escape、左右格納庫と予約保持、540p地星儀/展開図の連続トラック入力・カメラ復帰・継ぎ目文字の描画検査がPASS。実描画を伴うfixture診断で、追加スクリーンショット目視・OS入力通し・自然Demo専用直送便の到達確認とは区別する。HEAD前後fdc107c6不変、製品/テスト変更・新DEMOなし。次回は残る自然経路/CPU条件を優先し同条件反復なし。通常セーブ・利用者差分を保護、コミット・プッシュ・公開なし。終了JST：2026-11-21 01:02:34.140070

- 2026-11-21 01:19 点検回：開始01:19:47 JST、既存CPU軍事診断の範囲照合完了。指示ハッシュ/HEADfdc107c6/製品差分不変。DemoAuditCpuSortie.csprojとProbeCpuSortie.cs.txt:623–658を再読。LEGAL_QUEEN+POCKET_ONLY分岐は選択全完了→確定→赤チェック→閉じる後、QUEEN540で通常完走/非指定で終端スキップを選ぶ。一般軍事のSKIP_DURING_SELECTIONは別分岐873以降であり、ポケット専用中途選択スキップや表示OFFを既存フラグだけで確認したとは言えない。既報の未確認範囲を具体化し、新DEMO登録なし・製品/診断変更・ビルド/実操作なし。次回は独立fixtureでポケット専用の一部ID選択直後の通常Escapeと表示OFFの実行結果を通常完走と照合する必要がある。通常経路のShowComputerMarches直接呼出fixtureを自然軍事フェーズ検証と混同しない。利用者差分/通常セーブ保護、コミット・プッシュ・公開なし。終了JST：2026-11-21 01:20:34.532052

- 2026-11-21 01:40 点検開始：HEADfdc107c6/指示/関係製品差分不変。既存隔離診断ProbeCpuSortie.cs.txtに任意POCKET_SKIP_SELECTION分岐のみ追加し、ポケット1体選択直後の通常Escapeと指定全ID各1回/女王不参加/人間復帰を検査する。ゲームコード変更なし。C共用出力でビルド600秒・診断300秒、cpu-pocket-build.log/cpu-pocket-mid.log。world84/LEGAL_QUEEN1/POCKET_ONLY1/QUEEN_ESCORTS1/POCKET_SKIP_SELECTION1。最初の2コマンドはWindows引数長制限で実行前/編集前に失敗し、短いコマンドで追加済み。通常セーブ保護。

- 2026-11-21 01:45 結果：ポケット専用途中Escapeの隔離診断完了。追加ファイルは既存artifacts/demo-click-audit/ProbeCpuSortie.cs.txtの任意分岐のみ。初回ビルドPID8648、01:41:47–56 exit0、初回実行PID6272、01:42:11–24 exit3762504530、人間復帰アサーション停止。ShowComputerMarchesを直接呼ぶfixtureはDomesticのままCPU表示へ移るため、通常HandleInputにTryBeginTurnNewspaperが先行する条件があると静的確認。初回失敗を製品スキップ不具合と断定しない。
- 診断でCPU画面の新聞既読を現在ターン/PlayerSlotに合わせて再検査。ビルドPID16868、01:43:55–01:44:00 exit0、実行PID19892、01:44:15–22 exit0。ビルド600秒/UI300秒上限内、TIMEOUTなし・全終了回収済み。C共用出力ログcpu-pocket-build.log/cpu-pocket-mid.log/cpu-pocket-recheck-build.log/cpu-pocket-mid-recheck.log。1体選択直後に通常HandleInputへEscape、screenTransport/player0/再生false/予約0、戦闘参加307,308,309,744,745、指定ポケット744/745各1回、女王不参加をPASS。描画フレームを経る診断だが追加画像目視は未実施。
- HEAD前後fdc107c6不変、ゲームコード変更・新DEMOなし。表示OFFと通常完走との全状態比較、自然軍事フェーズでの新聞前提は未確認。新聞既読の直接設定によるfixture結果を自然入力全体の保証にしない。次回はその未確認条件を優先。利用者差分/通常セーブ保護、コミット・プッシュ・公開なし。終了JST：2026-11-21 01:44:55.445316

- 2026-11-21 02:00 点検開始：指示/HEADfdc107c6/製品差分不変。既存隔離CPU診断にPOCKET_OFF任意分岐を追加し、表示OFFでShowComputerMarchesがfalse→通常Campaign.Resolve/FinishMilitaryAdvanceとなる条件を検査。world84/LEGAL_QUEEN1/POCKET_ONLY1/QUEEN_ESCORTS1、通常画面切替なし/指定ポケットID各1回/女王不参加/人間維持を確認する。ビルド600秒・UI300秒、C共用出力cpu-pocket-off-build.log/cpu-pocket-off.log。製品コード無変更、自然Campaign.Advance経由の全状態比較ではない。

- 2026-11-21 02:03 結果：ポケット表示OFFの隔離診断完了。ビルドPID17192、02:01:52–59 JST、上限600秒exit0/警告0/エラー0。実行PID11436、02:02:14–20、上限300秒exit0、TIMEOUTなし・終了回収済み。C共用出力cpu-pocket-off-build.log/cpu-pocket-off.log。表示OFFで再生開始なし、player0維持/予約0/再生null、実参加307,308,309,744,745、指定744/745各1回・女王不参加をPASS。01:44途中Escapeの実参加ID列と一致。ただし全所有/在庫/勝敗状態の比較、通常完走との同HEAD同条件の全状態照合、自然Campaign.Advance経由は未確認。描画フレームは経るが表示OFFのため軍事操作の描画を確認したとは扱わない。HEAD前後fdc107c6不変、製品変更・新DEMOなし。診断の任意分岐追加のみ、通常セーブ・利用者差分保護、コミット・プッシュ・公開なし。次回は未確認の全状態照合または自然軍事経路。終了JST：2026-11-21 02:02:49.313655

- 2026-11-21 02:19 点検回：開始02:19:18 JST、比較境界の静的範囲完了。指示/HEADfdc107c6/製品差分不変。Game1.CampaignPhases.csのOpenMilitaryPhaseはCampaign.BeginMilitary→Campaign.Advance(allPlanned:true,deferResolution:ShowComputerMarches)→再生なし時FinishMilitaryAdvance。表示ONの終端もGame1.ComputerMarches.cs:79で同関数を呼ぶ。02:03のOFF診断は固定注文をResolveに渡す局所検査であり、BeginMilitary/AdvanceのCPU判断・他注文生成まで含む自然経路ではないことを再確認。次回全状態比較では初期world/cast/placement/注文とBeginMilitary後の境界を揃え、各経路でFinishMilitaryAdvance後の同じ時点を比較する必要がある。追加ビルド/実操作なし、新DEMO/製品変更なし。既存成功の反復なし、自然軍事・全状態・OS入力未確認を維持。利用者差分/通常セーブ保護、コミット・プッシュ・公開なし。終了JST：2026-11-21 02:20:13.799176

## 2026-11-21 05:26 新しい大幅改造の調査

- 利用者の「大幅改造したので調べて」に従い、HEAD 5e19a288f04a598b8523a605cf77119c8e8da164を確認。前回fdc107c6との差は165ファイル（3999追加/381削除）。仕掛品/加工合成/再開予約、製造の段階原料消費・開始条件、朝順の受付とCPU判断順、CPU計略、左インスペクター/地図/ナビゲーション配置を優先照合。AGENTS/README不変。開始時ツリーはclean、終了時の利用者個人資料編集中を保護。旧HEADの成功を新HEADへ流用しない。
- 旧OS入力ランチャー起動はnode_repl継続がexpiredとなり結果未取得。その後返却窓に検証ゲームなし、OsTutorialプロセスも未検出。OS入力は未実施。旧ランチャーを新HEAD実画面検証に使わない。

### DEMO-0029：国インスペクター移動予約のDemoクリックが306px左にずれる（完了：2026-11-21 05:49 JST）

- 再現：新HEADでtests/DemoInspectorUiをビルド/実行。役場→閉じる→輸送概要→閉じる→国表示まで進め、UpdateDomesticSlide(1f)後にDemoDomestic段階18をUpdateDemoDomestic(1.8d)で実行。
- 期待：描画された［移動予約］の中央をクリックし、国インスペクターから移動予約へ遷移する。
- 実際：段階18はMovementButton.Center=(377,938)のまま。NodeInspectorOffsetは全開時306となり、実描画矩形は(588,912,190,52)、中央(683,938)。MovementClickは地図外座標からoffsetを差し引くため旧位置クリックがボタンに当たらず、movement=false/reignView=trueのまま。Program.cs:87で「Demo did not open movement through the country inspector」。1920×1080。
- 関係：Game1.DomesticView.csの新NodeInspectorOffset、Game1.DemoDomestic.csの段階18、Game1.Movement.csの入力変換。TutorialActionGlowは既にoffsetを加えており、UI配置を戻す必要はない。修正回では描画/入力と同じ変換済み矩形をDemoから参照し、インスペクター内の確定ボタン等も照合する。
- 証拠：C作業領域artifacts/demo-click-audit/DEMO-0029-5e19a288.pngを目視、SHA256 534020b1ed586d7443993233de879b471373d4c55e6b9e4875ff22ea94c1af93（元demo-office-7.pngと一致）。NAS復旧後に参照/ハッシュ確認して移行。
- ビルド：dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path C共用folderverse-audit -p:UseAppHost=false、PID17616、05:26:33–05:27:08 JST、上限600秒exit0/警告0/エラー0。実行PID19708、05:27:31–49、上限300秒exit3762504530。両ログnew-head-inspector-build.log/new-head-inspector-run.log。TIMEOUTなし・終了回収済み。失敗以降の工場６プラン/540p回帰は未確認。
- 仕掛品モデルは新HEAD DLLをPublishedGameとしてtests/Workpiecesをビルド、PID25432、05:27:55–57、上限600秒exit0。実行PID24836、05:29:01–29、上限300秒exit0、367 assertions PASS。ログnew-head-workpieces-build.log/new-head-workpieces-run.log。加工/回収/保存/再開等のモデル範囲であり、新UI全操作や自然プレイ全条件の保証ではない。
- 調査範囲完了、ゲーム/テストコード変更なし。HEAD前後5e19a288不変。新HEADのチュートリアル通し・朝順CPU自然経路・新仕掛品UI全分岐は未確認。次回修正はDEMO-0029の最小修正と実描画/後続遷移、次回点検はこの未確認範囲から。通常セーブ・利用者差分保護、コミット・プッシュ・公開なし。
End_JST=2026-11-21 05:31:15.557381

- 2026-11-21 05:49 修正完了：DEMO-0029は共有InspectorScreenBoundsで現行描画位置へ追従。移動開始/確定予約一件と工場６計画の通常入力遷移を実描画診断でPASS（1080p/540p工場、移動1080p）。詳細・ログ・証拠・未確認範囲はdemo-click-fixes.mdの05:44修正回。自然全巡回やOS入力まで完了したとは扱わない。

- 2026-11-21 06:00 点検開始：指示ハッシュ/HEAD5e19a288/関係差分確認。05:49修正/05:57チュートリアル診断終了済み。大幅改造後未確認のDirectDeliveryUiでプライマリー・直送便退避復帰・左右格納庫と540p地図を照合。C共用folderverse-auditを再利用（NAS無応答/D容量制約、保持11/22）、ビルド600秒/診断300秒、ログdirect-new-build.log/direct-new-run.log。固定fixtureと自然Demo専用直送便経路を区別。ゲーム変更なし。

- 2026-11-21 06:02 点検結果：ビルドPID28076、05:59:53–59 JST、上限600秒exit0/警告0/エラー0。実行PID10752、06:00:12–28、上限300秒exit3762504530、TIMEOUTなし・終了回収済み。プライマリー比率・退避復帰アニメーション・移動中/非表示入力抑止・閉じる/EscapeはPASS。Program.cs:226で「Selecting direct delivery restores truck markers」に停止。
- 再現条件：通常インスペクターを開く→NetPanelへFitAll→次フレームOpenDirectMap→描画後にtrucks.Count>0を要求。現行DrawDirectTrucksは遷移中/地図アニメーション中・MapViewBounds外のルート中点を除外する。OpenDirectMapはResetInspectorするが、この診断は更新を待たず、狭いインスペクター時のFitAll位置をそのまま使う。原因はまだ実状態ログで特定していないため「要再確認」、製品バグ採番なし。次回は実MapViewBounds/NetPanel/アニメーション状態/ルート中点を照合し、通常更新と適切な画角で復帰するか確認する。
- 工場/移動の05:49修正済み結果とは別の条件。失敗以降の540p・連続トラック/左右切替・カメラ保持・継ぎ目描画は新HEADで未確認。先行fixture部分の成功を自然Demo専用直送便の保証にしない。HEAD5e19a288維持、ゲーム/診断コード変更なし。点検範囲完了、後続実行は中断。ログC共用direct-new-build.log/direct-new-run.logを保持。通常セーブ/利用者差分保護、コミット・プッシュ・公開なし。開始表記06:00は実ビルド開始05:59:53を含む。終了06:02 JST。

- 2026-11-21 06:20 点検回（終了06:24 JST、部分範囲完了）：AGENTS/READMEハッシュ・HEAD5e19a288・差分/両記録照合。06:09要再確認の画角条件を継続点検。tests/DirectDeliveryUi/Program.csへFOLDERVERSE_DIRECT_PAN任意診断のみ追加し、OpenDirectMap後に通常HandleInputで右ドラッグ/解放と更新を最大４回送る。ゲームコード変更なし、AI/予約/地形は変更しない。
- dotnet build tests/DirectDeliveryUi/DirectDeliveryUi.csproj --artifacts-path C共用folderverse-audit -p:UseAppHost=false、PID2980、06:22:11–18、上限600秒exit0/警告0/エラー0。dotnet 同出力/bin/DirectDeliveryUi/debug/DirectDeliveryUi.dll、DIRECT_PAN/PROBE各1、Content同出力FolderVerse/debug/Content、ALSOFT_DRIVERS=null、PID3192、06:22:30–39、上限300秒exit0。TIMEOUTなし・終了回収済み。ログdirect-pan-build.log/direct-pan-run.log、NAS/D制約と保持11/22は継続。
- 通常入力/更新後はMapViewBounds/NetPanelとも(40,142,1840,820)、地点0=(1214.0149,638.71497)、地点3=(1233.2849,523.095)、双方visible=true、trucks=1。右ドラッグだけでなく通常更新に伴うインスペクター退避/表示範囲復帰も含むため、パン単独効果とは断定しない。従来fixtureの直接呼出/更新省略条件での失敗と切り分け、製品DEMO採番なし。
- 後続の左右格納庫/予約維持・540p地星儀/展開図の連続トラッククリック・退避/復帰カメラ・回転した継ぎ目文字の実入力/アニメーション描画までPASS。540p左右格納庫画像を目視、C作業領域artifacts/demo-click-audit/direct-5e19a288-540.png、SHA256 0432da64910a14f8caa5801e4827e5c6dd4c195757b7b5f4172b3b50f16fa233。NAS復旧後参照/ハッシュ確認移行。
- HEAD前後5e19a288不変。06:02の後続未確認は今回の任意条件範囲では解消。自然Demo専用直送便・CPU朝順/全条件・OS実入力通しは未確認を維持。利用者差分/通常セーブ保護、コミット・プッシュ・公開なし。次回は自然Demo/CPU未確認条件を優先し同条件反復なし。

## 2026-11-21 13:09 最終処分場の大幅変更確認（利用者依頼）

- 現行HEAD5135aa54740a1fc9c762f3826c670167da5097a9。仕様コミット5a888987と確定仕様docs/設計/最終処分場とスクラップ.md、docs/dev/final-disposal.md、中核Scrap/解体/配送/回収/格納庫UIと既存差分を確認。利用者個人チャットの変更は保護。旧HEADの成功を流用せず、製品を再ビルド。
- 新規通常ゲームだけScrapRulesEnabledを有効化、既存セーブ/チュートリアルは従来ルール。最終処分場は港/大渦と分離して最低１つ、恒久施設/最低１格納庫を残す。解体進捗と製造仕掛品を分離し、スクラップは九分率、通常原料は十六分率。配送前に滞在期限処理、廃棄は１地点１全体ターン１格納庫分。
- 出力は既存C共用folderverse-audit（保持期限11/22 JST）を再利用。C空き199620100096 bytes。NAS再接続確認は短い子プロセス期限でも戻らず、自分の確認セッションを中断してローカルへ継続。元資料は変更/削除せず、NAS復旧後は参照とハッシュを照合して移す。
- dotnet build FolderVerse/FolderVerse.csproj --artifacts-path C共用folderverse-audit -p:UseAppHost=false、PID3772、13:12:21–48、上限600秒exit0/警告0/エラー0。ログdisposal-current-build.log。FinalDisposal/Workpieces/ProgressiveProductionを同出力bin/FolderVerse/debugへPublishedGame指定して各ビルド600秒上限exit0、ログdisposal-{名前}-build.log。
- dotnet 同出力/bin/{名前}/debug/{名前}.dll、GAME_RUNTIME=同出力bin/FolderVerse/debug、各上限300秒。FinalDisposal PID11080 13:13:09–20、108 assertions PASS。Workpieces PID28628 13:13:20–53、367 PASS。ProgressiveProduction PID17632 13:13:53–56、24 PASS。全exit0/TIMEOUTなし・終了回収済み。ログdisposal-{名前}-run.log。
- 独立描画ハーネスC作業領域artifacts/demo-click-audit/FinalDisposalUiを作成（製品無変更・通常セーブ/統計無効、独立ワールド）。初回はSDL未配置、次はPortrait素材基準ディレクトリ不足で実行失敗。既存同一SDL/OpenALをハッシュ照合して配置、素材基準をCゲーム出力へ設定しネイティブ参照を明示。最終ビルドdisposal-ui-native-build.log exit0、最終実行PID10288 13:17:12–19、上限300秒exit0、disposal-ui-native-run.log。準備段階の失敗ログは保持し製品不具合と混同しない。
- 実描画1080p/540pでスクラップ2/9束を通常HandleInputから廃棄予約→現物保持→取消、Escape閉じるをPASS。２解像度を目視、C作業領域artifacts/demo-click-audit/final-disposal-5135aa54-{1080,540}.png。SHA256順89e76424bd50a5f8616d2493a36b04060a8f4d5f1c7426ca3c70a051a09c4cd1、417899183d1ef44b86449f23f6dc14bde493d37e7db59fd75b0086885ac0f3ec。
- 範囲完了13:18 JST、HEAD前後5135aa54不変。今回確認範囲で新しい再現済み製品不具合なし。CPUの解体/搬出活用や長期収束、自然Demo全巡回、OS実入力通しは未確認。製品変更・コミット・プッシュ・公開なし。次回はこの新HEADを基準に未確認の自然経路を点検。

- 2026-11-21 13:19 点検回（終了13:21 JST、実施範囲完了）：指示ハッシュ/HEAD5135aa54/差分・両記録照合。最終処分場を取り込んだ新HEADでDemoUiの自然巡回を回帰。通常セーブ/統計を分離した既存診断、ゲームコード変更なし。
- dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path C共用folderverse-audit -p:UseAppHost=false、PID10900、13:19:30–38、上限600秒exit0/警告0/エラー0。dotnet 同出力/bin/DemoUi/debug/DemoUi.dll、PID2296、13:19:54–13:20:07、上限300秒exit0。SEED12/NATURAL_CONTINUE1/NORMAL_INPUT1、Content同出力FolderVerse/debug/Content、ALSOFT_DRIVERS=null。ログdemo-disposal-build.log/demo-disposal-run.log、TIMEOUTなし・終了回収済み。
- 通常HandleInput経由の新聞→内政復帰、turn0→2・配送２回、段階30→36の地図地点/出撃選択/確定即予約１件/赤チェック/インスペクター閉じる→陣営終了をPASS。実ワールドseed992459907/cast934883396/place883376777。直送便overview/directMap/directPairはfalseのため専用経路の成功とは扱わない。最終処分場自体の自然Demo実演・CPU朝順/計略/全状態・全Seed/OS入力は未確認を維持。
- HEAD前後5135aa54不変、新DEMO・製品/診断変更なし。C共用出力保持11/22、利用者差分/通常セーブ保護、コミット・プッシュ・公開なし。次回は未確認の専用直送便またはCPU条件から。

- 2026-11-21 13:21–13:22 点検回：指示ハッシュ/HEAD5135aa54/製品差分不変、直近自然Demo診断終了済み。13:20成功条件は反復しない。Game1.ComputerDomestic.csの通常格納庫/組立経路を静的照合し、ComputerHangarPoint・部位ドラッグ開始/終了・ページ操作の画面表示点にNodeInspectorOffsetが加算され、BeginComputerView/ShowComputerViewが操作する征服者へ先に切り替えることを確認。専用工場/解体の新操作まで再生する保証ではない。新DEMO・ゲーム変更なし。静的範囲完了、CPU表示ON/OFF/スキップ・人間復帰の新HEAD実行は未確認、次回はその条件へ。通常セーブ/利用者差分保護、コミット・プッシュ・公開なし。

- 2026-11-21 13:40 点検回（終了13:43 JST、部分範囲完了）：指示ハッシュ/HEAD5135aa54/両記録・差分照合。未確認だったCPU内政の表示OFF/通常/早期スキップ/部品保持中Escape比較を既存DemoAuditCpuSortieで実行。DOMESTIC_SKIP/DOMESTIC_ENTRY/AUTO_ENABLED/ASSEMBLY_SKIP_HELD/ASSEMBLY_ESC_INPUT各1、Content=C共用FolderVerse/debug/Content、ALSOFT_DRIVERS=null。固定３部位+人間予約fixtureで、通常世界生成や最終処分場のCPU活用条件ではない。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path C共用folderverse-audit -p:UseAppHost=false、各上限600秒exit0。dotnet 同出力/bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll、初回PID26324 13:41:13–25、状態ログ追加後PID31544 13:42:12–25、各上限300秒exit3762504530。TIMEOUTなし・終了回収済み。ログcpu-domestic-current-build/run.log、cpu-domestic-probe-build/run.log。
- OFFではComplete１体・Deliveryまで進行。ONは通常格納庫が開き、人間予約を保持、forecast=Completeまで通過したが、「Playback restores human and advances delivery」で停止。状態はplayer1/WorldStatus/Domestic、再生active、stage2/age3.1/steps有、HangarIntroProgress=0。Game1.HangarZoom.cs:18は(_animationTime-_hangarIntroStart)/.2、既存固定診断はUpdateを空にしてUpdateComputerDomesticだけを繰り返すため時刻が進まない。製品の進行不能と確定せず診断の時刻前提不足として要再確認、新DEMOなし。
- 追加変更はProbeCpuSortie.cs.txtの終了状態Consoleログ１行のみ、ゲーム変更なし。次回改善は通常の時刻更新を模擬/実行して格納庫導入を終えた条件で再比較し、ON完走/途中Escape/人間復帰・在庫ID同一を確認する。初回失敗以降は未確認を維持。HEAD5135aa54、C既存出力保持11/22、通常セーブ/利用者変更保護、コミット・プッシュ・公開なし。

- 2026-11-21 13:50 更新：13:43のCPU内政比較の要再確認は、診断の経過時間と現在操作中CPUの新聞既読前提を揃えた固定条件で解消。af4715cbで表示OFF/通常/早期スキップ/部品保持中Escapeの在庫ID・人間復帰が一致、実描画も確認。詳細はdemo-click-fixes.mdの13:45回。自然新聞・新規世界/最終処分場のCPU活用は未確認のまま。

- 2026-11-21 14:02 点検回（終了14:04 JST、部分範囲完了）：指示ハッシュ/HEADaf4715cb/差分・両記録照合、TUT-0007改善と先行CPU診断終了済み。最新共有UI差分を含めてCPU軍事ポケット専用出撃の通常表示/OFF/一部選択後Escapeを同じ固定注文で比較。ゲーム/診断コード変更なし。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --artifacts-path C共用folderverse-audit -p:UseAppHost=false、PID25016、14:02:33–38、上限600秒exit0/警告0/エラー0。dotnet 同出力/bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll、world84/LEGAL_QUEEN1/POCKET_ONLY1/QUEEN_ESCORTS1/QUEEN5401、Content同出力FolderVerse/debug/Content、ALSOFT_DRIVERS=null。通常PID20340 14:02:54–59、OFF PID8276 14:02:59–14:03:02、選択途中Escape PID31028 14:03:02–04。各上限300秒exit0、TIMEOUTなし・全終了回収済み。ログcpu-pocket-current-build.log/cpu-pocket-current-{normal,off,escape}.log。
- 540p通常UIの選択/確定/赤チェック/閉じる・人間復帰、表示OFF、指定ID１体選択後の通常EscapeがPASS。３条件の実参加ID307,308,309,744,745が一致、指定744/745各１回・女王不参加、Escape後Transport/player0/再生false/予約0を確認。実描画フレームを通る診断だが今回の追加画像目視は未実施。
- ShowComputerMarchesへ固定注文を渡す比較であり、自然BeginMilitary/AdvanceのCPU判断・朝順全状態・新規世界最終処分場の活用・OS入力までの保証ではない。未確認を保持し新DEMOなし。HEAD前後af4715cb不変、C既存出力保持11/22、利用者差分/通常セーブ保護、コミット・プッシュ・公開なし。次回は自然軍事の同じ比較境界を優先。

- Demo audit 2026-11-21 14:40:32.814714 JST (started 2026-11-21 14:39 JST): static scope complete, HEAD af4715cb and existing product/test differences unchanged; AGENTS/README and latest audit/fix records reviewed. No product edits or UI launch. Natural military comparison boundary confirmed: AutoSaveStore.Capture returns Document (Restore<WorldSetup> available), OpenMilitaryPhase uses Campaign.Advance with ShowComputerMarches and FinishMilitaryAdvance after playback. Fixed-order pocket checks remain distinct from natural CPU decisions. BattleInterception test inspected: covers adjacent interception and a rendering Preview that does not disable regular autosaves before base.LoadContent; therefore not launched as an isolated two-site-ahead test. Its existing success must not be counted as two-site-ahead UI verification. Next: isolate saves and cover actual ReliefDistances/InterceptOrigin distance-two conditions, or natural military comparison; direct-delivery natural Demo and OS input also remain unverified. No new confirmed DEMO; preserve user notebook and TUT-0007 changes. No commit/push/publication.

- Audit started 2026-11-21 14:59:21.259844 JST HEAD af4715cb: existing OutpostRelief model covers distance-two delays, friendly/hostile intermediate nodes, duplicate prevention and return; no UI or regular-save writes. Reuse current game binary freshly built 14:52; build600s/run300s limits, C shared relief-current-build/run.log, output retained Nov22 due existing NAS outage. Product unchanged.

- Completed 2026-11-21 15:00:10.267009 JST, model scope only: dotnet build tests/OutpostRelief/OutpostRelief.csproj --artifacts-path C shared folderverse-audit -p:UseAppHost=false -p:PublishedGame=C-shared/bin/FolderVerse/debug; PID25960 14:59:21-28 limit600s exit0 warnings0/errors0. dotnet C-shared/bin/OutpostRelief/debug/OutpostRelief.dll, FOLDERVERSE_PROBE_CONTENT_BASE=C-shared/bin/FolderVerse/debug; PID25788 14:59:40-43 limit300s exit0. No TIMEOUT; both exits collected, relief-current-build/run.log retained.
- PASS: distance-two node/edge delay12/16, no early arrival and exact due-turn deployment, pending arrival replay/save restoration, ordinary/hostile/intermediate-hostile exclusion and allied intermediate inclusion, seven robots split3/4 with no duplicate dispatch, undecided arrival cannot prolong finished battle, actual origin return. Current ReliefDistances restricts extended coverage to Frontline and allied routes. Dispatch model fixture only: normal UI target selection, InterceptOrigin natural incoming attack grouping, Demo narration and OS input remain unverified. No new DEMO or product/test change, HEAD before/after af4715cb unchanged, preserve TUT-0007 and user notebook; no commit/push/publication. Next natural military/actual UI rather than repeat this model.

- Static audit 2026-11-21 15:20:04.110231 JST (start15:19), complete within static scope. HEAD af4715cb/instruction hashes/product differences unchanged; user notebook and TUT-0007 protected. Shared InspectorScreenBounds still supplies assembly/factory/plan/movement/confirm positions in DemoDomestic. Green stage20 routes through DomesticViewClick then closes; war stages33/34 use current robot cards/Done, stage35 uses handle check and stage36 closes before turn-end. No removed war-confirm button restored; no new defect. Existing CPU second-military diagnostic calls OpenMilitaryPhase after FinishDomestic and logs an outcome, but initialization disables autosave only after base.LoadContent and does not disable statistics; review/save isolation needed before reusing it for a new natural comparison. No game/test changes or UI launch; previous own OS attempt is terminated and collected. Natural CPU full-state equivalence, natural direct-delivery Demo and physical UI remain unverified; next isolate that diagnostic then run supported natural paths, not repeat successful model cases. No commit/push/publication.

- Static audit 2026-11-21 15:42:12.033862 JST (start15:41), complete: instructions/HEAD af4715cb/existing differences unchanged. Correct the 15:20 isolation concern: Game1.AutoSave.cs13 and Game1.Statistics.cs8 initialize enabled flags only when entry assembly equals game assembly. DemoAuditCpuSortie has a separate entry assembly, so both default false before base.LoadContent; TutorialAchievements loading also returns when autosave is disabled. No evidence that this diagnostic read/wrote normal save, achievement or statistics data, and no isolation code edit is necessary solely for that ordering. No UI launch this round.
- Existing CheckSecondMilitary does enter FinishDomestic -> OpenMilitaryPhase and produces march/owner/queen signatures; unlike fixed-order pocket fixture it can cover natural second-batch decisions. It does not advance _animationTime in that loop and does not compare full state, so must observe actual stage progress/renderer before extending a guarantee. No new DEMO or product/test change. Next bounded current-HEAD natural second-military execution with explicit flags and common outcome boundary; OS capture/activation remains separately unverified. Preserve user data and TUT-0007; no commit/push/publication.

- Current second-military attempt 2026-11-21 16:01:21.221383 JST HEAD af4715cb: reuse legacy 1human59CPU fixed world fixture with natural second BeginMilitary/Advance; not new-world final-disposal coverage. SECOND_MILITARY1, other audit flags unset. Build600s/UI300s bounds, C shared second-current-build/run.log; retainNov22. Product unchanged and default separate-entry save/statistics guards confirmed.

- Natural second-military normal completed16:01:57; compare same deterministic fixture with SECOND_OFF1 then SECOND_SKIP1, each300s limit, logs second-current-off/skip.log. Normal has36 visible orders and one fallback ruler9 target27 stage2; no full-UI guarantee.

- Completed 2026-11-21 16:03:21.517626 JST, partial natural military comparison: build PID30232 16:01:21-26 limit600s exit0 warnings0/errors0; normal PID2852 16:01:41-57 limit300s exit0. OFF/skip logs above and current printed START/END retained, each300s exit0, no TIMEOUT/all exits collected. Normal36 visible orders, second natural batch queen2/pocket1/peaceful1; restores player0/Domestic. Natural queen ruler27 target25 reserved0/0/0, one queen march and one unique queen unit.
- Exact Second outcome signatures (sorted LastMarches + all node owners + LastQueenMarches) equal across normal/OFF/queen-selection-stage skip, SHA256 9de88b855d9b6c88432bced66b2b428a136935df7e6266f57e0a716af4145f82. This is deterministic fixture equivalence at completed natural second military, not full-world serialization or OS input. Normal retains one dedicated fallback ruler9 target27 stage2; do not call every order normal UI. Existing DEMO-0016 partial coverage remains; no duplicate defect ID. No product/test changes, HEAD af4715cb, C shared output retainedNov22, saves/user changes protected; no commit/push/publication. Next inspect that fallback and actual rendering, or expand common-state comparison.

### DEMO-0030 CPU peaceful confirmation clicks 306px left (complete 2026-11-21 16:10 JST)
- Confirmed 2026-11-21 16:08:09.461998 JST HEAD af4715cb with existing TUT-0007 differences,960x540 physical/1920x1080 logical. Existing SECOND_MILITARY1 natural second batch ruler9 target27: stage1 selected27; stage2 clicks local center707,938, movement remains open and falls back. Reproduced PID10440 16:07:18-33 exit0(limit300), peace-confirm-run.log; model resolution still succeeds, so not a state-loss claim. Local ConfirmMoveButton570,914,274,48; actual offset306 gives screen876,914,274,48 center1013,938. MovementClick subtracts306 for inspector input, so707 misses. Expect click actual confirm and close normal movement with no dedicated fallback.
- Evidence C work artifacts/demo-click-audit/DEMO-0030-before.png SHA256 74174ef3cc1afecd60c94006287b5073ff2eeb35e32bb3d044b8f4b01749be30, viewed540p. Distinct CPU callsite from completed DEMO-0029. Repair uses existing InspectorScreenBounds; diagnostic adds peaceful stage2->6/closed assertion. No UI specification/AI/order changes.

- Follow-up audit 2026-11-21 16:20:47.607494 JST (start16:20), static scope complete. HEAD af4715cb/instruction hashes/existing differences unchanged; DEMO-0030 verification sessions ended. Reviewed remaining CPU .Center callsites: assembly/page/drag adds NodeInspectorOffset explicitly, source-row click adds offset, check/close and picker use their screen UI rectangles; peaceful confirm now shares InspectorScreenBounds. No additional local-to-screen omission established. No product/test change or repeat of identical successful batch. Preserve DEMO-0030/TUT-0007/user notebook and pending seed12 diagnostic. Natural direct-delivery Demo, new-world full CPU state, all seeds and OS inputs remain unverified. Next concrete uncovered rendering/input condition; no commit/push/publication.

- Coverage planning audit 2026-11-21 16:42:57.239178 JST (start16:42), static scope complete: instructions/HEAD af4715cb/product differences unchanged. DemoUi supports seed, natural continuation and normal-input flags; no dedicated direct-delivery entry flag found. Game1.Demo*.cs has no DirectDelivery/directDelivery/_direct reference. This search alone is not a defect and does not prove the runtime cannot enter a related mode through normal handlers. Do not invent a switch/claim natural direct-map coverage from default DemoUi success. Existing DirectDeliveryUi fixed-input evidence remains distinct; next explicit normal UI entry/retreat-return observation needs a bounded diagnostic or observable OS window. No product/test edits, build, repeated run or newDEMO. Preserve DEMO-0030/TUT-0007/user notebook and new tutorial diagnostic; natural direct-delivery/OS/full-state scopes remain unverified. No commit/push/publication.

- Natural Demo seed28 audit 2026-11-21 17:02:29.664260 JST (start17:00), partial scope complete. HEAD af4715cb + DEMO-0030/TUT-0007, instructions/current differences reviewed; no product/test edit. dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path C shared folderverse-audit -p:UseAppHost=false PID19184 17:01:04-13 limit600s exit0 warnings0/errors0, demo28-current-build.log. dotnet same/bin/DemoUi/debug/DemoUi.dll, DEMO_TEST_SEED28/NATURAL_CONTINUE1/NORMAL_INPUT1, Content same/bin/FolderVerse/debug/Content, ALSOFT_DRIVERS=null; PID17372 17:01:27-48 limit300s exit0/noTIMEOUT/all collected, demo28-current-run.log.
- world948366069/cast763061582/place1153806411,1920x1080. Normal HandleInput continuation after helper setup passes newspaper, factory planning, domestic checks/close, turn0->2 and two subsequent transports. Natural war stages30->31->32->33->34->35->36->19: immediate order1, real red handle check before inspector close, faction turn-end toDelivery. Viewed red-check frame with hand at actual handle and order1; C work artifacts/demo-click-audit/demo28-redcheck-af4715cb.png SHA256 c7733721fe73a4647fcac843407ec1e248d51468f7d5f4bf01c2353b7240a8f9.
- Flags overviewFalse/directMapFalse/directPairFalse: dedicated direct-delivery remains unverified, not inferred from this pass. No OS inputs/full-state/all-seed guarantee. NAS outage C fallback retentionNov22; migrate evidence after reference/hash checks. No newDEMO, preserve user edits and pending tutorial diagnostic, no commit/push/publication.

- Unchanged follow-ups 2026-11-21 17:19 / 2026-11-21 17:39:26.564585 JST (latest start17:39), review complete: HEAD af4715cb/instructions/product differences stable, latest17:02 natural seed28 pass and16:10 DEMO-0030 completion retained within recorded scope. No additional reproduced issue or related new edit; no repeated build/run or product change. Natural direct-delivery, OS input, full-world/new-world CPU coverage remain unverified; continue from concrete uncovered route, not relabel prior success. User changes protected, no commit/push/publication.

- User-commit boundary/follow-up reviews 2026-11-21 18:00 / 2026-11-21 18:59:06.061423 JST (latest start18:58; earlier18:18/18:38 reviewed), review scope complete. HEAD079b3a77 contains previously verified DEMO-0030/TUT-0007 and diagnostic/record changes;17:50 tutorial audit inspected af4715cb..079b3a77 and found no extra specification change. Initial current dirty state only tutorial notes. Instruction hashes unchanged. No code/test change, newDEMO, build or repeated same-condition test. Prior successful evidence applies to af4715cb plus exact then-uncommitted changes, not an invented new current-HEAD execution. Dedicated direct-delivery/OS/full-state/new-world CPU scopes remain unverified. Preserve user commit and notes; no agent commit/push/publication.

- Static follow-up audit 2026-11-21 19:23:04.569010 JST (start19:21), scope complete: HEAD079b3a77/instruction hashes unchanged; only demo/tutorial notes dirty. Prior OS probe session1851 has ended and result collected at19:15 (tutorial record TIMEOUT); no duplicate shared-UI operation. Read current DemoDomestic InspectorScreenBounds callsites, DirectDelivery open/close and Stratagems22-25. CloseDirectDelivery restores prior ShowRoutes when direct map was open and clears pair/drag/truck state; this static check does not prove rendered retreat/return or left/right interaction. Demo has no explicit direct-delivery reference, as previously recorded; dedicated natural route remains unverified. StratagemInterface explicitly excludes Demo/autoPlay/tutorial and ComputerOnly, so no speculative missing manual-card lesson is registered. No new confirmed DEMO, product/test edit, build or repeated successful diagnostic. Existing primary/war-check/CPU evidence remains limited to recorded seeds; direct-delivery OS/full-state/new-world CPU/two-site and offshore natural UI scopes retained. Next explicit observable normal-UI direct-map entry and retreat/return; preserve user changes/saves, no commit/push/publication.

- Direct-pair transition static audit 2026-11-21 19:38:55.786538 JST (start19:38), scope complete: HEAD079b3a77/instructions unchanged, only audit notes dirty. Read Game1.DirectPairTransition.cs: pair switches preserve visible interpolated camera for Escape, CloseDirectPair begins return against untouched main camera, closing clears truck hits and finishes when slide reaches0; DirectDelivery input rejects closing/slide<1 and DirectPairMap adds hits only after full opening. No static mismatch confirmed. These checks do not prove actual animation/left-right hit geometry or natural Demo entry; retain explicit normal-entry/retreat-return as next runtime scope. No newDEMO, game/test edit, build or UI launch; prior isolated OS timeout already collected, user games/saves protected, no commit/push/publication.

- Unchanged follow-ups 2026-11-21 20:00 / 2026-11-21 21:19:02.036088 JST (latest start21:18), review complete: HEAD079b3a77/instructions unchanged; only audit notes dirty. No new product/specification difference or reproduced defect since19:38. No repeated same-condition test, UI launch or game edit. Direct-delivery normal-entry/retreat-return and recorded new-feature natural UI/full-state scopes remain unverified; next observable uncovered runtime condition. Prior validation processes ended/collected; user changes/saves protected, no commit/push/publication.

- Left/right geometry static audit 2026-11-21 20:39:44.397152 JST (start20:39), partial scope complete: HEAD079b3a77/instructions unchanged, notes-only differences. DirectHangar input uses side-specific DirectHangarViewport/DirectViewCell/DirectViewPart for drag/drop/material hit tests; DirectPairMap uses DirectPairMapBounds for pan/zoom/hit gating and records only fully contained truck rectangles. No mismatch confirmed by this search; actual rendered side/slot targets, transition return and natural Demo entry remain unverified. No runtime/build/game/test change or newDEMO. Next inspectable ordinary-input direct-pair observation, preserving user data and prior timeout record; no commit/push/publication.

- Follow-up reviews 2026-11-21 21:39 / 2026-11-22 00:22:38.172549 JST (latest start2026-11-22 00:22), complete within review scope: HEAD079b3a77/instructions unchanged. New tracked difference is Headless comparison reference repair, not Demo/product change; preserve it. Its graphical PID248 ended21:33:32 and result collected, no active overlap. No new reproduced DEMO, build/run or game edit. Dedicated direct-delivery normal-entry/retreat-return/left-right rendering and recorded full-state/new-feature natural UI scopes remain unverified; continue from inspectable uncovered runtime condition. No commit/push/publication.

- Current-specification regression 2026-11-22 00:42:24.260363 JST (start00:40), partial audit / RECHECK: HEADf0c4b6b3, instructions/records/current user differences reviewed. User merge includes guide/newspaper/primary and inspector changes; old079 proofs not current passes. Game unchanged. dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path C shared folderverse-audit -p:UseAppHost=false PID5244 00:41:11-17 limit600s exit0 warnings0/errors0/FontCoverage1088, demo-v014-inspector-build.log. Existing NAS-outage C fallback retainedNov22.
- dotnet same/bin/DemoInspectorUi/debug/DemoInspectorUi.dll, FOLDERVERSE_TEST_CONTENT=current gameContent/ALSOFT_DRIVERSnull, PID18876 00:41:32-48 limit300s exit3762504530/noTIMEOUT/session62854 collected, demo-v014-inspector-run.log. Office open/synchronized IDs/animated close/reset passed; frame5 stage12 cursor is inside TransportOverviewButton but expected overview did not open (assert Program82). Later movement/factory/540p not reached. No success inferred from build.
- Diagnostic directly invokes UpdateDemoDomestic but does not set _demoActive; current normal-world stratagem/manual primary gating differs from real Demo. Thus this failure is not yet a confirmed product click bug and gets no duplicate/newDEMO number. Next inspect actual primary enabled/manual state and reproduce with faithful Demo condition before repair; preserve user guide/primary specification. Dedicated natural direct-delivery and OS scopes remain unverified. No game/test edit, commit/push/publication.

- Diagnostic repair 2026-11-22 00:47:29.417816 JST (start00:44), HEADf0c4b6b3: completed00:42 failure reproduced from standard fixture; it calls Demo methods without _demoActive, so normal stratagem gating blocks transport. Optional faithful-Demo condition proves transport and all later assertions pass. tests/DemoInspectorUi/Program.cs now enables actual Demo state for its Demo path, excluding human factory-modal/pending variants; product unchanged. No newDEMO ID because this was diagnostic precondition, not product click displacement.
- Build same dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path C shared folderverse-audit -p:UseAppHost=false: optional PID24916 00:45:27-33, final PID15364 00:46:18-23, each600s/exit0/warnings0/errors0. Runs dotnet same/bin/DemoInspectorUi/debug/DemoInspectorUi.dll, current Content/null audio: optional FAITHFUL_DEMO1 PID2728 00:45:43-56 and final standard/no-extra-flag PID5692 00:46:35-48, each300s exit0/noTIMEOUT/all collected; logs demo-faithful-build/run.log and demo-fixture-final-build/run.log. Standard path passes office/close/reset, transport, country/movement/one peaceful reservation, count controls and six real factory-plan selections at1080p/540p with drawing. Two transport/factory frames viewed and preserved: C:\Users\muzud\.codex\visualizations\2026\10\30\01a18ce8-f326-7083-b8e7-ba69c77ea956\artifacts\demo-click-audit\demo-v014-fixture-5.png SHA256 e023c213822676f3d601da21c5d0e5b1ad6f9926e0100d427a0ee98dcf9ade14; C:\Users\muzud\.codex\visualizations\2026\10\30\01a18ce8-f326-7083-b8e7-ba69c77ea956\artifacts\demo-click-audit\demo-v014-fixture-23.png SHA256 6a4a60e235a7006306faa96accdbced1cb29bfa351061b0c54ecede2028f3a11.
- 00:42 RECHECK resolved as diagnostic setup; no gameplay repair. Shared UI/user guide specification preserved. Physical OS input/natural whole Demo/direct-delivery/CPU all-state/new-feature scopes remain unverified. C fallback retainNov22, diff --check target passes. No commit/push/publication.

- Follow-ups 2026-11-22 01:00 / 2026-11-22 02:21 JST (latest start02:20, end02:21), review complete: HEADf0c4b6b3/instructions/product code unchanged; preserve completed diagnostic fixture repair/user notes. Latest00:46 standard diagnostic passes recorded primary/inspector/factory scope; no identical rerun or game edit. Dedicated direct-delivery natural entry/retreat-return/left-right input, full CPU state/new-feature natural UI and physical OS remain unverified. No newDEMO; next concrete uncovered runtime condition, no commit/push/publication.

- 2026-11-22 02:21 / 02:39 JST static continuation (latest start02:38, end02:39), HEADf0c4b6b3: Game1.DemoChecks.cs stages30-36 still select target, select actual invasion row/picker card, call picker Done for immediate order, check PlannedOrders, click NodeCheckButton, close then stage19 turn-end. No deleted extra reservation-confirm click found. This is source-only; source-row stage32 opens picker directly after visible click animation and does not prove ordinary-input dispatch or physical OS replay. No new reproduced defect; next runtime compare this source-row dispatch and dedicated direct-delivery. No game edit.

- 02:39 static difference requiring runtime reproduction: current InvasionClick iterates InvasionVisibleRows but handles only InvasionQueenAt/InvasionCardAt, reserving immediately; DrawInvasion draws inline robot cards. Demo stage32 and ComputerMarches still call OpenInvasionPicker directly on a row-center animation. Thus shared row bounds alone do not establish the current normal selection route. Not registered as reproduced DEMO yet; next audit compare inline-card normal input with picker route, reservation count and closure under current HEAD.

- 2026-11-22 02:59 JST static continuation (start02:58/end02:59), HEADf0c4b6b3/instructions/differences unchanged: InvasionLayout row starts(84,y), width760; minimum row height76 for <=12 previews. Row center is(464,y+38), while first robot card is(94,y+34,57,37), and slot6 startsx460. Therefore row-center animation is not a first-card target and only coincides with a later card when enough previews exist. Direct OpenInvasionPicker bypass still prevents ordinary-input equivalence proof. Runtime fixture/actual rendered selection not executed this round; retain reproduction pending, no DEMO ID or product edit. Next compare same current source with one robot vs seven previews, then CPU OFF/ON/skip reservation equality.

## DEMO-0031 complete for reproduced Demo/CPU inline routes
- Audit start2026-11-22 03:19, end03:22 JST, HEADf0c4b6b3 unchanged; instructions and user differences reviewed. No game/diagnostic edits, no dotnet process before launch. Current normal InvasionClick selects inline InvasionCardAt/QueenAt and reserves immediately; Demo stage32 still calls OpenInvasionPicker directly and subsequently clicks its Done. Expected tutorial-like Demo to show the same available normal UI; actual natural Demo opens old modal after clicking row center, as rendered1920x1080 at stage33. This confirms replay presentation route differs, not a progression deadlock. CPU identical direct call source-confirmed but current CPU runtime not exercised.
- Repro: dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path existing C shared folderverse-audit -p:UseAppHost=false; PID9784 03:20:07-18 limit600s exit0/warnings0/errors0/FontCoverage1088. dotnet same/bin/DemoUi/debug/DemoUi.dll with FOLDERVERSE_DEMO_TEST_SEED=28/FOLDERVERSE_DEMO_NATURAL_CONTINUE=1/FOLDERVERSE_DEMO_NORMAL_INPUT=1, currentContent/null audio. PID28164 03:20:33-54 limit300s exit0/noTIMEOUT/session4769 collected. Logs demo-v014-natural-build.log/demo-v014-natural-run.log; initial game seeds948366069/763061582/1153806411. Existing NAS-outage C fallback, free194389626880B, retainNov22.
- Immediate reservation count1, red check before close, subsequent stage19 turn-end and newspaper-to-next-planning/delivery passed through turn2/two subsequent transports. base.Draw executed; stage33 PNG viewed, old modal with one robot shown and row-center cursor away from first card. Source-only suspicion02:39 now confirmed for visible modal route. Physical OS input, normal same-row click comparison, CPU OFF/ON/skip/all-state and dedicated direct-delivery remain pending. Natural flags overview/directMap/directPair allfalse; do not mark direct-delivery covered.
- Evidence C work artifacts/demo-click-audit/DEMO-0031-picker-f0c4b6b3.png SHA256 048509b5651b11ae1036bb559f166f4665d43cb649334e7091b56f86aa691197; next repair reproduce same normal inline selection vs Demo, reuse card bounds/input, preserve selected IDs/queen and avoid duplicate orders. No commit/push/publication.

- Repair03:27 JST: Demo now uses actual InvasionCardAt/QueenAt + InspectorScreenBounds and normal InvasionClick, no direct picker or obsolete Done. Natural Seed28 normal-input regression passes immediate single reservation, red check/close/turn-end/newspaper and two subsequent deliveries. Rendered1920x1080 stage33 viewed: inline card selected, cursor on same card, no modal. CPU ComputerMarches route remains unresolved; physical OS/all seeds/queen-only and CPU OFF/ON/skip remain pending. Detailed commands/results in demo-click-fixes.md.

- 2026-11-22 03:43 JST CPU continuation (start03:41/end03:43), HEADf0c4b6b3 + Demo inline repair unchanged: built existing artifacts/demo-click-audit/DemoAuditCpuSortie.csproj via --artifacts-path C shared folderverse-audit -p:UseAppHost=false, PID2864 03:42:30-42 limit600s exit0/warnings0/errors0/FontCoverage1088, demo31-cpu-build.log. Ran dotnet same/bin/DemoAuditCpuSortie/debug/DemoAuditCpuSortie.dll with FOLDERVERSE_AUDIT_SECOND_MILITARY1/currentContent/null audio; PID29244 03:42:56-03:43:11 limit300s exit3762504530/noTIMEOUT/session98368 collected, demo31-cpu-run.log. Stops at first settings-menu assertion line690, before CPU military replay: fixture begins after newspaper but current guide can consume normal initial input. Not a reproduced settings defect or CPU result; DEMO-0031 CPU remains runtime-unconfirmed. Next adapt diagnostic setup to complete current guide through its normal next/Escape input before testing settings, then resume CPU replay comparison. No game or test code edited this audit; preserve user work, no commit/push/publication.

- 2026-11-22 03:48 JST diagnostic repair complete (start03:46), HEADf0c4b6b3 + Demo inline repair preserved. Changed existing ignored artifacts/demo-click-audit/ProbeCpuSortie.cs.txt only: first normal HandleInput opens current guide, Escape/release finishes it before settings input. No guide suppression/game change. Current03:43 settings assertion was diagnostic precondition: normal guide completion restores all later settings/input checks. Build same DemoAuditCpuSortie project/C shared artifacts path, PID33208 03:47:26-31 limit600s exit0/warnings0/errors0; run same DLL/currentContent/null audio/SECOND_MILITARY1 PID24916 03:47:38-57 limit300s exit0/noTIMEOUT/session30114 collected; demo31-guide-fixture-build.log/demo31-guide-fixture-run.log.
- Current CPU replay confirms DEMO-0031 remaining route: first attack stage3 picker0; second batch queen order29/ruler27 target25 stage2->3 picker2 with click640,732, then picker Done1415,822. Natural batch36/fallback0/human player0 Domestic restored, no duplicate pending reservations assertion passes. This success is progress/state coverage, not normal-inline UI equivalence. CPU old-picker presentation remains unresolved and now runtime reproduced; next minimal CPU replay uses inline card/queen bounds and same original ID batch, validate OFF/ON/skip parity and real rendering. Physical OS/all-state/full-new-world remains unverified. No commit/push/publication; C fallback retentionNov22.

- 2026-11-22 04:01 JST evidence review (start04:00/end04:01), HEADf0c4b6b3/differences unchanged: viewed existing03:47 successful CPU run audit-latest-computer-confirm.png at960x540. Old modal is visibly used for CPU ruler selection; cursor lies on selected large picker card, whereas ordinary UI uses inline cards. Preserved C work artifacts/demo-click-audit/DEMO-0031-cpu-picker-f0c4b6b3.png SHA256 9e4a7df21496e0efaa3b3825fa9d6e4eae45878a40382dd5ae022efcb43a7330. No new launch/game edit; confirms existing DEMO-0031 CPU presentation residual, no duplicate ID. Next repair same IDs/queen through inline UI with original batch ownership and OFF/ON/skip equality. Full runtime OS/direct-delivery scopes remain unverified.

- CPU repair completed 2026-11-22T04:10:03.440496+09:00 JST (start04:05): Game1.ComputerMarches.cs now queues original robot IDs mapped to normal InvasionCardAt and InvasionQueenAt, uses InspectorScreenBounds and InvasionClick, no old modal/Done. Invasion.cs queen toggle suppresses ReserveInvasion during CPU preview just like robot cards; original batch owns resolution. Natural36/fallback0, normal/OFF/queen-selection skip outcomes exactly match pre-fix signature9de88b855d9b6c88432bced66b2b428a136935df7e6266f57e0a716af4145f82 (LastMarches/node owners/LastQueenMarches). 540p/1080p inline drawing viewed. Reproduced routes complete; whole-world/full-state/all seeds/OS, legacy19-card optional diagnostic fixtures and dedicated direct-delivery remain unverified. Commands/PIDs in fixes record. Evidence C work artifacts/demo-click-audit/DEMO-0031-cpu-inline-f0c4b6b3.png SHA256 ac80bd9abe9b9626d9614ed39db3bdd42bcb793a98c9d4436ebab707e9b533c0.

- Follow-ups 2026-11-22 04:21 / 2026-11-22T09:59:43.484997+09:00 JST (latest start09:59), change review complete only: HEADf0c4b6b3/instructions and DEMO-0031 product differences unchanged, latest CPU result04:08 and shared tutorial regression04:11 collected. No duplicate run or game change; no new reproduced issue. Prepared direct-delivery retreat-return/left-right diagnostic now passed04:48 with recorded setup constraints; natural Demo dedicated entry remains uncovered; 19/37 inline IDs passed05:29, larger capacities/source-page changes remain unverified. Full OS input and all-state/all-seed claims remain pending. Preserve user changes; no commit/push/publication.

- 2026-11-22 04:42 JST direct-delivery continuation (start04:40), HEADf0c4b6b3 + DEMO-0031 repairs unchanged. Built dotnet build tests/DirectDeliveryUi/DirectDeliveryUi.csproj --artifacts-path existing C shared folderverse-audit -p:UseAppHost=false, PID2948 04:40:40-50 limit600s exit0/warnings0/errors0/FontCoverage1088, direct-v014-build.log. Ran dotnet same/bin/DirectDeliveryUi/debug/DirectDeliveryUi.dll currentContent/null audio/FOLDERVERSE_DIRECT_PAN=1/FOLDERVERSE_DIRECT_PROBE=1, PID10900 04:41:00-11 limit300s exit3762504530/noTIMEOUT/session4425 collected, direct-v014-run.log.
- Fixture stops in LoadContent line33 at End turn follows current manual/card layout before Draw/pan/truck operations. It asserts normal PopulationTurnButton width192/height96-or144 and later Demo144; current v0.14 stratagem/footer changes were not reflected in these fixed size expectations. Do not report a direct-delivery product bug or runtime pass; geometry needs compare with current spec/render first. Game/test unchanged this audit. Next repair diagnostic stale primary-layout expectations only after confirming current layout, then continue pan/left-right/restore. Dedicated natural Demo/direct-delivery and physical OS remain unverified. C fallback retainNov22; preserve user edits, no commit/push/publication.

- Diagnostic repair complete 2026-11-22T04:49:38.908459+09:00 JST (start04:45), HEADf0c4b6b3/product differences unchanged. tests/DirectDeliveryUi/Program.cs now expects actual shared192x96 turn button (normal and Demo), establishes isolated manual-tray transition complete state, and finishes current guide via normal HandleInput Escape/release before direct-mode fixture. No product/UI changes or newDEMO ID. Source confirms PrimaryActionHomeBounds current96 height/tray-hidden actions; old144 expectation and uncompleted guide were diagnostic preconditions.
- Same dotnet build tests/DirectDeliveryUi/DirectDeliveryUi.csproj --artifacts-path C shared folderverse-audit -p:UseAppHost=false: PID13392 04:46:29-34;33512 04:47:29-34;25916 04:48:32-37, each600s exit0/warnings0/errors0/FontCoverage1088. dotnet same/bin/DirectDeliveryUi/debug/DirectDeliveryUi.dll currentContent/null audio/DIRECT_PAN1/DIRECT_PROBE1 with FOLDERVERSE_ prefix: PID34556 04:46:43-54 fails still-hidden primary baseline; PID5688 04:47:41-59 passes footer checks then fails offscreen truck (guide consumed pan); final PID27460 04:48:46-56 exit0. Each300s/noTIMEOUT/all collected (44321/98158/19474). Logs direct-{layout,manual,guide}-{build,run}.log.
- Final PASS: proportions/frame-out-in/hidden-moving suppression/close/Escape, normal-pan-visible truck and pair entry, both hangars/reservations, rotated seam letters, main/pair/return cameras, consecutive globe/net clicks at540p and preserved reservations. Main map and pair960x540 renders viewed; C work artifacts/demo-click-audit/direct-v014-pair-540.png SHA256 bf0a8abe7b56124e45a4882e29eae752b3a141319ad3566fc2ff67a87d17e178. This is prepared fixture, not natural Demo dedicated direct-delivery or physical OS route; guide/manual transition setup is not a full normal menu-entry test. Initial04:42 failure resolved as diagnostic stale expectations; no product issue established. C existing NAS fallback retainNov22, targeted diff --check exit0, preserve user changes, no commit/push/publication.

- 2026-11-22T05:21:17.339199+09:00 JST static review (start05:20/end05:21), HEADf0c4b6b3/product differences unchanged. Existing ignored ProbeCpuSortie CARD19/CARD37 branches explicitly require picker pages/draft (lines591-628); these expectations predate DEMO-0031 inline repair, so launching unchanged would test obsolete UI rather than current source pagination. Not run, not a game pass/failure. Next adapt only diagnostic to actual inline cards/IDs, test19/37 and source page changes/skip/reservation parity; natural Demo dedicated direct-delivery/physical OS remain unverified. No game/diagnostic edit this audit, no commit/push/publication.

- Large-card diagnostic repair completed 2026-11-22T05:30:04.111159+09:00 JST (start05:26), HEADf0c4b6b3/product unchanged. Existing ignored artifacts/demo-click-audit/ProbeCpuSortie.cs.txt CARD19/CARD37 now tests current inline selection instead of obsolete modal pages: each click one robot, exact original IDs/no extra reservation, next ruler queen toggle and skip restores human. Fixture chooses ground ruler according to actual DawnPhase (not fixed1-before3).
- Build same DemoAuditCpuSortie.csproj --artifacts-path C shared folderverse-audit -p:UseAppHost=false: PID10312 05:27:02-16, probe24564 05:28:04-09, dawn33644 05:29:00-05; each600s exit0/warnings0/errors0/FontCoverage1088. Runs same DLL/currentContent/null audio/FOLDERVERSE_AUDIT_CARD19=1: PID24400 05:27:24-35 and25876 05:28:18-30 exit3762504530, because actual first ruler3 queen preceded fixed expected ground1; stage logs confirm setup order mismatch. Final19 PID34772 05:29:13-19 exit0; CARD37=1 final37 PID11412 05:29:28-33 exit0. Each300s/noTIMEOUT/all collected; logs cpu-inline-large-{build,probe-build,dawn-build}.log and cpu-inline-{19-run,19-probe-run,19-dawn-run,37-dawn-run}.log.
- Both19/37 inline passes selected ID set exactly, zero duplicate planned orders before resolution, queen toggle no duplicate, next controller and skip restore player0. Viewed960x54037-card selected panel/cursor, C work artifacts/demo-click-audit/cpu-inline37-540.png SHA256 59d1002bd1c294ad0dbd6c787dc24d9685bed6845c16348dd042584cc729a344. No product issue found/newDEMO, diagnostic-only completion. This does not prove actual OS clicks, all counts/page changes at larger capacities or full-state equivalence; natural dedicated direct-delivery remains unverified. Existing C NAS fallback retainNov22/no new output tree, preserve user edits, no commit/push/publication.

- 2026-11-22T08:20:53.607245+09:00 JST source-page static trace (start08:20), HEADf0c4b6b3/product unchanged: ComputerMarches119-150 assigns InvasionPageForSource(source) before deriving ComputerWarSelectionPoint from the same InvasionVisibleRows row index and shared card/queen rectangle, then routes local input through InvasionClick. This is source consistency evidence only, not a rendered multi-source-page pass. No game/test change/new reproduced DEMO or build/UI launch; next actual multi-source pagination or natural dedicated direct-delivery. Physical OS/all-state scopes unchanged; preserve user changes, no commit/push/publication.

- 2026-11-22T10:20:48.171527+09:00 JST merge-boundary review (start10:20), complete for source/change review: HEADf0c4b6b3 ->41115d930a3974ce723ec54c85ab46ce6db8f669. User merge incorporates previously recorded DEMO-0031 Demo/CPU inline repairs and three diagnostic updates; no additional game changes beyond the already-verified differences. Current dirty state only tutorial review record; preserve it. Previous03:26/04:08/04:48/05:29 evidence retains exact tested-source scope, not a new current-HEAD runtime or physical OS pass. No new reproduced DEMO, game/test edit/build/UI launch. Next multi-source-page actual rendering or natural dedicated direct-delivery entry/restore; full OS/all-state unverified. No commit/push/publication.

- Follow-up reviews 2026-11-22T10:41:44.296185+09:00 / 2026-11-22T18:38:50.574321+09:00 JST (latest start18:38), source/change review complete only: HEAD41115d93/instructions/product unchanged since10:20 merge-boundary review. New tutorial optional EMPTY_SOURCE diagnostic passed10:31, ended/collected; no shared-UI overlap. No Demo game/test change, new reproduced defect, build or identical rerun. Natural dedicated direct-delivery/source-page rendering and physical OS remain unverified; next uncovered route, preserve all user/test edits/saves, no commit/push/publication.

- Merge-boundary review 2026-11-22T18:58:53.835025+09:00 JST (start18:58), HEAD41115d93 ->392429e15e773e00a97fb584471406f4dffed47a. Source review complete only. New recovery planner/model/save/robot reservations and newspaper/conquest presentation integrated; user notebook and tutorial review edits preserved. DirectDelivery change moves capacity text after cards; MapOverlays renames salvage label without geometry change; ResetInspector closes new recovery overlay. No new reproduced DEMO or game change. Prior runtime passes are scoped to old HEAD and cannot establish current merged rendering/input. No build/UI launch; current primary/direct pair/left-right inspector and natural Demo/CPU continuation regression pending. Next isolated current-build runtime, including recovery-reserved robot exclusion and human restoration; natural dedicated direct-delivery/multi-source pages/physical OS remain unverified. No commit/push/publication.

- Follow-up reviews 2026-11-22T19:18:57.057385+09:00 / 2026-11-22T19:42:22.425655+09:00 JST (latest start19:42), HEAD392429e1 unchanged; source consistency review complete only. Recovery reservation exclusion traced through RobotState.Stationed and RobotReservations.CanShip, while Demo/CPU inline selection continues to derive selectable IDs and previews from normal InvasionRobots/InvasionPreviewRobots. No separate Demo stock eligibility was added in this merge. This does not prove mixed recovery/march runtime ordering or cursor/rendered geometry. No new reproduced DEMO, game/test edit, build/UI launch. Current-HEAD primary/direct pair/inspector and natural Demo/CPU regression remain pending; next isolated current-build run, preserve notebook/audit edits and saves, no commit/push/publication.

- Merge-boundary review 2026-11-22T20:01:28.753090+09:00 JST (start20:01), HEAD392429e1 ->b3a5cb5cb5c2da14ef39d870adeea7dd16504a60. Source/change review complete only. Demo.cs/DemoDomestic.cs unchanged; Game1.Draw now renders conquest curtain after Demo overlay/autosave notice and before fullscreen cursor on overview early-return path; battle research timing/recipient presentation also changed. User committed earlier review records; current tutorial note preserved. No new reproduced DEMO, game/test edit/build/UI launch. Current-HEAD overlay visibility, battle-to-industry and human restoration need rendered/input regression; previous HEAD results are not this pass. Pending primary/direct/inspector/recovery interaction and physical OS scopes retained. No commit/push/publication.

- Follow-up reviews 2026-11-22T20:18:52.769863+09:00 / 2026-11-23T01:02:28.539370+09:00 JST (latest start2026-11-23 01:02), HEADb3a5cb5c/instructions/product unchanged since20:01 review. Review complete for change detection only; no new reproduced DEMO, game/test edit/build/UI launch. Current merged runtime, cursor geometry, recovery interaction, battle-to-industry and human restoration remain pending; physical OS and natural dedicated direct-delivery not passed. Next isolated current-build regression; preserve existing review notes, no commit/push/publication.

- Current-build DemoInspector diagnostic complete 2026-11-23T01:23:52.369876+09:00 JST (start01:21), HEADb3a5cb5c unchanged before/after, only review docs dirty. Reused NAS tmp/delete-after-2026-11-23/tutorial-current; no shared-UI overlap. dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path <output> -p:UseAppHost=false: PID32452 01:21:55-01:22:33 limit600s exit0 warnings0/errors0 FontCoverage1096. dotnet <output>/bin/DemoInspectorUi/debug/DemoInspectorUi.dll with FOLDERVERSE_TEST_CONTENT=<output>/bin/FolderVerse/debug/Content and ALSOFT_DRIVERS=null: PID4304 01:22:42-01:23:14 limit300s exit0/noTIMEOUT, session47185 collected. PASS office synchronized IDs, close animation, transport primary, country/movement and exactly one reservation, player-count confirm, six factory plans/current rectangles1080p/540p and real rendering. Viewed960x540 demo-office-34.png. Verified archive copies under archives/diagnostics/demo-current-b3a5cb5c-2026-11-23. Prepared fixture, not physical OS/natural dedicated direct-delivery, CPU full replay, mixed recovery or all routes; these stay pending. No newDEMO/code/test changes, no commit/push/publication. Next direct-delivery or CPU recovery-interaction runtime.

- TIMEOUT 2026-11-23T01:50:54.685296+09:00 JST, start01:40 review / build01:40:34.679503 end01:50:34.863509, HEADb3a5cb5c unchanged; review docs only dirty. Command dotnet build tests/DirectDeliveryUi/DirectDeliveryUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-23/tutorial-current -p:UseAppHost=false. Limit600s elapsed600.18s, PID36036; restore and FontCoverage1096 passed, product DLL emitted, no DirectDeliveryUi completion/exit before deadline. Wrapper taskkill /PID36036 /T /F successfully ended own build and children30716/21756/17676/38612; target waited/exit1, session29502 collected. Exact cause undetermined, do not attribute to Norton without evidence. No UI diagnostic launched, no product issue/newDEMO established. Verified log archived at archives/diagnostics/demo-current-b3a5cb5c-2026-11-23/direct-current-build.log; keep output throughNov23. Current direct-delivery retreat/return/pair rendering remains unverified; prior earlier-HEAD pass is not current validation. Interrupted; next inspect build stall before one bounded isolated retry, avoid duplicate launch/user game changes. No code/test edit/commit/push/publication.

- TIMEOUT follow-up 2026-11-23T02:03:11.564166+09:00 JST (start01:58), HEADb3a5cb5c unchanged. One diagnostic retry: same DirectDeliveryUi build/output, added -p:UseSharedCompilation=false -m:1 -v:normal, limit120s, start01:59:02.091351 PID38424 (wrapper27556/session77803). Detailed log direct-current-retry-build.log: product project completed; DirectDeliveryUi CoreCompile skipped as up-to-date, then no completion. At deadline taskkill reported PID38424 terminated, but wrapper has not returned and tasklist still shows38424; additional taskkill /PID38424 /F returned1 with no running task instance. Thus final process cleanup is NOT confirmed; do not relaunch against same output. Exact cause undetermined, including possible pending OS/file IO; Norton not established. No UI run/newDEMO/product edits. Interrupted: next collect session77803/verify PID creation and output/log progress before any rebuild. Original600s timeout and this120s retry are distinct; second end/exit pending. Preserve saves/user work; no commit/push/publication.

- Residual-build follow-ups 2026-11-23T02:21:40.081248+09:00 / 2026-11-23T03:42:36.844095+09:00 JST (latest start03:42), HEADb3a5cb5c/instructions unchanged. PID38424 still listed; session77803 yielded no completion. Same DirectDeliveryUi output remains blocked from duplicate build/launch, not all independent source review. No new game/test difference or reproduced DEMO; no edits/build/UI launch. Current direct-delivery and mixed-recovery rendered paths remain pending. Interrupted; resume by collecting existing wrapper and confirming process identity/exit and log update first. No commit/push/publication.

- User-requested layout review 2026-11-23T03:55:36.617281+09:00 JST, HEAD4fde752e (previous b3a5cb5c). Reviewed phase-plaque specification and current shared code; user notebook edit preserved. Fixed plaque=(680,36,560,112); title/ending excluded, dedicated domestic overlays draw own labels; popup hints queued after plaque. Compact war/peace inspector850x520, ConfirmMoveButton Y592; invasion48-card pages, DemoChecks/ComputerMarches select InvasionPageForRobot and subtract FirstSlot before shared card hit rectangle. Tutorial glow references shared confirm bounds. Existing NAS PhasePlaques images for direct delivery and battle-board were visually inspected, not regenerated in this review; these are prior-run evidence only. Broader changes include illustrated primary actions, HUD rings replacing spotlights, recovery workflow and broadcast layout; do not restore old coordinates/specification. No product/test edit, new reproduced ID or test execution. Current-head natural Demo/CPU, physical START-to-final and overlay/input regressions remain pending; next isolated current-build run with new layout. No commit/push/publication.

- Follow-up reviews 2026-11-23T03:59:37.830072+09:00 / 2026-11-23T11:41:39.885374+09:00 JST (latest start11:41), HEAD4fde752e/instructions unchanged after layout review; user notebook edits preserved. Prior-build PID38424 still listed; do not restart same DirectDeliveryUi output or mix its old-HEAD result with current layout. No new reproduced DEMO or code/test edit/build/UI launch. Current-head natural Demo/CPU and direct-delivery/inspector rendered regressions remain pending. Review complete only; next collect old session77803/confirm exit, then isolated current-head layout regression; no commit/push/publication.

- Merge-boundary review 2026-11-23T12:01:40.372340+09:00 JST (start12:01), HEAD8eb4b2e6. Since4fde752e only documentation/analysis changed; product/tests unchanged. PID38424 still listed; same DirectDeliveryUi output not restarted. No new reproduced DEMO, product/test edit, build or UI launch. Source/change review complete only; current-layout natural Demo/CPU, direct-delivery and inspector rendering/input remain pending. Next collect old session77803/confirm exit or independent isolated layout regression; preserve user work/saves, no commit/push/publication.

- Follow-up reviews 2026-11-23T12:20:12.613246+09:00 / 2026-11-23T15:01:20.159723+09:00 JST (latest start15:00), HEAD8eb4b2e6/instructions/product unchanged. PID38424 still listed; no duplicate same-output build. Difference review complete only; no new reproduced DEMO, product/test edit, build or UI launch. Current-layout natural Demo/CPU/direct-delivery/inspector rendering and input remain unverified; next collect old result or independent isolated regression. Preserve user work/saves; no commit/push/publication.

- Domestic source-path review 2026-11-23T15:40:30.701866+09:00 JST (start15:39), HEAD8eb4b2e6; only audit documents dirty. Game1.DemoDomestic.cs waits for map/inspector animations, uses InspectorScreenBounds for assembly/factory/movement/confirm and shared primary/close/check rectangles. Stages9/10/11/12/16/17/18/20 route to normal input handlers; stages0/3/8/19 still invoke open/end methods directly, so marker alignment alone does not prove all normal-click transitions. This is source evidence, not a newly reproduced defect or runtime pass. No build/UI launch, no resolution tested this round. Current-layout direct-delivery retreat/return, both inspector sides and natural CPU/Demo runtime remain unverified. Review complete only; next isolated current-head rendered regression. No game/test edit or commit/push/publication.

### DEMO-0032: movement diagnostic checks post-click rectangle (completed diagnostic repair)

- Audit completed 2026-11-23T16:04:14.902572+09:00 JST/start16:00, HEAD8eb4b2e6 unchanged; only audit docs dirty. Build dotnet build tests/DemoInspectorUi/DemoInspectorUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-23/tutorial-current/entry-layout -p:UseAppHost=false -p:UseSharedCompilation=false -m:1 -v:minimal: PID30840 16:00:55-16:01:42 limit600/46.58s exit0/warn0/error0/FontCoverage1103. Run dotnet same/bin/DemoInspectorUi/debug/DemoInspectorUi.dll with FOLDERVERSE_TEST_CONTENT=same/bin/FolderVerse/debug/Content,ALSOFT_DRIVERS=null: PID13044 16:01:59-16:02:35 limit300/35.08s exit3762504530/noTIMEOUT. Sessions14568/32680 collected.
- Reproduces Program.cs line90/frame8: Movement click misses visible button. Previous assertion confirms movement opened/country closed. Diagnostic computes InspectorScreenBounds AFTER UpdateDemoDomestic; CompactWarInspector switches offset306 to-30 when _movementOpen becomes true. Expected comparison is PRE-click rectangle. Demo stage18 already computes its shared target before MovementClick. Stale assertion timing is confirmed; game click defect is NOT established. No game/test edits.
- First1920x1080 phase fails before later540p regression; primary transport/animated close assertions passed. Later reservations/factory, natural Demo/CPU/direct delivery remain unverified. Logs preserved at NAS archives/diagnostics/inspector-layout-8eb4b2e6-2026-11-23. Next snapshot pre-click rectangle in diagnostic and inspect analogous confirm check; rerun/render both sizes. No commit/push/publication; temporary build retainNov23.

- DEMO-0032 completed 2026-11-23T16:09:36.137271+09:00 JST: pre-click snapshots fixed diagnostic timing;1080p/540p rendered regression exit0. Commands/results in fixes record. Natural/physical routes remain unverified.

- Primary-action continuation 2026-11-23T16:21:40.363533+09:00 JST/start16:20, HEAD8eb4b2e6 unchanged; DEMO-0032/TUT-0008 diagnostic edits preserved. Source review: Game1.MapOverlays.cs shares PrimaryActionBounds for drawing/hits, uses AvailablePrimaryActionCount for enabled tray range and suppresses home/current bounds for inactive actions during slide; MapModeCloseButton derives from home rectangle. DirectDeliveryUi prepared fixture checks frame-out/in and two-inspector transitions but iterates PrimaryActionCount. No current runtime claim from source alone. Old DirectDeliveryUi session77803 polled16:21 still pending/no output, so same test not duplicated. No game/test edit/build/UI launch this round/new DEMO. Review complete only; current direct-delivery natural entry/retreat/return and both inspector sides remain unverified. Next collect old result or resolve conflicting diagnostic before current isolated direct run; existing current DemoInspector1080p/540p success retained within its scope. No commit/push/publication.

- Current-layout DemoUi completed 2026-11-23T16:44:09.393113+09:00 JST/start16:41, HEAD8eb4b2e6 unchanged; existing diagnostic edits preserved, no game/test edit this audit. dotnet build tests/DemoUi/DemoUi.csproj --artifacts-path existing NAS tutorial-current/entry-layout -p:UseAppHost=false -p:UseSharedCompilation=false -m:1 -v:minimal: PID17668 16:41:53.342-16:42:40.789 limit600/47.44s exit0/warn0/error0/FontCoverage1103. Run same/bin/DemoUi/debug/DemoUi.dll, current Content/null audio/FOLDERVERSE_DEMO_TEST_SEED=12: PID29836 16:43:01.958-16:43:28.155 limit300/26.19s exit0/noTIMEOUT; sessions92650/98585 collected. PASS idle loop, globe/net, edge relocation18FPS, refusal fallback, ending loop/label rendering. Viewed1920x1080 demo.png. Test substitutes fixed battle at wave22; directMap/directPair false at sampled transport, so dedicated delivery/whole natural combat/CPU/physical input remain unverified. Evidence/logs hash-verified NAS archives/diagnostics/demo-layout-8eb4b2e6-2026-11-23. No new DEMO; next dedicated direct route after old result collection or shared war checks. Output reused/retainNov23, no commit/push/publication.

- DEMO-0031 diagnostic follow-up 2026-11-23T17:10:31.575752+09:00 JST/start17:01, HEAD8eb4b2e6; game unchanged. DemoChecksUi still expects obsolete modal/draft in Program.cs lines30-32 although current Demo uses inline reservation; existing DEMO-0031 game repair is not reverted. Additional diagnostic adaptation unresolved; no duplicate issue ID. Built tests/DemoChecksUi same NAS entry-layout/UseAppHost=false/UseSharedCompilation=false/-m:1/-v:minimal: PID25360 17:02:21-17:03:07 limit600/45.29s exit0/warn0/error0/FontCoverage1103. Run same/bin/DemoChecksUi/debug/DemoChecksUi.dll/null audio: PID21104 17:09:08-40 limit300/31.65s exit3762504530/noTIMEOUT, sessions87364/30853 collected. Green handle click/animated close/war primary/target projection assertions pass first1080p/net scenario; modal expectation fails next. Red check/expiry/540p/globe not reached, not normal. Logs/image hash-verified NAS archives/diagnostics/checks-layout-8eb4b2e6-2026-11-23. Next adapt only diagnostic for inline immediate reservation and current highlight policy, then render/rerun. No game/test edits this audit/no commit/push/publication; output retainNov23.

- DEMO-0031 diagnostic handoff review 2026-11-23T17:19:26.897313+09:00 JST/start17:18, HEAD8eb4b2e6/instructions unchanged; no game/test changes or rerun. Current DemoChecks stage32 invokes InvasionClick on shared inline card bounds,33/34 skip directly35,35 clicks red handle,36 closes to19 after reservation. This confirms the recorded modal/draft test expectation is obsolete; correction remains assigned to repair round. Red/green display separation, red-target clickability, expiry and current rendering beyond first scenario remain unverified until adapted test runs. Review complete only/no duplicate ID or speculative game fix. Next update diagnostic and verify net/globe1080p/540p; no commit/push/publication.

- DEMO-0031 additional DemoChecksUi adaptation completed 2026-11-23T17:23:15.730083+09:00 JST: obsolete modal assertions replaced with inline immediate reservation/no-duplicate checks. All4 net/globe1080p/540p scenarios pass/rendered; green/red separation/expiry and actual march confirmed. See fixes record. No game change; red-marked target re-click/physical/full CPU/dedicated delivery remain unverified.

- CPU/direct continuation 2026-11-23T17:44:31.988857+09:00 JST/start17:43, HEAD8eb4b2e6/instructions unchanged; diagnostic diffs preserved. ComputerMarches source still switches to acting ruler before shared WarButton/map/MovementClick/inline InvasionClick, pages via InvasionPageForRobot, then shared red handle/animated close. Source consistency does not prove on/off/skip outcomes at current HEAD; prior CPU results remain old-source evidence. Old DirectDeliveryUi session77803 polled17:44 still pending/no output; no duplicate direct run. No new game/test edits/build/UI/new DEMO. Source/result review complete only; next isolated CPU outcome parity/current rendering or collect old direct result. Physical input/red-target re-click/direct dedicated route remain unverified; no commit/push/publication.

- Red-check input source review 2026-11-23T18:03:31.883339+09:00 JST/start18:02, HEAD8eb4b2e6/instructions unchanged; diagnostic edits preserved. NodeCheckClick toggles only actual visible handle; RefreshWarTargetHighlights updates ActionTargetNodes, while map MovementClick hit-tests retained MovementTargets and then SelectMoveNode. Thus red highlighting and click eligibility are separate in code; no reproduced click-blocking defect from this review. Prior4-condition rendered checks retain scope, but actual red-marked map re-click is still unverified. No run/build/game/test edits/newDEMO. Review complete only; next isolated red-target re-click or CPU parity runtime; natural direct route/physical inputs remain pending. No commit/push/publication.

- Follow-up reviews 2026-11-23T18:19:45.401917+09:00 / 2026-11-23T21:20:35.788546+09:00 JST/latest start21:19, HEAD8eb4b2e6/instructions/product unchanged. Existing diagnostic edits/results preserved. Old DirectDeliveryUi PID38424 still has same creation1795366742086 and exact build command at21:20; same test not duplicated. Shared InspectorScreenBounds/NodeCheckButton references remain in DemoDomestic. No new reproduced issue or build/UI run/game/test edit. Source/result review complete only; current CPU on-off-skip/dedicated direct route remain unverified; red re-click handler fixture passed19:02. New-system runtime eligibility and outcomes remain unverified. Next isolated current CPU diagnostic or collect old direct result; no commit/push/publication.

- Red-marked target runtime completed 2026-11-23T19:03:02.563794+09:00 JST/start19:00, HEAD8eb4b2e6/product unchanged. Related diagnostic only tests/DemoChecksUi/Program.cs: after red mark, clear selected target in fixture then click its actual projected point through MovementClick; assert target reselected and exactly1 order remains, then close/end/march. Same NAS entry-layout DemoChecksUi build PID16388 limit600 exit0/warn0/error0/FontCoverage1103; run PID3872 limit300 exit0/noTIMEOUT, sessions31010/12117 collected. All4 net/globe1080p/540p scenarios pass including new re-click assertion. Logs timestamps/elapsed in checks-reclick-build/run.log, hash-verified NAS archives/diagnostics/checks-layout-8eb4b2e6-2026-11-23. diff --check0. No game changes/newDEMO. Handler/fixture input only, physical OS/full CPU/natural direct route remain unverified. No duplicate old direct run; output reused/retainNov23/no commit/push/publication.
