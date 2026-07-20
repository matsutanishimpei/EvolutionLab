# 設定ファイルリファレンス

設定ファイルは`Assets/StreamingAssets/evolution-config.json`です。Play Mode開始時に読み込まれます。

## 単位

| 表記 | 意味 |
|---|---|
| `simulation s` | Unity物理世界内のシミュレーション秒 |
| `real s` | PC上で経過する実時間秒 |
| `Unity unit` | Unity距離単位。通常は1m相当として扱う |
| `Hz` | 1シミュレーション秒あたりの周期数 |
| `deg/s` | 関節モーターの目標角速度相当 |
| `×` | `Time.timeScale`の時間倍率 |

`evaluationSeconds = 30 simulation s`を30倍速で実行した場合、理論上は約1 real sです。ただし物理演算が追いつかない場合は、それより長くかかります。

## population

| キー | 型 | 単位 | 既定値 | 内容 |
|---|---|---|---:|---|
| `size` | int | 体 | 20 | 1世代の個体数 |
| `eliteCount` | int | 体 | 4 | そのまま残す上位個体数 |
| `mutationChance` | float | 確率 0～1 | 0.1 | 各遺伝子の突然変異確率 |
| `evaluationSeconds` | float | simulation s | 30 | 1世代の物理評価時間 |
| `fastForwardTimeScale` | float | × | 30 | 通常世代の時間倍率 |

## environment

| キー | 型 | 単位 | 既定値 | 内容 |
|---|---|---|---:|---|
| `windDirection` | Vector3 | 方向 | `(0,0,1)` | 正規化して使用する風向き |
| `windStrength` | float | 力/面積係数 | 10 | 投影面積へ掛ける風力係数 |
| `laneSpacing` | float | Unity unit | 12 | レーン中心間隔 |
| `startHeight` | float | Unity unit | 2 | 個体の開始高度 |
| `trackLength` | float | Unity unit | 80 | コース長と距離点上限 |

## course

| キー | 型 | 単位 | 既定値 | 内容 |
|---|---|---|---:|---|
| `obstacleCount` | int | 個 | 3 | 障害物とチェックポイントの数 |
| `laneWidth` | float | Unity unit | 10 | レーン内幅 |
| `openingWidth` | float | Unity unit | 4 | 開口部の幅 |
| `obstacleHeight` | float | Unity unit | 4 | 障害物の高さ |
| `laneWallHeight` | float | Unity unit | 5 | レーン壁の高さ |

## fitness

| キー | 型 | 単位 | 既定値 | 内容 |
|---|---|---|---:|---|
| `checkpointReward` | float | fitness点/個 | 100 | 関門通過点 |
| `speedRewardPerCheckpoint` | float | fitness点/個 | 25 | 早期通過の最大加点 |
| `wallContactPenaltyPerSecond` | float | fitness点/s | 10 | 接触時間の減点係数 |
| `maximumAllowedHeight` | float | Unity unit | 3.5 | 壁越え判定高度 |
| `heightViolationBaseFitness` | float | fitness点 | -1000 | 高さ違反時の基礎点 |
| `heightViolationCheckpointReward` | float | fitness点/個 | 10 | 違反時に残す関門点 |

## replay

| キー | 型 | 単位 | 既定値 | 内容 |
|---|---|---|---:|---|
| `firstGeneration` | int | 世代 | 1 | 最初のリプレイ世代 |
| `interval` | int | 世代 | 25 | リプレイ間隔 |
| `maxSeconds` | float | simulation s | 50 | 録画の最大時間。リプレイは1倍速なので概ねreal sと同じ |
| `cameraDistance` | float | Unity unit | 18 | カメラ後方距離 |
| `cameraHeight` | float | Unity unit | 14 | カメラ高度 |
| `cameraFieldOfView` | float | degree | 70 | 垂直視野角 |

## genome

| キー | 型 | 単位 | 既定値 | 内容 |
|---|---|---|---:|---|
| `minimumParts` | int | 個 | 2 | 最小パーツ数 |
| `maximumParts` | int | 個 | 4 | 最大パーツ数。コード上限も4 |
| `minimumParts` | int | 個 | 2 | 個体の最小部品数 |
| `maximumParts` | int | 個 | 4 | 個体の最大部品数。現在は最大5まで設定可能 |

各数値遺伝子は `partSize`、`connectionX/Y/Z`、`jointAmplitude`、`jointFrequency`、`jointPhase`、`mass`、`drag`、`angularDrag`、`friction`、`bounciness` の設定を持ちます。

| 共通キー | 型 | 意味 |
|---|---|---|
| `initialMin` | float | 第1世代の乱数下限 |
| `initialMax` | float | 第1世代の乱数上限 |
| `min` | float | 突然変異後を含む絶対下限 |
| `max` | float | 突然変異後を含む絶対上限 |
| `mutationAmount` | float | 突然変異1回で加減する最大量 |

## 調整例

### 軽量な動作確認

```json
"population": {
  "size": 8,
  "eliteCount": 2,
  "mutationChance": 0.1,
  "evaluationSeconds": 10,
  "fastForwardTimeScale": 10
}
```

### 精度重視

`openingWidth`を狭くし、`checkpointReward`と`wallContactPenaltyPerSecond`を上げます。通過不能にならないようパーツ最大サイズとの関係を確認してください。

## 読み込みと検証

- JSONはPlay開始時に1回読み込みます。
- 欠けた値にはC#側の既定値を使用します。
- 個体数、エリート数、突然変異率、時間倍率などは安全な範囲へ補正します。
- JSON構文エラー時はConsoleへエラーを出し、全項目を既定値へ戻します。
