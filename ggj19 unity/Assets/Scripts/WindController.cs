using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindController : MonoBehaviour
{
    [SerializeField]
    Transform windTransform;

    [SerializeField]
    float minDirectionChange = -180f;
    [SerializeField]
    float maxDirectionChange = 180f;

    [SerializeField]
    float transitionDuration = 1f;

    public float windDirection { get; private set; }

    public void RunChangeWindDirection()
    {
        StartCoroutine(ChangeWindDirection());
    }

    IEnumerator ChangeWindDirection()
    {
        float currentDirection = windTransform.rotation.eulerAngles.y;
        float targetDirection = Random.Range(minDirectionChange, maxDirectionChange);
        float elapsedTime = 0f;

        while (elapsedTime <= transitionDuration)
        {
            windDirection = Mathf.Lerp(currentDirection, targetDirection, elapsedTime / transitionDuration);

            Quaternion newRotation = Quaternion.Euler(windTransform.rotation.eulerAngles.x, windDirection, windTransform.rotation.z);
            windTransform.rotation = newRotation;

            elapsedTime += Time.deltaTime;

            if (elapsedTime >= transitionDuration)
                elapsedTime = transitionDuration;
            yield return new WaitForEndOfFrame();
        }
    }
}
