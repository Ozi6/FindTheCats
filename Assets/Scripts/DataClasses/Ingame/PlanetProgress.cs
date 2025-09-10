[System.Serializable]
public class PlanetProgress
{
    public bool hasStarted = false;
    public bool isCompleted = false;
    public float completionTime = 0f;
    public long lastPlayedTime = 0;
    public float progressPercentage = 0f;
}