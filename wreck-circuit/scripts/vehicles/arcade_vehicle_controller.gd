extends RigidBody3D

const VisualBuilder := preload("res://scripts/vehicles/vehicle_visual.gd")

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
	if not has_node("CollisionShape3D"):
		var shape := BoxShape3D.new()
		shape.size = Vector3(1.65, 0.7, 2.45)
		var col := CollisionShape3D.new()
		col.name = "CollisionShape3D"
		col.shape = shape
		col.position = Vector3(0, 0.46, 0)
		add_child(col)
	if has_node("Visual"):
		VisualBuilder.build($Visual, paint)
		for c in $Visual.get_children():
			if str(c.name).begins_with("Wheel"):
				_wheels.append(c)
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
			cam.current = true
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

func _physics_process(delta: float) -> void:
	var hp := get_node_or_null("Health") as VehicleHealth
	if hp != null and hp.dead:
		linear_damp = 6.0
		angular_damp = 6.0
		if has_node("Turret"):
			$Turret.trigger = false
		return
	if human and not external_drive:
		_pull_human()
	external_drive = false
	_tick_boost(delta)
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
	if touch != null and touch.using_touch:
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
	boosting = false
	if drive_boost and boost_left <= 0.0 and boost_cd <= 0.0:
		boost_left = boost_time
	if drive_boost and boost_left > 0.0:
		boost_left -= delta
		boosting = true
		if boost_left <= 0.0:
			boost_cd = boost_cooldown
	elif boost_left > 0.0:
		boost_left -= delta

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
	var turn_scale := clampf(absf(speed) / 4.5, 0.28, 1.0)
	apply_torque(Vector3.UP * drive_steer * turn_torque * turn_scale)
	var spin := speed * delta / 0.34
	for w in _wheels:
		w.rotate_object_local(Vector3.UP, spin)

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
			cam.fov = clampf(58.0 + (aspect - 1.777) * 7.0, 52.0, 72.0)
	if shake > 0.0:
		shake = maxf(0.0, shake - delta)
		arm.position = Vector3(randf_range(-1, 1), 1.55, 0) * shake
	else:
		arm.position = Vector3(0, 1.55, 0)

func _on_body_entered(body: Node) -> void:
	if not body.is_in_group("vehicle"):
		return
	if get_instance_id() > body.get_instance_id():
		return
	if not (body is RigidBody3D):
		return
	var other := body as RigidBody3D
	var rel := (linear_velocity - other.linear_velocity).length()
	if rel < 7.0:
		return
	var now := Time.get_ticks_msec()
	if now < int(_ram_cd.get(other.get_instance_id(), 0)):
		return
	_ram_cd[other.get_instance_id()] = now + 500
	var dmg := clampf((rel - 7.0) * 1.7, 6.0, 24.0)
	var ohp := other.get_node_or_null("Health") as VehicleHealth
	var hp := get_node_or_null("Health") as VehicleHealth
	if ohp:
		ohp.apply_damage(dmg, self)
	if hp:
		hp.apply_damage(dmg * 0.65, other)
