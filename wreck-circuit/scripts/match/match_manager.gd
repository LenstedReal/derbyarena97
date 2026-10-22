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
		for vehicle in get_tree().get_nodes_in_group(group_name):
			if vehicle.has_node("Health"):
				var hp := vehicle.get_node("Health") as VehicleHealth
				if not hp.died.is_connected(_on_death):
					hp.died.connect(_on_death)
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
	if _dead("player"):
		_finish("defeat")
	elif _dead("enemy"):
		_finish("victory")

func _timeout() -> void:
	var ph := _hp("player")
	var eh := _hp("enemy")
	if ph > eh + 0.5:
		_finish("victory")
	elif eh > ph + 0.5:
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
	Sfx.play_at(self, 620.0 if kind == "victory" else 150.0, 0.2)

func _dead(group_name: String) -> bool:
	var vehicles := get_tree().get_nodes_in_group(group_name)
	if vehicles.is_empty():
		return false
	if group_name == "enemy":
		for vehicle in vehicles:
			var hp := vehicle.get_node_or_null("Health") as VehicleHealth
			if hp == null or not hp.dead:
				return false
		return true
	var first := vehicles[0]
	var player_hp := first.get_node_or_null("Health") as VehicleHealth
	return player_hp != null and player_hp.dead

func _hp(group_name: String) -> float:
	var vehicles := get_tree().get_nodes_in_group(group_name)
	if vehicles.is_empty():
		return 0.0
	if group_name == "enemy":
		var total := 0.0
		var count := 0
		for vehicle in vehicles:
			var hp := vehicle.get_node_or_null("Health") as VehicleHealth
			if hp != null:
				total += hp.health + hp.armor * 0.35
				count += 1
		return total / float(maxi(count, 1))
	var hp_player := vehicles[0].get_node_or_null("Health") as VehicleHealth
	return hp_player.health + hp_player.armor * 0.35 if hp_player != null else 0.0
