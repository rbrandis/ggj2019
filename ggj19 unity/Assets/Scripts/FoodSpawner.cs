﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject[] foodObjects;

    [SerializeField]
    int maxFoodSpawns = 3;

    [SerializeField]
    LayerMask hitLayers;

    FoodZone[] foodZones;
    // Start is called before the first frame update
    void Start()
    {
        foodZones = GetComponentsInChildren<FoodZone>();

        for (int i = 0; i < maxFoodSpawns; i++)
            SpawnFood();
    }

    public void SpawnFood()
    {
        List<FoodZone> emptyFoodZones = new List<FoodZone>();
        for (int i = 0; i < foodZones.Length; i++)
            if (foodZones[i].hasFood == false)
                emptyFoodZones.Add(foodZones[i]);

        if (emptyFoodZones.Count > 0)
        {
            int foodZoneIndex = Random.Range(1, emptyFoodZones.Count - 1);
            Vector2 randomPos = Random.insideUnitCircle * emptyFoodZones[foodZoneIndex].transform.localScale.x / 2f;

            RaycastHit hit;

            while (true)
            {
                if (Physics.Raycast(new Vector3(emptyFoodZones[foodZoneIndex].transform.position.x + randomPos.x,
                    20f,
                    emptyFoodZones[foodZoneIndex].transform.position.z + randomPos.y), Vector3.down, out hit, 100f, hitLayers))
                {
                    if (hit.transform.tag == "Terrain")
                    {
                        int foodObjectIndex = Random.Range(0, foodObjects.Length - 1);

                        GameObject spawnedFood = GameObject.Instantiate(foodObjects[foodObjectIndex], hit.point, Quaternion.FromToRotation(transform.up, hit.normal));
                        spawnedFood.GetComponent<Food>().foodZone = emptyFoodZones[foodZoneIndex];
                        spawnedFood.GetComponent<Food>().foodSpawner = this;
                        emptyFoodZones[foodZoneIndex].hasFood = true;
                        break;
                    }
                }
            }
        }
    }
}
