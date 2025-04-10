using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour {
	InputAction _move_action;
	InputAction _jump_action;
	InputAction _reset_action;
	InputAction _boost_action;
	InputAction _skip_action;

    void Start() {
		_move_action = InputSystem.actions.FindAction("Move");
		_jump_action = InputSystem.actions.FindAction("Jump");
		_reset_action = InputSystem.actions.FindAction("Reset");
		_boost_action = InputSystem.actions.FindAction("Boost");
		_skip_action = InputSystem.actions.FindAction("SkipScene");
    }

	public Vector3 GetInputDir() {
		Vector2 dir = _move_action.ReadValue<Vector2>();
		return new Vector3(dir.x, 0.0f, dir.y);
	}

	public bool IsJumpPressed() {
		return _jump_action.IsPressed();
	}

	public bool IsResetPressed() {
		return _reset_action.IsPressed();
	}

	public bool IsBoostPressed() {
		return _boost_action.IsPressed();
	}

	public bool IsSkipPressed() {
		return _skip_action.IsPressed();
	}
}
