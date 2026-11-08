# 追跡循環の戦訓

４手の合成盤と、任意指定した保護セーブの実戦４手・40手の再現を検証する。

```powershell
dotnet build tests/PursuitLessons/PursuitLessons.csproj --no-restore -p:OutputPath=C:/MuzudhoProjects/github.com/muzudho/FolderVerse/artifacts/tmp/delete-after-2026-11-11/cpu-convergence/
dotnet artifacts/tmp/delete-after-2026-11-11/cpu-convergence/PursuitLessons.dll artifacts/tmp/delete-after-2026-11-11/convergence-analysis/baseline-autosave.json.gz
```

最後のセーブ指定を省略すると合成盤だけを検証する。指定セーブは読み取り専用で使い、書き戻さない。

未学習の一周、観測後の通常退却、生存者の持ち帰りと研究、取得国の対策、戦訓#13と#14の独立した取得・研究・効果、片方だけを取得した場合のもう片方の循環、保存再開、履歴のない旧セーブ、死亡者・静止・明示行動の除外、履歴の上限を確認する。画像・UIは既存の `BattleLessonsUi` の最終図鑑ページで確認できる。
