# 開発に参加・興味がある人へ

[トップページ](../../README.md) / [ドキュメント案内](../README.md)

## 開発版を起動する

Windows 11を想定しています。.NET 9 SDKと、C#の開発ができるVisual Studioを用意してください。

1. このリポジトリーをGitで取得します。画像・音声はGit LFSを使っているので、Git LFSもインストールします。
2. リポジトリーで `git lfs pull` を実行して素材を取得します。
3. `FolderVerse.slnx` をVisual Studioで開き、パッケージを復元してFolderVerseを起動します。

ターミナルでは、リポジトリーのルートから実行できます。

```powershell
dotnet run --project FolderVerse/FolderVerse.csproj
```

依存パッケージはプロジェクトの復元で取得します。画像がLFSポインターのままの場合は、先に素材を取得してください。

## 実装・検証

- [実装と検証の記録](実装と検証.md)：IME、画像、クロップ、ロゴ。
- [ゲームの仕組み](../ref/ゲームの仕組み.md)：ルールと表示仕様。
- [実装計画](plan/)・[開発日誌](log/2026/10.md)
- [続きはここから](../続きはここから.md)：次回の起点。

```powershell
dotnet build FolderVerse/FolderVerse.csproj
dotnet run --project tests/RobotBattlePlayback/RobotBattlePlayback.csproj
dotnet run --project tests/RobotCampaign/RobotCampaign.csproj
dotnet run --project tests/RobotUi/RobotUi.csproj
```

UIの検証はウィンドウを開いて描画・操作を確認し、自動終了します。

## 素材とログ

- [原画とGit LFS](../../art-source/README.md)
- [征服者作成用画像の採用記録](../../art-source/portraits/conqueror-balanced/README.md)
- 操作ログは `Logs/`、スクリーンショットは `Screenshots/`。開発実行ではリポジトリーのルートに保存します。
- [IMEの調査状況](plan/IME入力不調_調査と対策.md)

公開案内用の小さい画像だけは `docs/pub/images/` で通常のGit管理を使います。ゲーム素材と原画のLFS管理は継続します。
