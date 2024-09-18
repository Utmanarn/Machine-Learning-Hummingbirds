using UnityEngine;


/// <summary>
/// Manages a single flower with nectar.
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The colour when the flower is full.")]
    public Color fullFlowerColour = new Color(1f, 0f, .3f);
    
    [Tooltip("The colour when the flower is empty.")]
    public Color emptyFlowerColour = new Color(.5f, 0f, 1f);
    
    /// <summary>
    /// The trigger collider representing the nectar.
    /// </summary>
    [HideInInspector]
    public Collider nectarCollider;
    
    // The solid collider representing the flower petals.
    private Collider flowerCollider;

    private Material flowerMaterial;

    public Vector3 FlowerUpVector
    {
        get
        {
            return nectarCollider.transform.up;
        }
    }

    /// <summary>
    /// The center position of the nectar collider.
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get
        {
            return nectarCollider.transform.position;
        }
    }
    
    /// <summary>
    /// The amount of nectar remaining in the flower.
    /// </summary>
    public float NectarAmount { get; private set; }

    public bool HasNectar
    {
        get
        {
            return NectarAmount > 0f;
        }
    }

    public void Awake()
    {
        flowerMaterial = GetComponent<MeshRenderer>().material;
        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }

    /// <summary>
    /// Attempts to remove nectar from the flower.
    /// </summary>
    /// <param name="amount">The amount of nectar to remove.</param>
    /// <returns>The actual amount successfully removed.</returns>
    public float Feed(float amount)
    {
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        NectarAmount -= amount;

        if (NectarAmount <= 0)
        {
            NectarAmount = 0f;
            
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);
            
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColour);
        }
        
        return nectarTaken;
    }

    public void ResetFlower()
    {
        NectarAmount = 1f;
        
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);
        
        flowerMaterial.SetColor("_BaseColor", fullFlowerColour);
    }
}
