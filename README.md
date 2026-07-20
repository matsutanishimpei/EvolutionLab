# EvolutionLab

EvolutionLabは、Unity 6の3D物理環境で複数パーツの個体を進化させる遺伝的アルゴリズム実験です。

各個体は2～4個のSphereとHingeJointで構成されます。全個体を同一構造の独立レーンで評価し、障害物の開口部を順番に通過する身体形状と周期運動を進化させます。

## 主な特徴

- 20体の集団とエリート保存方式
- 形状、物理特性、関節運動を持つGenome
- 全個体で条件が等しい独立レーン
- チェックポイント、速度、接触、高さ制限によるfitness
- 交叉と突然変異による自動世代更新
- JSONによる実験条件の変更
- 通常世代の高速・非表示実行
- 最優秀個体の自動リプレイとMP4録画

## クイックスタート

1. Unity 6でプロジェクトを開きます。
2. パッケージ取得とコンパイルが終わるまで待ちます。
3. Playボタンを押します。
4. Consoleで世代ランキングを確認します。
5. `Recordings`フォルダでチャンピオンリプレイを確認します。

`EvolutionManager`は再生時に自動生成されるため、シーンへの手動アタッチは不要です。

## 現在の評価概要

1世代は既定で30シミュレーション秒です。通常世代は既定30倍速なので、PCが物理計算に追いつく場合は約1実時間秒で進みます。

```text
fitness =
    チェックポイント通過数 × 100
    + 風下距離
    + 速度ボーナス
    - 障害物接触時間 × 10
```

高さ制限を超えた個体は壁越えとみなし、通常のfitnessではなく失格用の得点へ置き換えます。

## 設定

実験条件は次のJSONから変更できます。

[evolution-config.json](Assets/StreamingAssets/evolution-config.json)

```json
{
  "population": {
    "size": 20,
    "eliteCount": 4,
    "mutationChance": 0.1,
    "evaluationSeconds": 30,
    "fastForwardTimeScale": 30
  }
}
```

設定値の型、単位、既定値、調整例は[設定リファレンス](Docs/CONFIGURATION.md)を参照してください。

## ドキュメント

- [進化と評価の仕様](Docs/EVOLUTION.md)
- [設定ファイルリファレンス](Docs/CONFIGURATION.md)
- [操作マニュアル](Docs/OPERATION.md)
- [アーキテクチャと拡張方法](Docs/ARCHITECTURE.md)
- [コントリビューションガイド](CONTRIBUTING.md)

## 主なコード

| ファイル | 役割 |
|---|---|
| `Genome.cs` | 遺伝子と交叉・突然変異 |
| `SphereIndividual.cs` | 個体の身体、運動、計測 |
| `EvolutionConfig.cs` | JSON設定の読み込みと検証 |
| `GeneticAlgorithm.cs` | 集団生成、選択、世代更新 |
| `FitnessEvaluator.cs` | 最終fitnessの計算 |
| `EvolutionManager.cs` | 実験全体の進行とリプレイ |

## 動画出力

第1世代から25世代おきに、その世代の最優秀Genomeを同じコースで再走させます。

```text
Recordings/ChampionReplay_0001.mp4
Recordings/ChampionReplay_0026.mp4
Recordings/ChampionReplay_0051.mp4
```

通常世代の全体動画や静止チャンピオン動画は生成しません。

## ライセンス

このプロジェクトは[MIT License](LICENSE)で公開されています。
