# Folder Verse

MonoGame製のゲーム試作。Visual Studioで `FolderVerse.slnx` を開いて実行する。

## 画面の流れ

1. START → 世界作成。寸法は1～5。STOPで固定、左ドラッグで回転。
2. 世界確定 → 世界征服者作成。7×5グリッドの外周20マスに人物、中央15マスに世界を表示。
3. 人物のSTOP → 人物確定 → 初期配置の抽選。
4. 配置のSTOP → 国境線と首都の旗を確認 → 初期配置確定 → 世界征服者選択。
5. 顔をクリックして操作する人物を確定。選び直しや配置の確認もできる。

## SEEDの再入力

画面下のSEEDボタンをクリックすると、ゲーム内の電卓風ダイアログが開く。Windows Formsは使用しない。
数字ボタンまたは物理キーボードで入力する。Cは全消去、<は1文字削除、Enterは適用、Escapeはキャンセル。
範囲は0～2147483647。ダイアログ表示中は抽選を停止する。

- 世界SEED：寸法と海・陸の形を再現する。
- 人物SEED：60人×6枚の完成画像から選ぶ20人と国の色を再現する。
- 配置SEED：確定した世界と人物に対して、領地と首都を再現する。

完全に同じ初期状態を再現するには、3種類のSEEDを控え、世界→人物→配置の順に入力する。
人物を変更すると、依存する初期配置をリセットする。
世界の表面セル数は `2 × (Width × Height + Height × Depth + Depth × Width)`。3×3×3なら54。
セル数が20未満の場合、外周には20人全員を表示し、先頭のセル数ぶんが参戦する。残りは待機。
各参戦者には異なる首都が1セルあり、領地は首都につながるセルで構成される。

## グラフィック

タイトル画像：`FolderVerse/Content/Images/title-screen.png`
征服者のオリジナル版：`FolderVerse/Content/Images/conquerors-original.png`（5列×4行、20人）。
従来の拡大版：`FolderVerse/Content/Images/conquerors-v2.png`。オリジナルとともに保管。
人物は女の子の征服者と電池式玩具というデザイン。電池を取り外すゲーム処理は今後の実装。
海・陸、セル国境、首都の旗はゲーム内で生成・描画する。
## スクリーンショット

Ctrl+P（左右どちらのCtrlでも可）で、描画済みのゲーム画面をPNG保存する。
タイトル・世界作成・征服者作成・SEEDダイアログのいずれでも撮影できる。
保存先は開発実行時はプロジェクトルートの `Screenshots/`。配布版では実行ファイルの隣の `Screenshots/`。
画像サイズは現在のウィンドウの描画領域と同じ。余白の背景も含めて保存する。
ファイル名に日時と識別子を付けて上書きを防ぎ、キーを押しっぱなしにしても1回だけ保存する。
文房具UIのデモにある、Draw後のGetBackBufferDataとSaveAsPngによる保存方式を参考に実装。

征服者作成画面の操作ボタンと3種類のSEEDボタンは下端の1段に配置。
寸法・表面セル数・参戦人数と操作案内は中央の地球儀区画に表示する。
## 完成画像による人物抽選

imagegenで60人それぞれ6つの角度・ポーズを描いた、合計360枚の完成画像を使用する。
目・耳・眼鏡類のランタイム合成は廃止。従来のオリジナル画像と差分素材は保管する。
現在の素材は Content/Images/Portraits/sheet-01.png ～ sheet-10.png。
各シートは6列×6行で、1行が同じ人物の6パターン。1シート36枚、10シート360枚。
抽出した個別PNGは同ディレクトリーの tiles/ 以下。描画はシートの該当区画を直接読む。

目・耳・眼鏡類は人物ごとにランダムな共通特徴を指定し、各画像で性格・表情・状況をランダムに指定。
顔の角度とポーズを変え、見える目は視聴者へ向ける指定。線目・眠気・笑顔・瓶底眼鏡では目が見えない場合がある。
表情・状況は生成時のキーワードで、ゲーム内でその行動を演じる処理ではない。
manifest.json に360枚の指定を保存。prompt-01.txt ～ prompt-10.txt に実際の生成プロンプトを保管。
生成画像ではキーワードの細部が完全には一致しない場合がある。

60人から20枠を抽選するため、毎回出ない人物がいる。同じ人物は異なる画像で最大6人まで登場する。
同じ完成画像は同じ回で重複しない。双子から6つ子まで別々の国の色を持つ。
顔にマウスを置くと、中央に人物番号・画像番号・テーマ・表情と状況の指定・姉妹人数を表示。
人物SEEDは画像選択と国の色を再現する。31ビットの範囲を引き続き使用する。
人物生成方式を変更したため、過去の差分合成版の人物SEEDとは結果が異なる。
世界と配置の生成方式は維持。完全再現には3種類のSEEDを順番に入力する。

描画確認画像は Screenshots/PortraitReview/completed-01.png ～ completed-10.png。
## 配信向けセル数表示とバックグラウンド動作

領地が決まると、顔カード右下に国のセル数と cell を表示する。
数字は顔画像の縦幅の約1/3、cell はその半分の高さ。Comic Sans MS の丸い太字を使用し、
国境と同じ国の色で描き、暗い背景と縁取りで写真上でも読みやすくする。
領地確定前の「参戦」表示は廃止。待機人物は領地抽選後に 0 cell と表示する。

ウィンドウが非アクティブでも、抽選と地球儀の回転を更新し続ける。
クリック・キー操作はアクティブ時だけ受け付ける。SEED入力ダイアログ中の抽選停止は維持する。
## 世界征服者選択

7×5のカレンダー区画を維持し、各区画を4×4に分割した28×20（560マス）のサブグリッドを使う。
各参戦者の顔の面積は領地の割合で決める。辺の長さは floor(sqrt(496 × 国セル数 / 世界セル数))、
最小1、最大20。面積は1、4、9、16…400という平方数になる。
サブグリッドのマスはカレンダーと同じ長方形比率で、顔は縦横同じマス数を占める。
大きな顔から詰め、幾何的に収まらないときは大きな顔を一段縮小して再配置。
全参戦者が重ならず収まることを優先するため、比率の表現は近似。余りマスには画像を置かない。
領地がない待機人物はプレイヤーとして選べない。

顔へのホバーで左上へ10×10px移動し、元の位置にドロップシャドウを描く。
10pxは1920×1080の論理画面での寸法で、ウィンドウの拡縮時は他の要素と同率で拡縮する。
クリックで WorldSetup.PlayerSlot に国のスロット番号を記録し、その人物を「あなた」と表示する。
選び直すボタンで再選択できる。人物・世界・初期配置を作り直すと操作人物の指定を解除する。
プレイヤー確定後は「世界征服状況へ」ボタン、または Enter で状況画面へ進む。
## 顔クロップ修正・設定画面ロゴ・選択地球儀

生成画像のシートは行高さが微妙に不均等で、特にシート07はずれが大きかった。全10シートを測定して等分クロップを廃止。シート07は実際の境界
0,158,315,473,631,791,1024 px に修正。全シートの切り出しに2pxの内側余白を設けて区切り線の混入を抑える。
原本シートは変更せず、個別PNGを同じ切り出し規則で再抽出する。

title-logo.png は組み込みimagegenでタイトル画像からロゴ・副題・飾りを透過抽出したもの。
元の論理画面位置で不透明度14%の背景として世界作成・征服者作成・選択画面に描く。
生成指定は Content/Images/title-logo-prompt.txt。タイトル画像原本は保持する。

選択画面の地球儀はカレンダー2×2マス＝サブグリッド8×8の64マスを占める。
この固定サイズの地球儀も顔と一緒に配置する。残る496マスで人物の平方サイズを配分する。
地球儀は自動回転し、左ドラッグで回せる。人物選択対象にはならない。
最後にホバーした人物の領地を国境と同じ色で全面塗りし、照明に依存しない明るさで穏やかに点滅させる。
キャラ確定後は自分の国をハイライトする。配置確認へ戻ると解除する。

## 世界征服状況

左側にはカレンダー2×2マスの肖像と金色の額縁、征服者名、政治体制（全員「独裁者」）、首都名、領地数を表示する。
右側は地球儀と同じ地形を使う6面の展開図。自国のセルが最も多い面を中央に置き、自国の色を点滅させる。
左右回転ボタンで90度ずつ回転し、面クリックまたは矢印キーで中心面を移す。「自分の国へ」「首都の面へ」「北を上に」で表示を戻せる。
同じ英字が付いた辺同士は地球上でつながる。セルへのホバーで都市名と征服者名を表示する。
通常面の北は +Y。北極面の上は +Z、南極面の上は -Z を基準とする。

各セルは世界SEEDから再現できる固有の都市名を持つ。海・沿岸・草原・丘陵の地形に合わせた語をカタカナで組み合わせる。
海セルは面をまたぐ隣接関係で最寄りの陸地都市を探し、「都市名・方角」という集合地点名を付ける。
方角はその都市の北方向を基準にする。すべて海の世界には独立した名前を付ける。
同名になる海域には追加の語を付けて区別する。征服者名は人物SEEDから再現する。

語彙の出典は Wiktionary。読みはゲーム向けの近似で、合成名は架空の地名。

| 用語 | 読み・意味 | 出典 |
| --- | --- | --- |
| vallée / Tal | ヴァレー / タール：谷 | [仏](https://en.wiktionary.org/wiki/vall%C3%A9e)・[独](https://en.wiktionary.org/wiki/Tal) |
| río / Fluss | リオ / フルス：川 | [西](https://en.wiktionary.org/wiki/r%C3%ADo)・[独](https://en.wiktionary.org/wiki/Fluss) |
| montaña / Berg / colina | モンターニャ / ベルク：山、コリナ：丘 | [山](https://en.wiktionary.org/wiki/monta%C3%B1a)・[山](https://en.wiktionary.org/wiki/Berg)・[丘](https://en.wiktionary.org/wiki/colina) |
| bassin / falaise / cueva / pradera | バッサン：盆地、ファレーズ：崖、クエバ：洞窟、プラデラ：草原 | [盆地](https://en.wiktionary.org/wiki/bassin)・[崖](https://en.wiktionary.org/wiki/falaise)・[洞窟](https://en.wiktionary.org/wiki/cueva)・[草原](https://en.wiktionary.org/wiki/pradera) |
| mare / insula / península / fons | マーレ：海、インスラ：島、ペニンスラ：半島、フォンス：泉 | [海](https://en.wiktionary.org/wiki/mare)・[島](https://en.wiktionary.org/wiki/insula)・[半島](https://en.wiktionary.org/wiki/pen%C3%ADnsula)・[泉](https://en.wiktionary.org/wiki/fons) |
| nova / vecchio / alto / bajo | ノヴァ：新しい、ヴェッキオ：古い、アルト：高い、バホ：低い | [新](https://en.wiktionary.org/wiki/nova)・[古](https://en.wiktionary.org/wiki/vecchio)・[高](https://en.wiktionary.org/wiki/alto)・[低](https://en.wiktionary.org/wiki/bajo) |
| rosa / mela / victoria / miracolo | ローザ：バラ、メーラ：リンゴ、ヴィクトリア：勝利、ミラコロ：奇跡 | [バラ](https://en.wiktionary.org/wiki/rosa)・[リンゴ](https://en.wiktionary.org/wiki/mela)・[勝利](https://en.wiktionary.org/wiki/victoria)・[奇跡](https://en.wiktionary.org/wiki/miracolo) |
| polis / ciudad / plaza / ville / town | ポリス / シウダード：都市、プラサ：広場、ヴィル / タウン：町 | [都市](https://en.wiktionary.org/wiki/polis)・[都市](https://en.wiktionary.org/wiki/ciudad)・[広場](https://en.wiktionary.org/wiki/plaza)・[町](https://en.wiktionary.org/wiki/ville)・[町](https://en.wiktionary.org/wiki/town) |
| norte / sur / este / oeste | ノルテ：北、スール：南、エステ：東、オエステ：西 | [北](https://en.wiktionary.org/wiki/norte)・[南](https://en.wiktionary.org/wiki/sur)・[東](https://en.wiktionary.org/wiki/este)・[西](https://en.wiktionary.org/wiki/oeste) |
