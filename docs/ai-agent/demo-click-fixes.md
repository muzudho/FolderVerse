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

## 2026-11-04 01:30修正回：DEMO-0013

- 開始：2026-11-04 01:32:26 JST。完了：2026-11-04 01:36:41 JST。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。AGENTS、README、両記録、差分と先行点検の終了を最初に確認。他チャットidle、ゲーム／dotnet／MSBuildなし。既存点検記録の差分を保護。コミット・プッシュ・公開、実セーブ・勲章操作なし。
- 現本体のDemoChecksUiを再実行し、開戦プライマリー遷移のassertを再現（fix-latest-checks-before.log）。原因は新しい内政→配送→軍事のフェーズ分離。OpenWarPlanningは軍事だけ許可されるが、Demo／fixtureは内政のまま開戦を試みていた。通常のUI制限を緩めず、Demoを正しいフェーズへ接続した。
- FolderVerse/Game1.DemoDomestic.cs：内政の工場確認→緑チェック→閉じる後は内政終了へ進む。配送後の軍事開始は開戦または移動の巡回から開始し、軍事でない状態では開戦段階を実行しない。既存の実ボタン矩形／入力、赤チェック→閉じる→ターン終了を維持。
- tests/DemoChecksUi/Program.cs：緑チェックと閉鎖後に実Demoの内政終了・配送入り・緑メモ消去を検査。配送後の軍事へ入ってから実開戦クリックを検査する。緑メモがフェーズ終了をまたいで残るという古い期待を訂正し、赤の強調解除・即予約・進軍・消去は維持。
- tests/DemoUi/Program.cs：配送直後に戦闘fixtureへ入る旧前提を訂正。配送から軍事へ移行し、Demoが軍事操作を自然巡回してターンを進め、出兵Transportへ到達することを追加検査してから既存の戦闘fixtureを使う。
- 検証：DOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null。dotnet build tests/DemoChecksUi および tests/DemoUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers：警告・エラー０。最終ビルドログfix-latest-checks-build.log／fix-latest-demo-build.log。両DLLの実行はコード０。DemoChecksUiは地星儀／展開図×1080p／540pの４条件で緑・内政終了・軍事・即予約・赤・閉鎖・独立ターン終了・実進軍を確認（fix-latest-checks.log）。DemoUiはSeed12で配送／軍事巡回と戦闘・拒否・終了ループ成功（fix-latest-demo.log）。途中の古い緑メモ期待と配送後の戦闘fixture失敗は上記テスト修正で解消。
- demo-checks-0-6.pngを開き「軍事フェーズ」、赤ハンドルを指すDemoの手、地図の赤チェックと予約１件を目視。git diff --check成功。既存の分離出力・ログ／画像名を再利用。DEMO-0013を完了へ更新。
- 未確認：全Seed／人数、専用直送便Demo、今回変更後の全周辺UI再実行、チェック済み地点の独立した実再クリック。次回はフェーズ切替の回帰とこれらを優先。Norton保留の直接証拠・設定変更なし。

## 2026-11-04 04:30修正回：DEMO-0014

- 開始 04:34:29 JST、完了 2026-11-04 04:35:32 JST。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。先行点検完了、別チャットidle。既存のDEMO-0013差分を保持。
- 現DLLで「Transport click misses primary action」を再現（fix-latest-inspector-before.log）。内政の閉鎖後は段階19へ進むため、tests/DemoInspectorUi/Program.csでその遷移を明示的に検査し、長期輸送の矩形と入力は独立して段階12から検査するよう修正した。
- DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build tests/DemoInspectorUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers、ALSOFT_DRIVERS=nullで同出力DemoInspectorUi.dllを実行。コード０。長期輸送の実クリック・開閉、わたしの国から移動、人数確認、工場6プランの1080p／540p検査が成功。fix-latest-inspector-build.log／fix-latest-inspector.log。demo-office-5.pngで内政の長期輸送画面と閉じるハンドルの描画を目視。
- この検証は長期輸送の個別操作を対象とする。自然巡回が長期輸送へ分岐することや、赤チェック地点の物理再クリックは今回未検証。

## 2026-11-04 12:26修正回：DEMO-0015

- 開始 2026-11-04 12:26:05 JST、完了 2026-11-04 12:29:58 JST。HEAD=015d7ce960d7262ca70920f6d803fd38849d5584。先行点検完了と別チャットidleを確認。既存差分を保持。
- Seed20（970412988/848972489/1018591594）で初回配送前の停止を再現。地点へ向き直した直後は正しくヒットしたが、TickSlotsの抽選画面用の自動回転がWorldStatusでも地星儀の向きを更新し、クリック前に再照準が繰り返された（fix-latest-seed20-before.log）。
- FolderVerse/Game1.csのTickSlotsでWorldStatusを抽選画面用の自動回転の条件から除外。抽選時とDemoの地図紹介時の回転は既存経路を維持。
- dotnet build tests/DemoUi --no-restore -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。分離出力のDemoUi.dllをSeed20とSeed12で実行し、両方で初回Transport入り、WorldStatus復帰、終了ループを確認。DemoChecksUi.dllも終了コード0。1080p／540pの地星儀・展開図で実ハンドルクリック、即予約、赤チェック、閉鎖、ターン終了と表示消去を検査。demo-checks-0-6.pngで赤ハンドルを指す手と地図の赤チェック、予約1件を目視。
- 証拠は artifacts/demo-click-audit の fix-latest-seed20-build.log、fix-latest-seed20-after.log、fix-latest-seed12-after.log、fix-latest-checks.log。既存の分離出力を再利用。全Seed、通常プレイの物理マウス入力は未確認。

## 2026-11-05 00:26修正回：DEMO-0016

- 開始 00:26:52 JST、終了 00:30:45 JST。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。既存の点検差分を保持。修正前の CountrySettings.dll --ui で専用パネル経路を再実行。
- Game1.ComputerMarches.cs、Game1.Invasion.cs、Game1.InvasionPicker.cs、Game1.DemoChecks.cs、tests/CountrySettings/CountrySettingsUi.cs を変更。対応できる格納庫出撃で通常の開戦・地図ヒット・出撃元・出撃選択・確定・閉じるを使用。出撃元ボタンの矩形を描画・入力・Demoで共有。AIが計算した指示を従来どおり一括実行し、確認操作で予約を二重登録しない。
- dotnet build tests/CountrySettings および tests/DemoChecksUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers 成功。ALSOFT_DRIVERS=null で dotnet artifacts/corner-lesson/CountrySettings.dll --ui および dotnet artifacts/corner-lesson/DemoChecksUi.dll はコード0。通常ピッカーの開閉、確定で予約数が増えないこと、スキップ後の人間視点と進行を検査。country-settings-12.png（540p）で通常ピッカーと出撃3体を目視。ログ fix-latest-CountrySettings.log／fix-latest-DemoChecksUi.log。
- 検証待ち：CPUの予約マーカー・赤チェック、個々のロボット選択クリック、全指示をスキップせずに完走する経路。和平移動・ポケット出撃・画面上の対象が一致しない場合は専用パネル。内政の通常UI化は今回の範囲外。次回は残る軍事表示と内政の実描画点検へ。

## 2026-11-05 01:29修正回：DEMO-0016のピッカー表示直後のカーソル

- 開始 01:29:23 JST、完了 01:30:46 JST。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f、先行修正の差分を保持。既存 CountrySettings.dll --ui の540p画像でカード上に手が残る状態を再現。
- FolderVerse/Game1.ComputerMarches.csで、通常の出撃ピッカーを表示中は次の操作先 InvasionPickerDone.Center を静止状態で描画する。ピッカー確定の入力と同じ矩形を使用。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で dotnet artifacts/corner-lesson/CountrySettings.dll --ui と dotnet artifacts/corner-lesson/DemoChecksUi.dll は終了コード0。country-settings-12.pngで手がカードから確定ボタンへ移ったことを目視。確定後の二重予約防止とスキップ復帰、Demoの緑／赤遷移も確認。証拠 fix-latest-CountrySettings.log／fix-latest-DemoChecksUi.log。
- この表示ずれは修正・検証完了。DEMO-0016全体は検証待ち。個別ロボット選択の実演、CPUの予約マーカー・赤チェック、専用表示の残る操作、スキップなし完走、内政の通常UI化は今回未対応。

## 2026-11-05 02:30修正回：DEMO-0017

- 開始 02:30:54 JST、終了 02:33:08 JST。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。DEMO-0016の既存差分を保持。修正前の診断入り CountrySettings.dll --ui でフィリアの地点91の専用パネルと組み立てを再現。
- FolderVerse/Game1.ComputerDomestic.cs と tests/CountrySettings/CountrySettingsUi.cs を変更。CPU視点に切り替えた後、投影した地点と通常のヒット判定を照合し、地点インスペクターを開く。共通の格納庫タブ矩形と入力を使い、組み立て結果を通常格納庫のスロットとメッセージに反映。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で同出力 CountrySettings.dll --ui と DemoChecksUi.dll は終了コード0。地点一致、格納庫開放、実組み立て、スキップ後の人間復帰と配送、Demoの緑／赤を確認。audit-latest-computer-domestic.png（540p）でタブ上の手と完成ロボット、結果メッセージを目視。証拠 fix-latest-CountrySettings.log／fix-latest-DemoChecksUi.log。
- 専用パネルの代わりに格納庫を見せる経路は検証済み。組み立て自体は既存のCPU処理で、個別のドラッグや緑チェックは未実装。地点ヒットやタブ操作が成立しない場合は専用表示へ戻る。全CPUのスキップなし完走は未確認。

## 2026-11-05 09:30修正回：DEMO-0016の確定後カーソル

- 開始 09:30:00 JST、完了 09:31:28 JST。HEAD=1b6ed946b563cdd9b64b90303a9d0238945d686f。既存のDEMO-0016／0017差分を保持。修正前の診断入り CountrySettings.dll --ui で確定直後を再描画。
- FolderVerse/Game1.ComputerMarches.cs の描画を修正。ピッカー確定後は入力と共通の InspectorClose.Center を指し、インスペクターを閉じた後は手を消す。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で CountrySettings.dll --ui と DemoChecksUi.dll は終了コード0。960×540の audit-latest-computer-confirm.png で閉じるハンドルを指す手を目視。確定で二重登録がないこと、スキップ後の復帰、Demoの緑・赤遷移を確認。証拠 fix-latest-CountrySettings.log／fix-latest-DemoChecksUi.log。
- 描画診断用のテスト差分は復元。閉鎖後の手の非表示はコード確認。CPUの個別ロボット選択・予約マーカー・赤チェック、通常UI未対応の軍事経路は引き続き残る。

## マージ競合解消

- 2026-11-05 22:31:58 JST、利用者の明示依頼により Game1.Invasion.cs の競合を解消。CPU操作表示中のCanPlanRuler判定と、機能移転後の非機能地点を除外するEnemyTarget判定を両立。git addで同ファイルのみ解決済みとして登録。ゲーム変更はこの競合箇所のみ。
- 既存分離出力へ CountrySettings、FunctionTransfer、DemoUi、DemoChecksUi を --source artifacts/nuget-feed --disable-build-servers でビルドし全て警告・エラー0。CountrySettings --ui、FunctionTransferモデル検査、Seed45のDemoUiとDemoChecksUiの緑赤チェック・即予約・閉鎖・終了遷移は全てコード0。コミット・プッシュ・公開なし。

## 2026-11-06 13:30修正回

- 終了 13:34:57 JST。DEMO-0016の個別選択を対象に Game1.ComputerMarches.cs で通常のカード矩形・入力を使う変更と CountrySettingsUi.cs の検証を試行し、両ファイルの差分を取り消した。
- DOTNET_CLI_TELEMETRY_OPTOUT=1 で dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で CountrySettings.dll --ui は征服者切替の検査で失敗し、テスト内の重複ターン進行を避けた再試行では出撃ピッカー到達の検査で失敗。既存の DemoChecksUi.dll も軍事プライマリーの検査で失敗。原因の切り分けと修正後の実描画は未確認。
- 分離出力に試行ビルドが残るため、次回は現行ソースで対象テストを揃えて再ビルドし、変更前の失敗を切り分ける。修正完了扱いなし。

## 2026-11-06 14:30修正回

- 終了 14:33:22 JST。DEMO-0016の検証前に、前回の現行ビルドで失敗した両テストの軍事フェーズ入口を確認。HEAD=81d4057cee77b11bdcec2f6bc64637b549e1ef84。
- Game1.CampaignPhases.cs の OpenMilitaryPhase は BeginMilitary の直後に Advance を呼ぶ。CPU表示が始まる場合に CountrySettingsUi は続けて EndPlannedTurn を呼ぶ。DemoChecksUi は OpenMilitaryPhase 後も軍事の操作待ちを想定するが、現行経路は Demo で automaticAll を有効にして即時進行する。前回の失敗位置と一致する。操作待ちを直接設定した試験で、実経路の遷移と個別選択の検証を分ける余地がある。
- 今回はコード・テスト無変更、再実行なし。DEMO-0016は現行の実経路でピッカー到達を再確認するまで検証待ち。実ゲームで軍事の操作待ちを設けるかは、配達後の自動進行とDemoの予約実演を切り分けて点検する。

## 2026-11-06 15:30修正回

- 完了 15:34:48 JST。DEMO-0018。Game1.Invasion.cs で開戦入力と予約の条件を既存の WarPlanningAllowed に揃え、内政の出撃元判定と配送後の予約処理を復元。軍事フェーズの予約処理も維持。
- 修正前は内政・段階31・war=False を既存診断で再現済み。修正後は DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditPlanningChecks.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers が警告・エラー0。ALSOFT_DRIVERS=null で dotnet artifacts/corner-lesson/DemoAuditPlanningChecks.dll が終了コード0。
- 地図／地星儀、1080p／540pで実描画し、開戦クリック (410,1022)、地点選択、出撃確定と即予約、赤チェック、閉鎖、ターン終了と消去を検証。診断は内政を起点とする分離試験。配送後の戦闘実行とCPU個別選択は未再検証。

## 2026-11-06 18:30修正回

- 完了 18:36:08 JST。DEMO-0016の通常出撃選択に到達しない経路を修正。Game1.Invasion.cs の CanPlanRuler でCPU軍事表示中は表示中の征服者を候補にし、既存の活動条件を適用。
- DOTNET_CLI_TELEMETRY_OPTOUT=1 で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で dotnet artifacts/corner-lesson/DemoAuditCpuSortie.dll は終了コード0。fix-latest-cpu-sortie.log で征服者1、目標93、picker=0、確定時の二重予約なし、スキップ後の復帰を確認。country-settings-12.png（960×540）で通常ピッカーと確定を指すカーソルを目視。
- 個別ロボット選択は引き続き省略される。専用表示への退避条件とDemo巡回は今回未再検証。DEMO-0016全体は検証待ちを継続。

## 2026-11-06 19:30修正回

- 完了 19:33:00 JST。DEMO-0016の個別選択。Game1.ComputerMarches.cs でAIの出撃対象を通常ピッカーのカード矩形へ対応付け、通常入力で1体ずつ選択してから確定する。表示できないカードを含む場合は既存の専用表示へ戻る。
- DOTNET_CLI_TELEMETRY_OPTOUT=1、dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で DemoAuditCpuSortie.dll と DemoAuditPlanningChecks.dll は終了コード0。既存の非追跡診断を拡張し、地点93の空の選択から4回のクリック、AI指定ID、二重予約なし、スキップ復帰を確認。960×540の audit-latest-computer-confirm.png で1枚目のカード内のカーソルと選択色を目視。診断画像にフェーズ演出の重なりあり。
- Demoは展開図／地星儀、1080p／540pで緑赤チェック、出撃確定と即予約、閉鎖、ターン終了と配送後の進軍を再確認。証拠は artifacts/demo-click-audit の fix-latest-cpu-sortie.log と fix-latest-planning-checks.log。専用表示への退避とCPU側の赤チェックは未検証。

## 2026-11-07 03:30修正回

- 終了 03:33:38 JST。DEMO-0016の和平移動を確認。直近の実描画は表示用データのみ和平移動へ差し替えた診断で、実際の和平指示の生成・実行は要再確認。
- Game1.Movement.cs の通常の移動確定は Campaign.Plan に予約を追加する。CPU表示は既に受け取った指示一覧を最後に実行するため、通常入力を共用する場合は表示中の確定と実予約の追加を分離する必要がある。次回は現行のAIが生成した和平指示を用い、移動先のクリック判定と指示の一回実行を照合する。
- ゲームコード・テスト無変更。今回の実行検証なし。既存の通常出撃選択の検証結果は前回記録参照。

## 2026-11-07 05:30修正回

- 完了 05:30:07 JST。DEMO-0016の和平移動。Game1.ComputerMarches.cs と Game1.Movement.cs で通常の移動パネル、地図のヒット判定、移動予約ボタンの入力を共用。表示中の確定は追加予約を作らず、元の指示一覧が実行を担当する。通常UIで選べない指示は専用表示に戻る。
- dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で DemoAuditCpuSortie.dll と DemoAuditPlanningChecks.dll は終了コード0。征服者17の地点204の選択、確定後の閉鎖と追加予約なし、スキップ復帰を確認。960×540の audit-latest-computer-fallback.png で通常移動パネルと地点を指す手を目視。Demoの緑赤チェックと予約から配送後の進軍も再確認。
- 和平移動ごとの実行回数と残る専用表示・CPU赤チェックは未検証。

## 2026-11-07 07:30修正回

- 完了 07:29:11 JST。DEMO-0016のマージ後回帰検査。CountrySettingsUi.cs を個別選択の手順に合わせ、各クリックで1体増えること、AI指定IDと一致すること、確定前後に予約件数が増えないことを検査。ゲームコード無変更。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=artifacts/tmp/delete-after-2026-11-07/merge-check/ -v quiet --disable-build-servers 相当の絶対出力パス指定で警告・エラー0。ALSOFT_DRIVERS=null で同出力の CountrySettings.dll --ui は終了コード0。選択ごとの描画、確定時の閉鎖、スキップ後の復帰を確認。今回は画像の目視比較とDemo自然巡回は未実施。

## 2026-11-07 09:30修正回

- 完了 09:32:13 JST。09:00回の庁舎検査（DEMO新規採番なし）。tests/DemoInspectorUi/Program.cs の初期巡回番号を1に設定。既定値0では開戦経路を選ぶため、庁舎検査に到達しなかった。ゲームコード無変更。
- dotnet build tests/DemoInspectorUi --source artifacts/nuget-feed -p:OutputPath=<絶対パス>/artifacts/tmp/delete-after-2026-11-07/merge-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で DemoInspectorUi.dll は終了コード0。1080p／540pで庁舎、閉鎖、直送便プライマリー、工場の実矩形と製造開始を検証。証拠は同出力の fix-DemoInspectorUi.log。左右の直送便とCPU和平移動の実行回数は未再検証。

## 2026-11-07 13:30修正回

- 完了 13:29:39 JST。DEMO-0016のCPU赤チェック。Game1.ComputerMarches.cs に通常の NodeCheckButton と NodeCheckClick を使う確定→赤チェック→閉鎖の段階を追加。Game1.NodeChecks.cs でCPU軍事表示中のチェック所有者を表示中の征服者にし、再生終了時にCPUの赤チェックを消去する。既に付いたチェックは解除しない。
- CountrySettingsUi に赤チェックの保持と終了時消去の検査を追加。tests/CountrySettings と既存 DemoAuditCpuSortie を artifacts/tmp/delete-after-2026-11-07/merge-check へビルドし警告・エラー0。ALSOFT_DRIVERS=null で両方の実行がコード0。個別4選択、予約件数維持、赤チェック保持・消去、スキップ復帰、通常再生完了を確認。
- artifacts/demo-click-audit/audit-latest-cpu-red.png（960×540）でハンドルと地点の赤チェックを目視。診断ログの段階5・所有者1も一致。既存 DemoAuditPlanningChecks もコード0で展開図／地星儀の1080p／540pを確認。証拠は一時出力の fix-cpu-red.log、fix-cpu-red-draw.log、fix-red-demo-regression.log。CPU内政の緑チェック、画面外の指示や専用表示への退避は未検証。

## 2026-11-07 15:30修正回

- 完了 15:28:15 JST。DEMO-0017のCPU内政緑チェック。Game1.ComputerDomestic.cs に格納庫確認後の実ハンドル入力→閉鎖→次地点の段階を追加し、内政終了時にCPUの緑チェックを消去。Game1.NodeChecks.cs でCPU内政表示中も表示中の征服者へ保持。
- CountrySettingsUi に緑チェック保持と配送前消去の検査を追加。CountrySettings と既存 DemoAuditCpuSortie を artifacts/tmp/delete-after-2026-11-07/merge-check へビルドし、両方とも警告・エラー0。ALSOFT_DRIVERS=null で両実行は終了コード0。960×540の artifacts/demo-click-audit/audit-latest-computer-domestic.png でハンドル・地点・地点見出しの緑チェックを目視。赤チェックと和平移動の通常完了も回帰成功。
- 証拠は一時出力の fix-cpu-green.log と fix-cpu-green-draw.log。画面外の地点で専用表示へ戻る経路、全人数・全Seed、内政の通常終了時の緑チェック消去の実描画は未確認。

## 2026-11-08 20:28修正回

- 終了 20:29:19 JST。前回の国設定検査失敗をコード照合。CountrySettingsUi は主色と副色の両方に色6を選んだ結果を期待するが、現行 Game1.CountrySettings.cs は副色パレットから主色と同じ色を除外する。検査条件と入力可否の不一致がある。
- ゲーム側の新規不具合としては未登録。国設定の失敗時の各設定値とDemo開戦対象未到達の状態採取は未実施で、修正完了扱いなし。今回はゲーム・テスト無変更、実行検証なし。

## 2026-11-08 21:30修正回

- 完了 21:31:36 JST。DemoChecksUi の失敗を修正。tests/DemoChecksUi/Program.cs を現行の内政中の予約手順へ合わせ、緑チェック後も内政のまま開戦・出撃選択・即予約・赤チェック・閉鎖・ターン終了を進める。配送終了後に予約先への進軍を確認。ゲームコード無変更。
- dotnet build tests/DemoChecksUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-08/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null で DemoChecksUi.dll は終了コード0。展開図／地星儀、1920×1080／960×540の4条件が成功。赤チェックのハンドルと地点を demo-checks-3-6.png で目視。
- 証拠は同出力の fix-DemoChecksUi.log。緑赤表示の分離、ターン終了時の両チェック消去も検査。国設定の失敗は今回未修正。物理マウス再選択と自然Demo完走は未確認。

## 2026-11-08 22:28修正回

- 完了 22:29:04 JST。19:55回の国設定検査失敗を解消（ゲーム不具合の新規IDなし）。tests/CountrySettings/CountrySettingsUi.cs で主色6と異なる副色7を実入力で選び、その後の副色6への入力が拒否されて7を維持することを検査。現行UIの同色除外へ期待値を合わせた。ゲームコード無変更。
- dotnet build tests/CountrySettings --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-08/demo-check/ -v quiet --disable-build-servers は警告・エラー0。DOTNET_CLI_TELEMETRY_OPTOUT=1、ALSOFT_DRIVERS=null で同出力の CountrySettings.dll --ui は終了コード0。証拠は fix-CountrySettings.log。
- 実描画を伴う検査で国設定の入力・表示オンオフ、征服者切り替え、通常格納庫・組み立て、軍事の個別選択・予約維持、緑赤チェックの保持と消去、スキップ復帰まで成功。今回画像の目視比較と自然Demo完走、全Seedや専用表示への退避は未検証。DEMO-0016／0017の既存制約は継続。

## 2026-11-08 23:27修正回

- 終了 23:28:03 JST、部分修正・検証待ち。対象は22:58回のDirectDeliveryUi実行不能（DEMO新規IDなし）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分は点検記録のみ、ゲーム・検証プロセスなし。
- tests/DirectDeliveryUi/Program.cs の削除済み DirectMapCloseButton の反射参照を、描画・入力が共有する MapModeCloseButton(3) に置換。ゲームコード無変更。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DirectDeliveryUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-08/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=nullで同出力のDirectDeliveryUi.dllを起動、fix-direct.logへ採取。
- 旧参照例外は解消し、プライマリー退避・復帰・途中入力抑止・直送便終了・Escapeの検査はPASS。ただし後続Program.cs 70行の右→左ドラッグ予約で停止（終了コード -532462766）。現行はPartPlanning.Reserveを使うため、DirectDelivery.Ordersを即時期待する検査との差を次回切り分ける。Draw到達前の停止で実描画・縮小表示・Demo遷移は未確認、全体修正完了とは扱わない。git diff --check成功。

## 2026-11-09 00:26修正回

- 完了 00:28:37 JST。対象は22:58回からの直送便テスト不一致（DEMO新規IDなし）。HEAD=200f19005a2d5d1ec5114282941a9e425e75d6eb。開始時差分は旧ボタン参照修正と両記録、ゲーム・検証プロセスなし。ゲームコード無変更。
- tests/DirectDeliveryUi/Program.cs を現行PartPlanning予約へ移行。右→左・左→右、実在庫保持とプレビュー反映、同地点組替の予約、保存復元、実取消ボタン、全パーツの一括予約と失敗時の無変更を検査。地図切替・チェック・アニメーション中の予約保持も空の旧DirectDelivery予約ではなくPartPlanning予約で照合。旧ボタン参照の共有矩形化も維持。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DirectDeliveryUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=nullで同出力DirectDeliveryUi.dllは終了コード0。最終変更後に再ビルド・再実行成功。証拠fix-direct.log、direct-0.png〜direct-22.png。同日の既存出力がなかったため期限付き出力を作成し、前日分は削除せず保存。
- 実描画の960×540 direct-6.pngで左右インスペクター、中央地図、取消・閉じる・チェックの表示を目視。プライマリー退避復帰、地星儀／回転展開図の左右配置、540p物理座標入力、連続ペア切替、地図接続アニメーションと予約保持が成功。git diff --check成功。自然Demo巡回とCPU操作は今回未再検証で、既存DEMO-0016／0017の未確認範囲は継続。

## 2026-11-09 01:35修正回

- 終了 01:36:03 JST、部分修正・検証待ち。対象00:58回のDemoUi固定戦闘シーン例外（新規DEMO採番なし）。開始時差分はDirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲームコード無変更。
- tests/DemoUi/Program.csで固定シーン導入時の旧配列長・波番号をログ化し、単一シーンに合わせ波番号を0へ初期化。実行ログは旧16シーン・波番号4で、長さ1への差替え後に波番号が残るテスト条件が原因と確認。通常OpenRobotBattleは波番号を初期化しており、ゲーム側へ防御変更は加えていない。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12でDemoUi.dllを実行。配列例外は解消し、戦闘導入の進行検査は成功。
- 後続の勧誘拒否フォールバック検査（Program.cs104行）でPendingが残り停止、終了コード -532462766。証拠は同出力fix-demo.log。Drawへ未到達で描画・Demo完走は未確認。後続停止は待ち時間・現行処遇UIとの条件差を次回切り分け、ゲーム不具合とは未判定。全体修正完了にはしない。

## 2026-11-09 02:29修正回

- 完了 02:30:49 JST。対象01:55回の処遇テスト条件不一致（DEMO新規IDなし）。開始時はDemoUi・DirectDeliveryUiの既存修正と両記録のみ、ゲーム・検証プロセスなし。ゲームコード無変更。
- tests/DemoUi/Program.cs のTickで通常更新と同じUpdateDispositionScene時計を進める。性格別の処遇は固定55フレームから、最大360フレームでPending・交渉入力・結果確認・演出のすべてが終わるまで進める検査へ変更。各終了状態と勧誘受諾／解放の実結果を確認し、次の固定捕虜へ進む。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_DEMO_TEST_SEED=12でDemoUi.dllは最終変更後も終了コード0。証拠fix-demo.logとdemo.png。1920×1080のタイトル上のDemoラベルを目視。
- 配送、地星儀／展開図と18FPS地図接続、固定戦闘導入、4性格の処遇、エンディング経路・タイトル復帰と再開の検査が成功。固定場面を含む検査であり、自然Demo全経路完走や処遇途中の各画面の実描画、CPU操作の再検証は未実施。ゲーム変更の完了とは扱わず、検査不一致の解消として記録。

## 2026-11-09 13:27修正回

- 完了 13:28:29 JST。対象12:59回の非追跡診断の和平0件時例外（DEMO新規IDなし）。開始時差分はDemoUi・DirectDeliveryUiと両記録、ゲーム・検証プロセスなし。ゲーム・追跡テスト無変更。
- artifacts/demo-click-audit/ProbeCpuSortie.cs.txtで和平指示がない場合は別の指示を探し続けず、有限1000更新内で再生完了・人間0・内政フェーズを検査してDraw後に終了するよう変更。既定の和平あり経路は維持。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-09/demo-check/ -v quiet --disable-build-servers は警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_FALLBACK=0、FOLDERVERSE_AUDIT_WORLD=1でDemoAuditCpuSortie.dllは終了コード0。証拠fix-cpu-world1.log。
- 世界1の15表示指示で通常出撃選択・個別4クリック・赤チェックからスキップせず完了し、人間復帰・内政描画を確認。今回画像目視なし。診断例外の解消であり、ゲーム修正ではない。自然退避・全指示実行回数・全Seedは未確認、既存DEMO-0016の制約を継続。

## 2026-11-10 03:33修正回

- 完了 03:35:56 JST。対象DEMO-0016の同行ロボット0体の非和平Queen指示。02:56回の合法固定場面を現行コードで使用。開始時差分は点検記録のみ、ゲーム・検証プロセスなし。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。
- FolderVerse/Game1.ComputerMarches.csでQueen=true／Fighters=0を通常再生の対象へ追加。現在地のQueen行を選び、通常ピッカーで実同行トグルをクリック→確定→赤チェック→通常閉鎖を再生。共有InvasionQueenToggle／InvasionPickerDone／NodeCheckButton／InspectorClose矩形を使用。CPU確定の既存ガードを維持し、元指示列だけが実行を受け持つ。ロボットありQueen／PocketOnlyのみの非和平指示と画面外カードは専用表示のまま。
- 非追跡ProbeCpuSortieの合法Queen診断を新経路に更新。通常ピッカー・同行トグル・確定時追加予約なし・赤チェック・閉鎖・スキップ人間復帰とLastQueenMarchesの同地点指示1件を検査。DOTNET_CLI_TELEMETRY_OPTOUT=1でDemoAuditCpuSortie.csprojをartifacts/tmp/delete-after-2026-11-10/demo-check/へビルド（--source artifacts/nuget-feed、-v quiet、--disable-build-servers）、警告・エラー0。
- ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_LEGAL_QUEEN=1で終了コード0。fix-legal-queen.logと1920×1080 audit-legal-queen.pngで通常開戦インスペクター、Queen参加0体、ハンドル・地点の赤チェックを確認・目視。通常診断（LEGAL_QUEEN=0、FALLBACK=0、WORLD=0）も終了コード0で、オンオフ・CPU切替・緑赤消去・個別出撃選択・和平移動と到着・人間復帰を回帰確認。証拠fix-queen-regression.log。git diff --check成功。
- この対応は合法固定指示の同行0体に限定。自然AI生成、Queenの540p実描画・スキップなし終了、同行ありQueen、19体カード、自然Demo全経路は未確認。DEMO-0016全体は一部対応・検証待ちを継続。コミット・プッシュなし。

## 2026-11-10 05:26修正回

- 完了 05:29:54 JST。対象DEMO-0016、05:04点検で再現した合法な同行2体のQueen指示。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。先行作業・ゲームプロセスなしを確認し、既存の同行0体対応差分を保って拡張。
- FolderVerse/Game1.ComputerMarches.csの通常再生条件をQueenの非負同行数へ拡張。既存の共有矩形・入力経路でQueen参加トグル、指定RobotIdsの各カード、確定、赤チェック、閉鎖を順次クリックする。通常UI仕様・AI指示を変更せず、CPU確定時の追加予約防止を維持。非追跡artifacts/demo-click-audit/ProbeCpuSortie.cs.txtを同行カードごとの選択件数とID集合の検査へ更新。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功、警告・エラー0。ALSOFT_DRIVERS=nullで同出力のDemoAuditCpuSortie.dllを実行。
- LEGAL_QUEEN=1／QUEEN_ESCORTS=1（環境変数接頭辞FOLDERVERSE_AUDIT_）でQUEEN540=1と0の両方が終了コード0。540pはスキップなし、1080pはスキップで人間0へ復帰。確定時PlannedOrders不増、赤チェック、閉鎖、LastQueenMarches同指示1件を確認。証拠fix-queen-escorts.log、fix-queen-escorts1080.log。540pの実描画でQueen参加・2体と赤チェックを目視（audit-legal-queen.pngは後続の同一診断で上書き再利用）。
- QUEEN_ESCORTS=0／QUEEN540=1も成功（fix-queen-solo-regression.log）。LEGAL_QUEEN=0／FALLBACK=0／WORLD=0の通常診断も成功し、操作表示オンオフ、征服者切替、個別出撃選択、緑赤消去、和平到着、人間復帰を回帰確認（fix-queen-escorts-regression.log）。
- DEMO-0016全体は一部対応・検証待ち。自然AI生成の同行Queen、画面外カード、非QueenのPocketOnly専用表示、自然Demo全経路、戦闘内の同行個体ID照合は未確認。既存のカード到達不能時フォールバックを維持。コミット・プッシュなし。

## 2026-11-10 06:32修正回

- 開始06:32:27、終了06:32:40 JST。AGENTS.md・README・両記録・現行差分とプロセスを確認。先行作業・ゲーム・検証プロセスなし。HEAD=838cd06d70063f5af6344f6901c6b0d09b3d1ff6。既存同行Queen差分を保護し、ゲーム・テスト・診断コード無変更。
- 対象DEMO-0016の残件。直前の完了済み05:59点検では非Queen PocketOnlyと画面外カードは静的照合のみで、合法場面の実行再現・実描画は未実施。既に再現済みの同行0／2体Queenは05:26修正の検証済み範囲であり、追加修正の根拠となる新しい実行結果はない。
- 今回は追加修正・ビルド・実行を行わず、一部対応・検証待ちを維持。未確認を完了に変更しない。次の点検で非Queen PocketOnlyの合法な通常出撃元・対象・IDを確立し、専用表示と通常UIの差を実描画で再現してから修正対象を確定する。出力追加なし、コミット・プッシュなし。

## 2026-11-10 07:28修正回

- 開始07:28:58 JST、完了07:33:46 JST。対象DEMO-0016、06:59完了点検の非Queen PocketOnly出撃。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時差分は点検記録のみ、先行作業・ゲーム・検証プロセスなし。修正前の既存診断を同条件で実行し、通常出撃元があるのに専用パネルを使う状態を再現（fix-pocket-before.log、終了コード0）。
- 変更ファイルFolderVerse/Game1.ComputerMarches.cs。PocketOnly=true／Fighters>0を通常再生へ追加し、QueenまたはPocketOnlyなら現在地のポケット行を使用。ピッカーの征服者参加をオフで開始し、Queen=trueの場合だけ既存トグルクリックを行う。指定カード→確定→赤チェック→閉鎖は既存の共有矩形・入力を再利用。AIの元指示・一括解決とCPU確定時の追加予約防止は維持。
- 非追跡artifacts/demo-click-audit/ProbeCpuSortie.cs.txtを通常UI期待へ更新。指定ID集合とカードごとの件数、参加オフ、追加予約なし、赤チェック、閉鎖、人間復帰、Queen移動記録0件を検査。戦闘内の指定ID各1件とQueen不参加も確認。初回の「CPU1の戦闘全個体＝指定2体」検査は既存駐屯ID274～276を含むため不一致。敵側守備1体を固定場面に追加し、指定ID767・768が各1件参加する検査へ補正して成功。ゲームの指示不一致とは扱わない。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功、最終ビルド警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_LEGAL_QUEEN／QUEEN_ESCORTS／POCKET_ONLY=1で同出力DemoAuditCpuSortie.dllを実行。QUEEN540=1の通常終了（fix-pocket540.log）と0の1080pスキップ（fix-pocket1080-skip.log）がともに終了コード0。
- 960×540 audit-legal-queen.pngの通常開戦インスペクター、Queen不参加／2体、2カード表示、地点とハンドル赤チェックを目視。画像は後続Queen回帰で同名再利用。POCKET_ONLY=0の同行Queen回帰も成功（fix-pocket-queen-regression.log）。LEGAL_QUEEN=0／FALLBACK=0／WORLD=0の通常診断も成功し、オンオフ・征服者切替・緑赤消去・個別出撃・和平到着・人間復帰を確認（fix-pocket-regression.log）。git diff --check成功。
- DEMO-0016は固定場面のポケット単独2体まで対応済み、全体は一部対応・検証待ち。自然AI生成、画面外19体カード・適格出撃元なし等の専用表示、自然Demo全経路は未確認／制約を継続。UI仕様変更・出力先追加・コミット・プッシュなし。

## 2026-11-10 08:33修正回

- 開始08:33:29、終了08:33:45 JST。必須文書・差分・プロセスを確認。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。先行作業・ゲームプロセスなし。07:28のポケット単独対応差分を保護。
- 直前07:59の完了点検は直送便検査成功・新規不具合なし。DEMO-0016残件の19体カードは合法固定場面の実行点検が未完了で、追加修正の根拠となる新しい再現結果なし。今回はコード・テスト・診断変更、ビルド・実行を行わず、一部対応・検証待ちを維持。次回点検の続き位置は19体カードの専用表示退避と次指示復帰。出力追加・コミット・プッシュなし。

## 2026-11-10 09:29修正回

- 開始09:29:30、終了09:30:22 JST。必須文書・関係差分・プロセス確認済み。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。先行作業・ゲームなし。既存ポケット対応差分を保護。ゲーム・テスト・診断ソース変更なし。
- 08:55点検は19体場面の容量条件不一致で中断し、追加ゲーム修正の根拠なし。DEMO-0016の当該範囲は要再確認を維持。除去済み試行分岐がDLLに残る引き継ぎ制約のみ解消するため、現行診断を既存出力へ再ビルド。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（警告・エラー0）。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_LEGAL_QUEEN／QUEEN_ESCORTS／QUEEN540／POCKET_ONLY=1、CARD19=0で同出力DemoAuditCpuSortie.dllは終了コード0。fix-probe-refresh.logでポケット2体個別選択・確定・追加予約なし・赤チェック・閉鎖・人間復帰、指定個体各1件の実戦参加とQueen不参加を確認。今回画像目視なし。
- ゲームの新規修正完了ではなく検証出力の同期。次回点検は拡張地上格納庫による19体の合法場面から継続。自然AI／自然Demo全経路は未確認。出力先追加・コミット・プッシュなし。

## 2026-11-10 10:26修正回

- 開始10:26:01、終了10:26:37 JST。対象DEMO-0016の画面外19体カード。必須文書・差分・プロセスを確認。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。先行作業・ゲームなし。既存ポケット対応差分を保護し、ゲーム・テスト・診断コード変更なし。
- 09:56完了点検の現行診断をALSOFT_DRIVERS=null／FOLDERVERSE_AUDIT_CARD19=1で既存出力DemoAuditCpuSortie.dllから再実行。終了コード0（fix-card19-review.log）。source0→target3の19体は専用表示へ退避、追加予約なし、次CPU3で通常ピッカーへ復帰する現状を再現。今回はビルド・画像目視なし、描画の確認は前回記録を参照。
- Game1.InvasionPicker.csと関連入力を照合。通常カードは固定6列でInvasionRobotCard(i)に並び、カード専用ページ・スクロール入力がない。19枚目は4段目Y=830からで確定ボタンY=795以降に入る。既存CPUガードはこの到達不能な位置へのクリックを防ぐ。通常UI仕様を変更せず既存操作だけで19体を選ぶ経路を確認できなかったため、ガード削除・直接選択による見かけの対応は行わない。
- 最小限の通常UI再利用では解消できない表示容量の制約として専用表示を維持。DEMO-0016は一部対応・検証待ち、19体通常操作は要再確認。自然AIでの拡張格納庫発生・通常UIの多数個体への対応方針は未確認。次回点検は自然Demo経路・チェック実演の回帰へ戻り、この制約を全体完了に含めない。出力先追加・コミット・プッシュなし。

## 2026-11-10 11:35修正回

- 開始11:35:02、終了11:35:20 JST。必須文書・現行差分・プロセス確認済み。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。先行作業・ゲームなし。既存ポケット対応差分を保護。
- 直前11:04完了点検の緑赤チェック4条件は成功、新規不具合なし。DEMO-0016の19体表示容量制約は10:26回の判断から変化なし。追加修正対象がないため、ゲーム・テスト・診断の変更および追加ビルド・実行なし。状態は一部対応・検証待ちを維持し、次回点検はDemoUi Seed12の既存巡回から継続。出力追加・コミット・プッシュなし。

## 2026-11-10 14:33修正回

- 開始14:33:05、終了14:34:00 JST。対象DEMO-0017のCPU組立個別操作不足。必須文書・関係差分・プロセス確認済み。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。先行作業・ゲームなし。既存ポケット対応差分を保護。ゲーム・テスト・診断変更なし。
- 13:56完了点検の既存診断をALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_CARD19=0／LEGAL_QUEEN=0／FALLBACK=0／WORLD=0で再実行。既存出力DemoAuditCpuSortie.dll終了コード0（fix-domestic-review.log）。通常格納庫を開いて直接組立する現状を再現。今回ビルド・画像目視なし、実描画証拠は直前点検を参照。
- 通常入力再利用の前提を精査。DemoAssemblyPress／ReleaseはNodeAssemblyInputを経てPartPlanning.Reserveへ入り、実在庫ではなくPlanningStockの組替後予定を作る。CPU現行経路はRobots.Assembleで実在庫を即変更する。予約の確定はConquestCampaign.BeginDeliveryで、機能移転・生産入荷・通常配送後にPartPlanning.Resolveを一括実行する。単純に両経路を併用すると同じ素材に実組立と予約が重なり、全予約を早期Resolveすると利用者予約にも影響しうるため行わない。
- 今回は安全な最小差分を確定できず、修正未完了。DEMO-0017は一部対応・検証待ちを維持し、直接組立の専用処理を明記。次回点検はAI選択IDの通常予約への変換と配送確定後の完成個体・在庫結果を、操作表示オフ／オンで照合する固定場面を優先する。UIを開けるだけで実操作対応済みとは扱わない。19体制約・自然Demo全経路も未確認を継続。出力先追加・コミット・プッシュなし。

## 2026-11-10 16:25修正回

- 開始16:25:36、完了16:28:07 JST。対象16:03点検のDemoActionsUi初期条件不一致（新規DEMO IDなし）、DEMO-0017の通常入力検証前提。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時はポケット対応Game1.ComputerMarches.csと両記録の既存差分、ゲーム・検証プロセスなし。ゲーム無変更。
- tests/DemoActionsUi/Program.csで_demoMapVisits=1を明示し内政巡回として開始。組立対象Target()を実在庫からPlanningStockへ変更。各固定場面の予約をリセットし自動組立をオフにして、個別パーツ操作自体を検査する。実在庫の操作前後一致、実ドロップ数と予約件数の一致、検証WorldSetupで予約確定後に完成1体・残予約0となる検査も追加。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build tests/DemoActionsUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功（最終警告・エラー0）。ALSOFT_DRIVERS=nullで同出力DemoActionsUi.dllの最終実行は終了コード0。fix-actions.log、demo-actions-0～61.pngを既存出力先で再利用。
- 1080p／540pで分離3パーツ、2梱包、一体型の6条件が実press／保持／dropを通過。確定前実在庫不変・予約各1回・確定後完成1体を確認。6投影条件の地点実ヒット、和平予約と別ターン終了、再試行／対象なしも成功。960×540 demo-actions-33.pngで組立対象上のカーソルと格納庫実描画を目視。git diff --check成功。
- 検査不一致の解消でありCPU通常組立の実装完了ではない。DEMO-0017は一部対応・検証待ち。CPU表示オンオフ、征服者切替、スキップ途中、利用者の既存予約との共存・実配送全順序は未検証。次回はこれらを固定CPU場面で比較してからゲーム実装へ進む。コミット・プッシュなし。

## 2026-11-10 20:28修正回

- 開始20:28:40、完了20:31:56 JST。対象DEMO-0017の19:59完了点検で確認された自動組立オフ無視・表示オンオフの在庫差。HEAD=919eec6cb7e5af679be1d2efe3af4c46dc52e175。開始時差分はポケット対応、DemoActionsUiと両記録。先行作業・ゲームなし。修正前に既存ENTRY診断を再実行しオフ3パーツ／オン完成1体を再現（fix-disabled-before.log）。
- 変更ファイルFolderVerse/Game1.ComputerDomestic.cs。表示待ち中の直接Assemble条件に、その地点のWorkshop.AutoAssemblyDisabledがfalseであることを追加。自動組立を無効にしたAI設定を表示再生が上書きしない。UI仕様・AI選択条件・既存予約・スキップ時点管理は変更しない。個別パーツ操作実演の未対応は継続。
- 非追跡ProbeCpuSortieのENTRY診断を、自動組立設定ごとに表示オフ／オン通常／組立前後スキップの最終在庫ID・Parts・Ownerがすべて一致する検査へ更新。既存人間予約の保持と配送時1回確定、人間復帰・Deliveryも検査。自動組立オフは頭胴脚ID1～3を維持、オンはID1の完成1体で両方成功。
- DOTNET_CLI_TELEMETRY_OPTOUT=1で dotnet build artifacts/demo-click-audit/DemoAuditCpuSortie.csproj --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-10/demo-check/ -v quiet --disable-build-servers 成功、最終警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_AUDIT_DOMESTIC_SKIP=1／DOMESTIC_ENTRY=1で実行し、AUTO_ENABLED=0と1の両方が終了コード0（fix-auto-disabled.log、fix-auto-enabled.log）。
- 無効時の通常再生は960×540で3パーツ維持、組立成功フラグなし、実ハンドル緑チェックを検査・描画し目視（audit-latest-computer-domestic.pngを同名再利用、後続回帰で上書き）。通常診断（DOMESTIC_SKIP=0、LEGAL_QUEEN=0、CARD19=0、ASSEMBLY_COMPARE=0、FALLBACK=0、WORLD=0）も終了コード0で、オンオフ・征服者切替・緑赤消去・個別出撃・和平到着・人間復帰が成功（fix-auto-regression.log）。git diff --check成功。
- 修正完了は自動組立オフ時の表示による追加組立に限定。DEMO-0017全体は一部対応・検証待ち、通常入力での組立実演は未完了。生産・機能移転や品質／損傷を伴う全条件・自然Demo全経路・19体カード通常操作は未確認。出力先追加・コミット・プッシュなし。
