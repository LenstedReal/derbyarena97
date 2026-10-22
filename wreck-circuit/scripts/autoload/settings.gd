extends Node

signal changed

const PATH := "user://settings.cfg"
const VERSION := 1

var master_muted: bool = false
var quality: String = "medium"
var steer_sensitivity: float = 1.0
var camera_sensitivity: float = 1.0
var vibration: bool = true
var auto_aim: bool = true


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_ensure_actions()
	load_settings()
	_lock_landscape()
	apply()


func _ensure_actions() -> void:
	_bind("throttle", KEY_W)
	_bind("brake", KEY_S)
	_bind("steer_left", KEY_A)
	_bind("steer_right", KEY_D)
	_bind("fire", KEY_F)
	_bind("boost", KEY_SPACE)
	_bind("handbrake", KEY_SHIFT)
	_bind("pause", KEY_ESCAPE)


func _bind(action: String, key: Key) -> void:
	if not InputMap.has_action(action):
		InputMap.add_action(action, 0.2)
	for ev in InputMap.action_get_events(action):
		if ev is InputEventKey and ev.physical_keycode == key:
			return
	var press := InputEventKey.new()
	press.physical_keycode = key
	InputMap.action_add_event(action, press)


func _lock_landscape() -> void:
	if DisplayServer.get_name() == "headless":
		return
	DisplayServer.screen_set_orientation(DisplayServer.SCREEN_SENSOR_LANDSCAPE)
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
	DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, true)


func load_settings() -> void:
	var cfg := ConfigFile.new()
	if cfg.load(PATH) != OK:
		return
	if int(cfg.get_value("meta", "version", 0)) != VERSION:
		return
	master_muted = bool(cfg.get_value("audio", "muted", false))
	quality = str(cfg.get_value("graphics", "quality", "medium"))
	if quality not in ["low", "medium", "high"]:
		quality = "medium"
	steer_sensitivity = clampf(float(cfg.get_value("controls", "steer", 1.0)), 0.4, 2.0)
	camera_sensitivity = clampf(float(cfg.get_value("controls", "camera", 1.0)), 0.4, 2.0)
	vibration = bool(cfg.get_value("controls", "vibration", true))
	auto_aim = bool(cfg.get_value("controls", "auto_aim", true))


func save_settings() -> void:
	var cfg := ConfigFile.new()
	cfg.set_value("meta", "version", VERSION)
	cfg.set_value("audio", "muted", master_muted)
	cfg.set_value("graphics", "quality", quality)
	cfg.set_value("controls", "steer", steer_sensitivity)
	cfg.set_value("controls", "camera", camera_sensitivity)
	cfg.set_value("controls", "vibration", vibration)
	cfg.set_value("controls", "auto_aim", auto_aim)
	cfg.save(PATH)
	apply()
	changed.emit()


func apply() -> void:
	_apply_audio()
	var vp := get_viewport()
	if vp == null:
		return
	var scale := 0.85
	var msaa := Viewport.MSAA_DISABLED
	match quality:
		"low":
			scale = 0.7
		"high":
			scale = 1.0
			msaa = Viewport.MSAA_2X
		_:
			scale = 0.85
	vp.scaling_3d_scale = scale
	vp.msaa_3d = msaa


func _apply_audio() -> void:
	if AudioServer.bus_count > 0:
		AudioServer.set_bus_mute(0, master_muted)


func particle_amount() -> int:
	match quality:
		"low":
			return 0
		"high":
			return 16
		_:
			return 8


func shadows_enabled() -> bool:
	return quality != "low"
