# PrimaryActionArtUi

内政プライマリーボタンの絵素材・配置の隔離UI回帰。メダル全体の範囲と全11ボタンが重ならないこと、ガイドが通常幅96・ターン終了が1.5倍幅144であること、移動後のガイド呼出し、国色４種類の描画、透過アトラス10図柄の対応を確認する。各図柄を個別描画し、国旗を描く右上・左下の実際の16×16領域に絵の不透明画素がないことも検証する。

GameDll／RuntimeDirを指定してビルド。GAME_RUNTIMEは新しいUiアトラスを含むゲーム出力、OLD_RUNTIMEはコンパイル済みContentを含む実行素材。SDL2.dllをテスト実行フォルダーへ置き、画像保存先を引数に実行する。実行中のゲームとセーブは操作しない。

2026-11-22の原画・イメージボード・生成指示：`\\TAKAHASHI-NAS\Share\muzudho-with-ai-agent\FolderVerse\art-source\ui\primary-actions-v001\`。ビルドと画像検証はNASの`tmp/delete-after-2026-11-23/primary-action-art{,-tests}/`へ保存し、日本時間2026-11-23まで保持。翌日から削除候補。
