using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindController : MonoBehaviour
{
    [SerializeField]
    Transform windTransform, particleTransform;

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

            Quaternion newRotation = Quaternion.Euler(particleTransform.rotation.eulerAngles.x, windDirection, particleTransform.rotation.z);
            windTransform.rotation = newRotation;
            particleTransform.rotation = newRotation;

            elapsedTime += Time.deltaTime;

            if (elapsedTime >= transitionDuration)
                elapsedTime = transitionDuration;
            yield return new WaitForEndOfFrame();
        }
        print("done");
    }
}
