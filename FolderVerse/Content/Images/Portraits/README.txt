女の子画像の版管理

art-source/portraits/characters/：60人×6差分を、女の子別・差分別・版番号付きで保存する編集元。
v1-original/：旧画風の元画像。
v2-toy-style/：以前のシート形式の新版と、その修正前バックアップ。
v3-standardized/：現在ゲームが使用する256x192の個別画像360枚。

現在の設定：active-version.txt は v3-standardized。
ゲームは360枚の個別画像を読み、表示枠に合わせて縦横比を保ってトリミングする。
旧版に戻す場合は active-version.txt を v1-original または v2-toy-style に変更してビルド・再起動する。

画像の版番号とゲーム用セット名は別のもの。
例：art-source/portraits/characters/character-60/character-60-2-v004.png は女の子60・差分2・第4版。
同じ番号の -generated.png は大きな生成原本。過去の版を上書きしない。
art-source/portraits/characters/catalog.json で最新版、寸法、SHA256、生成原本、変更内容を確認できる。
v3-standardized/selected-versions.json はゲームへ採用した版の一覧。

次の描き足し・描き直しは save-generated-character.ps1 で登録し、生成原本と256x192版を保存する。
すべての最新版が256x192なら export-standardized-portraits.ps1 でゲーム用セットを更新できる。
詳しくは art-source/portraits/characters/README.txt と art-source/portraits/characters/standardization.txt を参照。
