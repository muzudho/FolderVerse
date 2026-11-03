# Demo クリック位置の修正記録

## 初期状態（2026-10-31）

定期修正は未実施。点検記録で確認された問題を対象にする。

## 修正履歴

各回に対象ID、修正前の再現、変更ファイルと内容、実行した検証と結果、残る制約、完了時刻（JST）を追記する。検証できない項目を完了扱いにしない。

## 2026-10-31 初回修正回

- 開始：10:57 JST。完了：2026-10-31 11:04:04 JST。
- 対象：DEMO-0001〜DEMO-0005。修正前のHEADはa9f642b159cf09922e6b50635174346fd13b08e7。開始時の差分は先行点検のdemo-click-audit.mdのみで、点検回の完了記録を確認した。利用者のゲームプロセスは確認されず、停止操作は行っていない。
- 修正前に既存出力のDemoClickProbeを再実行し、輸送とofficeの同時有効、閉じる位置の不一致、わたしの国を経由しない移動、人数画面からRollingへの飛び越しを再現した。DemoUiの引数不一致例外も再現した。

### 修正内容と完了判定

- DEMO-0001：Game1.DemoDomestic.csから非表示のNodeTransportTileによる操作を取り除いた。地点の格納庫・工場を見た後、×でインスペクターを閉じ、実際のTransportOverviewButtonとMapOverlayClickから輸送長期計画を開く。旧officeとtransportの同時有効を起こさず、実描画で輸送計画画面を確認した。段階12で開き、16で閉じる。
- DEMO-0002：段階11と16はInspectorClose.Centerを参照してDomesticViewClickを呼ぶ。通常と同じスライドで閉じ、開閉アニメーション中は次のDemo操作を待機する。クリック矩形内、閉じ始め、終了時のResetInspectorを検証した。
- DEMO-0003：輸送計画を閉じた後、段階17でStrategyToggleとMapOverlayClickからわたしの国を開き、段階18で表示中のMovementButtonからMovementClickへ進む。国の画面→移動パネルの実描画と、各状態・クリック位置を確認した。移動先選択後に不要だった旧段階14の固定座標クリックも飛ばすよう整理した。
- DEMO-0004：tests/DemoUi/Program.csのTickに現在時刻を加えて６引数とし、実際のUpdate順序と同様にインスペクターのアニメーションも更新する。起動時の期待値を人数確認→コンピューター人数確認→Rollingへ合わせた。人数・内政操作が増えた分の巡回時間を確保した。和平移動では戦闘が必ず発生するとは限らないため、戦闘導入の検証は固定BattleStateを使い、ランダムな対戦発生への依存を取り除いた。既存の地図切替・継ぎ目移動・説得拒否後の分岐・20分後の終了操作・再ループも最後まで成功した。
- DEMO-0005：Game1.Demo.csの両人数画面はPlayerCountConfirmの中心へ向かい、クリック時もその中心に揃える。PlayerCountInputとComputerPlayerCountInputという通常の確認処理を通し、コンピューター人数の確認を省略しない。18FPS相当のステップで双方のクリックが確認矩形内にあり、人数と画面遷移が成立することを検証した。人間／コンピューター両画面の描画も確認した。
- 回帰検証をtests/DemoInspectorUi/Program.csに追加した。ゲームUI本体の配置・仕様は変更していない。

### 実施した検証

共通環境：1920×1080、ALSOFT_DRIVERS=null。出力先は既存artifacts/corner-lessonを再利用した。

1. dotnet build tests/DemoUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet：成功、警告・エラー０。
2. dotnet artifacts/corner-lesson/DemoUi.dll：成功。引数修正後に判明した旧180秒巡回・500フレームの時間不足とランダム戦闘前提も更新した上で、最終実行は全チェック成功。
3. 同じ出力先へのtests/DemoInspectorUiビルド：成功、警告・エラー０。
4. dotnet artifacts/corner-lesson/DemoInspectorUi.dll：成功。地点監督、通常の×閉鎖、輸送計画プライマリーボタン、わたしの国を経由する移動、人数確認とクリック矩形を検証し、実描画を保存した。
5. git diff --check：成功。

ローカル画像はartifacts/corner-lesson/demo-office-0.png〜demo-office-10.png。特に5が輸送計画、7がわたしの国、8が移動、9が人間の人数確認、10がコンピューターの人数確認。画像を開いて画面内容も確認した。PNG等はGit管理外であり、再生成できるソースは上記testsに残した。

### 残る確認範囲

５件を完了としたが、Demoの全経路・全解像度の位置ズレがなくなったという判定ではない。段階0の地点選択、格納庫の組立と工場の各クリック、段階13の移動先選択、直送便専用操作、540pなど別解像度、回転後の地図のクリックは次回点検対象として残す。地図切替や終了操作は状態遷移テスト成功であり、各クリックの全画像を目視したわけではない。コミット・プッシュ・公開は行っていない。

## 2026-11-01 01:30枠の修正回（トリガー01:27）

- 開始：2026-11-01 01:27:27 JST。完了：2026-11-01 01:29:41 JST。対象：DEMO-0006／DEMO-0007。
- 先行01:00点検の01:03完了記録を確認した。HEADは `43b1b14aada2ca849ba9cca32a8a6a56925b4ade`。開始時、ゲーム・テストソースに差分なし。既存のREADME・点検記録の変更と、利用者のチャット文書を保持した。全体差分は前回同様にコマンド単位でLFSフィルターを無効にして読み取り、No.52のPNG候補８件は変更しなかった。
- アプリ一覧では同じ対象を作業中の別チャットなし（このチャットのみactive）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildの実行中プロセスは検出されなかった。ゲームの停止、コミット・プッシュ・公開は行っていない。

### 修正前の再現

点検時に現在のHEADからビルド済みの `DemoAudit20261101.dll` を再実行した。工場を「製造しない」にした状態で、段階９のクリックが(340,332)となり、表示ボタン(160,288,72,88)から外れる一方、直代入で一覧が開くことを再現した。続く段階10で候補０の矩形内(184,482)をクリックした演出の後も、SelectedPlanが３のままであることを再現した。ログは `artifacts/demo-click-audit/before-fix-20261101-output.txt`。1080p／540pの段階９と1080pの段階10を再現した。

### 変更ファイルと内容

- `FolderVerse/Game1.NodeFactory.cs`：現在のレーンの製造期間から実際の製造品ボタンを得る `FactoryProductionButton(line)` を追加。通常の描画・入力も同じ矩形を参照する。製造しない場合は期間１、表示上の上限は既存のFactoryRobotButtonを使うため従来どおり。UIの配置・仕様は変更しない。
- `FolderVerse/Game1.DemoDomestic.cs`：段階９は期間３固定を廃止して上記矩形へカーソルを合わせる。段階９・10は工場の内部表示フラグを直接代入せず、`NodeDialogClick`を通して通常操作と同じプラン表示・選択を行う。
- `tests/DemoInspectorUi/Program.cs`：プラン３（製造しない）・０・１・２・４・10からの一覧表示と候補０への変更を、1080p／540pで各６通り検証する。クリック点の矩形包含、一覧の開閉、SelectedPlan=0、Manufacturing=trueまで確認し、各画面を描画する。

### 検証結果と証拠

既存 `artifacts/corner-lesson` の出力先を再利用。DOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null。

1. `dotnet build tests/DemoInspectorUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers`：成功、警告・エラー０。現在のゲーム本体と拡張した回帰テストをビルド。
2. `dotnet artifacts/corner-lesson/DemoInspectorUi.dll`：成功。上記12組の一覧表示・候補選択と、既存の監督・×・輸送計画・わたしの国→移動・人数確認のテストを完走。ログは `artifacts/demo-click-audit/fixed-inspector-20261101-output.txt`。製造しない場合は(196,332)、期間５は(484,332)、期間４は(412,332)、期間２は(268,332)と表示に追従し、すべて選択後はプラン０・製造ありになった。
3. `dotnet artifacts/corner-lesson/DemoUi.dll`：現在のゲーム本体に対して既存テスト出力を再実行し成功。自然に進めた開始・地図切替・18FPS面接続・拒否後分岐・終了ループの既存チェック。ログは `artifacts/demo-click-audit/fixed-demo-20261101-output.txt`。
4. `dotnet artifacts/corner-lesson/FactoryPlansUi.dll`：既存テスト出力を現在のゲーム本体で再実行し成功。工場プラン・休暇・空き枠・他国／配下権限・描画。
5. `dotnet artifacts/corner-lesson/NodeInspectorUi.dll`：既存テスト出力を現在のゲーム本体で再実行し成功。回数メニュー優先・地図入力・地点切替・スライド開閉・人物位置・ロボットとパネル描画。
6. `git diff --check -- FolderVerse tests`：成功。

描画証拠は `artifacts/corner-lesson/demo-office-11.png`～`demo-office-34.png`（同じ検証出力フォルダー内）。11は1080pの製造しない状態からの一覧、12は選択後、23は540pの一覧、24は選択後。11と24を開き、カーソル／クリック演出と実画面、製造再開を目視確認した。数値チェックは全12組のクリック位置と実際のプラン変更を検証している。診断用PNGはGit管理外。テストソースを保存して再生成できる。

### 完了判定と制約

DEMO-0006／0007を完了へ更新した。未解決の別件を推測で追加したり、先行の未確認項目を正常扱いしたりしていない。工場の対象段階はfixtureから実際のDemo段階を実行し、自然再生の全世界・全時間にわたる工場操作を検証したわけではない。段階０の地点選択、格納庫の組立クリック、段階13の移動先、直送便専用巡回の有無と全体の540p自然再生は、点検記録の続き位置どおり次の点検回へ引き継ぐ。

## 2026-11-01 02:30枠の修正回（トリガー02:26）

- 開始：02:27:00 JST。完了：2026-11-01 02:31:56 JST。対象：DEMO-0008／0009。
- 先行02:00点検の02:00:29完了を確認した。HEADは `43b1b14aada2ca849ba9cca32a8a6a56925b4ade`。先行修正0006／0007のGame1.DemoDomestic.cs・Game1.NodeFactory.cs・tests/DemoInspectorUi/Program.csと、README・両記録の差分を保持して作業した。全体差分のNo.52画像候補と利用者のチャット文書には触れていない。
- アプリ一覧では同じ対象を作業中の別チャットなし（当該チャットのみactive）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、ゲームや別プロセスの停止操作は行っていない。コミット・プッシュ・公開なし。

### 修正前の再現

`ALSOFT_DRIVERS=null` で `dotnet artifacts/corner-lesson/DemoAuditNext.dll` を現在の修正前ゲーム本体に対して再実行。地点26の投影(1026,503)、90度時(1008,618)に対し、固定クリック(480,260)のHitNode=-1から別地点が開く現象を1080p／540pで再現した。３パーツの検証用fixtureで、一度の胴移動演出からCompleteになる一方、同じ通常ドラッグはHead＋BodyとLegsの２体を残す相違も再現。ログは `artifacts/demo-click-audit/before-fix-0230-output.txt`。

### 変更内容

- `FolderVerse/Game1.DemoDomesticActions.cs`（追加）：所有する候補地点の現在の投影と、通常のHitGlobeNode／CubeNet.HitNodeを照合してDemoのカーソル位置を決める。表示レイヤーを地点選択に合わせ、選択できる地点が視野外なら先に地図を合わせて次の更新を待つ。見えていない地点を固定座標から開かない。
- `FolderVerse/Game1.DemoDomestic.cs`：段階０は上記の実際の位置と当たり判定で得た地点を開く。地図の遷移・面接続・回転アニメーション中は次の操作を待つ。
- 同２ファイルの格納庫処理：一括Assembleを廃止し、移す部位の描画矩形で通常のNodeAssemblyInputに押下と解放を渡す。移動中は実際のドラッグを保持する。３パーツや未結合の束は必要な回数だけ部位移動を繰り返し、一体型は実際に接続された部位ごと移す。拒否されて部位が変わらなかった場合は同じ操作を無限に繰り返さず次へ進む。実入力の前フレーム状態は復元する。
- `tests/DemoActionsUi/`（追加）：見える地点の当たり判定・カーソル・開いた地点の一致、視野外の地星儀、地図回転、非表示レイヤー、押下・移動中・解放の状態と部位ごとの変化を1080p／540pで検証する。３体・２体の未結合の束・一体型を用意し、前２つは２回、一体型は１回のドラッグで完成することを確認する。
- `tests/DemoInspectorUi/Program.cs`：視野外の地点を合わせる更新が増えることを反映し、初期の１回呼び出し固定を、実際に地点が開くまでの上限４回の更新に変更した。先行の工場プラン検証はそのまま保った。
- ゲームのUI配置・組立ルールは変更していない。工場の先行修正も保持した。

### 検証コマンドと結果

共通環境はDOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null。出力は既存 `D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/` を再利用。

1. `dotnet build tests/DemoActionsUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers`：成功、警告・エラー０。
2. 最終ソースで `dotnet build tests/DemoInspectorUi --no-restore -p:OutputPath=（同上） -v quiet --disable-build-servers`：成功、警告・エラー０。ゲーム本体も更新。
3. 最終ゲーム本体に対して `dotnet artifacts/corner-lesson/DemoActionsUi.dll`：成功。地点レイヤー無効、展開図０／90度、64倍かつ視野外へ移した地星儀を1080p／540pで検証。クリック点のHitNodeと開いた地点が一致し、地図位置に追従した。組立は押下・移動中に部位を変えず、解放ごとに変わる。３体・２体未結合・一体型の６ケースを完走。ログは `artifacts/demo-click-audit/fixed-actions-0230-output.txt`。
4. `dotnet artifacts/corner-lesson/DemoInspectorUi.dll`：最終実行成功。既存の監督・×・プライマリー・移動・人数確認と、先行修正の工場プラン12組を完走。ログは同 `fixed-inspector-0230-output.txt`。初回は旧fixtureの「１回で必ず開く」前提により失敗したが、視野外では地図を合わせてから選択する現在の動作を待つよう修正し、成功を確認した。
5. `dotnet artifacts/corner-lesson/DemoUi.dll`：最終ゲーム本体に対して既存テスト出力を再実行し成功。自然に進めた開始・地図切替・18FPSの面接続・拒否後分岐・終了ループの既存チェック。ログは同 `fixed-demo-0230-output.txt`。
6. `git diff --check -- FolderVerse tests`：成功。

実描画は `artifacts/corner-lesson/demo-actions-0.png`～35.png。０～５が各地図条件、６～10が1080pの頭・胴・脚３体の組立、21～25が同540p。８（最初のドロップ後に頭＋胴と別の脚）と10（２回目のドロップ後に完全体）を開いて目視し、一度の操作で脚まで増えないことを確認した。工場と既存経路の描画は同フォルダーのdemo-office画像を再利用。利用者のセーブは読み書きしないテストホストで実行した。

### 完了判定と残る制約

DEMO-0008／0009を完了へ更新した。自然再生の全世界・全期間での画面確認を済ませたとはしない。移動先・確定の各クリック、直送便専用Demo巡回の扱い、戦闘・輸送・説得のクリック画像など、点検記録の未確認範囲は次回へ引き継ぐ。今回の２件以外の過去の開発依頼は再開していない。

## 2026-11-01 03:30修正回

- 開始：03:30:36 JST。完了：2026-11-01 03:34:21 JST。対象：DEMO-0010。
- 03:00点検の03:06:03完了記録を確認した。HEADは `43b1b14aada2ca849ba9cca32a8a6a56925b4ade`。先行0006～0009のゲーム・テスト差分と新規ファイル、README・点検／修正記録を保持した。アプリ一覧で同対象の別実行なし（当該チャットのみactive）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、プロセス停止なし。利用者の素材とチャット文書には触れていない。コミット・プッシュ・公開なし。

### 修正前の再現

`ALSOFT_DRIVERS=null` で `dotnet artifacts/corner-lesson/DemoAuditMovement.dll` を現在の修正前本体に対して再実行。候補６件なのにDemoのクリックが地点より25px上でHitNode=-1、SelectedMoveNode=-1となり、次の確定演出から予約０件でTransportへ進むことを再現した。展開図０度／90度、地星儀の1080p／540pで確認。ログは `artifacts/demo-click-audit/before-fix-0330-output.txt`。

### 修正内容

- `FolderVerse/Game1.DemoDomestic.cs`：DemoDestinationPointを古いDestinationBadge中心から、現在の地点投影位置＋移動候補限定の通常HitNodeに変更した。地図内で実際に選択できる地点だけを指す。候補が視野外なら、先に候補へカメラを合わせて待機する。
- 移動先選択が失敗したときは確定演出へ進まず、選択し直す。候補がない場合はインスペクターを閉じ、表示中のターン終了ボタンを指す。以前のCampaign.Advance直呼びによる隠れた進行を廃止した。
- 確定操作は通常のMovementClickを使い、実際に移動予約ができて画面を閉じた後、別の段階19でプライマリーのターン終了ボタンを指してEndPlannedTurnを実行する。予約とターン終了を一つのクリック演出で済ませない。UIの位置・移動のゲームルールは変更していない。
- `tests/DemoActionsUi/Program.cs`とREADME：1080p／540p、展開図０度／90度、画面外へ移した地星儀の６ケースを追加。候補選択のクリック点と限定HitNodeの一致、確定前は予約０件、確定後は選択先へのPeaceful／Queen／PocketOnlyの予約１件、内政のまま予約を保持すること、別のターン終了クリックを確認する。選択なしの段階15からの再選択と、候補なし時に偽の移動クリックを出さず終了するケースも追加した。

### 検証コマンドと結果

共通設定はDOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null。既存 `artifacts/corner-lesson` 出力を再利用した。

1. `dotnet build tests/DemoActionsUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers`：成功、警告・エラー０。現在のゲーム本体と追加ケースをビルド。
2. `dotnet artifacts/corner-lesson/DemoActionsUi.dll`：成功。今回の移動６条件すべてで対象89への実際の和平移動予約を確認した。選択失敗の再選択、候補なしの終了、先行の地点選択・視野外・組立６ケースも完走。ログは `artifacts/demo-click-audit/fixed-actions-0330-output.txt`。
3. `dotnet artifacts/corner-lesson/DemoInspectorUi.dll`：現在のゲーム本体に対して既存テスト出力を再実行し成功。監督・閉じる・輸送計画・わたしの国→移動・人数確認と工場プラン12組。ログは同 `fixed-inspector-0330-output.txt`。
4. `dotnet artifacts/corner-lesson/DemoUi.dll`：現在のゲーム本体に対して既存テスト出力を再実行し成功。自然に進めた開始・地図切替・18FPS面接続・拒否後分岐・終了ループ。ログは同 `fixed-demo-0330-output.txt`。
5. `git diff --check -- FolderVerse tests`：成功。

画像は既存 `artifacts/corner-lesson/demo-actions-36.png`～61.png。37は1080p展開図での実地点の選択、38は予約確定後、39は独立したターン終了後。57・58・59は540p地星儀の同段階、60・61は候補なしの場合。37と58を開いて目視した。37では選択先の強調と選択済み表示があり、58は内政フェーズのカットイン越しに「移動予約１件／ターン終了で実行」が表示されている。数値チェックはカットインの有無に依存せず予約の対象と全フラグを確認している。利用者のセーブは読み書きしていない。

### 完了判定と制約

DEMO-0010を完了へ更新した。以前のDestinationBadgeを描画・通常入力へ追加してDemoにUIを合わせる変更はしていない。自然再生全体のあらゆる世界・解像度の検証を済ませたわけではない。直送便専用Demo巡回、世界構築・征服者選択、戦闘・輸送・説得のクリック画像は次の点検回へ引き継ぐ。過去の別の開発依頼は再開していない。

## 2026-11-01 04:30枠の修正回（トリガー04:34）

- 開始：04:35:04 JST。完了：2026-11-01 04:40:06 JST。対象：DEMO-0011。
- 先行04:00点検の04:04:23完了記録と現在の差分を確認した。HEADは `43b1b14aada2ca849ba9cca32a8a6a56925b4ade`。先行0006～0010のゲーム・テスト差分と新規ファイル、README・両記録を保持した。Game1.Demo.cs／Game1.csは開始時に差分なし。アプリ一覧で同対象の別実行なし（当該チャットのみactive）。Get-ProcessでFolderVerse・dotnet・Demo・MSBuildは検出されず、プロセス停止なし。利用者の素材・チャット文書へ変更なし。コミット・プッシュ・公開なし。

### 修正前の再現

`ALSOFT_DRIVERS=null`、`dotnet artifacts/corner-lesson/DemoAuditSetup.dll` を現在の修正前本体に対して再実行。18FPS・1080p／540pで、区画権利停止／確定と選択後確認が(1578～1579,789)で実ボタン外となり、人物選択では別タイルの位置を指してスロット０が選ばれることを再現した。ログは `artifacts/demo-click-audit/before-fix-0430-output.txt`。

### 変更内容

- `FolderVerse/Game1.Demo.cs`：区画権利の停止・確定はCastAction、選択後の確認はStatusProceed、人物選択は現在のCharacterSelectionLayoutのタイル矩形を参照する。クリックを出す瞬間も対象へ揃え、補間途中の位置を確定座標にしない。既に人間へ割り当てたスロットを除外し、次の人間は別の実タイルを選ぶ。
- `FolderVerse/Game1.cs`：通常の人物選択で使っていたHitSelection・選択可能判定・SelectPlayer・PlayerReadyへの遷移をTrySelectPlayerへ抽出し、通常入力とDemoで共有した。選択画面の仕様・レイアウトは変更していない。Demoの人物選択もクリックが当たったスロットを実際に選ぶ。
- `tests/DemoSetupUi/`（追加）：18FPSの自然な３秒待機を使い、1080p／540p・人間１人／２人で、対象矩形内のクリックと実際の選択人物、重複しない人間割当、確認から次の担当または土地征服への遷移を確認する。通常の物理マウス入力でスロット３を選び、確認する比較も追加。

### 検証結果

共通設定はDOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null、既存出力 `D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/` を再利用。

1. `dotnet build tests/DemoSetupUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers`、最終は同引数の `--no-restore` ビルド：成功、最終は警告・エラー０。初回のテストヘルパー名Tickによる警告はStepDemoへ変更して解消した。
2. `dotnet artifacts/corner-lesson/DemoSetupUi.dll`：最終実行成功。区画権利と確認は全条件で(758,1045)へ一致。人物は実タイルへ一致し、スロット０→確認、人間２人の場合は続けてスロット１→確認となり、重複なくLandConquestへ進んだ。通常マウスでのスロット３選択と確認のチェックも成功。ログは `artifacts/demo-click-audit/fixed-setup-0430-output.txt`。
3. `dotnet artifacts/corner-lesson/DemoUi.dll`：修正後のゲーム本体に対する既存テスト出力の実行成功。自然に進めた開始・地図切替・18FPS面接続・拒否後分岐・終了ループ。ログは同 `fixed-demo-0430-output.txt`。
4. `dotnet artifacts/corner-lesson/DemoInspectorUi.dll`：同じ本体に対する既存テスト出力の実行成功。監督・閉じる・プライマリー・移動・人数と工場12組。ログは同 `fixed-inspector-0430-output.txt`。
5. `git diff --check -- FolderVerse tests`：成功。

描画証拠は既存 `artifacts/corner-lesson/demo-setup-0.png`～27.png。３は1080pの人物選択直後、26は540pの２人目選択直後。両方を開き、カーソルが実際に「あなた」となったタイルを指すことを目視確認した。Seedによってタイル配置が変わっても、テストではその時点の実タイル・HitSelectionを照合する。

通常マウス比較のfixtureを作る際、土地を既に割当済みのSetPlacementから土地征服へ入り、比較後の解放入力で自動土地選択が例外になった。通常の世界構築に合わせてSetTerritoryRightsで未征服の土地を用意するよう診断fixtureだけを訂正し、最終実行が成功した。これを新しいゲーム不具合として登録していない。利用者のセーブの読み書きなし。

### 完了判定と未確認範囲

DEMO-0011を完了へ更新した。今回の範囲は対象フェーズからの18FPS再生と１・２人のケースであり、タイトルからの全時間・全人数の画像確認を完了したわけではない。世界・人物抽選の停止／再抽選の残り、戦闘・輸送・説得、直送便専用Demo巡回などは次の点検回へ引き継ぐ。過去の別の開発依頼は再開していない。

## 2026-11-03 10:30枠の修正回（トリガー10:25）

- 開始：2026-11-03 10:25:08 JST。完了：2026-11-03 10:39:25 JST。対象：DEMO-0012。先行点検の10:07:08完了記録、AGENTS、README、両記録と差分を先に確認。開始HEAD=428a6f24392084537a5e74fd6d41cf173714deec、終了時HEAD=dc50148546f48b100a96bdb7e15bac15efbf009a。途中で外部のHEAD更新を検出したが、その変更は戻していない。本エージェントによるコミット・プッシュ・公開なし。アプリ一覧は当該チャットのみactive、他はnotLoaded。開始時ゲーム・Demo・dotnet・MSBuildプロセスなし。利用者のセーブは読み書きしていない。

### 再現と原因

現本体に対する既存DemoUiの最初の再実行は成功した。非決定的な世界抽選を切り分けるため、テストの乱数を固定してSeed・画面・国内段階・スライド・実クリック位置・候補の投影／ヒットを記録した。乱数Seed０（世界2099272109／人物2137491580／配置1754296375）では、地図説明はstage=5へ進む一方、内政stage=13で移動先を選べず停止した。記録はfix-latest-seed0.log／fix-latest-diagnostic.log（artifacts/demo-click-audit内）。起動後に同じゲーム内段階を再現しており、Nortonの起動待ちとは異なる。

移動先へ向きを合わせても、NetPanel／地星儀の投影中心と、インスペクターを除いたMapViewBoundsの中心が異なるため、狙う地点が実際の表示範囲外に残った。さらにDemo全体の自動回転がクリックの1.8秒待機中も動き、ぎりぎり見えている地点を再び隠す場合があった。待ち時間を3,600更新まで増やしても同じ段階で停止したため、その変更は取り消し、最終検証は元の1,200更新を維持した。

### 変更ファイルと修正

- FolderVerse/Game1.Demo.cs：内政のクリック操作中は自動のyaw回転を止める。地図の説明中と他フェーズの回転は継続。
- FolderVerse/Game1.DemoDomesticActions.cs：FocusDemoNodeを共通化。地星儀は向きを合わせた後、投影した地点とMapViewBoundsの中心との差をGlobePanへ補正する。展開図も実際の表示範囲の中央へ移す。
- FolderVerse/Game1.DemoDomestic.cs：行先が見つからない場合の視点合わせも同じFocusDemoNodeを使う。地点を開く操作と行先を選ぶ操作で、別の固定位置を使わない。
- tests/DemoUi/Program.cs：FOLDERVERSE_DEMO_TEST_SEED（既定０）で再現性を確保し、Seed・段階・候補の投影／ヒットを出力。地図説明後に内政操作へ復帰したことと、実際に次の伝播へ進むことを分けて検査する。待機上限は元の1,200更新。通常のUI仕様、当たり判定、予約を直接成功にする処理は変更しない。

### 検証

共通：DOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null、既存出力artifacts/corner-lessonを再使用。

1. dotnet build tests/DemoUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers。続く最終ビルドは--no-restore：成功、警告・エラー０。ログfix-latest-build.log。
2. FOLDERVERSE_DEMO_TEST_SEED=0／1／2でdotnet artifacts/corner-lesson/DemoUi.dll：３実行とも成功。ログfix-latest-seed0-after.log、fix-latest-seed1-after.log、fix-latest-seed2-after.log。各Seedで地図説明後の内政１巡、全体ターン１→２と次のTransport到達、戦闘・拒否・終了ループが成功。Seed１の世界／人物／配置は217468053／1321446349／2039633683、Seed２は483147644／505401118／177487344。
3. dotnet artifacts/corner-lesson/DemoActionsUi.dll：現本体に対する既存テスト成功。1080p／540pの展開図・回転・地星儀、視野外からの復帰、部位／包み／結合部位の実ドラッグ、６条件の和平移動予約と独立したターン終了。ログfix-latest-actions.log。
4. dotnet artifacts/corner-lesson/DemoInspectorUi.dll：現本体に対する既存テスト成功。工場６プランの現在矩形・実選択、監督・閉じる・長期輸送プライマリー・わたしの国→移動。ログfix-latest-inspector.log。
5. demo-actions-38.pngを開き、地図上の「移動予約済」と予約１件／ターン終了で実行、実際のプライマリー描画を目視した。DemoUiの描画は最後のタイトルまでであり、固定３Seedの全フレーム画像を目視したとは扱わない。

DEMO-0012を完了へ更新。全Seed／人数、直送便専用Demo、利用者ゲーム併存は未確認。次の点検は固定Seedログを起点に、必要なら他Seedや解像度を追加する。以前のリリース準備時の伝播未到達と同一原因だったかは確定していない。利用者の新しい依頼に従い、開発日誌とNorton起動待ちの引き継ぎを追記して通常開発を終了する。

## 2026-11-03夜：利用者依頼による操作完了チェックのDemo実演

- 22:30定期修正回の終了後、競合がないことを確認して追加依頼を実装。既存DEMO-0001～0012の状態を変更せず、誤差の修正とは別の実演追加として扱う。
- Game1.DemoDomestic.cs：工場等の確認後に段階20の緑チェックを挿入し、閉じる前にハンドルを実際にクリックする。開戦と和平移動を巡回ごとに交互に見せる。
- Game1.DemoChecks.cs：新規。段階30～36で通常の入力処理・UI矩形を使い、開戦予約、実地点選択、出撃選択、選択確定、赤チェック、閉じるを順に実行する。削除済みの最終予約ボタンは使わない。見えない地点は視点を合わせ、候補なしは移動へ戻り、既存チェックを外さない。クリックまでの1.8秒の移動・待機を維持する。
- Game1.NodeChecks.cs：Demoが自分の世界にチェックできるようにし、通常の自動プレイでは手動チェック不可を維持する。ターン終了で両種類を消す。セーブ・実績のDemo除外には触れていない。
- tests/DemoChecksUi：実クリック・実予約・実進軍を４条件で検査。初回実行は成功（テストのTick名に警告があったためStepDemoへ改名）。画像はtests/DemoChecksUi/bin/DemoChecks/demo-checks-*-*.png。地図・地星儀それぞれ1920×1080／960×540で、緑・赤のハンドルと状態更新を確認した。
- DOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null。DemoUi、DemoActionsUi、DemoInspectorUiをキャッシュから復元し、既存artifacts/corner-lessonへそれぞれ現ソースをビルド（警告・エラー０）、DLLを実行して成功。自然巡回はFOLDERVERSE_DEMO_TEST_SEED=12、世界／人物／配置=992459907／934883396／883376777。実地点・ドラッグ・和平移動６条件、工場６プラン×２解像度も成功した。
- 全Seed・全人数、専用直送便Demo、すべての途中フレームの目視は未実施。v0.2.0の配布物を差し替えていない。完了：2026-11-03T22:40:28+09:00。コミット・プッシュ・公開なし。
