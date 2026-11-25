extends Node
## 戰鬥結算中樞（autoload 名稱：Combat）。
##
## 架構（設計文件 4.9）：
##   碰撞偵測 (Area2D) → emit signal → Combat → 結算 → emit signal → 特效/UI
##
## 檯面上的東西只負責「回報我被打到了」，傷害怎麼算、連擊怎麼疊、
## 誰該噴什麼特效，全部在這裡決定。這樣之後加狀態效果才不會變成災難。

## kind: "bumper" | "sling" | "part" | "armor" | "core"
signal hit_resolved(pos: Vector2, damage: int, combo: int, killed: bool, kind: String)
signal combo_broken()

## 連擊視窗（秒，實際時間，不受 hitstop 影響）
@export var combo_window := 1.8
## 連擊上限，避免音高飆到聽不見
@export var combo_cap := 24

var combo := 0

var _last_hit_at := -999.0


func _now() -> float:
	return Time.get_ticks_msec() / 1000.0


## 檯面元件打完傷害後呼叫這支。
func report_hit(pos: Vector2, damage: int, kind: String, killed := false) -> void:
	var t := _now()
	if t - _last_hit_at <= combo_window:
		combo = mini(combo + 1, combo_cap)
	else:
		combo = 1
	_last_hit_at = t
	hit_resolved.emit(pos, damage, combo, killed, kind)


## 漏球、關卡重置時呼叫。
func break_combo() -> void:
	if combo > 0:
		combo_broken.emit()
	combo = 0
	_last_hit_at = -999.0
