# Contributing to EvolutionLab

EvolutionLabへのIssue、提案、ドキュメント改善、コード変更を歓迎します。

## 開発環境

- Unity 6
- Universal Render Pipeline
- C#
- Unity Recorder 5.1.6

## 変更を始める前に

大きな仕様変更、新しい進化ルール、Genome互換性を壊す変更は、先にIssueで目的と設計を共有してください。小さなバグ修正やドキュメント改善は、そのままPull Requestを作成できます。

## ブランチ

`main`から作業ブランチを作成してください。

```text
feature/short-description
fix/short-description
docs/short-description
```

## コーディング方針

- クラスの責務を小さく保つ
- 環境ルールをGenomeへ混ぜない
- 調整可能なゲームルールは`EvolutionConfig`へ置く
- 新しい設定には型、単位、既定値を定義する
- 新しい遺伝子は生成、交叉、突然変異、Clone、身体反映をすべて実装する
- Unity実行時に生成したMaterialやGameObjectを適切に破棄する
- 公開APIと複雑な物理処理にはコメントを付ける

## 設定変更

ゲームルールだけを変更する場合は、可能な限り`evolution-config.json`を使ってください。コード側の既定値や構造を変えた場合は、次も更新します。

- `EvolutionConfig.cs`
- `Assets/StreamingAssets/evolution-config.json`
- `Docs/CONFIGURATION.md`

## Genome変更チェックリスト

- [ ] フィールドを追加した
- [ ] 初期値生成を追加した
- [ ] 交叉を追加した
- [ ] 突然変異と範囲制限を追加した
- [ ] Cloneを追加した
- [ ] 個体への反映を追加した
- [ ] 単位と範囲を文書化した
- [ ] 既存Genomeとの互換性を検討した

## 動作確認

Pull Request前に次を確認してください。

1. Unity Consoleにコンパイルエラーがない
2. Play Modeへ入れる
3. 少なくとも2世代更新できる
4. fitnessランキングが出力される
5. 高さ違反、チェックポイント、接触ペナルティが意図どおり動く
6. 対象変更が録画へ関係する場合、MP4が正常に終了する

短時間で確認する場合は、設定JSONで個体数と評価時間を一時的に下げてください。テスト専用値はコミット前に戻します。

## Pull Request

Pull Requestには次を含めてください。

- 変更の目的
- 実装内容
- 動作確認方法と結果
- fitnessや進化傾向への影響
- 設定互換性、Genome互換性への影響
- 見た目の変更がある場合は画像または短い動画

無関係なUnity自動生成設定や大容量の録画ファイルを含めないでください。`Library`、`Temp`、`Logs`、`Recordings`はGit管理対象外です。

## コミット

1コミットへ無関係な変更を混ぜず、目的が分かる短いメッセージを使用してください。

例：

```text
Add configurable sensor range
Fix checkpoint order validation
Document fitness time units
```

## ライセンス

コントリビューションは、このリポジトリのMIT Licenseの下で提供されることに同意したものとして扱います。
