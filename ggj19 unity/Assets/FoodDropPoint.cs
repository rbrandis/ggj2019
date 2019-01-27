using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodDropPoint : MonoBehaviour
{
    ResourceManager resourceManager;
    // Start is called before the first frame update
    void Start()
    {
        resourceManager = GameObject.FindGameObjectWithTag("GameMaster").GetComponent<ResourceManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            resourceManager.DeliverFood();
        }
    }
}
