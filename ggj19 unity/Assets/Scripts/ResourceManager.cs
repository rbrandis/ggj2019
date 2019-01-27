using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    Image lifeImage, lifeChildrenImage;

    [SerializeField]
    Text foodUnitsText;

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

        if (life <= 0 || lifeChildren <= 0)
            SceneManager.LoadScene(0);
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
        lifeImage.fillAmount = life / 100f;
        lifeChildrenImage.fillAmount = lifeChildren / 100f;
        foodUnitsText.text = "FOOD: " + foodUnits;
    }
}
