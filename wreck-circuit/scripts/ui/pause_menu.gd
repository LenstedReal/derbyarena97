extends Control

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var dim := ColorRect.new()
	dim.color = Color(0, 0, 0, 0.55)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	dim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(dim)
	var title := Label.new()
	title.text = "DURAKLATILDI"
	title.layout_mode = 1
	title.anchor_left = 0.3
	title.anchor_top = 0.22
	title.anchor_right = 0.7
	title.anchor_bottom = 0.32
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 36)
	add_child(title)
	_btn("Resume", "DEVAM", 0.38)
	_btn("Restart", "YENİDEN", 0.50)
	_btn("Menu", "ANA MENÜ", 0.62)
	$Resume.pressed.connect(_resume)
	$Restart.pressed.connect(_restart)
	$Menu.pressed.connect(_menu)

func open() -> void:
	visible = true
	mouse_filter = Control.MOUSE_FILTER_STOP

func close() -> void:
	visible = false
	mouse_filter = Control.MOUSE_FILTER_IGNORE

func _btn(node_name: String, text: String, top: float) -> void:
	var b := Button.new()
	b.name = node_name
	b.text = text
	b.layout_mode = 1
	b.anchor_left = 0.32
	b.anchor_top = top
	b.anchor_right = 0.68
	b.anchor_bottom = top + 0.1
	add_child(b)

func _match():
	return get_tree().get_first_node_in_group("match")

func _resume() -> void:
	var m = _match()
	if m:
		m.toggle_user_pause()

func _restart() -> void:
	get_tree().paused = false
	get_tree().reload_current_scene()

func _menu() -> void:
	get_tree().paused = false
	get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
