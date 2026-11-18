# 遠隔製造品の到着後描画

GameDllとRuntimeDirを指定し --artifacts-path でNASへビルド。対象FolderVerse.dllを出力へコピーしてSHA-256照合し、SDL2.dllも用意。FOLDERVERSE_PROBE_CONTENT_BASEは実行素材Contentの親（末尾区切り付き）。引数はNAS出力先。

Game1.LoadContentを呼ばず、元セーブ・統計・音声を初期化しない。Materials診断世界のQueueProduction→保存→入荷→Transport引継ぎで到着した混成隊長頭胴一体型を読む。実DrawHangarBay/DrawHangarRobotを呼び、脚素材・一体型指定・役職・胴素材を１つずつ変えた対照が機体領域内の画素にだけ影響することを確認する。arrival-0.pngとarrival-4.pngを目視する。製造レシピ・コストの合法性の試験ではない。
