extends SceneTree

func _initialize() -> void:
	call_deferred("_run")

func _fail(message: String) -> void:
	push_error(message)
	quit(1)

func _run() -> void:
	var menu_pack := load("res://scenes/main_menu.tscn") as PackedScene
	if menu_pack == null:
		_fail("main menu scene could not be loaded")
		return
	var menu := menu_pack.instantiate()
	root.add_child(menu)
	await process_frame
	if menu.find_child("Play", true, false) == null or menu.find_child("Garage", true, false) == null:
		_fail("main menu Play/Garage buttons missing")
		return
	print("MENU_OK")
	menu.queue_free()
	await process_frame

	var arena_pack := load("res://scenes/arena/arena_mvp.tscn") as PackedScene
	if arena_pack == null:
		_fail("arena scene could not be loaded")
		return
	var arena := arena_pack.instantiate()
	root.add_child(arena)
	await process_frame
	await physics_frame

	var player := get_first_node_in_group("player") as RigidBody3D
	var enemies := get_nodes_in_group("enemy")
	if player == null or enemies.size() < 4:
		_fail("player and four AI opponents must exist")
		return
	var player_collider := player.get_node_or_null("CollisionShape3D") as CollisionShape3D
	if player_collider == null or player_collider.shape == null:
		_fail("player collision shape was not initialized")
		return
	if get_nodes_in_group("arena_pickups").size() < 5:
		_fail("GLB powerup pickups missing")
		return
	if load("res://assets/models/arena/arena01_colosseum.glb") == null or load("res://assets/models/crowd/crowd_tiers.glb") == null:
		_fail("arena or crowd GLB failed to import")
		return
	print("ASSETS_OK | enemies=", enemies.size(), " | pickups=", get_nodes_in_group("arena_pickups").size())

	var controls := get_first_node_in_group("touch_controls")
	if controls == null or not controls.has_method("uses_custom_controls") or arena.find_child("PlayerHp", true, false) == null:
		_fail("touch controls or HUD scripts failed")
		return
	for required_zone in ["Steer", "Knob", "Fire", "Boost", "Brake", "Hand"]:
		if controls.find_child(required_zone, true, false) == null:
			_fail("mobile control missing: " + required_zone)
			return
	if not player.has_node("Turret") or get_first_node_in_group("projectile_pool") == null:
		_fail("weapon or projectile pool missing")
		return
	var pool := get_first_node_in_group("projectile_pool")
	if pool.get_child_count() < 12:
		_fail("projectile pool capacity is too low")
		return
	var player_hp := player.get_node("Health") as VehicleHealth
	player_hp.health = 60.0
	if not player.apply_pickup(&"repair") or player_hp.health < 97.0:
		_fail("repair pickup did not restore health")
		return
	player_hp.health = player_hp.max_health

	var turret := player.get_node("Turret")
	turret.call("_shoot")
	var active_shots := 0
	for projectile in pool.get_children():
		if bool(projectile.get("active")):
			active_shots += 1
	if active_shots < 1:
		_fail("turret could not spawn a projectile")
		return
	print("CONTROLS_WEAPONS_PICKUPS_OK | active_shots=", active_shots)

	# Regression test: on-screen buttons on desktop must use the same control
	# state as touch input instead of disappearing into the keyboard path.
	controls.set("using_touch", false)
	controls.set("_using_mouse_controls", true)
	controls.call("_sync")
	var control_state: Dictionary = controls.get("state")
	if not bool(controls.call("uses_custom_controls")) or absf(float(control_state.get("throttle", 0.0)) - 0.82) > 0.02:
		_fail("on-screen mouse controls were not routed to the vehicle")
		return
	controls.set("_using_mouse_controls", false)
	controls.call("_sync")

	controls.set_process_input(false)
	var top := 0.0
	for _i in 80:
		player.set_drive_input({"throttle": 1.0, "brake": 0.0, "steer": 0.0, "boost": false, "handbrake": false, "fire": false})
		await physics_frame
		top = maxf(top, Vector2(player.linear_velocity.x, player.linear_velocity.z).length())
	print("TOP_SPEED_TEST ", snappedf(top, 0.1), " m/s")
	if top < 4.0:
		_fail("vehicle did not accelerate as expected")
		return
	print("DRIVE_OK")
	quit(0)
