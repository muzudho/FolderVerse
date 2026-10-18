# 開戦予約・説得演出の検証

リポジトリーのルートで実行する。

```powershell
dotnet run --project tests/InvasionPlanning
dotnet run --project tests/InvasionPlanning -- --ui
```

通常実行は、個体IDによる出撃、格納庫とポケットの予約の独立、クイーンの参加・不参加と単独出撃、選ばなかった個体の残留、不正なIDを実行前に拒否すること、セーブ復元と旧形式の互換、ダイアログの確定・取消、説得の上下左右距離、寝返り前後のフレームと表示時間を検証する。

`--ui` は実際のゲーム描画を使い、目標選択・×印・ロボット選択・説得範囲・寝返り成功の画像を実行ファイルの隣へ保存する。説得成功を確実に撮るため、描画用の戦闘だけ成功率100％に設定している。通常ルールの成功率は5％のまま。

2026-10-18検証：上記の両モード、RobotCampaign、RobotBattlePlayback、AutoSave、BattleFlagSuppliesが成功。画面も目視し、新聞の裏に隠れていた説得説明を下の説明欄へ修正した。

後続の2026-10-18更新で旗のルール自体を変更した。現在の検証範囲と、広範囲BattleRulesで確認した既存の辺戦長期化は [更新記録](../../docs/dev/2026-10-18-conquest-flags-tutorial.md) を参照。
