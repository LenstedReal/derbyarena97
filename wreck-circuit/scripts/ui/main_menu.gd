extends Control

var _panel: Panel

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var bg := ColorRect.new()
	bg.color = Color(0.16, 0.09, 0.06)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	var safe := MarginContainer.new()
	safe.name = "SafeFrame"
	safe.set_script(load("res://scripts/ui/safe_frame.gd"))
	safe.set_anchors_preset(Control.PRESET_FULL_RECT)
	safe.anchor_right = 1.0
	safe.anchor_bottom = 1.0
	add_child(safe)
	var box := VBoxContainer.new()
	box.name = "MenuBox"
	box.alignment = BoxContainer.ALIGNMENT_CENTER
	box.add_theme_constant_override("separation", 14)
	box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	box.size_flags_vertical = Control.SIZE_EXPAND_FILL
	safe.add_child(box)
	var title := Label.new()
	title.text = "WRECK CIRCUIT"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 52)
	box.add_child(title)
	var sub := Label.new()
	sub.text = "Yatay tam ekran  ·  3 dk  ·  son kalan"
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	box.add_child(sub)
	_menu_btn(box, "Play", "SAVAŞ")
	_menu_btn(box, "Settings", "AYARLAR")
	_menu_btn(box, "Mute", "SES")
	_menu_btn(box, "Quit", "ÇIKIŞ")
	$SafeFrame/MenuBox/Play.pressed.connect(_play)
	$SafeFrame/MenuBox/Settings.pressed.connect(_toggle_settings)
	$SafeFrame/MenuBox/Mute.pressed.connect(_mute)
	$SafeFrame/MenuBox/Quit.pressed.connect(_quit)
	_build_panel(safe)
	_refresh_mute()
	Settings.changed.connect(_refresh_mute)

func _menu_btn(parent: Node, node_name: String, text: String) -> void:
	var b := Button.new()
	b.name = node_name
	b.text = text
	b.custom_minimum_size = Vector2(320, 64)
	b.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	parent.add_child(b)

func _build_panel(safe: Node) -> void:
	_panel = Panel.new()
	_panel.name = "SettingsPanel"
	_panel.visible = false
	_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	safe.add_child(_panel)
	var v := VBoxContainer.new()
	v.set_anchors_preset(Control.PRESET_FULL_RECT)
	v.offset_left = 24
	v.offset_top = 24
	v.offset_right = -24
	v.offset_bottom = -24
	v.add_theme_constant_override("separation", 10)
	_panel.add_child(v)
	var q := OptionButton.new()
	q.name = "Quality"
	q.add_item("Düşük", 0)
	q.add_item("Orta", 1)
	q.add_item("Yüksek", 2)
	q.selected = {"low": 0, "medium": 1, "high": 2}.get(Settings.quality, 1)
	v.add_child(q)
	var steer := HSlider.new()
	steer.name = "SteerSlider"
	steer.min_value = 0.5
	steer.max_value = 1.8
	steer.step = 0.05
	steer.value = Settings.steer_sensitivity
	steer.custom_minimum_size = Vector2(280, 40)
	v.add_child(steer)
	var cam := HSlider.new()
	cam.name = "CamSlider"
	cam.min_value = 0.5
	cam.max_value = 1.8
	cam.step = 0.05
	cam.value = Settings.camera_sensitivity
	cam.custom_minimum_size = Vector2(280, 40)
	v.add_child(cam)
	var vib := CheckBox.new()
	vib.name = "Vibrate"
	vib.text = "Titreşim"
	vib.button_pressed = Settings.vibration
	v.add_child(vib)
	var aim := CheckBox.new()
	aim.name = "AutoAim"
	aim.text = "Nişan yardımı"
	aim.button_pressed = Settings.auto_aim
	v.add_child(aim)
	var back := Button.new()
	back.text = "KAPAT"
	back.custom_minimum_size = Vector2(200, 56)
	v.add_child(back)
	q.item_selected.connect(_on_quality)
	steer.value_changed.connect(func(val: float): Settings.steer_sensitivity = val; Settings.save_settings())
	cam.value_changed.connect(func(val: float): Settings.camera_sensitivity = val; Settings.save_settings())
	vib.toggled.connect(func(on: bool): Settings.vibration = on; Settings.save_settings())
	aim.toggled.connect(func(on: bool): Settings.auto_aim = on; Settings.save_settings())
	back.pressed.connect(_toggle_settings)

func _on_quality(index: int) -> void:
	Settings.quality = ["low", "medium", "high"][index]
	Settings.save_settings()

func _play() -> void:
	Sfx.play_at(self, 440.0, 0.06)
	get_tree().change_scene_to_file("res://scenes/arena/arena_mvp.tscn")

func _toggle_settings() -> void:
	_panel.visible = not _panel.visible

func _mute() -> void:
	Settings.master_muted = not Settings.master_muted
	Settings.save_settings()
	if not Settings.master_muted:
		Sfx.play_at(self, 520.0, 0.07)
	_refresh_mute()

func _refresh_mute() -> void:
	var btn := $SafeFrame/MenuBox/Mute as Button
	btn.text = "SES KAPALI" if Settings.master_muted else "SES AÇIK"

func _quit() -> void:
	get_tree().quit()

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_GO_BACK_REQUEST:
		if _panel and _panel.visible:
			_panel.visible = false
		else:
			get_tree().quit()
