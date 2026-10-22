extends SceneTree

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var menu_pack := load("res://scenes/main_menu.tscn") as PackedScene
	if menu_pack == null:
		push_error("menu pack missing")
		quit(1)
		return
	var menu := menu_pack.instantiate()
	root.add_child(menu)
	await process_frame
	if menu.find_child("Play", true, false) == null:
		push_error("Play button missing")
		quit(1)
		return
	print("MENU_OK")
	menu.queue_free()
	await process_frame
	var arena_pack := load("res://scenes/arena/arena_mvp.tscn") as PackedScene
	if arena_pack == null:
		push_error("arena pack missing")
		quit(1)
		return
	var arena := arena_pack.instantiate()
	root.add_child(arena)
	await process_frame
	await physics_frame
	var player := get_first_node_in_group("player") as RigidBody3D
	if player == null or get_first_node_in_group("enemy") == null:
		push_error("vehicles missing")
		quit(1)
		return
	if get_first_node_in_group("touch_controls") == null or arena.find_child("PlayerHp", true, false) == null:
		push_error("ui scripts failed")
		quit(1)
		return
	var touch := get_first_node_in_group("touch_controls")
	touch.set_process_input(false)
	var top := 0.0
	for _i in 80:
		player.set_drive_input({"throttle": 1.0, "brake": 0.0, "steer": 0.0, "boost": false, "handbrake": false, "fire": false})
		await physics_frame
		top = maxf(top, Vector2(player.linear_velocity.x, player.linear_velocity.z).length())
	print("TOP_SPEED ", top)
	if top < 4.0:
		push_error("car did not accelerate")
		quit(1)
		return
	print("DRIVE_OK")
	quit(0)
