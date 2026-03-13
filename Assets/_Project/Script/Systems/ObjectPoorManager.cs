using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple generic Object Pool System to avoid frequent Instantiate and Destroy calls.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    // A dictionary linking a Prefab's InstanceID to a queue of available GameObjects.
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Gets a GameObject from the pool, or instantiates a new one if the pool is empty.
    /// </summary>
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        int poolKey = prefab.GetInstanceID();

        // If the pool doesn't exist for this prefab, create it
        if (!poolDictionary.ContainsKey(poolKey))
        {
            poolDictionary.Add(poolKey, new Queue<GameObject>());
        }

        GameObject objectToSpawn = null;

        // Try to get an inactive object from the queue
        while (poolDictionary[poolKey].Count > 0)
        {
            GameObject go = poolDictionary[poolKey].Dequeue();
            if (go != null) // Check if it wasn't destroyed by something else
            {
                objectToSpawn = go;
                break;
            }
        }

        // If no inactive object was found, instantiate a new one
        if (objectToSpawn == null)
        {
            objectToSpawn = Instantiate(prefab);
            // We use GetInstanceID() of the prefab as a tag so we know where to return it
            ObjectPoolTag tagComponent = objectToSpawn.AddComponent<ObjectPoolTag>();
            tagComponent.prefabInstanceId = poolKey;
        }

        // Reset state and activate
        objectToSpawn.transform.SetParent(parent);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    /// <summary>
    /// Returns a GameObject to its respective pool and deactivates it.
    /// </summary>
    public void ReturnToPool(GameObject objectToReturn)
    {
        ObjectPoolTag tagComponent = objectToReturn.GetComponent<ObjectPoolTag>();

        if (tagComponent != null)
        {
            objectToReturn.SetActive(false);
            objectToReturn.transform.SetParent(transform); // Parent it to the manager for tidiness

            if (!poolDictionary.ContainsKey(tagComponent.prefabInstanceId))
            {
                poolDictionary.Add(tagComponent.prefabInstanceId, new Queue<GameObject>());
            }

            poolDictionary[tagComponent.prefabInstanceId].Enqueue(objectToReturn);
        }
        else
        {
            // Fallback: If it wasn't spawned from the pool, just destroy it normally
            Destroy(objectToReturn);
        }
    }
}

/// <summary>
/// A small helper component to remember which pool dictionary queue an object belongs to.
/// </summary>
public class ObjectPoolTag : MonoBehaviour
{
    public int prefabInstanceId;
}
