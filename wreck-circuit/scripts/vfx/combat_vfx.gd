extends RefCounted

const FIRE: PackedScene = preload("res://assets/models/vfx/fire.glb")
const SMOKE: PackedScene = preload("res://assets/models/vfx/smoke.glb")
const DEBRIS: PackedScene = preload("res://assets/models/vfx/debris.glb")
const SPARK: PackedScene = preload("res://assets/models/vfx/spark.glb")

static func spawn_death(parent: Node, at: Vector3) -> void:
	if parent == null or Settings.particle_amount() <= 0:
		return

	var fire := FIRE.instantiate() as Node3D
	if fire != null:
		parent.add_child(fire)
		fire.global_position = at + Vector3(0.0, 0.48, 0.0)
		fire.scale = Vector3.ONE * 0.5
		var blast := fire.create_tween()
		blast.tween_property(fire, "scale", Vector3.ONE * 2.15, 0.16).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		blast.tween_interval(0.06)
		blast.tween_property(fire, "scale", Vector3.ONE * 0.18, 0.32)
		blast.tween_callback(fire.queue_free)

	if Settings.quality == "high":
		var smoke := SMOKE.instantiate() as Node3D
		if smoke != null:
			parent.add_child(smoke)
			smoke.global_position = at + Vector3(0.0, 0.65, 0.0)
			smoke.scale = Vector3.ONE * 0.4
			var smog := smoke.create_tween()
			smog.tween_property(smoke, "scale", Vector3.ONE * 1.35, 0.55).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
			smog.tween_interval(0.2)
			smog.tween_property(smoke, "scale", Vector3.ONE * 0.15, 0.55)
			smog.tween_callback(smoke.queue_free)

	var fragment_count := 5 if Settings.quality == "high" else 3
	for i in fragment_count:
		var fragment := DEBRIS.instantiate() as Node3D
		if fragment == null:
			continue
		parent.add_child(fragment)
		var start := at + Vector3(0.0, 0.42, 0.0)
		fragment.global_position = start
		fragment.scale = Vector3.ONE * randf_range(0.8, 1.35)
		fragment.rotation = Vector3(randf_range(-PI, PI), randf_range(-PI, PI), randf_range(-PI, PI))
		var direction := Vector3(randf_range(-1.0, 1.0), randf_range(0.25, 1.0), randf_range(-1.0, 1.0)).normalized()
		var finish := start + direction * randf_range(1.0, 2.8)
		var toss := fragment.create_tween()
		toss.tween_property(fragment, "global_position", finish, 0.28).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		toss.parallel().tween_property(fragment, "scale", Vector3.ONE * 0.01, 0.43)
		toss.tween_callback(fragment.queue_free)

static func spawn_collision(parent: Node, at: Vector3, intensity: float) -> void:
	if parent == null or Settings.particle_amount() <= 0 or intensity < 9.0:
		return
	var count := 3 if Settings.quality == "high" else 1
	for i in count:
		var spark := SPARK.instantiate() as Node3D
		if spark == null:
			continue
		parent.add_child(spark)
		spark.global_position = at + Vector3(randf_range(-0.35, 0.35), randf_range(0.05, 0.55), randf_range(-0.35, 0.35))
		spark.rotation = Vector3(randf_range(-PI, PI), randf_range(-PI, PI), randf_range(-PI, PI))
		spark.scale = Vector3.ONE * randf_range(1.2, 2.1)
		var flash := spark.create_tween()
		flash.tween_property(spark, "scale", Vector3.ONE * 0.05, 0.13)
		flash.tween_callback(spark.queue_free)
