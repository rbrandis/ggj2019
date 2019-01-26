using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{
    [SerializeField]
    Transform particleTransform;

    public FoodZone foodZone;

    WindController windController;

    // Start is called before the first frame update
    void Start()
    {
        windController = GameObject.FindGameObjectWithTag("GameMaster").GetComponent<WindController>();
    }

    // Update is called once per frame
    void Update()
    {
        particleTransform.rotation = Quaternion.Euler(particleTransform.rotation.eulerAngles.x, windController.windDirection, particleTransform.rotation.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            //Do something
            GameObject.FindGameObjectWithTag("GameMaster").GetComponent<ResourceManager>().CollectFood();
            foodZone.HasFood = false;
            GameObject.Destroy(gameObject);
        }
    }
}
