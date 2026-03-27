# 開発仕様書：ミーティング直前BGM再生アプリ（Windows用）

## プロジェクト概要

カレンダーの予定開始時刻をトリガーに、カウントダウン表示とBGM自動再生を行うWindowsタスクトレイ常駐アプリ。

**技術スタック：** Python（会社PCへの影響を最小化するため。配布は .exe 化）

---

## 技術選定の背景と制約

### 会社PC前提の制約

- 管理者権限が取得できない可能性がある
- API認証のためのブラウザOAuthフローが使えない可能性がある
- セキュリティポリシーで外部API通信が制限される可能性がある

### カレンダー連携方針：iCalendar (.ics) URL方式を採用

Google CalendarもOutlookも「カレンダーの公開URL（iCal形式）」を発行できる。このURLをアプリに登録するだけで予定を取得できる。**APIキー不要、OAuth不要、会社のセキュリティポリシーに引っかかりにくい。**

- Google Calendar: カレンダー設定 → 「カレンダーの統合」→ 「iCal形式の非公開URL」
- Outlook: カレンダー → 共有 → 「ICSリンク」

---

## 使用ライブラリ

```
pystray       # タスクトレイ常駐
Pillow        # pystray用アイコン画像処理
pygame        # BGM再生（mp3/wav対応）
icalendar     # .icsファイルのパース
requests      # iCal URLからのデータ取得
tkinter       # フローティングウィンドウ（Python標準ライブラリ）
pyinstaller   # .exe化（配布用）
```

---

## ディレクトリ構成

```
meeting-bgm/
├── main.py              # エントリーポイント
├── tray.py              # タスクトレイ管理
├── calendar_watcher.py  # カレンダー監視・予定取得
├── countdown_window.py  # フローティングカウントダウンUI
├── audio_player.py      # BGM再生
├── config.py            # 設定管理（ics URL、音声ファイルパス等）
├── config.json          # ユーザー設定ファイル（初回起動時に生成）
├── assets/
│   ├── icon.png         # タスクトレイアイコン
│   └── bgm.mp3          # デフォルトBGM（BBCニュース風などユーザーが配置）
└── requirements.txt
```

---

## 設定ファイル仕様（config.json）

初回起動時に生成。タスクトレイの右クリックメニュー「設定」から編集UIを提供する。

```json
{
  "ical_url": "",
  "bgm_file_path": "assets/bgm.mp3",
  "alert_seconds_before": 30,
  "window_position": "bottom-right",
  "meeting_keywords": ["ミーティング", "会議", "MTG", "sync", "meet", "zoom", "teams"],
  "auto_start": false
}
```

| キー | 説明 |
|---|---|
| `ical_url` | iCal形式のカレンダーURL（必須設定） |
| `bgm_file_path` | 再生するBGMファイルパス |
| `alert_seconds_before` | 何秒前からカウントダウン開始するか |
| `window_position` | フローティングウィンドウの表示位置 |
| `meeting_keywords` | ミーティング判定に使うキーワード（部分一致） |
| `auto_start` | Windows起動時に自動起動するか |

---

## 各モジュールの実装仕様

### calendar_watcher.py

- 5分に1回、iCal URLにGETリクエストして予定リストを更新
- `icalendar` ライブラリで `.ics` をパース
- `meeting_keywords` に一致する予定のみを対象とする
- 次の1件（直近の未来の予定）を返す `get_next_meeting()` 関数を実装
- タイムゾーンは `Asia/Tokyo` に統一して処理

```python
def get_next_meeting() -> dict | None:
    """
    Returns:
        {"title": str, "start": datetime} or None
    """
```

### countdown_window.py

- `tkinter` でフローティングウィンドウを実装
- `overrideredirect(True)` でタイトルバーを消す
- `attributes('-topmost', True)` で常に最前面に表示
- `attributes('-alpha', 0.85)` で半透明にする
- 画面右下に配置（タスクバーと重ならない位置に調整）
- 表示内容：
  - 予定タイトル（上部、小文字）
  - カウントダウン `00:30` 形式（中央、大きいフォント）
  - 開始時刻になったら `🔴 IS LIVE!` 表示に切り替え
- `IS LIVE!` 表示から60秒後に自動的にウィンドウを閉じる
- ウィンドウのドラッグ移動に対応する

### audio_player.py

- `pygame.mixer` を使用
- `play(file_path)` と `stop()` を実装
- BGMはフェードアウトせずミーティング開始時に即停止（IS LIVE切替と同時）
- 音声ファイルが見つからない場合はエラーを出さずにスキップし、ログに記録

### tray.py

- `pystray` でタスクトレイアイコンを常駐
- 右クリックメニュー：
  - 次の予定を表示（タイトルと開始時刻）
  - 設定を開く
  - 今すぐテスト（カウントダウン＋BGM動作確認用）
  - 終了

---

## 動作フロー

```
起動
  ↓
config.json 読み込み（なければ初期設定UIを表示）
  ↓
タスクトレイにアイコンを表示
  ↓
バックグラウンドスレッドで監視ループ開始（5秒ごとに時刻チェック）
  ↓
[次のミーティング開始まで alert_seconds_before 秒以内になったら]
  ↓
フローティングウィンドウ表示 + BGM再生開始
  ↓
カウントダウン（リアルタイム更新）
  ↓
開始時刻になったら → BGM停止 + "IS LIVE!" 表示
  ↓
60秒後にウィンドウ自動クローズ
  ↓
次の予定の監視を再開
```

---

## エラーハンドリング

| ケース | 対応 |
|---|---|
| iCal URLが未設定 | 起動時に設定UIを表示、タスクトレイアイコンに警告バッジ |
| iCal URLへのアクセス失敗 | 前回取得済みのキャッシュを使用、5分後にリトライ |
| BGMファイルが見つからない | BGMなしでカウントダウンのみ動作、ログに記録 |
| icalendar パースエラー | ログに記録してスキップ |
| ネットワーク未接続 | キャッシュ使用、復旧後自動リトライ |

---

## .exe 化の方法（PyInstaller）

```bash
pip install pyinstaller
pyinstaller --onefile --windowed --icon=assets/icon.ico --name=MeetingBGM main.py
```

- `--windowed` : コンソールウィンドウを非表示
- `--onefile` : 1つの .exe にまとめる（管理者権限不要で配布可能）
- 生成された `dist/MeetingBGM.exe` を奥さんのPCに渡す

---

## Windows自動起動の設定方法（オプション）

アプリ内「設定 → Windows起動時に自動起動」をONにすると、以下のレジストリキーに登録する：

```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

`HKEY_CURRENT_USER` を使用するため**管理者権限不要**。

---

## 実装の優先順位

1. **Phase 1（最小動作版）**
   - iCal URL読み込み＋次の予定取得
   - タスクトレイ常駐
   - カウントダウンウィンドウ表示
   - BGM再生

2. **Phase 2（使いやすさ向上）**
   - 設定UI
   - 「今すぐテスト」機能
   - Windows自動起動対応

3. **Phase 3（仕上げ）**
   - .exe 化
   - エラー時のユーザー向けメッセージ改善
   - アイコンのデザイン

---

## 補足メモ

- 対象ユーザーは技術者でないため、設定は最小限にする
- BGMファイルはユーザーが自分で用意して `assets/bgm.mp3` に置く運用
- BBCニュースのテーマ等は著作権があるため、アプリにバンドルしない
- テスト用のフリー素材サイトへの案内をREADMEに記載する
