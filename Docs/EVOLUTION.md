# 進化と評価の仕様

## 個体構造

1個体は2～4個のSphereで構成されます。配列インデックス0がルートパーツ、1以降がHingeJointでルートへ接続される可動パーツです。

各パーツにはRigidbody、Collider、個体専用PhysicsMaterialが設定されます。個体の総質量は使用中のパーツへ均等に分配されます。

## Genome

### 個体共通遺伝子

| 遺伝子 | 内容 | 第1世代の範囲 | 突然変異後の範囲 |
|---|---|---:|---:|
| `partCount` | 使用するパーツ数 | 2～4 | 2～4 |
| `mass` | 個体全体の質量 | 0.5～3.0 | 0.1～5.0 |
| `drag` | 直線抵抗 | 0～0.5 | 0～2.0 |
| `angularDrag` | 回転抵抗 | 0.05～0.8 | 0～2.0 |
| `friction` | 静止・動摩擦係数 | 0.1～1.0 | 0～1.0 |
| `bounciness` | 反発係数 | 0～0.6 | 0～1.0 |

### パーツ別遺伝子

| 遺伝子 | 内容 | 第1世代の範囲 | 突然変異後の範囲 |
|---|---|---:|---:|
| `partSizes[i]` | パーツ直径 | 0.45～1.25 | 0.25～1.75 |
| `connectionX[i]` | 接続方向X | -1～1 | -1～1 |
| `connectionY[i]` | 接続方向Y | -0.35～0.75 | -0.5～1 |
| `connectionZ[i]` | 接続方向Z | -1～1 | -1～1 |
| `jointAmplitude[i]` | 関節の最大目標角速度 | 30～160 | 0～240 |
| `jointFrequency[i]` | 関節周期 | 0.4～2.0 Hz相当 | 0.1～3.0 Hz相当 |
| `jointPhase[i]` | 初期位相 | 0～2π rad | 0～2π rad |

接続方向はX/Y/Zから作ったベクトルを正規化します。ほぼゼロの場合は右方向を使用します。

## 周期運動

各関節の目標角速度はシミュレーション経過時間から計算します。

```text
angle = elapsedTime[s] × jointFrequency[Hz] × 2π + jointPhase[rad]
targetVelocity = sin(angle) × jointAmplitude
```

現在の制御は時間だけを入力とする開ループ制御です。障害物を検知するセンサー制御は未実装です。

## 風力

風は各FixedUpdateで全パーツへ継続的に加えます。

```text
radius = partSize / 2
projectedArea = π × radius²
windForce = normalizedWindDirection × windStrength × projectedArea
```

`partSize`と`radius`はUnity距離単位、`projectedArea`はUnity面積単位です。Unityの標準的な縮尺として1距離単位を1m相当として扱います。

## 公平な評価環境

- 全個体は同じ構造の独立レーンを使用します。
- スタートの相対位置、高さ、時刻を統一します。
- 風、重力、障害物、開口位置を統一します。
- レーン壁により他個体との衝突を抑えます。
- 各レーンの開口部に同じ順番のチェックポイントを配置します。

## Fitness

### 通常評価

```text
fitness =
    checkpointsPassed × checkpointReward
    + downwindDistance
    + speedScore
    - obstacleContactTime[s] × wallContactPenaltyPerSecond
```

風下距離は個体の全パーツの`worldCenterOfMass`平均から計測し、0からコース長までに制限します。

### チェックポイント

個体中心が次のチェックポイントより風下へ進み、開口中心から開口幅の45%以内にある場合に通過と判定します。必ず風上側から順番に通過する必要があります。

### 速度ボーナス

```text
remainingRatio = 1 - Clamp01(arrivalTime[s] / evaluationSeconds[s])
checkpointSpeedBonus = remainingRatio × speedRewardPerCheckpoint
```

早く到達したチェックポイントほど高い速度点を得ます。

### 接触ペナルティ

身体パーツがレーン壁または障害物へ接触しているシミュレーション時間を累積します。複数パーツの同時接触はパーツごとに加算されます。

### 高さ違反

いずれかのパーツ重心が`maximumAllowedHeight`を超えると壁越えと判定します。

```text
fitness = heightViolationBaseFitness
        + checkpointsPassed × heightViolationCheckpointReward
```

高さ違反時は距離、速度、接触時間を使用しません。

## 世代更新

1. 全個体を既定30シミュレーション秒評価します。
2. fitnessの降順に並べます。
3. 上位4体のGenomeをエリートとしてコピーします。
4. エリートから異なる親2体をランダムに選びます。
5. 各遺伝子を50%の確率で親Aまたは親Bから取得します。
6. 各遺伝子へ独立に既定10%の確率で突然変異を加えます。
7. 子16体とエリート4体で次の20体を作ります。
8. 旧GameObjectを削除し、新しいGenomeから次世代を生成します。
