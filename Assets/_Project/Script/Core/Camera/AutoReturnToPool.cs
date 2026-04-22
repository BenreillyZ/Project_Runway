using UnityEngine;
using System.Collections;

/// <summary>
/// A helper component that automatically returns an object to the ObjectPoolManager
/// after a set amount of time or when a Particle System finishes playing.
/// Implements IPoolable to correctly reset its state when spawned from the pool.
/// </summary>
public class AutoReturnToPool : MonoBehaviour, IPoolable
{
    [Tooltip("If true, returns to pool after lifeTime seconds. If false, waits for ParticleSystem to finish.")]
    public bool useTime = true;
    public float lifeTime = 2.0f;

    private ParticleSystem _particleSystem;
    private Coroutine _returnCoroutine;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    public void OnSpawn()
    {
        if (useTime)
        {
            _returnCoroutine = StartCoroutine(ReturnAfterTime(lifeTime));
        }
        else if (_particleSystem != null)
        {
            _returnCoroutine = StartCoroutine(ReturnAfterParticles());
        }
    }

    public void OnDespawn()
    {
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }

        if (_particleSystem != null)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private IEnumerator ReturnAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        ReturnNow();
    }

    private IEnumerator ReturnAfterParticles()
    {
        // Wait until particle system stops playing
        while (_particleSystem != null && _particleSystem.IsAlive(true))
        {
            yield return null;
        }
        ReturnNow();
    }

    private void ReturnNow()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
