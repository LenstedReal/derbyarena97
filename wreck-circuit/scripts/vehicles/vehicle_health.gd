class_name VehicleHealth
extends Node

const CombatVFX := preload("res://scripts/vfx/combat_vfx.gd")

signal damaged(amount: float, source: Node)
signal died

@export var max_health: float = 100.0
@export var max_armor: float = 60.0
var health: float = 100.0
var armor: float = 0.0
var dead: bool = false
var spawn_protection_left: float = 0.0
var last_hit_frame: int = -999
var last_source_id: int = 0

func _ready() -> void:
	health = max_health
	armor = 0.0
	var vehicle := get_parent()
	if vehicle != null and vehicle.is_in_group("player"):
		spawn_protection_left = 2.4

func _process(delta: float) -> void:
	if spawn_protection_left > 0.0:
		spawn_protection_left = maxf(0.0, spawn_protection_left - delta)

func apply_damage(amount: float, source: Node = null) -> void:
	if dead or amount <= 0.0 or spawn_protection_left > 0.0:
		return
	var frame := Engine.get_physics_frames()
	var sid := source.get_instance_id() if source != null else 0
	if frame == last_hit_frame and sid == last_source_id:
		return
	last_hit_frame = frame
	last_source_id = sid

	# Armor is a finite buffer, not a second health bar that makes the player immortal.
	var absorbed := minf(armor, amount * 0.68)
	armor = maxf(0.0, armor - absorbed)
	var health_damage := maxf(0.0, amount - absorbed)
	health = maxf(0.0, health - health_damage)
	damaged.emit(amount, source)
	if health <= 0.0:
		dead = true
		var vehicle := get_parent() as Node3D
		if vehicle != null:
			var scene_root := get_tree().current_scene
			if scene_root != null:
				CombatVFX.spawn_death(scene_root, vehicle.global_position)
			Sfx.play_at(vehicle, 62.0, 0.24)
		died.emit()
