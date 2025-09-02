using System.Collections.Generic;

[System.Serializable]
public class PlacedSplineData
{
    public List<SplinePointData> points = new List<SplinePointData>();
    public bool isClosed;
}