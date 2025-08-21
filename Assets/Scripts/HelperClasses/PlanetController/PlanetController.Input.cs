using UnityEngine;

public partial class PlanetController
{
    void HandleInput()
    {
        if (enableMouseInput)
            HandleMouseInput();
        if (enableTouchInput && Input.touchCount > 0)
            HandleTouchInput();
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                IClickable clickable = hit.collider.GetComponent<IClickable>();
                if (clickable != null && clickable.IsClickable())
                {
                    clickable.OnClicked();
                    return;
                }
            }
            StartDragging(mousePos);
        }
        else if (Input.GetMouseButton(0) && isDragging)
            ContinueDragging(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0))
            StopDragging();
    }

    void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
            Ray ray = mainCamera.ScreenPointToRay(touch.position);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                IClickable clickable = hit.collider.GetComponent<IClickable>();
                if (clickable != null && clickable.IsClickable())
                {
                    clickable.OnClicked();
                    return;
                }
            }
            StartDragging(touch.position);
        }
        else if (touch.phase == TouchPhase.Moved && isDragging)
            ContinueDragging(touch.position);
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            StopDragging();
    }
}
