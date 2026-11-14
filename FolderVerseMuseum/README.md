# FolderVerseMuseum

FolderVerse の旧機能を、単独で起動して見たり触ったりできる形で保存する図鑑です。ゲーム本体とは別の実行プロジェクトで、ゲームのセーブや設定を読み込みません。

## 起動

.NET 9 SDK が入った環境で、このディレクトリから実行します。

```powershell
dotnet run --project .\FolderVerseMuseum.csproj
```

## 展示 01 · 旧「方向のロボット」

元の立体モデル、箸と碗、背面のゼンマイ、XYZ軸の色付きネジを保存しています。展示のソースは `Exhibits/OrientationRobot/` にあり、ゲーム側の描画コードから独立しています。

- 左ドラッグで自由回転
- X・Y・Zで各軸を30度回転。Shiftを押しながらで逆回転
- Rで初期姿勢に戻す
- Escで閉じる
