# RecoveryPlannerUi

廃品回収の13画面を1920×1080のRenderTargetへ描画する隔離UI回帰。実行中のゲームやユーザー保存を読み書きせず、テスト用世界と画面外の専用ウィンドウを使う。

`GameDll`と`RuntimeDir`を指定してNASへ隔離ビルドし、`runtimes/win-x64/native/SDL2.dll`をテスト実行フォルダーへ置く。環境変数は`GAME_RUNTIME`（今回の実行素材）、`OLD_RUNTIME`（コンパイル済みContentを含む実行環境）、`PROBE_FONT`（Content/UiFont.xnbの親）。画像出力フォルダーを引数に実行する。

状況一覧→地点の展開図→格納庫→吹き出し内の質問→形→素材→到達可否→汚損条件→ターゲットの展開図→カタログ→確認→出発予約→状況詳細を検証する。地点とターゲットは実際の地図クリックで選択する。予約済みロボットの再選択防止、最大６班の選択制限、複数予約で品物が重複しないことも確認する。

2026-11-22の検証出力：`\\TAKAHASHI-NAS\Share\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-23\recovery-{flow,ui,model}\`。期限は日本時間2026-11-23まで（翌日から削除候補）。
