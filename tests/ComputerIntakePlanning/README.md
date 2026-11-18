# CPUの製造入荷先変更

満杯で止まった製造レーンを、既存の入荷先変更で隣接自国地点へ向ける判断の回帰。留保枠・完成品待機・巡回内の出力予測、手動工場、人間担当国の明示設定と保存、戦闘地点／辺、施設移転を確認する。

NASのMaterials104729・通常100ターンの読み取りコピーを引数に渡す局所診断試験。基準コピーは `Z:\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-18\convergence-analysis\product-follow-1416\full-100.fvs`。コピーを維持するか同じ条件の検証器で再生成して使い、実ゲームの元セーブを渡さない。

`dotnet run --project tests/ComputerIntakePlanning --artifacts-path <NAS出力> -p:PublishedGame=<隔離DLLフォルダー> -- <NAS上の検証コピー>`

`FOLDERVERSE_PROBE_CONTENT_BASE` は隔離Contentのディレクトリー。追加レーン・戦闘・移転予約は保護判定の合成配置であり、それ自体の自然発生や戦闘全体は検証しない。征服への効果は別の複数SEED比較で測る。