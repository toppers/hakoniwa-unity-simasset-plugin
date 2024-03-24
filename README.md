# hakoniwa-unity-simasset-plugin
 A plugin for Unity to easily integrate assets into hakoniwa simulation environment.

## 本パッケージでできること

* 箱庭ロボット開発キットで、ロボットを組み立てできます
* 本環境だけで、作成したロボットの動作確認ができます
* 動作確認済みのロボットを Python で強化学習できます

## 前提条件

* OS
  * OS
    * Windows, MacOS, Ubuntu
  * ツール類
    * net-tools がインストールされていること
    * unzip がインストールされていること
    * git がインストールされていること
* Unity Hub
  * Unity Hub 3.7.0以降
* Unity
  * Unity 2022.3.10f1以降

## インストール手順

本リポジトリを クローンします。

```
git clone https://github.com/toppers/hakoniwa-unity-simasset-plugin.git
```

次に、必要なライブラリ等をインストールします。

```
cd hakoniwa-unity-simasset-plugin
```

```
bash install.bash
```

### Unity起動

この状態で Unity Hub で当該プロジェクトを開きましょう。

注意：Unityエディタは、当該CPUアーキテクチャに対応したものをインストールしてご利用ください。

対象フォルダ：plugin-srcs

Unityのバージョン違いに起因するメッセージ（"Opening Project in Non-Matching Editor Installation"）が出る場合は、「Continue」として問題ありません。

以下のダイアログが出ますが、`Continue` してください。

![image](https://github.com/toppers/hakoniwa-unity-drone-model/assets/164193/e1fbc477-4edc-4e39-ab15-ccd6f0707f33)


次に、以下のダイアログが出ますので、`Ignore` してください。

![image](https://github.com/toppers/hakoniwa-unity-drone-model/assets/164193/7c03ae41-f988-44cb-9ac1-2263507d254d)


成功するとこうなります。

![image](https://github.com/toppers/hakoniwa-unity-drone-model/assets/164193/50398cfa-f6fc-4eef-9679-5442bbd9de76)

起動直後の状態ですと、コンソール上にたくさんエラーが出ています。原因は以下の２点です。
リンク先を参照して、順番に対応してください。

* [Newtonsoft.Json が不足している](https://github.com/toppers/hakoniwa-document/tree/main/troubleshooting/unity#unity%E8%B5%B7%E5%8B%95%E6%99%82%E3%81%ABnewtonsoftjson%E3%81%8C%E3%81%AA%E3%81%84%E3%81%A8%E3%81%84%E3%81%86%E3%82%A8%E3%83%A9%E3%83%BC%E3%81%8C%E5%87%BA%E3%82%8B)
* [gRPC のライブラリ利用箇所がエラー出力している](https://github.com/toppers/hakoniwa-document/blob/main/troubleshooting/unity/README.md#grpc-%E3%81%AE%E3%83%A9%E3%82%A4%E3%83%96%E3%83%A9%E3%83%AA%E5%88%A9%E7%94%A8%E7%AE%87%E6%89%80%E3%81%8C%E3%82%A8%E3%83%A9%E3%83%BC%E5%87%BA%E5%8A%9B%E3%81%97%E3%81%A6%E3%81%84%E3%82%8B)(Mac版のみ)


https://qiita.com/sakano/items/6fa16af5ceab2617fc0f

Unity起動したら、`Work` シーンをダブルクリックしてください。

![image](https://user-images.githubusercontent.com/164193/236663723-e50cfc04-a6fb-4794-86c2-95adf65f7161.png)

成功すると、こうなります。

![スクリーンショット 2024-03-25 6 32 56](https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/33d48ff1-2ef0-4fb9-bb13-305be01bc825)


まずは、ロボットの場所を確認しましょう。
ヒエラルキービューの`Robot`をダブルクリックすると、ロボットがこのように見えます。

![スクリーンショット 2024-03-25 6 34 03](https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/1aacf013-7939-4879-9032-d593004cd497)

この状態で、`Window/Hakoniwa/GenerateDebug`をクリックすると、箱庭のコンフィグ情報が自動生成されます。

![image](https://user-images.githubusercontent.com/164193/236663809-ffd548ee-aa20-4324-a704-f2a1df7c5634.png)


## TODO

- [ ] Unityアセットパッケージ対応
- [X] SHM対応
- [ ] Ubuntu対応
- [X] Mac対応
- [X] TB3モデル対応
- [X] EV3モデル対応
