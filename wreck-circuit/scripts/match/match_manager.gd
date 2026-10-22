extends Node3D

var time_left: float = 180.0
var finished: bool = false
var user_paused: bool = false
var system_paused: bool = false

func _enter_tree() -> void:
	add_to_group("match")

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	var sun := get_node_or_null("Sun") as DirectionalLight3D
	if sun:
		sun.rotation_degrees = Vector3(-48, 32, 0)
		sun.shadow_enabled = Settings.shadows_enabled()
	for group_name in ["player", "enemy"]:
		var n := get_tree().get_first_node_in_group(group_name)
		if n and n.has_node("Health"):
			n.get_node("Health").died.connect(_on_death)
	get_viewport().gui_release_focus()

func _process(delta: float) -> void:
	if Input.is_action_just_pressed("pause"):
		toggle_user_pause()
	if finished or get_tree().paused:
		return
	time_left -= delta
	if time_left <= 0.0:
		time_left = 0.0
		_timeout()

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_GO_BACK_REQUEST:
		_back()
	elif what == NOTIFICATION_APPLICATION_PAUSED:
		AudioServer.set_bus_mute(0, true)
		if not get_tree().paused:
			system_paused = true
			get_tree().paused = true
	elif what == NOTIFICATION_APPLICATION_RESUMED:
		Settings.apply()
		if system_paused and not user_paused and not finished:
			get_tree().paused = false
		system_paused = false

func toggle_user_pause() -> void:
	if finished:
		return
	user_paused = not user_paused
	get_tree().paused = user_paused
	var menu := $UI/SafeFrame/Stack/PauseMenu
	if user_paused:
		menu.open()
	else:
		menu.close()

func _back() -> void:
	if finished:
		get_tree().paused = false
		get_tree().change_scene_to_file("res://scenes/main_menu.tscn")
		return
	if not user_paused:
		toggle_user_pause()
	else:
		toggle_user_pause()

func _on_death() -> void:
	if finished:
		return
	var pdead := _dead("player")
	var edead := _dead("enemy")
	if pdead:
		_finish("defeat")
	elif edead:
		_finish("victory")

func _timeout() -> void:
	var ph := _hp("player")
	var eh := _hp("enemy")
	if ph > eh:
		_finish("victory")
	elif eh > ph:
		_finish("defeat")
	else:
		_finish("draw")

func _finish(kind: String) -> void:
	if finished:
		return
	finished = true
	user_paused = false
	$UI/SafeFrame/Stack/PauseMenu.close()
	$UI/SafeFrame/Stack/ResultPanel.show_result(kind)
	get_tree().paused = true
	Sfx.play_at(self, 220.0 if kind == "victory" else 110.0, 0.18)

func _dead(group_name: String) -> bool:
	var n := get_tree().get_first_node_in_group(group_name)
	if n and n.has_node("Health"):
		return bool(n.get_node("Health").dead)
	return false

func _hp(group_name: String) -> float:
	var n := get_tree().get_first_node_in_group(group_name)
	if n and n.has_node("Health"):
		return float(n.get_node("Health").health)
	return 0.0
