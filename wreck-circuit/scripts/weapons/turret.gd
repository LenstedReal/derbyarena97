extends Node3D

const DEFAULT_STATS := preload("res://resources/basic_cannon.tres")

@export var stats: WeaponStats
@export var use_auto_aim: bool = true
@export var shot_layer: int = 8
@export var shot_mask: int = 5

var trigger: bool = false
var extra_yaw: float = 0.0
var heat: float = 0.0
var _flash: float = 0.0
var overdrive_left: float = 0.0
var bonus_shots: int = 0

@onready var _muzzle: Marker3D = $Muzzle
@onready var _flash_light: OmniLight3D = $Flash
@onready var _flash_mesh: MeshInstance3D = $FlashMesh
@onready var _muzzle_fx: Node3D = $MuzzleModel


func _ready() -> void:
	if stats == null:
		stats = DEFAULT_STATS
	_muzzle_fx.visible = false


func add_aim(pixels: float) -> void:
	extra_yaw = clampf(extra_yaw - pixels * 0.004 * Settings.camera_sensitivity, -1.15, 1.15)


func cooldown_ratio() -> float:
	if stats == null or stats.cooldown <= 0.0:
		return 0.0
	return clampf(heat / stats.cooldown, 0.0, 1.0)


func _physics_process(delta: float) -> void:
	_aim(delta)
	overdrive_left = maxf(0.0, overdrive_left - delta)
	if heat > 0.0:
		heat = maxf(0.0, heat - delta)
	_flash = maxf(0.0, _flash - delta)
	var show_fx := _flash > 0.0 and Settings.particle_amount() > 0
	_flash_light.visible = show_fx
	_flash_mesh.visible = show_fx
	_muzzle_fx.visible = show_fx
	if trigger and heat <= 0.0 and _vehicle_alive():
		_shoot()
		heat = stats.cooldown * (0.64 if overdrive_left > 0.0 else 1.0)
		_flash = 0.085
		Sfx.play_at(self, 320.0, 0.06)


func _vehicle_alive() -> bool:
	var body := get_parent()
	if body == null:
		return false
	var health := body.get_node_or_null("Health") as VehicleHealth
	return health == null or not health.dead


func _target() -> Node3D:
	var body := get_parent()
	if body == null:
		return null
	if body.is_in_group("player"):
		var nearest: Node3D = null
		var nearest_dist := INF
		for candidate in get_tree().get_nodes_in_group("enemy"):
			if not candidate is Node3D:
				continue
			var health := candidate.get_node_or_null("Health") as VehicleHealth
			if health != null and health.dead:
				continue
			var d := global_position.distance_squared_to(candidate.global_position)
			if d < nearest_dist:
				nearest_dist = d
				nearest = candidate as Node3D
		return nearest
	return get_tree().get_first_node_in_group("player") as Node3D


func _aim(delta: float) -> void:
	var parent := get_parent() as Node3D
	var aim_on := use_auto_aim and (Settings.auto_aim or not parent.is_in_group("player"))
	var target_yaw := extra_yaw
	if aim_on:
		var target := _target()
		if target != null:
			var to := target.global_position - global_position
			to.y = 0.0
			if to.length() > 0.4:
				var local_dir: Vector3 = parent.global_transform.basis.inverse() * to
				target_yaw = atan2(-local_dir.x, -local_dir.z) + extra_yaw
	# Smooth local yaw is easier to read than a turret snapping instantly to a
	# target every physics frame, while remaining responsive for assisted aim.
	rotation.x = 0.0
	rotation.z = 0.0
	rotation.y = lerp_angle(rotation.y, target_yaw, clampf(delta * 9.0, 0.0, 1.0))


func _shoot() -> void:
	var pool := get_tree().get_first_node_in_group("projectile_pool")
	if pool == null or not pool.has_method("spawn"):
		return
	var origin := _muzzle.global_transform if _muzzle != null else global_transform
	var dir := -global_transform.basis.z
	var damage_mul := 1.45 if overdrive_left > 0.0 else 1.0
	var speed_mul := 1.12 if overdrive_left > 0.0 else 1.0
	if bonus_shots > 0:
		bonus_shots -= 1
		damage_mul *= 1.22
	pool.spawn(origin, dir, stats.damage * damage_mul, stats.speed * speed_mul, get_parent(), stats.range_m, shot_layer, shot_mask)
