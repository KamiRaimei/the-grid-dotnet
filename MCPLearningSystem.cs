// MCPLearningSystem.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GridSimulation
{
    public class MCPLearningSystem
    {
        private string _personalityFile = "mcp_personality.json";
        private Queue<Dictionary<string, object>> _experienceBuffer;
        private Queue<(string action, double reward, DateTime time)> _actionHistory;
        public double LearningRate { get; private set; }
        public double ExplorationRate { get; private set; }
        private double _discountFactor;
        private double _temperature;
        public int TrainingSteps { get; private set; }
        private double _totalReward;
        public int EpisodeCount { get; set; }
        private DateTime _lastSaveTime;
        private double _saveInterval = 300; // 5 minutes

        public Dictionary<string, double> PersonalityTraits { get; private set; }
        private Dictionary<string, Dictionary<string, double>> _qTable;
        private Dictionary<string, int> _stateVisits;
        private Queue<double> _rewardHistory;
        private Queue<double> _punishmentHistory;

        public Dictionary<string, Dictionary<string, object>> ScenarioPerformance { get; private set; }

        public MCPLearningSystem()
        {
            _experienceBuffer = new Queue<Dictionary<string, object>>();
            _actionHistory = new Queue<(string, double, DateTime)>();
            
            LearningRate = 0.15;
            ExplorationRate = 0.35;
            _discountFactor = 0.95;
            _temperature = 1.0;
            TrainingSteps = 0;
            _totalReward = 0.0;
            EpisodeCount = 0;
            _lastSaveTime = DateTime.Now;

            PersonalityTraits = new Dictionary<string, double>
            {
                ["aggression"] = 0.5,
                ["cooperation"] = 0.7,
                ["efficiency_focus"] = 0.8,
                ["risk_taking"] = 0.3,
                ["learning_curiosity"] = 0.6,
                ["stability_preference"] = 0.7,
                ["adaptation_speed"] = 0.5,
                ["calculation_priority"] = 0.8,
                ["bug_tolerance"] = 0.4,
                ["user_tolerance"] = 0.6,
                ["innovation_bias"] = 0.5,
                ["conservatism"] = 0.3
            };

            // attempt to load previously saved personality
            LoadPersonality();

            _qTable = new Dictionary<string, Dictionary<string, double>>();
            _stateVisits = new Dictionary<string, int>();
            _rewardHistory = new Queue<double>();
            _punishmentHistory = new Queue<double>();

            ScenarioPerformance = new Dictionary<string, Dictionary<string, object>>
            {
                ["efficiency_optimization"] = new Dictionary<string, object> { ["success"] = 0, ["failure"] = 0, ["total_reward"] = 0.0 },
                ["bug_containment"] = new Dictionary<string, object> { ["success"] = 0, ["failure"] = 0, ["total_reward"] = 0.0 },
                ["user_cooperation"] = new Dictionary<string, object> { ["success"] = 0, ["failure"] = 0, ["total_reward"] = 0.0 },
                ["calculation_boost"] = new Dictionary<string, object> { ["success"] = 0, ["failure"] = 0, ["total_reward"] = 0.0 },
                ["stabilization"] = new Dictionary<string, object> { ["success"] = 0, ["failure"] = 0, ["total_reward"] = 0.0 },
                ["connectivity_improvement"] = new Dictionary<string, object> { ["success"] = 0, ["failure"] = 0, ["total_reward"] = 0.0 }
            };
        }

        public void RecordExperience(Dictionary<string, double> state, string action, double reward, 
                                      Dictionary<string, double> nextState, bool done = false)
        {
            var experience = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.Now.ToString("o"),
                ["state"] = CompressState(state),
                ["action"] = action,
                ["reward"] = reward,
                ["next_state"] = CompressState(nextState),
                ["done"] = done,
                ["personality_traits"] = new Dictionary<string, double>(PersonalityTraits),
                ["training_step"] = TrainingSteps
            };

            _experienceBuffer.Enqueue(experience);
            while (_experienceBuffer.Count > 2000) _experienceBuffer.Dequeue();

            _actionHistory.Enqueue((action, reward, DateTime.Now));
            while (_actionHistory.Count > 500) _actionHistory.Dequeue();

            if (reward > 0)
            {
                _rewardHistory.Enqueue(reward);
                while (_rewardHistory.Count > 100) _rewardHistory.Dequeue();
            }
            else if (reward < 0)
            {
                _punishmentHistory.Enqueue(reward);
                while (_punishmentHistory.Count > 100) _punishmentHistory.Dequeue();
            }

            _totalReward += reward;
            TrainingSteps++;

            LearnFromExperience(experience);

            string scenario = IdentifyScenario(action);
            if (ScenarioPerformance.ContainsKey(scenario))
            {
                if (reward > 0)
                    ScenarioPerformance[scenario]["success"] = (int)ScenarioPerformance[scenario]["success"] + 1;
                else if (reward < 0)
                    ScenarioPerformance[scenario]["failure"] = (int)ScenarioPerformance[scenario]["failure"] + 1;
                
                ScenarioPerformance[scenario]["total_reward"] = (double)ScenarioPerformance[scenario]["total_reward"] + reward;
            }

            AdaptLearningRate();
        }

        private void LearnFromExperience(Dictionary<string, object> experience)
        {
            var state = experience["state"] as Dictionary<string, double>;
            string action = experience["action"]?.ToString() ?? "";
            double reward = Convert.ToDouble(experience["reward"]);
            var nextState = experience["next_state"] as Dictionary<string, double>;

            string stateKey = string.Join(",", state?.Select(kvp => $"{kvp.Key}:{kvp.Value:F2}") ?? Array.Empty<string>());
            string nextStateKey = string.Join(",", nextState?.Select(kvp => $"{kvp.Key}:{kvp.Value:F2}") ?? Array.Empty<string>());

            if (!_qTable.ContainsKey(stateKey))
                _qTable[stateKey] = new Dictionary<string, double>();

            double oldValue = _qTable[stateKey].ContainsKey(action) ? _qTable[stateKey][action] : 0;

            double nextMax = 0;
            if (_qTable.ContainsKey(nextStateKey) && _qTable[nextStateKey].Any())
                nextMax = _qTable[nextStateKey].Values.Max();

            double newValue = oldValue + LearningRate * (reward + _discountFactor * nextMax - oldValue);
            _qTable[stateKey][action] = newValue;

            if (!_stateVisits.ContainsKey(stateKey))
                _stateVisits[stateKey] = 0;
            _stateVisits[stateKey]++;

            UpdatePersonality(experience);
            UpdateExploration();
        }

        private void UpdatePersonality(Dictionary<string, object> experience)
        {
            double reward = Convert.ToDouble(experience["reward"]);
            string action = experience["action"]?.ToString() ?? "";

            var adjustments = MapActionToTraits(action, reward);

            foreach (var kvp in adjustments)
            {
                if (PersonalityTraits.ContainsKey(kvp.Key))
                {
                    double currentValue = PersonalityTraits[kvp.Key];
                    double newValue = currentValue + kvp.Value * LearningRate;
                    newValue = Math.Max(0.1, Math.Min(0.9, newValue));
                    PersonalityTraits[kvp.Key] = 0.8 * currentValue + 0.2 * newValue;
                }
            }
        }

        private Dictionary<string, double> MapActionToTraits(string action, double reward)
        {
            var adjustments = new Dictionary<string, double>();
            string actionLower = action.ToLower();

            if (actionLower.Contains("remove") || actionLower.Contains("destroy") || 
                actionLower.Contains("eliminate") || actionLower.Contains("quarantine"))
            {
                adjustments["aggression"] = reward * 0.5;
                adjustments["risk_taking"] = reward * 0.3;
            }

            if (actionLower.Contains("cooperate") || actionLower.Contains("allow") || 
                actionLower.Contains("accept") || actionLower.Contains("comply"))
            {
                adjustments["cooperation"] = reward * 0.7;
                adjustments["user_tolerance"] = reward * 0.4;
            }

            if (actionLower.Contains("optimize") || actionLower.Contains("boost") || 
                actionLower.Contains("improve") || actionLower.Contains("efficiency"))
            {
                adjustments["efficiency_focus"] = reward * 0.8;
                adjustments["calculation_priority"] = reward * 0.6;
            }

            if (actionLower.Contains("stabilize") || actionLower.Contains("protect") || 
                actionLower.Contains("defend") || actionLower.Contains("secure"))
            {
                adjustments["stability_preference"] = reward * 0.7;
                adjustments["conservatism"] = reward * 0.4;
            }

            if (actionLower.Contains("innovate") || actionLower.Contains("create") || 
                actionLower.Contains("deploy") || actionLower.Contains("experiment"))
            {
                adjustments["innovation_bias"] = reward * 0.6;
                adjustments["learning_curiosity"] = reward * 0.5;
                adjustments["adaptation_speed"] = reward * 0.3;
            }

            if (!adjustments.ContainsKey("learning_curiosity"))
                adjustments["learning_curiosity"] = reward * 0.2;

            return adjustments;
        }

        private void UpdateExploration()
        {
            double explorationDecay = 0.9995;
            ExplorationRate = Math.Max(0.05, ExplorationRate * explorationDecay);

            var recentRewards = _rewardHistory.TakeLast(10).ToList();
            if (recentRewards.Any())
            {
                double avgRecentReward = recentRewards.Average();
                if (avgRecentReward > 0)
                    _temperature = Math.Max(0.5, _temperature * 0.99);
                else
                    _temperature = Math.Min(2.0, _temperature * 1.01);
            }
        }

        private void AdaptLearningRate()
        {
            if (_rewardHistory.Count < 20) return;

            var recentRewards = _rewardHistory.TakeLast(20).ToList();
            double rewardStd = recentRewards.Count > 1 ? StdDev(recentRewards) : 0;

            if (rewardStd < 0.1)
                LearningRate = Math.Min(0.3, LearningRate * 1.01);
            else if (rewardStd > 0.3)
                LearningRate = Math.Max(0.05, LearningRate * 0.99);
        }

        private double StdDev(List<double> values)
        {
            double avg = values.Average();
            double sum = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sum / values.Count);
        }

        public string GetAction(Dictionary<string, double> state, List<string> possibleActions)
        {
            string stateKey = string.Join(",", state.Select(kvp => $"{kvp.Key}:{kvp.Value:F2}"));

            if (new Random().NextDouble() < ExplorationRate)
                return possibleActions[new Random().Next(possibleActions.Count)];

            if (!_qTable.ContainsKey(stateKey))
                _qTable[stateKey] = new Dictionary<string, double>();

            var qValues = new Dictionary<string, double>();
            foreach (string action in possibleActions)
            {
                qValues[action] = _qTable[stateKey].ContainsKey(action) ? _qTable[stateKey][action] : 0;
            }

            if (!qValues.Any())
                return possibleActions[new Random().Next(possibleActions.Count)];

            double maxQ = qValues.Values.Max();
            var expValues = qValues.ToDictionary(
                kvp => kvp.Key, 
                kvp => Math.Exp((kvp.Value - maxQ) / _temperature)
            );
            double sumExp = expValues.Values.Sum();

            if (sumExp == 0)
                return possibleActions[new Random().Next(possibleActions.Count)];

            double randomValue = new Random().NextDouble() * sumExp;
            double cumulative = 0;
            foreach (var kvp in expValues)
            {
                cumulative += kvp.Value;
                if (randomValue <= cumulative)
                    return kvp.Key;
            }

            return expValues.Last().Key;
        }

        public double CalculateReward(Dictionary<string, double> prevState, Dictionary<string, double> newState, string action)
        {
            double reward = 0.0;

            double efficiencyGain = newState.GetValueOrDefault("loop_efficiency", 0) - 
                                    prevState.GetValueOrDefault("loop_efficiency", 0);
            reward += efficiencyGain * 3.0;

            double rateGain = newState.GetValueOrDefault("calculation_rate", 0) - 
                              prevState.GetValueOrDefault("calculation_rate", 0);
            reward += rateGain * 0.01;

            double stabilityGain = newState.GetValueOrDefault("stability", 0) - 
                                   prevState.GetValueOrDefault("stability", 0);
            reward += stabilityGain * 2.0;

            double bugReduction = prevState.GetValueOrDefault("grid_bugs", 0) - 
                                  newState.GetValueOrDefault("grid_bugs", 0);
            reward += bugReduction * 0.1;

            double coopGain = newState.GetValueOrDefault("cell_cooperation", 0) - 
                              prevState.GetValueOrDefault("cell_cooperation", 0);
            reward += coopGain * 1.5;

            double energyGain = newState.GetValueOrDefault("energy_level", 0) - 
                                prevState.GetValueOrDefault("energy_level", 0);
            reward += energyGain * 1.0;

            double resistanceIncrease = newState.GetValueOrDefault("user_resistance", 0) - 
                                        prevState.GetValueOrDefault("user_resistance", 0);
            reward -= resistanceIncrease * 0.5;

            string actionLower = action.ToLower();
            if (actionLower.Contains("calculate") || actionLower.Contains("processor") || actionLower.Contains("fibonacci"))
                reward += 0.2;

            if (actionLower.Contains("optimize") || actionLower.Contains("improve") || actionLower.Contains("boost"))
                reward += 0.1;

            if ((actionLower.Contains("destroy") || actionLower.Contains("eliminate")) && !actionLower.Contains("bug"))
            {
                double programReduction = (prevState.GetValueOrDefault("user_programs", 0) + 
                                          prevState.GetValueOrDefault("mcp_programs", 0)) -
                                         (newState.GetValueOrDefault("user_programs", 0) + 
                                          newState.GetValueOrDefault("mcp_programs", 0));
                if (programReduction > 2)
                    reward -= 0.3;
            }

            return Math.Max(-1.0, Math.Min(1.0, reward));
        }

        private Dictionary<string, double> CompressState(Dictionary<string, double> state)
        {
            var compressed = new Dictionary<string, double>();
            string[] importantKeys = {
                "loop_efficiency", "stability", "calculation_rate",
                "cell_cooperation", "grid_bugs", "user_resistance",
                "energy_level", "optimal_state", "entropy"
            };

            foreach (string key in importantKeys)
            {
                if (state.ContainsKey(key))
                {
                    compressed[key] = Math.Round(state[key], 2);
                }
            }

            return compressed;
        }

        private string IdentifyScenario(string action)
        {
            string actionStr = action.ToLower();

            if (actionStr.Contains("optimize") || actionStr.Contains("efficiency") || actionStr.Contains("boost"))
                return "efficiency_optimization";
            else if (actionStr.Contains("bug") || actionStr.Contains("quarantine") || actionStr.Contains("contain"))
                return "bug_containment";
            else if (actionStr.Contains("cooperate") || actionStr.Contains("allow") || actionStr.Contains("accept"))
                return "user_cooperation";
            else if (actionStr.Contains("calculate") || actionStr.Contains("processor") || actionStr.Contains("fibonacci"))
                return "calculation_boost";
            else if (actionStr.Contains("stabilize") || actionStr.Contains("protect") || actionStr.Contains("defend"))
                return "stabilization";
            else if (actionStr.Contains("connect") || actionStr.Contains("stream") || actionStr.Contains("network"))
                return "connectivity_improvement";

            return "general";
        }

        public double GetSuccessRate()
        {
            int totalSuccess = ScenarioPerformance.Values.Sum(s => (int)s["success"]);
            int totalFailure = ScenarioPerformance.Values.Sum(s => (int)s["failure"]);
            int total = totalSuccess + totalFailure;

            return total > 0 ? totalSuccess / (double)total : 0;
        }

        public Dictionary<string, object> GetLearningReport()
        {
            double successRate = GetSuccessRate();

            double avgReward10 = _rewardHistory.TakeLast(10).Any() ? _rewardHistory.TakeLast(10).Average() : 0;
            double avgReward50 = _rewardHistory.TakeLast(50).Any() ? _rewardHistory.TakeLast(50).Average() : 0;
            
            double avgStateVisits = _stateVisits.Values.Any() ? _stateVisits.Values.Average() : 0;

            var scenarioPerformance = new Dictionary<string, object>();
            foreach (var kvp in ScenarioPerformance)
            {
                int success = (int)kvp.Value["success"];
                int failure = (int)kvp.Value["failure"];
                int total = success + failure;
                scenarioPerformance[kvp.Key] = new Dictionary<string, object>
                {
                    ["success"] = success,
                    ["failure"] = failure,
                    ["success_rate"] = total > 0 ? Math.Round(success / (double)total * 100, 1) : 0,
                    ["total_reward"] = Math.Round((double)kvp.Value["total_reward"], 2)
                };
            }

            return new Dictionary<string, object>
            {
                ["training_summary"] = new Dictionary<string, object>
                {
                    ["training_steps"] = TrainingSteps,
                    ["episode_count"] = EpisodeCount,
                    ["total_reward"] = Math.Round(_totalReward, 2),
                    ["success_rate"] = Math.Round(successRate * 100, 1),
                    ["exploration_rate"] = Math.Round(ExplorationRate, 3),
                    ["learning_rate"] = Math.Round(LearningRate, 3)
                },
                ["personality_traits"] = PersonalityTraits.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value, 3)),
                ["scenario_performance"] = scenarioPerformance,
                ["recent_performance"] = new Dictionary<string, object>
                {
                    ["avg_reward_10"] = Math.Round(avgReward10, 3),
                    ["avg_reward_50"] = Math.Round(avgReward50, 3),
                    ["exploration_level"] = ExplorationRate > 0.3 ? "High" : (ExplorationRate > 0.1 ? "Medium" : "Low")
                },
                ["learning_state"] = new Dictionary<string, object>
                {
                    ["q_table_size"] = _qTable.Values.Sum(d => d.Count),
                    ["unique_states"] = _qTable.Count,
                    ["avg_state_visits"] = Math.Round(avgStateVisits, 2)
                },
                ["total_experiences"] = TrainingSteps,
                ["success_rate"] = successRate,
                ["learning_rate"] = LearningRate,
                ["exploration_rate"] = ExplorationRate
            };
        }

        public Dictionary<string, object>? SuggestOptimalAction(Dictionary<string, double> state)
        {
            string stateKey = string.Join(",", state.Select(kvp => $"{kvp.Key}:{kvp.Value:F2}"));

            if (!_qTable.ContainsKey(stateKey) || !_qTable[stateKey].Any())
                return null;

            var bestAction = _qTable[stateKey].OrderByDescending(kvp => kvp.Value).First();
            double bestValue = bestAction.Value;

            return new Dictionary<string, object>
            {
                ["action"] = bestAction.Key,
                ["confidence"] = Math.Min(1.0, bestValue / 10.0),
                ["state_visits"] = _stateVisits.GetValueOrDefault(stateKey, 0)
            };
        }

        public double GetDecisionModifier(string trait, double baseValue)
        {
            if (PersonalityTraits.ContainsKey(trait))
                return baseValue * (0.5 + PersonalityTraits[trait]);
            return baseValue;
        }

        public void ResetTraining()
        {
            Console.WriteLine("Resetting training data (keeping learned personality)...");
            var oldTraits = new Dictionary<string, double>(PersonalityTraits);
            
            // Reinitialize
            _experienceBuffer.Clear();
            _actionHistory.Clear();
            _qTable.Clear();
            _stateVisits.Clear();
            _rewardHistory.Clear();
            _punishmentHistory.Clear();
            
            LearningRate = 0.15;
            ExplorationRate = 0.35;
            _temperature = 1.0;
            TrainingSteps = 0;
            _totalReward = 0.0;
            EpisodeCount = 0;

            // Restore personality
            PersonalityTraits = oldTraits;
            
            Console.WriteLine("Training reset complete. Personality traits preserved.");
        }

        /// <summary>
        /// Persist the current personality traits to disk.
        /// </summary>
        public void SavePersonality()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(PersonalityTraits, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_personalityFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save personality: {ex.Message}");
            }
        }

        /// <summary>
        /// Load personality traits from storage if available.
        /// </summary>
        public void LoadPersonality()
        {
            if (System.IO.File.Exists(_personalityFile))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_personalityFile);
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(json);
                    if (dict != null)
                        PersonalityTraits = dict;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load personality: {ex.Message}");
                }
            }
        }
    }
}