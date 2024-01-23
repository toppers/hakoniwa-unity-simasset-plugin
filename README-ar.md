# ARアプリ向け箱庭Unityプラグライン

## 目次

- [はじめに](#はじめに)
- [動作環境](#動作環境)
- [設計情報](#設計情報)
- [設定情報](#設定情報)
- [Unity設定](#Unity設定)

## はじめに

Unity版の箱庭は、ARアプリを作成するための簡単なライブラリを提供します。この文書では、ライブラリの内容と使用方法について説明します。

## 動作環境

本ライブラリは以下の環境で動作することを確認しています。

- [X] iOS(iPhone/iPad)
- [ ] Android

また、本ライブラリを使用する場合は、Unityの以下のパッケージが必要となります。

* AR Foundation
* Apple ARKit XR Plugin

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


# Unity設定

## Build Settings

Build Settings を開きます。

![スクリーンショット 2023-09-21 12 24 36](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/2a8527bf-89e0-48f6-8152-e537120d0353)

Platformを iOS にして、`switch platform` を選択します。

![スクリーンショット 2023-09-21 12 25 15](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/d97d21c3-1eac-4e12-9625-7993228525bb)

既存のシーンを削除します。

![スクリーンショット 2023-09-21 12 25 59](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/9a0aa95e-7ba0-4dd1-85aa-c7e9106b2301)


## Package Manager

Package Managerで、Unityのレジストリを選択します。

![スクリーンショット 2023-09-21 12 29 20](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/cdb48954-c61c-4adc-a825-74d7549633e7)

`AR foundation` と `Apple ARKit XR Plugin` をインストールします。

![スクリーンショット 2023-09-21 12 30 01](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/e9435d15-047f-4c87-ac65-b56360f9c339)

最新バージョンd値は、`Apple ARKit XR Plugin` のエラーが出ることがあります。

```
BuildFailedException: ARKit requires a Camera Usage Description (Player Settings > iOS > Other Settings > Camera Usage Description)
```

この場合は、エラー内容に従って、`Camera Usage Description` に何か文字列を入れましょう。今回は、`Hakoniwa` と入れておきます。

![スクリーンショット 2024-01-23 7 20 05](https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/f4e46607-8ab4-4695-a45f-182c5b41014f)


## Project Settings

`XR Plugin-in Management` の `Apple ARKit` を選択します。

![スクリーンショット 2023-09-21 12 33 39](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/773c9956-9db9-42bb-8fc5-10dd12d7f528)

`Project vValidation` のエラーを解消するために、`Fix` をクリックします。

## ARDevice Scene の設定

ARDevice Scene を選択します。

![スクリーンショット 2023-09-21 12 36 38](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/661f2998-41b6-4369-8f9b-ba171a0f98e2)

Hierarchyビューの `AR` を選択し、インスペクタービューの `Server_ipaddr` に iphone のIPアドレスを設定します。
Client_ipaddr には、Mac の IPアドレスを設定します。

![スクリーンショット 2023-09-21 12 37 11](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/cb3ef341-8251-4bf1-bcfc-82fdf8f5c40e)


## Hakoniwa Scene の設定

Hakoniwa Scene を選択します。

Hierarchyビューの `AR` を選択し、インスペクタービューの `Server_ipaddr` に Mac のIPアドレスを設定します。
Client_ipaddr には、iphone の IPアドレスを設定します。

![スクリーンショット 2023-09-21 12 39 35](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/c71d57d9-7952-48e3-afa9-5a43f2e5535a)

# Unityビルド手順

`Build Settings` のシーンに、`ARDevice`を追加し、`Build` をクリックします。

![スクリーンショット 2023-09-21 12 42 04](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/a9e0b02d-071e-46d5-8a4b-a33bde1db715)

`Build`ディレクトリとして、適当な空の場所を作り、選択します。

![スクリーンショット 2023-09-21 12 43 50](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/6dd1a45a-16e2-4095-b022-3de088d15627)

ビルド成功すると、Xcodeでビルド可能なプロジェクトが作成されます。

![スクリーンショット 2023-09-21 12 45 42](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/7f593f6a-1e65-482f-bb18-3d15f7977c9c)



# iphone へのインストール手順

準備物：

* iphone
  * Appleアカウント認証が必要になります
* Mac

以下のステップで設定およびインストールします。

* iphoneをソフトウェアアップデートをする（最新にする）
  * 理由：https://harumi.sakura.ne.jp/wordpress/2018/10/22/debug/
* iphoneとMacをUSB接続し、Macを信頼する
* [iphoneをデベロッパーモードにする](#iphoneをデベロッパーモードにする)
* [XCodeで認証する](#XCodeで認証する)
* [Xcodeでのビルド](#Xcodeでのビルド)
* [当該アプリを信頼する](#当該アプリを信頼する)

## iphoneをデベロッパーモードにする

`設定` => `プライバシーとセキュリティ` => `デベロッパーモード`を `オン` にしてください。

もし、`デベロッパーモード`が表示されない場合は、「[XCodeで認証する](#XCodeで認証する)」と「[Xcodeでのビルド](#Xcodeでのビルド)」を先に実行することで表示されるようになります。

参考：https://zenn.dev/m_j_t/articles/17f6a8631b88f8

## XCodeで認証する

Xcodeの`Signing & Capabilities` を開き、下図のように指定してください。

<img width="838" alt="スクリーンショット 2024-01-23 9 02 12" src="https://github.com/toppers/hakoniwa-unity-simasset-plugin/assets/164193/640021bb-7370-48b9-b00c-92239eb84271">

注意：Bundle Identifier は、インストール対象端末後に変える必要がある


## Xcode でのビルド

デベロッパモードのiphoneとMacをUSB接続します。

次に、Buildディレクトリを Xcode でオープンします。

`Signing & Capabilities` で、以下のように Teamには AppleID と Bundle Identifier には、`com.hakoniwa-lab.picomodel` と入力して、ビルドボタンをクリックします。


![スクリーンショット 2023-09-21 12 49 25](https://github.com/toppers/hakoniwa-unity-picomodel/assets/164193/3c3b15a3-da45-4457-8d3a-27e3287ce925)

## 当該アプリを信頼する

`設定` => `一般` => `VPNとデバイス管理`を開き、当該ARアプリ(model)を信頼するようにしてください。
