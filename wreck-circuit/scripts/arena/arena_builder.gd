extends Node3D

# The imported colosseum is the visual source of truth. Collision is built
# separately so the mobile build stays deterministic and inexpensive.
const ARENA_MODEL: PackedScene = preload("res://assets/models/arena/arena01_colosseum.glb")
const CROWD_TIERS: PackedScene = preload("res://assets/models/crowd/crowd_tiers.glb")
const PICKUP_SCRIPT: Script = preload("res://scripts/arena/arena_pickup.gd")
const PICKUP_MODELS := {
	"repair": preload("res://assets/models/props/loot_repair.glb"),
	"armor": preload("res://assets/models/props/loot_armor.glb"),
	"nitro": preload("res://assets/models/props/loot_nitro.glb"),
	"weapon": preload("res://assets/models/props/loot_weapon.glb"),
	"ammo": preload("res://assets/models/props/loot_ammo.glb"),
	"health": preload("res://assets/models/props/loot_health.glb"),
}

const ARENA_SCALE := 0.65
const ARENA_RADIUS := 19.25

func _ready() -> void:
	_build_visual_arena()
	_build_collision_world()
	_build_pickups()
	_build_rim_lighting()

func _build_visual_arena() -> void:
	var arena := ARENA_MODEL.instantiate() as Node3D
	if arena != null:
		arena.name = "ColosseumGLB"
		arena.scale = Vector3.ONE * ARENA_SCALE
		add_child(arena)
		# The arena GLB contains six merged prop meshes (wrecks, barriers and
		# rubble). Use their authored triangles for static collision instead of
		# guessed invisible boxes that would not line up with the visuals.
		for node in arena.find_children("*", "MeshInstance3D", true, false):
			if str(node.name).begins_with("Props"):
				(node as MeshInstance3D).create_trimesh_collision()

	var crowd := CROWD_TIERS.instantiate() as Node3D
	if crowd != null:
		crowd.name = "CrowdTiersGLB"
		# Crowd geometry was authored in the same unit scale as the colosseum.
		# Matching scales place robot feet on the actual tier seating instead of
		# dropping the audience down into the arena floor.
		crowd.scale = Vector3.ONE * ARENA_SCALE
		crowd.position = Vector3.ZERO
		add_child(crowd)

func _build_collision_world() -> void:
	# Flat invisible collision deck sits exactly under the imported arena's ground.
	_add_static_box("ArenaFloorCollision", Vector3(42.0, 1.0, 42.0), Vector3(0.0, -0.5, 0.0), 0.0)

	# Continuous segmented wall: each collider overlaps its neighbours slightly,
	# preventing fast cars from slipping through seams on 60 Hz mobile physics.
	const SEGMENTS := 40
	for i in SEGMENTS:
		var angle := TAU * float(i) / float(SEGMENTS)
		var pos := Vector3(sin(angle) * ARENA_RADIUS, 0.72, cos(angle) * ARENA_RADIUS)
		_add_static_box("RimCollision_%02d" % i, Vector3(3.35, 1.65, 0.92), pos, angle)

func _build_pickups() -> void:
	# Every pickup has a real GLB, a matching gameplay effect and a timed respawn.
	_spawn_pickup("repair", Vector3(-10.8, 0.0, 5.2))
	_spawn_pickup("armor", Vector3(10.6, 0.0, 5.4))
	_spawn_pickup("nitro", Vector3(-10.7, 0.0, -7.8))
	_spawn_pickup("weapon", Vector3(10.8, 0.0, -7.6))
	_spawn_pickup("ammo", Vector3(0.0, 0.0, 13.8))
	_spawn_pickup("health", Vector3(0.0, 0.0, -14.0))

func _spawn_pickup(kind: String, pos: Vector3) -> void:
	var pickup := Area3D.new()
	pickup.name = "Pickup_%s" % kind.capitalize()
	pickup.set_script(PICKUP_SCRIPT)
	pickup.set("pickup_type", StringName(kind))
	pickup.set("model_scene", PICKUP_MODELS[kind])
	pickup.position = pos
	add_child(pickup)

func _build_rim_lighting() -> void:
	# Subtle warm/cool arena lighting adds readable landmarks without many shadows.
	var colors := [Color(1.0, 0.30, 0.08), Color(0.12, 0.62, 1.0), Color(1.0, 0.60, 0.16), Color(0.16, 0.82, 0.76)]
	for i in colors.size():
		var angle := TAU * float(i) / float(colors.size()) + 0.25
		var light := OmniLight3D.new()
		light.name = "ArenaAccentLight_%d" % i
		light.position = Vector3(sin(angle) * 13.0, 4.0, cos(angle) * 13.0)
		light.light_color = colors[i]
		light.light_energy = 1.25
		light.omni_range = 10.0
		light.shadow_enabled = false
		add_child(light)

	var rim := MeshInstance3D.new()
	rim.name = "InnerCombatRing"
	var ring_mesh := TorusMesh.new()
	ring_mesh.inner_radius = 15.15
	ring_mesh.outer_radius = 15.27
	ring_mesh.rings = 96
	ring_mesh.ring_segments = 8
	rim.mesh = ring_mesh
	rim.position.y = 0.045
	var ring_material := StandardMaterial3D.new()
	ring_material.albedo_color = Color(0.95, 0.26, 0.075, 1.0)
	ring_material.emission_enabled = true
	ring_material.emission = Color(0.95, 0.15, 0.035, 1.0)
	ring_material.emission_energy_multiplier = 1.25
	ring_material.roughness = 0.42
	rim.material_override = ring_material
	add_child(rim)

func _add_static_box(node_name: String, size: Vector3, pos: Vector3, yaw: float) -> void:
	var body := StaticBody3D.new()
	body.name = node_name
	body.collision_layer = 1
	body.collision_mask = 0
	body.position = pos
	body.rotation.y = yaw
	var col := CollisionShape3D.new()
	col.name = "CollisionShape3D"
	var shape := BoxShape3D.new()
	shape.size = size
	col.shape = shape
	body.add_child(col)
	add_child(body)
