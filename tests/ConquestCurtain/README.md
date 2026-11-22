# カーテン・点点背景・フォルダーツリーの回帰

実行中のゲームやセーブを操作せず、隔離した Game1 サブクラスで検証する。

- 実 Game1.Draw の早期 return 経路で、入場／退場カーテンの９状態をピクセル確認。全閉時はHUDも覆う。
- 現点背景は既存の点点画像を読み込む。
- フォルダーツリーのフォルダー絵で展開・折り畳み。点点名で正しい点点インスペクターを開く。

RecoveryPlannerUi と同様に GameDll／RuntimeDir を指定してNAS上へビルドする。実行時は GAME_RUNTIME を製品素材のある出力、OLD_RUNTIME をコンテンツ実行環境、PROBE_FONT を Content/UiFont.xnb の親へ指定し、SDL2.dll のある実行環境を PATH に追加する。引数は画像保存先。保存先もNASを使用する。