﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSMovement : MonoBehaviour
{
	// FAKE SINGLETON
	public static FPSMovement playerMovement;

	// Tag Handling (Replace with LayerMasks)
	const string DUCK_BUTTON = "Duck";
	const string SPRINT_BUTTON = "Sprint";
	//[SerializeField] string GROUND_TAG = "null";
	//[SerializeField] string WATER_TAG = "null";

	[Header("Movement")]
	CharacterController charCon = null;
	public CharacterStats _speed = null;
	public CharacterStats _jumpForce = null;
	public CharacterStats _gravity = null;
	public CharacterFlags _flags = null;
	public Vector3 _velocity = Vector3.zero;
	[SerializeField] float _crawlSpeedFactor = 0.5f;
	[SerializeField] float _duckDistance = 0.4f;
	[SerializeField] float _slidingSpeedFactor = 0.5f;
	//Vector3 _slopeDirection;
	[SerializeField] float _groundRayExtraDist = 3f;
	[SerializeField] float _allowedJumpDistance = 0.2f;
	float _groundRayDistance = 0;
	//[SerializeField] float _minSlidingAngle = 25f;
	[SerializeField] float _slopeWalkCorrection = 2f;
	[SerializeField] float _strafingSpeedFactor = 0.8f;
	[SerializeField] float _sprintSpeedFactor = 2f;
	[SerializeField] LayerMask layerMask = 0;
	Vector3 _cameraStartPosition = Vector3.zero;
	bool _inAir = false;

	[Header("Bobbing")]
	[SerializeField] float _bobbingAmount = 0.05f;
	[SerializeField] float _bobbingSpeed = 1f;
	float _bobTimer = 0;
	float _defPosY = 0;

	Transform _playerCam = null;

	// SFX Variables
	Player_Emitter _emitPlayerSound = null;
	Vector3 _prevPos = Vector3.zero;
	float _randWalk = 0;
	float _timeSinceLastStep = 0;
	float _travelledDist = 0;
	[SerializeField] private float _travelDist = 0;

	// Swimming Variables
	[SerializeField] float _waterRayDist = 1f;
	[SerializeField] float _waterForceMod = 8f;
	[SerializeField] float _swimMaxSpeed = 3f;
	[SerializeField] float _swimStartSpeedFactor = 0.5f;
	[SerializeField] float _swimAccelerationTime = 1f;
	[SerializeField] float _underwaterFloatBack = 3f;
	[SerializeField] float _swimBobAmount = 0.05f;
	[SerializeField] float _swimBobSpeed = 1.0f;
	[SerializeField] float _swimDeceleration = 1.0f;

	Vector2 _swimVelocity = new Vector2(0f, 0f);
	[SerializeField] LayerMask _waterLayer;
	Collider _lastWaterChunk;
	bool _isUnderwater = false;
	float _savedMoveModifier = 1.0f;
	
	[SerializeField] float _levitationSpeed = 1f;

	// !OBS Weird bug causing script to disable itself when awake is used.
	void Awake()
	{
		playerMovement = this;
		_swimVelocity = new Vector2(0f, 0f);
	}

	void Start()
	{
		_prevPos = transform.position;
		_randWalk = Random.Range(0.8f, 1.2f);
		_emitPlayerSound = GetComponent<Player_Emitter>();

		charCon = GetComponent<CharacterController>();
		_playerCam = transform.Find("PlayerCam");
		_cameraStartPosition = _playerCam.localPosition;
		_defPosY = _cameraStartPosition.y;
		CharacterState.SetControlState(CHARACTER_CONTROL_STATE.PLAYERCONTROLLED);
	}

	void FixedUpdate()
	{

	}

	void Update()
	{
		if (CharacterState.Control_State == CHARACTER_CONTROL_STATE.PLAYERCONTROLLED || CharacterState.Control_State == CHARACTER_CONTROL_STATE.MENU)
		{
			// == Variables ==
			//Input
			Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
			if (moveInput.magnitude > 1)
			{
				moveInput.Normalize();
			}

			Vector3 jump = new Vector3(0, 1f * _jumpForce.Value, 0);
			float moveModifier = 1.0f;

			//Ground Detection
			//float terrainAngle;
			RaycastHit groundDetection;
			bool grounded = GroundRay(transform.position, Vector3.down, charCon.bounds.size.y / 2 + _groundRayExtraDist, out groundDetection);

			RaycastHit waterDetection;
			bool inWater = Physics.Raycast(_playerCam.position + 0.3f * Vector3.up, Vector3.down, out waterDetection, _waterRayDist, _waterLayer);
			bool isStoned = CharacterState.IsAbilityFlagActive("STONE");
			bool isLevitating = CharacterState.IsAbilityFlagActive("LEVITATE");
			float gravityFactor = 1.0f;

			// == Functions ==
			if (charCon.isGrounded)
			{
				_inAir = false;
			}

			if(inWater)
			{
				_isUnderwater = false;
				_lastWaterChunk = waterDetection.collider;
			}
			if(isStoned)
			{
				_isUnderwater = false;
			}
			if (_isUnderwater && !isStoned && _lastWaterChunk.transform.position.y > _playerCam.position.y)
			{
				_velocity = Vector3.up*_underwaterFloatBack*Time.deltaTime;
				charCon.Move(_velocity);
				_inAir = true;
			}
			else if (inWater && !isStoned)
			{
				_inAir = true;
				Swimming(moveInput);
			}
			// Everything that can be done while grounded
			else if (grounded)
			{
				if (Input.GetButton(SPRINT_BUTTON))
				{
					moveModifier *= _sprintSpeedFactor;
				}
				// Jump, otherwise Slide, otherwise Walk
				if (Input.GetButtonDown("Jump") && groundDetection.distance <= charCon.bounds.size.y / 2 + _allowedJumpDistance && !_inAir && !Input.GetButton(DUCK_BUTTON))
				{
					Debug.Log("JUMP!");
					_velocity.y = 0;
					Launch(jump);
					_inAir = true;
					_savedMoveModifier = moveModifier;
				}
				//else if (Input.GetButton(DUCK_BUTTON) && terrainAngle > 10f)
				//{
				//	Debug.Log("SLIDING!");
				//	Sliding(x, slopeDirection);
				//}
				else
				{
					if (Input.GetButtonDown(DUCK_BUTTON))
					{
						Ducking(-_duckDistance);
					}
					else if (Input.GetButton(DUCK_BUTTON))
					{
						moveModifier *= _crawlSpeedFactor;
					}
					else if (Input.GetButtonUp(DUCK_BUTTON))
					{
						Ducking(0);
					}

					Walking(moveInput.x, moveInput.y, groundDetection, moveModifier);
				}
			}
			else
			{
				//_swimVelocity = Vector2.zero;
				Strafing(moveInput.x, moveInput.y);
			}

			if (!inWater && !isStoned && _lastWaterChunk != null && _lastWaterChunk.transform.position.y > transform.position.y)
			{
				_isUnderwater = true;
			}

			//Gravity
			if ((!inWater && !isLevitating) || isStoned )
			{
				Gravity(gravityFactor);
			}
			else if(Time.time%1 < 0.1)
			{
				Debug.Log("Gravity is disabled");
			}

			if(isLevitating)
			{
				charCon.Move(_levitationSpeed *Vector3.up*Time.deltaTime);
				_inAir = true;
				_velocity.y = -5f;
			}
			// Bobbing
			HeadBob(moveInput.x * _speed.Value, moveInput.y * _speed.Value);

		}
	}

	void Strafing(float horizontal, float vertical)
	{
		Vector3 lookDir = _playerCam.forward;
		lookDir.y = 0;
		Vector3 move =
			_playerCam.right.normalized * horizontal +
			lookDir.normalized * vertical;
		charCon.Move(move * _speed.Value * _strafingSpeedFactor * _savedMoveModifier * Time.deltaTime);
	}

	void Walking(float horizontal, float vertical, RaycastHit ground, float modifier)
	{
		//Vector2 velocity = new Vector2(horizontal, vertical).normalized;

		Vector3 lookDir = _playerCam.forward;
		lookDir.y = 0;
		Vector3 move =
			_playerCam.right.normalized * horizontal +
			lookDir.normalized * vertical;
		charCon.Move(move.normalized * _speed.Value * modifier * Time.deltaTime);

		//Post move distance to ground check
		/*if (ground.distance <= _slopeWalkCorrection && !_inAir)
		{
			charCon.Move(Vector3.down * ground.distance);
		}*/
	}

	void Ducking(float duckDirection)
	{
		Vector3 ducking = new Vector3(0, duckDirection, 0);
		_playerCam.localPosition = _cameraStartPosition + ducking;
		_defPosY = _cameraStartPosition.y + duckDirection;
	}

	void Sliding(float z, Vector3 slopeDirection)
	{
		Vector3 lookDir = _playerCam.forward;
		lookDir.y = 0;
		Vector3 move = new Vector3(slopeDirection.x, 0f, slopeDirection.z);
		Vector3 strafe = Vector3.Cross(move, Vector3.up);   //Normalize Directin
		Debug.Log("Sliding");
		move += strafe * -z;
		//Debug.Log(move * _speed * _slidingSpeedFactor * Time.deltaTime);
		charCon.Move(move * _speed.Value * _slidingSpeedFactor * Time.deltaTime);
	}

	void Gravity(float factor)
	{
		if (CharacterState.IsAbilityFlagActive(CharacterState.GetFlagFromString("SLOWFALL")))
		{
			if (charCon.isGrounded)
			{
				CharacterState.RemoveAbilityFlag("SLOWFALL");
			}
			else
			{
				factor *= 0.35f;
			}
		}
		charCon.Move(_velocity * Time.deltaTime);
		if (!charCon.isGrounded)
		{
			_velocity.y += _gravity.Value * factor * Time.deltaTime;
		}
		else
		{
			_velocity.y = _gravity.Value;
		}
	}

	void Swimming(Vector2 inputs)
	{
		Vector2 swimming = inputs * _swimAccelerationTime * _swimMaxSpeed;
		_swimVelocity += swimming;

		if (_swimVelocity.magnitude < _swimStartSpeedFactor * _swimMaxSpeed && inputs.magnitude == 1 && _swimVelocity.magnitude != 0)
		{
			float ratio = (_swimStartSpeedFactor * _swimMaxSpeed) / _swimVelocity.magnitude;
			_swimVelocity *= ratio;
		}
		else if (_swimVelocity.magnitude > _swimMaxSpeed && _swimVelocity.magnitude != 0)
		{
			float ratio = _swimMaxSpeed / _swimVelocity.magnitude;
			_swimVelocity *= ratio;
		}

		Vector3 readyX = _playerCam.right;
		Vector3 readyZ = _playerCam.forward;
		readyX.y = 0f;
		readyZ.y = 0f;
		Vector3 readySwimVelocity = readyX.normalized * _swimVelocity.x + readyZ.normalized * _swimVelocity.y;
		charCon.Move(readySwimVelocity * Time.deltaTime);

		SwimBob(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		if (inputs.magnitude < 0.1f)
		{
			_swimVelocity -= (_swimVelocity / _swimDeceleration) * Time.deltaTime;
		}

		_swimVelocity -= _swimVelocity * Time.deltaTime;
	}

	void Launch(Vector3 launchVector)
	{
		_velocity += launchVector;
	}

	//Head Bobbing !Stolen from the internet
	void HeadBob(float x, float z)
	{
		_timeSinceLastStep += Time.deltaTime;
		_travelledDist += (transform.position - _prevPos).magnitude;

		if (Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f)
		{
			//Player is moving
			FootstepsSound();
			_bobTimer += Time.deltaTime * _bobbingSpeed;
			_playerCam.localPosition = new Vector3(_playerCam.localPosition.x,
				_defPosY + Mathf.Sin(_bobTimer) * _bobbingAmount, _playerCam.localPosition.z);
		}
		else
		{
			//Idle
			_bobTimer = 0;
			_playerCam.localPosition = new Vector3(_playerCam.localPosition.x,
				Mathf.Lerp(_playerCam.localPosition.y, _defPosY, Time.deltaTime * _bobbingSpeed), _playerCam.localPosition.z);
		}
	}
	void SwimBob(float x, float z)
	{

		if (Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f)
		{
			//Player is moving
			_bobTimer += Time.deltaTime * _swimBobSpeed;
			_playerCam.localPosition = new Vector3(_playerCam.localPosition.x,
				_defPosY + Mathf.Sin(_bobTimer) * _swimBobAmount, _playerCam.localPosition.z);
		}
		else
		{
			//Idle
			_bobTimer = 0;
			_playerCam.localPosition = new Vector3(_playerCam.localPosition.x,
				Mathf.Lerp(_playerCam.localPosition.y, _defPosY, Time.deltaTime * _swimBobSpeed), _playerCam.localPosition.z);
		}
	}

	//Ground Detection !Stolen from the internet
	bool GroundRay(Vector3 rayStart, Vector3 rayDirection, float rayDistance, out RaycastHit hit)
	{
		//Ray groundRay = new Ray(rayStart, rayDirection);
		bool onHit = Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, layerMask);
		if (onHit)
		{
			//if (hit.transform.name == "Terrain Chunk")
			//{
			//    Renderer rend = hit.transform.GetComponent<Renderer>();
			//    Texture2D tex = rend.material.GetTexture("_NoiseTextures") as Texture2D;
			//    CharacterState.PositionType = tex.GetPixel((int)hit.textureCoord.x * tex.width, (int)hit.textureCoord.y * tex.height).r;
			//    //Debug.Log(tex.GetPixel((int)hit.textureCoord.x * tex.width, (int)hit.textureCoord.y * tex.height));
			//}
			//Debug.Log("Bee");
			return true;
		}

		return false;
		/*
        if (Physics.Raycast(groundRay, out hit, rayDistance))
        {
            if (GROUND_TAG == "null" || hit.collider.gameObject.tag == GROUND_TAG)
            {
                //_slopeDirection = hit.normal;
                //Debug.DrawRay(transform.position, slopeDirection, Color.magenta, 1f);
                return true;
            }
        }
        return false;*/
	}

	void FootstepsSound()
	{
		if (!_inAir && _travelledDist >= _travelDist + _randWalk)
		{
			_emitPlayerSound.Init_Footsteps(0);
			_travelledDist = 0f;
		}
		_prevPos = transform.position;
	}
}
