# AyanoTimer

ミーティング直前にカウントダウンとBGMを再生するWindowsタスクトレイ常駐アプリ。

## セットアップ

1. `AyanoTimer.exe` を好きなフォルダに配置
2. ダブルクリックで起動（初回はSmartScreen警告が出るので「詳細情報」→「実行」）
3. 設定画面が開くので、カレンダーURLを入力して「保存」

## カレンダーURL の取得方法

### Outlook (Microsoft 365)
1. https://outlook.office.com でカレンダーを開く
2. 設定（歯車）→「カレンダーの共有」
3. 共有したいカレンダーを選択 →「ICSリンク」を発行
4. そのURLをアプリの設定に貼り付け

### Google Calendar
1. Google カレンダーの設定を開く
2.「カレンダーの統合」→「iCal形式の非公開URL」をコピー
3. そのURLをアプリの設定に貼り付け

### ローカルファイル
Outlookからエクスポートした `.ics` ファイルを直接指定することもできます。
- 絶対パス: `C:\Users\you\calendar.ics`
- 相対パス（exeと同じフォルダ）: `calendar.ics`

## BGM の設定

1. exeと同じフォルダに `assets` フォルダを作成
2. `assets\bgm.mp3` に好きなBGMファイルを配置
3. 別のパスを使いたい場合は設定画面の「BGMファイル」で変更

### フリーBGM素材サイト
- [DOVA-SYNDROME](https://dova-s.jp/)
- [甘茶の音楽工房](https://amachamusic.chagasi.com/)
- [魔王魂](https://maou.audio/)

## 使い方

- タスクトレイの青いアイコンを右クリック →メニュー表示
- 「設定...」で設定画面を開く
- 「テスト再生」で動作確認（5秒後にカウントダウン開始）
- ミーティング開始30秒前（設定変更可）にカウントダウン＋BGMが自動再生
- 開始時刻になるとBGM停止＋「IS LIVE!」表示
- 60秒後に自動的にバナーが消える
- バナーはドラッグで移動可能、✕ボタンで手動クローズも可能

## ビルド（開発者向け）

```bash
# 必要なもの: .NET 8 SDK (WSL2)
# ビルド
./build.sh
# 出力: dist/AyanoTimer.exe
```

## 技術スタック

- C# / .NET 8
- Avalonia UI 11
- Ical.Net（iCalパース）
- NAudio（音声再生）
