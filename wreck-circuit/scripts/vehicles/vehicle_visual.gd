extends RefCounted

const PLAYER_MODEL: PackedScene = preload("res://assets/models/vehicles/player_muscle.glb")
const ENEMY_A: PackedScene = preload("res://assets/models/vehicles/enemy_raider_a.glb")
const ENEMY_B: PackedScene = preload("res://assets/models/vehicles/enemy_raider_b.glb")
const ENEMY_C: PackedScene = preload("res://assets/models/vehicles/enemy_raider_c.glb")
const BOSS_MODEL: PackedScene = preload("res://assets/models/vehicles/enemy_boss_warrig.glb")
const BOOST_FLAME: PackedScene = preload("res://assets/models/vfx/fire.glb")

static func build(root: Node3D, paint: Color) -> void:
	if root.get_child_count() > 0:
		return

	# Import the authored GLB car models. The player garage selection persists in
	# Settings; AI cars choose from distinct raider bodies for an actual derby field.
	var parent := root.get_parent()
	var scene: PackedScene = PLAYER_MODEL
	var model_scale := 0.46
	if parent != null and parent.is_in_group("enemy"):
		var choice := randi() % 4
		match choice:
			0: scene = ENEMY_A
			1: scene = ENEMY_B
			2: scene = ENEMY_C
			_:
				scene = BOSS_MODEL
				model_scale = 0.34
	else:
		match Settings.vehicle_skin:
			"raider_a": scene = ENEMY_A
			"raider_b": scene = ENEMY_B
			"raider_c": scene = ENEMY_C
			"boss":
				scene = BOSS_MODEL
				model_scale = 0.34
			_: scene = PLAYER_MODEL

	var instance := scene.instantiate() as Node3D
	if instance != null:
		instance.name = "ImportedVehicleModel"
		instance.scale = Vector3.ONE * model_scale
		instance.position = Vector3(0, 0.08, 0)
		_hide_static_weapon_meshes(instance)
		root.add_child(instance)
		_add_lighting_accents(root)
		_add_boost_fx(root)
		return

	_build_fallback(root, paint)
	_add_boost_fx(root)


static func _hide_static_weapon_meshes(root: Node) -> void:
	# Vehicle GLBs include a static turret silhouette. Hide just those meshes so
	# the separate gameplay turret is the single rotating, firing weapon.
	for node in root.find_children("*", "MeshInstance3D", true, false):
		var key := str(node.name).to_lower()
		if "turret" in key or "barrel" in key:
			(node as MeshInstance3D).visible = false

static func _add_boost_fx(root: Node3D) -> void:
	var exhausts := Node3D.new()
	exhausts.name = "BoostFX"
	exhausts.visible = false
	root.add_child(exhausts)
	for x in [-0.39, 0.39]:
		var flame := BOOST_FLAME.instantiate() as Node3D
		if flame == null:
			continue
		flame.name = "ExhaustFlame"
		flame.position = Vector3(x, 0.23, 1.17)
		flame.scale = Vector3.ONE * 0.36
		exhausts.add_child(flame)

static func collect_wheels(root: Node) -> Array[Node3D]:
	var wheels: Array[Node3D] = []
	_collect_wheel_pivots(root, wheels)
	return wheels

static func _collect_wheel_pivots(node: Node, wheels: Array[Node3D]) -> void:
	for child in node.get_children():
		if child is Node3D:
			var key := str(child.name).to_lower()
			# Imported GLBs suffix wheel nodes with stable unique IDs, for example
			# Wheel_RR_93ba52; exact-name checks missed every authored wheel.
			if key.begins_with("wheel_") or key in ["wheelfl", "wheelfr", "wheelrl", "wheelrr"]:
				wheels.append(child as Node3D)
			else:
				_collect_wheel_pivots(child, wheels)
		else:
			_collect_wheel_pivots(child, wheels)

static func _add_lighting_accents(root: Node3D) -> void:
	var lamps := Node3D.new()
	lamps.name = "VehicleAccents"
	root.add_child(lamps)
	for x in [-0.42, 0.42]:
		var lamp := MeshInstance3D.new()
		lamp.name = "Headlamp"
		var mesh := BoxMesh.new()
		mesh.size = Vector3(0.18, 0.07, 0.05)
		lamp.mesh = mesh
		lamp.position = Vector3(x, 0.34, -1.15)
		var mat := StandardMaterial3D.new()
		mat.albedo_color = Color(1.0, 0.78, 0.36)
		mat.emission_enabled = true
		mat.emission = Color(1.0, 0.48, 0.12)
		mat.emission_energy_multiplier = 1.8
		lamp.material_override = mat
		lamps.add_child(lamp)

static func _build_fallback(root: Node3D, paint: Color) -> void:
	var body_mat := _mat(paint, 0.55, 0.35)
	var dark := _mat(paint.darkened(0.45), 0.4, 0.2)
	var glass := _mat(Color(0.15, 0.2, 0.24), 0.15, 0.05)
	var tire := _mat(Color(0.08, 0.08, 0.09), 0.9, 0.0)
	var lamp := _mat(Color(1.0, 0.82, 0.45), 0.3, 0.1, Color(1.0, 0.75, 0.3), 2.2)
	_box(root, "Body", Vector3(1.55, 0.38, 2.7), Vector3(0, 0.48, 0), body_mat)
	_box(root, "Hood", Vector3(1.35, 0.16, 0.9), Vector3(0, 0.62, -0.85), body_mat)
	_box(root, "Cabin", Vector3(1.05, 0.38, 1.05), Vector3(0, 0.82, 0.15), glass)
	_box(root, "Bumper", Vector3(1.62, 0.22, 0.28), Vector3(0, 0.36, -1.45), dark)
	_box(root, "CageL", Vector3(0.06, 0.42, 1.3), Vector3(-0.48, 1.05, 0.05), dark)
	_box(root, "CageR", Vector3(0.06, 0.42, 1.3), Vector3(0.48, 1.05, 0.05), dark)
	_box(root, "CageTop", Vector3(1.0, 0.06, 1.3), Vector3(0, 1.26, 0.05), dark)
	_box(root, "LampL", Vector3(0.22, 0.1, 0.08), Vector3(-0.42, 0.58, -1.5), lamp)
	_box(root, "LampR", Vector3(0.22, 0.1, 0.08), Vector3(0.42, 0.58, -1.5), lamp)
	for spec in [["WheelFL", Vector3(-0.78, 0.32, -0.95)], ["WheelFR", Vector3(0.78, 0.32, -0.95)], ["WheelRL", Vector3(-0.78, 0.32, 0.95)], ["WheelRR", Vector3(0.78, 0.32, 0.95)]]:
		var cyl := MeshInstance3D.new()
		cyl.name = spec[0]
		var mesh := CylinderMesh.new()
		mesh.top_radius = 0.34
		mesh.bottom_radius = 0.34
		mesh.height = 0.28
		mesh.radial_segments = 10
		cyl.mesh = mesh
		cyl.position = spec[1]
		cyl.rotation_degrees = Vector3(0, 0, 90)
		cyl.material_override = tire
		root.add_child(cyl)

static func _box(root: Node3D, node_name: String, size: Vector3, pos: Vector3, mat: Material) -> void:
	var mi := MeshInstance3D.new()
	mi.name = node_name
	var mesh := BoxMesh.new()
	mesh.size = size
	mi.mesh = mesh
	mi.position = pos
	mi.material_override = mat
	root.add_child(mi)

static func _mat(color: Color, rough: float, metal: float, emission: Color = Color(0, 0, 0), energy: float = 0.0) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	m.metallic = metal
	if energy > 0.0:
		m.emission_enabled = true
		m.emission = emission
		m.emission_energy_multiplier = energy
	return m
