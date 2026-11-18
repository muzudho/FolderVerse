# 地点グレードと中継物流の回帰

`SiteDevelopment.csproj` はモデル45項目、`Render.csproj` は24モデルの地上・海上別角度と実展開図・地星儀、`Bench.csproj` は３SEEDの移転あり／なし比較用。

全て `GameDll` に検証対象DLL、`RuntimeDir` にMonoGame.Framework.dllの親を渡し、`--artifacts-path` はNAS作業別ディレクトリーへ。実行前に対象FolderVerse.dllを出力へ明示コピーしSHA-256を照合する。Render/Benchは `FOLDERVERSE_PROBE_CONTENT_BASE` に実行素材Contentの親（末尾区切り付き）を指定する。

- モデル：引数なし。旧容量のMaterials保存コピーを `Z:/muzudho-with-ai-agent/FolderVerse/archives/site-development-regression/pre-relay-materials-628374-turn400.fvs` から読む。Legacyパーツの互換試験ではない。NAS不可なら元資料は消さず試験を保留。
- 描画：引数はNAS出力先。そこに現行UiFont.spritefontから単独生成した `font-content/UiFont.xnb` を用意し、実行先へSDL2.dllを置く。Game1.LoadContentは呼ばず、元セーブ・音声・統計を初期化しない。PNG４枚を目視。
- CPU比較：引数 `SEED on|off 保存先.fvs`。新規Materials、20CPU、cast1476417906、placement492471852、fortune1544938375、配置25/25、全原料有効。上限60ターン（途中征服なら停止）。offは判断後に全施設移転予約を消す診断条件で、提案数と実施数を分ける。60未決着を非収束と判定しない。再開する場合も世界開始から累計400以内。

2026-11-18の詳細・元出力はNAS tmp/delete-after-2026-11-18/site-development。UIのみの最終説明変更前と後でDLLが異なるので記録を確認する。
