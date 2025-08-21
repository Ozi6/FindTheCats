using UnityEngine;

public class CatHidingObject : AnimatedPlanetObject, IClickable
{
    [Header("Cat Hiding Object Settings")]
    public Cat hiddenCat;
    public bool isOpened = false;
    public GameObject openEffect;
    public float openAnimationDuration = 1f;

    [Header("Animation Parts")]
    public Transform movingPart;
    public Vector3 openOffset = Vector3.forward;
    public bool rotateInsteadOfMove = false;
    public Vector3 openRotation = new Vector3(0, 0, 90);

    private Vector3 originalPosition;
    private Vector3 originalRotation;

    public override void Initialize(Planet planet)
    {
        base.Initialize(planet);
        if (movingPart != null)
        {
            originalPosition = movingPart.localPosition;
            originalRotation = movingPart.localEulerAngles;
        }
        if (hiddenCat != null)
        {
            hiddenCat.gameObject.SetActive(false);
            hiddenCat.transform.SetParent(transform);
        }
        var collider = GetComponent<Collider>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider>();
    }

    protected override void UpdateAnimation()
    {
        if (!isOpened)
        {
            if (movingPart != null)
            {
                float wiggle = Mathf.Sin(Time.time * animationSpeed * 3f) * 0.01f;
                movingPart.localPosition = originalPosition + Vector3.right * wiggle;
            }
        }
    }

    public void OnClicked()
    {
        if (!isOpened && IsClickable())
            OpenAndRevealCat();
    }

    public bool IsClickable()
    {
        return !isOpened && hiddenCat != null;
    }

    private void OpenAndRevealCat()
    {
        isOpened = true;
        if (openEffect != null)
            Instantiate(openEffect, transform.position, transform.rotation);
        StartCoroutine(OpenAnimation());
    }

    private System.Collections.IEnumerator OpenAnimation()
    {
        float elapsed = 0f;

        while (elapsed < openAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / openAnimationDuration;

            if (movingPart != null)
            {
                if (rotateInsteadOfMove)
                    movingPart.localEulerAngles = Vector3.Lerp(originalRotation, originalRotation + openRotation, t);
                else
                    movingPart.localPosition = Vector3.Lerp(originalPosition, originalPosition + openOffset, t);
            }

            yield return null;
        }
        if (hiddenCat != null)
        {
            hiddenCat.gameObject.SetActive(true);
            //hiddenCat.Initialize(parentPlanet);
            hiddenCat.transform.localPosition = Vector3.zero;
        }
    }
}