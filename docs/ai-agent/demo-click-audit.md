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
