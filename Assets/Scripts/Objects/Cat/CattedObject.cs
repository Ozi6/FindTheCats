using UnityEngine;

public class CattedObject : AnimatedPlanetObject
{
    [Header("Catted Object Settings")]
    public Cat associatedCat;
    public Transform catFoundPosition;
    public bool hasSpecialAnimation = true;
    public float animationDuration = 1f;
    public GameObject celebrationEffect;

    private bool animationPlayed = false;

    public override void Initialize(Planet planet)
    {
        base.Initialize(planet);
        blocksCats = false;
        if (associatedCat != null)
        {
            associatedCat.cattedObject = this;
            associatedCat.transform.SetParent(transform);
            if (catFoundPosition != null)
            {
                Vector3 hidingOffset = -catFoundPosition.localPosition;
                associatedCat.transform.localPosition = hidingOffset;
            }
        }
    }

    protected override void UpdateAnimation()
    {
        if (!animationPlayed)
            transform.position += Vector3.up * Mathf.Sin(Time.time * animationSpeed * 2f) * 0.005f;
    }

    public void PlayCatFoundAnimation()
    {
        if (animationPlayed || associatedCat == null) return;
        animationPlayed = true;
        if (hasSpecialAnimation)
            StartCoroutine(CatRevealAnimation());
        if (celebrationEffect != null)
            Instantiate(celebrationEffect, transform.position, transform.rotation);
    }

    private System.Collections.IEnumerator CatRevealAnimation()
    {
        if (catFoundPosition != null && associatedCat != null)
        {
            Vector3 startPos = associatedCat.transform.localPosition;
            Vector3 endPos = catFoundPosition.localPosition;

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = 1 - (1 - t) * (1 - t);
                associatedCat.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                float hop = Mathf.Sin(t * Mathf.PI) * 0.3f;
                associatedCat.transform.localPosition += Vector3.up * hop;
                yield return null;
            }

            associatedCat.transform.localPosition = endPos;
        }
    }
}