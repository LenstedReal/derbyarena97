extends Control

var _flash: float = 0.0

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_label("PlayerHp", 0.0, 0.0, 0.34, 0.11, HORIZONTAL_ALIGNMENT_LEFT)
	_label("Timer", 0.34, 0.0, 0.66, 0.11, HORIZONTAL_ALIGNMENT_CENTER)
	_label("EnemyHp", 0.52, 0.0, 0.82, 0.11, HORIZONTAL_ALIGNMENT_RIGHT)
	_label("Boost", 0.0, 0.11, 0.34, 0.18, HORIZONTAL_ALIGNMENT_LEFT)
	_label("Weapon", 0.0, 0.18, 0.34, 0.25, HORIZONTAL_ALIGNMENT_LEFT)
	var flash := ColorRect.new()
	flash.name = "Flash"
	flash.color = Color(0.7, 0.05, 0.02, 0.0)
	flash.mouse_filter = Control.MOUSE_FILTER_IGNORE
	flash.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(flash)
	var pause := Button.new()
	pause.name = "PauseButton"
	pause.text = "DURAKLAT"
	pause.layout_mode = 1
	pause.anchor_left = 0.82
	pause.anchor_top = 0.01
	pause.anchor_right = 0.99
	pause.anchor_bottom = 0.11
	add_child(pause)
	pause.pressed.connect(_on_pause)
	var player := get_tree().get_first_node_in_group("player")
	if player and player.has_node("Health"):
		player.get_node("Health").damaged.connect(func(_a, _s): _flash = 0.35)

func _on_pause() -> void:
	var round := get_tree().get_first_node_in_group("match")
	if round and round.has_method("toggle_user_pause"):
		round.toggle_user_pause()

func _label(node_name: String, a: float, b: float, c: float, d: float, align: HorizontalAlignment) -> void:
	var l := Label.new()
	l.name = node_name
	l.layout_mode = 1
	l.anchor_left = a
	l.anchor_top = b
	l.anchor_right = c
	l.anchor_bottom = d
	l.horizontal_alignment = align
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	l.add_theme_font_size_override("font_size", 22)
	l.add_theme_color_override("font_color", Color(0.96, 0.94, 0.9))
	add_child(l)

func _process(delta: float) -> void:
	var player := get_tree().get_first_node_in_group("player") as RigidBody3D
	var enemy := get_tree().get_first_node_in_group("enemy") as Node3D
	var round := get_tree().get_first_node_in_group("match")
	if player and player.has_node("Health"):
		var hp: VehicleHealth = player.get_node("Health")
		$PlayerHp.text = "CAN  %d" % int(hp.health)
		var spd := Vector2(player.linear_velocity.x, player.linear_velocity.z).length() * 3.6
		$Weapon.text = "HIZ  %d" % int(spd)
		if player.has_method("boost_fill"):
			$Boost.text = "BOOST  %d%%" % int(player.boost_fill() * 100.0)
	if enemy and enemy.has_node("Health"):
		$EnemyHp.text = "RAKİP  %d" % int(enemy.get_node("Health").health)
	if round and "time_left" in round:
		var t := maxi(int(ceil(round.time_left)), 0)
		$Timer.text = "%d:%02d" % [int(t / 60.0), t % 60]
	if _flash > 0.0:
		_flash = maxf(0.0, _flash - delta)
	var tint: Color = $Flash.color
	tint.a = _flash
	$Flash.color = tint
