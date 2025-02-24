﻿### **📌 README.md（WPF タイマーアプリ）**

# ⏰ WPF タイマーアプリ

## 📖 概要
このアプリは、指定した分数ごとにアラートを表示する WPF アプリです。  
タイトルを設定し、カウントダウンタイマーが終了すると、メッセージボックスでアラートを通知します。  
アラートのログは CSV に記録され、アプリ内でリストとして表示されます。

---

## 🎯 主な機能
✅ **タイトルの入力（必須）**  
✅ **指定時間ごとにアラート表示（最前面）**  
✅ **カウントダウンタイマー表示**  
✅ **アラート履歴を CSV に保存**  
✅ **履歴をアプリ内でリスト表示（列ごとに表示）**  
✅ **開始・停止ボタンの適切な制御**  
✅ **ログのクリア機能**  

---

## 🖥 動作環境
- **OS:** Windows 10 / 11
- **.NET Version:** .NET Core 3.1 以上 または .NET 6 以上
- **開発環境:** Visual Studio 2019 / 2022

---

## 🚀 インストール方法
1. **リポジトリをクローン**

2. **Visual Studio でプロジェクトを開く**
   - `TimerAlertApp.sln` を開く

3. **NuGet パッケージを復元**
   - `dotnet restore` または Visual Studio の `NuGet Package Manager` で依存関係を復元

4. **ビルド & 実行**
   - Visual Studio の `F5` で実行
   - または `dotnet run`

---

## 🛠 使い方
### **1. タイトルを入力（必須）**
- アラートのタイトルを入力（例: `ミーティング開始`）
- **タイトルを入力しないと開始できません**

### **2. アラート時間を設定**
- `分` を入力（例: `10`）

### **3. `開始` ボタンを押す**
- カウントダウンが開始
- `開始` ボタンが無効化され、`停止` ボタンが有効化される

### **4. 指定時間後にアラート通知**
- `MessageBox` が **最前面で表示**
- アラート履歴が CSV (`log.csv`) に記録

### **5. `停止` ボタンでカウントダウンを停止**
- `開始時間` と `経過時間` がログに記録
- `開始` ボタンが再び有効化

### **6. ログの確認**
- アラート履歴が UI の `ListView` に表示
- `CSV (log.csv)` に保存され、再起動後もデータが保持される

### **7. `ログをクリア` ボタン**
- `ListView` の履歴を削除
- `log.csv` のデータも削除

---

## 📂 プロジェクト構成
```
📦 TimerAlertApp
 ┣ 📂 Assets         // アイコンや画像
 ┣ 📂 bin           // ビルドされた実行ファイル
 ┣ 📂 obj           // 一時的なビルドデータ
 ┣ 📜 App.xaml      // WPF アプリのエントリーポイント
 ┣ 📜 App.xaml.cs   // WPF アプリのイベントハンドラー
 ┣ 📜 MainWindow.xaml      // UI デザイン（XAML）
 ┣ 📜 MainWindow.xaml.cs   // UI のロジック（C#）
 ┣ 📜 log.csv       // アラートログデータ
 ┣ 📜 README.md     // 本ファイル
 ┣ 📜 TimerAlertApp.sln  // ソリューションファイル
 ┗ 📜 TimerAlertApp.csproj  // プロジェクトファイル
```

---

## ⚙ ビルド方法
### **Visual Studio を使用**
1. Visual Studio でプロジェクトを開く
2. `Ctrl + Shift + B` でビルド
3. `bin/Debug` または `bin/Release` に実行ファイルが生成される

### **コマンドラインからビルド**
```sh
dotnet build -c Release
```
- `bin/Release` に `exe` が生成される

---

## 📝 注意事項
- `.NET Core` 版を使う場合は `.NET Core 3.1` 以上が必要
- `.NET Framework` 版を使う場合は `4.7.2` 以上が必要
- `log.csv` はアプリのディレクトリに作成され、削除すると履歴が消えます

---

## 📜 ライセンス
MIT License

