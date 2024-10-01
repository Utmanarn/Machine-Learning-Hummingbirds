using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of flower plants and attached flowers.
/// </summary>
public class FlowerArea : MonoBehaviour
{
    // The diameter of the area where the agent and flowers can be
    // used for observing relative distance from agent to flower.
    public const float AreaDiameter = 20f;

    private List<GameObject> _flowerPlants;
    private Dictionary<Collider, Flower> _nectarFlowerDictionary;
    private bool _findChildFlowersHasBeenCalled;
    
    public List<Flower> Flowers { get; private set; }

    private void Awake()
    {
        _flowerPlants = new List<GameObject>();
        _nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();

        FindChildFlowers(transform); // Must be called in awake to make sure it is called before the GameManager calls MainMenu and ResetFlowers.
    }

    public void ResetFlowers()
    {
        //if (!_findChildFlowersHasBeenCalled) FindChildFlowers(transform);

        foreach (var flowerPlant in _flowerPlants)
        {
            float xRotation = Random.Range(-5f, 5f);
            float zRotation = Random.Range(-5f, 5f);
            float yRotation = Random.Range(-180f, 180f);

            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        foreach (var flower in Flowers)
        {
            flower.ResetFlower();
        }
    }

    public Flower GetFlowerFromNectar(Collider collider)
    {
        return _nectarFlowerDictionary[collider];
    }

    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("flower_plant"))
            {
                _flowerPlants.Add(child.gameObject);
                
                FindChildFlowers(child);
            }
            else
            {
                Flower flower = child.GetComponent<Flower>();
                if (flower != null)
                {
                    Flowers.Add(flower);
                    _nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }
                else
                {
                    // Flower component not found, so check children.
                    FindChildFlowers(child);
                }
            }
        }
    }
}
