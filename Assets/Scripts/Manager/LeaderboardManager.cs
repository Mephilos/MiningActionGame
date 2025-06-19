using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;


public class LeaderboardManager
{
    private const int LeaderboardSize = 5;
    private readonly string savePath;

    public List<int> HighScores { get; private set; }

    public LeaderboardManager()
    {
        savePath = Path.Combine(Application.persistentDataPath, "leaderboard.dat");
        HighScores = new List<int>();
        LoadScores();
    }

    /// <summary>
    /// 저장된 점수 불러오기
    /// </summary>
    private void LoadScores()
    {
        if (File.Exists(savePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = null;
            try
            {
                stream = new FileStream(savePath, FileMode.Open);
                LeaderboardData data = formatter.Deserialize(stream) as LeaderboardData;

                if (data != null)
                {
                    HighScores = data.highScores;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"점수 파일 로드 실패: {e.Message}");
                HighScores.Clear(); // 문제가 있을 시 랭킹을 초기화
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }
        while (HighScores.Count < LeaderboardSize)
        {
            HighScores.Add(0);
        }
    }

    /// <summary>
    /// 최고 기록 저장
    /// </summary>
    private void SaveScores()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = null;
        try
        {
            stream = new FileStream(savePath, FileMode.Create);
            LeaderboardData data = new LeaderboardData { highScores = this.HighScores };
            formatter.Serialize(stream, data);
        }
        catch (Exception e)
        {
            Debug.LogError($"점수 파일 저장 실패: {e.Message}");
        }
        finally
        {
            if (stream != null)
            {
                stream.Close();
            }
        }
    }

    /// <summary>
    /// 새로운 점수를 랭킹에 추가
    /// </summary>
    /// <param name="newScore">새로운 게임 점수 (클리어한 스테이지)</param>
    public void AddScore(int newScore)
    {
        HighScores.Add(newScore);
        HighScores.Sort((a, b) => b.CompareTo(a)); // 내림차순 정렬

        if (HighScores.Count > LeaderboardSize)
        {
            HighScores.RemoveRange(LeaderboardSize, HighScores.Count - LeaderboardSize);
        }

        SaveScores();
    }
}
