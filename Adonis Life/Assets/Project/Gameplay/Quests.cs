using System;
using System.Collections.Generic;

namespace AdonisLife.Gameplay
{
    /// <summary>Lifecycle of a quest.</summary>
    public enum QuestState
    {
        NotStarted,
        Active,
        Completed
    }

    /// <summary>One countable objective inside a quest.</summary>
    [Serializable]
    public class QuestObjective
    {
        public string id;
        public string description;
        public int requiredCount;
        public int progress;

        public bool IsComplete => progress >= requiredCount;
    }

    /// <summary>
    /// A quest with ordered objectives. Missions are quests whose objectives must be completed
    /// in sequence (<see cref="sequential"/>); free-form quests allow progress in any order.
    /// </summary>
    [Serializable]
    public class Quest
    {
        public string id;
        public string title;
        public bool sequential;
        public QuestState state = QuestState.NotStarted;
        public List<QuestObjective> objectives = new List<QuestObjective>();

        public bool AllObjectivesComplete
        {
            get
            {
                foreach (QuestObjective objective in objectives)
                {
                    if (!objective.IsComplete)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    /// <summary>
    /// Pure quest tracking: registers quests, advances objective progress (respecting mission
    /// ordering), and completes quests when every objective is done. Raises events for UI and
    /// gameplay hooks.
    /// </summary>
    public class QuestLog
    {
        private readonly Dictionary<string, Quest> _quests = new Dictionary<string, Quest>();

        /// <summary>Raised when a quest becomes active.</summary>
        public event Action<Quest> OnQuestStarted;

        /// <summary>Raised when objective progress changes.</summary>
        public event Action<Quest, QuestObjective> OnObjectiveProgress;

        /// <summary>Raised when a quest completes.</summary>
        public event Action<Quest> OnQuestCompleted;

        public IReadOnlyDictionary<string, Quest> Quests => _quests;

        public void Register(Quest quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.id))
            {
                throw new ArgumentException("Quest must have an id.", nameof(quest));
            }

            _quests[quest.id] = quest;
        }

        public bool Start(string questId)
        {
            if (!_quests.TryGetValue(questId, out Quest quest) || quest.state != QuestState.NotStarted)
            {
                return false;
            }

            quest.state = QuestState.Active;
            OnQuestStarted?.Invoke(quest);
            return true;
        }

        /// <summary>
        /// Advances an objective of an active quest. Sequential quests only accept progress on
        /// their first incomplete objective.
        /// </summary>
        public bool AddProgress(string questId, string objectiveId, int amount = 1)
        {
            if (amount <= 0 || !_quests.TryGetValue(questId, out Quest quest) || quest.state != QuestState.Active)
            {
                return false;
            }

            QuestObjective target = null;
            foreach (QuestObjective objective in quest.objectives)
            {
                if (quest.sequential && !objective.IsComplete)
                {
                    target = objective.id == objectiveId ? objective : null;
                    break;
                }

                if (objective.id == objectiveId)
                {
                    target = objective;
                    break;
                }
            }

            if (target == null || target.IsComplete)
            {
                return false;
            }

            target.progress = Math.Min(target.progress + amount, target.requiredCount);
            OnObjectiveProgress?.Invoke(quest, target);

            if (quest.AllObjectivesComplete)
            {
                quest.state = QuestState.Completed;
                OnQuestCompleted?.Invoke(quest);
            }

            return true;
        }
    }
}
