extends Area3D

var velocity: Vector3 = Vector3.ZERO
var damage: float = 14.0
var life: float = 0.0
var max_life: float = 0.7
var shooter: Node = null
var active: bool = false
var traveled: float = 0.0
var max_range: float = 30.0


func _ready() -> void:
	body_entered.connect(_on_body_entered)
	monitoring = false
	monitorable = false
	visible = false


func launch(from: Transform3D, dir: Vector3, dmg: float, spd: float, who: Node, rng: float) -> void:
	global_transform = from
	var flat := dir
	flat.y = 0.0
	if flat.length() < 0.01:
		flat = -from.basis.z
	velocity = flat.normalized() * spd
	damage = dmg
	shooter = who
	max_range = rng
	traveled = 0.0
	life = maxf(0.2, rng / maxf(spd, 1.0))
	active = true
	visible = true
	monitoring = true


func deactivate() -> void:
	active = false
	visible = false
	monitoring = false
	velocity = Vector3.ZERO
	shooter = null


func _physics_process(delta: float) -> void:
	if not active:
		return
	var step := velocity * delta
	global_position += step
	traveled += step.length()
	life -= delta
	if life <= 0.0 or traveled >= max_range:
		deactivate()


func _on_body_entered(body: Node) -> void:
	if not active or body == shooter:
		return
	if shooter != null and body.is_in_group("vehicle") and body == shooter:
		return
	var health := body.get_node_or_null("Health") as VehicleHealth
	if health != null:
		health.apply_damage(damage, shooter)
		Sfx.play_at(self, 180.0, 0.05)
	deactivate()
