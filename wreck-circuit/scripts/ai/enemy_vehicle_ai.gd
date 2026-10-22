extends Node

var state: String = "APPROACH"
var recover: float = 0.0
var think: float = 0.0
var aim_err: float = 0.0
var stuck_pos: Vector3 = Vector3.ZERO
var stuck_t: float = 0.0

@onready var body: RigidBody3D = get_parent()

func _physics_process(delta: float) -> void:
	var hp := body.get_node_or_null("Health") as VehicleHealth
	if hp != null and hp.dead:
		body.set_drive_input({"throttle": 0.0, "brake": 1.0, "steer": 0.0, "boost": false, "handbrake": false, "fire": false})
		return
	var player := get_tree().get_first_node_in_group("player") as Node3D
	if player == null:
		return
	var to := player.global_position - body.global_position
	to.y = 0.0
	var dist := to.length()
	if body.global_position.distance_to(stuck_pos) < 0.45 and state != "RECOVER":
		stuck_t += delta
	else:
		stuck_t = 0.0
		stuck_pos = body.global_position
	if stuck_t > 1.05:
		state = "RECOVER"
		recover = 0.6
		stuck_t = 0.0
	match state:
		"APPROACH":
			if dist < 14.0:
				state = "ATTACK"
		"ATTACK":
			if dist > 24.0:
				state = "APPROACH"
			elif dist < 4.2:
				state = "EVADE"
		"EVADE":
			if dist > 11.0:
				state = "APPROACH"
		"RECOVER":
			recover -= delta
			if recover <= 0.0:
				state = "APPROACH"
	var throttle := 0.0
	var brake := 0.0
	var steer := 0.0
	var desired := to.normalized() if dist > 0.2 else Vector3(0, 0, -1)
	if state == "RECOVER":
		brake = 1.0
		steer = 0.9
	elif state == "EVADE":
		desired = -desired
		throttle = 1.0
	else:
		throttle = 1.0 if dist > 8.0 else 0.4
	var local := body.global_transform.basis.inverse() * desired
	if state != "RECOVER":
		steer = clampf(local.x * 2.3, -1.0, 1.0)
	think -= delta
	var turret := body.get_node_or_null("Turret")
	if think <= 0.0:
		think = randf_range(0.25, 0.55)
		aim_err = randf_range(-0.28, 0.28)
		if turret:
			turret.extra_yaw = aim_err
	var fire := state == "ATTACK" and dist < 22.0 and absf(local.x) < 0.5
	body.set_drive_input({
		"throttle": throttle,
		"brake": brake,
		"steer": steer,
		"boost": state == "APPROACH" and dist > 16.0,
		"handbrake": false,
		"fire": fire,
	})
