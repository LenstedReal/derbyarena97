class_name Sfx
extends RefCounted

static func beep(freq: float, dur: float, volume: float = 0.35) -> AudioStreamWAV:
	var rate := 22050
	var count := int(rate * dur)
	var data := PackedByteArray()
	data.resize(count * 2)
	for i in count:
		var env := 1.0 - float(i) / float(maxi(count, 1))
		var sample := sin(TAU * freq * float(i) / float(rate)) * volume * env
		var v := int(clampf(sample, -1.0, 1.0) * 16000.0)
		data[i * 2] = v & 255
		data[i * 2 + 1] = (v >> 8) & 255
	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = rate
	wav.stereo = false
	wav.data = data
	return wav


static func play_at(parent: Node, freq: float, dur: float = 0.08) -> void:
	if parent == null:
		return
	var p := AudioStreamPlayer.new()
	p.stream = beep(freq, dur)
	p.bus = "Master"
	parent.add_child(p)
	p.finished.connect(p.queue_free)
	p.play()
