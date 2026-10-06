女の子画像の版管理

v1-original/   前回の元画像（画像データは変更せず保存）
v2-toy-style/  仮決めした画風を反映した新版

各版の sheet-01～10.png、base-*-normalized.png、manifest.json をゲームに同梱。
tiles/ は60キャラ×6差分の個別画像。ゲームはシートから描画します。

切り替え方法
active-version.txt の１行を v1-original または v2-toy-style に変更し、ゲームを再起動。
開発時は変更後にビルドして設定を出力先へコピーします。
配布後は実行ファイルの隣の Content/Images/Portraits/active-version.txt を変更するだけで切り替えできます。
現在の初期設定は v2-toy-style。

original-file-hashes.json は移動前の元ファイルの SHA256 一覧です。
