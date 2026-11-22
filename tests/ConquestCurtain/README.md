# カーテン・点点背景・フォルダーツリーの回帰

実行中のゲームやセーブを操作せず、隔離した Game1 サブクラスで検証する。

- 実 Game1.Draw の早期 return 経路で、入場／退場カーテンの９状態をピクセル確認。全閉時はHUDも覆う。
- 現点背景は既存の点点画像を読み込む。
- フォルダーツリーのフォルダー絵で展開・折り畳み。点点名で正しい点点インスペクターを開く。

RecoveryPlannerUi と同様に GameDll／RuntimeDir を指定してNAS上へビルドする。実行時は GAME_RUNTIME を製品素材のある出力、OLD_RUNTIME をコンテンツ実行環境、PROBE_FONT を Content/UiFont.xnb の親へ指定し、SDL2.dll のある実行環境を PATH に追加する。引数は画像保存先。保存先もNASを使用する。
2026-11-22追加：退場カーテン全閉中に新聞へ切り替わること、開く間は新聞の時間・入力が進まないこと、開き終わりに放送画面が再表示されないことを確認。GuideRuntimeではガイド再呼出し、６面の地点カメラ、ガイド最終操作から地点庁舎／地点監督船へ進む経路、城壁への用途変更も検証する。

今回のビルドと画像は `\\TAKAHASHI-NAS\Share\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-23\phase-guide-{flow,tests}\` に保存。日本時間2026-11-23まで保持し、翌日から削除候補とする。
