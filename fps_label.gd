extends Label

func _process(delta):
	text = "FPS: %s" % str(int(Engine.get_frames_per_second()))
