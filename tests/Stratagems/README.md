# 計略カード回帰

Materials新規world1/cast2/placement3を元に、隔離した所有/兵力配置を作る。３種の地理マッチ、10件を超える候補、既存守備維持、予約の遅延適用、保存復元、個別取消、手動操作競合、全予約の事前検証と内政終了確定を確認する。

`PublishedGame` に対象FolderVerse.dllとMonoGame.Framework.dllのある実行ディレクトリーを指定できる。`--artifacts-path` はNAS上の作業別期限付きフォルダーへ。実行先DLLを明示コピーしSHA-256一致を確認する。`FOLDERVERSE_PROBE_CONTENT_BASE` は実行素材ディレクトリー、任意の `STRATAGEM_FIXTURE` はUI試験用の診断保存出力先。

2026-11-19モデル63確認通過。勝利やCPUの採用頻度を測る試験ではない。実ゲーム・元セーブを使用しない。操作・仕様と試験範囲は docs/dev/stratagem-cards.md を参照。

UI隔離ハーネスは Ui.cs / Ui.csproj（GameDll、RuntimeDirを指定）。STRATAGEM_FIXTUREにモデル出力、STRATAGEM_ART_BASEにゲームのContentがあるディレクトリー、STRATAGEM_FONTにContent/UiFont.xnbを含むコンパイル済みフォントの親ディレクトリー、STRATAGEM_OUTPUTにNAS出力先を指定する。既定は簡易LoadContentで実UI関数を呼ぶ。選択/予約/取消/ページ送り/手動復帰に加え、閉じる・Escapeの予約保持、手動守備との競合後に実ターン終了ボタンを押して理由付きカード復帰・部分適用なしを確認する。初期版27確認と描画13枚はNAS stratagem-interface/ui-final.txtおよびui-*.png。OSマウスの通し入力・実ゲーム全体を検証するものではない。

`STRATAGEM_FULL_FLOW=1` は通常の `base.LoadContent` と明示セーブ無効を使う別経路。`STRATAGEM_FONT` は全コンパイル済みContentと実行素材のあるゲーム実行ディレクトリーにする。モデルfixtureを読み、カード予約→実ターン終了ボタン→配送後進軍→戦闘再生/結果→研究/産業・必要なら征服Ending継続→世界復帰を確認する。結果の画像は `STRATAGEM_OUTPUT/full-flow-summary.png`。通常ルートとは別に実行し、各実行に有限の上限を設ける。

固定fixtureが征服条件を満たした場合は正規のEnding分岐を通る。世界復帰後の処遇、タイトル、手下/CPU待機、自然入口、全Seed、OS実入力を確認する試験ではない。検証HEAD・コマンド・素材ルート・PID・結果と制約は docs/ai-agent/tutorial-audit.md の2026-11-20記録を参照。
