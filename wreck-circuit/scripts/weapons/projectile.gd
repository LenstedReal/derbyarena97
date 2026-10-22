extends Area3D

const IMPACT_SPARK: PackedScene = preload("res://assets/models/vfx/spark.glb")

var velocity: Vector3 = Vector3.ZERO
var damage: float = 14.0
var life: float = 0.0
var shooter: Node = null
var active: bool = false
var traveled: float = 0.0
var max_range: float = 30.0

func _ready() -> void:
	body_entered.connect(_on_body_entered)
	monitoring = false
	monitorable = false
	visible = false

func launch(from: Transform3D, dir: Vector3, dmg: float, spd: float, who: Node, rng: float) -> void:
	global_transform = from
	var flat := dir
	flat.y = 0.0
	if flat.length() < 0.01:
		flat = -from.basis.z
	velocity = flat.normalized() * spd
	damage = dmg
	shooter = who
	max_range = rng
	traveled = 0.0
	life = maxf(0.2, rng / maxf(spd, 1.0))
	active = true
	visible = true
	set_deferred("monitoring", true)

func deactivate() -> void:
	active = false
	visible = false
	set_deferred("monitoring", false)
	velocity = Vector3.ZERO
	shooter = null

func _physics_process(delta: float) -> void:
	if not active:
		return

	var step := velocity * delta
	var from_pos := global_position
	var to_pos := from_pos + step
	# The projectile travels faster than a car; ray casting across each physics
	# step prevents thin collision shapes from being skipped on mobile hardware.
	var exclude: Array[RID] = []
	if shooter is CollisionObject3D:
		exclude.append((shooter as CollisionObject3D).get_rid())
	var query := PhysicsRayQueryParameters3D.create(from_pos, to_pos, collision_mask, exclude)
	query.collide_with_areas = false
	query.collide_with_bodies = true
	var hit := get_world_3d().direct_space_state.intersect_ray(query)
	if not hit.is_empty():
		var collider := hit.get("collider") as Node
		var hit_pos: Vector3 = hit.get("position", to_pos)
		var hit_normal: Vector3 = hit.get("normal", Vector3.UP)
		_impact(collider, hit_pos, hit_normal)
		return

	global_position = to_pos
	traveled += step.length()
	life -= delta
	if life <= 0.0 or traveled >= max_range:
		deactivate()

func _on_body_entered(body: Node3D) -> void:
	# Fallback for contacts that the overlap monitor detects between ray steps.
	if not active or body == shooter:
		return
	_impact(body, global_position, Vector3.UP)

func _impact(body: Node, at: Vector3, normal: Vector3) -> void:
	if not active:
		return
	if body != null and body != shooter:
		var health := body.get_node_or_null("Health") as VehicleHealth
		if health != null:
			health.apply_damage(damage, shooter)
			Sfx.play_at(self, 180.0, 0.05)
	_spawn_impact_spark(at, normal)
	deactivate()

func _spawn_impact_spark(at: Vector3, normal: Vector3) -> void:
	if Settings.particle_amount() <= 0:
		return
	var parent := get_tree().current_scene
	if parent == null:
		parent = get_tree().root
	var fx := IMPACT_SPARK.instantiate() as Node3D
	if fx == null:
		return
	parent.add_child(fx)
	fx.global_position = at + normal * 0.025
	fx.scale = Vector3.ONE * (1.8 if Settings.quality == "high" else 1.25)
	var tween := fx.create_tween()
	tween.tween_property(fx, "scale", Vector3.ONE * 0.08, 0.16)
	tween.tween_callback(fx.queue_free)
