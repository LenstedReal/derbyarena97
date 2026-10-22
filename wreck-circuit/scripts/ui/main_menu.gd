extends Control

const PLAYER_MODELS := {
	"muscle": preload("res://assets/models/vehicles/player_muscle.glb"),
	"raider_a": preload("res://assets/models/vehicles/enemy_raider_a.glb"),
	"raider_b": preload("res://assets/models/vehicles/enemy_raider_b.glb"),
	"raider_c": preload("res://assets/models/vehicles/enemy_raider_c.glb"),
	"boss": preload("res://assets/models/vehicles/enemy_boss_warrig.glb"),
}
const MODEL_LABELS := {
	"muscle": "V8 MUSCLE",
	"raider_a": "RAIDER / A",
	"raider_b": "RAIDER / B",
	"raider_c": "RAIDER / C",
	"boss": "WAR RIG",
}
const MODEL_ORDER := ["muscle", "raider_a", "raider_b", "raider_c", "boss"]

var _panel: PanelContainer
var _content: Control
var _mute_button: Button
var _garage_label: Label
var _preview_root: Node3D
var _preview_car: Node3D
var _preview_model: Node3D
var _garage_index: int = 0
var _garage_changed_at: float = 0.0

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP
	_build_background()
	_build_car_preview()
	_build_main_panel()
	_build_settings_panel()
	_refresh_mute()
	Settings.changed.connect(_refresh_mute)

func _process(delta: float) -> void:
	if _preview_car != null:
		_preview_car.rotation.y += delta * 0.14
	if _garage_changed_at > 0.0:
		_garage_changed_at = maxf(0.0, _garage_changed_at - delta)

func _build_background() -> void:
	var bg := ColorRect.new()
	bg.name = "Backdrop"
	bg.color = Color(0.025, 0.035, 0.052, 1.0)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	# A restrained industrial HUD grid rather than a blank flat menu screen.
	var glow := ColorRect.new()
	glow.name = "ArenaGlow"
	glow.color = Color(0.30, 0.085, 0.035, 0.23)
	glow.anchor_left = 0.34
	glow.anchor_top = 0.0
	glow.anchor_right = 1.0
	glow.anchor_bottom = 1.0
	glow.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(glow)

	for i in 9:
		var line := ColorRect.new()
		line.name = "GridLine_%02d" % i
		line.color = Color(0.55, 0.61, 0.68, 0.035)
		line.anchor_left = 0.38 + float(i) * 0.065
		line.anchor_right = line.anchor_left + 0.0008
		line.anchor_top = 0.04
		line.anchor_bottom = 0.96
		line.mouse_filter = Control.MOUSE_FILTER_IGNORE
		add_child(line)

	var accent := ColorRect.new()
	accent.name = "AccentRail"
	accent.color = Color(1.0, 0.25, 0.075, 0.9)
	accent.anchor_left = 0.0
	accent.anchor_top = 0.0
	accent.anchor_right = 1.0
	accent.anchor_bottom = 0.007
	accent.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(accent)

func _build_car_preview() -> void:
	var container := SubViewportContainer.new()
	container.name = "VehicleShowcase"
	container.anchor_left = 0.34
	container.anchor_top = 0.035
	container.anchor_right = 1.0
	container.anchor_bottom = 0.96
	container.stretch = true
	container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(container)

	var viewport := SubViewport.new()
	viewport.name = "VehicleViewport"
	viewport.size = Vector2i(960, 760)
	viewport.transparent_bg = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	container.add_child(viewport)

	var world := Node3D.new()
	world.name = "ShowcaseWorld"
	viewport.add_child(world)

	var env_node := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_CLEAR_COLOR
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.50, 0.59, 0.71)
	env.ambient_light_energy = 0.72
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env_node.environment = env
	world.add_child(env_node)

	var key := DirectionalLight3D.new()
	key.name = "KeyLight"
	key.rotation_degrees = Vector3(-32, -38, 0)
	key.light_color = Color(1.0, 0.80, 0.61)
	key.light_energy = 1.7
	key.shadow_enabled = true
	world.add_child(key)

	var rim := OmniLight3D.new()
	rim.name = "OrangeRimLight"
	rim.position = Vector3(-3.2, 1.8, 0.6)
	rim.light_color = Color(1.0, 0.20, 0.055)
	rim.light_energy = 2.8
	rim.omni_range = 8.0
	rim.shadow_enabled = false
	world.add_child(rim)

	var cool := OmniLight3D.new()
	cool.name = "CoolFillLight"
	cool.position = Vector3(3.5, 1.4, 2.0)
	cool.light_color = Color(0.12, 0.48, 1.0)
	cool.light_energy = 1.25
	cool.omni_range = 7.0
	cool.shadow_enabled = false
	world.add_child(cool)

	var pedestal := MeshInstance3D.new()
	pedestal.name = "DisplayPlinth"
	var cylinder := CylinderMesh.new()
	cylinder.top_radius = 2.65
	cylinder.bottom_radius = 2.82
	cylinder.height = 0.24
	cylinder.radial_segments = 64
	pedestal.mesh = cylinder
	pedestal.position.y = -0.26
	var plinth_mat := StandardMaterial3D.new()
	plinth_mat.albedo_color = Color(0.055, 0.073, 0.095)
	plinth_mat.metallic = 0.68
	plinth_mat.roughness = 0.32
	pedestal.material_override = plinth_mat
	world.add_child(pedestal)

	var plinth_ring := MeshInstance3D.new()
	plinth_ring.name = "PlinthRing"
	var torus := TorusMesh.new()
	torus.inner_radius = 2.54
	torus.outer_radius = 2.61
	torus.rings = 96
	torus.ring_segments = 8
	plinth_ring.mesh = torus
	plinth_ring.position.y = -0.13
	var ring_mat := StandardMaterial3D.new()
	ring_mat.albedo_color = Color(1.0, 0.23, 0.055)
	ring_mat.emission_enabled = true
	ring_mat.emission = Color(1.0, 0.12, 0.025)
	ring_mat.emission_energy_multiplier = 1.8
	plinth_ring.material_override = ring_mat
	world.add_child(plinth_ring)

	var camera := Camera3D.new()
	camera.name = "ShowcaseCamera"
	camera.position = Vector3(3.5, 2.25, -5.5)
	camera.fov = 39.0
	world.add_child(camera)
	camera.look_at(Vector3(0.0, 0.56, 0.0), Vector3.UP)
	camera.current = true

	_preview_root = world
	_refresh_preview()

func _refresh_preview() -> void:
	if _preview_car != null and is_instance_valid(_preview_car):
		_preview_car.queue_free()
	_preview_car = Node3D.new()
	_preview_car.name = "ShowcaseCar"
	_preview_car.rotation.y = -0.18
	_preview_root.add_child(_preview_car)
	var selected := Settings.vehicle_skin
	if selected not in MODEL_ORDER:
		selected = "muscle"
		Settings.vehicle_skin = selected
	var model_scene: PackedScene = PLAYER_MODELS[selected]
	_preview_model = model_scene.instantiate() as Node3D
	if _preview_model != null:
		_preview_model.name = "ShowcaseModel"
		_preview_model.scale = Vector3.ONE * (0.34 if selected == "boss" else 0.72)
		_preview_model.position.y = -0.12
		_preview_car.add_child(_preview_model)
	if _garage_label != null:
		_garage_label.text = "%s  /  ARAÇ DEĞİŞTİR" % MODEL_LABELS[selected]

func _build_main_panel() -> void:
	var safe := MarginContainer.new()
	safe.name = "SafeFrame"
	safe.set_anchors_preset(Control.PRESET_FULL_RECT)
	safe.set_script(load("res://scripts/ui/safe_frame.gd"))
	add_child(safe)

	_content = Control.new()
	_content.name = "MenuContent"
	_content.set_anchors_preset(Control.PRESET_FULL_RECT)
	_content.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_content.size_flags_vertical = Control.SIZE_EXPAND_FILL
	safe.add_child(_content)

	var panel := PanelContainer.new()
	panel.name = "MainPanel"
	panel.anchor_left = 0.025
	panel.anchor_top = 0.055
	panel.anchor_right = 0.445
	panel.anchor_bottom = 0.945
	panel.add_theme_stylebox_override("panel", _panel_style(Color(0.035, 0.048, 0.065, 0.94), Color(1.0, 0.25, 0.075, 0.68), 14))
	_content.add_child(panel)

	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 24)
	margin.add_theme_constant_override("margin_right", 24)
	margin.add_theme_constant_override("margin_top", 18)
	margin.add_theme_constant_override("margin_bottom", 15)
	panel.add_child(margin)

	var box := VBoxContainer.new()
	box.name = "MenuBox"
	box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	box.size_flags_vertical = Control.SIZE_EXPAND_FILL
	box.add_theme_constant_override("separation", 8)
	margin.add_child(box)

	var eyebrow := _label("Eyebrow", "K O L İ Z Y U M   /   SEZON 01", 13, Color(1.0, 0.47, 0.22))
	box.add_child(eyebrow)

	var title := _label("Title", "DERBY", 54, Color(0.94, 0.96, 0.98))
	title.add_theme_font_size_override("font_size", 54)
	box.add_child(title)
	var title2 := _label("TitleAccent", "ARENA", 50, Color(1.0, 0.29, 0.075))
	title2.add_theme_font_size_override("font_size", 50)
	box.add_child(title2)

	var subtitle := _label("Subtitle", "METAL. BARUT. SON KALAN ARAÇ.\nKolezyumda hayatta kal, hurdayı ganimete çevir.", 15, Color(0.65, 0.71, 0.78))
	subtitle.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	box.add_child(subtitle)

	var statrow := HBoxContainer.new()
	statrow.name = "StatRow"
	statrow.add_theme_constant_override("separation", 8)
	box.add_child(statrow)
	statrow.add_child(_stat_card("03:00", "SAVAŞ SÜRESİ"))
	statrow.add_child(_stat_card("04", "RAKİP ARAÇ"))
	statrow.add_child(_stat_card("01", "ARENA"))

	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	spacer.custom_minimum_size.y = 2
	box.add_child(spacer)

	_menu_btn(box, "Play", "▶    ARENAYA GİR", true).pressed.connect(_play)
	_menu_btn(box, "Garage", "◈    GARAJ / ARAÇ DEĞİŞTİR").pressed.connect(_cycle_vehicle)
	_garage_label = _label("GarageSkin", "", 11, Color(0.99, 0.56, 0.28))
	box.add_child(_garage_label)
	_menu_btn(box, "Settings", "⚙    AYARLAR").pressed.connect(_toggle_settings)
	_mute_button = _menu_btn(box, "Mute", "◖  SES AÇIK")
	_mute_button.pressed.connect(_mute)

	var footer_space := Control.new()
	footer_space.custom_minimum_size.y = 2
	box.add_child(footer_space)
	var controls := _label("ControlsHint", "MOBİL: OTOMATİK GAZ / SOL DİREKSİYON / ATEŞ + OTO NİŞAN\nFREN-GERİ  ·  NİTRO  ·  DRİFT     |     PC: WASD / F / SPACE / SHIFT", 10, Color(0.48, 0.55, 0.63))
	controls.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	box.add_child(controls)
	var version := _label("Version", "DERBY ARENA  //  v1.0.5   •   OFFLINE COMBAT", 10, Color(0.33, 0.4, 0.48))
	box.add_child(version)

	var status := PanelContainer.new()
	status.name = "CombatStatus"
	status.anchor_left = 0.72
	status.anchor_top = 0.035
	status.anchor_right = 0.975
	status.anchor_bottom = 0.105
	status.add_theme_stylebox_override("panel", _panel_style(Color(0.03, 0.045, 0.06, 0.84), Color(0.22, 0.63, 0.79, 0.5), 9))
	_content.add_child(status)
	var status_label := _label("StatusLabel", "●  COMBAT SYSTEMS ONLINE     /     1.0.5", 11, Color(0.45, 0.84, 0.91))
	status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	status.add_child(status_label)

	_refresh_preview()

func _build_settings_panel() -> void:
	_panel = PanelContainer.new()
	_panel.name = "SettingsPanel"
	_panel.visible = false
	_panel.anchor_left = 0.23
	_panel.anchor_top = 0.12
	_panel.anchor_right = 0.77
	_panel.anchor_bottom = 0.88
	_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.025, 0.04, 0.055, 0.98), Color(1.0, 0.27, 0.08, 0.8), 14))
	_content.add_child(_panel)

	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 24)
	margin.add_theme_constant_override("margin_right", 24)
	margin.add_theme_constant_override("margin_top", 20)
	margin.add_theme_constant_override("margin_bottom", 20)
	_panel.add_child(margin)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 11)
	margin.add_child(v)
	v.add_child(_label("SettingsTitle", "SİSTEM AYARLARI", 28, Color(0.96, 0.97, 0.99)))
	v.add_child(_label("SettingsInfo", "Telefon performansı, sürüş hissi ve nişan yardımı.", 13, Color(0.62, 0.68, 0.75)))

	var q := OptionButton.new()
	q.name = "Quality"
	q.add_item("DÜŞÜK  /  30+ FPS hedefi", 0)
	q.add_item("ORTA  /  Dengeli", 1)
	q.add_item("YÜKSEK  /  En iyi görüntü", 2)
	q.selected = {"low": 0, "medium": 1, "high": 2}.get(Settings.quality, 1)
	_style_option(q)
	v.add_child(q)

	var steer_title := _label("SteerTitle", "DİREKSİYON HASSASİYETİ", 12, Color(0.87, 0.9, 0.94))
	v.add_child(steer_title)
	var steer := HSlider.new()
	steer.name = "SteerSlider"
	steer.min_value = 0.5
	steer.max_value = 1.8
	steer.step = 0.05
	steer.value = Settings.steer_sensitivity
	v.add_child(steer)

	var cam_title := _label("CameraTitle", "NİŞAN HASSASİYETİ", 12, Color(0.87, 0.9, 0.94))
	v.add_child(cam_title)
	var cam := HSlider.new()
	cam.name = "CamSlider"
	cam.min_value = 0.5
	cam.max_value = 1.8
	cam.step = 0.05
	cam.value = Settings.camera_sensitivity
	v.add_child(cam)

	var options := HBoxContainer.new()
	options.add_theme_constant_override("separation", 24)
	var vib := CheckBox.new()
	vib.name = "Vibrate"
	vib.text = "Titreşim"
	vib.button_pressed = Settings.vibration
	options.add_child(vib)
	var aim := CheckBox.new()
	aim.name = "AutoAim"
	aim.text = "Nişan yardımı"
	aim.button_pressed = Settings.auto_aim
	options.add_child(aim)
	v.add_child(options)

	var grow := Control.new()
	grow.size_flags_vertical = Control.SIZE_EXPAND_FILL
	v.add_child(grow)
	var back := _menu_btn(v, "SettingsBack", "KAYDET  /  GERİ DÖN", true)
	back.pressed.connect(_toggle_settings)
	q.item_selected.connect(_on_quality)
	steer.value_changed.connect(func(val: float): Settings.steer_sensitivity = val; Settings.save_settings())
	cam.value_changed.connect(func(val: float): Settings.camera_sensitivity = val; Settings.save_settings())
	vib.toggled.connect(func(on: bool): Settings.vibration = on; Settings.save_settings())
	aim.toggled.connect(func(on: bool): Settings.auto_aim = on; Settings.save_settings())

func _stat_card(value: String, caption: String) -> PanelContainer:
	var card := PanelContainer.new()
	card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	card.custom_minimum_size = Vector2(0, 52)
	card.add_theme_stylebox_override("panel", _panel_style(Color(0.055, 0.071, 0.09, 0.92), Color(0.37, 0.43, 0.5, 0.22), 7))
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 0)
	var val_label := _label("Value", value, 18, Color(0.98, 0.98, 0.99))
	var caption_label := _label("Caption", caption, 9, Color(0.48, 0.56, 0.64))
	v.add_child(val_label)
	v.add_child(caption_label)
	card.add_child(v)
	return card

func _menu_btn(parent: Node, node_name: String, text: String, primary: bool = false) -> Button:
	var b := Button.new()
	b.name = node_name
	b.text = text
	b.custom_minimum_size = Vector2(0, 45 if not primary else 50)
	b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	b.alignment = HORIZONTAL_ALIGNMENT_LEFT
	b.add_theme_font_size_override("font_size", 15 if primary else 13)
	b.add_theme_color_override("font_color", Color(0.99, 0.97, 0.94))
	b.add_theme_color_override("font_hover_color", Color.WHITE)
	b.add_theme_color_override("font_pressed_color", Color.WHITE)
	var normal_color := Color(0.78, 0.19, 0.055, 0.98) if primary else Color(0.075, 0.092, 0.115, 0.96)
	var border := Color(1.0, 0.36, 0.12, 0.85) if primary else Color(0.37, 0.44, 0.52, 0.36)
	b.add_theme_stylebox_override("normal", _panel_style(normal_color, border, 8))
	b.add_theme_stylebox_override("hover", _panel_style(Color(0.91, 0.26, 0.075, 1.0) if primary else Color(0.12, 0.15, 0.19, 1.0), Color(1.0, 0.49, 0.22, 0.9), 8))
	b.add_theme_stylebox_override("pressed", _panel_style(Color(0.58, 0.13, 0.04, 1.0) if primary else Color(0.045, 0.055, 0.07, 1.0), border, 8))
	b.add_theme_stylebox_override("focus", _panel_style(normal_color, Color(1.0, 0.68, 0.3, 1.0), 8))
	parent.add_child(b)
	return b

func _label(node_name: String, text_value: String, size: int, color: Color) -> Label:
	var l := Label.new()
	l.name = node_name
	l.text = text_value
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	return l

func _panel_style(bg: Color, border: Color, radius: int) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = border
	style.set_border_width_all(1)
	style.set_corner_radius_all(radius)
	style.content_margin_left = 12.0
	style.content_margin_right = 12.0
	style.content_margin_top = 7.0
	style.content_margin_bottom = 7.0
	return style

func _style_option(control: Control) -> void:
	control.add_theme_stylebox_override("normal", _panel_style(Color(0.07, 0.09, 0.12), Color(0.32, 0.42, 0.53, 0.52), 7))
	control.add_theme_stylebox_override("hover", _panel_style(Color(0.09, 0.12, 0.16), Color(1.0, 0.34, 0.12, 0.8), 7))

func _on_quality(index: int) -> void:
	Settings.quality = ["low", "medium", "high"][index]
	Settings.save_settings()

func _play() -> void:
	Sfx.play_at(self, 440.0, 0.07)
	get_tree().change_scene_to_file("res://scenes/arena/arena_mvp.tscn")

func _cycle_vehicle() -> void:
	_garage_index = MODEL_ORDER.find(Settings.vehicle_skin)
	if _garage_index < 0:
		_garage_index = 0
	_garage_index = (_garage_index + 1) % MODEL_ORDER.size()
	Settings.vehicle_skin = MODEL_ORDER[_garage_index]
	Settings.save_settings()
	_refresh_preview()
	_garage_changed_at = 0.5
	Sfx.play_at(self, 540.0 + 45.0 * _garage_index, 0.06)

func _toggle_settings() -> void:
	_panel.visible = not _panel.visible
	Sfx.play_at(self, 350.0 if _panel.visible else 260.0, 0.045)

func _mute() -> void:
	Settings.master_muted = not Settings.master_muted
	Settings.save_settings()
	_refresh_mute()

func _refresh_mute() -> void:
	if _mute_button != null:
		_mute_button.text = "◖  SESİ AÇ" if Settings.master_muted else "◖  SES AÇIK"

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_GO_BACK_REQUEST:
		if _panel != null and _panel.visible:
			_panel.visible = false
		else:
			get_tree().quit()
