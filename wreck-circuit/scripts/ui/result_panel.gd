extends Control

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var dim := ColorRect.new()
	dim.color = Color(0.05, 0.03, 0.02, 0.62)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	dim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(dim)
	var title := Label.new()
	title.name = "Title"
	title.layout_mode = 1
	title.anchor_left = 0.2
	title.anchor_top = 0.24
	title.anchor_right = 0.8
	title.anchor_bottom = 0.38
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 48)
	add_child(title)
	var again := Button.new()
	again.name = "Restart"
	again.text = "YENİDEN"
	again.layout_mode = 1
	again.anchor_left = 0.32
	again.anchor_top = 0.46
	again.anchor_right = 0.68
	again.anchor_bottom = 0.58
	add_child(again)
	var menu := Button.new()
	menu.name = "Menu"
	menu.text = "ANA MENÜ"
	menu.layout_mode = 1
	menu.anchor_left = 0.32
	menu.anchor_top = 0.62
	menu.anchor_right = 0.68
	menu.anchor_bottom = 0.74
	add_child(menu)
	again.pressed.connect(_restart)
	menu.pressed.connect(_menu)

func show_result(kind: String) -> void:
	visible = true
	mouse_filter = Control.MOUSE_FILTER_STOP
	match kind:
		"victory":
			$Title.text = "ZAFER"
		"defeat":
			$Title.text = "YENİLGİ"
		_:
			$Title.text = "BERABERE"

func _restart() -> void:
	get_tree().paused = false
	get_tree().reload_current_scene()

func _menu() -> void:
	get_tree().paused = false
	get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
