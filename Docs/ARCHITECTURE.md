# アーキテクチャと拡張ガイド

## 実行フロー

```text
EvolutionConfigLoader
        ↓
EvolutionManager ── Course / Wind / Replay
        ↓
GeneticAlgorithm ── Genome
        ↓
SphereIndividual ── Rigidbody / HingeJoint / Sensors
        ↓
FitnessEvaluator
```

## 責務

### EvolutionManager

実験開始、物理更新、世代ループ、コース生成、ランキング、リプレイを統括します。選択・交叉の詳細とfitness式は他クラスへ委譲します。

### EvolutionConfig

JSONのデータ構造と既定値を定義します。新しいゲームルール設定は、まず該当する設定セクションへ追加します。

### GeneticAlgorithm

初期集団、エリート保存、親選択、交叉、突然変異を担当します。シーンやRigidbodyには依存しません。

### Genome

遺伝可能な値だけを保持します。環境条件やfitness配点は含めません。

### SphereIndividual

GenomeをGameObjectへ反映し、身体の物理状態と計測値を管理します。最終的な配点計算は行いません。

### FitnessEvaluator

計測済みの通過数、距離、速度、接触時間、高さ違反と設定値からfitnessを返します。Unityシーンの生成には依存しません。

## 新しい設定を追加する

1. `EvolutionConfig`の適切なセクションへフィールドと既定値を追加します。
2. `evolution-config.json`へ同名キーを追加します。
3. 必要なら`EvolutionConfigLoader.Validate`へ範囲検証を追加します。
4. 使用するサービスまたはManagerへ値を渡します。
5. `Docs/CONFIGURATION.md`へ型、単位、既定値を追記します。

## 新しい遺伝子を追加する

1. `Genome`へ遺伝子フィールドを追加します。
2. `CreateRandom`へ初期生成を追加します。
3. `Crossover`へ親A/Bの50%選択を追加します。
4. `Mutate`へ突然変異量とClamp範囲を追加します。
5. `Clone`へコピーを追加します。
6. `SphereIndividual`で身体または制御へ反映します。
7. 必要な範囲を`EvolutionConfig.GenomeSettings`とJSONへ追加します。
8. `Docs/EVOLUTION.md`へ意味と単位を追記します。

この手順を1か所へ集約するため、将来は遺伝子定義を型単位へ分割する余地があります。ただし文字列辞書だけのGenomeは型安全性を失うため、強く型付けしたフィールドを維持する方針です。

## 新しいfitness項目を追加する

1. `SphereIndividual`へ計測値を追加します。
2. `EvolutionConfig.FitnessSettings`へ配点を追加します。
3. JSONへ値を追加します。
4. `FitnessEvaluator.Calculate`へ式を追加します。
5. ランキング表示とドキュメントを更新します。

## 超音波センサーを追加する案

センサー本体は全個体共通の固定能力とし、センサーへの反応だけをGenome化すると公平です。

推奨構成：

- ルートパーツへ前、左前、右前のRaycastを固定
- 距離を0～1へ正規化
- センサー値から各関節の振幅または位相を補正
- 補正重みを`ControllerGenome`として遺伝
- Raycast距離、角度、更新頻度は環境設定として共通化

## 再現性

現在はUnityのグローバル乱数を使用しており、乱数シードは外部設定されていません。実験再現性を高める場合は、設定へ`randomSeed`を追加し、開始時に`Random.InitState`を呼びます。

## 今後分離できる領域

`EvolutionManager`にはコース生成とリプレイ制御が残っています。規模が増えた場合は、`CourseBuilder`と`ReplayController`へ分離するのが次の候補です。
