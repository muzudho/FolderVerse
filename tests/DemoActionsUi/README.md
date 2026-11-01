# DemoActionsUi

Demoの地点選択と格納庫の部位移動を、現在の地図投影・当たり判定・通常ドラッグ処理・実描画と照合する。

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet build tests/DemoActionsUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ --disable-build-servers
$env:ALSOFT_DRIVERS='null'
dotnet artifacts/corner-lesson/DemoActionsUi.dll
```

1080p／540pで、展開図・90度回転・地点レイヤー無効からの復帰・視野外で大きく拡大した地星儀、３体のパーツ、２体のパーツ束、一体型の移動を検証する。押下と移動中は合体せず、ドロップした部位だけが移り、必要な回数の操作後に次へ進むことを確認する。描画は既存artifacts/corner-lesson/demo-actions-0.png～61.pngを再利用する。検証用世界のみを使い、利用者のセーブは読み書きしない。

移動では、候補限定の実際の当たり判定、画面外からの位置合わせ、選択失敗後の再選択、和平の征服者・ポケット移動予約の対象とフラグ、独立したターン終了クリック、候補なしの終了を1080p／540pで確認する。
