class_name VehicleHealth
extends Node

signal damaged(amount: float, source: Node)
signal died

@export var max_health: float = 100.0
var health: float = 100.0
var dead: bool = false
var last_hit_frame: int = -999
var last_source_id: int = 0


func _ready() -> void:
	health = max_health


func apply_damage(amount: float, source: Node = null) -> void:
	if dead or amount <= 0.0:
		return
	var frame := Engine.get_physics_frames()
	var sid := source.get_instance_id() if source != null else 0
	if frame == last_hit_frame and sid == last_source_id:
		return
	last_hit_frame = frame
	last_source_id = sid
	health = maxf(0.0, health - amount)
	damaged.emit(amount, source)
	if health <= 0.0:
		dead = true
		died.emit()
