extends Node

@export_range(-1, 2, 1) var ai_style: int = -1 # -1 random, 0 rammer, 1 gunner, 2 skirmisher

var state: String = "APPROACH"
var recover: float = 0.0
var think: float = 0.0
var aim_err: float = 0.0
var stuck_pos: Vector3 = Vector3.ZERO
var stuck_t: float = 0.0
var orbit_sign: float = 1.0

@onready var body: RigidBody3D = get_parent()

func _ready() -> void:
	if ai_style < 0:
		ai_style = randi_range(0, 2)
	orbit_sign = -1.0 if randf() < 0.5 else 1.0
	stuck_pos = body.global_position

func _physics_process(delta: float) -> void:
	var hp := body.get_node_or_null("Health") as VehicleHealth
	if hp != null and hp.dead:
		body.set_drive_input({"throttle": 0.0, "brake": 1.0, "steer": 0.0, "boost": false, "handbrake": false, "fire": false})
		return
	var player := get_tree().get_first_node_in_group("player") as Node3D
	if player == null:
		return

	var to_player := player.global_position - body.global_position
	to_player.y = 0.0
	var dist := to_player.length()
	if body.global_position.distance_to(stuck_pos) < 0.45 and state != "RECOVER":
		stuck_t += delta
	else:
		stuck_t = 0.0
		stuck_pos = body.global_position
	if stuck_t > 1.25:
		state = "RECOVER"
		recover = 0.7
		stuck_t = 0.0

	var desired := to_player.normalized() if dist > 0.2 else Vector3(0, 0, -1)
	var throttle := 0.0
	var brake := 0.0
	var steer := 0.0
	var fire := false
	var boost := false
	var handbrake := false

	if state == "RECOVER":
		recover -= delta
		brake = 1.0
		steer = 0.85 * orbit_sign
		if recover <= 0.0:
			state = "APPROACH"
	else:
		match ai_style:
			0: # Rammer: closes distance and uses short boosts to land heavy hits.
				throttle = 1.0 if dist > 5.0 else 0.68
				boost = dist > 12.0
				fire = dist < 20.0
				if dist < 4.4:
					# Stay aggressive at point-blank range: use a short lateral
					# arc rather than turning 180 degrees away from the opponent.
					desired = desired.rotated(Vector3.UP, orbit_sign * 0.28)
					throttle = 0.82
					handbrake = dist < 2.8
			1: # Gunner: tries to keep a firing lane instead of ramming continuously.
				if dist > 13.5:
					throttle = 0.8
					boost = dist > 22.0
				elif dist < 7.5:
					brake = 1.0
					throttle = 0.0
				else:
					throttle = 0.28
				fire = dist < 27.0
			2: # Skirmisher: circles the player to avoid a static firing-line feel.
				desired = desired.rotated(Vector3.UP, orbit_sign * 0.52)
				throttle = 0.88
				boost = dist > 17.0 and dist < 25.0
				fire = dist < 24.0

	var local_dir := body.global_transform.basis.inverse() * desired
	if state != "RECOVER":
		steer = clampf(local_dir.x * (2.0 if ai_style == 1 else 2.45), -1.0, 1.0)
	think -= delta
	var turret := body.get_node_or_null("Turret")
	if think <= 0.0:
		think = randf_range(0.28, 0.58)
		aim_err = randf_range(-0.14, 0.14) if ai_style == 1 else randf_range(-0.24, 0.24)
		if turret:
			turret.extra_yaw = aim_err

	body.set_drive_input({
		"throttle": throttle,
		"brake": brake,
		"steer": steer,
		"boost": boost,
		"handbrake": handbrake,
		"fire": fire,
	})
