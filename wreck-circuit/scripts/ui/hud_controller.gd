extends Control

var _flash: float = 0.0
var _pickup_time: float = 0.0
var _player_hp_bar: ProgressBar
var _enemy_hp_bar: ProgressBar
var _player_hp_text: Label
var _player_armor_text: Label
var _enemy_text: Label
var _timer_text: Label
var _speed_text: Label
var _boost_text: Label
var _weapon_text: Label
var _pickup_text: Label
var _flash_rect: ColorRect

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_hud()
	var player := get_tree().get_first_node_in_group("player")
	if player != null and player.has_node("Health"):
		player.get_node("Health").damaged.connect(func(_amount: float, _source: Node): _flash = 0.24)
		if player.has_signal("pickup_received"):
			player.pickup_received.connect(_on_pickup_received)

func _build_hud() -> void:
	var player_panel := _card("PlayerPanel", 0.022, 0.025, 0.265, 0.155, Color(1.0, 0.30, 0.085))
	var player_box := VBoxContainer.new()
	player_box.add_theme_constant_override("separation", 3)
	player_panel.add_child(player_box)
	var player_row := HBoxContainer.new()
	_player_hp_text = _label("PlayerHp", "SAĞLIK  /  100", 14, Color(0.97, 0.98, 1.0))
	player_row.add_child(_player_hp_text)
	player_row.add_spacer(false)
	_player_armor_text = _label("Armor", "ZIRH  00", 11, Color(0.38, 0.82, 1.0))
	player_row.add_child(_player_armor_text)
	player_box.add_child(player_row)
	_player_hp_bar = _bar("PlayerHpBar", Color(1.0, 0.29, 0.075))
	player_box.add_child(_player_hp_bar)

	var enemy_panel := _card("EnemyPanel", 0.735, 0.025, 0.978, 0.155, Color(0.24, 0.72, 0.92))
	var enemy_box := VBoxContainer.new()
	enemy_box.add_theme_constant_override("separation", 3)
	enemy_panel.add_child(enemy_box)
	_enemy_text = _label("EnemyHp", "DÜŞMANLAR  /  03", 14, Color(0.96, 0.98, 1.0))
	enemy_box.add_child(_enemy_text)
	_enemy_hp_bar = _bar("EnemyHpBar", Color(0.24, 0.72, 0.92))
	enemy_box.add_child(_enemy_hp_bar)

	var timer_panel := _card("TimerPanel", 0.431, 0.025, 0.569, 0.108, Color(1.0, 0.54, 0.16))
	_timer_text = _label("Timer", "03:00", 24, Color(1.0, 0.92, 0.78))
	_timer_text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	timer_panel.add_child(_timer_text)

	var pause := Button.new()
	pause.name = "PauseButton"
	pause.text = "Ⅱ"
	pause.anchor_left = 0.94
	pause.anchor_top = 0.17
	pause.anchor_right = 0.978
	pause.anchor_bottom = 0.255
	pause.add_theme_font_size_override("font_size", 22)
	pause.add_theme_color_override("font_color", Color(0.98, 0.98, 0.98))
	pause.add_theme_stylebox_override("normal", _style(Color(0.035, 0.05, 0.067, 0.86), Color(0.45, 0.54, 0.64, 0.5), 9))
	pause.add_theme_stylebox_override("hover", _style(Color(0.18, 0.22, 0.28, 0.95), Color(1.0, 0.32, 0.09, 0.85), 9))
	add_child(pause)
	pause.pressed.connect(_on_pause)

	var info_panel := _card("VehicleInfo", 0.395, 0.865, 0.605, 0.967, Color(1.0, 0.43, 0.11))
	var info_box := VBoxContainer.new()
	info_box.add_theme_constant_override("separation", 0)
	info_panel.add_child(info_box)
	_speed_text = _label("Speed", "000 KM/SA", 24, Color(1.0, 0.95, 0.86))
	_speed_text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	info_box.add_child(_speed_text)
	_boost_text = _label("Boost", "NİTRO  HAZIR", 11, Color(1.0, 0.55, 0.20))
	_boost_text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	info_box.add_child(_boost_text)
	_weapon_text = _label("Weapon", "V8 TOPU  /  HAZIR", 10, Color(0.65, 0.74, 0.84))
	_weapon_text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	info_box.add_child(_weapon_text)

	_pickup_text = _label("PickupToast", "", 16, Color(0.92, 1.0, 0.88))
	_pickup_text.anchor_left = 0.32
	_pickup_text.anchor_top = 0.17
	_pickup_text.anchor_right = 0.68
	_pickup_text.anchor_bottom = 0.235
	_pickup_text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_pickup_text.visible = false
	_pickup_text.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.8))
	_pickup_text.add_theme_constant_override("shadow_offset_x", 2)
	_pickup_text.add_theme_constant_override("shadow_offset_y", 2)
	add_child(_pickup_text)

	_flash_rect = ColorRect.new()
	_flash_rect.name = "DamageFlash"
	_flash_rect.color = Color(0.7, 0.035, 0.015, 0.0)
	_flash_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_flash_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_flash_rect.z_index = 8
	add_child(_flash_rect)

func _card(node_name: String, left: float, top: float, right: float, bottom: float, accent: Color) -> PanelContainer:
	var p := PanelContainer.new()
	p.name = node_name
	p.anchor_left = left
	p.anchor_top = top
	p.anchor_right = right
	p.anchor_bottom = bottom
	p.add_theme_stylebox_override("panel", _style(Color(0.025, 0.038, 0.052, 0.82), Color(accent.r, accent.g, accent.b, 0.62), 9))
	add_child(p)
	return p

func _bar(node_name: String, accent: Color) -> ProgressBar:
	var p := ProgressBar.new()
	p.name = node_name
	p.custom_minimum_size = Vector2(0, 9)
	p.min_value = 0
	p.max_value = 100
	p.value = 100
	p.show_percentage = false
	p.add_theme_stylebox_override("background", _style(Color(0.09, 0.12, 0.15, 1), Color(0.15, 0.19, 0.23, 1), 4))
	p.add_theme_stylebox_override("fill", _style(accent, accent, 4))
	return p

func _label(node_name: String, value: String, size: int, color: Color) -> Label:
	var l := Label.new()
	l.name = node_name
	l.text = value
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	return l

func _style(bg: Color, border: Color, radius: int) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = bg
	s.border_color = border
	s.set_border_width_all(1)
	s.set_corner_radius_all(radius)
	s.content_margin_left = 10.0
	s.content_margin_right = 10.0
	s.content_margin_top = 6.0
	s.content_margin_bottom = 6.0
	return s

func _on_pause() -> void:
	var round := get_tree().get_first_node_in_group("match")
	if round != null and round.has_method("toggle_user_pause"):
		round.toggle_user_pause()

func _on_pickup_received(kind: StringName) -> void:
	var messages := {
		"repair": "+38  /  GÖVDE ONARILDI",
		"health": "+38  /  GÖVDE ONARILDI",
		"armor": "+32  /  ZIRH GÜÇLENDİ",
		"nitro": "NİTRO  /  YENİLENDİ",
		"weapon": "SİLAH AŞIRI YÜK  /  10 SANİYE",
		"ammo": "GÜÇLÜ MERMİ  /  +12",
	}
	_pickup_text.text = str(messages.get(String(kind), "GÜÇLENDİRİCİ ALINDI"))
	_pickup_time = 2.0
	_pickup_text.visible = true

func _process(delta: float) -> void:
	var player := get_tree().get_first_node_in_group("player") as RigidBody3D
	var match_node := get_tree().get_first_node_in_group("match")
	if player != null and player.has_node("Health"):
		var hp := player.get_node("Health") as VehicleHealth
		_player_hp_text.text = "SAĞLIK  /  %03d" % int(hp.health)
		_player_hp_bar.value = hp.health / maxf(hp.max_health, 1.0) * 100.0
		_player_armor_text.text = "ZIRH  %02d" % int(hp.armor)
		if hp.spawn_protection_left > 0.0:
			_boost_text.text = "BAŞLANGIÇ KALKANI  %.1f sn" % hp.spawn_protection_left
			_boost_text.add_theme_color_override("font_color", Color(0.32, 0.82, 1.0))
		else:
			var boost_fill: float = player.boost_fill() if player.has_method("boost_fill") else 1.0
			_boost_text.text = "NİTRO  %03d%%" % int(boost_fill * 100.0)
			_boost_text.add_theme_color_override("font_color", Color(1.0, 0.55, 0.20))
		var speed := Vector2(player.linear_velocity.x, player.linear_velocity.z).length() * 3.6
		_speed_text.text = "%03d KM/SA" % int(speed)
		var turret := player.get_node_or_null("Turret")
		if turret != null:
			if turret.overdrive_left > 0.0:
				_weapon_text.text = "SİLAH AŞIRI YÜK  /  %.1f sn" % turret.overdrive_left
			elif turret.bonus_shots > 0:
				_weapon_text.text = "V8 TOPU  /  MERMİ %02d" % turret.bonus_shots
			else:
				_weapon_text.text = "V8 TOPU  /  HAZIR"

	var enemies := get_tree().get_nodes_in_group("enemy")
	var alive := 0
	var total_health := 0.0
	var max_total := 0.0
	for enemy in enemies:
		if enemy.has_node("Health"):
			var ehp := enemy.get_node("Health") as VehicleHealth
			total_health += ehp.health
			max_total += ehp.max_health
			if not ehp.dead:
				alive += 1
	_enemy_text.text = "DÜŞMANLAR  /  %02d KALDI" % alive
	_enemy_hp_bar.value = total_health / maxf(max_total, 1.0) * 100.0
	if match_node != null and "time_left" in match_node:
		var t := maxi(int(ceil(match_node.time_left)), 0)
		_timer_text.text = "%02d:%02d" % [int(t / 60.0), t % 60]
	if _flash > 0.0:
		_flash = maxf(0.0, _flash - delta)
		var tint := _flash_rect.color
		tint.a = (_flash / 0.24) * 0.17
		_flash_rect.color = tint
	else:
		var clear_tint := _flash_rect.color
		clear_tint.a = 0.0
		_flash_rect.color = clear_tint
	if _pickup_time > 0.0:
		_pickup_time = maxf(0.0, _pickup_time - delta)
		_pickup_text.modulate.a = minf(1.0, _pickup_time * 2.0)
		if _pickup_time <= 0.0:
			_pickup_text.visible = false
