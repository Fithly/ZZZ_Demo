using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProgressData
{
    public string CurrentStageId;
    public List<string> UnlockedStageIds = new List<string>();
    public List<string> FinishedQuestIds = new List<string>();
}