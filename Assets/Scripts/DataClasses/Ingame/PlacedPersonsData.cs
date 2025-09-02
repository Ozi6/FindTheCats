using Dreamteck.Splines;

[System.Serializable]
public class PlacedPersonData : PlacedObjectData
{
    public SplinePoint[] splinePoints;
    public bool isClosed;
    public Spline.Type splineType;
    public int assignedSplineIndex = -1;
}