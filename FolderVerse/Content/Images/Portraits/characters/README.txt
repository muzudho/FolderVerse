女の子ごとの画像履歴

characters/character-01/ ～ character-60/ が女の子別の編集元です。
各女の子に差分1～6があり、差分ごとに版番号を管理します。
例：character-60/character-60-2-v004.png は女の子60・差分2・第4版。
差分番号は元シートの左から1～6。女の子の番号は元シートの行順。

現在の最新版は全360枚、横256px×縦192px（4:3＝16:12）。
女の子37～42・55～60はv004、ほかはv003です。
同じ番号の -generated.png は大きな生成原本（現在は全360枚が1448x1086）。
元画像と過去の版は上書き・削除せず、旧版792枚を含む1152枚の通常画像を保存しています。
v001は旧画風、v002は現画風の初期版。37～42・55～60のv003は以前の高さ調整版です。

catalog.json には全画像の版番号、寸法、SHA256、元画像、変更内容を記録します。
Current=true が差分ごとの最新版。OriginalFile が大きな原本の場所です。
指示を出すときは「女の子60、差分2、v004」のように指定してください。
描き直しには大きな原本を参照し、仕上がりを4:3に保ちます。

生成画像を次の版へ保存（プロジェクトルートのPowerShellから）：
./FolderVerse/Content/Images/Portraits/save-generated-character.ps1 -CharacterId 60 -Variant 2 -GeneratedSource 'C:/path/to/generated.png'
大きな生成原本をコピー保存し、縦横比を保って256x192に縮小。番号は自動採番します。
変更内容や指示はcatalog.jsonのNoteと、指示の記録ファイルへ残してください。
すでに仕上げたPNGを直接登録する場合はadd-character-version.ps1を使えます。

ゲームへの反映：
./FolderVerse/Content/Images/Portraits/export-standardized-portraits.ps1
全360枚の寸法・ハッシュ・生成原本を検証し、v3-standardized/tilesへ最新版を出力します。
ゲームは個別画像を読み、表示枠に合わせて縦横比を保ってトリミングします。
v3-standardized/selected-versions.jsonが採用版の一覧。更新後はビルドしてゲームを再起動します。
シートの一括切り出しスクリプトはcharacters内の履歴を変更しません。

今回の指示・再描画の記録はstandardization.txtとoutpaint-4x3-prompt.txt。
生成を拒否された24枚は、利用者の指示により衣装・構図を変更して新しく描画しました。
standardization-preview-XX-YY.pngは全360枚を4人ずつ確認する一覧です。