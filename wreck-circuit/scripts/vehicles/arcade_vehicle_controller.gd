extends RigidBody3D

signal pickup_received(kind: StringName)

const VisualBuilder := preload("res://scripts/vehicles/vehicle_visual.gd")
const CombatVFX := preload("res://scripts/vfx/combat_vfx.gd")

@export var human: bool = false
@export var paint: Color = Color(0.14, 0.16, 0.18)
@export var accel: float = 16.0
@export var brake_accel: float = 22.0
@export var reverse_accel: float = 8.0
@export var max_speed: float = 18.0
@export var reverse_max: float = 6.0
@export var turn_torque: float = 12000.0
@export var grip: float = 7.5
@export var handbrake_grip: float = 2.0
@export var boost_mul: float = 1.45
@export var boost_time: float = 1.3
@export var boost_cooldown: float = 4.0

var drive_throttle: float = 0.0
var drive_brake: float = 0.0
var drive_steer: float = 0.0
var drive_boost: bool = false
var drive_handbrake: bool = false
var drive_fire: bool = false
var external_drive: bool = false
var boost_left: float = 0.0
var boost_cd: float = 0.0
var boosting: bool = false
var shake: float = 0.0
var _spawn: Vector3 = Vector3.ZERO
var _ram_cd: Dictionary = {}
var _wheels: Array[Node3D] = []
var _boost_fx: Node3D
var _boost_pressed_last: bool = false
var _shake_phase: float = 0.0
var _steer_smoothed: float = 0.0

func _ready() -> void:
	_spawn = global_position
	contact_monitor = true
	max_contacts_reported = 4
	continuous_cd = true
	can_sleep = false
	axis_lock_angular_x = true
	axis_lock_angular_z = true
	var mat := PhysicsMaterial.new()
	mat.friction = 0.85
	mat.bounce = 0.02
	physics_material_override = mat
	# More stable arcade handling on touch devices: less yaw oscillation and a
	# slightly narrower body collider aligned to the visible GLB car.
	angular_damp = 4.0
	if not has_node("CollisionShape3D"):
		var shape := BoxShape3D.new()
		shape.size = Vector3(1.22, 0.72, 2.50)
		var col := CollisionShape3D.new()
		col.name = "CollisionShape3D"
		col.shape = shape
		col.position = Vector3(0, 0.40, 0)
		add_child(col)
	if has_node("Visual"):
		VisualBuilder.build($Visual, paint)
		_wheels = VisualBuilder.collect_wheels($Visual)
		_boost_fx = $Visual.get_node_or_null("BoostFX") as Node3D
	if has_node("SpringArm3D"):
		var arm: SpringArm3D = $SpringArm3D
		arm.rotation_degrees = Vector3(-14, 180, 0)
		arm.spring_length = 7.2
		arm.margin = 0.4
		arm.collision_mask = 1
		arm.add_excluded_object(get_rid())
		var cam := arm.get_node_or_null("Camera3D") as Camera3D
		if cam:
			cam.rotation_degrees = Vector3(-8, 180, 0)
			# Enemy scenes also carry a camera subtree; only the player's camera
			# may become current or the last spawned AI would take over the view.
			cam.current = human
	body_entered.connect(_on_body_entered)
	var hp := get_node_or_null("Health")
	if hp and hp.has_signal("damaged"):
		hp.damaged.connect(_on_damaged)

func set_drive_input(state: Dictionary) -> void:
	external_drive = true
	drive_throttle = clampf(float(state.get("throttle", 0.0)), 0.0, 1.0)
	drive_brake = clampf(float(state.get("brake", 0.0)), 0.0, 1.0)
	drive_steer = clampf(float(state.get("steer", 0.0)), -1.0, 1.0)
	drive_boost = bool(state.get("boost", false))
	drive_handbrake = bool(state.get("handbrake", false))
	drive_fire = bool(state.get("fire", false))

func boost_fill() -> float:
	if boost_left > 0.0:
		return clampf(boost_left / boost_time, 0.0, 1.0)
	if boost_cd > 0.0:
		return clampf(1.0 - boost_cd / boost_cooldown, 0.0, 1.0)
	return 1.0

func apply_pickup(kind: StringName) -> bool:
	var hp := get_node_or_null("Health") as VehicleHealth
	var turret := get_node_or_null("Turret")
	match String(kind):
		"repair", "health":
			if hp == null or hp.health >= hp.max_health - 0.5:
				return false
			hp.health = minf(hp.max_health, hp.health + 38.0)
		"armor":
			if hp == null or hp.armor >= hp.max_armor - 0.5:
				return false
			hp.armor = minf(hp.max_armor, hp.armor + 32.0)
		"nitro":
			boost_cd = 0.0
			boost_left = 0.0
		"weapon":
			if turret == null:
				return false
			turret.overdrive_left = maxf(turret.overdrive_left, 10.0)
		"ammo":
			if turret == null:
				return false
			turret.bonus_shots = mini(turret.bonus_shots + 12, 36)
		_:
			return false
	pickup_received.emit(kind)
	return true

func _physics_process(delta: float) -> void:
	var hp := get_node_or_null("Health") as VehicleHealth
	if hp != null and hp.dead:
		if _boost_fx != null:
			_boost_fx.visible = false
		linear_damp = 6.0
		angular_damp = 6.0
		if has_node("Turret"):
			$Turret.trigger = false
		return
	if human and not external_drive:
		_pull_human()
	external_drive = false
	_tick_boost(delta)
	if _boost_fx != null:
		_boost_fx.visible = boosting and Settings.quality != "low"
	_integrate(delta)
	if has_node("Turret"):
		$Turret.trigger = drive_fire
	if global_position.y < -3.0:
		global_position = _spawn
		linear_velocity = Vector3.ZERO
		angular_velocity = Vector3.ZERO

func _pull_human() -> void:
	var touch := get_tree().get_first_node_in_group("touch_controls")
	var touched := false
	var aim := 0.0
	if touch != null and touch.has_method("uses_custom_controls") and touch.uses_custom_controls():
		touched = true
		var state: Dictionary = touch.state
		drive_throttle = float(state.get("throttle", 0.0))
		drive_brake = float(state.get("brake", 0.0))
		drive_steer = float(state.get("steer", 0.0))
		drive_boost = bool(state.get("boost", false))
		drive_handbrake = bool(state.get("handbrake", false))
		drive_fire = bool(state.get("fire", false))
		aim = float(touch.consume_aim())
	if not touched:
		drive_throttle = Input.get_action_strength("throttle")
		drive_brake = Input.get_action_strength("brake")
		drive_steer = clampf((Input.get_action_strength("steer_right") - Input.get_action_strength("steer_left")) * Settings.steer_sensitivity, -1.0, 1.0)
		drive_boost = Input.is_action_pressed("boost")
		drive_handbrake = Input.is_action_pressed("handbrake")
		drive_fire = Input.is_action_pressed("fire")
	if aim != 0.0 and has_node("Turret"):
		$Turret.add_aim(aim)

func _tick_boost(delta: float) -> void:
	if boost_cd > 0.0:
		boost_cd = maxf(0.0, boost_cd - delta)
	# Nitro is activated on button-down and completes its short burst even if
	# the button is released. Holding the button cannot silently waste every
	# cooldown cycle; the player must tap again when it is ready.
	if drive_boost and not _boost_pressed_last and boost_left <= 0.0 and boost_cd <= 0.0:
		boost_left = boost_time
	_boost_pressed_last = drive_boost
	boosting = boost_left > 0.0
	if boost_left > 0.0:
		boost_left = maxf(0.0, boost_left - delta)
		if boost_left <= 0.0:
			boost_cd = boost_cooldown
			boosting = false

func _integrate(delta: float) -> void:
	var forward := -global_transform.basis.z
	forward.y = 0.0
	if forward.length() < 0.001:
		return
	forward = forward.normalized()
	var right := global_transform.basis.x
	right.y = 0.0
	right = right.normalized()
	var flat := Vector3(linear_velocity.x, 0.0, linear_velocity.z)
	var speed := flat.dot(forward)
	var mul := boost_mul if boosting else 1.0
	var force := Vector3.ZERO
	if drive_throttle > 0.0:
		force += forward * drive_throttle * accel * mul * mass
	if drive_brake > 0.0:
		if speed > 0.6:
			force -= forward * drive_brake * brake_accel * mass
		else:
			force -= forward * drive_brake * reverse_accel * mass
	apply_central_force(force)
	var cap := max_speed * mul
	if speed > cap:
		apply_central_force(-forward * (speed - cap) * mass * 5.0)
	if speed < -reverse_max:
		apply_central_force(forward * (-reverse_max - speed) * mass * 5.0)
	var lateral := flat.dot(right)
	var g := handbrake_grip if drive_handbrake else grip
	apply_central_force(-right * lateral * g * mass)
	# Smooth sudden thumb movement and reduce yaw authority at top speed. This
	# keeps touch steering predictable without removing the handbrake drift.
	var steer_rate := 5.2 if drive_handbrake else 4.4
	_steer_smoothed = move_toward(_steer_smoothed, drive_steer, delta * steer_rate)
	var turn_scale := clampf(absf(speed) / 5.0, 0.12, 1.0)
	var speed_stability := lerpf(1.0, 0.62, clampf(absf(speed) / maxf(max_speed, 1.0), 0.0, 1.0))
	# Positive steering input means right. Godot's positive Y rotation turns
	# the car's -Z forward vector toward -X, so invert the torque sign here.
	# Reverse steering is inverted again so the car behaves naturally backwards.
	var reverse_sign := -1.0 if speed < -0.25 else 1.0
	apply_torque(Vector3.UP * -_steer_smoothed * turn_torque * turn_scale * speed_stability * reverse_sign)
	var spin := speed * delta / 0.24
	for w in _wheels:
		# GLB wheel pieces share Wheel_* names and pivots at the wheel centre.
		w.rotate_object_local(Vector3.RIGHT, spin)

func _on_damaged(amount: float, _source: Node) -> void:
	shake = minf(0.28, amount * 0.012)
	if human and Settings.vibration:
		Input.vibrate_handheld(40)
	Sfx.play_at(self, 90.0, 0.07)

func _process(delta: float) -> void:
	if not has_node("SpringArm3D"):
		return
	var arm: SpringArm3D = $SpringArm3D
	var cam := arm.get_node_or_null("Camera3D") as Camera3D
	if cam:
		var vp := get_viewport().get_visible_rect().size
		if vp.y > 1.0:
			var aspect := vp.x / vp.y
			var base_fov := clampf(58.0 + (aspect - 1.777) * 7.0, 52.0, 72.0)
			var target_fov := base_fov + (4.5 if boosting else 0.0)
			cam.fov = lerpf(cam.fov, target_fov, clampf(delta * 6.5, 0.0, 1.0))
	if shake > 0.0:
		shake = maxf(0.0, shake - delta * 1.65)
		_shake_phase += delta * 30.0
		var impulse := shake
		arm.position = Vector3(
			sin(_shake_phase) * 0.30 * impulse,
			1.55 + absf(cos(_shake_phase * 0.72)) * 0.16 * impulse,
			sin(_shake_phase * 0.55) * 0.12 * impulse
		)
	else:
		arm.position = Vector3(0, 1.55, 0)

func _on_body_entered(body: Node) -> void:
	if not body.is_in_group("vehicle") or not (body is RigidBody3D):
		return
	if get_instance_id() > body.get_instance_id():
		return
	var other := body as RigidBody3D
	var own_health := get_node_or_null("Health") as VehicleHealth
	var other_health := other.get_node_or_null("Health") as VehicleHealth
	# A destroyed car remains as a physical wreck, but cannot keep dealing
	# invisible collision damage after the round has effectively ended.
	if (own_health != null and own_health.dead) or (other_health != null and other_health.dead):
		return
	var rel := (linear_velocity - other.linear_velocity).length()
	if rel < 7.0:
		return
	var now := Time.get_ticks_msec()
	if now < int(_ram_cd.get(other.get_instance_id(), 0)):
		return
	_ram_cd[other.get_instance_id()] = now + 500
	var dmg := clampf((rel - 7.0) * 1.7, 6.0, 24.0)
	var scene_root := get_tree().current_scene
	if scene_root != null:
		CombatVFX.spawn_collision(scene_root, (global_position + other.global_position) * 0.5, rel)
	if other_health:
		other_health.apply_damage(dmg, self)
	if own_health:
		own_health.apply_damage(dmg, other)
