# DemoSetupUi

区画権利の停止・確定、実際の顔の選択、確認ボタン、次の人間担当者への遷移を、18FPSの通常待ち時間と実描画で検証する。1080p／540p、人間１人／２人の４条件。通常マウス操作による人物選択・確認も比較する。

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet build tests/DemoSetupUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ --disable-build-servers
$env:ALSOFT_DRIVERS='null'
dotnet artifacts/corner-lesson/DemoSetupUi.dll
```

検証用世界のみを使い、利用者のセーブは読み書きしない。画像は既存artifacts/corner-lesson/demo-setup-0.png～27.pngを再利用する。
