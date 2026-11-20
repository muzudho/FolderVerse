# 海上プラスチック再生採取場

2026-11-19、承認済みの白い浮体・オレンジの縁・青緑の設備のイメージボードをゲームへ実装。

## 配置と原料

既存の海上PP採取場を、回収コンベアー・選別箱・循環洗浄ドラム・ペレット加工機・貯蔵容器を備えた浮体施設に変更する。新しい資源種を増やさず、採れる品は既存のPP樹脂ペレット。製造対象はポリプロピレン製パーツ。

今回、歩留まりの減少は導入しない。１荷は16/16、PP製パーツ１つの必要量は8/16で従来通り。海上の初期埋蔵量・抽選確率・配置位置・極や大渦付近の除外・海と湖の判定を変更しない。全ての海地域に採取場がある保証はない。既存セーブの残量を増やさず、枯渇した施設は従来同様に暗く表示する。

利用者の追加明示決定（2026-11-19）：ひとまず海・陸とも同じように（再生）PP樹脂ペレットが採れる仕様で試遊する。海だけ6/16等の減産は行わず、出自による製造能力差も付けない。これは現実の回収歩留まりを示す比率ではなくゲームの暫定仕様。

内部のResourceSiteKind.OffshoreGasと保存フラグは既存のまま使用する。名称は旧実装由来だが、現在の海上外観は再生採取場。陸上の丸タンクは従来通り。原料の供給履歴別に格納品や能力を分けない。

## 描画

- 地星儀：共通DrawResourceSiteの立体モデル。浮体、回収ベルト、選別箱、洗浄ドラム、配管、ホッパー、ペレット、欄干を描く。星抽選・土地征服の共通プレビューもこのモデルを使う。
- 展開図：新規に生成した透過イラスト `FolderVerse/Content/Images/recycled-pp-facility-map-v001.png` を地図へ配置。比較表を切り貼りした素材ではない。ロード時にアルファを乗算済みに変換し、輪郭の色にじみを避ける。
- 原画と完全プロンプト：NAS `art-source/raw-materials/recycled-pp-facility-map-v001/`。内蔵imagegen、1536×1024、四隅と外周の透過を確認。元ファイル・NAS・ContentのSHA256は `1E548A2062C7AB4913933A29652F5B55A9F3378B18FE5BFE6A483A05DDDA1C7C`。

## 検証

tests/OffshorePPFacilityにモデルと描画ハーネス。Materials world42/cast2/territory3/fortune4、２CPU・25/25の新規配置を起点とする。

海上採取場12件の位置とPP対応、実採取員予約→１産業ターンで16unit採取・１荷減、保存復元で残量維持、製造員切替→PP頭１つで8unit消費を確認（31確認）。変更前D2E34D89版と同じ試験を実行し、全地点PP埋蔵量JSONと採取製造結果が一致した。

３方向の立体、360/120/60pxのイラスト、枯渇表示、実展開図全体を隔離描画して目視。１施設302面の有限座標を確認。ゲームとテストの隔離ビルドは警告・エラー０。

初回の採取試験はAdvanceIndustryを２回しか呼ばず失敗し、１回が1/3産業ターンであることに合わせ３回へ訂正。全体地図試験はCPU専用fixtureの現在プレイヤーが無いため失敗し、診断のみ表示プレイヤーを指定して通過。どちらも製品の修正ではない。

出力はNAS `tmp/delete-after-2026-11-19/recycled-pp-facility/`、重要記録は `archives/recycled-pp-facility/`。実ゲームの全画面入力・全SEED・収束への効果は未検証。元ゲーム・元セーブ・公開物を変更しない。


2026-11-20 01:44 JST 海上PP出荷取消→保存復元で注文なし/在庫PP16不変/材料待ち解除、18刻みでPP頭１個・PP8残を追加検証。tests/OffshorePPFacility全40確認、警告0/エラー0。固定C23全SHA実行コピー照合。NAS archives/raw-materials/marine-pp-cancel-restart-2026-11-20-0144にソース/result/notes、tmp20 raw-materials同名に隔離ビルド。製品/稼働ゲーム/元セーブ変更なし。次は出荷先変更と作業員解除または実入荷後の受取製造、UIは別検証。


2026-11-20 02:49 JST 海上採取PP16を隣接実製造員へ出荷予約→保存→実ResolveMapleShipments→保存で荷record完全一致/元と注文消滅、受取工場18刻みでPP頭１個/PP8残を確認。診断受取側は容量12/上限60・管理人２の制御fixture、自然世界の容量/CPU判断とは別。tests/OffshorePPFacility全44確認、固定C23全SHA実行照合/警告エラー0。NAS archives/raw-materials/marine-pp-arrival-production-2026-11-20-0249にソース/result/notes、tmp20同名にビルド。製品/元ゲーム/セーブ変更なし。次は出荷先変更と作業員解除/実UI予測へ。


2026-11-20 03:46 JST 海上PP由来の完成製品を隣接先へ入荷先変更→製造員解除（実レーン0）→保存復元でも製品record/新入荷先維持、実ResolveProductionIntakesで新地点に１回入荷しPP8残を３確認。tests/OffshorePPFacility全47確認、固定C23全SHA/警告エラー0。受取容量/在庫は制御fixture、実UIクリック/目視は別。NAS archives/raw-materials/redirect-release-2026-11-20-0346にソース/result/notes、tmp20同名ビルド。製品/稼働ゲーム/元保存変更なし。次は入荷先変更と容量予測/理由UI整合へ。


2026-11-20 04:46 JST 入荷先変更＋製造員解除後の受取満杯で完成品record維持/未入荷、Status『入荷待機：格納枠不足』。保存復元で理由/PP8残維持、非管理人１機を空けると１回だけ入荷。tests/OffshorePPFacility全49確認、固定C23全SHA/警告エラー0。Game1.ProductionIntakesがStatusを直接表示する読取経路も確認、文字収まり/クリック目視は別。NAS archives/raw-materials/redirect-capacity-reason-2026-11-20-0446にソース/result/notes、tmp20同名ビルド。製品/元ゲーム/保存変更なし。次は経路・所有国・戦闘の別理由またはピッカー目視へ。


2026-11-20 05:43 JST 完成品入荷先変更＋製造員解除後の受取Owner=-1で『入荷待機：経路・所有国・戦闘状況』、容量不足と別理由。保存復元で製品record/PP8残維持、Owner0回復で１回入荷を３確認追加。tests/OffshorePPFacility全52確認、固定C23全SHA/警告エラー0/stderr空。所有権変化は制御診断、実UI描画/入力は未確認。NAS archives/raw-materials/redirect-ownership-reason-2026-11-20-0543にソース/result/notes、tmp20同名ビルド。製品/元ゲーム/保存変更なし。次はピッカー実描画/入力または待機表示まとめへ。


2026-11-20 06:42 JST PP_WAIT_FIXTURE任意出力をtests/OffshorePPFacilityへ追加、モデル52確認維持。実製造員解除後の入荷待機保存をGame1派生で読み、実DrawProductionIntakePicker一覧/選択をFHD描画/目視、理由と説明が枠内。実ProductionIntakeInputに行中心を渡して一覧→Line選択→同地点Target=null/閉じるを確認、modal消費true、UI４確認。固定C23全SHA/双方警告エラー0/stderr空。NAS archives/raw-materials/intake-picker-2026-11-20-0642にソース/モデル/result/notes、tmp20同名に待機保存/画像/ビルド。OS入力/元ゲーム通し/８件超/全小画面は未確認。製品/元保存/稼働ゲーム変更なし。次は小画面か多件数ページ等へ。

2026-11-20 19:43 JST 固定C23/実製造員解除後の完成PP頭入荷待機fixtureを起点に、入荷先の最後１格納枠を５原材料配送と競合させ25確認通過。48/48は完成品予約枠保護で予約不可、16/48は予約できるが保存復元後の実ResolveMapleShipmentsは待機し原料/注文/完成品維持。実ResolveProductionIntakesでPP頭が１回入荷→保存→原料実Resolveで残り部位空間に16/48が１回入荷、原料出元消去/製品不変/容量超過なし。これは原料注入・空き１枠化のfixtureで、採取・全世界資源保存・実Campaign順序・UIの検証ではない。小分け機能やレシピ変更なし。警告エラー0/exit0/stderr空、製品/元保存/稼働ゲーム/既存差分不変。NAS archives/raw-materials/intake-raw-competition-2026-11-20-1943にProgram/csproj/result/固定DLL・Probe・fixture SHA/notes、tmp/delete-after-2026-11-20/raw-materials/intake-raw-competition-1943にビルド。次は実Campaignの解決順序または複数入荷の競合へ。

2026-11-20 20:47 JST 固定C23/別初期fixtureで実ConquestCampaign.BeginDeliveryを呼び、５原料48/48搬出と長距離混成隊長頭胴便の実処理順を確認、５ケース通過。長距離Advanceが原料Resolveより先なので、この１配送処理では便は満杯待機のまま、その後原料が出て空きができPhaseDeliveryになる。同じ配送処理内で長距離便は再試行されない。続く単体LongDelivery.Advanceでは荷が正確に１回到着/原料identity維持（これは部品再解決テストであり通常の次全体ターン所要時間の測定ではない）。20:44の原料→長距離の単体fixtureを実Campaign順序と混同しない。順序変更や高速化のルール変更なし。NAS archives/raw-materials/campaign-raw-export-order-2026-11-20-2047にProgram/csproj/result/固定DLL・Probe SHA/notes、tmp同名にビルド。警告エラー0/exit0/stderr空、400停止保存/元保存/製品/稼働ゲーム不変。次は複数入荷予約または通常の次ターンまで含む別fixtureへ。

2026-11-20 22:56 JST 21:48の予約拒否を切分け：ハーネスの文字列Replaceが最終LongDelivery.Advanceだけでなく準備段階の最初のAdvanceにも一致し、意図せず初期配送をCampaign.BeginDeliveryへ変えていた。元2047成功版から最後の一致だけ置換するv002を新規保存し、５原料/混成隊長頭胴荷すべて通過。１回目の実BeginDeliveryでは長距離が満杯待機→原料が後処理で搬出、保存復元後の２回目の実BeginDeliveryで長距離荷が正確に１回到着、原料identity/元残部/容量維持を確認。固定DLLと実行コピーのSHAは両C23AAF573A88448EC7163CD0D43E8086690F36C521EBB7773A5FE832BA2AFFC1。前回失敗は製品予約不具合の証拠ではない。２回の配送ディスパッチを検査したfixtureで、間の内政/軍事/産業を進めた自然な次全体ターンの証明ではない。警告エラー0/５件/stderr空。失敗版は保持、400保存/製品/稼働ゲーム/元保存は変更しない。NAS archives/raw-materials/campaign-delivery-reentry-v002-2026-11-20-2256にsource/result/notes/SHA、tmp/delete-after-2026-11-20/raw-materials/campaign-delivery-reentry-v002-2256にビルド。次は同じ５条件を反復せず、複数注文の競合か通常ターン全体の別fixtureへ。
