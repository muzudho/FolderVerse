# CompactWarInspectorUi

隔離したテスト世界と画面外の描画ウィンドウを使う。検証対象と仕様は docs/設計/征服者の記念メダル.md を参照。

GameDllとRuntimeDirをゲーム出力へ指定してNASへビルド。SDL2.dllを実行フォルダーへ配置する。GAME_RUNTIMEは実行素材のあるゲーム出力、OLD_RUNTIMEはContentのある既存実行環境。出力フォルダーを第1引数へ渡す。

NAS出力は tmp/delete-after-2026-11-24/souvenir-medallion/。日本時間2026-11-24まで保持、翌日から削除候補。利用者のセーブや起動中のゲームは変更しない。
