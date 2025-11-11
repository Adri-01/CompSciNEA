using UnityEngine;

public class RacingLineTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RacingLinePoint"))
        {
            RacingLinePoint point = other.GetComponent<RacingLinePoint>();
            Debug.Log(point);
            if (point != null)
            {
                point.SetActiveState(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RacingLinePoint"))
        {
            RacingLinePoint point = other.GetComponent<RacingLinePoint>();
            if (point != null)
            {
                point.SetActiveState(false);
            }
        }
    }
}