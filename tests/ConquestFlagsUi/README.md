# ConquestFlagsUi

世界征服状況のホワイトボード横の地星儀について、地域の開示に合わせて国旗が０本→１本→２本→３本と増えることをピクセル検証する。実際のGame1.Drawで４画面を保存し、現地中継へ国旗が残らないことも確認する。実行中のゲーム・セーブは操作しない。

ConquestCurtainと同じGameDll／RuntimeDirおよびGAME_RUNTIME／OLD_RUNTIME／PROBE_FONTを指定し、画像保存先を引数に渡す。SDL2.dllもテスト出力先に配置する。

2026-11-22の検証出力は `\\TAKAHASHI-NAS\Share\muzudho-with-ai-agent\FolderVerse\tmp\delete-after-2026-11-23\conquest-flags{,-tests}\`。期限は日本時間2026-11-23、翌日から削除候補。
