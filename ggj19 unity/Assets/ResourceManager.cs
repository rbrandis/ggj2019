using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    [SerializeField]
    float life, lifeChildren;

    [SerializeField]
    float lifeDrainMultiplier, lifeChildrenDrainMultiplier;

    [SerializeField]
    int foodUnits;

    [SerializeField]
    Text lifeText, lifeChildrenText, foodUnitsText;

    // Start is called before the first frame update
    void Start()
    {
        UpdateTexts();
    }

    // Update is called once per frame
    void Update()
    {
        life -= Time.deltaTime * lifeDrainMultiplier;
        lifeChildren -= Time.deltaTime * lifeChildrenDrainMultiplier;
        UpdateTexts();
    }

    public void CollectFood()
    {
        foodUnits++;
    }

    void UpdateTexts()
    {
        lifeText.text = "LIFE: " + life;
        lifeChildrenText.text = "LIFE CHILDREN: " + lifeChildren;
        foodUnitsText.text = "FOOD: " + foodUnits;
    }
}
