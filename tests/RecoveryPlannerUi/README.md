# RecoveryPlannerUi

新しい廃品回収の８画面を1920×1080のRenderTargetへ描画する隔離UI回帰。実行中のゲームやユーザー保存を読み書きせず、テスト用世界と画面外の専用ウィンドウを使う。

`GameDll`と`RuntimeDir`を指定してNASへ隔離ビルドし、SDL2.dllをテスト実行フォルダーへ置く。環境変数は`GAME_RUNTIME`（今回の実行素材）、`OLD_RUNTIME`（コンパイル済みContentを含む実行環境）、`PROBE_FONT`（今回生成したContent/UiFont.xnbの親）。出力フォルダーを引数に実行する。

実際の入力処理で完全体選択→条件→カタログ→地図→確認→出発予約→状況→詳細まで進み、ポケットの貸出紙と移動禁止判定も確認する。画像とログはNASのarchives/diagnosticsへ保存する。