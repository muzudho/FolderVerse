# 計略カード回帰

Materials新規world1/cast2/placement3を元に、隔離した所有/兵力配置を作る。３種の地理マッチ、10件を超える候補、既存守備維持、予約の遅延適用、保存復元、個別取消、手動操作競合、全予約の事前検証と内政終了確定を確認する。

`PublishedGame` に対象FolderVerse.dllとMonoGame.Framework.dllのある実行ディレクトリーを指定できる。`--artifacts-path` はNAS上の作業別期限付きフォルダーへ。実行先DLLを明示コピーしSHA-256一致を確認する。`FOLDERVERSE_PROBE_CONTENT_BASE` は実行素材ディレクトリー、任意の `STRATAGEM_FIXTURE` はUI試験用の診断保存出力先。

2026-11-19モデル63確認通過。勝利やCPUの採用頻度を測る試験ではない。実ゲーム・元セーブを使用しない。操作・仕様と試験範囲は docs/dev/stratagem-cards.md を参照。

UI隔離ハーネスは Ui.cs / Ui.csproj（GameDll、RuntimeDirを指定）。STRATAGEM_FIXTUREにモデル出力、STRATAGEM_ART_BASEにゲームのContentがあるディレクトリー、STRATAGEM_FONTにContent/UiFont.xnbを含むコンパイル済みフォントの親ディレクトリー、STRATAGEM_OUTPUTにNAS出力先を指定する。簡易LoadContentで実UI関数を呼び、実際のターン終了は押さない。最終27確認と描画13枚はNAS stratagem-interface/ui-final.txtおよびui-*.png。OSマウスの通し入力・実ゲーム全体を検証するものではない。
