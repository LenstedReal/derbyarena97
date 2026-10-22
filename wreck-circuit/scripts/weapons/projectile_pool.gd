extends Node3D

const SCENE := preload("res://scenes/weapons/projectile.tscn")
const SIZE := 16

var _items: Array[Area3D] = []


func _ready() -> void:
	add_to_group("projectile_pool")
	for i in SIZE:
		var p := SCENE.instantiate() as Area3D
		p.name = "Shot%d" % i
		add_child(p)
		_items.append(p)


func spawn(from: Transform3D, dir: Vector3, dmg: float, spd: float, who: Node, rng: float, layer: int, mask: int) -> void:
	var pick: Area3D = null
	for p in _items:
		if not p.active:
			pick = p
			break
	if pick == null:
		pick = _items[0]
		pick.deactivate()
	pick.collision_layer = layer
	pick.collision_mask = mask
	pick.launch(from, dir, dmg, spd, who, rng)
