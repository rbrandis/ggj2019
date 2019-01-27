using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    [SerializeField]
    float life, lifeChildren;

    [SerializeField]
    float foodLife, foodLifeChildren;   //The amount of life received when food is collected

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

        if (Input.GetKeyDown(KeyCode.F) && foodUnits > 0)
        {
            foodUnits--;
            life += foodLife;
            if (life > 100f)
                life = 100f;
        }

    }

    public void CollectFood()
    {
        foodUnits++;
    }

    public void DeliverFood()
    {
        lifeChildren += foodUnits * foodLifeChildren;
        foodUnits = 0;
    }

    void UpdateTexts()
    {
        lifeText.text = "LIFE: " + life;
        lifeChildrenText.text = "LIFE CHILDREN: " + lifeChildren;
        foodUnitsText.text = "FOOD: " + foodUnits;
    }
}
