# 内政開始時のガイド

2026-11-21実装。各国専属のGuideが人間の内政操作開始時に登場する。新聞・外交・CPUの操作演出が終わった後、通常の地図入力より先に案内する。既存チュートリアル、自動操縦、デモ、コンピューターのみのゲームには割り込まない。

## プレイヤーの操作

- 初回は世界征服の目標、所属陣営の支配地域・地点数と順位、独立したライバル陣営数、クイーン捕獲の危険、袋小路や輪の一帯の確保、最初の一手を６ページで案内する。
- 翌日以降は現在の地域数・順位、共通の指南２ページ、提案の４ページ。前日の変化の因果分析や勝利確率を生成する機能ではない。
- 表示するボタンは吹き出し内の［次へ］だけ。最終ページでは案内終了になる。Enterでも進み、Escは非表示のショートカットとして途中終了できる。［前へ］とページ番号は表示しない。
- 同じ国の同じ全体ターンでは繰り返さない。初回既読と最終案内ターンはワールドへ保存する。
- 提案は、敵国への開戦対象を持つ自由用途型の自国地点があれば城壁への変更を検討するもの。港・最終処分場などの永続ランドマーク、無機能地点、中継地点などを変更対象にしない。
- 最終ページの次へで、提案対象があればその庁舎、なければ計略トレイを開く。用途変更・予約・命令は自動適用しない。

## 台詞の編集場所

`src/Shared/Messages/Guide/Domestic.json`へ本文、役割名、ボタン表記を集約。各キーの文字列配列が１ページの行になる。`{conquerorName}` `{ownedRegions}` `{totalRegions}` `{ownedNodes}` `{rank}` `{rivals}` `{nodeName}`を実データで置換する。

JSONは埋め込みリソースなので変更後はビルドする。未知/未供給の差し込み項目は黙って表示せずエラーとする。FontCoverageがJSONの文字列値も検査する。新たに必要だった「嬢」「険」をUIフォントへ追加した。

どの台詞を選ぶかは`GuideNarration.Build`、何と話すかはJSON、画面・入力は`Game1.Guide.cs`に分ける。勝利条件は現行の配下を含む全地域支配を説明し、「ライバル全員を倒すだけで勝利」とは説明しない。戦場で倒された後の捕獲/処遇と、即敗退も区別する。

## 国ごとの担当と画像

`WorldSetup.Guides`に名前、素体番号、初回既読、最終案内ターンを保存。名前は[ガイド名の由来](guide-names.md)の独立生成器、素体は４種類から抽選。WorldSeedとCastSeedに基づく専用乱数を使う。旧保存にガイド情報が無い場合は最初の案内時に補完し、保存済みの名前は再抽選しない。

ゲーム用の透過原画４種と共通ネームプレートは`FolderVerse/Content/Images/Guides/`。重要原画・制作条件はNAS `archives/artwork/guides/2026-11-21-v001/`を保持する。

衣服の中立色部分へ国旗の主色・副色・模様を動的に反映する。肌・髪の有彩色と靴を保護する色域/位置判定方式で、専用の手描き衣服マスクではない。白手袋も衣服側として着色する。国旗の紋章そのものは制服には貼らない。画像を切り替えると旧テクスチャを破棄し、60国分の大きな画像を同時キャッシュしない。

## 検証

`tests/GuideRuntime`で名前/素体、初回４ページと日次２ページ、差し込み、案内によるモデル非破壊性、保存復元、ランドマーク除外、自動操縦除外、同ターンの再登場防止、Esc/Enter、計略トレイへの導線の18項目を確認。４素体/４ページのFHD画像を生成し、文字・枠・全身と色替えを目視確認した。

最初の描画試験は配布版の古いフォントを読んで「嬢」「険」で失敗した。新規ビルドしたUIフォントを隔離試験へ渡した後に全項目を再実行して通過。色替えの矩形境界が服に出た初稿は、衣服の中立色を連続して扱う形へ修正した。

証拠はNAS `archives/diagnostics/guide-runtime-2026-11-21/`。再生成ビルドは`tmp/delete-after-2026-11-22/guide-runtime`、`guide-runtime-tests`、`guide-runtime-font`。実行中ゲーム・元保存・公開物を操作せず独立ハーネスで確認した。通常の新聞/外交から連続して実ゲームを通す試遊は未実施。


## 現在の表示（2026-11-21改訂）

ガイドはHUDとして手前へ置く。左1/3（640×1080）には現在地の監督原画から人物を除いた景色をアスペクト比を保って切り出し、右側は宇宙と地星儀を表示する。灰色の矩形・全面暗幕は使わない。専用の描画経路でシステムメニュー、計略トレイ、地図ボタン、その他のボタンを描かない。入力もガイドが消費する。

画面上側の吹き出しは白系の楕円と、ガイドへ向く尾。影を右下に２層重ねる。文字は楕円の内側の余白に収め、次へはその中に置く。ボタン外枠と緑のランプがゆっくり明滅する。楕円はコードで生成するUI素材で、地点背景はbuilt-in image_genによる画像。

名札は280×94、中心（160,720）。文字は実際のグリフ範囲で縦中央へ揃え、名前のサイズを維持し長名は２行にする。[人物HUDの画面規格](portrait-hud.md)参照。

人物なし背景14種はContent/Images/Guides/background-*-v001.png。NodeSupervisors.Forと同じ判定で現在地から選び、現在地点が特定できなければ同地域内地点、さらに地形の種類から補完する。１背景のみキャッシュし、切替時に前のテクスチャを破棄する。原画・各参照・共通プロンプト・SHA256はNAS archives/artwork/guides/backgrounds-2026-11-21-v001。art-source/index.htmlから閲覧可能。

産業ルーレット室も同フォルダーへ追加制作した。採用済みの木製・真鍮ルーレットと地点背景を参照した人物なしの部屋。産業フェーズの左640×1080背景として組み込み済み。司会もガイドと同じ人物領域と共通吹き出しを使う。

検証はNAS archives/diagnostics/guide-balloon-2026-11-21、ビルドはtmp/delete-after-2026-11-22/guide-balloon・guide-balloon-tests。14背景の人物残りを目視。独立UIで４素体・楕円・名札・入力・保存復元を確認。検証初回のカメラ参照はプロパティをフィールドとして読んで失敗したため、検証側だけ修正して再実行した。
## Follow-up validation 2026-11-22 00:33:32.219015 JST

- HEADf0c4b6b3, user changes preserved; no game code changed. Built current FolderVerse in existing C fallback folderverse-audit (prior NAS outage, retainNov22), limit600s PID17360 00:31:14-49 exit0 warnings0/errors0/FontCoverage1088 PASS; guide-v014-build.log. GuideRuntime references this exact new DLL via GameDll/RuntimeDir; limit600s PID15284 00:32:08-09 exit0, guide-v014-test-build.log.
- dotnet same/bin/GuideRuntime/debug/GuideRuntime.dll, GAME_RUNTIME/GUIDE_ASSET_ROOT=current game output, GUIDE_OUTPUT=C work artifacts/guide-v014-check, null audio. Native SDL2/openal copied only between own validation outputs. PID5220 00:32:36-43 limit300s exit0, guide-v014-test-run.log, all38 assertions pass; no TIMEOUT/residual process. Four1920x1080 body/page renders viewed: oval balloon, labels, background/live globe and next/end button. Names/save state/narration nonmutation/landmark exclusion/autoplay exclusion/Escape/Enter/no-repeat/next-day/last-page tray transition covered.
- Demo/tutorial/CPU-only exclusion checked in GuideInput source; these three modes not newly exercised by this fixture. Ordinary title/newspaper-to-guide continuous OS playthrough, small resolution, office-target continuation and fortune two-handle real-input regression remain unverified. Do not equate diagnostic input calls with physical OS input or old fortune defaults with headless25/25.
- Evidence C:\Users\muzud\.codex\visualizations\2026\10\30\01a18ce8-f326-7083-b8e7-ba69c77ea956\artifacts\guide-v014-check\guide-0.png SHA256 71fff5f3c3171b9e21bbd38e0a18432e29c3486915c68b1b97905ca66da29dfe
- Evidence C:\Users\muzud\.codex\visualizations\2026\10\30\01a18ce8-f326-7083-b8e7-ba69c77ea956\artifacts\guide-v014-check\guide-1.png SHA256 7c2b6ada0a1fcb53eb5dbfafb76da03f9d9b2ac08691e3e00304d18675c6d5b2
- Evidence C:\Users\muzud\.codex\visualizations\2026\10\30\01a18ce8-f326-7083-b8e7-ba69c77ea956\artifacts\guide-v014-check\guide-2.png SHA256 c2d018f5305c95ef0a177808b6072fadd4754ceb2c90ba012819083aaded605f
- Evidence C:\Users\muzud\.codex\visualizations\2026\10\30\01a18ce8-f326-7083-b8e7-ba69c77ea956\artifacts\guide-v014-check\guide-3.png SHA256 8aa0054be3b6195e9b57ade6bef812e1e64fda534059723cc4ebe612303f73b7


## 2026-11-23 袋小路・輪の確保の指南

最終提案の前に `securePocket` / `securePocketCaution` の２ページを追加。入口の少ない一帯を取り切って背後の敵経路を減らし、入口の守備、補給、帰路を残す。閉じた輪という形だけで安全を断定せず、海路、外への分岐、残存敵を確認する。守れる範囲を優先する共通の戦略説明であり、安全な領域を自動検出する機能や自動進軍命令ではない。

内政のマニュアル操作・システムメニュー・回転トグルは操作可能時に自国色。回転のオン／オフはチェック記号で示す。計略トレイの矢印はスクロール可能な方向だけ自国色、対象がない［今どこ見てる？］は無効色を維持する。

検証：2026-11-23、本体ビルド警告・エラー０、GuideRuntimeの63項目（全６ページ描画、Esc/Enter、最終導線、保存復元、庁舎カメラを含む）通過。LateralCourtesyLessonのUI検証では回転オン／オフ・メニュー・マニュアルの実ピクセルが自国色であることと、対象なしの無効色、指南８行の幅を確認。画像はNAS `tmp/delete-after-2026-11-24/lateral-courtesy-lesson/guide-ui/` と `ui/`。
