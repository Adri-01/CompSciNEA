using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public GameObject focus;
    public float distance = 4f;
    public float height = 1.5f;
    public float smoothTime = 3f;
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, focus.transform.position + focus.transform.TransformDirection(new Vector3(0f, height, -distance)), smoothTime * Time.deltaTime);
        transform.LookAt(focus.transform.position + new Vector3(0, 1, 0));
    }
}
