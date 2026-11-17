# チュートリアル点検記録

## 2026-11-17 08:11回

- 開始08:11:50、終了08:16 JST。部分点検完了。HEAD `4620e675987fcde833f0c35a1dc02051ad2409a4`。開始時の差分は引き継ぎ・Demo点検記録・利用者メモ。ゲームコード変更なし。開始時に競合するDemo・チュートリアルの実行プロセスは確認されなかった。
- `dotnet build tests/<対象>/<対象>.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v minimal` で TutorialPlanetMenuUi と TutorialJourneyProgressUi をビルド。警告・エラーともに0。各診断DLLを `ALSOFT_DRIVERS=null` で順に実行。起動要求は08:14:38、08:15:43、両方とも終了0。
- 入口診断ではタイトルのSTARTメニューを処理から開き、［遊び方］と星カードの入力を検査。六つの星を描画し、残り五つのクリックで起動しないこと、準備中の星でのEnter無効、プチステラのクリック・Enter起動、EscでSTARTメニュー復帰を確認。準備中の文言の実画像目視は未実施。
- 進行診断では地図案内を済ませた状態を設定し、戦闘解決から現周回の勲章判定、ポケット・移動、完了、案内の再表示とセーブ再開を検証。診断出力内の `journey-save.gz` と `journey-medals.json` を利用。証拠は各PASSと `tutorial-planets-0.png`、`journey-progress-0.png`〜`journey-progress-7.png`。PNGヘッダーで1920×1080を確認。
- 新規TUT採番なし。実操作での通し点検は未実施。次回は分離した保存先でタイトルのSTART実クリックから入り、絵本とモブ女の案内、地図→開戦予約→陣営のターン終了→戦闘を順に操作する。続いてポケット・移動、最終目標、完了表示・勲章・タイトル復帰を同一セッションで確認する。

## 2026-11-17 08:17改善回

- 08:17 JST。HEAD `c2a8113c`。直近の部分点検では入口・進行のUI診断がともに終了0。採番済みの未解決項目なし。点検時のHEADからゲームコードの差分はなく、今回の修正・再実行はなし。
- 次回点検は、分離保存先でSTARTの実クリックから入り、モブ女の案内と通常入力を照合する通し操作から再開する。準備中の文言の目視、最終目標からタイトル復帰までの連続操作は未確認。

## 2026-11-17 08:29点検回

- 開始08:29:20、終了08:30 JST。部分点検完了。HEAD `c2a8113c61cf724993429c4f4c62db87a4185d4f`。開始時の関係差分は直送便テストとDemo両記録、既存の本記録。競合する検証プロセスなし。ゲームコード変更・新規TUT採番なし。
- 08:11回のNAS出力 `bin/TutorialPlanetMenuUi/debug/tutorial-planets-0.png`（1920×1080）を目視。プチステラに「この星へ出発」、残る5星すべてに「このチュートリアルはまだ作ってません。」を確認。現行の描画分岐と一致し、撮影時のHEADからゲームと入口テストに差分なし。入口の入力結果は08:11回を参照。新規ビルド・診断起動なし。
- START実クリック以降の通し操作は未実施。通常起動時のオートセーブと勲章はユーザーのLocalApplicationDataを使うため、次回は両方を分離した検証ホストでタイトルの通常入力から開始する。地図・開戦予約・ターン終了を先に通し、その後の戦闘から最終目標・タイトル復帰までを追う。

## 2026-11-17 08:36改善回

- 08:36 JST。HEAD `c2a8113c61cf724993429c4f4c62db87a4185d4f`。08:29点検で準備中表示を確認し、修正対象となる未解決のTUT項目はなし。ゲームコード変更・検証起動なし。既存のDemo差分を保持。
- 次回点検は分離した保存先を使い、タイトルのSTART実クリックから地図・開戦予約・陣営のターン終了までを追う。戦闘から最終目標・タイトル復帰までの通し操作も未確認。

## 2026-11-17 08:49点検回

- 開始08:49:20、終了08:53 JST。部分点検後に診断中断。HEAD `c2a8113c61cf724993429c4f4c62db87a4185d4f`。関係する未コミット差分は既存のDemoテストと記録類。ゲームコード変更なし。
- 既存のNAS出力 `Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit` に `dotnet build tests/TitleStartMenu/TitleStartMenu.csproj --artifacts-path <同出力> --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v minimal`。起動・作成08:50:46、PID 19676、上限600秒。08:52:22終了0、警告・エラー0。
- 同出力の `bin/TitleStartMenu/debug/TitleStartMenu.dll` を分離オートセーブと `ALSOFT_DRIVERS=null` で実行。起動・作成08:52:22、PID 26460、上限300秒。08:52:47終了 -532462766。証拠は `TitleStartMenu-{build,run}-0849.{stdout,stderr}.log`。通常入力処理でSTART→遊び方→星選択まで通過。描画前の初期化で中断し、今回の実解像度は未取得。
- 次回は下記の診断を現行の絵本導入に合わせてから、地図・開戦予約・ターン終了の通常入力を確認する。戦闘から最終目標・タイトル復帰までの通し操作は未確認。

### TUT-0001 START診断が絵本導入前の遷移を要求

- 2026-11-17 08:52:47 JST、上記HEAD・差分。診断修正済み、通し操作は検証待ち（09:15改善回）。 `tests/TitleStartMenu/Program.cs:28` でプチステラをクリックすると、診断は即座に `TutorialMapBeforeWar` が立つことを期待し「First planet directly starts integrated tutorial」で停止する。
- 現行の星カードは `BeginOpening(true)` で絵本を開始し、その終了でチュートリアルを初期化する。期待する診断順序は絵本への遷移を確認し、通常入力で導入を進めてから地図案内を検査すること。08:11回の別診断では絵本をスキップして入口が起動することを確認済み。

## 2026-11-17 09:11点検回

- 開始09:11:32、終了 09:14:30 JST。開戦案内の部分点検は診断で中断。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。開始時の差分はDemoのテスト・両記録と既存の本記録。関係するゲーム・チュートリアルテストの差分と競合プロセスなし。ゲームコード変更なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v minimal。09:12:02起動・作成、PID 7420、上限600秒、09:13:04終了0。警告・エラー0。
- 同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null で実行。09:13:05起動・作成、PID 28976、上限300秒、09:13:31終了 -532462766。診断専用 war-guides-save.gz を使用。証拠は TutorialWarGuidesUi-{build,run}-0911.{stdout,stderr}.log と bin/TutorialWarGuidesUi/debug/war-guide-0.png（1920×1080）。
- 最後に描画できたのは地図案内済みの固定場面で予約の導入案内16。開戦対象案内の表示前に停止。次はTUT-0001のタイトル通常入力診断と、TUT-0002の現行フェーズを使う開戦案内診断から再開。残り5星は08:11・08:29回参照。実際のSTARTからモブ女、地図、開戦予約、陣営のターン終了、戦闘、ポケット・移動、最終目標・勲章・タイトル復帰までを同一セッションで通す操作は未確認。

### TUT-0002 開戦案内診断が統合前のフェーズを前提にする

- 診断修正済み、通し操作は検証待ち（09:15改善回）。2026-11-17 09:13:31 JST、上記HEAD・差分・解像度。上記DLLを実行すると、tests/TutorialWarGuidesUi/Program.cs:28 の固定場面で開戦インスペクターを開き、回転と地点選択の案内22が出る想定に対し Expected guide 22 で停止する。
- 診断は FinishDomestic と BeginMilitary で旧フェーズに移り、Ready が演出名を「軍事フェーズ」に設定する。現行の Game1.TutorialWar.cs は演出名が PlanningPhaseLabel と一致する場合に進み、Game1.CampaignPhases.cs の同ラベルは統合後の「内政フェーズ」。固定場面を現行の内政中の開戦予約経路に合わせて再検証する。実プレイでの案内欠落は未確認。

## 2026-11-17 09:15改善回

- 開始09:16:03、終了 09:21:49 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。08:49・09:11点検時からゲームと対象テストの差分がなかったため、その実行結果を修正前の再現証拠とした。変更は tests/TitleStartMenu/Program.cs、tests/TutorialWarGuidesUi/Program.cs と本記録。ゲームコード変更なし。
- TUT-0001。START→遊び方→プチステラで絵本に遷移することを確認し、通常入力ハンドラにスキップのクリックを送って地図へ進む検査に修正。初回再実行はこの経路を通過し、同じ絵本導入が入った［最初から］の旧期待値で停止。通常の開始と破損セーブ後の開始も絵本→人数選択の順に更新。最終実行は終了0。タイトルと星メニューの実描画は1920×1080／960×540、分離セーブからの再開もPASS。絵本の各ページの連続操作・実画面確認は未実施。
- TUT-0002。内政中に開戦インスペクターを開く固定場面に変更し、演出名は現行の PlanningPhaseLabel を参照。対象選択案内22、出撃選択案内20、出撃確定による予約、閉鎖アニメーション後のターン終了案内21、保存・再開後に案内が重複しないことを1920×1080の描画付き診断で確認。固定場面から操作処理を呼ぶ診断で、タイトルからの通常入力による連続操作は未確認。
- 各プロジェクトを dotnet build tests/<対象>/<対象>.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal でビルド。最終実行は TitleStartMenu が09:20:40 PID 28660→09:20:52、TutorialWarGuidesUi が09:18:53 PID 22836→09:20:08。いずれも上限600秒、終了0・警告/エラー0。
- ALSOFT_DRIVERS=null で同出力の bin/<対象>/debug/<対象>.dll を dotnet 実行。TitleStartMenu は09:20:52 PID 27796→09:21:18、TutorialWarGuidesUi は09:20:08 PID 6868→09:20:33。上限各300秒、終了0。証拠は TitleStartMenu-{build,run}-0920 と TutorialWarGuidesUi-{build,run}-0918 の標準出力・標準エラー、および start-menu-*.png と war-guide-*.png。最終版は両診断でセーブと勲章保存先を分離。
- 次回はタイトルからモブ女・地図、開戦予約、陣営のターン終了と戦闘まで通常入力だけで進める。ポケット・移動から最終目標、完了表示・勲章・タイトル復帰の連続操作も検証待ち。

## 2026-11-17 09:40点検回

- 開始09:40:33、終了 09:43:33 JST。地図診断で中断。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存差分はDemoの地星儀・CPU軍事、Demo両記録、直送便・チュートリアルの既存テスト、本記録。対象TutorialThreeUiの差分と競合プロセスなし。ゲーム・テストコード変更なし。
- dotnet build tests/TutorialThreeUi/TutorialThreeUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v minimal。09:40:54起動・作成、PID 31052、上限600秒、09:42:33終了0、警告・エラー0。
- ALSOFT_DRIVERS=null で同出力の bin/TutorialThreeUi/debug/TutorialThreeUi.dll --journey を dotnet 実行。09:42:34起動・作成、PID 10736、上限300秒、09:43:03終了 -532462766。モデル側の地図、拠点、旧・現行セーブ検査を通過。LoadContent 中の入口アサーションで停止し、今回は描画・実解像度未取得。証拠は TutorialThreeUi-{build,journey}-0940.{stdout,stderr}.log。
- TUT-0001に未解決の関連診断を追加。TitleStartMenuの修正は検証済み。TutorialThreeUiの63行目はプチステラ選択直後に地図が始まることを期待し、「Menu starts step three with player one as No.1 on globe」で停止。現行の星カードは絵本に移るため、通常入力で絵本を終えてから地図案内へ進む必要がある。同じ導入変更によるため重複採番なし。通常プレイの進行不能は未確認。
- 次回は同診断の入口を通常のタイトル入力と絵本終了に合わせ、地図案内から開戦の解放まで確認する。残り5星は08:11・08:29回参照。タイトルから開戦予約、ターン終了、戦闘、ポケット・移動、最終目標・勲章・タイトル復帰までの通し操作は未検証。

## 2026-11-17 09:45改善回

- TUT-0001のTutorialThreeUiを修正・検証済み（開始09:46:06、終了 09:48:54 JST）。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。修正前は同じゲーム・テスト差分での09:40点検で再現。変更は tests/TutorialThreeUi/Program.cs と本記録。ゲームコード変更なし。
- プチステラはタイトルのSTART・遊び方・星カード・絵本スキップを通常入力ハンドラへ送り、絵本から地球儀へ進むことを確認。旧ステップ3は既存のシナリオを直接初期化して回帰検証を維持。セーブ・勲章は検証出力先に分離。
- dotnet build tests/TutorialThreeUi/TutorialThreeUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。09:46:33起動・作成、PID 15388、上限600秒、09:46:46終了0、警告・エラー0。
- ALSOFT_DRIVERS=null で同出力の bin/TutorialThreeUi/debug/TutorialThreeUi.dll を dotnet 実行。--journey は09:46:46 PID 2824→09:47:13、旧ステップ3は09:47:13 PID 19176→09:47:44、画像保存用の --journey 再実行は09:47:58 PID 14892→09:48:26。上限各300秒、すべて終了0。証拠は TutorialThreeUi-*-0946 と TutorialThreeUi-journey-0948 の各ログ、tutorial-three-*.png（1920×1080）。
- モブ女の地図案内、展開・回転・スライド・辺部のクリック・組み立て、開戦とターン終了の解放、開戦UIの起動、分離セーブからの復帰を実描画付きで確認。地図課題は個別入力処理を使う診断。絵本の全ページ、タイトルから戦闘・最終目標・勲章・タイトル復帰までの通常操作は検証待ち。

## 2026-11-17 09:51点検回

- 開始09:52:04、終了 09:55:01 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は地星儀・CPU軍事と既存UIテスト・記録。本回は点検記録のみ変更、競合なし、新規採番なし。
- dotnet build tests/<対象>/<対象>.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --source artifacts/nuget-feed --source C:/Users/muzud/.nuget/packages --disable-build-servers -p:NuGetAudit=false -p:UseAppHost=false -v minimal。TutorialPlanetMedalsUiは09:52:25 PID 24300→09:53:21、TutorialJourneyProgressUiは09:53:47 PID 20268→09:54:02。上限各600秒、終了0・警告とエラー0。
- 同出力の各DLLを ALSOFT_DRIVERS=null で dotnet 実行。TutorialPlanetMedalsUiは09:53:21 PID 6488→09:53:47、TutorialJourneyProgressUiは09:54:02 PID 10792→09:54:35。上限各300秒、終了0。専用セーブ・勲章を使用。証拠は <対象>-{build,run}-0952.{stdout,stderr}.log と planet-medals-*.png・journey-progress-*.png。
- 固定場面の描画付き診断で、攻撃解決と当周回の条件、ポケットへのドラッグ、移動の解決、完了案内、セーブ再開を確認。別の固定場面で学習勲章と征服勲章の独立付与、終了からタイトルへの戻り、星メニューでの保持・消去を確認。モデルへ状態を設定する段階を含む。残る5星の表示・入口は08:11・08:29回参照。
- 次回は09:45回で到達した地図課題の解放から、通常入力で開戦予約・陣営のターン終了・戦闘への連続操作を検証する。絵本の全ページとタイトルから最終目標・勲章・タイトル復帰までの一続きの操作は未確認。

## 2026-11-17 09:55改善回

- 開始09:55:35、終了 09:55:50 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。直近点検の後半・勲章の診断は両方合格。TUT-0001/0002の診断修正は検証済み。新たな再現済み修正対象はなし。既存差分を保持し、コード変更・新規の検証起動なし。
- 次回点検は地図課題の解放後から通常入力で開戦予約→陣営ターン終了→戦闘をつなぐ。絵本全ページとタイトルから完了・勲章・タイトル復帰までの通し操作は引き続き検証待ち。

## 2026-11-17 10:10点検回

- 開始10:11:05、終了 10:12:20 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存差分は地星儀・CPU軍事、Demo両記録、直送便・チュートリアルのテスト、本記録。案内とボタンの描画コードに差分なし。競合、ゲーム・テストコード変更、新規起動なし。
- 開戦予約後の案内21を既存の実描画（1920×1080）と現行コードで照合。最後に確認できたのは予約1件・インスペクター閉鎖後の案内。TUT-0003を新規登録。続きは案内を閉じて通常入力で陣営ターン終了を押し、配送・出兵・戦闘への遷移。絵本全ページとタイトルから完了・勲章・タイトル復帰までの連続操作は未確認。残り5星は08:11・08:29回参照。

### TUT-0003 開戦予約後の案内が旧ターン終了ボタン名を表示

- 完了（10:14改善回）。2026-11-17 10:11 JST、上記HEAD・差分。プチステラで地図課題を済ませ、開戦予約で目標とロボットを選び、予約を維持したままインスペクターを閉じる。既存 TutorialWarGuidesUi のフレーム3で再現。
- 期待はモブ女の案内が左下の実ボタン名「この陣営のターン終了」を示すこと。実際の案内21は廃止された「軍事終了・ターン終了」と表示する。Game1.TutorialMessages.cs の案内21、Game1.MapOverlays.cs の二段描画「この陣営の」「ターン終了」、NASの tutorial-audit/bin/TutorialWarGuidesUi/debug/war-guide-3.png（1920×1080、09:15改善回）で一致しないことを確認。案内に従った実操作の進行不能は未確認。

## 2026-11-17 10:14改善回

- TUT-0003完了。開始10:14:34、終了 10:16:18 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。修正前の案内・描画コードは10:10点検と同じ。変更は FolderVerse/Game1.TutorialMessages.cs、tests/TutorialWarGuidesUi/Program.cs、本記録。
- 案内21のボタン名を画面の「この陣営のターン終了」に合わせた。既存UIテストで案内文を確認し、開戦予約を再開した固定場面でボタンの実矩形の中心へ通常入力ハンドラでクリックを送り、配送画面への遷移を検証した。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。10:15:00起動・作成 PID 28512、上限600秒、10:15:24終了0、警告・エラー0。
- ALSOFT_DRIVERS=null で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を分離セーブ・勲章で実行。10:15:24起動・作成 PID 23388、上限300秒、10:15:51終了0。war-guide-3.png（1920×1080）で新文言を目視し、war-guide-5.pngが配送遷移の証拠。ログは TutorialWarGuidesUi-{build,run}-1014.{stdout,stderr}.log。
- 次回は配送から出兵・戦闘以降の通常操作。タイトルから最終目標・勲章・タイトル復帰までの連続操作は検証待ち。

## 2026-11-17 10:30点検回

- 開始10:30:36、終了 10:31:57 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の関係差分は地星儀・CPU軍事、案内21、各UIテストと記録。競合なし。ゲームコード変更・新規採番なし。既存の tests/TutorialWarGuidesUi/Program.cs に配送から戦闘までの検査を追加。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。10:31:00起動・作成 PID 31036、上限600秒、10:31:12終了0、警告・エラー0。
- 同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null で dotnet 実行。10:31:12起動・作成 PID 11676、上限300秒、10:31:34終了0。分離セーブ・勲章。ログは TutorialWarGuidesUi-{build,run}-1031.{stdout,stderr}.log、画像は既存出力の war-guide-5.png（配送）・war-guide-6.png（戦闘、1920×1080）。
- 出撃予約済みの固定場面で「この陣営のターン終了」を通常入力ハンドラでクリックし、配送・出兵演出を進め、プレイヤーの進軍と戦闘画面への遷移を確認。次回はこの戦闘から戦後画面、ポケット・移動を通常入力でつなぐ。タイトルから完了・勲章・タイトル復帰までの単一セッションと絵本全ページの連続操作は未確認。残る5星は08:11・08:29回参照。

## 2026-11-17 10:35改善回

- 開始10:35:33、終了 10:35:45 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。直近の点検は戦闘画面への遷移まで合格し、新たな再現済み修正対象なし。既存差分を保持し、ゲーム・テストコード変更、検証の再起動なし。
- 次回点検は戦闘画面から戦後処理を経て、ポケット・移動へ通常入力で進む。タイトルから最終目標・勲章・タイトル復帰までの単一セッションは検証待ち。

## 2026-11-17 10:51点検回

- 開始10:51:35、終了 10:54:12 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。先行差分は地星儀・CPU軍事、案内21、既存UIテストと記録。競合・ゲームコード変更・新規採番なし。既存の tests/TutorialWarGuidesUi/Program.cs に戦闘後の検査を追加。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。初回10:52:01 PID 8604→10:52:13、最終10:53:04 PID 8180→10:53:17。上限各600秒、終了0、警告・エラー0。
- 同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null で実行。初回10:52:13 PID 28700→10:52:50、最終10:53:17 PID 24352→10:53:48。上限各300秒、終了0。分離セーブ・勲章。証拠は TutorialWarGuidesUi-{build,run}-1052/1053.{stdout,stderr}.log と war-guide-7.png（1920×1080）。
- 開戦予約済みの固定場面からターン終了・配送・出兵・戦闘に進み、通常ハンドラへのEnter入力で戦闘を進行。結果新聞、戦訓、開戦達成、地図復帰とポケット・移動の案内4の表示まで確認。最終実行では地図に戻ったらEnter送信を止めて案内を保持。
- 次は案内4からわたしの国、ポケットへのドラッグ、移動先と予約、陣営ターン終了まで通常入力で確認。タイトルから最終目標、完了・勲章・タイトル復帰までの単一セッション、絵本全ページの連続操作は未確認。残る5星は08:11・08:29回参照。

## 2026-11-17 10:56改善回

- 開始10:57:03、終了 10:57:16 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。10:51点検の戦闘・戦訓・ポケット案内への遷移は合格。再現済みの新たな修正対象なし。既存差分を保持し、ゲーム・テストコードの変更と検証の再起動はなし。
- 次回はポケット案内からわたしの国、ポケットへのドラッグ、移動先選択・予約、陣営ターン終了を通常入力で確認。絵本全ページとタイトルから最終目標・勲章・タイトル復帰までの通し操作は検証待ち。

## 2026-11-17 11:08点検回

- 開始11:08:41、終了 11:10:54 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の関係差分は地星儀・CPU軍事、案内21、UIテスト・記録。ゲームコード変更・競合なし。tests/TutorialWarGuidesUi/Program.cs にポケットの通常操作の検査を追加。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。11:09:23 PID 4512→11:09:36、上限600秒、終了0、警告・エラー0。
- 同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null で実行。11:09:37 PID 9520→11:10:07、上限300秒、終了0。分離セーブ・勲章。証拠は TutorialWarGuidesUi-{build,run}-1109.{stdout,stderr}.log と war-guide-7.png／war-guide-8.png（1920×1080）。
- 戦闘と戦訓後に案内4を閉じ、通常入力で［わたしの国］→［ポケット］を選択。実描画矩形の座標(1068,290)から(122,290)へ機体14をドラッグし、ポケットに入ったことと、戻った際の達成案内5を確認。次回は移動予約の入力から続ける。絵本全ページ、タイトルから最終目標・勲章・タイトル復帰までの通し操作は未検証。残る5星は08:11・08:29回参照。

### TUT-0004 ポケット案内の移動予約ボタン名が実表示と一致しない

- 完了（11:16改善回）。2026-11-17 11:10 JST、上記HEAD・差分。プチステラで最初の戦闘・戦訓を終えて地図へ戻ると案内4が表示される。既存 TutorialWarGuidesUi のフレーム7でも再現。
- 期待は［わたしの国］から［移動予約］を選ぶ案内。実際の案内は［移動］と表示。本回の war-guide-7.png（1920×1080）を目視し、Game1.TutorialMessages.cs の案内4と Game1.MoveReservationMarkers.cs の実ボタン「移動予約」を照合。移動の入力可否・予約確定は次回に確認する。

## 2026-11-17 11:16改善回

- TUT-0004完了。開始11:17:05、終了 11:20:18 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。修正前の関係文言は11:08点検と同じ。変更は FolderVerse/Game1.TutorialMessages.cs、tests/TutorialWarGuidesUi/Program.cs、本記録。
- ポケット・移動の案内4を実ボタンの［移動予約］に合わせた。既存UI診断で、戦闘・戦訓からポケットへのドラッグを経て、国のインスペクターの移動予約ボタン、地図上の地点4、［移動を予約］を通常入力でクリックし、予約1件を確認。1920×1080の war-guide-7.png で文言を目視。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。初回11:17:31 PID 19612→11:17:56、最終11:19:01 PID 30524→11:19:13。上限各600秒、終了0、警告・エラー0。
- ALSOFT_DRIVERS=null で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を分離セーブ・勲章で実行。初回11:17:56 PID 28168→11:18:42、終了 -532462766。診断が地星儀で押せる候補を見つけられなかったため、描画状態を更新し、必要なら通常の展開図ボタンを押してから候補を探すよう変更。最終11:19:13 PID 5336→11:19:47、終了0。上限各300秒、証拠は TutorialWarGuidesUi-{build,run}-1117/1119.{stdout,stderr}.log と war-guide-7/8.png。
- 次回は陣営ターン終了で移動予約を実行し、到着・勲章と最終目標を確認する。地星儀だけでの最初の候補が見つからなかった配置、タイトルから完了・勲章・復帰までの単一セッション、絵本全ページは未確認。

## 2026-11-17 11:28点検回

- 開始11:29:05、終了 11:30:34 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存差分は地星儀・CPU軍事、チュートリアル案内、各UIテストと記録。競合・ゲームコード変更・新規採番なし。既存の tests/TutorialWarGuidesUi/Program.cs に予約実行・到着・勲章の検査を追加。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。11:29:30 PID 4072→11:29:42、上限600秒、終了0、警告・エラー0。ALSOFT_DRIVERS=null で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を実行。11:29:42 PID 20400→11:30:10、上限300秒、終了0。
- 開戦予約の固定場面から戦闘・戦訓・ポケット操作・移動予約をつなぎ、通常入力で陣営ターン終了後、ポケット内の機体14を保持して地点5へ到着。地図上の移動達成案内と分離ストアのプチステラ学習勲章を確認。証拠は TutorialWarGuidesUi-{build,run}-1129.{stdout,stderr}.log と war-guide-8.png（1920×1080）。
- 次回は移動達成案内から最終目標の案内、征服・エンディング、タイトルの勲章表示まで確認。タイトルから地図課題を含めた単一セッション、絵本全ページ、世界征服を通常入力で達成する通し操作は未確認。残る5星は08:11・08:29回参照。

## 2026-11-17 11:34改善回

- 開始11:35:05、終了 11:35:18 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。直近の到着・学習勲章の検査は合格。新たな再現済み修正対象なし。既存差分を保持し、ゲーム・テストコードの変更や検証の再起動なし。
- 次回は移動達成案内から最終目標の案内、征服・エンディング、タイトルの勲章表示まで確認。タイトルからの単一セッションと絵本全ページは未検証。

## 2026-11-17 11:48点検回

- 開始11:49:05、終了 11:53:44 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存差分は地星儀・CPU軍事、チュートリアル案内、各UIテストと記録。競合、ゲームコード変更、新規採番なし。既存 TutorialWarGuidesUi に移動後の案内と終了操作の確認を追加。分離勲章は各実行の開始時に初期化。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。最終11:52:23 PID 20808→11:52:35、上限600秒、終了0、警告・エラー0。ALSOFT_DRIVERS=null で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を実行。最終11:52:35 PID 10580→11:53:12、上限300秒、終了0。
- 先行2回はシステムメニューのアサーションで終了 -532462766（11:49:56 PID 24560、11:51:16 PID 3952）。診断ログで帰還時の説得演出が入力を処理中と確認。通常更新で演出と閉鎖を待つよう診断を調整した。ログは TutorialWarGuidesUi-{build,run}-1149/1151/1152.{stdout,stderr}.log。
- 開戦予約の固定場面から通常入力で戦闘・戦訓・ポケット・移動を経て学習勲章を獲得。移動達成後に世界征服の案内17、続きからとセーブ枠の案内18・19を通過。システムメニューから手動エンディングに進み、通常ボタンでタイトルへ復帰。再度開いた［遊び方］の1920×1080 war-guide-8.png でプチステラの「修了」メダルと残り5星の準備中表示を目視。ストアを読み直して学習勲章の保持と征服勲章が付かないことを確認。
- 次回は実際の世界征服から自動エンディングと征服勲章を通常入力で確認する。世界征服は09:51回の固定場面診断のみ。タイトルから地図課題を含む単一セッション、絵本全ページも未確認。残り5星の入力は08:11回参照。

## 2026-11-17 11:56改善回

- 開始11:57:04、終了 11:57:20 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。直近の最終案内・手動エンディング・修了メダル保持は合格。説得演出を待つとシステムメニューを操作できたため、未解決の再現済み不具合なし。既存差分を保持し、ゲーム・テストコードの変更、検証再起動なし。
- 次回点検は世界征服から自動エンディング・征服勲章へ進む通常経路。絵本全ページ、タイトルから完了までの単一セッションも検証待ち。

## 2026-11-17 12:15改善回

- 開始12:15:39、終了 12:16:08 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。再現済みの未解決修正対象なし。今回の変更は本記録のみ。ゲーム・テストコードの既存差分を保持し、検証の重複起動なし。
- 12:09点検の残りを回収。tests/TutorialPlanetMedalsUi/Program.cs の診断差分は、全地点を自陣営に設定した固定場面で BeginIndustry を呼び、自動エンディングへ進むことを確認。描画後、タイトルに戻るボタンの矩形へ通常入力ハンドラで押下・解放を送り、分離ストアの学習・征服勲章を再読込して確認。
- dotnet build tests/TutorialPlanetMedalsUi/TutorialPlanetMedalsUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。12:10:07から12:10:20のログで成功、警告・エラー0。同出力の bin/TutorialPlanetMedalsUi/debug/TutorialPlanetMedalsUi.dll を ALSOFT_DRIVERS=null で実行。12:10:20から12:10:45のログで PASS、標準エラーなし。実行中に確認したPIDは20632。終了コードは回収できず、12:15:39に対象プロセスが残っていないことを確認。証拠は TutorialPlanetMedalsUi-{build,run}-1210.{stdout,stderr}.log と同DLL隣の planet-medals-1/2.png。
- 次回点検は通常入力で残りの領土を実際に征服するところから、自動エンディングと征服勲章までをつなぐ。絵本全ページとタイトルから完了までの単一セッションは引き続き検証待ち。

## 2026-11-17 12:28点検回

- 開始12:28:36、終了 12:30:46 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存のゲーム・UI診断差分を保持。変更は tests/TitleStartMenu/Program.cs の絵本点検分岐と本記録。ゲームコード変更・競合・新規採番なし。
- 実描画のタイトルから通常の入力ハンドラで［START］→［遊び方］→［プチステラ］→［絵本を開く］をクリック。宇宙ページでは演出完了前のクリックで移動せず、通常入力ループで計2秒を進めた後、送りボタンで地図案内開始を確認。固定プチステラの絵本2画面をスキップなしで通過。分離セーブ・勲章を使用。証拠は1920×1080の petitstella-book-0～3.png と TitleStartMenu-book-{build,run}-1228.{stdout,stderr}.log。
- dotnet build tests/TitleStartMenu/TitleStartMenu.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。12:29:47起動・作成 PID 17476、上限600秒、12:30:01終了0、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_BOOK_AUDIT=1 で同出力の bin/TitleStartMenu/debug/TitleStartMenu.dll を dotnet 実行。12:30:01起動・作成 PID 11556、上限300秒、12:30:22終了0、PASS。
- TUT-0001の絵本通常送りを確認。地図課題を含むタイトルから完了までの単一セッションは検証待ち。次回は通常の開戦・戦闘で領土を実際に征服し、自動エンディング・征服勲章へつなぐ。残る5星の入口は08:11・08:29回参照。

## 2026-11-17 12:35改善回

- 開始12:36:06、終了 12:36:18 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。12:28点検でTUT-0001の絵本2画面をスキップなしの通常入力で通過し、地図案内に遷移。再現済みの未修正項目なし。変更は本記録のみ、検証の再起動なし。
- 次回点検は実際の開戦・戦闘による領土獲得から自動エンディング・征服勲章へつなぐ。タイトルから完了までの単一セッションは検証待ち。

## 2026-11-17 12:48点検回

- 開始12:48:36、終了 12:50:35 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の地星儀・CPU軍事、チュートリアル文言、UI診断の差分を保持。今回の変更は tests/TutorialWarGuidesUi/Program.cs の領土所有ログと本記録。ゲームコード変更、競合、新規採番なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。12:49:22起動・作成 PID 25776、上限600秒、12:49:36終了0、警告・エラー0。ALSOFT_DRIVERS=null で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を dotnet 実行。12:49:36起動・作成 PID 24352、上限300秒、12:50:14終了0。
- 開戦予約の固定場面から陣営ターン終了、配送、戦闘、戦訓、地図復帰まで通常入力で進行。今回の1回の出撃では獲得地点0、自陣営2/11、世界征服フラグなし。ポケットと移動を経て地点10への到着、学習勲章、手動終了とタイトル復帰まで再確認。証拠は TutorialWarGuidesUi-ownership-{build,run}-1248.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 次回は出撃選択で実戦に勝って領土を獲得する条件を通常UIで試し、残る領土の征服から自動エンディング・征服勲章へつなぐ。タイトルから完了までの単一セッションは未確認。絵本通常送りは12:28回、残る5星は08:11・08:29回参照。

## 2026-11-17 12:54改善回

- 開始12:55:06、終了 12:55:21 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。12:48点検の出撃では領土を獲得しなかったが、戦闘・戦訓の後に通常操作で移動し、学習勲章とタイトル復帰まで到達。進行を妨げる再現済みの未修正項目はなし。変更は本記録のみ、検証の再起動なし。
- 次回は出撃戦力と対象を選んで実際の領土獲得を確認し、自動エンディング・征服勲章へつなぐ。タイトルから完了までの単一セッションは検証待ち。

## 2026-11-17 13:10点検回

- 開始13:10:57、終了 13:13:24 JST。後半の点検は中断。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の地星儀・CPU軍事、案内文と各UI診断の差分を保持。変更は tests/TutorialWarGuidesUi/Program.cs と本記録。ゲームコード変更・競合なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。13:11:27起動・作成 PID 848、上限600秒、13:11:40終了0、警告・エラー0。ALSOFT_DRIVERS=null で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を dotnet 実行。13:11:40起動・作成 PID 6460、上限300秒、13:12:28終了-532462766。証拠は TutorialWarGuidesUi-sortie-{build,run}-1310.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 出撃選択の入口は診断で開き、通常入力で格納庫の6体をクリックし確定。陣営ターン終了から戦闘・戦訓・地図復帰へ進み、新規領土は0、自陣営2/14。ポケットに機体45を入れ、地点0への移動を予約。移動先とポケット保持の検査は通ったが、地図での移動達成案内を要求する後続の検査で停止。今回の勲章・エンディングは未確認。
- 全画面状態と保留中の案内・演出がアサーション時に記録されておらず、ゲーム上の再現は未確定のため採番保留。次回は移動到着直後の画面・案内番号・内政演出を記録し、必要な通常操作を続けて案内6を再確認する。その後、勝利条件と世界征服からの自動終了、タイトルからの単一セッションを継続。絵本と残り5星は12:28回、08:11・08:29回参照。

## 2026-11-17 13:14改善回

- 開始13:15:06、終了 13:17:03 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。13:10点検の移動達成案内での停止を追試。変更は tests/TutorialWarGuidesUi/Program.cs の到着状態のログと本記録。ゲームコード変更・競合・新規採番なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。13:15:30起動・作成 PID 30156、上限600秒、13:16:01終了0、警告・エラー0。ALSOFT_DRIVERS=null で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を dotnet 実行。13:16:01要求、13:16:02作成 PID 164、上限300秒、13:16:44終了0。証拠は TutorialWarGuidesUi-move-{build,run}-1315.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 通常入力で地点8に機体25を保持して到着し、内政演出開始から2秒後に案内6が表示された。その後、学習勲章、手動エンディング、タイトル復帰まで確認。前回は自領2/14、今回は3/13で、元配置での停止は要再確認。次回は世界・配役・領土Seedを出力し、再発時に同条件で案内の保留要因を照合する。世界征服から自動終了、およびタイトルから完了までの単一セッションも未確認。

## 2026-11-17 13:30点検回

- 開始13:30:06、終了 13:32:00 JST。戦後処理で部分点検中断。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の地星儀・CPU軍事、案内文とUI診断の差分を保持。変更は tests/TutorialWarGuidesUi/Program.cs の診断用Seed指定・出力と本記録。ゲームコード変更・競合・新規採番なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。13:30:30起動・作成 PID 20296、上限600秒、13:30:43終了0、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0 で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を dotnet 実行。13:30:43起動・作成 PID 452、上限300秒、13:31:22終了-532462766。証拠は TutorialWarGuidesUi-seed-{build,run}-1330.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 世界1559595549・配役1755192844・領土1649316166。格納庫6体を通常クリックで確定し、陣営ターン終了、戦闘、戦訓を通過。最後の画面は戦後処理 Disposition。診断はEnterだけを送っており、通常の処遇ボタンの選択と演出の更新が未実装。Game1.Disposition.cs と Game1.DispositionScene.cs で処遇選択と演出終了後の地図・世界征服判定への分岐を照合した。この実行の領土増0、自陣営3/14、世界征服フラグなし。
- 次回は同じSeedで戦後処理の通常ボタンを押し、演出を進めて地図・移動達成案内まで確認する。13:10回の案内停止は別配置で未再現、自動終了・征服勲章・タイトルから完了までの単一セッションは未確認。

## 2026-11-17 13:35改善回

- 開始13:35:37、終了 13:39:08 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs と本記録。ゲームコード変更・競合なし。
- 13:30点検の戦後処理に、通常入力ハンドラで処遇ボタンをクリックし、通常の演出処理を進める診断を追加。同じSeed0で［手下にする］に相当する Recruit の矩形中心(565,895)が反応し、戦後処理から地図・ポケット案内へ復帰。機体67をポケットへ移し地点4の移動予約まで確認。領土増0、自陣営3/14。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。初回13:36:03 PID 15660→13:36:15、追試13:37:29 PID 31536→13:37:41。各上限600秒、終了0、警告・エラー0。同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0 で dotnet 実行。初回13:36:15 PID 28348→13:36:59、追試13:37:41 PID 2268→13:38:29。各上限300秒、終了-532462766。証拠は TutorialWarGuidesUi-disposition-{build,run}-1335/1337.{stdout,stderr}.log、1920×1080の war-guide-*.png。
- 次の陣営ターン終了で直ちにTransportへ入るという診断が両回で停止。追試クリック直前は地図、案内・説得・インスペクターなし。クリック後の画面・陣営・フェーズ、予約の状態が未取得のため、進行不能は要再確認、採番保留。次回はクリック直後の画面・フェーズと予約実行を照合する。案内6、征服・自動終了、タイトルから完了までの単一セッションはこの配置では未確認。

## 2026-11-17 13:52点検回

- 開始13:52:11、終了 13:54:32 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存の地星儀・CPU軍事、案内文とUI診断の差分を保持。今回の変更は tests/TutorialWarGuidesUi/Program.cs と本記録。ゲームコード変更、競合、新規採番なし。
- 13:35回と同じSeed0で診断を再実行。移動予約後の陣営ターン終了をクリックすると、地図のまま操作対象が配下の征服者2へ切り替わった。配下内政を通常更新で進めると、予約どおり機体67をポケットに保持して地点4に到着。案内6・学習勲章、手動エンディング、タイトル復帰を1920×1080の実描画付きで確認。13:35回の停止は診断が配送画面への即時遷移を要求したためと確認。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。13:53:18起動・作成 PID 3956、上限600秒、13:53:32終了0、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0 で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を dotnet 実行。13:53:32起動・作成 PID 10460、上限300秒、13:54:09終了0。証拠は TutorialWarGuidesUi-subordinate-{build,run}-1352.{stdout,stderr}.log と war-guide-*.png。
- 13:10回の別配置での到着後の案内停止は引き続き要再確認。次回は領土獲得から自動エンディング・征服勲章へ進む。タイトルから完了までの単一セッションは未確認。絵本と残り5星は12:28回、08:11・08:29回参照。

## 2026-11-17 13:55改善回

- 開始13:55:36、終了 13:55:50 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。13:35回の診断停止は13:52点検の同一Seedで解消を確認済み。配下内政を経て移動が実行され、案内6・学習勲章・タイトル復帰まで到達。今回の変更は本記録のみ、既存差分を保持し、検証の再起動なし。
- 13:10回の別配置はSeed未記録で、移動達成案内の停止は要再確認。次回は記録可能な配置で領土獲得と自動エンディング・征服勲章への遷移を検証する。タイトルから完了までの単一セッションは未確認。

## 2026-11-17 14:10点検回

- 開始14:10:37、終了 14:13:14 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。今回の変更は tests/TutorialWarGuidesUi/Program.cs と本記録。ゲームコード変更、競合、新規採番なし。
- 戦闘の描画開始時点では戦闘結果が既に反映されていたため、診断の比較基準を陣営ターン終了クリック前へ移した。Seed0では通常入力による出撃確定、ターン終了、戦闘と戦後処理を経て地点5を獲得し、自陣営3/14。従来記録のSeed0「領土増0」は比較開始時点が遅かったため訂正。Seed1では獲得0、自陣営2/17。両方とも移動達成案内、学習勲章、手動終了、タイトル復帰まで確認。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。14:11:12起動・作成 PID 4596、上限600秒、14:11:25終了0、警告・エラー0。同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0/1 で dotnet 実行。Seed0は14:11:25 PID 4512→14:12:05、Seed1は14:12:05 PID 4932→14:12:49。上限各300秒、両方終了0。証拠は TutorialWarGuidesUi-capture-{build,seed0,seed1}-1410.{stdout,stderr}.log と1920×1080の war-guide-*.png（最終Seed1）。
- 次回は地点5を獲得したSeed0の戦闘後から勝利条件を満たすまで通常入力を継続し、自動終了・征服勲章につなぐ。開始時の開戦案内は診断の固定場面。残り5星の入口は08:11・08:29回、絵本は12:28回参照。タイトルから完了までの単一セッション、13:10回の別配置での案内停止は未確認。

## 2026-11-17 14:16改善回

- 開始14:16:39、終了 14:16:53 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。14:10点検のSeed0/1は両方終了0。地点5の獲得を確認し、以前の集計時点の誤りは点検記録で訂正済み。再現済みの未修正項目なし。変更は本記録のみ、検証の再起動なし。
- 次回はSeed0の地点5獲得後から、勝利条件・自動エンディング・征服勲章への通常操作を検証する。タイトルから完了までの単一セッションと13:10回の別配置の案内停止は引き続き要確認。

## 2026-11-17 14:29点検回

- 開始14:29:07、終了 14:30:46 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存のDemo、案内文、UIテストの差分を保持。変更は tests/TutorialWarGuidesUi/Program.cs の順位計測と本記録。ゲームコード変更・競合・新規採番なし。
- Seed0で戦闘と戦後処理を経て地点5を獲得。直後の暫定順位は自陣営2/6面、勝利資格あり、自動終了フラグなし。地点所有の3/14とは別の集計。移動達成案内・学習勲章・手動終了・タイトル復帰も通常入力で再確認。描画1920×1080。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。14:29:30起動・作成 PID 5652、上限600秒、14:29:43終了0、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0 で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を dotnet 実行。14:29:43起動・作成 PID 25252、上限300秒、14:30:25終了0。証拠は TutorialWarGuidesUi-ranking-{build,run}-1429.{stdout,stderr}.log と war-guide-*.png。
- 次回は地点5獲得後の2/6面から追加ターンを通常入力で進め、全6面と自動終了・征服勲章を確認する。タイトルから完了までの単一セッション、13:10回の別配置での案内停止は未確認。絵本と残り5星は12:28回、08:11・08:29回参照。

## 2026-11-17 14:34改善回

- 開始14:34:39、終了 14:35:02 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。14:29点検は終了0。地点5獲得後の2/6面では自動終了条件を満たしておらず、再現済みの未修正項目なし。変更は本記録のみ、検証の再起動なし。
- 次回はSeed0で2/6面から追加ターンを進め、世界征服・自動終了・征服勲章まで確認する。タイトルから完了までの単一セッションと13:10回の案内停止の元配置は未確認。

## 2026-11-17 14:49点検回

- 開始14:49:38、終了 14:51:29 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。既存のDemo・案内文・UIテスト差分を保持。変更は tests/TutorialWarGuidesUi/Program.cs の追加ターン検査と本記録。ゲームコード変更・競合・新規採番なし。
- Seed0で地点5を獲得し、配下内政、ポケット移動、学習勲章と終盤案内を通過。追加の開戦予約を入れず通常の陣営ターン終了をクリックし、ターン2から3へ進んで地図の内政に復帰。戦闘直後は自陣営2/6面、追加ターン後は1/6面。手動終了とタイトル復帰も確認。自動終了条件には到達せず。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。14:50:07起動・作成 PID 2956、上限600秒、14:50:21終了0、警告・エラー0。ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1 で同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を dotnet 実行。14:50:21起動・作成 PID 15092、上限300秒、14:51:07終了0。証拠は TutorialWarGuidesUi-extra-{build,run}-1449.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 次回は学習勲章取得後に通常UIで追加の開戦予約を入れる。全6面、自動終了と征服勲章、タイトルから完了までの単一セッション、13:10回の別配置は未確認。絵本と残り5星については12:28回と08:11・08:29回を参照。

## 2026-11-17 14:55改善回

- 開始14:55:37、終了 14:55:53 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。14:49点検は終了0。学習完了後にターン2から3へ進んで地図に復帰でき、今回の修正対象となる再現済みの問題なし。変更は本記録のみ、検証の再起動なし。
- 次回は学習勲章取得後に通常UIで追加の開戦予約を入れ、全6面、自動終了、征服勲章の順に確認する。タイトルから完了までの単一セッションと13:10回の別配置は引き続き未確認。

## 2026-11-17 15:11点検回

- 開始15:11:40、終了 15:15:31 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の追加開戦診断と本記録。既存差分を保持。ゲームコード変更・競合・新規採番なし。
- Seed0で学習勲章取得後に開戦ボタン、通常地図の地点1 (1137,517)、出撃可能な機体カードを通常入力でクリックし、追加開戦予約を確認。インスペクターを閉じて陣営ターンを終了し、ターン2から3へ進んで地図へ復帰。初回診断は機体カード0固定などの前提で予約アサーション停止。再試行では有効な目標・出撃可能な表示カードを選び、描画更新を待った。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。初回15:12:11要求・15:12:12作成 PID 4040→15:12:26。最終15:14:00要求・15:14:01作成 PID 8664→15:14:14。上限各600秒、両方終了0・警告とエラー0。
- 同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 で dotnet 実行。初回15:12:26要求・15:12:28作成 PID 10380→15:13:18、終了-532462766。最終15:14:14要求・15:14:15作成 PID 5828→15:15:04、終了0。上限各300秒。証拠は TutorialWarGuidesUi-second-war-{build,run}-1511/1514.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 追加ターン終了後は1/6面。次回は追加予約の機体IDと戦闘参加・所有変化を照合し、戦力を確保して残る面を攻略する。全6面、自動終了、征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。絵本と残り5星は12:28回および08:11・08:29回参照。

## 2026-11-17 15:16改善回

- 開始15:16:08、終了 15:16:24 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。15:11点検の予約アサーションは、出撃可能な表示カードを選び直した同一Seedの診断で解消済み。追加開戦予約と次ターンの地図復帰を確認済み。再現済みの未修正項目はなく、変更は本記録のみ。検証の再起動なし。
- 次回は地点1への追加予約を起点に、予約機体の戦闘参加と領土変化を追う。世界征服・自動エンディング・征服勲章、タイトルからの単一セッション、13:10回の別配置は要確認。

## 2026-11-17 15:30点検回

- 開始15:30:44、終了 15:32:36 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の追加出撃ログと本記録。既存差分保持、ゲームコード変更・競合・新規採番なし。
- Seed0で学習勲章の取得後、通常入力で配下の征服者2、出撃元6、目標1の機体26を予約。進軍は1件、戦闘フレームに機体26、取消なし。地点1は征服者1のままで、自陣営1/6面。ターン3の内政に復帰し、手動終了とタイトル復帰を確認。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。15:31:15起動・作成 PID 9312、上限600秒、15:31:28終了0、警告1・エラー0。警告CS8632の注釈は実行後に除去、除去後の再ビルド未実施。
- 同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 で dotnet 実行。15:31:28起動・作成 PID 31268、上限300秒、15:32:10終了0。証拠は TutorialWarGuidesUi-participants-{build,run}-1530.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 次回は複数機体を選択した追加出撃から続ける。全6面と自動終了・征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。絵本・残り5星は12:28回と08:11・08:29回参照。

## 2026-11-17 15:36改善回

- 開始15:36:11、終了 15:36:24 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。15:30点検で追加予約の機体26が予定どおり地点1の戦闘に参加し、取消なし。領土は獲得せず。再現済みの未修正項目なし。変更は本記録のみ、検証の再起動なし。
- 次回は複数機体を選択して追加出撃を点検する。世界征服・自動終了・征服勲章、タイトルから完了までの単一セッション、13:10回の案内停止の元配置は未確認。

## 2026-11-17 15:51点検回

- 開始15:51:08、終了 15:53:42 JST。後半の点検は中断。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の複数機体選択と本記録。既存差分を保持、ゲームコード変更・競合なし。
- Seed0・1920×1080で学習勲章取得後、通常の出撃カードで配下の征服者2・出撃元6の機体26、27、28を地点1へ予約。進軍1件、全3体の戦闘参加と取消なしを確認。地点1は征服者1のまま。ターン3の地図へ復帰した後、システムメニューが開くという後続診断で停止。クリック時の演出・案内状態は未取得。ゲーム上の進行不能は未確定、採番保留。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。初回15:51:29 PID 892→15:51:51、診断の変数名重複で終了1。最終15:52:07 PID 24756→15:52:19、終了0・警告とエラー0。上限各600秒。
- 同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 で dotnet 実行。15:52:19起動・作成 PID 4568、上限300秒、15:53:17終了-532462766。証拠は TutorialWarGuidesUi-multi-{build,run}-1551/1552.{stdout,stderr}.log と war-guide-*.png。
- 次回は追加ターン後の地図で説得や内政演出、システムメニューの入力可否を照合する。全6面、自動終了、征服勲章、タイトルからの単一セッション、13:10回の別配置は引き続き未確認。

## 2026-11-17 15:56改善回

- 開始15:56:40、終了 15:58:20 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs と本記録。ゲームコード変更・競合なし。
- 15:51点検の同じSeed0・機体26/27/28の戦闘後、ターン3の地図で別の説得演出が入力を処理していた。診断に通常更新で演出と閉鎖を待つ処理を追加。演出後の通常クリックでシステムメニューが開き、手動エンディング、タイトル復帰、学習勲章保持まで1920×1080で確認。15:51の停止は診断修正済み。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。15:57:01起動・作成 PID 29736、上限600秒、15:57:13終了0、警告・エラー0。同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 で dotnet 実行。15:57:13起動・作成 PID 26976、上限300秒、15:57:57終了0。証拠は TutorialWarGuidesUi-postturn-{build,run}-1556.{stdout,stderr}.log と war-guide-*.png。
- 次回は世界征服に向けて領土を増やす出撃条件を点検する。全6面、自動終了、征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。

## 2026-11-17 16:10点検回

- 開始16:10:38、終了 16:12:14 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の出撃元選択と本記録。既存差分を保持し、ゲームコード変更・競合・新規採番なし。
- Seed0で地点1への追加出撃元を、同じ画面で出撃可能な機体が最多の行から選んだ。この画面では前回と同じ地点6の機体26、27、28が上限で、通常クリックから進軍1件、全機の戦闘参加と取消なし。地点1の所有は変わらず、ターン3の地図・手動終了・タイトル復帰まで確認。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。16:10:56起動・作成 PID 30212、上限600秒、16:11:09終了0、警告・エラー0。同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 で dotnet 実行。16:11:09起動・作成 PID 4040、上限300秒、16:11:51終了0。証拠は TutorialWarGuidesUi-reinforced-{build,run}-1610.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 次回は同一目標への別出撃元か、別の目標を通常UIで選ぶ。全6面、自動終了、征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。絵本と残り5星は12:28回と08:11・08:29回参照。

## 2026-11-17 16:17改善回

- 開始16:17:09、終了 16:17:22 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。16:10点検は終了0。追加予約の3体は全機が戦闘に参加し、再現済みの未修正項目なし。変更は本記録のみ。検証の再起動なし。
- 次回は別出撃元か別目標を通常UIで選ぶ。自動終了・征服勲章、タイトルからの単一セッション、13:10回の別配置での案内停止は未確認。

## 2026-11-17 16:31点検回

- 開始16:31:13、終了 16:32:47 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の追加開戦目標の探索順と本記録。既存差分を保持、ゲームコード変更・競合・新規採番なし。
- Seed0の学習勲章取得後、追加の開戦先を地点13に変更。通常地図で論理(1382,412)をクリックし、征服者2・地点6の機体26、27、28を予約。進軍1件、全機の戦闘参加、取消なし。戦闘後の地点13の所有者は-1、自陣営1/6面。ターン3の地図・手動終了・タイトル復帰まで確認。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。16:31:29起動・作成 PID 30292、上限600秒、16:31:43終了0、警告とエラー0。同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 で dotnet 実行。16:31:43起動・作成 PID 24344、上限300秒、16:32:24終了0。証拠は TutorialWarGuidesUi-other-target-{build,run}-1631.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 次回は複数出撃元か自軍の別配置を選び、面の支配変動を確認する。全6面、自動終了・征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。絵本と残り5星は12:28回、08:11・08:29回参照。

## 2026-11-17 16:35改善回

- 開始16:35:40、終了 16:35:53 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。16:31点検は終了0。地点13の追加予約は3体とも戦闘に参加し、取消なし。戦闘後も進行でき、再現済みの未修正項目なし。変更は本記録のみ。検証の再起動なし。
- 次回は複数出撃元か自軍の別配置で面の支配変動を追う。世界征服・自動終了・征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。

## 2026-11-17 16:48点検回

- 開始16:48:40、終了 16:50:26 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の通常UIでの出撃元の走査と本記録。既存差分保持、ゲームコード変更・競合・新規採番なし。
- Seed0の地点13で同じ画面に表示された全出撃元から選択可能な機体を通常クリックした。この場面では出撃元1件・3体のみ。地点6からの機体26/27/28が各1回戦闘に参加、取消なし。戦闘後の地点13の所有者は-1、自陣営1/6面。ターン3の地図、手動終了とタイトル復帰まで確認。複数出撃元の同時参加は未確認。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。16:49:06起動・作成 PID 26384、上限600秒、16:49:18終了0、警告・エラー0。同出力の bin/TutorialWarGuidesUi/debug/TutorialWarGuidesUi.dll を ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=0、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 で dotnet 実行。16:49:19起動・作成 PID 31064、上限300秒、16:50:03終了0。証拠は TutorialWarGuidesUi-multisource-{build,run}-1648.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 次回は別Seedか別ページの出撃元を検査する。世界征服・自動終了・征服勲章、タイトルからの単一セッション、13:10回の別配置は引き続き未確認。絵本と残り5星は12:28回、08:11・08:29回参照。

## 2026-11-17 16:53改善回

- 開始16:53:39、終了 16:53:53 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。16:48点検は終了0。地点13の出撃元は1か所で、予約した3体の戦闘参加を確認済み。再現済みの未修正項目なし。変更は本記録のみ。検証の再起動なし。
- 次回は別Seedか別ページで複数の出撃元を検査する。世界征服・自動終了・征服勲章、タイトルからの単一セッション、13:10回の案内停止の元配置は引き続き未確認。

## 2026-11-17 17:10点検回

- 開始17:10:12、終了 17:11:29 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。今回の変更は本記録のみ。既存のDemo・チュートリアル差分を保持。ゲームコード変更・競合・新規採番なし。
- 16:48回の既存NAS出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=1、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll を実行。17:10:25起動・作成 PID 21964、上限300秒、17:11:05終了0。証拠は TutorialWarGuidesUi-multisource-seed1-run-1710.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 通常UIによる追加出撃は自軍0、出撃元11、地点9、機体91と92。進軍1件、両機の戦闘参加、取消なし、終了時の地点9所有者0を確認。出撃元は1件で複数出撃元の同時参加は未確認。自陣営1/6面のままターン3の地図へ進み、学習勲章と手動終了・タイトル復帰を確認。
- 次回はSeed1で地点9の戦闘前後の所有を比較し、複数の出撃元か次の攻撃先を通常入力で検査する。世界征服・自動終了・征服勲章、タイトルからの単一セッション、13:10回の案内停止の元条件は未確認。残り5星と絵本は08:11・08:29回、12:28回を参照。

## 2026-11-17 17:15改善回

- 開始17:15:39、終了 17:15:55 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。17:10点検は終了0。地点9への追加予約を実行し、機体2体の戦闘参加と次ターンへの遷移を確認。再現済みの未修正項目なし。変更は本記録のみ、検証の再起動なし。
- 次回はSeed1の地点9で戦闘前後の所有を比較し、追加の出撃元か攻撃先を検査する。世界征服・自動終了・征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。

## 2026-11-17 17:29点検回

- 開始17:29、終了 17:31:07 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存のチュートリアル案内と UI テスト。変更は本記録のみ。新規採番なし。
- 既存NAS出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=2、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll を実行。17:29:58起動・作成 PID 30040、上限300秒、17:30:43終了0。証拠は TutorialWarGuidesUi-seed2-run-172958.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 戦闘後にポケット操作から機体49を地点13へ移動し、学習勲章を取得。追加出撃では自軍0・出撃元12から地点9へ9体を予約し、参加9体、進軍1件、取消なし、戦闘後の所有者0。ターン3の地図、手動終了、タイトル復帰と学習勲章の保存を確認。
- 次回は同じ地点9の戦闘前後の所有を比較し、別の出撃元か次の攻撃先を検査する。複数出撃元、世界征服・自動終了・征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。絵本と残り5星は12:28回、08:11・08:29回参照。

## 2026-11-17 17:36:55 改善回

- 確認時刻 17:36:55 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。17:29点検は終了0。追加出撃に予約した9体すべての戦闘参加、ターン3への進行、手動終了・タイトル復帰、学習勲章の保存を確認済み。再現済みの未修正項目なし。変更は本記録のみ。
- 次回は地点9の戦闘前後の所有を比較する。複数出撃元、世界征服による自動終了・征服勲章、タイトルからの単一セッション、13:10回の別配置は未確認。

## 2026-11-17 17:50点検回

- 開始17:50 JST、終了 17:51:46 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の tests/TitleStartMenu/Program.cs。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/TitleStartMenu/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_BOOK_AUDIT=1 として dotnet TitleStartMenu.dll を実行。17:51:03起動・作成 PID 10800、上限300秒、17:51:28終了0。証拠は同出力の TitleStartMenu-run-175103.{stdout,stderr}.log。
- 1920×1080の描画と通常クリック処理で、タイトルの START → 遊び方 → プチステラ → 絵本の表紙と宇宙ページ → 地図を確認。今回の実行は地図まで。残り5星は08:11・08:29回、戦闘・移動・手動終了は17:29回を参照。
- 次回は地点9の戦闘前後の所有を比較する。世界征服による自動終了・征服勲章、複数出撃元、13:10回の別配置、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 17:53改善回

- 確認終了 17:53:52 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。17:50点検は終了0。STARTから絵本を経て地図に入るまでの範囲で、再現済みの未修正項目なし。変更は本記録のみ。
- 次回は地点9の戦闘前後の所有を比較。世界征服による自動終了と征服勲章、複数出撃元、13:10回の別配置、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 18:10点検回

- 開始18:10 JST、終了 18:12:17 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の戦闘前の所有者ログと本記録。既存差分を保持し、新規採番なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。18:10:36起動・作成 PID 16656、上限600秒、18:10:52終了0、警告・エラー0。同出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=2、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll。18:11:06起動・作成 PID 19528、上限300秒、18:11:56終了0。証拠は TutorialWarGuidesUi-owner-build-181036、TutorialWarGuidesUi-owner-run-181106 の両ログと1920×1080の war-guide-*.png。
- 地点9の所有者が2から0へ変わった。出撃元12から予約した9体がすべて参加し、進軍1件・取消なし。支配面は1/6のままターン3に進み、学習勲章の保存と手動終了・タイトル復帰まで確認。
- 次回は追加の攻撃先または複数出撃元で面の支配変化を追う。世界征服による自動終了、征服勲章、13:10回の別配置、タイトルから完了までの単一セッションは未確認。タイトル入口は17:50回、残り5星は08:11・08:29回参照。

## 2026-11-17 18:16改善回

- 確認終了 18:16:24 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。18:10点検は終了0。地点9の所有移転、予約9体の戦闘参加、次ターン進行を確認済み。再現済みの未修正項目はなく、変更は本記録のみ。
- 次回は追加の攻撃先または複数出撃元で面の支配変化を追う。13:10回の案内停止は元Seedが未記録のため要再確認。世界征服による自動終了、征服勲章、タイトルからの単一セッションは未確認。

## 2026-11-17 18:30点検回

- 開始18:30 JST、終了 18:31:57 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存のチュートリアル案内と tests/TutorialWarGuidesUi/Program.cs。変更は本記録のみ。新規採番なし。
- 既存NAS出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=3、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll。18:30:57起動・作成 PID 5068、上限300秒、18:31:38終了0。証拠は TutorialWarGuidesUi-seed3-run-183057.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 移動予約した機体1が地点6に到着。追加出撃は出撃元0から地点11へ5体。予約した5体すべてを戦闘参加一覧で確認。取消なし、地点11の所有者は2から0へ移転。1/6面のままターン3へ進み、手動終了・タイトル復帰と学習勲章の保存を確認。
- 次回は同じ地点の追加2体の所属か、複数出撃元と支配面の変化を追う。世界征服による自動終了、征服勲章、13:10回の別配置、タイトルから完了までの単一セッションは未確認。タイトル入口は17:50回、残り5星は08:11・08:29回参照。

## 2026-11-17 18:33改善回

- 確認終了 18:33:55 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。18:30点検は終了0。地点11の所有移転、予約した5体の戦闘参加、学習勲章の保存とタイトル復帰を確認済み。再現済みの未修正項目なし。変更は本記録のみ。
- 次回は地点11の戦闘に参加した残り2体の所属か、複数出撃元と支配面の変化を照合する。世界征服による自動終了、征服勲章、13:10回の別配置、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 18:49点検回

- 開始18:49:13、終了 18:51:03 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の所属・役割ログと本記録。ゲームコード変更・新規採番なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。18:49:46起動 PID 4992、上限600秒、18:49:59終了0、警告・エラー0。同出力の bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=3、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1、dotnet TutorialWarGuidesUi.dll。18:49:59起動 PID 24952、上限300秒、18:50:43終了0。証拠は TutorialWarGuidesUi-roles-build-184946 と TutorialWarGuidesUi-roles-seed3-184959 の両ログ、1920×1080の war-guide-*.png。
- 地点11の戦闘で予約した5体と追加2体を確認。追加の機体83・84は自軍0の Soldier。出撃元0からの進軍は1件、取消なし、所有者は2から0へ変化。ターン3、手動終了・タイトル復帰と学習勲章の保存まで確認。
- 次回は追加2体の戦闘参加元を照合する。世界征服による自動終了、征服勲章、13:10回の別配置、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 18:54改善回

- 確認終了 18:54:27 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。18:49点検は終了0。地点11の追加機体83・84は自軍兵士と確認。参加元は未確認で、再現済みの未修正項目なし。変更は本記録のみ。
- 次回は追加2体の参加元を戦闘開始前の配置と照合する。世界征服による自動終了、征服勲章、タイトルから完了までの単一セッション、13:10回の別配置は未確認。

## 2026-11-17 19:12点検回

- 開始19:12:14、終了 19:14:12 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の戦闘前格納庫ログと本記録。新規採番なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。19:12:50起動 PID 12180、上限600秒、19:13:02終了0、警告・エラー0。同出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=3、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll。19:13:02起動 PID 28228、上限300秒、19:13:47終了0。証拠は TutorialWarGuidesUi-garrison-build-191250、TutorialWarGuidesUi-garrison-seed3-191302 の両ログ、1920×1080の war-guide-*.png。
- 機体83・84は開戦処理前から地点11の格納庫にいた自軍機。戦闘参加一覧ではこの2体と予約した5体が一致し、進軍1件、取消なし。地点11の獲得と手動終了、タイトル復帰、学習勲章の保存まで再確認。
- 次回は複数出撃元や別の攻撃先を通常操作で試し、面の支配変化を追う。世界征服による自動終了、征服勲章、タイトルから完了までの単一セッション、13:10回の別配置は未確認。

## 2026-11-17 19:14改善回

- 確認終了 19:14:54 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。19:12点検は終了0。地点11の追加2体は開戦前から現地の格納庫にいた自軍機と確認。再現済みの未修正項目はなく、変更は本記録のみ。
- 次回は複数出撃元か別の攻撃先で支配面の変化を追う。13:10回の案内停止は元Seed未記録のため要再確認。世界征服による自動終了と征服勲章、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 19:28点検回

- 開始19:28:46、終了 19:30:07 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の案内と tests/TutorialWarGuidesUi/Program.cs。変更は本記録のみ。新規採番なし。
- 既存NAS出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=4、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll。19:29:06起動・作成 PID 21832、上限300秒、19:29:47終了0。証拠は TutorialWarGuidesUi-seed4-run-192906.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 通常の出撃選択で地点9へ出撃元2・12からそれぞれ8体・6体を予約。各出撃元からの進軍は1件、取消なし。予約した14体と現地の自軍機89・90が戦闘に参加。地点9の所有者は4から無所属へ変化し、支配面は1/6のままターン3へ進行。学習勲章の保存、手動終了・タイトル復帰を確認。
- 次回は通常の攻撃で支配面を増やせる配置を追う。世界征服による自動終了、征服勲章、タイトルから完了までの単一セッション、13:10回の別配置は未確認。

## 2026-11-17 19:36改善回

- 確認終了 19:36:26 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。19:28点検は終了0。地点9への2出撃元の進軍と予約14体の戦闘参加、ターン3への進行を確認。再現済みの未修正項目なし。変更は本記録のみ。
- 次回は通常の攻撃で支配面が増える配置を点検する。世界征服による自動終了・征服勲章、タイトルから完了までの単一セッション、13:10回の別配置は未確認。

## 2026-11-17 19:52点検回

- 開始19:52:12、終了 19:53:42 JST。部分点検中断。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の案内と tests/TutorialWarGuidesUi/Program.cs。変更は本記録のみ。
- 既存NAS出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=5、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll。19:52:29起動・作成 PID 20080、上限300秒、19:53:15終了 -532462766。証拠は TutorialWarGuidesUi-seed5-run-195229.{stdout,stderr}.log と1920×1080の既存描画出力。
- 地点2への移動と学習勲章、出撃元5・2から地点13へ予約した全7体の戦闘参加、地点13の所有者5→0、ターン3と支配面1/6を確認。
- TUT-0005（未解決、UI診断の操作不足）。上記Seedで追加戦闘後に処分待ちの Disposition が表示された。期待は通常の処分ボタンを操作して地図へ復帰すること。診断は Disposition でクリックを送らず、地図復帰のアサーションで停止した。根拠は標準出力の Additional turn: 2->3, screen=Disposition と Program.cs:320。次回は通常入力で処分を選ぶ分岐を診断に加えて追試する。ゲームの進行不能は未確認。タイトル復帰、世界征服の自動終了と征服勲章も今回未確認。

## 2026-11-17 19:55改善回

- 完了 19:57:39 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。TUT-0005 は完了。変更は tests/TutorialWarGuidesUi/Program.cs と本記録。19:52回に同じSeedで処分画面での診断停止を再現済み。
- 診断が処分画面の選択肢を描画し、通常の「電池を抜く」に対応する RemoveBattery の矩形中心 (1365,895) を通常入力でクリックする。保留件数が1→0となり、処分演出後にターン3の地図へ復帰。手動終了、タイトルへの復帰、学習勲章の保存まで確認。ゲームコードの変更なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。19:56:20作成 PID 30672、上限600秒、19:56:33終了0、警告・エラー0。同出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=5、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 を設定し dotnet TutorialWarGuidesUi.dll。19:56:33作成 PID 31276、上限300秒、19:57:16終了0。証拠は TutorialWarGuidesUi-disposition-build-195620 と TutorialWarGuidesUi-disposition-seed5-195633 の両ログ、1920×1080の描画。
- 今回の処分は通常UIへ診断からマウス入力を送り、選んだのは RemoveBattery のみ。物理入力、他の処分選択肢、世界征服による自動終了・征服勲章、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 20:11点検回

- 開始20:11:15、終了 20:12:31 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の案内と TUT-0005 の診断コード。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=6、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll。20:11:32起動・作成 PID 28292、上限300秒、20:12:11終了0。証拠は TutorialWarGuidesUi-seed6-201132.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 機体21が地点11へ到着し、学習勲章を取得。出撃元4と11から予約した7体すべての戦闘参加、各進軍1件、取消なし。地点11の所有者は3から4、支配面0/6。処分画面の通常クリックで保留1→0、ターン3の地図、手動終了、タイトル復帰、勲章保存まで確認。
- 次回は支配面の増加を確認できる攻撃先を選ぶ。世界征服の自動終了・征服勲章、タイトルから完了までの単一セッション、13:10回の別配置、処分の他選択肢は未確認。

## 2026-11-17 20:17改善回

- 確認終了 20:17:26 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。20:11点検は終了0。別配置でも TUT-0005 の処分操作後に地図へ戻り、手動終了・タイトル復帰・学習勲章の保存を確認済み。再現済みの未修正項目なし。変更は本記録のみ。
- 次回は支配面が増える攻撃先を点検する。世界征服による自動終了・征服勲章、タイトルから完了までの単一セッション、13:10回の別配置と処分の他の選択肢は未確認。

## 2026-11-17 20:30点検回

- 開始20:30:42、終了 20:32:08 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の案内と TUT-0005 の診断コード。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=7、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll。20:31:05起動・作成 PID 14572、上限300秒、20:31:47終了0。証拠は TutorialWarGuidesUi-seed7-203105.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- 初回攻撃で地点10を獲得。機体14をポケットに入れて地点6へ移動し学習勲章を取得し、続けて地点12への通常出撃で機体14の参加を確認。進軍1件、取消なし、地点12の所有者3は維持。ターン3の地図、手動終了、タイトル復帰、学習勲章の保存を確認。
- 今回は支配面の増加なし。次回は地図上の攻撃先と支配面の判定を照合する。世界征服の自動終了・征服勲章、タイトルから完了までの単一セッション、13:10回の別配置、処分の他選択肢は未確認。

## 2026-11-17 20:36改善回

- 確認終了 20:36:57 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。20:30点検は終了0。領土獲得、ポケット移動、追加出撃、タイトル復帰と学習勲章の保存を確認。再現済みの未修正項目なし。変更は本記録のみ。
- 次回は地図上の攻撃先と支配面の判定を照合する。世界征服の自動終了・征服勲章、タイトルから完了までの単一セッション、13:10回の別配置、処分の他選択肢は未確認。

## 2026-11-17 20:50点検回

- 開始20:50:15、終了 20:51:31 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。関係差分は既存の案内と TUT-0005 の診断コード。変更は本記録のみ、新規採番なし。
- 既存NAS出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=8、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1 として dotnet TutorialWarGuidesUi.dll。20:50:34要求・20:50:35作成 PID 19988、上限300秒、20:51:09終了0。証拠は TutorialWarGuidesUi-seed8-205034.{stdout,stderr}.log と1920×1080の war-guide-*.png。
- ポケットの機体25が地点14へ到着し、学習勲章を取得。同地点への追加出撃は進軍1件、取消なし。機体25と現地自軍機89・90の戦闘参加を確認、地点所有者は5→3。処分操作後にターン3の地図へ復帰し、手動終了・タイトル復帰・学習勲章保存まで確認。
- 支配面は1/6から0/6。ConquerorRelations.ProvisionalRanking は、その面に地点があり全地点の所有者が同陣営の配下を含む範囲に入るときに加算する。次回は面内で不足している地点を採取して攻撃先を選ぶ。世界征服の自動終了・征服勲章、タイトルから完了までの単一セッション、13:10回の別配置と処分の他選択肢は未確認。

## 2026-11-17 20:55改善回

- 確認終了 20:55:59 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。20:50点検は終了0。処分後の地図復帰と学習勲章の保存を確認済み。再現済みの未修正項目なし。変更は本記録のみ。
- 次回は面内で不足している地点を確認する。世界征服の自動終了と征服勲章、タイトルから完了までの単一セッション、13:10回の別配置と処分の他選択肢は未確認。

## 2026-11-17 21:11点検回

- 開始21:11:45、終了 21:13:29 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs に任意の候補順とログを追加、および本記録。新規採番なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。21:12:21起動 PID 10880、上限600秒、21:12:34終了0、警告・エラー0。同出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=5、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1、FOLDERVERSE_TUTORIAL_PRIORITIZE_CELL=1 として dotnet TutorialWarGuidesUi.dll。21:12:34起動 PID 22736、上限300秒、21:13:07終了0。証拠は TutorialWarGuidesUi-cell-build-211221 と TutorialWarGuidesUi-cell-seed5-211234 の両ログ、1920×1080の war-guide-*.png。
- 残り1地点の面にある地点13を通常クリックで攻撃し、予約した7体の戦闘参加と地点所有者5→0を確認。処分を経て地図へ戻った後の支配面は1/6で、追加攻撃の前後で総数の増加は確認できなかった。学習勲章の保存、手動終了・タイトル復帰まで確認。
- 次回は総数に加えて面ごとの支配を攻撃前後で記録する。世界征服の自動終了・征服勲章、タイトルから完了までの単一セッション、13:10回の別配置と処分の他選択肢は未確認。

## 2026-11-17 21:16改善回

- 確認終了 21:16:58 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。21:11点検は終了0。地点13の獲得と処分後の地図復帰を確認済み。支配面の総数は1/6で、面ごとの攻撃前後の内訳は未確認。再現済みの未修正項目はなく、変更は本記録のみ。
- 次回は攻撃前後で支配している面と各地点の所有者を照合する。世界征服による自動終了・征服勲章とタイトルから完了までの単一セッションは未確認。

## 2026-11-17 21:31点検回

- 開始21:31:45、終了 21:33:27 JST。部分点検完了。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の面別所有ログと本記録のみ。新規採番なし。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。21:32:13作成 PID 25620、上限600秒、21:32:26終了0、警告・エラー0。同出力 bin/TutorialWarGuidesUi/debug で ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=5、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1、FOLDERVERSE_TUTORIAL_PRIORITIZE_CELL=1 として dotnet TutorialWarGuidesUi.dll。21:32:26作成 PID 26184、上限300秒、21:33:01終了0。証拠は TutorialWarGuidesUi-cell-owners-build-213213 と TutorialWarGuidesUi-cell-owners-seed5-213226 の両ログ、1920×1080の描画。
- 追加出撃の直前に完全支配面は0。地点13を獲得し、面5の地点13・14がともに自軍0となって完全支配は1/6に増加。前回比較した1/6はそれより前の初回戦闘直後の値。処分後の地図、手動終了、タイトル復帰と学習勲章の保存を確認。
- 次回は支配面を増やした後の通常ターンを追う。世界征服による自動終了、征服勲章、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 21:34改善回

- 確認終了 21:34:27 JST。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。21:31点検は終了0。地点13の獲得で完全支配面が0から1に増加し、地図へ復帰。再現済みの未修正項目なし。変更は本記録のみ。
- 次回は面5を支配した後の通常ターンを追う。世界征服による自動終了、征服勲章、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 21:51点検回

- 開始21:51:14、終了 21:53:19 JST。部分点検中断。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の追加ターン反復オプションと本記録。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。21:51:52作成 PID 4256、上限600秒、21:52:05終了0、警告・エラー0。同出力 bin/TutorialWarGuidesUi/debug の dotnet TutorialWarGuidesUi.dll。ALSOFT_DRIVERS=null、FOLDERVERSE_TUTORIAL_WAR_SEED=5、FOLDERVERSE_TUTORIAL_EXTRA_TURN=1、FOLDERVERSE_TUTORIAL_EXTRA_WAR=1、FOLDERVERSE_TUTORIAL_PRIORITIZE_CELL=1、FOLDERVERSE_TUTORIAL_REPEAT_TURN=1。21:52:05作成 PID 5068、上限300秒、21:52:53終了 -532462766。両ログは TutorialWarGuidesUi-repeat-build-215152 と TutorialWarGuidesUi-repeat-seed5-215205。描画は1920×1080。
- 面5の獲得、処分とターン3の地図復帰まで通過。次の攻撃では通常クリックで地点8を選べたが、「表示カードをすべて予約」のアサーションで停止。選択対象0件とクリック後の予約不一致の双方を同じ条件で判定しており、原因と製品側の不具合は未確認、採番保留。
- 次回は地点8の出撃元、表示カード数、押下位置、予約前後を採取する。ターン4、世界征服による自動終了、征服勲章、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 21:57改善回

- 開始21:57:14、終了 21:59:20 JST。再現と切り分けまで。HEAD c2a8113c61cf724993429c4f4c62db87a4185d4f。変更は tests/TutorialWarGuidesUi/Program.cs の出撃カード診断ログと本記録。ゲームコード変更なし。採番保留。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。21:57:46 PID 2776、上限600秒、21:57:59終了0。21:51回と同じ環境変数で既存出力の dotnet TutorialWarGuidesUi.dll。21:57:59 PID 28536、上限300秒、21:58:42終了 -532462766。証拠は TutorialWarGuidesUi-picker-build-215746 と TutorialWarGuidesUi-picker-seed5-215759 のログ。1920×1080。
- 地点8で出撃元3行、診断上の選択可能候補は2・7・1体。最初の有効範囲内のクリック (122,352) から予約0件。途中で矩形のXが負になるため、クリック中にインスペクター状態が変わった。原因は未確定。次回は描画対象が出撃一覧か撤退画面か、最初のクリックの前後の画面・パネルとガイド状態、入力の分岐を採取する。ターン4以降は未確認。

## 2026-11-17 22:10点検回

- 開始22:10:45、終了 22:12:48 JST。部分点検中断。HEAD 0a14fa16c243eee52032c9a7a1e1840c28cca7ae、開始時Git差分なし。変更は tests/TutorialWarGuidesUi/Program.cs の入力状態ログと本記録。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。22:11:14 PID 11684→22:11:41終了0、警告・エラー0。同出力で21:51回と同じSeed5・追加出撃・反復設定で dotnet TutorialWarGuidesUi.dll。22:11:41 PID 30688→22:12:25終了 -532462766。上限は600秒と300秒。証拠は TutorialWarGuidesUi-input-state-build-221114、TutorialWarGuidesUi-input-state-seed5-221141 の両ログ、1920×1080の描画。
- TUT-0006（未解決、診断の操作順）。面5獲得とターン3の地図に復帰後、開戦から地点8を選び、8更新待って出撃カードをクリック。期待は有効な出撃画面から予約すること。実際は初回カードの押下前に説得が開き、移動モードは閉じて選択地点が-1になっていた。カード座標 (305,432) の押下で説得が閉じ、予約は全て0。撤退画面・案内は非表示。診断が説得中にも古い出撃行をクリックすると再現する。ゲームの進行不能は未確認。
- 次回は説得の操作と閉鎖を通常UIで先に済ませ、開戦・地点の再選択から予約を追う。ターン4、自動終了・征服勲章、タイトルから完了までの単一セッションは未確認。

## 2026-11-17 TUT-0006 改善回

- 完了 22:19:50 JST。HEAD 0a14fa16c243eee52032c9a7a1e1840c28cca7ae。TUT-0006 は完了。変更は tests/TutorialWarGuidesUi/Program.cs と本記録。診断は次ターンの地図を先に描画し、内政の開始演出・説得・インスペクター閉鎖を通常更新で待ってから開戦操作に入る。
- dotnet build tests/TutorialWarGuidesUi/TutorialWarGuidesUi.csproj --artifacts-path Z:/muzudho-with-ai-agent/FolderVerse/tmp/delete-after-2026-11-17/tutorial-audit --no-restore --disable-build-servers -p:UseAppHost=false -v minimal。最初の待機のみの試行は22:17:14ビルド終了0、PID 30264の診断は22:17:59終了 -532462766。描画を先に実行する修正後は22:18:37 PID 2416→22:18:51終了0、警告・エラー0。出力 bin/TutorialWarGuidesUi/debug で22:10回と同じSeed5・追加出撃・反復設定、ALSOFT_DRIVERS=null にて dotnet TutorialWarGuidesUi.dll を実行。22:18:51 PID 17500→22:19:26終了0。上限600秒／300秒。ログは TutorialWarGuidesUi-settle-draw-build-221837 と TutorialWarGuidesUi-settle-draw-seed5-221851。
- 1920×1080の通常UI入力で地点8へ3か所から計10体を予約し、全機の戦闘参加と取消なしを確認。地点8の獲得、ターン4の地図復帰、手動終了、タイトル復帰、学習勲章の保存まで通過。次回は支配面を増やす後続ターンから続ける。世界征服の自動終了と征服勲章、タイトルから完了までの単一セッション、物理入力は未確認。
