# ProgressiveProduction

段階消費、途中変更、不足から再開、混成原料の工程単位同時消費、出荷待ち中の二重消費防止、有限個数と繰り返し、トリガー・循環拒否・依存元消失、保存復元を検証する。

出力はAGENTS.mdに従いNASの作業別artifacts-pathへ。`PublishedGame`で固定DLLを指定可能。任意の`OLD_SAVE`を設定すると400ターンの旧Materials保存の読み取り互換も確認する。元保存の書き換えや追加進行は行わない。初期世界から在庫・容量・レーンを調整する診断fixtureで、自然なCPUプレイの収束判定ではない。
