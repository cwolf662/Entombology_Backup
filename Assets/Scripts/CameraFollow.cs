using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 8f;

    //New target for every new game started
    public void SetTarget(Transform newTarget) {target = newTarget;}

    //Smoothly moves camera around to the player
    private void LateUpdate() {
        if (target == null)
            return;

        Vector3 targetPosition = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime)
        );
    }
}
