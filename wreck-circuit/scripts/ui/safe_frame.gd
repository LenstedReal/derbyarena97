extends MarginContainer

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	get_viewport().size_changed.connect(_apply)
	_apply()

func _apply() -> void:
	var win := DisplayServer.window_get_size()
	var safe := DisplayServer.get_display_safe_area()
	var vp := get_viewport().get_visible_rect().size
	var left := 18.0
	var top := 12.0
	var right := 18.0
	var bottom := 12.0
	if win.x > 8 and win.y > 8 and safe.size.x > 0 and safe.size.y > 0:
		var sx := vp.x / float(win.x)
		var sy := vp.y / float(win.y)
		left = maxf(left, safe.position.x * sx)
		top = maxf(top, safe.position.y * sy)
		right = maxf(right, (float(win.x) - safe.end.x) * sx)
		bottom = maxf(bottom, (float(win.y) - safe.end.y) * sy)
	add_theme_constant_override("margin_left", int(left))
	add_theme_constant_override("margin_top", int(top))
	add_theme_constant_override("margin_right", int(right))
	add_theme_constant_override("margin_bottom", int(bottom))
