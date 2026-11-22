# RecoveryExpeditions

廃品回収の実物ロボット・ポケット共有貸出・帰還・保存のモデル回帰。製品DLLを隔離ビルドし、そのDLLと既存の実行環境を指定する。

```powershell
dotnet build tests/RecoveryExpeditions/RecoveryExpeditions.csproj --artifacts-path <NASの検証出力> -p:GameDll=<製品DLL> -p:RuntimeDir=<MonoGame.Framework.dllのあるフォルダー>
$env:GAME_RUNTIME = '<ゲーム素材のあるフォルダー>'
dotnet <検証出力>/bin/RecoveryExpeditions/debug/RecoveryExpeditions.dll [ヘッドレス出力のworld.fvs]
```

保存ファイル引数がなければ44項目。指定時はCPU回収の実行・完了、貸出数と実在IDの整合も調べる。元セーブは読み込むだけで変更しない。