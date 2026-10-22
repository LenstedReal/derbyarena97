extends Control

# Mobile-first arcade controls: auto throttle + assisted aiming keep the
# player's thumbs focused on steering, firing and short tactical actions.
var state: Dictionary = {
	"throttle": 0.82,
	"brake": 0.0,
	"steer": 0.0,
	"boost": false,
	"handbrake": false,
	"fire": false,
}
var using_touch: bool = false
var aim_delta: float = 0.0
var _idx: Dictionary = {}
var _steer_center: Vector2 = Vector2.ZERO
var _zones: Dictionary = {}
var _zone_base: Dictionary = {}
var _knob: Control
var _hint: Label
var _hint_left: float = 8.0
var _using_mouse_controls: bool = false

func _ready() -> void:
	add_to_group("touch_controls")
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	using_touch = DisplayServer.is_touchscreen_available()
	_build_controls()
	_sync()

func _build_controls() -> void:
	# Invisible aim swipe only when aim-assist is disabled. It deliberately
	# starts below the HUD pause button so it cannot consume the pause tap.
	_zone("Aim", 0.48, 0.285, 0.985, 0.565, Color(0, 0, 0, 0), "", false, Color(0, 0, 0, 0), 12)
	_zone("Steer", 0.025, 0.605, 0.235, 0.955, Color(0.035, 0.055, 0.075, 0.23), "", true, Color(0.50, 0.68, 0.80, 0.58), 14)
	_knob = _zone("Knob", 0.085, 0.70, 0.175, 0.82, Color(0.64, 0.76, 0.86, 0.44), "", true, Color(0.85, 0.92, 0.98, 0.75), 14)
	_zone("Boost", 0.725, 0.565, 0.842, 0.705, Color(0.72, 0.30, 0.055, 0.82), "NİTRO", true, Color(1.0, 0.57, 0.20, 0.95), 14)
	_zone("Brake", 0.725, 0.725, 0.842, 0.865, Color(0.36, 0.10, 0.085, 0.84), "FREN / GERİ", true, Color(1.0, 0.37, 0.28, 0.90), 12)
	_zone("Hand", 0.595, 0.815, 0.705, 0.955, Color(0.08, 0.15, 0.21, 0.84), "DRİFT", true, Color(0.42, 0.72, 0.88, 0.78), 12)
	_zone("Fire", 0.855, 0.645, 0.985, 0.955, Color(0.72, 0.105, 0.045, 0.90), "ATEŞ\nOTO NİŞAN", true, Color(1.0, 0.43, 0.22, 0.98), 17)

	var steer_label := Label.new()
	steer_label.name = "SteerHint"
	steer_label.text = "DİREKSİYON"
	steer_label.anchor_left = 0.055
	steer_label.anchor_top = 0.565
	steer_label.anchor_right = 0.205
	steer_label.anchor_bottom = 0.602
	steer_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	steer_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	steer_label.add_theme_font_size_override("font_size", 10)
	steer_label.add_theme_color_override("font_color", Color(0.82, 0.89, 0.95, 0.72))
	steer_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(steer_label)

	_hint = Label.new()
	_hint.name = "ControlsHint"
	_hint.text = "OTOMATİK GAZ  •  SOL JOYSTICK İLE DÖN  •  ATEŞ: OTOMATİK NİŞAN"
	_hint.anchor_left = 0.27
	_hint.anchor_top = 0.112
	_hint.anchor_right = 0.73
	_hint.anchor_bottom = 0.155
	_hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_hint.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_hint.add_theme_font_size_override("font_size", 11)
	_hint.add_theme_color_override("font_color", Color(0.86, 0.90, 0.93, 0.90))
	_hint.add_theme_color_override("font_shadow_color", Color(0.0, 0.0, 0.0, 0.85))
	_hint.add_theme_constant_override("shadow_offset_x", 1)
	_hint.add_theme_constant_override("shadow_offset_y", 1)
	_hint.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_hint)

func _zone(node_name: String, a: float, b: float, c: float, d: float, color: Color, label: String, round: bool = false, border: Color = Color(0.4, 0.5, 0.6, 0.45), font_size: int = 14) -> Control:
	var box := Panel.new()
	box.name = node_name
	box.mouse_filter = Control.MOUSE_FILTER_IGNORE
	box.anchor_left = a
	box.anchor_top = b
	box.anchor_right = c
	box.anchor_bottom = d
	_zone_base[node_name] = {"bg": color, "border": border, "round": round}
	add_child(box)
	_zones[node_name] = box
	_apply_zone_style(node_name, false)
	if label != "":
		var t := Label.new()
		t.name = "Label"
		t.text = label
		t.mouse_filter = Control.MOUSE_FILTER_IGNORE
		t.set_anchors_preset(Control.PRESET_FULL_RECT)
		t.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		t.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		t.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		t.add_theme_font_size_override("font_size", font_size)
		t.add_theme_color_override("font_color", Color(1.0, 0.97, 0.93, 1.0))
		t.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.78))
		t.add_theme_constant_override("shadow_offset_x", 1)
		t.add_theme_constant_override("shadow_offset_y", 1)
		box.add_child(t)
	return box

func _apply_zone_style(node_name: String, pressed: bool) -> void:
	if not _zones.has(node_name) and not _zone_base.has(node_name):
		return
	var base: Dictionary = _zone_base.get(node_name, {})
	if base.is_empty():
		return
	var box: Control = _zones.get(node_name) as Control
	if box == null:
		# During construction the Panel is stored after the first style is made.
		return
	var style := StyleBoxFlat.new()
	var bg: Color = base["bg"]
	var border: Color = base["border"]
	style.bg_color = bg.lightened(0.12) if pressed else bg
	style.border_color = border.lightened(0.15) if pressed else border
	style.set_border_width_all(2 if pressed else 1)
	style.set_corner_radius_all(70 if bool(base["round"]) else 13)
	style.content_margin_left = 5.0
	style.content_margin_right = 5.0
	style.content_margin_top = 4.0
	style.content_margin_bottom = 4.0
	box.add_theme_stylebox_override("panel", style)

func _set_pressed_control(node_name: String, pressed: bool) -> void:
	if node_name == "Aim":
		return
	if node_name == "Knob":
		return
	if _zones.has(node_name):
		_apply_zone_style(node_name, pressed)
	if node_name == "Steer" and _knob != null:
		_knob.modulate = Color(1.08, 1.08, 1.08, 1.0) if pressed else Color.WHITE

func _hit(pos: Vector2) -> String:
	# Button zones come before the optional aim-swipe zone.
	for key in ["Fire", "Boost", "Brake", "Hand", "Steer"]:
		var ctl: Control = _zones[key]
		if ctl.get_global_rect().has_point(pos):
			return key
	if not Settings.auto_aim and _zones["Aim"].get_global_rect().has_point(pos):
		return "Aim"
	return ""

func _input(event: InputEvent) -> void:
	# Let pause/results UI receive touches normally; never eat menu taps.
	if get_tree().paused:
		_clear_controls()
		return

	if event is InputEventScreenTouch:
		using_touch = true
		_sync()
		var touch := event as InputEventScreenTouch
		if touch.pressed:
			var hit := _hit(touch.position)
			if hit == "":
				return
			_idx[touch.index] = hit
			if hit == "Steer":
				_steer_center = _zones["Steer"].get_global_rect().get_center()
				_apply_steer(touch.position.x)
			_set_pressed_control(hit, true)
			_sync()
			get_viewport().set_input_as_handled()
		else:
			if not _idx.has(touch.index):
				return
			var released := str(_idx[touch.index])
			_idx.erase(touch.index)
			if released == "Steer":
				state["steer"] = 0.0
				_reset_knob()
			if not _idx.values().has(released):
				_set_pressed_control(released, false)
			_sync()
			get_viewport().set_input_as_handled()

	elif event is InputEventScreenDrag:
		var drag := event as InputEventScreenDrag
		if not _idx.has(drag.index):
			return
		using_touch = true
		var hit := str(_idx[drag.index])
		if hit == "Steer":
			_apply_steer(drag.position.x)
		elif hit == "Aim":
			aim_delta += drag.relative.x
		get_viewport().set_input_as_handled()

	elif not using_touch and event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		var mouse := event as InputEventMouseButton
		if mouse.pressed:
			var hit_mouse := _hit(mouse.position)
			if hit_mouse == "":
				return
			_using_mouse_controls = true
			_idx[-1] = hit_mouse
			if hit_mouse == "Steer":
				_steer_center = _zones["Steer"].get_global_rect().get_center()
				_apply_steer(mouse.position.x)
			_set_pressed_control(hit_mouse, true)
			_sync()
			get_viewport().set_input_as_handled()
		else:
			if not _idx.has(-1):
				return
			var released_mouse := str(_idx[-1])
			_idx.erase(-1)
			if released_mouse == "Steer":
				state["steer"] = 0.0
				_reset_knob()
			if not _idx.values().has(released_mouse):
				_set_pressed_control(released_mouse, false)
			_sync()
			get_viewport().set_input_as_handled()

	elif not using_touch and event is InputEventMouseMotion and _idx.has(-1):
		var motion := event as InputEventMouseMotion
		var held_control := str(_idx[-1])
		if held_control == "Steer":
			_apply_steer(motion.position.x)
		elif held_control == "Aim":
			aim_delta += motion.relative.x

func _apply_steer(pointer_x: float) -> void:
	var rect: Rect2 = _zones["Steer"].get_global_rect()
	var radius := maxf(rect.size.x * 0.43, 1.0)
	var raw := clampf((pointer_x - _steer_center.x) / radius, -1.0, 1.0)
	# A small dead zone prevents drift from imperfect thumb placement.
	if absf(raw) < 0.075:
		raw = 0.0
	else:
		raw = signf(raw) * pow((absf(raw) - 0.075) / 0.925, 0.82)
	state["steer"] = clampf(raw * Settings.steer_sensitivity, -1.0, 1.0)
	_move_knob(float(state["steer"]))

func uses_custom_controls() -> bool:
	# Touch works on phones; on desktop this also makes clickable on-screen
	# controls feed the vehicle instead of silently falling back to the keyboard.
	return using_touch or _using_mouse_controls

func _sync() -> void:
	var held := {}
	for value in _idx.values():
		held[str(value)] = true
	var mobile_mode := using_touch or _using_mouse_controls
	var braking := held.has("Brake")
	# No need to hold a separate gas button while steering and fighting.
	state["throttle"] = 0.0 if braking else (0.82 if mobile_mode else 0.0)
	state["brake"] = 1.0 if braking else 0.0
	state["boost"] = held.has("Boost")
	state["handbrake"] = held.has("Hand")
	state["fire"] = held.has("Fire")
	if not held.has("Steer"):
		state["steer"] = 0.0
		_reset_knob()

func _move_knob(steer_v: float) -> void:
	if _knob == null:
		return
	var shift := clampf(steer_v, -1.0, 1.0) * 0.062
	_knob.anchor_left = 0.085 + shift
	_knob.anchor_right = 0.175 + shift

func _reset_knob() -> void:
	if _knob == null:
		return
	_knob.anchor_left = 0.085
	_knob.anchor_right = 0.175

func _clear_controls() -> void:
	var old_values: Array = _idx.values()
	_idx.clear()
	for value in old_values:
		_set_pressed_control(str(value), false)
	state["steer"] = 0.0
	state["brake"] = 0.0
	state["boost"] = false
	state["handbrake"] = false
	state["fire"] = false
	state["throttle"] = 0.82 if (using_touch or _using_mouse_controls) else 0.0
	aim_delta = 0.0
	_reset_knob()

func consume_aim() -> float:
	var result := aim_delta
	aim_delta = 0.0
	return result

func _process(delta: float) -> void:
	if _hint_left > 0.0:
		_hint_left = maxf(0.0, _hint_left - delta)
		if _hint != null:
			_hint.modulate.a = clampf(_hint_left / 1.5, 0.0, 1.0)
			_hint.visible = _hint_left > 0.0

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_PAUSED or what == NOTIFICATION_WM_WINDOW_FOCUS_OUT:
		_clear_controls()
		if what == NOTIFICATION_WM_WINDOW_FOCUS_OUT:
			_using_mouse_controls = false
			_sync()
