# 外交フェーズの隔離UI回帰

Game1.LoadContentを呼ばずに生成した２人の人間プレイヤー間の実取引を使う。元セーブ・元ログは読まず、保存・統計出力を無効化する。

NASの作業ディレクトリーをoutput引数とし、そこに最新UiFont.spritefontから生成したfont-content/UiFont.xnbを用意する。ビルドは --artifacts-path でNASへ。GameDllに検証対象FolderVerse.dll、RuntimeDirにMonoGame.Framework.dllのあるディレクトリーを渡す。実行先に対象DLLを明示コピーしSHA-256一致を確認する。FOLDERVERSE_PROBE_CONTENT_BASEは実行素材のContentがある親ディレクトリー（末尾区切り付き）。SDL2.dllも実行先に用意する。

確認：世界征服状況中は外交を待機、終了後開始、30秒未満の返事待ち、操作リセット、期限で辞退・荷物不変、結果８秒待ち・内政へ一度だけ遷移、手動承諾、外交後の保存再実行、自動操縦が外交画面に滞留しない。offer.png/results.pngを1920×1080で保存し目視する。

2026-11-18実行結果：14項目通過、ビルド警告・エラー０。詳細はNAS tmp/delete-after-2026-11-18/diplomacy-timer。検証用出力の期限は日本時間2026-11-18最終保存日、翌日以降は使用確認後に削除候補。
