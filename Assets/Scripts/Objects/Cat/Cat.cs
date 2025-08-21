using UnityEngine;
using System;

public class Cat : AnimatedPlanetObject, IClickable
{
    [Header("Cat Settings")]
    public bool isFound = false;
    public GameObject foundEffect;
    public AudioClip meowSound;

    [Header("Catted Object Integration")]
    public CattedObject cattedObject;

    public event Action<Cat> OnCatClicked;

    private SphereCollider catCollider;

    public override void Initialize(Planet planet)
    {
        base.Initialize(planet);
        blocksCats = false;

        catCollider = GetComponent<SphereCollider>();
        if (catCollider == null)
        {
            catCollider = gameObject.AddComponent<SphereCollider>();
            catCollider.radius = 0.3f;
        }
        catCollider.isTrigger = true;

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }

    protected override void UpdateAnimation()
    {
        if (!isFound)
            transform.localPosition += Vector3.up * Mathf.Sin(Time.time * animationSpeed) * 0.01f;
    }

    public void OnClicked()
    {
        if (!isFound && IsClickable())
        {
            isFound = true;
            if (cattedObject != null)
                cattedObject.PlayCatFoundAnimation();
            if (meowSound != null)
                AudioSource.PlayClipAtPoint(meowSound, transform.position);
            if (foundEffect != null)
                Instantiate(foundEffect, transform.position, transform.rotation);
            transform.localScale *= 1.2f;
            OnCatClicked?.Invoke(this);
        }
    }

    public bool IsClickable()
    {
        return !isFound;
    }
}