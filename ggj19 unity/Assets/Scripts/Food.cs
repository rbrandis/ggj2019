using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{
    [SerializeField]
    Transform particleTransform;

    public FoodZone foodZone;
    public FoodSpawner foodSpawner;

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
            StartCoroutine(RemoveFood());
    }

    IEnumerator RemoveFood()
    {
        GameObject.FindGameObjectWithTag("GameMaster").GetComponent<ResourceManager>().CollectFood();
        foodZone.hasFood = false;
        //Find and disable all mesh renderers
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
            meshRenderers[i].enabled = false;

        GetComponent<SphereCollider>().enabled = false;
        foodSpawner.SpawnFood();

        //Wait for duration of particle lifetime before destroying object to make sure active particles won't be deleted
        GetComponentInChildren<ParticleSystem>().Stop();
        yield return new WaitForSeconds(GetComponentInChildren<ParticleSystem>().startLifetime);
        GameObject.Destroy(gameObject);
    }
}
