extends Node3D

@export var mouse_speed := 10.0
@export var zoom_speed := 2.0

@export var target_puppet : Node3D

@onready var cam := $Camera3D
const MOUSE_FACTOR := 0.003

func _input(event):
	if event is InputEventMouseMotion:
		var iemm := event as InputEventMouseMotion
		var rel := iemm.relative * mouse_speed * MOUSE_FACTOR * -1
		if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
			rotate(Vector3.UP, rel.x)
			rotate(global_transform.basis.x, rel.y)
		elif Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
			_shake_puppet(rel)
	if event is InputEventMouseButton:
		var iemb := event as InputEventMouseButton
		var amount = iemb.factor * zoom_speed * clamp(log(cam.position.z + 1), 0.1, 1)
		if iemb.button_index == MOUSE_BUTTON_WHEEL_UP:
			cam.position.z -= amount
		elif iemb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			cam.position.z += amount
		cam.position.z = max(cam.position.z, 0.1)
		if iemb.is_released() and iemb.button_index == MOUSE_BUTTON_RIGHT:
			target_puppet.position = Vector3.ZERO

func _shake_puppet(delta : Vector2):
	var accumulate := Vector3()
	accumulate += global_basis.x * -delta.x
	accumulate += global_basis.y * delta.y
	target_puppet.position += accumulate
