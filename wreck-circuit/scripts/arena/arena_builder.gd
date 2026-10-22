extends Node3D

# Original blockout. Colosseum layout is inspiration only — no imported reference meshes.

func _ready() -> void:
	_body(Vector3(46, 1.0, 46), Vector3(0, -0.5, 0), Color(0.72, 0.58, 0.36), "Sand")
	var ring := 20.5
	for i in 16:
		var a := TAU * float(i) / 16.0
		var p := Vector3(sin(a) * ring, 2.2, cos(a) * ring)
		_body(Vector3(1.3, 4.4, 1.3), p, Color(0.78, 0.66, 0.46), "Pier%d" % i)
		var a2 := a + TAU / 32.0
		var lintel := Vector3(sin(a2) * ring, 4.15, cos(a2) * ring)
		var beam := _body(Vector3(2.4, 0.55, 1.15), lintel, Color(0.7, 0.56, 0.4), "Arch%d" % i)
		beam.rotation.y = a2
	_body(Vector3(8, 3.2, 1.2), Vector3(0, 1.6, -21.5), Color(0.45, 0.28, 0.18), "Gate")
	for spec in [[Vector3(6.5, 0.7, 2.2), Vector3(0, 0.35, 0)], [Vector3(2.2, 0.8, 2.2), Vector3(-7, 0.4, 5)], [Vector3(2.2, 0.8, 2.2), Vector3(7.5, 0.4, -4)]]:
		_body(spec[0], spec[1], Color(0.62, 0.5, 0.36), "Block")
	for b in [[Vector3(-6, 3.2, -18), Color(0.55, 0.1, 0.1)], [Vector3(6, 3.2, -18), Color(0.12, 0.16, 0.28)], [Vector3(-18, 3.2, 0), Color(0.55, 0.1, 0.1)], [Vector3(18, 3.2, 0), Color(0.12, 0.16, 0.28)]]:
		_visual_only(Vector3(0.12, 1.6, 2.4), b[0], b[1])
	if Settings.quality != "low":
		for pos in [Vector3(-16, 6.5, -16), Vector3(16, 6.5, -16), Vector3(-16, 6.5, 16), Vector3(16, 6.5, 16)]:
			var light := OmniLight3D.new()
			light.position = pos
			light.light_color = Color(1.0, 0.72, 0.42)
			light.light_energy = 1.1
			light.omni_range = 16.0
			light.shadow_enabled = false
			add_child(light)

func _body(size: Vector3, pos: Vector3, color: Color, node_name: String) -> StaticBody3D:
	var body := StaticBody3D.new()
	body.name = node_name
	body.collision_layer = 1
	body.collision_mask = 0
	body.position = pos
	var shape := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = size
	shape.shape = box
	body.add_child(shape)
	var mi := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	mi.mesh = mesh
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = 0.86
	mi.material_override = mat
	body.add_child(mi)
	add_child(body)
	return body

func _visual_only(size: Vector3, pos: Vector3, color: Color) -> void:
	var mi := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	mi.mesh = mesh
	mi.position = pos
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mi.material_override = mat
	add_child(mi)
