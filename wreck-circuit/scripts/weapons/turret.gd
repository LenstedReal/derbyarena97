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

@onready var _muzzle: Marker3D = $Muzzle
@onready var _flash_light: OmniLight3D = $Flash
@onready var _flash_mesh: MeshInstance3D = $FlashMesh


func _ready() -> void:
	if stats == null:
		stats = DEFAULT_STATS


func add_aim(pixels: float) -> void:
	extra_yaw = clampf(extra_yaw - pixels * 0.004 * Settings.camera_sensitivity, -1.15, 1.15)


func cooldown_ratio() -> float:
	if stats == null or stats.cooldown <= 0.0:
		return 0.0
	return clampf(heat / stats.cooldown, 0.0, 1.0)


func _physics_process(delta: float) -> void:
	_aim(delta)
	if heat > 0.0:
		heat = maxf(0.0, heat - delta)
	_flash = maxf(0.0, _flash - delta)
	var show_fx := _flash > 0.0 and Settings.particle_amount() > 0
	_flash_light.visible = show_fx
	_flash_mesh.visible = show_fx
	if trigger and heat <= 0.0 and _vehicle_alive():
		_shoot()
		heat = stats.cooldown
		_flash = 0.08
		Sfx.play_at(self, 320.0, 0.06)


func _vehicle_alive() -> bool:
	var body := get_parent()
	if body == null:
		return false
	var health := body.get_node_or_null("Health") as VehicleHealth
	return health == null or not health.dead


func _target() -> Node3D:
	var body := get_parent()
	if body != null and body.is_in_group("player"):
		return get_tree().get_first_node_in_group("enemy") as Node3D
	return get_tree().get_first_node_in_group("player") as Node3D


func _aim(delta: float) -> void:
	var aim_on := use_auto_aim and (Settings.auto_aim or not get_parent().is_in_group("player"))
	if aim_on:
		var target := _target()
		if target != null:
			var to := target.global_position - global_position
			to.y = 0.0
			if to.length() > 0.4:
				look_at(global_position + to.normalized(), Vector3.UP)
				rotate_object_local(Vector3.UP, extra_yaw)
				return
	rotation = Vector3(0.0, extra_yaw, 0.0)


func _shoot() -> void:
	var pool := get_tree().get_first_node_in_group("projectile_pool")
	if pool == null or not pool.has_method("spawn"):
		return
	var origin := _muzzle.global_transform if _muzzle != null else global_transform
	var dir := -global_transform.basis.z
	pool.spawn(origin, dir, stats.damage, stats.speed, get_parent(), stats.range_m, shot_layer, shot_mask)
