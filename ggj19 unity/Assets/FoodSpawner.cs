using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject foodObject;

    FoodZone[] foodZones;
    // Start is called before the first frame update
    void Start()
    {
        foodZones = GetComponentsInChildren<FoodZone>();
        SpawnFood();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void SpawnFood()
    {


        List<IFoodZone> emptyFoodZones = new List<IFoodZone>();
        for (int i = 0; i < foodZones.Length; i++)
            if (foodZones[i].HasFood == false)
                emptyFoodZones.Add(foodZones[i]);

        if (emptyFoodZones.Count > 0)
        {
            int foodZoneIndex = Random.Range(1, emptyFoodZones.Count - 1);
            Vector2 randomPos = Random.insideUnitCircle * emptyFoodZones[foodZoneIndex].FoodZoneTransform.localScale.x / 2f;

            RaycastHit hit;

            while (true)
            {
                if (Physics.Raycast(new Vector3(randomPos.x, 20f, randomPos.y), Vector3.down, out hit, 100f))
                {
                    if (hit.transform.tag == "Terrain")
                    {
                        GameObject spawnedFood = GameObject.Instantiate(foodObject, hit.point, Quaternion.identity);
                        spawnedFood.GetComponent<IFoodZone>().FoodZoneTransform = emptyFoodZones[foodZoneIndex].FoodZoneTransform;
                        emptyFoodZones[foodZoneIndex].HasFood = true;
                        break;
                    }
                }
            }
        }
    }
}

interface IFoodZone
{
    Transform FoodZoneTransform { get; set; }
    bool HasFood { get; set; }

}

public struct FoodZone : IFoodZone
{
    private Transform _foodZoneTransform;
    public Transform FoodZoneTransform
    {
        get { return _foodZoneTransform; }
        set { FoodZoneTransform = value; }
    }

    private bool _hasfood;
    public bool HasFood
    {
        get { return _hasfood; }
        set { HasFood = value; }
    }
}
