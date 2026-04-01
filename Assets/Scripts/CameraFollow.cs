using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	[Header("Takip Edilecek Obje")]
	public Transform target;

	[Header("Kamera Ayarlarý")]
	public Vector3 offset = new Vector3(0f, 3f, -6f); // camera coordinate
	public float xSmoothness = 5f; 

	void LateUpdate()
	{
		if (target == null) return;

		// camera motion using lerp
		float smoothX = Mathf.Lerp(transform.position.x, target.position.x, xSmoothness * Time.deltaTime);

		// camera new coordinate
		Vector3 newPosition = new Vector3(smoothX, target.position.y + offset.y, target.position.z + offset.z);

		transform.position = newPosition;
	}
}