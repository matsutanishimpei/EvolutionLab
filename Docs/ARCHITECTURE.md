# アーキテクチャと拡張ガイド

## 実行フロー

```text
EvolutionConfigLoader
  -> CourseBuilder -> CourseLayout
  -> IndividualFactory -> SphereIndividual
  -> EvolutionManager -> GeneticAlgorithm / EvolutionRanking
                      -> ChampionReplayController
                           -> EvolutionCameraController / Recorder
```

`EvolutionManager` はサービスを組み立て、世代の開始・評価・更新を順番に呼び出すだけです。物体の作り方や評価式を変更しても、世代ループへ影響しにくい構成にしています。

## クラスの役割

| クラス | 役割 |
|---|---|
| `EvolutionManager` | 実験開始と世代ライフサイクルの進行 |
| `EvolutionConfig` | JSON設定の型、既定値、範囲検証 |
| `CourseBuilder` | 床・レーン・障害物の生成、表示、破棄 |
| `CourseLayout` | 風下・横方向、スタート地点、チェックポイント座標 |
| `IndividualFactory` | Genomeから個体を生成し、同じ評価条件を設定 |
| `SphereIndividual` | 身体構築、物理運動、計測状態 |
| `Genome` / `PartGene` | 個体全体の物理遺伝子と部品単位の遺伝子 |
| `GeneticAlgorithm` | 初期集団、エリート保存、親選択、交叉、突然変異 |
| `EvolutionRanking` | 評価確定、順位付け、ログ作成 |
| `FitnessEvaluator` | 計測値からfitnessを算出する純粋な評価式 |
| `ChampionReplayController` | 再走個体、録画開始・終了、追従カメラの制御 |
| `EvolutionHud` | 実験状態の画面表示 |

## Genomeの構造

`Genome.parts` は `PartGene` の配列です。サイズ、接続方向、関節振幅、周期、位相が同じ部品オブジェクトにまとまっているため、並行配列の添字ずれが起きません。

内部上限は現在5部品です。通常はJSONの `minimumParts` と `maximumParts` だけで2〜5部品の範囲を変更できます。6部品以上へ拡張するときだけ `Genome.MaxParts` を変更します。

## 遺伝子の範囲を変更する

浮動小数点の各遺伝子は、JSON内の同じ形式で管理します。

```json
"mass": {
  "initialMin": 0.5,
  "initialMax": 3,
  "min": 0.1,
  "max": 5,
  "mutationAmount": 0.2
}
```

- `initialMin` / `initialMax`: 第1世代の乱数範囲
- `min` / `max`: 突然変異後も超えない範囲
- `mutationAmount`: 1回の突然変異で加減する最大値

新しい数値遺伝子を追加する場合は、`GenomeSettings`、JSON、`Genome` の生成・交叉・突然変異・複製、そして `SphereIndividual` の反映処理を更新します。部品に属する値は `PartGene` へ、個体全体の値は `Genome` へ追加してください。

## ゲームルールを追加する

1. `EvolutionConfig` の適切な設定セクションへ既定値付きフィールドを追加します。
2. `evolution-config.json` に同名キーを追加します。
3. 値に制約があれば `EvolutionConfigLoader.Validate` で検証します。
4. 該当サービスで設定を参照します。
5. `Docs/CONFIGURATION.md` に型・単位・既定値を記載します。

## 今後の拡張候補

- 乱数seedと世代ごとのGenomeを保存し、実験を再現可能にする
- コース生成方式をインターフェース化し、複数ルールを差し替える
- Genomeと評価結果のバージョンを持ち、古い保存データを移行する
- `FitnessEvaluator` と遺伝操作へEditModeテストを追加する
- センサー入力と関節制御を別Controllerへ分離する

この構成は拡張箇所を局所化していますが、完成形を固定するものではありません。ルールの種類が増えた時点で、共通インターフェースを追加するのが適切です。
