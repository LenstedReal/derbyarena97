extends RefCounted

static func build(root: Node3D, paint: Color) -> void:
	if root.get_child_count() > 0:
		return
	var body_mat := _mat(paint, 0.55, 0.35)
	var dark := _mat(paint.darkened(0.45), 0.4, 0.2)
	var glass := _mat(Color(0.15, 0.2, 0.24), 0.15, 0.05)
	var tire := _mat(Color(0.08, 0.08, 0.09), 0.9, 0.0)
	var lamp := _mat(Color(1.0, 0.82, 0.45), 0.3, 0.1, Color(1.0, 0.75, 0.3), 2.2)
	_box(root, "Body", Vector3(1.55, 0.38, 2.7), Vector3(0, 0.48, 0), body_mat)
	_box(root, "Hood", Vector3(1.35, 0.16, 0.9), Vector3(0, 0.62, -0.85), body_mat)
	_box(root, "Cabin", Vector3(1.05, 0.38, 1.05), Vector3(0, 0.82, 0.15), glass)
	_box(root, "Bumper", Vector3(1.62, 0.22, 0.28), Vector3(0, 0.36, -1.45), dark)
	_box(root, "CageL", Vector3(0.06, 0.42, 1.3), Vector3(-0.48, 1.05, 0.05), dark)
	_box(root, "CageR", Vector3(0.06, 0.42, 1.3), Vector3(0.48, 1.05, 0.05), dark)
	_box(root, "CageTop", Vector3(1.0, 0.06, 1.3), Vector3(0, 1.26, 0.05), dark)
	_box(root, "LampL", Vector3(0.22, 0.1, 0.08), Vector3(-0.42, 0.58, -1.5), lamp)
	_box(root, "LampR", Vector3(0.22, 0.1, 0.08), Vector3(0.42, 0.58, -1.5), lamp)
	for spec in [["WheelFL", Vector3(-0.78, 0.32, -0.95)], ["WheelFR", Vector3(0.78, 0.32, -0.95)], ["WheelRL", Vector3(-0.78, 0.32, 0.95)], ["WheelRR", Vector3(0.78, 0.32, 0.95)]]:
		var cyl := MeshInstance3D.new()
		cyl.name = spec[0]
		var mesh := CylinderMesh.new()
		mesh.top_radius = 0.34
		mesh.bottom_radius = 0.34
		mesh.height = 0.28
		mesh.radial_segments = 10
		cyl.mesh = mesh
		cyl.position = spec[1]
		cyl.rotation_degrees = Vector3(0, 0, 90)
		cyl.material_override = tire
		root.add_child(cyl)

static func _box(root: Node3D, node_name: String, size: Vector3, pos: Vector3, mat: Material) -> void:
	var mi := MeshInstance3D.new()
	mi.name = node_name
	var mesh := BoxMesh.new()
	mesh.size = size
	mi.mesh = mesh
	mi.position = pos
	mi.material_override = mat
	root.add_child(mi)

static func _mat(color: Color, rough: float, metal: float, emission: Color = Color(0, 0, 0), energy: float = 0.0) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = rough
	m.metallic = metal
	if energy > 0.0:
		m.emission_enabled = true
		m.emission = emission
		m.emission_energy_multiplier = energy
	return m
