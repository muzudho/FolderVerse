# EndingFactoryUi

工場のドロップダウンとエンディングの実描画・実入力の回帰テスト。

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet build tests/EndingFactoryUi --source artifacts/nuget-feed -p:OutputPath=D:/github.com/muzudho/FolderVerse/artifacts/corner-lesson/ --disable-build-servers
$env:ALSOFT_DRIVERS='null'
dotnet artifacts/corner-lesson/EndingFactoryUi.dll
```

検証用世界を使い、利用者のセーブは読み書きしない。1080p／540pの工場メニュー、エンディング画面を既存の検証出力フォルダーへ保存する。音源は実ストリームのライフサイクルとPCMの決定性・終端を検証する。曲調の聴感は手動で確認する。

2026-11-01追加：非アクティブになってもエンディングBGMがPlayingを維持し、フォーカス復帰でも同じインスタンスで継続することを検証。実ログ `operations_20261101_194258_106_b978c308.jsonl` では20:15:45.452にエンディング開始、20:15:45.542に非アクティブ化しており、従来の一時停止条件に該当した。タイトル・戦闘と同じく、エンディング中は背景でも音楽を継続する。
