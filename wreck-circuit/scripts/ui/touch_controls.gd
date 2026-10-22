extends Control

var state: Dictionary = {
	"throttle": 0.0,
	"brake": 0.0,
	"steer": 0.0,
	"boost": false,
	"handbrake": false,
	"fire": false,
}
var using_touch: bool = false
var aim_delta: float = 0.0
var _idx: Dictionary = {}
var _steer_origin: Vector2 = Vector2.ZERO
var _zones: Dictionary = {}
var _knob: Control

func _ready() -> void:
	add_to_group("touch_controls")
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_zone("Aim", 0.50, 0.16, 1.0, 1.0, Color(0, 0, 0, 0), "")
	_zone("Steer", 0.02, 0.52, 0.22, 0.96, Color(1, 1, 1, 0.14), "")
	_knob = _zone("Knob", 0.07, 0.66, 0.17, 0.86, Color(1, 1, 1, 0.28), "")
	_zone("Gas", 0.24, 0.70, 0.38, 0.96, Color(0.15, 0.55, 0.28, 0.78), "GAZ")
	_zone("Brake", 0.39, 0.74, 0.50, 0.96, Color(0.65, 0.22, 0.16, 0.78), "FREN")
	_zone("Boost", 0.62, 0.58, 0.76, 0.76, Color(0.85, 0.48, 0.12, 0.82), "BOOST")
	_zone("Hand", 0.62, 0.78, 0.76, 0.96, Color(0.25, 0.28, 0.34, 0.8), "EL FREN")
	_zone("Fire", 0.78, 0.60, 0.98, 0.96, Color(0.75, 0.18, 0.14, 0.84), "ATEŞ")

func consume_aim() -> float:
	var d := aim_delta
	aim_delta = 0.0
	return d

func _zone(node_name: String, a: float, b: float, c: float, d: float, color: Color, label: String) -> Control:
	var box := ColorRect.new()
	box.name = node_name
	box.color = color
	box.mouse_filter = Control.MOUSE_FILTER_IGNORE
	box.layout_mode = 1
	box.anchor_left = a
	box.anchor_top = b
	box.anchor_right = c
	box.anchor_bottom = d
	box.offset_left = 0
	box.offset_top = 0
	box.offset_right = 0
	box.offset_bottom = 0
	add_child(box)
	if label != "":
		var t := Label.new()
		t.text = label
		t.mouse_filter = Control.MOUSE_FILTER_IGNORE
		t.set_anchors_preset(Control.PRESET_FULL_RECT)
		t.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		t.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		t.add_theme_font_size_override("font_size", 20)
		t.add_theme_color_override("font_color", Color(1, 1, 1, 0.95))
		box.add_child(t)
	_zones[node_name] = box
	return box

func _hit(pos: Vector2) -> String:
	for key in ["Fire", "Boost", "Hand", "Gas", "Brake", "Steer"]:
		var ctl: Control = _zones[key]
		if ctl.get_global_rect().has_point(pos):
			return key
	if _zones["Aim"].get_global_rect().has_point(pos):
		return "Aim"
	return ""

func _input(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		using_touch = true
		if event.pressed:
			var hit := _hit(event.position)
			if hit == "":
				return
			_idx[event.index] = hit
			if hit == "Steer":
				_steer_origin = event.position
				state["steer"] = 0.0
			get_viewport().set_input_as_handled()
		else:
			if not _idx.has(event.index):
				return
			var hit := str(_idx[event.index])
			_idx.erase(event.index)
			if hit == "Steer":
				state["steer"] = 0.0
				_reset_knob()
			get_viewport().set_input_as_handled()
		_sync()
	elif event is InputEventScreenDrag:
		var drag := event as InputEventScreenDrag
		if not _idx.has(drag.index):
			return
		using_touch = true
		var hit := str(_idx[drag.index])
		if hit == "Steer":
			var dx: float = (drag.position.x - _steer_origin.x) / 90.0
			state["steer"] = clampf(dx, -1.0, 1.0) * Settings.steer_sensitivity
			_move_knob(float(state["steer"]))
		elif hit == "Aim":
			aim_delta += drag.relative.x
		get_viewport().set_input_as_handled()
	elif not using_touch and event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			var hitm := _hit(event.position)
			if hitm == "":
				return
			_idx[-1] = hitm
			if hitm == "Steer":
				_steer_origin = event.position
			get_viewport().set_input_as_handled()
		else:
			_idx.erase(-1)
			state["steer"] = 0.0
			_reset_knob()
			get_viewport().set_input_as_handled()
		_sync()
	elif not using_touch and event is InputEventMouseMotion and _idx.has(-1):
		var hit2 := str(_idx[-1])
		if hit2 == "Steer":
			state["steer"] = clampf((event.position.x - _steer_origin.x) / 90.0, -1.0, 1.0) * Settings.steer_sensitivity
			_move_knob(state["steer"])
		elif hit2 == "Aim":
			aim_delta += event.relative.x

func _sync() -> void:
	var held := {}
	for k in _idx.values():
		held[str(k)] = true
	state["throttle"] = 1.0 if held.has("Gas") else 0.0
	state["brake"] = 1.0 if held.has("Brake") else 0.0
	state["boost"] = held.has("Boost")
	state["handbrake"] = held.has("Hand")
	state["fire"] = held.has("Fire")
	if not held.has("Steer"):
		state["steer"] = 0.0
		_reset_knob()

func _move_knob(steer_v: float) -> void:
	if _knob == null:
		return
	var shift := clampf(steer_v, -1.0, 1.0) * 0.04
	_knob.anchor_left = 0.07 + shift
	_knob.anchor_right = 0.17 + shift

func _reset_knob() -> void:
	if _knob == null:
		return
	_knob.anchor_left = 0.07
	_knob.anchor_right = 0.17
