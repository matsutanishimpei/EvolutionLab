# 操作マニュアル

## 実験を開始する

1. Unity 6でプロジェクトを開きます。
2. Consoleに赤いコンパイルエラーがないことを確認します。
3. 必要なら`Assets/StreamingAssets/evolution-config.json`を編集します。
4. Playボタンを押します。

通常世代は個体とコースを非表示にし、高速で進みます。Gameビュー左上には世代番号と時間倍率が表示されます。

## 実験を停止する

Playボタンをもう一度押します。停止時には録画を終了し、`Time.timeScale`を1へ戻します。

途中停止したMP4は、エンコーダーの終了タイミングによって正常に再生できない場合があります。

## ランキングを読む

Consoleには各世代の結果が表示されます。

```text
1. Gen26_Sphere_004  CP: 3/3  Speed: 42.1  Fitness: 361.24
```

- `CP`：順番どおりに通過した関門数
- `Speed`：関門への早期到達ボーナス
- `Fitness`：親選択に使われる最終得点

## リプレイを見る

既定では第1世代から25世代おきに最優秀個体を再走させます。

```text
1, 26, 51, 76, 101, ...
```

リプレイ中は時間倍率が1になり、金色の個体をカメラが追従します。最大50シミュレーション秒、または全関門通過後のゴール到達まで続きます。

## 動画を確認する

保存先：

```text
C:\Users\matsu\EvolutionLab\Recordings
```

ファイル名：

```text
ChampionReplay_0001.mp4
ChampionReplay_0026.mp4
```

仕様は1280×720、30fps、音声なしです。

## 設定を変更する

1. Play Modeを停止します。
2. `evolution-config.json`を編集して保存します。
3. Playを再開します。

Play中の変更は現在の実験へ反映されません。

## トラブルシューティング

### Play Modeへ入れない

Consoleの最初の赤いエラーを確認します。後続エラーは最初のエラーが原因の場合があります。

### Unityが重い

- `fastForwardTimeScale`を30から10へ下げる
- `population.size`を減らす
- `genome.maximumParts`を減らす

100倍速でも物理計算は省略されないため、必ず100倍の実効速度になるわけではありません。

### 関門を通れない

- `openingWidth`を広げる
- `evaluationSeconds`を増やす
- `obstacleCount`を一時的に減らす
- `maximumAllowedHeight`が開始姿勢より低くないか確認する

### 動画が作られない

- 対象世代か確認する
- Unity Recorder 5.1.6がインストール済みか確認する
- リプレイ終了まで待つ
- `Recordings`への書き込み権限を確認する

### JSONを壊した

Consoleに読み込みエラーが表示され、コード内の既定値で実行されます。Gitからファイルを戻すか、設定リファレンスの既定値へ修正してください。
