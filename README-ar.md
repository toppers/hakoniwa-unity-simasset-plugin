# ARアプリ向け箱庭Unityプラグライン

## 目次

- [はじめに](#はじめに)
- [動作環境](#動作環境)
- [設計情報](#設計情報)
- [設定情報](#設定情報)

## はじめに

Unity版の箱庭は、ARアプリを作成するための簡単なライブラリを提供します。この文書では、ライブラリの内容と使用方法について説明します。

## 動作環境

本ライブラリは以下の環境で動作することを確認しています。

- [X] iOS(iPhone/iPad)
- [ ] Android

# 設計情報

## アーキテクチャ

箱庭とAR端末は、UDP通信で接続します(下図)。現時点では、１対１の構成になります。

![スクリーンショット 2024-01-22 9 38 08](https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/b90f6aa6-31f5-4860-baae-f917e9e6014c)

通信ポート：

* AR端末：`54002` UDPポートで受信します。
* 箱庭は：`54001` UDPポートで受信します。

## 内部構成と通信データ

AR端末と箱庭側で登場するアクターは下図のとおりです。

![スクリーンショット 2024-01-22 9 50 09](https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/5573f90d-b1c9-4ced-9b55-adc7bda56bf8)

どちらにも Avator と Player が存在しており、AR端末と箱庭側とで対の関係になっています。
それぞれの役割は以下のとおりです。

* Player
  * 自律的に移動するアクターです。
  * 箱庭側のPlayerの例としては、仮想ロボットなどです。
  * AR端末側のPlayerの例としては、AR端末そのものになります。
* Avator
  * Playerの分身であり、Playerの動きに追従して動きます。
  * 箱庭側のAvatorの例としては、AR端末のアバターがあります。他にもAR端末内で自律的に動作するPlayerがいる場合は、それらも対象となります。
  * AR端末側のAvatorの例としては、箱庭側に存在する仮想ロボットなどです。

## 通信データ

AR端末と箱庭の間でやり取りする通信データは、Playerの名前、位置。姿勢および内部状態です。

* name：名前
* nameLength：名前の長さ
* position(x, y, z)：Unity座標系での位置
* rotation(x, y, z)：Unity座標系での姿勢
* state：内部状態（int型）

通信データの送信周期は、Unityの`Fixed Timestep`です。

## 内部クラス

内部クラス設計は下図のとおりです。

![スクリーンショット 2024-01-22 10 42 58](https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/5916f9ca-75d8-4de1-8109-3f1c9348247f)

ソース配置場所：

https://github.com/toppers/hakoniwa-unity-simasset-plugin/tree/main/plugin-srcs/Assets/Plugin/src/Unity/AR

Unityのゲームオブジェクトにアタッチするクラスは以下のとおり。

* HakoObjectSynchronizer
  * Emptyなゲームオブジェクトを作成し、アタッチします。
* ArAvatorObject
  * Avator対象となるゲームオブジェクトにアタッチします。
* ArPlayerObject
  * Player対象となるゲームオブジェクトにアタッチします。

# 設定情報

本ライブラリを利用するには、アタッチしたスクリプトに対して、各種設定を追加で行う必要があります。

## 箱庭側

`AR`(名前は任意です)というEmptyなゲームオブジェクトをHierachyビューに配置し、`HakoObjectSynchronizer` をアタッチします。

![スクリーンショット 2024-01-22 10 48 40](https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/9061614a-edb9-4d5d-a3ac-335975d6e611)

次に、Inspectorビューを参照し、`HakoObjectSynchronizer` の設定項目を埋めます。

* Players
  * `ArPlayerObject`をアタッチしたゲームオブジェクトを配列要素として全て追加します。
* Avators
  * `ArPlayerObject`をアタッチしたゲームオブジェクトを配列要素として全て追加します。
* Server_ipaddr
  * 箱庭を実行するマシンのIPアドレスを設定します。
* Server_portno
  * 箱庭を実行するマシンの受信UDPポート番号を設定します。54001がデフォルトです。
* Timeout_sec：UDP受信スレッドのタイムアウト値です（デフォルトのままで、変更不要です）。
* Scale：基本的には`1`を設定してください。もし、箱庭とAR端末とで縮尺を変更したい場合に利用できます。
* Client_ipaddr
  * AR端末のIPアドレスを設定します。
* Client_portno
  * AR端末の受信UDPポート番号を設定します。54002がデフォルトです。


## AR端末側


`AR`(名前は任意です)というEmptyなゲームオブジェクトをHierachyビューに配置し、`HakoObjectSynchronizer` をアタッチします。

![スクリーンショット 2024-01-22 10 59 45](https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/52690874-3d63-4006-96c7-ba51a8f0ef7a)

次に、Inspectorビューを参照し、`HakoObjectSynchronizer` の設定項目を埋めます。

* Players
  * `ArPlayerObject`をアタッチしたゲームオブジェクトを配列要素として全て追加します。
* Avators
  * `ArPlayerObject`をアタッチしたゲームオブジェクトを配列要素として全て追加します。
* Server_ipaddr
  * AR端末のIPアドレスを設定します。
* Server_portno
  * AR端末の受信UDPポート番号を設定します。54002がデフォルトです。
* Timeout_sec：UDP受信スレッドのタイムアウト値です（デフォルトのままで、変更不要です）。
* Scale：基本的には`1`を設定してください。もし、箱庭とAR端末とで縮尺を変更したい場合に利用できます。
* Client_ipaddr
  * 箱庭を実行するマシンのIPアドレスを設定します。
* Client_portno
  * 箱庭を実行するマシンの受信UDPポート番号を設定します。54001がデフォルトです。
