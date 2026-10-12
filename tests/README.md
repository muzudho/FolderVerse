# テストの実行

リポジトリのルートから `./scripts/run-tests.ps1 -Project RobotBattlePlayback` のように実行する。複数指定は `-Project RobotBattlePlayback,OrdersUi`、指定を省略すると全テストを実行する。

実行が終わると、成功・失敗にかかわらず対象テストの `bin` 内の `Content` 複製を削除する。ゲーム本体の出力、素材原本、スクリーンショット、ログ、テストソースは保持する。次回のビルドで必要な複製は再生成される。

`dotnet run` を直接使うと後片付けされないため、通常は上記スクリプトを使う。

Visual Studio の［ソリューションのクリーン］でも、ゲームプロジェクトの Clean 後に全テストの bin 内の Content 複製を削除する（Directory.Build.targets）。通常のビルドでは削除しない。
