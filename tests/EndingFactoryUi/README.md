# EndingFactoryUi

工場のドロップダウンとエンディングの実描画・実入力の回帰テスト。

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet build tests/EndingFactoryUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ --disable-build-servers
$env:ALSOFT_DRIVERS='null'
dotnet artifacts/corner-lesson/EndingFactoryUi.dll
```

検証用世界を使い、利用者のセーブは読み書きしない。1080p／540pの工場メニュー、エンディング画面を既存の検証出力フォルダーへ保存する。音源は実ストリームのライフサイクルとPCMの決定性・終端を検証する。曲調の聴感は手動で確認する。
