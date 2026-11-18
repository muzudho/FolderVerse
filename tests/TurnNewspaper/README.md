# 新聞フェーズの回帰

`GameDll`に検証対象FolderVerse.dll、`RuntimeDir`にMonoGame.Framework.dllのあるディレクトリーを指定して、NASへ`--artifacts-path`で隔離ビルドする。実行先へ対象DLLを明示コピーしSHA256を照合、SDL2.dllも用意する。

`FOLDERVERSE_PROBE_CONTENT_BASE`は実行素材Contentの親（末尾区切り付き）。引数はNAS出力先。出力先の`font-content/UiFont.xnb`は最新のUiFont.spritefontから生成しておく。

Game1.LoadContentを呼ばず、元セーブ・統計・実ゲームに触れない診断。25項目と1920×1080の配達途中・紙面・４国のポストのPNGを出力する。最後は人間の８秒無操作進行・操作リセット、外交の返事保持、捕虜画面への復帰、自動８秒を確認。画像は目視する。初回の追加戦闘診断でコンストラクター引数を省いたコンパイル失敗を修正し、再ビルド成功後の25項目を採用した。

紙面編集は１社１トピックで、すべての候補記事が毎号載るわけではない。実戦の長期間記事追跡、全ウィンドウサイズ、すべての国名の可読性を保証する試験ではない。
