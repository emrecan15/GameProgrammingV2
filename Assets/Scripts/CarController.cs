using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
	[Header("Temel Hareket")]
	public float laneDistance = 3.0f;
	public float forwardSpeed = 20.0f;
	public float laneChangeSpeed = 10.0f;

	[Header("Zorluk (Hýzlanma) Ayarlarý")]
	public float maxSpeed = 50.0f;
	public float acceleration = 0.5f;

	[Header("Dönüþ Ayarlarý")]
	public float turnAngle = 15.0f;
	public float turnSpeed = 15.0f;

	private int currentLane = 1; // 0: left, 1: middle, 2: right

	void Update()
	{
		// adding acceleration
		if (forwardSpeed < maxSpeed)
		{
			forwardSpeed += acceleration * Time.deltaTime;
		}

		// controller(input)
		if (Keyboard.current != null)
		{
			if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
			{
				currentLane++;
				if (currentLane > 2) currentLane = 2;
			}

			if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
			{
				currentLane--;
				if (currentLane < 0) currentLane = 0;
			}
		}

		// go forward
		transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime, Space.World);

		// left/right motion using larp function
		float targetX = (currentLane - 1) * laneDistance;
		float smoothX = Mathf.Lerp(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
		transform.position = new Vector3(smoothX, transform.position.y, transform.position.z);

		// steering
		float xDiff = targetX - transform.position.x;
		float targetRotationY = xDiff * turnAngle;
		Quaternion targetRotation = Quaternion.Euler(0, targetRotationY, 0);
		transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
	}

	// crash control
	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Obstacle"))
		{
			if (GameManager.Instance != null)
				GameManager.Instance.GameOver();
		}
		else if (other.CompareTag("Coin"))
		{
			if (GameManager.Instance != null)
				GameManager.Instance.AddCoin();

			other.gameObject.SetActive(false);
		}
	}
}

