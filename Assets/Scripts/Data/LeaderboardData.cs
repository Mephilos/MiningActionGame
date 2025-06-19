using System.Collections.Generic;

[System.Serializable]
public class LeaderboardData
{
    public List<int> highScores;

    public LeaderboardData()
    {
        highScores = new List<int>();
    }
}