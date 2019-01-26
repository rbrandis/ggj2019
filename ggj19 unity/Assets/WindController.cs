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

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator ChangeWindDirection()
    {
        float currentDirection = windTransform.rotation.eulerAngles.y;
        float targetDirection = Random.Range(minDirectionChange, maxDirectionChange);
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            Mathf.Lerp(currentDirection, targetDirection, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }
}
