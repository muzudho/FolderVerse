# ゲームオーバー後の観戦

DisplayBreakerUiと同じGameDll/RuntimeDirと環境変数を渡す隔離UIプローブ。出力先を引数で指定する。複数人・処遇待ち・電池除去・自動開始／更新・一時停止／再開・電池復帰・元CPUのみの表示を検証し、Game Overラベルを描画保存する。元セーブや実ゲームには接続しない。

COUNTER_RUNTIMEには今回ビルドの出力先を指定する。counterにはUiFontを流用せず、製品と同じFonts.Counter（ToyCounterFont）を実ロードし、３種のラベルの全文字があることを確認する。
