# ロボットアート実装の再開点

2026年11月10日、利用者が動的国旗PP頭の試作を承認し、本実装を定期タスクで依頼。実装タスクIDは `folderverse`、30分間隔。このチャットの最新指示を併読する。

## 保存先

- 大容量の原画・重要資料：`Z:\muzudho-with-ai-agent\FolderVerse\art-source\robots\`
- UNC：`\\TAKAHASHI-NAS\Share\muzudho-with-ai-agent\FolderVerse`
- 一時出力：同NASの `tmp/delete-after-YYYY-MM-DD/robot-art-implementation/`。日付は日本時間の最終保存日。
- 通常の制限環境ではZが見えないが、権限付き確認では読み書き可能。実行権限を確認して利用する。
- 原画とカタログ85ファイル、56,852,140 bytesをNASへコピーし、各SHA-256一致を確認済み。移行時点の写しなので以後の更新はNASを優先。既存ローカル原画は参照・カタログ・テストを移行/検証するまで保持する。
- ゲームに必要なソース・実行素材はプロジェクトに置く。制作原画、検証ビルド、ログの大容量出力はNASへ。元セーブと公開物に触らない。

## 実装の方針

共通素地/着色マスク/曲面座標/反射/紋章の２Ｄ合成。既存の全模様・紋章・旗形をゲームの国旗データから反映する。試作HTMLは２模様のみなので、そのまま全機能対応としない。承認原画と派生の一覧は制作仕様を参照。色付きぜんまいは未決定。ゲームルール・素材能力/コストを勝手に決めない。

格納庫は３列２段６枠表示、容量は既存値を維持。12〜最大60枠にページ等で対応し、選択/ドラッグ/予約ヒットテストを整合させる。戦場との素体共通化・役職・汚損も段階実装。現在ユーザーがインスペクター配置モックアップを依頼中。仮配置を承認済みと扱わず最新指示を確認する。

本体表示への接続は未実施。国旗合成の基盤を追加済み（下記11月10日23時41分節）。次は明示マスク付き実行素材と６枠の導線を調査する。CPUタスクとのソース/ビルド競合、個人チャット原本の編集を避ける。コミット/プッシュ/リリースはしない。検証と目視確認まで完了したら報告して実装タスクを削除する。

## インスペクター仮配置（同日追加）

利用者の追加依頼でA左のみ/B右のみ/C左右半分ずつ/D左右＋中央地図の４画像をNAS art-source/robots/inspector-layout-study-v001へ保存し原寸カタログ作成。未承認。Dは２地点と経路を同時に見る配送向け案。生成画像の地形/建物/文字/素材取り違え/ぜんまいを製品仕様として扱わない。B操作バーのはみ出し等は実装時に解消が必要。画像のレイアウト承認を待って確定する。

実装タスクfolderverse ACTIVE、30分間隔。既存CPUタスクもNAS保存先と描画実装との競合回避を公式updateで更新済み。

## 2026年11月10日23時41分・明示マスク合成の基盤

現行Game1.Robots.csは矩形素体とパーツごとのCountryPatternSecondaryで描き、紋章は代表１部位、予約は靴。戦場は別のBattleUniform経路もある。今回これらの既存表示は置換せず、RobotFlagComposite.csにCPU側の２Ｄ合成基盤を追加した。入力は共通素地Color配列と、画素ごとのUV・着色Coverage・Diffuse・Reflection。灰色判定で顔を推測して塗るHTML方式を本実装へ持ち込まず、制作済みマップだけが塗装領域を指定する。元画像とアルファを保持し、無着色部位・反射は国旗から独立する。

国旗は既存CountryFlagArt.Pixel/Containsを使用し、全５要素を共有。三角形外の領域は共通素地を残す。素材別のDiffuse/Reflectionを渡せるが、９素材の実マップ・素材トーン変換・ステッカー専用UVはまだ作成していない。旗形の縦横比を含むUVの配置は実行素材制作時に決める。320組合せ（４旗形×５模様×16紋章値）を全画素で既存国旗に照合。顔保護・完全反射・透明画素・原画不変も成功。テストの初回はSetWorld欠落でActiveCount０となり失敗、検証世界を初期化して再実行し成功。Release警告０/エラー０。固定採用DLL C9ED5F123D7B4B0C319039F2A00BD696B824727CB3B98AF6AA06CCF1078C9D64を参照した隔離テストで、本体ビルドや実ゲーム実行はしていない。

テストソース tests/RobotFlagComposite、出力 NAS tmp/delete-after-2026-11-14/robot-art-implementation/composite-foundation/。実画像の目視・GPUテクスチャ接続・キャッシュ・６枠ページ/選択/ドラッグ・全素材/兵隊/隊長/クイーン/汚損/ぜんまいは未対応。比較表切り出しを完成素材とせず、次はPP頭の共通素地と明示マスク/UV/反射を用意し、この合成基盤で実画像２国比較を検証する。未決定の能力/コスト/ぜんまい色・仮レイアウトは確定しない。公開物/実ゲーム/セーブ/CPU判断未変更、コミット/プッシュ/リリースなし。タスク継続。
## 2026年11月11日０時11分・PP頭専用の明示サーフェスマップ

承認済み試作のpp-head-neutral.pngを目視確認（1280角、クリームの顔が左寄り、右面に光沢）。RobotHeadSurface.Polypropyleneに、この１枚専用の正規化した丸角矩形による顔/縁の保護領域、曲面UV、素地の明暗から得るDiffuse/Reflectionを追加。マスクは灰色/肌色判定ではなく幾何形状で顔を守る。顔が灰色でも保護される点をテストした。他の頭や９素材へ同じマップを流用しない。原画を編集/上書きしていない。

RobotFlagCompositeは36×24の国旗を１合成につき一度だけラスタライズし、大きな画像の画素ごとの紋章検索を除去。国旗320組合せの既存全画素照合と顔/反射/透明度/原画不変は引き続き成功。新しいPP頭マップについて顔内部４座標のCoverage０、右面Coverage255、白い反射/透明部分の保護を追加して成功。Release警告０/エラー０、固定DLL参照の隔離テストのみ。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/pp-head-map/ にbin/obj/results。次は実PNGを読み込みこのマップで合成した画像を出力し目視確認する。現状の旗全体UVだけでは主紋章が顔保護領域に重なるため、承認済みの右下ステッカー用UV/白い輪郭を別レイヤーで追加する必要がある。旗の縦横比も含めて確認してからゲーム描画へ接続する。GPUキャッシュと素材別トーン/６枠/役職/汚損/ぜんまいは未対応。まだ実画像の合成結果やゲーム表示を確認したとは扱わない。CPU/実ゲーム/元セーブ/公開物は未変更、コミット等なし。
## 2026年11月11日０時41分・PP頭の実PNG合成とステッカーを目視確認

RobotFlagComposite.Stickerにゲーム共通CountryEmblemArtの32角タイルを渡す独立合成を追加。ベージュ台座なし、１タイル幅の白い縁、右下の矩形内に猫/ハートを表示。透明画素と輪郭アルファを維持する。テストでは固定DLLのinternal Tilesをreflectionで取得したが、本体接続時は同じアセンブリ内から直接渡す。テスト用PNG入出力Render.ps1を追加し、NAS承認原画1280角をRGBAへ読み出し、実際のC#合成結果２国をPNGに保存した。Windows依存のPNG codecライブラリ参照２件不足を補って実行成功。

初回画像の目視で旗全体の低解像度枠/巨大紋章が頭表面へ重複し、縞も階段状になることを発見。fieldOnly指定を追加し、頭は共有CountryStripeSecondary/CountryMapColorによる地模様、紋章は独立ステッカーに分離した。既存の旗全体の合成はデフォルトで維持。再出力した斜め縞＋猫、市松＋ハートの両PNGを目視し、顔の表情/クリーム色、光沢、右下ステッカー、滑らかな斜め縞を確認した。元画像の無着色マスク内と全画素アルファを完全一致assert。320組合せの旗全体照合/幾何マップ試験も成功。Release警告０/エラー０。

出力NAS tmp/delete-after-2026-11-15/robot-art-implementation/pp-head-render/render/head-0.pngとhead-1.png、RGBA/neutral、bin/obj。ローカル画像キャッシュ不要。見た目の最終ゲームサイズ確認、ステッカーの曲面変形/アンチエイリアス、４旗形の実画像比較、GPUキャッシュ、全素材/役職/汚損/予約/６枠操作は未対応。今回の実画像２国確認を全素材対応済みと扱わない。次はゲーム側で必要なPP素地をContentへ登録し、同一国旗のキャッシュと解放を備えた描画へ接続する（元画像NAS原本は保持）。能力/コストの素材対応は未決定のまま。実ゲーム/元保存/公開物/CPU判断未変更、コミット等なし。タスク継続。
## 2026年11月11日１時11分・Content登録とGPUキャッシュを検証

ゲーム用素地Content/Images/Robots/pp-head-neutral-v001.pngをNAS承認原画から照合コピーし、csprojの出力コピー対象へ登録。NAS原画/ローカル制作原画は削除しない。RobotHeadTextureは256角へ読み取り縮小した素地/マップから国旗頭を生成し、５要素の値でGPUテクスチャをキャッシュ。16件上限でFIFO解放、Disposeの全解放/二重呼出/破棄後拒否を実装。通常SpriteBatch AlphaBlendに合わせてアップロード前にpremultiplyする。顔/ステッカーのC#共通処理を利用し、国旗変更で顔の原画を作り替えない。

tests/RobotHeadGpuはGame1を起動せず、セーブ/自動保存/ゲーム状態を持たない小さなMonoGame.Gameだけで検証。同じ国旗の参照一致、色/模様変更時の別テクスチャ、24デザインで16件上限と古い画像のIsDisposed、全解放/再破棄/破棄後アクセス例外をassertし成功。640×360に300角２国頭を描画しpreview.pngを目視確認、顔/猫/ハート/模様/光沢/背景との輪郭を確認。最初はSDL2不足で起動失敗したため既存v0.7.0検証用SDL2.dllをNAS検証binへコピーして再実行成功。Release警告０/エラー０。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/pp-head-gpu/ にbin/obj/preview.png。実行素材１枚だけプロジェクトへ、再生成出力はNASへ保存。通常Game1からの呼出は未接続で、格納庫/戦場が新アートへ切り替わったとは扱わない。次はロード/UnloadContentへ所有関係を接続し、部分パーツ表示と元の操作当たり判定を整合させる。９素材の能力マッピング、未承認仮配置やぜんまい色を決めない。全素材の実行素材と全身/役職/汚損/６枠ページは未対応。元保存/実ゲーム/公開版/CPU判断未変更、コミット等なし。タスク継続。
## 2026年11月11日１時42分・通常パーツ描画へPP頭を段階接続

Game1.RobotHeadArt.csを追加し、DrawRobotGlyphから幅48以上・一般兵・分離型・正規・未汚損の頭だけを新PP頭へ接続。頭の旧矩形/旧外周線/頭のみ時の重複紋章をスキップし、既存の部位別opacityを反映。頭の下にある胴/脚、既存操作判定は変更していない。小アイコン/隊長/一体型/安物/高級/汚損は素材未対応のため既存描画を維持。これは９素材や戦場全体の切替完了ではない。実行素地が無ければ旧表示へフォールバック。初回の必要時にロードし、UnloadContentで所有キャッシュを解放。

キャッシュを調査し、SpriteBatchの遅延描画中に追い出し画像を即Disposeする危険を修正。追い出しは退避し、次フレームDraw冒頭で前フレーム分を解放、Unloadでも解放。GPU試験を１フレーム24国旗の実Drawへ拡張し、追い出し後もEnd前に生存/End後に解放をassert。既存の再利用/国旗変更/上限/全解放も成功。

全体ReleaseビルドをNASへ隔離して実行。最初はBaseIntermediateOutputPath変更でローカルobjの古いAssemblyInfoが通常ソースに混ざり重複属性エラー。csprojのDefaultItemExcludesへbin/**とobj/**を明示し再実行、警告０/エラー０。FontCoverage 988文字PASS。出力NAS tmp/delete-after-2026-11-15/robot-art-implementation/game-head-integration/bin/とbuild.log。BuildContent=falseで既存Contentキャッシュを利用する検証ビルドであり公開物ではない。専用GPU試験の出力はpp-head-gpu配下。通常Game1画面のスクリーンショット/頭当たり判定の目視回帰はまだ未実施なので次回優先する。実ゲーム/元セーブは起動・変更していない。

次は隔離したGame1描画試験で通常/頭のみ/混在部位/薄表示を確認し、３列２段へのページと選択/ドラッグの整合を進める。承認済みモック配置とは混同しない。全素材/役職/汚損/予約ぜんまいは未対応。CPUソース/ルール/公開版/個人原本未変更、コミット等なし。タスク継続。
## 2026年11月11日２時12分・実Game1のパーツ描画を隔離目視

tests/RobotGlyphIntegrationを追加し、NASの新本体DLL FCFEBC567345CBD44B850F9DF0C944BB85AC3974B12752B7F955C9387051CF53を参照。Game1派生だがLoadContentのbase呼出を省き、Updateも空として自動保存/達成状況/音楽/ゲーム進行を読み書きしない。必要なSpriteBatch/白画素/UiPainter/人工WorldSetupだけ注入して、実際のDrawRobotGlyphを呼ぶ。Game1コンストラクタの操作ログはNASの検証実行先配下へ作成され、利用者ログと混ざらない。

完成体・頭のみ・異国所属頭の半透明・汚損頭・安物頭・隊長の６例を960×540に描き、画像を目視。通常/頭のみには新頭が入り、半透明も反映。未対応の汚損/安物/隊長は旧表示を維持した。新頭と旧矩形の胴脚が混在しており、全身のアート統一は未完了である点も確認。頭が大きく表示できる６枠と胴/脚の素地を続ける必要あり。単純な矩形中心Contains試験は操作回帰の根拠にならないので削除し、実際のクリック/ドラッグ回帰が済んだとは報告しない。

Release再実行は警告０/エラー０、Game1共通描画経路とキャッシュ生成assert成功。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/ にbin/obj/preview.png/Logs。元保存/実ゲーム/公開物へ触れず。今回製品ソースの追加変更なし。次は既存半画面インスペクター内の格納庫を６枠ページへ変更し、実スロット番号・表示番号・選択/ドラッグを分離してテストする。未承認４配置案の採用とは分ける。全素材/全身/役職/汚損/ぜんまいは未対応。コミット等なし、タスク継続。
## 2026年11月11日２時44分・通常格納庫を３列２段の６枠ページへ

通常インスペクターのHangarCellを３列２段（252×260、横264/縦270間隔）へ変更し、ロボット表示を68×102から100×180、部位対象を102×62へ拡大。格納庫容量/ロボットIDは変更しない。表示・予約クリック・部位ドラッグ開始・ドロップ・帰国予約の対象をVisibleHangarSlotsへ共通化。ページ枚数は格納庫専用HangarPageCount（６枠）に分離し、直送便の既存12枠ページは今回維持。ページ変更時はドラッグを取消して、前ページの座標を別IDへ誤適用しない。文字/入荷ラベルを拡大枠の下側へ移動した。４つの仮配置案を採用する変更ではない。

本体Releaseビルド警告０/エラー０。隔離Game1試験へ実VisibleHangarSlots/HangarCellの検証を追加し、容量1/6/7/12/59/60の全物理IDがちょうど１回ずつ表示されること、１ページ最大６、可視セルの非重複/インスペクター内収容をassert成功。既存パーツ６例の描画も再実行成功。出力NAS game-head-integration/build-six.logとglyph-integration/preview-six-check.png（画像はパーツ検証の再走で、格納庫全体の目視確認ではない）。

未確認：拡大した格納庫全体の文字/荷札/予約表示の目視、実クリック・ドラッグ操作、ページ越しドラッグの取消、チュートリアル/デモの自動指差しで対象ページへ移る処理。次回はこの回帰を優先。直送便/長距離便の左右格納庫はまだ12枠なので、６枠へ揃える作業も残る。実ゲーム/元セーブ/CPU判断/ルール/公開物未変更。コミット等なし、タスク継続。
## 2026年11月11日３時14分・実ページボタンとデモ追従

Game1.DemoDomesticの自動指差し前に、stage4/7は元スロット、5/6は先スロットの６枠ページを表示する処理を追加。通常ユーザーのページボタンはドラッグ取消、デモの自動追従は保持したまま本来のDemoAssemblyReleaseへ渡す区別。ページをまたぐ組立例でも見えていない別スロットを指さないようにした。ただし実デモ全行程の再生回帰は未実施。

隔離RobotGlyphIntegrationを人工配置世界と操作プレイヤーで初期化し、実HangarPageClickへ矢印座標を渡して０→１→上限１→０と、保持ドラッグの取消をassert成功。容量12への設定は内部SetCapacityをreflectionで呼ぶテストのみ。初回の直接呼出はinternalのためコンパイル失敗、修正後は操作プレイヤー未設定のCurrent(-1)で失敗、SelectTurnRuler(0)を補って再実行成功。元セーブは使わない。最終Release警告０/エラー０、既存６枠ID網羅/描画試験も成功。本体ビルドも警告０/エラー０。

NAS game-head-integration/build-input.log、glyph-integration/preview-input.png/bin/objに保存。今回の入力回帰はページ矢印であり、ロボット本体を押して別枠へ落とす一連の部位移動の回帰ではない。次回は２ページ目の物理スロット６/７へ異なるIDを置き、実NodeAssemblyLocalInputのpress/releaseで組替予約対象が正しいかを検証する。格納庫全体のフォント/荷札目視、配送画面６枠化、全素材/全身は引き続き未対応。実ゲーム/元保存/公開物/CPU判断/ルール未変更、コミット等なし。タスク継続。
## 2026年11月11日３時44分・２ページ目の部位ドラッグ予約IDを確認

AGENTS/再開点/差分を確認し、通常ゲームとセーブには触らず隔離RobotGlyphIntegrationへ実入力回帰を追加。人工世界の自国格納庫容量12に頭100001/胴100002を配置、表示スロット配列は物理６/７へ割り当て。HumanPlayersと操作プレイヤーを明示し、実NodeAssemblyLocalInputへMouseState.Pressedと頭の対象座標を渡す。_hangarDrag=100001/_hangarSource=６をassert。その後スロット７へReleasedを渡し、実PartPlanning.OrdersのRobotId=100001/TargetRobotId=100002/Parts=Head、ドラッグ終了をassert成功。

初回はテスト内のslots変数名競合でコンパイル失敗しmappingへ改名して修正。最終Release警告０/エラー０、既存ページID網羅/矢印入力/描画６例も成功。今回は製品コードの追加変更なし。NAS glyph-integration/preview-drag.png/bin/objに出力（画像は既存パーツ比較で、格納庫全体の見た目を検証した画像ではない）。

残課題：格納庫全体の文字/荷札/予約ラベルをフォント付きで目視、予約クリック（守備/帰国）とページ最後の端数枠、配送２地点画面の６枠化。デモのページ追従はコード追加済みで全行程実再生は未確認。９素材の全身/役職/汚損/ぜんまい実素材も未対応。実ゲーム/元保存/公開物/CPU判断/ルール未変更、コミット等なし。タスク継続。
## 2026年11月11日４時14分・６枠全体を実フォントで目視、守備予約IDも確認

隔離RobotGlyphIntegrationへ実UiFont.xnbを読み込み、人工世界に完成体６体を配置。実NodeAssemblyLocalInputの２番目のセルクリックで守備予約を入れ、PartPlanning.Preview内のID200002だけDefenseOnlyになることをassert。他個体には付かない。初回は実在庫へ即反映すると誤認して試験失敗、予約は予定プレビューへ反映する既存仕様を確認して試験を修正した。製品動作の変更は不要だった。

そのまま実DrawNodeAssemblyを1920基準の0.5倍（960×540）で描画しNAS inspector-six.pngを目視。３列２段６枠、拡大したロボット、基本部位ラベル/ID、２番目の予約演出、ページ矢印、下部予約ボタンが枠内に収まることを確認。入荷/出荷荷札はこの人工場面に無いため、荷札の重複がないとは未検証。小画面で文字は細かく、通常解像度/拡大画面との見比べも残す。旧胴脚と新頭の混在は継続。

最終Release警告０/エラー０、全ページ列挙/矢印入力/２ページ目ドラッグ/守備予約の既存試験もPASS。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/inspector-six.png/bin/obj、フォントも検証Contentへコピー。ユーザーのセーブ/ゲームは起動せず人工世界のみ。今回製品ソース変更なし。次は帰国予約と端数ページのクリック、荷札のある場面、直送/長距離便の６枠化。全素材/全身/帽子/汚損/ぜんまい実装は未完了。CPU/公開物/ルール/個人原本未変更、コミット等なし。タスク継続。
## 2026年11月11日４時44分・帰国モードと端数ページの実入力回帰

AGENTS/再開点/差分を確認。隔離人工世界の格納庫容量を７へ変更し、外国の完成体ID300001（Owner1）を物理スロット６だけへ配置。実NodeAssemblyLocalInputで帰国予約モードのまま次ページ矢印を押し、page１/帰国モード維持をassert。１体だけ表示する最終ページでID300001をクリックしReturnHomeCommander=0を確認。存在しないスロット７相当の位置をクリックしても予約状態が変わらず、元のスロット６を再クリックすると解除されることをassert成功。帰国先の国が存在する人工世界のみで、元保存を使わない。

最終Release警告０/エラー０、ID網羅/ページ矢印/部位ドラッグ/守備予約/帰国端数の全試験成功。製品コードの追加修正は不要だった。NAS glyph-integration/bin/obj/inspector-return-test.png（画像は先に描画した通常６枠で、帰国端数ページのスクリーンショットではない）。今回の回帰は入力と状態変化を確認したもの。次は荷札のある格納庫表示、直送/長距離便の左右を６枠へ統一する作業を進める。全素材/全身/帽子/汚損/ぜんまいはまだ未完了。実ゲーム/元セーブ/公開物/CPU判断/ルール/個人原本未変更、コミット等なし。タスク継続。
## 2026年11月11日５時14分・直送/長距離便の左右も６枠ページへ

AGENTS/再開点/差分を確認。Game1.DirectDelivery/LongDeliveryの表示・部位選択・ドラッグ/ドロップ先走査をVisibleHangarSlotsへ揃え、DirectCellの繰返し周期を12→６へ変更。既存３列４段の上２段だけを１ページとし、容量は維持。左右の枚数/クランプもHangarPageCountへ変更。ページを押したら_directDragを取消し、別ページの同座標へ誤配送しない。今回ロボットの大きさ/セル寸法は従来のままであり、通常格納庫と同じ拡大まで済んだとは扱わない。

本体Release警告０/エラー０。隔離Game1テストへ実DirectPageClickを追加し、右だけ０→１/上限１、左だけ０→１で他側のページが変わらないことを容量７でassert成功。既存の全容量ID網羅/通常格納庫矢印/２ページ目部位組替/守備/帰国端数も再走PASS。出力NAS game-head-integration/build-delivery-six.log、glyph-integration/bin/obj/inspector-delivery-page-test.png（画像は通常格納庫で配送画面の目視ではない）。

次は実DirectPairInput/LongDeliveryInputの２ページ目ドロップで予約のTargetSlot/RobotIdと荷札表示を確認し、配送画面の素材拡大と荷札配置を調整する。左右パネルが通常格納庫より狭い現行配置なので、横にはみ出さない寸法を確認する。未承認モック配置を自動採用しない。全素材/全身/役職/汚損/ぜんまいは未完了。元セーブ/実ゲーム/CPU判断/ルール/公開物/個人原本未変更、コミット等なし。タスク継続。
## 2026年11月11日５時46分・長距離便専用ページに残った12枠計算を修正

AGENTS/再開点/差分を確認。前回は共有DirectPageClickの検証までで、LongDeliveryInput内の専用LongPageButtonには12枠の除算が残っていた。容量７〜12では表示が６枠になっても次ページへ進めない実装漏れだった。専用LongPageClickを切り出し、ページ上限/表示ページ数/SyncLongSlotsのクランプをHangarSlotsPerPageで統一。専用ページ変更でも保持ドラッグを取消す。

隔離試験へ実SyncLongSlotsとLongPageClickを追加し、容量７の右側０→１/上限１、左側不変をassert成功。既存の通常格納庫/直送便ページ、ドラッグ、守備/帰国端数の全試験も成功。本体とテストのRelease警告０/エラー０。NAS game-head-integration/build-long-page.log、glyph-integration/bin/obj/inspector-long-page-test.png。画像は通常格納庫の再走で、長距離便全体の目視ではない。

前回の「長距離便も６枠ページへ統一」は専用入力の網羅が不十分だった点をこの節で訂正。今回配送ドロップID/荷札の試験へ入る前にページ到達不具合を修正した。次回は実LongDeliveryInputで左スロット６から右スロット６へpress/releaseし、RobotId/TargetSlot/Receiverを確認。その後、実配送画面の荷札と拡大配置を目視する。全素材/全身/役職/汚損/ぜんまいは未完了。実ゲーム/元保存/公開物/CPU判断/ルール未変更、コミット等なし。タスク継続。
## 2026年11月11日６時16分・長距離便の２ページ目ドロップ予約を確認

AGENTS/再開点/差分を確認。人工世界の隣接する２地点を自国所有/容量７にし、左の物理６へ頭400001、右の物理６へ胴400002を置いた。実LongPairInput（記録の旧呼称LongDeliveryInputに相当）へPressed/Releasedを渡し、左ページ２で保持ドラッグが始まり、右ページ２へ落とすとLongDelivery.OrdersにRobotId400001/Receiver400002/TargetSlot６/正しいSource・Targetが入ることをassert成功。画面座標の繰返しによる枠０への誤記録は無かった。

最終Release警告０/エラー０。通常組替/守備/帰国/共有ページ/長距離専用ページを含む既存操作試験も再走PASS。今回製品ソース追加変更なし。NAS glyph-integration/bin/obj/inspector-long-drag-test.png（画像は先に描いた通常格納庫で、配送場面ではない）。次は実DrawLongPanel/直送ペアを荷札付きで描き、配送側の拡大と荷札配置を調整する。長距離予約取消のクリックも追加するとよい。全素材/全身/役職/汚損/ぜんまいは未対応。実ゲーム/元保存/公開物/CPU判断/ルール/個人原本未変更、コミット等なし。タスク継続。
## 2026年11月11日６時46分・荷札付き配送パネルを目視し素材を拡大

AGENTS/再開点/差分を確認。隔離テストで実DrawLongPaneの左右を、前回の頭400001→胴400002予約付きで描画。入荷/出荷荷札と透けた到着予定頭を目視した。旧164×156セルには縦余白がある一方ロボットは小さかったため、共通DirectCellを164×280/縦290間隔の３列２段へ変更。直送/長距離便のロボットを68×102→90×180、部位ヒット領域を90×58/縦60間隔へ拡大。荷札は右側（セル内）へ置き、入荷と出荷の両方がある部位では出荷札を26px下へずらす。旧左側への札は隣セルへ出るため使わない。ID/予約ラベルもセル下へ移動した。左右パネルの位置/幅は未承認モックへ変更していない。

本体と隔離試験のRelease警告０/エラー０。拡大後の座標で実ページ２ドラッグが引き続き正しい元/先/TargetSlotを予約し、既存通常格納庫/予約/ページ試験もPASS。更新したNAS glyph-integration/long-panes-tags.pngを目視し、荷札がロボット右側に収まり、到着予定と入荷札が表示されることを確認。描画例は容量７の最終１枠であり、６体が並ぶ場面/入出荷両札/直送全体の目視はまだ残る。build-delivery-size.logはNAS game-head-integration配下。

次は６体/両荷札を並べる場面と予約取消、直送の実入力を確認する。その後、未対応の胴/脚素地を制作して全身のアート統一へ戻る。全素材/役職/汚損/ぜんまいは未完了。元セーブ/実ゲーム/公開物/CPU判断/ルール/個人原本未変更、コミット等なし。タスク継続。
## 2026年11月11日７時16分・左右６体と両荷札の確認

隔離試験で左右各６体を２ページ目に配置し、左の頭に入荷予定と出荷予定を同時に表示した。960×540の実パネルを目視し、２枚の荷札が上下に並び、隣のセルと重ならないことを確認。実クリックで出荷予約だけが解除され、逆向きの入荷予約は残ることを確認した。Releaseビルドは警告０・エラー０、既存操作回帰も通過。

NASの tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/long-panes-tags.png に確認画像を保存。次は直送の実入力と、胴・脚の実行素材を進める。

## 2026年11月11日７時48分・直送便の２ページ目操作を確認

人工世界の左右２ページ目で、直送便の実入力処理に頭400001の押下と胴400002へのドロップを渡した。組替予定の元と先の地点、両個体ID、頭部の指定が一致した。隔離Releaseは警告０・エラー０で、通常格納庫と長距離便の既存回帰も通過。NASの glyph-integration/inspector-direct-test.png は試験中に描いた通常格納庫。直送画面の目視と胴・脚の実行素材は次の作業に残る。

## 2026年11月11日８時18分・ポリプロピレンの胴と脚の原画

既存の共通頭を参照して中立色の胴と脚を個別に生成。両方1536×1024で角と余白のサンプルはアルファ０。原画はNASの art-source/robots/pp-parts-v001/torso.png と legs.png に保存。外周の半透明のにじみ、頭との接合、実サイズでの比率を確認してから実行素材を登録する。国旗マスクと描画接続も次の作業に残る。

## 2026年11月11日８時48分・胴と脚の透過分布

NAS原画を４画素間隔で計測。アルファ240以上の範囲は胴が (144,136)〜(1376,920)、脚が (268,144)〜(1220,928)。非ゼロのサンプルのうちアルファ１〜31は胴728点、脚899点。胴と脚で横方向の余白が異なるため、頭と同じ正方形への伸縮を使うと接合時の比率が変わる。次の試作では素材ごとに縦横比を保ち、実サイズで輪郭と接合を確認する。

計測結果はNASの tmp/delete-after-2026-11-15/robot-art-implementation/pp-parts-alpha/alpha.json。実サイズでの目視、国旗マスク、描画への組み込みは残っている。

## 2026年11月11日９時18分・頭・胴・脚の接合見本

中立色の原画を縦横比を保って合成し、幅84pxと42pxの全身を濃色背景で目視。首元と腰で隙間なくつながり、小さい方でも顔と脚を識別できた。確認画像はNASの pp-parts-alpha/assembly-preview.png。元画像の描画範囲は頭 (76,190,1076,880)、胴 (136,128,1248,800)、脚 (260,136,968,800)。幅84pxの見本では頭72px、胴84px、脚58px。これらは描画試作の寸法で、次は国旗の着色範囲と紋章の位置を合わせて確認する。

### 2026-11-11 09:48 JST
PPの胴・脚向けに、素地の透明部を保護しながら指定矩形から国旗のUVを求める処理を追加した。頭と同じGPU合成でパーツごとに紋章の位置を指定できる。国旗320組と、胴・脚向けマップの透明部・反射・UV境界の回帰が通過した。GPUテストプロジェクトはビルド成功。次はNASの中立胴・脚で実画像の合成を目視し、格納庫描画へ接続する。

## 2026年11月11日10時18分・PP胴と脚の実画像GPU合成

前回追加した胴・脚のマップをRobotHeadGpuの実画像試験へ接続。原画の縦横比3:2で描画し、胴の胸と脚の片足へ共通国旗の紋章を配置。ピンク/青の斜め縞と緑/青の格子の２種類を目視し、光沢、腕、脚の隙間、輪郭の透明が維持されることを確認。描画は仮の矩形UVであり立体面ごとのUV調整と全身での紋章サイズ確認は残る。ゲーム本体への胴脚の接続は次回。

GPU試験はRelease警告０・エラー０、胴/脚の両方でキャッシュ再利用、色変更、遅延描画中の資源保持、フレーム後の破棄をPASS。出力はNAS tmp/delete-after-2026-11-15/robot-art-implementation/pp-head-gpu/torso-preview.png と legs-preview.png。比較表の切り貼りは使わず中立原画を実行時に着色した。テストのログ文言は既存のhead表記を含むが、実行引数torso/legsで別々の原画とマップを使用。全素材・汚損・役職・ぜんまいは未完了。次は全身の実行時合成と通常格納庫への接続。

## 2026年11月11日10時48分・PP全身を格納庫へ接続

NASの中立胴・脚をContent/Images/Robotsのpp-torso-neutral-v001.pngとpp-legs-neutral-v001.pngに登録し、元画像とのSHA-256一致を確認。通常品質・無傷・分割式の完成一般兵で、各部位の製造国を使って頭・胴・脚を動的合成する。首と腰の描画順は前回の接合見本に合わせ、格納庫のセル内で縦横比を維持した。部位別の半透明表示も同じ経路に渡す。

本体とRobotGlyphIntegrationの隔離Releaseビルドは警告０・エラー０。６枠のページ、選択、ドラッグ、守備、帰国、直送、長距離便の既存回帰を通過。960×540の通常格納庫で全身６体を目視し、頭・胸・足の紋章、首と腰の接合、セル内の収まりを確認した。画像はNASのtmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/inspector-pp-full.png。

次は単体や混成機の胴・脚にも個別のアートを適用し、完成機の部位境界とドラッグの見た目を確認する。戦場、他素材、汚損、役職、ぜんまいは引き続き作業対象。

## 2026年11月11日11時18分・単体と混成機のPP胴脚

通常品質・無傷の胴と脚を部位単位で判定し、単体と混成機でも製造国の紋章・配色を合成する。単体は各部位の従来の表示帯へ元画像の縦横比を保って収めた。３部位に素材が揃う場合は前回の接合配置を使う。

隔離Releaseビルドは警告０・エラー０。単体が完成機より先に描画されても素材が読み込まれること、６枠の選択・ドラッグ・配送回帰を確認。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-part-samples.png で単体、異なる製造国、旧表示の頭とPP胴脚の組み合わせを目視した。混成機には従来の灰色の腰接合が見えるため、接合とドラッグ時の見た目を次に調整する。戦場、他素材、汚損、役職、ぜんまいは引き続き作業対象。

## 2026年11月11日11時48分・腰の接合と脚ドラッグ

PP脚は素材自体に腰の横帯があるため、混成機でその上に重なっていた旧表示の灰色横帯を省いた。胴と脚をつなぐ中央の支柱は維持した。隔離ビルドは警告０・エラー０。通常格納庫２ページ目で脚100003を押して胴100002へドロップし、脚の組立予約とドラッグ終了を確認。960×540でドラッグ中の足とドロップ先の枠、混成機の腰を目視した。既存の６枠・配送回帰も通過。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/ の inspector-pp-leg-drag.png と pp-part-samples.png に確認画像。次は戦場の描画入口と向き、役職別の重ね描き位置を確認してPP素体の共用を進める。他素材、汚損、帽子、小銃、予約ぜんまいは作業対象。

## 2026年11月11日12時18分・戦闘導入で共通素体を使用

戦場の実描画を確認。導入時の部品表示は62×85、通常戦闘は向き別BattlePieceArtへフェードする構成。導入の３回の部位描画をRobotを直接渡す共通DrawRobotGlyph１回へ変更した。製造国、一体型情報、品質を渡し、PP完成兵は格納庫と同じ接合配置を使う。各部位のフェードは既存BattlePartOpacityを渡す。

Release本体と隔離試験は警告０・エラー０。製造国が違う頭を持つ62×85の完成兵で、導入０秒と２秒を描画し、頭だけが消え自国の胴脚が残る画素検証と目視を通過。６枠と配送の回帰も成功。確認画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-battle-intro.png。実戦全体の再生・向き別の新兵隊アートは次の作業。通常戦闘のBattlePieceArtと帽子・小銃の実行素材を合わせて調査する。

## 2026年11月11日12時48分・通常戦闘の混成部品の配色

通常戦闘では部品の主色だけ製造国から取得し、模様と副色には所属軍を使っていた。部品の製造国を一度求め、主色・副色・模様すべてに使用するよう修正。従来の向き別BattlePieceArtに対する変更で、通常戦闘の新しい素体や装備は作業中。隔離Release警告０・エラー０。異なる製造国の頭の塗り画素がその国の２色だけになることを検証し、戦闘導入・格納庫・配送の既存回帰も成功。次は帽子・小銃の素材制作と通常戦闘の向き別描画を進める。

## 2026年11月11日13時18分・予約ぜんまい

格納庫などが共用するロボット描画で、予約靴の代わりに頭右側へ金属色のぜんまいを表示する。守備・迎撃・工場作業・出撃の既存表示判定と、頭を持つ廃棄予約を使用。用途別の着色は未決定なので共通の金属色とした。予約受付と演出の進行は既存状態から取得する。戦闘中・地図上・輸送計画の靴表示には今後の接続が必要。

本体と隔離試験のRelease警告０・エラー０。無予約と守備予約の同じPP素体を並べ、ぜんまいの画素が守備予約にだけ出ることを確認。顔と側面紋章の位置も目視。格納庫６枠・脚ドラッグ・配送の既存回帰成功。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-windup-comparison.png に比較画像。次は残る靴の描画入口と、輸送予約の表示条件を照合する。

## 2026年11月11日13時48分・地図と戦闘と輸送の予約表示

地図の守備予約、戦闘中の守備表示、輸送計画の受付演出を共通の金属色ぜんまいに切り替えた。格納庫などの共通描画でも、長距離便の予約番号を持つ頭付き機体にぜんまいを表示する。直送の予約は既存荷札で表示しており、ぜんまいとの併用は未検証。

隔離Releaseは警告０・エラー０。無予約・守備予約・長距離便予約を並べて画素と目視で確認し、６枠と配送操作の既存回帰が通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-windup-comparison.png に比較画像。地図・戦闘・輸送受付の実画面での重なりは未確認。次は戦闘用の帽子と小銃の素材を進める。

## 2026年11月11日14時18分・クイーンの正面素材

採用済みqueen-study-v002の一体型磁器玩具を参照し、imagegenで背景と床影を除いた透過正面素材を制作。NAS art-source/robots/queen-runtime-v001/queen-front.png とゲーム用Content/Images/Robots/queen-front-v001.png のSHA-256はAFE384FD134EB319A19B5599C8D79DD5C90ED65F8D97411575DE77538B8DB6AE。戦闘の南向きに接続。北・東・西は従来の向き別駒を使う。原画の猫紋章と装飾色は現段階で固定。製造国の旗を反映する部分は後続作業。

隔離Release警告０・エラー０。62×85の戦闘枠で人物画素と透過の角を検証し、王冠・顔・笏を目視した。北向きの既存表示への分岐と既存格納庫・配送の回帰も通過。画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/queen-battle-front.png。次は横・背面と、ゲーム中に向きが変わる場面を確認する。

## 2026年11月11日14時48分・クイーンの４方向

採用済み正面を基に透過の右側面と背面を制作。NAS art-source/robots/queen-runtime-v001/queen-side.png と queen-back.png を Content/Images/Robots/queen-side-v001.png と queen-back-v001.png に登録。SHA-256は側面6D2B6D4DE66328D7F0A76E4D80CE41B81418E5084755A7AFC0340239DFB163F5、背面E53F626B8E1AD59DA3C6042089B87CA182F682A39208A65CE2E07990BDD7A482。北は背面、南は正面、東は右側面、西は右側面の反転。反転時は笏も反転する。各素材は初回使用時に読み込んで再利用する。

本体・隔離試験Release警告０・エラー０。62×85枠の４方向で人形画素と透過角を確認し、顔の向きと王冠・裾を目視。既存の格納庫・配送回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/queen-battle-directions.png に確認画像。実戦中の向き変更の通し再生とクイーンの旗反映は未確認。次は戦場の所属表示と一般兵・隊長の帽子・小銃を進める。

## 2026年11月11日15時18分・クイーンの所属旗

クイーンの戦闘枠の右下に所属国の旗を表示。既存CountryFlagArtを使い、４方向で同じ位置に旗を出す。磁器の装飾と猫紋章は原画どおり。旗は駒の横に表示する方式で、衣装への合成は未対応。

隔離Release警告０・エラー０。62×85枠の４方向と２国の正面を描き、所属国によって旗が変わり顔の画素が同じであることを確認。既存の格納庫・配送回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/queen-battle-flags.png に確認画像。旗が隣の駒や戦闘エフェクトに重なるかは通し再生で確認する。次は一般兵・隊長の帽子と小銃の実行素材。

## 2026年11月11日15時48分・兵隊装備の透過原画

採用済みsoldier-hat-study-v002／soldier-rifle-study-v001とcaptain-hat-study-v001のPP例を参照し、共通素体へ重ねられる一般兵帽、隊長帽、上向き玩具小銃を独立した透過画像として生成した。NAS art-source/robots/soldier-runtime-v001/ に保存し、生成元とSHA-256が一致することを確認。４隅のうち左上アルファ０と中心の不透明画素を確認し、外形と形状を目視した。８画素間隔で測った不透明範囲は描画位置の初期値。

| 原画 | サイズ | 不透明範囲 x1,y1–x2,y2 | SHA-256 |
| --- | --- | --- | --- |
| soldier-cap.png | 1312×1199 | 208,168–1096,1000 | 6D8B6717C477A63DAE9F3A2ED2976A64126F220CEB91C38E2205A948A47C35B9 |
| captain-cap.png | 1536×1024 | 536,312–992,728 | 9A071CC6517F2BB90741C5EC39D455D276CE00BB8D45A99C457224AEAB17550D |
| toy-rifle.png | 724×2172 | 192,120–520,2072 | 3F338DCF7ED316D3119D773526804A27A570EB80B231FCA47F9D33E158F3757D |

ゲーム描画とビルド試験は次の作業。62×85の戦闘枠内で帽子の役職差、腕との接点、顔と紋章の視認性を確認してから、通常戦闘のPP兵に接続する。

## 2026年11月11日16時18分・一般兵と隊長の正面戦闘駒

NAS soldier-runtime-v001 の一般兵帽、隊長帽、玩具小銃を同じハッシュで Content/Images/Robots の soldier-cap-v001.png、captain-cap-v001.png、toy-rifle-v001.png に登録。南向きの無傷・通常品質・分割式の完成機に格納庫と共通のPP頭胴脚を使用し、役職別の帽子と小銃を重ねる。小銃は戦闘フレームの残数があるときに表示。描画処理は戦闘データを変更しない。横・背面や他素材は既存駒を使用する。

隔離Release警告０・エラー０。62×85で一般兵・隊長・小銃残数０の一般兵を描き、帽子と顔と胸紋章を目視。小銃の表示差を画素で確認し、格納庫・配送回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-battle-soldiers.png に確認画像。次は戦闘再生での向き変更と横・背面の素材を進める。

## 2026年11月11日16時48分・PPの汚損合成

wear-study-v005の表現に合わせ、通常品質の分割式PP部品にクロスハッチ状の塗装擦れと絆創膏を実行時合成。既存の損傷状態を表示に使用し、顔と紋章の保護領域、透明輪郭は維持する。格納庫、戦闘導入、南向きのPP戦闘駒で共用。安物・高級・一体型・他方向の既存表示は後続作業。

隔離Releaseと格納庫６枠・ドラッグ・配送回帰が成功。３部位のGPU試験で汚損のキャッシュ分離、輪郭の全アルファ一致、頭の顔画素の一致を確認。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-part-samples.png で汚損した頭を目視。胴・脚の汚損を戦闘寸法で並べる確認と、横・背面の素材は次の作業。

## 2026年11月11日17時18分・戦闘枠で汚損を確認

南向きPP戦闘駒を62×85で４体描き、無傷と頭・胴・脚の単独汚損を比較。胴と脚では絆創膏が小さく見えたため、同じマスク内で絆創膏の幅と明るさを調整した。各部位で汚損の画素差が出て、戦闘駒の旧表示へ切り替わらないことを検証。実寸では擦れの細線は控えめで、主な目印は絆創膏。横・背面の汚損描画は未対応。

本体・隔離試験Releaseは警告０・エラー０。格納庫と配送の回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-battle-wear.png の下段は左から無傷・頭・胴・脚。次は横・背面の素地を制作し、戦闘再生で向きが変わる場面を確認する。

## 2026年11月11日17時48分・PP頭の横と背面

正面のpp-head-neutral-v001.pngを参照し、横向きの右端にクリーム色の顔が見える頭と、背面の連続した灰色外装をimagegenで制作。NAS art-source/robots/pp-views-v001/ に保存し、ハッシュ照合・左上の透明・中心の不透明を確認。両方1254×1254で、８画素間隔の不透明範囲は横が80,200–1136,1064、背面が88,200–1144,1056。正面とほぼ同じ輪郭寸法を維持できる。

| 素材 | SHA-256 |
| --- | --- |
| head-side.png | 715404D788D3AB860ACD1B258E81FE1899EB836F88320EE36FC3B8FED7116E8B |
| head-back.png | E12DE0252DA4BA09B117673D7C45F4A2A98B67846B93B4AD1BE615580A8A6257 |

次は横の顔と光沢を保護するマスクと国旗UVを実画像で試す。背面は顔の保護範囲を持たない。胴・脚・装備の向き別素材と組み込みは未完了。今回の検証は原画の寸法・透過・目視まで。

## 2026年11月11日18時18分・横と背面の国旗合成

横向きの右端の顔と縁を、原画上で測った角丸矩形で保護。背面は外装全体に共通の国旗UVを適用。両方向とも光沢を残し、側面寄りに紋章を配置した。素材はNASの pp-views-v001 の２枚を使用。

隔離GPUビルド警告０・エラー０。横・背面それぞれに斜め縞と格子を描画して目視。横の顔の内側25×62画素が国旗を変えても一致し、塗装面は変わることを検証。汚損前後のアルファ一致とキャッシュの回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/pp-head-gpu/ の head-side-flags.png と head-back-flags.png に確認画像。今回の接続先は検証描画。胴・脚・装備の向き別素材を揃え、戦闘駒へ接続する作業は残る。

## 2026年11月11日18時48分・PP胴脚の側面

正面のPP素地を参照して右向きの胴と脚を独立した透過画像として制作。NAS art-source/robots/pp-views-v001/ に保存し、生成元のSHA-256と一致を確認。腕とブーツの光沢を残して２種類の国旗と紋章を合成した。

| 素材 | サイズ | 不透明範囲 x1,y1–x2,y2 | SHA-256 |
| --- | --- | --- | --- |
| torso-side.png | 1536×1024 | 432,144–1112,920 | 1748DF5C5150254E3A9FE7A204E22B969BF99F902CA4FFFF0CBC7ED72D2D1D12 |
| legs-side.png | 1312×1199 | 296,128–992,1072 | D4C5E815C7B78004FF0E3C830F878590662F61855AD9961907B46F622CE12677 |

GPU試験Release警告０・エラー０。２部位で国旗切替・透過・汚損前後のアルファ一致・キャッシュ回帰が成功。NAS tmp/delete-after-2026-11-15/robot-art-implementation/pp-head-gpu/ の torso-side-flags.png と legs-side-flags.png を目視した。ゲームへの接続は次の作業。横向き全身の首と腰、腕に重ねる小銃の位置を合わせる。背面の胴脚と帽子の向き別素材も残る。

## 2026年11月11日19時18分・PP戦闘駒の横向き

標準品質の分割PP一般兵と隊長に左右の全身描画を接続。頭・胴・脚ごとの出身国と汚損を既存の合成へ渡す。右向き素材の左右反転で左向きを描くため、左側の紋章も反転する。帽子は正面素材を共用。小銃はフレームに残数がある場合に腕へ重ねる。

| NAS原画（art-source/robots/pp-views-v001） | ゲーム実行素材（Content/Images/Robots） |
| --- | --- |
| head-side.png | pp-head-side-v001.png |
| torso-side.png | pp-torso-side-v001.png |
| legs-side.png | pp-legs-side-v001.png |

３ファイルのSHA-256一致を確認。NAS隔離Releaseビルドは警告・エラー各０。描画試験で左右それぞれの一般兵と隊長の３部位を確認。正面の小銃・汚損と格納庫の６枠操作の回帰も通過。実寸62×85の描画を目視し、小銃を腕へ寄せた。確認画像は NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-battle-wear.png。

次は背面の胴・脚と向き別の帽子。北向きの戦闘駒は既存表示。今回の目視は隔離した静止画まで。戦闘再生中の向き切替とPP以外の素材は未検証。

## 2026年11月11日19時48分・左向きの紋章

右向きの素体を反転して左向きを描く際、頭・胴・脚の紋章ステッカーだけを合成時に先に反転する。これで画面上の紋章の向きが左右で一致する。国旗地の模様は素体と一緒に反転する。GPUキャッシュは通常と反転ステッカーを区別する。使用素材は上記の横向き３点。

非対称紋章と白縁の画素を照合し、ステッカー外の画素が一致することを確認。製品DLLを直接使うよう試験２件の重複ソース参照を外した。隔離Releaseで製品・画素・GPU・格納庫描画の各ビルドは警告・エラー各０。国旗320通り、横顔の保護、キャッシュ再利用、格納庫６枠の回帰が通過。左右の戦闘駒は隔離静止画を目視。NASの向き別原画には背面頭があり、背面胴・脚と向き別帽子の制作および戦闘への接続が残る。

## 2026年11月11日20時18分・PP胴脚の背面

正面PP素地を参照し、組み込み用の背面胴・脚を組み込みImageGenで別々に制作した。NASの art-source/robots/pp-views-v001/ に保存し、生成元とSHA-256を照合。

| NAS原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| torso-back.png | 1536×1024 | E18586903F673B2486EFE1589655707052A58029E761E14C3355E791696D66AC |
| legs-back.png | 1536×1024 | 6F728DC68EF6D30D17B69F6D1C493006C7A3D4DF79A0F9FB26AB95459C20B869 |

隔離Release GPU試験は警告・エラー各０。国旗切替と汚損の前後で全画素のアルファが一致し、脚の隙間が透過することを確認。斜め縞・格子と紋章を合成し、光沢と輪郭を目視した。確認画像は NAS tmp/delete-after-2026-11-15/robot-art-implementation/pp-head-gpu/ の torso-back-flags.png と legs-back-flags.png。次は背面帽子を制作し、頭・胴・脚を北向き戦闘駒に接続する。今回の製品描画は未変更。

### 生成プロンプト

Create a production 2D game sprite, a direct orthographic REAR view of the exact same neutral gray glossy injection molded polypropylene toy robot part as the reference. One part only, the robot torso shell with both attached short arms and rounded mitten fists. Plain smooth rounded-square back panel with a subtle molded perimeter seam, no fastener, no head or legs. Match the reference silhouette, proportions, grayscale base and controlled glossy white specular highlights. Clean outer edges; no haze, floor, ground shadows, logos, flag, graphics, weapon, text or other objects. Center the entire part with 10% clear space, genuine transparent background.

Create a production 2D game sprite, a direct orthographic REAR view of the exact same neutral gray glossy injection molded polypropylene toy robot part as the reference. One part only, the joined hips and both short legs with boot heels. Rounded hip shell, symmetric rounded boots showing their heels and soles from behind, a real transparent gap between legs. No torso. Match the reference silhouette, proportions, grayscale base and controlled glossy white specular highlights. Clean outer edges; no haze, floor, ground shadows, logos, flag, graphics, weapon, text or other objects. Center the entire part with 10% clear space, genuine transparent background.

## 2026年11月11日20時48分・北向き戦闘駒

標準品質の分割PP一般兵・隊長に北向きの頭・胴・脚を接続。部位ごとの国旗・汚損を合成し、小銃が残る場合は背面の左腕側に表示。一般兵の黒帽子は正面素材を共用。隊長は金帯とボンボンの背面素材を使用し、前面の帽章とつばは隠れる。

| NAS原画（art-source/robots/pp-views-v001） | ゲーム素材（Content/Images/Robots） |
| --- | --- |
| head-back.png | pp-head-back-v001.png |
| torso-back.png | pp-torso-back-v001.png |
| legs-back.png | pp-legs-back-v001.png |
| captain-cap-back.png | captain-cap-back-v001.png |

４点のSHA-256一致を確認。隊長帽のSHA-256は 05BAE4A83DCF16B4F7A161F12401437538D620AEEB0AC5AD514265F66BB59695。隔離Releaseのゲーム・描画試験とも警告とエラー各０。北向きの頭・胴・脚の描画、既存方向と格納庫６枠の回帰を確認。62×85の駒は NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-battle-wear.png を目視。左右の隊長帽は正面素材共用の段階。戦闘再生での向き切替とPP以外の素材は未検証。

### 背面隊長帽の生成プロンプト（組み込みImageGen）

Create a single transparent game sprite: the orthographic back view of the exact toy captain shako in the reference. Keep the squat black cylinder, glossy rolled crown edge, centered golden pompom and continuous gold lower band. Camera faces the rear directly; the front flower badge and forward visor are entirely occluded by the black cap. Show a smooth unmarked black rear wall, with no rear badge, and the gold band. Match the toy lighting and proportions. No head, text, background, shadows or additional objects. Center it with clear margins.

## 2026年11月11日21時18分・横向き隊長帽

組み込みImageGenで右向きの隊長帽を制作。NAS原画 art-source/robots/pp-views-v001/captain-cap-side.png とゲーム素材 Content/Images/Robots/captain-cap-side-v001.png のSHA-256は 9C51063DF8E71FB44FDABF483DFEBAF3FADFBC79000A53D7AB9911239832EC12。左向きでは反転し、つばを進行方向に合わせる。

隔離Releaseは警告・エラー各０。全方向の駒を62×85で目視し、格納庫６枠と地星儀の描画回帰も通過。確認画像は NAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/pp-battle-wear.png。戦闘再生中の向き切替とPP以外の素材は未検証。

Create one transparent game sprite of this exact toy captain shako in strict orthographic side profile, with the short black visor pointing to image right. Show the same squat black glossy cylinder, centered golden pompom, continuous gold lower band, and only a slim edge of the front flower badge at the right. The front badge must not appear on the side panel. Isolated cap only, transparent background, no head or other objects, clear margin on all sides.

## 2026年11月11日21時48分・戦闘フレームの方向と装備

隔離描画試験に、向きと小銃残数が変わる前後の BattleState.Capture を追加。過去フレームが変わらず、東西北の保存された向き・残数でPP駒を描画できることを確認。製品の DrawRobotBattle が同じフレーム項目を使用していることをコードで照合。隔離Release警告・エラー各０、格納庫６枠と地星儀の回帰も通過。製品コード・実行素材の追加変更なし。全画面の実時間再生とPP以外の実行素材は引き続き未確認。

## 2026年11月11日22時18分・素材別描画の接続点

現行の部位データには素材IDがなく、出身国・安物・高級品・汚損・一体成形の情報がある。通常格納庫のPP素材は分割された正規品の部位で使われ、安物・高級品は既存シルエットへ戻る。戦闘の新しい全身描画は頭・胴・脚が揃う場合に使う。地図の小アイコンも現状は安物・高級品のフラグで描き分ける。旧保存は高級品・一体成形フィールドの欠損を許す。９素材へ接続する際は、素材を既存等級とは独立して部位ごとに扱えるかを、組み替え・分離・旧保存の３経路で確認する。

今回の素材と装備数は相談段階。描画監査のみでゲームコード、能力、保存形式、原画に変更なし。９素材の動的合成を検証する次の対象は、木目を保つメープルの素地と塗装マスク。素材の保存・配布先を決める前でも隔離描画で比較できる。

## 2026年11月11日22時48分・メープルの塗装マスクの画素試験

既存の合成器を使い、木目の明暗を持つ合成画素４点で、露出した木肌と塗装下の木目を２種類の国旗で比較。露出部分は画素が一致し、塗装部分は国旗で変わり、木目の明暗差とアルファが残った。隔離Release警告・エラー各０、既存の国旗320通りも通過。ゲーム描画・素材割当・能力の変更なし。次はメープル原画の素地と実寸の塗装マスクで目視する。試験出力先はNAS tmp/delete-after-2026-11-15/robot-art-implementation/flag-composite/。

## 2026年11月11日23時18分・メープル頭の２国旗比較

head-study-v002のメープルを参照して、組み込みImageGenで無塗装の頭を制作。NAS art-source/robots/maple-head-neutral-v001/maple-head-neutral-v001.png に保存。SHA-256 58F6C15CF04CE2C7C9C8E3D5CFA840E12B4EFBA5AAA4C9A21CCE98F4BD41B686。試験用に顔の丸い輪郭と側面の木肌をマスクで保護し、塗装部分には元の木目の明暗を反映。

隔離Release警告・エラー各０。384×256に縮小した実原画で、保護画素と全画素のアルファ一致、２国旗の塗装差を確認。国旗320通りの回帰も通過。比較画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/flag-composite/maple-two-flags.png。目視では塗装下にも木目が残る。今回の比較は国旗地のみ。紋章、汚損、実行素材への登録は次回。

生成プロンプト

Create one unpainted pale maple wood toy robot HEAD sprite, based only on the top middle maple head of the reference design sheet. Preserve its three-quarter right view, rounded rectangular shell, pale inset face, two black oval eyes and curved black smile. All red and teal areas are now bare maple matching the right flank. Preserve continuous fine natural maple wood grain, smooth clean satin surface with very soft highlights. No paint, sticker, text, body, winding key, ground or shadow. One centered full object with margins, genuinely transparent background. Production game asset.

## 2026年11月11日23時48分・メープル頭を共通描画へ接続

素材９種モードで部位の素材IDを参照し、メープル頭を格納庫・工場などの共通描画に接続。正面の戦闘駒でも胴脚がPPなら使用できる。メープルの胴脚と別方向の頭、他素材の個別原画は未接続。ゲーム素材 Content/Images/Robots/maple-head-neutral-v001.png は NAS art-source/robots/maple-head-neutral-v001/ の同名原画とSHA-256一致（58F6C15CF04CE2C7C9C8E3D5CFA840E12B4EFBA5AAA4C9A21CCE98F4BD41B686）。

前回の木目マスクを共通化し、紋章と汚損を合成。隔離Releaseは警告・エラー各０。保護画素・アルファ、国旗320通り、実際の共通描画で２国旗と汚損、格納庫６枠の回帰が通過。確認画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/maple-game-glyph.png。戦闘の実時間再生は未検証。

## 2026年11月12日０時18分・メープルの胴と脚

v012のメープルを参照し、組み込みImageGenで胴と脚を独立した透過原画に制作。NAS art-source/robots/maple-body-neutral-v001/ からゲームの Content/Images/Robots/ へ同名で配置し、SHA-256を照合。

| 実行素材 | SHA-256 |
| --- | --- |
| maple-torso-neutral-v001.png | F4B79BE00F6ACF27F88AB229D0738DA4B5AAE8DD6F92A8018253FFFA8B7CA2F9 |
| maple-legs-neutral-v001.png | 97B19C4F8602A80E50E908A5D29A9D4D9D4FD79B2AD1524A1CDD0DD80E0668AA |

素材９種モードの共通描画と正面戦闘駒で使用。胴の同心円状の木目と靴の木肌を保護し、国旗、紋章、汚損を各部位へ合成。隔離Release警告・エラー各０。国旗２種類と汚損の全身を目視し、格納庫６枠・地星儀の回帰も通過。比較画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/glyph-integration/maple-game-glyph.png。メープルの横・背面と戦闘全画面の再生は未確認。

生成プロンプト

Create one independent transparent production game sprite based only on the top-middle maple toy of the reference. Show the isolated torso and both attached wooden arms. Preserve the broad inset end-grain on the chest with gentle concentric growth rings flush with the torso and no symbol. Keep pale satin maple and its visible fine natural grain, with the original toy proportions. All surfaces unpainted. No flag, emblem, text, winding key, head, other parts, floor shadow or background. Center the full part with clear margins.

Create one independent transparent production game sprite based only on the top-middle maple toy of the reference. Show the isolated joined waist and both legs with broad wooden shoes. Preserve a genuinely transparent gap between the legs, show the same three-quarter front angle and sturdy squared shoe shape. Keep pale satin maple and its visible fine natural grain, with the original toy proportions. All surfaces unpainted. No flag, emblem, text, winding key, head, other parts, floor shadow or background. Center the full part with clear margins.

## 2026年11月12日０時48分・メープル頭の横と背面

メープル正面頭を質感、PP横・背面頭を輪郭の参照にして、組み込みImageGenで独立した透過原画を制作。NAS art-source/robots/maple-views-v001/ に保存し、生成元とSHA-256一致を確認。

| 原画 | 寸法 | アルファ128以上の範囲（８px間隔の計測） | SHA-256 |
| --- | --- | --- | --- |
| maple-head-side-v001.png | 1536×1024 | 256,104–1232,904 | 47E8C768AD82F994A30A0D27727556C55A69F8EB62AFA52E41C7963E53AC9B3E |
| maple-head-back-v001.png | 1536×1024 | 312,152–1224,880 | B90234124EB4712B359F6A86715468A2D21C66B0A6D8B6C5853C5240A99C6643 |

横は右端に片目と口、背面は木目だけを目視した。今回は原画と透過の確認まで。次は横顔の保護マスクと国旗合成、横・背面の胴脚の制作。ゲーム素材への登録とビルドは次の作業。

Create a single transparent game sprite of the pale unpainted maple robot head in the first reference. Match its wood grain and soft satin finish. Use the second reference only to match the strict right-facing profile with one black oval eye and the edge of the smile visible on the right. No body, paint, badge, key, cast shadow, text or background. Keep the complete rounded block centered with margin.

Create a single transparent game sprite of the pale unpainted maple robot head in the first reference. Match its wood grain and soft satin finish. Use the second reference only to match the direct rear silhouette, a smooth wooden shell with no face visible. No body, paint, badge, key, cast shadow, text or background. Keep the complete rounded block centered with margin.

## 2026年11月12日１時18分・メープル頭の横・背面を戦闘へ接続

前節のNAS原画２点を Content/Images/Robots/ の同名PNGへ配置し、SHA-256一致を確認。素材９種モードでメープル頭とPP胴脚を組み合わせた戦闘駒に東・西・北向きの頭を適用。横顔の右端は木肌のまま残し、紋章は顔を避けて配置。西向きの紋章は文字や非対称形が反転しないよう既存の処理を利用した。

隔離Releaseでゲーム・描画試験とも警告とエラー各０。実描画で東西北と２国旗を確認し、横顔の保護領域と全画素のアルファが２国旗で一致。960×540の確認画像を目視し、格納庫６枠・選択・配送ドラッグの回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/maple-oriented-0118/ に試験出力。メープル胴脚の横・背面、戦闘全画面での再生、今回の向き別素材での国旗全種の画素比較は未実施。

## 2026年11月12日１時48分・メープル横向き胴脚の原画

正面メープルを木目と形、横向きPPを向きの参照に使用。組み込みImageGenで部位を個別に生成し、NAS art-source/robots/maple-views-v001/ に生成元とのSHA-256一致を確認して保存。

| NAS原画 | 寸法 | アルファ128以上の範囲（８px間隔） | SHA-256 |
| --- | --- | --- | --- |
| maple-legs-side-v001.png | 1312×1199 | 288,160–976,976 | 3F57A39DA742F53B8B9A3BF08B6423C3D727A92FB77CCE09DD404530009263D2 |
| maple-torso-side-study-v001.png | 1536×1024 | 432,136–1184,912 | CA9EC40C8F8D8052DD01AF4DF25F314C5EE61F57BACFA961257B6B5B37A8BCE3 |

両原画の左上は完全透過。目視で脚の木目と右向きの靴を確認。胴は腕が前へ伸び、輪郭に光のにじみが見えるため比較用として保存。別の胴出力はImageGenの出力審査でブロックされた。ゲーム用Contentへの登録とビルドは未実施。次は横向き胴の輪郭を揃え、脚の木肌を保つ塗装マスクと背面胴脚を準備する。

### 保存した出力の生成指示

- 胴。木製ぜんまい玩具の機械部品として、正面が画面右の側面視。丸い木製の拳付きの胴。正面メープルを木目と加工、PP横向きを視点とシルエットの参照にする。背景透過、床影・装飾なし。
- 脚。右向きの靴と短い木製支持部、腰の接続部を持つ独立した玩具部品。正面メープルの材質と横向きPPの視点を参照。輪郭の光・霧・床影を避け、背景を透過。

## 2026年11月12日２時18分・メープルの背面全身

正面メープルを木目と加工、PP背面を向きと輪郭の参照にし、組み込みImageGenで背面の胴と脚を個別に制作。NAS art-source/robots/maple-views-v001/ とゲームの Content/Images/Robots/ に同名で保存し、生成元とSHA-256一致を確認。

| 原画・実行素材 | 寸法 | アルファ128以上の範囲（８px間隔） | SHA-256 |
| --- | --- | --- | --- |
| maple-torso-back-v001.png | 1536×1024 | 96,136–1408,920 | 56F242FD6F1F28E1572F0A6B32FAF3EB4A9D90AD619F08211F7685021A4A585C |
| maple-legs-back-v001.png | 1536×1024 | 256,112–1280,928 | 89FBF6A380F4186B9BEE42D0C65E5E0B2061E3A83FCFF9C8769C61B4E697CE59 |

北向きで部位ごとにメープル素材を選び、木目の明暗を残す国旗と紋章を合成。かかとの下部は木肌。隔離Releaseのゲーム・描画・国旗試験は警告とエラー各０。国旗320組の合成、背面脚の全アルファ・脚間透過・木肌の２国旗比較、格納庫６枠の選択と配送ドラッグが通過。960×540の描画を目視。確認画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/maple-rear-0218/maple-oriented.png。横向きの胴と脚、背面の全旗での素材別比較、戦闘全画面での再生は未実施。

生成指示は木製ぜんまい玩具の機械部品を真後ろから描くもの。胴は横木目の背板と左右対称の短い腕、脚は左右のかかとと透過する脚間。両方とも無塗装の白いメープルで、背景・床影・装飾なし。

## 2026年11月12日２時48分・メープルの横向き全身

PP横向き胴の形とメープル背面の木目を参照し、組み込みImageGenで腕を胴の近くに収めた横向き胴を制作。既存の横向き脚とともにNAS原画 art-source/robots/maple-views-v001/ とゲーム実行素材 Content/Images/Robots/ のSHA-256を照合。

| 原画・実行素材 | SHA-256 |
| --- | --- |
| maple-torso-side-v001.png | 67491929A4BAA2C82DD8E9E0ADA202888D7D4914744FE80B68DE856EA1C3E8E7 |
| maple-legs-side-v001.png | 3F57A39DA742F53B8B9A3BF08B6423C3D727A92FB77CCE09DD404530009263D2 |

東西の戦闘駒で素材を部位ごとに選び、国旗と紋章を合成。西向きは紋章の向きを維持する既存処理を共有。足元は木肌を残す。胴の左右外と上方のうち計測した点でアルファ０。隔離Releaseのゲーム・描画試験は警告・エラー各０。実描画の東西北と２国旗、横顔・靴の保護画素と脚全体のアルファ一致、格納庫６枠の選択・配送ドラッグが通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/maple-side-0248/maple-oriented.png を960×540で目視。今回の向き別素材による国旗全種の画素比較と戦闘全画面の再生は未実施。次は実際の戦闘フレームで方向と装備を確認する。

## 2026年11月12日３時18分・戦闘全画面で小銃と方向を確認

前回の隔離ReleaseゲームDLLを参照し、RobotGlyphIntegrationに戦闘全画面の回帰を追加。２軍の模擬戦闘を用意し、メープル製一般兵が東向きで小銃１→東向きで小銃０→北向きへ変わる３フレームを BattleState.Capture に保存。実際の DrawRobotBattle を再生時間の３点で呼び、960×540の駒周辺の画素差と試験画像の目視で、小銃が消えてから後頭部が見えることを確認。格納庫６枠と配送ドラッグも通過。試験のReleaseは警告・エラー各０。製品コードと実行素材の追加変更なし。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/maple-replay-0318/ の maple-battle-frame-0〜2.png。試験は模擬戦闘の指定３点で、実時間の入力、戦闘開始から決着までの通し再生、他素材の実行素材は引き続き未検証。

## 2026年11月12日３時48分・シード固定戦闘の全201フレーム

RobotGlyphIntegrationに、シード42の平地節戦を追加。メープル２体とPP１体、両軍のクイーンを配置し、通常の BattleTurnResolver で国０が19ターンに勝利。生成された201フレームすべてについて、再生時刻から選ばれたフレーム番号と DrawRobotBattle の描画を確認した。960×540の開始・中間・決着の３枚を目視。既存の小銃・方向・格納庫６枠操作の回帰も通過。試験Releaseは警告とエラー各０。製品コードや素材の追加変更なし。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/maple-resolved-0348/ に maple-resolved-0.png、100.png、200.png（同じ接頭辞）。試験は再生時刻を順に指定したもので、実時間の操作と決着後の画面遷移は未検証。次は未対応の素材の実行素地と素材ごとの塗装マスクを進める。

## 2026年11月12日４時18分・紙製の頭の無地原画

採用済み head-study-v002 の右下の紙製頭を参照し、組み込みImageGenで独立した透過原画を描き起こした。角の折り返し、薄い継ぎ目、平らな面の陰影とクリーム色の顔を目視。国旗・紋章・ぜんまいは合成用の別要素とし、原画は無地。NAS art-source/robots/paper-head-neutral-v001/paper-head-neutral-v001.png、1485×1059。左上の背景アルファ０、顔中央254、右の折り目付近253。生成元とSHA-256一致（0FF83018C21C62CA67D33A9371A9CD773CA20C6A0183B4B0EC7BEC1EE48AD8AC）。

次は正面の折り紙面を多角形マスクで指定し、顔の丸い輪郭、白い角の折り返しと側面を保護する。実原画の目視上の基準点は正面外周がおよそ (407,290)、(934,294)、(998,425)、(998,755)、(938,855)、(393,850)、(333,748)、(333,419)。顔の範囲はおよそ (386,430)–(902,758)。これらはマスク作成前の目安。今回は原画までで、実行素材への登録・動的合成・ビルドは未実施。

生成指示は参照の右下の紙製頭を単体の透過スプライトとして描き起こし、斜めに折った角とクリーム色の顔、黒い目と笑顔を保持。赤と青緑を均一な無地のつや消し折り紙に置き換え、マーカーのむらや段ボールの波形、紋章、文字、床影を入れない。

## 2026年11月12日４時48分・紙の正面だけに国旗を合成

前節の無地原画に合わせ、RobotHeadSurface.PaperHead に８頂点の正面マスクと顔の保護領域を追加。正面UVは既存の５要素の国旗合成を使用し、明暗は紙の素地から取得。鏡面反射はゼロ。折り返しと側面は素地のまま残す。

隔離Releaseのゲームと画素試験は警告・エラー各０。1485×1059の実原画に斜め縞と格子を合成し、マスク外の画素と全画素のアルファが原画と一致。顔・折り目・側面の測定点と正面の着色も確認し、２枚を目視した。既存の国旗320組の回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-mask-0448/render/head-0.png と head-1.png。今回は国旗地まで。紋章の位置決定、Contentへの登録と共通描画への接続、格納庫での縮小確認は次回。

## 2026年11月12日５時18分・紙製頭を共通描画へ接続

NAS原画 paper-head-neutral-v001/paper-head-neutral-v001.png をゲームの Content/Images/Robots/ の同名素材へ配置し、SHA-256一致を確認（0FF83018C21C62CA67D33A9371A9CD773CA20C6A0183B4B0EC7BEC1EE48AD8AC）。素材９種モードで共通の頭部描画と正面戦闘駒に接続。白縁の紋章は顔に掛からない右下へ配置。横・背面は対応素材ができるまで既存の簡易戦闘駒を使用する。

隔離Releaseのゲーム・描画試験は警告とエラー各０。２国の紙製頭、正面の紙頭＋PP胴脚、顔と折り返しの測定画素、全アルファ一致を確認。960×540の確認画像を目視。格納庫６枠・選択・配送ドラッグとメープルの201フレーム試験も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-game-0518/paper-glyph.png。紙製頭の紋章は縮小時に小さく、紙の汚損と実格納庫に置いた状態での見え方は追加確認する。次は紙の胴脚と向き別素材。

## 2026年11月12日８時32分・紙製の胴と脚の無地原画

採用済みv012の右下を形と角度、実行素材の紙製頭を紙の色と折り目の参照にし、ImageGenで胴・脚を別々に制作。平らなつや消しの面、肩の折り返し、角張った靴を目視。NAS art-source/robots/paper-body-neutral-v001/ に保存し、生成元とSHA-256一致を確認。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| paper-torso-neutral-v001.png | 1536×1024 | 9AF759401D11126F750B04D3701DF374E8ED787866D8F9193B4F81540321C41B |
| paper-legs-neutral-v001.png | 1536×1024 | 46C4146E4DE1E53558DADB8A44DAE363F5F40E89CA92B67BDACEA2172721F9C6 |

両原画の左上と上方(768,160)はアルファ０。脚間(737,700)にはアルファ３の残りがあり、実行素材化の際に暗色背景で確認する。胴の正面はおよそ(451,313)、(992,314)、(945,809)、(343,799)。脚はすねの正面２面を塗り、靴先と側面は紙のまま残す。次は胸・袖・すねの国旗マスクと紋章の配置、実行素材への接続。今回は原画までで、ゲーム側の変更とビルドは未実施。

生成指示は参照の紙製ロボットから、台形の胴と下向きの袖、または短い腰と二つの角靴を単体で描くもの。無地のアイボリー、折り目と平面の陰影、透明背景。

## 2026年11月12日９時02分・紙製胴脚の国旗マスク

前節のNAS原画に合わせ、胸と袖の４面、左右のすねの２面を多角形で指定。頭と共通の紙用合成を使い、肩の折り返し、胴の側面、靴先は無地の紙を残した。既存国旗を同じ座標系で合成し、素地の明暗とアルファを保持する。

隔離Releaseは警告・エラー各０。胴脚の実原画に２種を合成し、保護画素と全アルファが原画と一致。国旗320組の回帰と胸・袖・すねの測定点も通過。斜め縞の２部位を目視し、折り目と靴先に色が回り込まないことを確認した。NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-body-mask-0902/ の Torso と Legs に各２枚。脚間の微小アルファは保存される。実行素材への登録、紋章・汚損、暗色背景の格納庫と戦闘駒での縮小検証は次回。

## 2026年11月12日９時27分・紙製の正面全身を共通描画へ接続

前節の胴・脚を Content/Images/Robots/ に原画と同名で配置し、SHA-256一致を確認。素材９種モードで部位ごとに紙を選び、正面の共通描画と戦闘駒に国旗・紋章・汚損を合成。胴の胸と脚の右足に紋章の位置を設定。横・背面は簡易駒を使用する。

隔離Releaseのゲーム・描画試験は警告・エラー各０。紙の胴と脚は汚損で着色画素が変わり、全アルファが一致。暗色背景の960×540で紙の全身・汚損・正面戦闘駒を目視し、脚間に目立つ残りは見えなかった。格納庫６枠と配送ドラッグ、戦闘201フレームの回帰が通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-body-0927/paper-glyph.png。紙製全身を実格納庫に置いたときの紋章の視認性と、別方向の素材は未検証。

## 2026年11月12日９時44分・紙製の紋章を格納庫６枠で確認

既存の隔離Release DLLで DrawNodeAssembly を使用し、紙製全身６体を汚損あり・なしで交互に表示。960×540で紋章の有無を比較し、全６枠で画素差を確認。保存画像を目視し、胸と足の白縁は確認できたが、頭と足では紋章の形の判別が難しい。格納庫の選択・配送ドラッグ、戦闘201フレームの回帰も通過。製品描画の変更なし。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-hangar-0944/paper-hangar.png。次は紋章の余白と横・背面の素材を確認する。

## 2026年11月12日９時59分・紙製の頭と靴の紋章を拡幅

頭の紋章を顔の下へ移し、靴は右足のつま先正面に収めた。頭の幅を原画の5.2%から7.8%、靴の幅を9%から11.6%に変更。960×540の格納庫６枠を再描画して目視。顔と靴の折り目は見え、白縁も確認できるが、この解像度では紋章の細部は依然小さい。

隔離Release警告・エラー各０。全６枠の紋章画素差、選択・配送ドラッグ、戦闘201フレームの回帰が通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-sticker-0959/paper-hangar.png。次は紙製の横・背面の素材。

## 2026年11月12日10時14分・紙製頭の横と背面

正面の紙製頭を形・材質、メープルの横・背面をカメラ角度の参照にして、ImageGenで紙製頭を別々に制作。横は右端に片目、背面は無地の八角面。白い折り返しとつや消しの紙を目視した。NAS art-source/robots/paper-views-v001/ に保存し、生成元とSHA-256一致を確認。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| paper-head-side-v001.png | 1536×1024 | 42BFA50FE2006BD12F36FAAA01631AB463DA636BC4F7BBA30D149C5DA73DAEF3 |
| paper-head-back-v001.png | 1536×1024 | DD54A6318975C6CFF04D7EF83CCDC34F84FE1C4131BD31CD2F701A94E7D093F1 |

２点とも(0,0)と(250,500)はアルファ０、(768,500)は254。次は横の中央矩形と背面八角面のマスク、横顔を避けた紋章、横・背面の胴脚。ゲーム用登録と描画ビルドは未実施。

## 2026年11月12日10時29分・紙製頭の横と背面の国旗マスク

横の中央矩形と背面八角面に国旗を合成し、折り返しと横顔は保護。紋章を顔から離れた右下に置いた。NASの実原画で斜め縞と格子を合成し、保護領域と全画素のアルファの一致を確認。斜め縞の横・背面を目視。隔離Releaseは警告・エラー各０で、国旗320組の回帰も通過。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-head-views-1029/ の Side と Back に比較画像。今回は合成まで。ゲーム用登録、縮小時の表示、横・背面の胴脚は次回。

## 2026年11月12日10時44分・紙製頭を東西北の戦闘駒へ接続

NAS paper-views-v001 の横・背面頭２点を Content/Images/Robots/ に同名で配置しSHA-256一致を確認。紙製頭とPP・メープルの胴脚の組み合わせで東西北を描画。西向きは既存の紋章反転補正を使用。紙製の胴脚を含む横・背面は引き続き簡易駒。

隔離Release警告・エラー各０。東西北の描画と素材読込、正面の保護画素、格納庫６枠の選択と配送、戦闘201フレームの回帰を確認。隔離試験の新聞・人物画像を補った再実行で終了０。960×540の紙製頭の東西北を目視。NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-head-game-1044/paper-glyph.png。次は紙製の横・背面の胴と脚。

## 2026年11月12日10時59分・紙製胴と脚の横向き原画

紙の正面素材を材質と折り目、メープルの横向き素材を角度の参照にし、ImageGenで胴と脚を個別に制作。片袖、手前のすね、前後に重なる角靴を目視。NAS art-source/robots/paper-views-v001/ に保存し生成元とSHA-256を照合。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| paper-torso-side-v001.png | 1536×1024 | 5021BA8408FB9CFEB0800DC27510E4FF976107C37E0B8C6593FC9EBE43BD9104 |
| paper-legs-side-v001.png | 1312×1199 | A61FDF92AAE7D9C0592315440363E5BEDB7B2FE38FBB7367356D8B2FF6F575F1 |

背景(0,0)・(140,500)のアルファは両方０、素地(550,550)は胴253・脚254。次は胴の広い側面と袖、すねの紙面をマスクし、折り返しと靴を保護する。脚は1536×1024と異なるため、マスクと切り出しに固有寸法を使う。ゲーム登録、ビルド、背面胴脚は未実施。

## 2026年11月12日11時14分・紙製の横向き胴脚に国旗を合成

横向きの胴の側面と袖２面、脚のすね２面を多角形で指定。脚には原画固有の1312×1199座標を使い、折り目・肩・靴先を保護。隔離Release警告・エラー各０。２種類の国旗の合成で保護画素と全アルファの一致を確認し、斜め縞の仕上がりを目視。国旗320組の回帰も通過。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-side-mask-1114/ の Torso と Legs に出力。今回は合成まで。紋章・汚損、実行素材への登録、戦闘駒と格納庫の縮小確認、背面胴脚は次回。

## 2026年11月12日11時29分・紙製の東西向き全身

NAS paper-views-v001 の paper-torso-side-v001.png と paper-legs-side-v001.png を Content/Images/Robots/ に同名配置し、前節の原画とSHA-256一致を確認。東西の戦闘駒で部位ごとに紙を選び、胴と靴に紋章を配置。西向きは既存の紋章反転補正を使う。紙の横向き脚ではすねに汚損の絆創膏を配置。

隔離Release警告・エラー各０。東西の紙製全身、汚損前後での胴・脚の全アルファ一致と着色画素差、格納庫６枠と配送ドラッグ、戦闘201フレームの回帰が通過。960×540で縮小された紙製駒の折り目・靴先を目視。NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-side-game-1129/paper-glyph.png。北向きで紙製胴脚を含む場合は簡易駒。次は背面の胴脚を制作する。

## 2026年11月12日11時59分・紙製胴脚の背面原画

ImageGenで制作した紙製の背面胴と背面脚を目視。胴の背板と左右の袖、脚の折り返しと角靴を確認した。NASの art-source/robots/paper-views-v001/ に保存し、生成元とのSHA-256一致を確認した。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| paper-torso-back-v001.png | 1536×1024 | F6FD3E1733C7BF953642503AABF58D16B8752AAC38C1AB28DC2DC3D0BE1D8F24 |
| paper-legs-back-v001.png | 1536×1024 | 83E4E42E4183A703C2E1F4062DBBAF2668FB0362D761F233BB8081A6FC4002F2 |

両方の左上と脚間(768,760)はアルファ０、胴の同座標は254。次は背板・袖・すねの国旗マスクを指定し、折り返しと脚間を保護したうえで北向き戦闘駒に接続する。今回は原画の保存と確認まで。ゲームへの登録・ビルド・描画回帰は次回。

## 2026年11月12日12時14分・紙製の背面胴脚に国旗を合成

paper-views-v001 の背面胴と脚に、背板と両袖の３面、左右のすねの２面の多角形マスクと国旗のUV範囲を設定。肩の折り返し、腰、靴、脚間を保護した。斜め縞の出力を目視し、紙の折り目と縁を確認した。

NASへ隔離したゲームと合成プローブのReleaseビルドは各警告０・エラー０。既存国旗320組の回帰と、背面胴脚の実原画２点に２旗を合成する画素試験を通過。保護領域の色と全画素のアルファは入力と一致。NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-back-mask-1214/ の Torso と Legs に比較画像。次は実行素材を登録し、紋章と汚損を含む北向きの戦闘駒に接続する。戦闘画面と格納庫の回帰はその時点で行う。

## 2026年11月12日12時29分・紙製の北向き戦闘駒

NASの paper-views-v001 から paper-torso-back-v001.png と paper-legs-back-v001.png を Content/Images/Robots/ に同名で登録し、SHA-256を照合した。北向きの戦闘駒で部位ごとに紙製素材を選び、国旗・紋章・汚損を共通合成で描画する。紋章は背板と画面右の靴に配置した。

隔離Releaseのゲーム・描画プローブは警告・エラー各０。紙製全身の東西北、背面胴脚の汚損による画素差とアルファ一致、960×540の格納庫６枠の選択・ページ送り・ドラッグ、戦闘201フレームの回帰が通過した。縮小表示で背板・袖・靴の区別を目視した。紋章の細部は戦闘駒の大きさでは小さい。比較画像は NAS tmp/delete-after-2026-11-15/robot-art-implementation/paper-back-game-1229/paper-glyph.png。次は未対応素材の素地を制作する。

## 2026年11月12日12時44分・段ボール製の正面頭

採用見本 material-study-v012 の段ボール製を参照し、ImageGenで正面寄りの無地の頭を制作。クラフト紙の顔面と露出した波形断面を目視した。NAS art-source/robots/cardboard-views-v001/cardboard-head-neutral-v001.png に保存し、生成元とSHA-256一致を確認。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| cardboard-head-neutral-v001.png | 1536×1024 | 08C6A3A2F76D9D282B793C6AEABCC7B06A5EF2850814226AC1760A4D83F31079 |

アルファは左上と頭上(768,50)で０、顔面(650,350)で253。次は正面パネルの着色マスクを作り、目・口と波形断面を保護し、暗色背景と縮小時の識別性を検証する。今回は原画まで。ゲーム登録とビルドは未実施。

## 2026年11月12日12時59分・段ボール製頭の国旗マスク

cardboard-head-neutral-v001.png に正面の丸角マスクとUVを設定し、目・口と波形断面を保護。クラフト紙の明暗を使い、鏡面反射は加えない。斜め縞の合成を目視し、紙繊維と断面が残ることを確認。口の周囲には保護したクラフト紙が見える。

ゲームと合成プローブの隔離Releaseは警告・エラー各０。プローブのローカル名の重複を修正して再ビルドした。国旗320組と実原画への２旗の合成が通過し、保護画素と全画素のアルファが入力と一致。比較画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-head-mask-1359/render/。次は紋章と汚損を共通描画に接続し、縮小時の識別性と外周の微小な透過画素を確認する。

## 2026年11月12日13時14分・段ボール製の正面頭をゲームへ接続

NAS cardboard-views-v001 の cardboard-head-neutral-v001.png を Content/Images/Robots/ に同名で登録し、SHA-256一致を確認。素材９種モードで格納庫と共通描画、正面の戦闘駒が段ボール製頭を使用する。紋章は顔の右下、汚損は国旗マスク内に合成。横・背面の段ボール製頭は簡易駒を使う。

隔離Releaseのゲーム・描画プローブは警告・エラー各０。段ボール製頭の汚損による画素差とアルファ一致、960×540の６枠の操作、戦闘201フレームの回帰を確認。縮小表示で暗色背景上の波形断面を目視し、外周に目立つ残りは見えなかった。紋章の細部は戦闘駒では小さい。NAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-head-game-1314/cardboard-glyph.png。次は段ボール製の胴脚と別方向の素地。

## 2026年11月12日13時29分・段ボール製の正面胴と脚

素材見本v012と段ボール製の正面頭を参照し、無地の胴と脚を別々に制作。胸・袖・すねの平面と、腰や靴の波形断面を目視した。原画をNAS art-source/robots/cardboard-views-v001/ に保存し、生成元とSHA-256一致を確認。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| cardboard-torso-neutral-v001.png | 1536×1024 | EAFD6D0FA3C383AD4EE795255328BC384B32E0505815397F0C7D19CA90D423FB |
| cardboard-legs-neutral-v001.png | 1536×1024 | AB143222F8D1D54973C170BB84840AB5AAED81CEE17E5C94D744B31486369E36 |

両方の左上と脚間(750,550)はアルファ０、素地(550,550)は両方253。次は胸・袖と左右のすねに国旗マスクを指定し、波形の穴と靴を保護する。ゲーム用登録と描画回帰は未実施。

## 2026年11月12日13時44分・段ボール製の正面胴脚の国旗マスク

cardboard-views-v001 の正面胴脚に、胸・左右の袖・左右のすねの着色多角形とUVを設定。紙製と共通の平面マスク処理でクラフト紙の明暗を残し、肩、波形の穴、腰、靴と脚間を保護した。斜め縞の両部位を目視した。実原画の輪郭にわずかな半透明画素があり、ゲームの縮小表示で確認する。

隔離Releaseのゲームとプローブは警告・エラー各０。国旗320組と実原画２点の２旗合成が通過。保護画素と全アルファが入力と一致し、共通化前後の紙製背面胴脚は２旗ともRGBAがバイト単位で一致した。NAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-body-mask-1344/ に比較画像。次は実行素材登録、紋章・汚損と格納庫・正面戦闘駒への接続。

## 2026年11月12日13時59分・段ボール製の正面全身

NAS cardboard-views-v001 の cardboard-torso-neutral-v001.png と cardboard-legs-neutral-v001.png を Content/Images/Robots/ へ同名で登録し、原画とのSHA-256一致を確認。素材９種モードの格納庫と正面戦闘駒で部位別に段ボールを選び、胸と右すねの紋章、着色面内の汚損を合成する。段ボールを含む横・背面の戦闘駒は簡易描画。

ゲームと描画試験の隔離Releaseは警告・エラー各０。頭・胴・脚の汚損による画素差とアルファ一致、格納庫６枠と配送ドラッグ、戦闘201フレームの回帰を確認。960×540で波形断面、脚間、絆創膏を目視。暗色背景で目立つ外周の残りは見えなかった。段ボールの紋章の細部はこの大きさでは未確認。NAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-body-game-1359/cardboard-glyph.png。次は段ボール製の横・背面の原画。

## 2026年11月12日14時14分・段ボール製の横・背面頭

段ボール製の正面頭を材質、紙製頭の横・背面を角度の参照にして、無地原画を別々に制作した。横は右端に片目と広い側面、背面は左右対称の無地面。両方の波形断面を目視した。NAS art-source/robots/cardboard-views-v001/ に保存し、生成元とのSHA-256一致を確認。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| cardboard-head-side-v001.png | 1536×1024 | 3473F78A76EAF0A839252703F7DFD59C21ABC80EC15ACFF66C513A592693AB81 |
| cardboard-head-back-v001.png | 1536×1024 | 0DFE4B4F30958D6145D5F0E7B0E72126C8A8AB75BE4EDBF074EF1342DCB3E724 |

両方の左上と(768,50)はアルファ０、パネル内(768,520)は253。次は横顔と波形断面を保護する国旗マスクを作る。ゲーム登録、ビルド、横・背面の胴脚は未実施。

## 2026年11月12日14時29分・段ボール製頭の横・背面国旗マスク

cardboard-head-side-v001.png と cardboard-head-back-v001.png の平面に別々の丸角マスクとUVを指定。横顔と片目、波形の穴を着色範囲の外に置いた。斜め縞を目視し、クラフト紙の繊維と無地の縁を確認した。外周の微小な半透明画素は縮小表示で再確認する。

隔離Releaseのゲームとプローブは警告・エラー各０。実原画２点の２旗合成で保護画素と全アルファが入力と一致し、国旗320組の回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-head-views-1429/ の Side と Back に比較画像。次は紋章と汚損を配置して東西北の戦闘駒に接続する。

## 2026年11月12日14時44分・段ボール製頭を東西北の戦闘駒に接続

cardboard-views-v001 の横・背面頭２点を Content/Images/Robots/ に同名登録し、原画とSHA-256一致を確認。段ボール製頭の東西北で国旗・紋章・汚損を合成。西向きは既存の紋章の反転補正を使う。段ボール製の胴または脚を含む横・背面は簡易駒。

隔離Releaseのゲーム・描画プローブは警告・エラー各０。段ボール頭とメープル胴・紙脚の東西北、汚損時の色差とアルファ一致、960×540の６枠操作、戦闘201フレームの回帰を確認。縮小時に段ボールの縁と横顔を目視。比較画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-head-game-1444/cardboard-glyph.png。次は横・背面の胴脚。

## 2026年11月12日14時59分・段ボール製の横向き胴脚原画

段ボール製の正面胴脚を材質と形、紙製の横向き胴脚を角度の参照として個別に制作。片袖と広い胴側面、手前と奥のすねと靴、波形の穴を目視した。NAS art-source/robots/cardboard-views-v001/ に保存し、生成元とSHA-256一致を確認した。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| cardboard-torso-side-v001.png | 1536×1024 | 88715498CD4B3ECF508F7B7DF1B12FA8393B49E28F524138A4F3132019F8B62C |
| cardboard-legs-side-v001.png | 1312×1199 | D9A4524B20F64AE726B6703195CBFD13D25C7CCFABB2F03FFF8D0BE19D536072 |

両方の左上と(140,500)はアルファ０、素地(550,550)は253。次は胴側面、袖、すねの国旗マスクを作る。脚は固有の1312×1199座標を使用する。外周の微小な半透明画素は縮小表示で確認する。実行素材登録とビルドは未実施。

## 2026年11月12日15時14分・段ボール製の横向き胴脚に国旗を合成

cardboard-torso-side-v001.png の胴側面と袖、cardboard-legs-side-v001.png の手前と奥のすねに多角形マスクとUVを設定。脚は原画固有の1312×1199座標を使用。斜め縞の出力を目視し、肩、波形断面、靴の紙地を確認した。輪郭の微小な半透明画素は戦闘駒の縮小表示で再確認する。

ゲームと合成試験の隔離Releaseは警告・エラー各０。国旗320組の回帰と２部位への２旗の合成が通過し、保護画素と全アルファが入力と一致。NAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-side-mask-1514/ の Torso と Legs に出力。次は実行素材の登録、紋章と汚損、東西向き戦闘駒への接続。

## 2026年11月12日15時29分・段ボール製の東西向き全身

NAS cardboard-views-v001 の cardboard-torso-side-v001.png と cardboard-legs-side-v001.png を Content/Images/Robots/ に同名登録し、SHA-256一致を確認。東西向きで胴と脚を部位別に選び、胴側面と手前のすねに紋章、着色面内に汚損を合成した。北向きで段ボール製胴脚を含む場合は簡易描画。

隔離Releaseのゲーム・描画試験は警告・エラー各０。東西向きの全身、汚損の画素差と全アルファ一致、格納庫６枠と配送ドラッグ、戦闘201フレームの回帰が通過。960×540で袖・すね・靴の波形の縁を目視。紋章の細部は戦闘駒のサイズでは未確認。比較画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-side-game-1529/cardboard-glyph.png。次は背面の胴脚。

## 2026年11月12日15時44分・段ボール製の背面胴脚原画

段ボール製正面胴脚の材質と、紙製背面胴脚の角度を参照して個別に制作。背板と両袖、左右のすね・かかと、波形断面を目視した。NAS art-source/robots/cardboard-views-v001/ に保存し、生成元とSHA-256一致を確認した。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| cardboard-torso-back-v001.png | 1536×1024 | 2B5D0C00B98296B9020EED65F5080C6DA743D7C13879D6DBCA08B581C8B145A6 |
| cardboard-legs-back-v001.png | 1536×1024 | B82B2145487C94CA360E6327FAC49097A8E6BFA8D82CC1A4D0A8FD17D2A579E2 |

両方の左上と脚間(768,760)はアルファ０、素地(550,550)は両方253。次は背板・袖・すねの国旗マスクと北向き戦闘駒への登録。今回は原画の保存と確認まで。

## 2026年11月12日15時59分・段ボール製の背面胴脚の国旗マスク

背板、両袖、左右のすねに多角形とUVを指定。斜め縞を目視し、波形断面・腰・かかとのクラフト紙と脚間を確認した。ゲーム・合成プローブの隔離Releaseは警告・エラー各０。実原画２点に２旗を合成し、保護画素と全アルファが入力と一致。国旗320組の回帰も通過した。

NAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-back-mask-1559/ の Torso と Legs に比較画像。次は実行素材へ登録し、紋章・汚損を含む北向き戦闘駒に接続。外周の微小な半透明画素は縮小表示で確認する。

## 2026年11月12日16時14分・段ボール製の北向き全身

cardboard-views-v001 の cardboard-torso-back-v001.png と cardboard-legs-back-v001.png を Content/Images/Robots/ に同名登録し、原画とのSHA-256一致を確認。北向きで頭・胴・脚を部位ごとに選び、背板と右すねに紋章、着色面に汚損を合成する。

隔離Releaseのゲーム・描画プローブは警告・エラー各０。段ボール全身の東西北、汚損の色差と全アルファ一致、格納庫６枠の操作と戦闘201フレームの回帰を確認。960×540で背板と袖、かかとの波形断面を目視。紋章の細部は縮小表示で判別しにくい。NAS tmp/delete-after-2026-11-15/robot-art-implementation/cardboard-back-game-1614/cardboard-glyph.png。次は発泡スチロール製の素地。

## 2026年11月12日16時29分・発泡スチロール製の正面頭原画

素材見本v012の発泡スチロール製を参照して、白い無地の正面頭を制作した。外殻の粒の継ぎ目、目と口のあるはめ込み面を目視した。NAS art-source/robots/foam-views-v001/foam-head-neutral-v001.png に保存し、生成元とのSHA-256一致を確認した。1536×1024、SHA-256 A95182861AB3FBDDB166079B731883B890B2A084CA040EFB617E0D39095369B1。

左上と頭上(768,65)はアルファ０、外殻(650,290)は252。次は顔を保護するマスクと、白い粒の明暗・塗りむらを残す国旗合成を検証する。実行素材登録とビルドは未実施。

## 2026年11月12日16時46分・発泡スチロール製頭の国旗マスク

正面頭に顔を保護する丸角マスクと外殻のUVを設定。粒の明暗を使い、明るい粒の頂点では着色を弱め、緩やかな塗りむらを加えた。鏡面反射は加えていない。斜め縞の出力を目視し、白い顔、外殻の粒と継ぎ目を確認。

隔離Releaseはゲーム・合成プローブとも警告・エラー各０。２旗で保護画素と全アルファが一致し、国旗320組の回帰も通過。NAS tmp/delete-after-2026-11-15/robot-art-implementation/foam-head-mask-1646/render/ に比較画像。次は紋章と汚損を共通描画に接続し、縮小時の粒と輪郭を確認する。

## 2026年11月12日17時01分・発泡スチロール製頭をゲームへ接続

NAS foam-views-v001 の foam-head-neutral-v001.png を Content/Images/Robots/ へ同名登録し、SHA-256一致を確認。素材９種モードの共通描画と正面戦闘駒で、国旗、額の紋章、着色面の汚損を合成。塗りむらのある外殻に汚損を適用するため、発泡スチロール用の着色マスク閾値を設定した。横・背面は簡易駒。

隔離Releaseはゲーム・描画プローブとも警告・エラー各０。汚損で外殻の色が変わり、顔の測定点と全アルファが一致。格納庫６枠、配送ドラッグと戦闘201フレームの回帰も通過した。960×540で粒の質感と白い顔を目視。紋章の細部は縮小表示では判別しにくい。NAS tmp/delete-after-2026-11-15/robot-art-implementation/foam-head-game-1701/foam-glyph.png。次は発泡スチロール製の正面胴脚。

## 2026年11月12日17時16分・発泡スチロール製の正面胴脚原画

素材見本v012と発泡スチロール製の正面頭を参照し、白い胴と脚を個別に制作。胸と袖、腰と左右の靴、粒の継ぎ目を目視した。NAS art-source/robots/foam-views-v001/ に保存し、生成元とのSHA-256一致を確認。

| 原画 | 寸法 | SHA-256 |
| --- | --- | --- |
| foam-torso-neutral-v001.png | 1536×1024 | C320C54225E55ACBED7C43E94FE1BE6B6917F884781ACF3A8D46CFE70AE69822 |
| foam-legs-neutral-v001.png | 1536×1024 | 860D89BF1507F9B2C07DC43CD45D4F0E5DBCE7A4ACD283D75822174D4230D469 |

両方の左上と脚間(755,590)はアルファ０、素地(550,550)は両方253。次は粒の明暗を残す胴脚の国旗合成。今回は原画の保存まで。ゲーム登録、ビルド、描画回帰は未実施。

## 2026年11月12日17時31分・発泡スチロール胴脚の国旗合成

胸、両袖、左右のすねのマスクとUVを設定し、頭と共通の着色処理で白い粒の明暗と塗りむらを残した。肩、袖口、腰、靴は白地。斜め縞の出力を目視し、胴原画の輪郭の外側に半透明の縁が見えた。実行素材の登録前に暗色背景で縮小して確認する。

隔離Releaseは警告・エラー各０。国旗320組と、胴脚の実原画に２旗を合成する画素試験が通過し、保護画素と全アルファが入力と一致。既存の頭の２旗の出力はバイト単位で一致した。比較画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/foam-body-mask-1731/。次は輪郭確認、紋章・汚損とゲーム描画への接続。

## 2026年11月12日17時47分・発泡スチロール胴脚の縮小確認

17時31分の斜め縞合成を暗色背景（RGB 24, 30, 40）に置き、素材画像の幅126、84、42pxの３段階で目視した。胴の外周に目立つ半透明の縁は見えず、幅42pxでも胸・袖と脚間を判別できた。小さい表示では粒の細部が弱くなる。比較画像はNAS tmp/delete-after-2026-11-15/robot-art-implementation/foam-body-mask-1731/foam-dark-scale-preview.png。今回は縮小比較のみ。次は同じ原画を実行素材へ登録し、紋章・汚損を共通描画へ接続して格納庫と戦闘の実寸で確認する。
