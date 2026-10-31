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
