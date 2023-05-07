# hakoniwa-unity-simasset-plugin
 A plugin for Unity to easily integrate assets into hakoniwa simulation environment.

## 本パッケージでできること

* 箱庭ロボット開発キットで、ロボットを組み立てできます
* 本環境だけで、作成したロボットの動作確認ができます
* 動作確認済みのロボットを Python で強化学習できます

## 前提条件

* OS
  * Windows/WSL2
    * net-tools がインストールされていること
* Unity Hub
  * Unity Hub 3.4.1以降
* Unity
  * Unity 2021.3.7f1以降
* [hakoniwa-base](https://github.com/toppers/hakoniwa-base/tree/ai)のインストール
  * 詳細は[こちら](https://qiita.com/kanetugu2018/items/65a57b6bc4bbab7e43d5#%E3%82%A4%E3%83%B3%E3%82%B9%E3%83%88%E3%83%BC%E3%83%AB%E6%89%8B%E9%A0%86)を参照

## 事前準備

`hakoniwa-base`をクローンします。

```
git clone -b ai --recursive https://github.com/toppers/hakoniwa-base.git
```

次に、以下のファイルを編集します。

```
hakoniwa-base/workspace/runtime/asset_def.txt
```

ファイルの内容を以下のように変更してください。

```
dev/ai/sample_robo.py:mqtt
```

変更差分：

```
-dev/ai/ai_qtable.py
+dev/ai/sample_robo.py:mqtt
```

## インストール手順

本リポジトリを `hakoniwa-base` と同じディレクトリ階層でクローンします。

```
git clone https://github.com/toppers/hakoniwa-unity-simasset-plugin.git
```

成功するとこうなります。

```
$ ls
hakoniwa-base
hakoniwa-unity-simasset-plugin
```

次に、必要な dll をインストールします。

```
cd hakoniwa-unity-simasset-plugin
```

```
bash install.bash
```

Unity Hub から、`hakoniwa-unity-simasset-plugin/plugin-srcs` を開き、
`Hakoniwa` シーンをダブルクリックしてください。

![image](https://user-images.githubusercontent.com/164193/236663723-e50cfc04-a6fb-4794-86c2-95adf65f7161.png)

成功すると、こうなります。

![image](https://user-images.githubusercontent.com/164193/236663731-690d69e4-2545-4e74-ac25-b089fc85019f.png)

まずは、ロボットの場所を確認しましょう。
ヒエラルキービューの`Robot/SampleRobo`をダブルクリックすると、ロボットがこのように見えます。

![image](https://user-images.githubusercontent.com/164193/236663767-01732e69-7797-4658-a09b-bd20bc0e22cb.png)

この状態で、`Window/Hakoniwa/Generate`をクリックすると、箱庭のコンフィグ情報が自動生成されます。

![image](https://user-images.githubusercontent.com/164193/236663809-ffd548ee-aa20-4324-a704-f2a1df7c5634.png)

## 動作確認方法

`SampleRobo`をお好きな場所に移動させてください。

次に、`hakoniwa-base`で以下のコマンドを実行します。

```
 bash docker/run.bash runtime
```

ログ：
```
ASSET_DEF=asset_def.txt
INFO: ACTIVATING MOSQUITTO
[38052.770022]~DLT~    9~INFO     ~FIFO /tmp/dlt cannot be opened. Retrying later...
INFO: ACTIVATING HAKO-MASTER
OPEN RECIEVER UDP PORT=172.25.195.216:54001
OPEN SENDER UDP PORT=172.25.195.216:54002
mqtt_url=mqtt://172.25.195.216:1883
PUBLISHER Connecting to the MQTT server...
PUBLISHER CONNECTED to the MQTT server...
delta_msec = 20
max_delay_msec = 100
INFO: shmget() key=255 size=80768
Server Start: 172.25.195.216:50051
INFO: ACTIVATING :dev/ai/sample_robo.py
START TB3 TEST
LOADED: SampleRobo
INFO: SampleRobo create_lchannel: logical_id=3 real_id=0 size=48
subscribe:channel_id=0
subscribe:typename=Bool
subscribe:pdu_size=4
subscribe:channel_id=1
subscribe:typename=CompressedImage
subscribe:pdu_size=1229064
subscribe:channel_id=2
subscribe:typename=LaserScan
subscribe:pdu_size=3044
WAIT START:
```

その後、Unityのシミュレーションを開始します。

![image](https://user-images.githubusercontent.com/164193/236664202-1b9fcd17-98b9-41bb-85dd-5a1ad1594d3b.png)

この状態で、`START`ボタンを押下して、`Sene`タブをみると、下図のようにロボットが動き出しているのが見えます。

![image](https://user-images.githubusercontent.com/164193/236664228-1e7b3799-f7d1-43d7-acbb-280ed672c44d.png)

シミュレーションを終わる時は、Unityのシミュレーションを停止し、hakoniwa-baseの実行コマンドを `CTRL+C`で停止してください。

## TODO

* Unityアセットパッケージ対応
* SHM対応
* Ubuntu対応
* Mac対応
* TB3モデル対応
* EV3モデル対応
