extends AnimationPlayer


var last_anim := "ShakeAnim"

func _input(event):
	if event.is_action_pressed("toggle_play"):
		if is_playing():
			last_anim = current_animation
			pause()
		else:
			play(last_anim)
