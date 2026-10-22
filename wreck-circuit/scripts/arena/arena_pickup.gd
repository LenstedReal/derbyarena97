extends Area3D

@export var pickup_type: StringName = &"repair"
@export var model_scene: PackedScene
@export var respawn_seconds: float = 10.0

var _respawn_left: float = 0.0
var _visual_pivot: Node3D
var _ring: MeshInstance3D
var _phase: float = 0.0

const PICKUP_COLORS := {
	"repair": Color(0.22, 1.0, 0.47),
	"health": Color(0.20, 0.92, 0.36),
	"armor": Color(0.20, 0.82, 1.0),
	"nitro": Color(1.0, 0.58, 0.12),
	"weapon": Color(1.0, 0.22, 0.12),
	"ammo": Color(1.0, 0.80, 0.18),
}

func _ready() -> void:
	add_to_group("arena_pickups")
	collision_layer = 0
	collision_mask = 6 # Player and enemy vehicle layers.
	monitoring = true
	monitorable = false
	body_entered.connect(_on_body_entered)
	_phase = randf() * TAU

	var collision := CollisionShape3D.new()
	var sphere := SphereShape3D.new()
	sphere.radius = 0.85
	collision.shape = sphere
	collision.position.y = 0.6
	add_child(collision)

	_visual_pivot = Node3D.new()
	_visual_pivot.name = "VisualPivot"
	_visual_pivot.position.y = 0.24
	add_child(_visual_pivot)
	if model_scene != null:
		var model := model_scene.instantiate() as Node3D
		if model != null:
			model.name = "PickupModel"
			model.scale = Vector3.ONE * 0.31
			_visual_pivot.add_child(model)

	_ring = MeshInstance3D.new()
	_ring.name = "PickupGlowRing"
	var mesh := TorusMesh.new()
	mesh.inner_radius = 0.60
	mesh.outer_radius = 0.72
	mesh.rings = 32
	mesh.ring_segments = 8
	_ring.mesh = mesh
	_ring.position.y = -0.17
	var mat := StandardMaterial3D.new()
	var glow: Color = PICKUP_COLORS.get(String(pickup_type), Color(1.0, 0.55, 0.1))
	mat.albedo_color = glow
	mat.emission_enabled = true
	mat.emission = glow
	mat.emission_energy_multiplier = 1.6
	mat.roughness = 0.28
	_ring.material_override = mat
	add_child(_ring)

func _process(delta: float) -> void:
	if not visible:
		_respawn_left -= delta
		if _respawn_left <= 0.0:
			visible = true
			set_deferred("monitoring", true)
		return
	if _visual_pivot != null:
		_visual_pivot.rotation.y += delta * 0.85
		_visual_pivot.position.y = 0.24 + sin(Time.get_ticks_msec() * 0.0023 + _phase) * 0.07
	if _ring != null:
		_ring.rotation.y -= delta * 0.32

func _on_body_entered(body: Node3D) -> void:
	if not body.is_in_group("vehicle") or not body.has_method("apply_pickup"):
		return
	if bool(body.call("apply_pickup", pickup_type)):
		visible = false
		set_deferred("monitoring", false)
		_respawn_left = respawn_seconds
		Sfx.play_at(self, 680.0 if pickup_type != &"weapon" else 920.0, 0.1)
