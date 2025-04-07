using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerScript : MonoBehaviour {
	PlayerActions _pa;
	CharacterController _cc;
	GameObject _cam;
	ParticleSystem _particles;

	public enum PLAYER_STATE {
		IDLE,
		RUN,
		JUMP,
		FALL,
		CHARGE,
		BOOST
	} PLAYER_STATE state = PLAYER_STATE.IDLE;

	bool on_ground = true;
	float gravity = 0.35f;

	Vector3 velocity;
	Vector3 horizontal_velocity;

	const float horizontal_max_default = 0.1f;
	float horizontal_max = 0.1f;
	
	Vector3 camera_offset_default = new Vector3 (0.0f, 5.5f, -2.0f);
	Vector3 camera_offset = new Vector3(0.0f, 5.5f, -2.0f);
	
	const float camera_pitch_default = 60.0f;
	const float camera_pitch_min = 60.0f;
	const float camera_pitch_max = 90.0f;

	float camera_pitch = camera_pitch_default;

	Vector3 start_position;

	bool boost_used = false;
	Vector3 boost_dir = Vector3.zero;
	float boost_amount = 0.0f;

	bool SCENE_VIEW = true;
	const ushort scene_perspective_count = 3;
	ushort active_perspective = 0;

	Vector3[] scene_points = {
		new Vector3( -20.0f, 7.5f, 3.5f   ),	
		new Vector3( -18.0f, 105.0f, 3.0f ),	
		new Vector3( 185.0f, 75.0f, 10.0f )
	};

	Vector3[] scene_angles = {
		new Vector3( 14.5f, 110.0f, 0.0f ),	
		new Vector3( 85.0f, 110.0f, 0.0f ),	
		new Vector3( 24.0f, -62.0f, 0.0f )
	};

    void Start() {
		_pa = GetComponent<PlayerActions>();
		_cc = GetComponent<CharacterController>();
		_cam = GameObject.Find("Camera");
		_particles = GetComponent<ParticleSystem>();

		start_position = transform.position;
		_particles.Stop();
    }

	void Update() {
		if(SCENE_VIEW) SceneViewUpdate();
		else PlayerUpdate();
	}

    void PlayerUpdate() {
		// Get movement direction
		Vector3 input_dir = _pa.GetInputDir();
		
		// Add movement direction to player velocity
		horizontal_velocity += (input_dir * Time.deltaTime);
		
		// Prevent player from moving too fast
		horizontal_velocity = Vector3.ClampMagnitude(horizontal_velocity, horizontal_max);
		
		// Slow down if no move keys are held
		if(input_dir.magnitude == 0.0f) horizontal_velocity = Vector3.Lerp(horizontal_velocity, Vector3.zero, 0.05f);
		
		// Apply new horizontal velocity to total velocity
		velocity.x = horizontal_velocity.x;
		velocity.z = horizontal_velocity.z;

		CheckGround();

		if(on_ground) {
			if(_pa.IsJumpPressed()) StartJump(); // Jump

			boost_used = false;
			boost_amount = 0.0f;
		} else {
			// Decrease velocity by gravity
			velocity.y -= gravity * Time.deltaTime;
			
			// Manage boost action
			if(!boost_used) {
				if(_pa.IsBoostPressed()) StartBoost();
			} else {
				if(boost_amount <= 0.001f) {
					boost_amount = 0.0f;
				}
			}
		}

		if(boost_amount > 0.0f) boost_amount = Mathf.Lerp(boost_amount, 0.0f, 0.1f);

		// Update player and camera positions
		_cc.Move(velocity + (boost_dir * boost_amount));
		CameraUpdate();

		switch(state) {
			case PLAYER_STATE.IDLE:
				// Zzzzzzzz...
				// Set state to run if moving
				if(velocity.magnitude > 0) state = PLAYER_STATE.RUN;
				if(!on_ground) {
					if(velocity.y > 0.0f) state = PLAYER_STATE.JUMP;
					else state = PLAYER_STATE.FALL;
				}
				break;
			case PLAYER_STATE.RUN:
				horizontal_max = horizontal_max_default;

				// Set state to idle if not moving
				if(horizontal_velocity.magnitude < 0.01f) state = PLAYER_STATE.IDLE;
				break;
			case PLAYER_STATE.JUMP:
				horizontal_max = horizontal_max_default * 2.25f;

				if(velocity.y <= 0) state = PLAYER_STATE.FALL; 
				break;
			case PLAYER_STATE.FALL:
				horizontal_max = horizontal_max_default * 2.0f;

				if(on_ground) state = PLAYER_STATE.IDLE;
				break;
			case PLAYER_STATE.BOOST:
				if(boost_amount <= 0.0f) state = PLAYER_STATE.IDLE;
				break;
		}

		if(state != PLAYER_STATE.BOOST) _particles.Stop();
		
		// Respawn if player falls out of bounds or requested
		if(transform.position.y < -100.0f || _pa.IsResetPressed()) Reset();
    }

	void CameraUpdate() {
		if(on_ground) {
			camera_pitch -= 50 * Time.deltaTime;	// Pitch camera down
			camera_offset = camera_offset_default;	// Set position offset to default
		} else {
			camera_pitch += 10 * Time.deltaTime;	// Pitch camera up

			// Adjust position offset
			camera_offset.z = -0.5f;
			camera_offset.y = 5.0f;

			if(boost_amount > 0.0f) camera_offset.y += 1.0f;
			
			// Ensure camera can never be below minimum distance from the player
			if(transform.position.y > _cam.transform.position.y - camera_offset.y) {
				_cam.transform.position = new Vector3(
						_cam.transform.position.x,
						transform.position.y + camera_offset.y,
						_cam.transform.position.z
				);
			}
		}
		
		// Move camera smoothly towards new desired position
		Vector3 cam_target_position = (transform.position + (horizontal_velocity * 2.0f)) + camera_offset;
		_cam.transform.position = Vector3.Lerp(_cam.transform.position, cam_target_position, 0.1f);
		
		// Rotate camera smoothly towards new desired rotation
		camera_pitch = Mathf.Clamp(camera_pitch, camera_pitch_min, camera_pitch_max);
		_cam.transform.rotation = Quaternion.Euler(new Vector3(camera_pitch, 0.0f, 0.0f));
	}

	void CheckGround() {
		if(_cc.isGrounded) { 
			on_ground = true;
			if(velocity.y < -0.1f) velocity.y = 0.0f;
		} else on_ground = false;
	}

	void StartJump() {
		on_ground = false;
		velocity.y = 0.8f;
		state = PLAYER_STATE.JUMP;
	}

	void Reset() {
		// Reset all values to default/zero
		transform.position = start_position;
		_cam.transform.position = start_position + camera_offset_default;

		velocity = Vector3.zero;
		on_ground = true;	
	}

	void StartBoost() {
		boost_used = true;

		boost_dir = horizontal_velocity.normalized;
		boost_amount = 3.0f;
		
		_particles.transform.LookAt(transform.position - boost_dir);
		_particles.Play();

		state = PLAYER_STATE.BOOST;
	}

	void SceneViewUpdate() {
		// Calculate right direction using forward 
		Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.up, scene_angles[active_perspective]));

		// Calculate end point, all perspectives move towards the left so scale right by some value and subtract from start point
		Vector3 destination = (scene_points[active_perspective] - (right * 70));
		
		// Adjust camera position and rotation
		_cam.transform.rotation = Quaternion.Euler(scene_angles[active_perspective]);
		_cam.transform.position = Vector3.Lerp(_cam.transform.position, destination, 0.01f);
		
		// If the camera's position is close enough to the destination,
		// either move on to next perspective or start the game
		if(Vector3.Distance(_cam.transform.position, destination) <= 1.0f) {
			if(active_perspective < scene_perspective_count - 1) {
				// Go to next perspective
				active_perspective++;	
				_cam.transform.position = scene_points[active_perspective];
			} else {
				// Turn off scene view and set camera to start position
				SCENE_VIEW = false;
				_cam.transform.position = start_position + camera_offset_default; 
			}
		}
	}
}

    if(hit_found) {
