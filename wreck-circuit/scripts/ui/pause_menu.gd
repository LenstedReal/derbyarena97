extends Control

const ORANGE := Color(1.0, 0.30, 0.08)
const CYAN := Color(0.24, 0.79, 0.91)
const PANEL := Color(0.025, 0.04, 0.055, 0.97)

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)

	var dim := ColorRect.new()
	dim.name = "PauseDim"
	dim.color = Color(0.006, 0.011, 0.018, 0.78)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	dim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(dim)

	var card := Panel.new()
	card.name = "PauseCard"
	card.anchor_left = 0.28
	card.anchor_top = 0.14
	card.anchor_right = 0.72
	card.anchor_bottom = 0.87
	card.add_theme_stylebox_override("panel", _style(PANEL, Color(0.91, 0.25, 0.08, 0.72), 16))
	add_child(card)

	var rail := ColorRect.new()
	rail.name = "AccentRail"
	rail.color = ORANGE
	rail.anchor_left = 0.28
	rail.anchor_top = 0.14
	rail.anchor_right = 0.72
	rail.anchor_bottom = 0.151
	rail.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(rail)

	var eyebrow := _label("SYSTEM PAUSE  /  DERBY ARENA", 12, Color(1.0, 0.55, 0.30))
	eyebrow.anchor_left = 0.32
	eyebrow.anchor_top = 0.205
	eyebrow.anchor_right = 0.68
	eyebrow.anchor_bottom = 0.255
	eyebrow.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(eyebrow)

	var title := _label("DURAKLATILDI", 34, Color(0.95, 0.97, 0.99))
	title.anchor_left = 0.32
	title.anchor_top = 0.26
	title.anchor_right = 0.68
	title.anchor_bottom = 0.35
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(title)

	var hint := _label("Araçlar durduruldu • Hazır olduğunda arenaya dön.", 12, Color(0.56, 0.64, 0.72))
	hint.anchor_left = 0.30
	hint.anchor_top = 0.355
	hint.anchor_right = 0.70
	hint.anchor_bottom = 0.405
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(hint)

	_btn("Resume", "DEVAM ET", 0.445, true)
	_btn("Restart", "YENİDEN BAŞLAT", 0.555)
	_btn("Menu", "ANA MENÜ", 0.665)
	$Resume.pressed.connect(_resume)
	$Restart.pressed.connect(_restart)
	$Menu.pressed.connect(_menu)

func open() -> void:
	visible = true
	mouse_filter = Control.MOUSE_FILTER_STOP

func close() -> void:
	visible = false
	mouse_filter = Control.MOUSE_FILTER_IGNORE

func _btn(node_name: String, text: String, top: float, primary: bool = false) -> void:
	var b := Button.new()
	b.name = node_name
	b.text = text
	b.anchor_left = 0.35
	b.anchor_top = top
	b.anchor_right = 0.65
	b.anchor_bottom = top + 0.085
	b.add_theme_font_size_override("font_size", 14)
	b.add_theme_color_override("font_color", Color(0.98, 0.98, 0.99))
	b.add_theme_color_override("font_hover_color", Color.WHITE)
	var base := Color(0.73, 0.20, 0.065) if primary else Color(0.065, 0.085, 0.11, 0.98)
	var edge := ORANGE if primary else Color(0.35, 0.43, 0.52, 0.58)
	b.add_theme_stylebox_override("normal", _style(base, edge, 8))
	b.add_theme_stylebox_override("hover", _style(Color(0.92, 0.29, 0.085) if primary else Color(0.12, 0.16, 0.2), CYAN if not primary else ORANGE, 8))
	b.add_theme_stylebox_override("pressed", _style(Color(0.49, 0.13, 0.045) if primary else Color(0.035, 0.05, 0.065), edge, 8))
	b.add_theme_stylebox_override("focus", _style(base, Color(1.0, 0.75, 0.43), 8))
	add_child(b)

func _label(text: String, size: int, color: Color) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l

func _style(bg: Color, edge: Color, radius: int) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = edge
	style.set_border_width_all(1)
	style.set_corner_radius_all(radius)
	style.content_margin_left = 12.0
	style.content_margin_right = 12.0
	style.content_margin_top = 7.0
	style.content_margin_bottom = 7.0
	return style

func _match() -> Node:
	return get_tree().get_first_node_in_group("match")

func _resume() -> void:
	var m := _match()
	if m != null:
		m.call("toggle_user_pause")

func _restart() -> void:
	get_tree().paused = false
	get_tree().reload_current_scene()

func _menu() -> void:
	get_tree().paused = false
	get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
