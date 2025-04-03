# ZoneInfoDisplay ヘルプ

このプログラムはタイムゾーンの詳細情報と遷移情報を表示します。

## 使用方法:
java ZoneInfoDisplay [タイムゾーンID] [表示モード] 
java ZoneInfoDisplay help - このヘルプを表示 
java ZoneInfoDisplay list - 利用可能なタイムゾーンIDの一覧を表示

### 引数:
- **タイムゾーンID**  
  例: `Asia/Tokyo`, `America/New_York`, `Europe/London`  
  デフォルト: `Asia/Tokyo`  

- **表示モード**  
  時刻の表示方法を指定:  
  - `U`: UTC時刻 (デフォルト)  
  - `O`: OS設定のタイムゾーン時刻  
  - `L`: 指定したタイムゾーンのローカル時刻  

### 例:
- `java ZoneInfoDisplay Asia/Tokyo U`  
  東京のタイムゾーン情報をUTC時刻で表示  

- `java ZoneInfoDisplay Europe/London L`  
  ロンドンのタイムゾーン情報をローカル時刻で表示  

- `java ZoneInfoDisplay America/New_York O`  
  ニューヨークのタイムゾーン情報をOS時刻で表示  

### その他コマンド:
- `java ZoneInfoDisplay list`  
  利用可能なタイムゾーンのリストを表示
