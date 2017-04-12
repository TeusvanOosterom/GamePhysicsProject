using System;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
	/* 
	 * 
	 * WASD to move around on the horizontal plane.
	 * Mouse to change direction of the camere.
	 * Scroll the mousewheel to change the speed.
	 * Space to gain height.
	 * Shift to go down.
	 * 
	 */

	private const float MAX_Y_ANGLE = 85f;
	private bool freeMouse = false;

	public float MainSpeed = 4; // Movement speed (can be changed by scrolling)
	public float MaxSpeed = 300; // Maximum movement speed
	public float MinSpeed = 1; // Minimum movement speed
	public float ScrollSensitivity = 40; // Sensitivity of scrolling
	public float LookSensitivity = 90; // Sensitivity of the mouse when looking around

	void Start()
	{
		Cursor.visible = false;
	}

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Q))
		{
			freeMouse = !freeMouse;
			Cursor.visible = !Cursor.visible;
		}

		if(freeMouse)
		{
			return;
		}

		// Looking around
		float mouseX = Input.GetAxis("Mouse X");
		float mouseY = -Input.GetAxis("Mouse Y");

		// Stop it from going over <MAX_Y_ANGLE> degrees up or down
		float rotationY = mouseY * LookSensitivity * Time.deltaTime;
		float currentY = transform.localRotation.eulerAngles.x;
		if (rotationY > 0 && currentY < 180 && currentY + rotationY > MAX_Y_ANGLE)
		{
			rotationY = MAX_Y_ANGLE - currentY;
		}
		else if (rotationY < 0 && currentY >= 180 && currentY + rotationY < 360 - MAX_Y_ANGLE)
		{
			rotationY = -MAX_Y_ANGLE - currentY;
		}

		// Note that in Unity the X and Y are swapped
		transform.Rotate(rotationY, mouseX * LookSensitivity * Time.deltaTime, transform.up.z);

		// Keep the Y axis pointing up
		transform.LookAt(transform.position + transform.forward, Vector3.up);

		// Scroll the mousewheel to change movement speed
		MainSpeed += Input.GetAxis("Mouse ScrollWheel") * ScrollSensitivity;
		MainSpeed = Mathf.Clamp(MainSpeed, MinSpeed, MaxSpeed);

		// Get the basic movement directions
		Vector3 p = GetBaseInput();

		// X and Z should be aligned with current looking direction
		p = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * p;

		// Multipliers
		p *= MainSpeed * Time.deltaTime;

		// Apply movement
		transform.Translate(p, Space.World);

		//Correct the position so that the camera stays within certain boundaries
		/*if (transform.position.z > 200)
			transform.position = new Vector3(transform.position.x, transform.position.y, 200);
		if (transform.position.z < -100)
			transform.position = new Vector3(transform.position.x, transform.position.y, -100);
		if (transform.position.x > 200)
			transform.position = new Vector3(200, transform.position.y, transform.position.z);
		if (transform.position.x < -250)
			transform.position = new Vector3(-250, transform.position.y, transform.position.z);
		if (transform.position.y > 140)
			transform.position = new Vector3(transform.position.x, 140, transform.position.z);
		if (transform.position.y < 11)
			transform.position = new Vector3(transform.position.x, 11, transform.position.z);*/
	}

	private Vector3 GetBaseInput()
	{
		// Returns the current directions of movement
		Vector3 p_Velocity = Vector3.zero;

		if (Input.GetKey(KeyCode.W))
		{
			p_Velocity += new Vector3(0, 0, 1);
		}
		if (Input.GetKey(KeyCode.S))
		{
			p_Velocity += new Vector3(0, 0, -1);
		}
		if (Input.GetKey(KeyCode.A))
		{
			p_Velocity += new Vector3(-1, 0, 0);
		}
		if (Input.GetKey(KeyCode.D))
		{
			p_Velocity += new Vector3(1, 0, 0);
		}
		if (Input.GetKey(KeyCode.Space))
		{
			p_Velocity += new Vector3(0, 1, 0);
		}
		if (Input.GetKey(KeyCode.LeftShift))
		{
			p_Velocity += new Vector3(0, -1, 0);
		}
		return p_Velocity;
	}
}
