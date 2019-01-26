using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{

    WindController windController;

    float elapsedTime = 0f;
    float adjustmentInterval = 0f;

    [SerializeField]
    float minInterval, maxInterval;

    // Start is called before the first frame update
    void Start()
    {
        windController = GetComponent<WindController>();
    }

    // Update is called once per frame
    void Update()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= adjustmentInterval)
        {
            //Do adjustment stuff
            AdjustEnvironment();

            //Reset timer
            elapsedTime = 0;
            adjustmentInterval = Random.Range(minInterval, maxInterval);
        }
    }

    void AdjustEnvironment()
    {
        windController.RunChangeWindDirection();
    }
}
