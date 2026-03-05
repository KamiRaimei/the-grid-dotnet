// LearnableNaturalLanguageProcessor.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GridSimulation
{
    public class LearnableNaturalLanguageProcessor
    {
        private string _learnedPatternsFile = "mcp_learned_patterns.json";
        private Dictionary<string, List<string>> _baseIntents;
        private Dictionary<string, List<string>> _learnedIntents;
        private Queue<Dictionary<string, object>> _intentHistory;
        public bool LearningMode { get; private set; }
        private Dictionary<string, object>? _pendingTeaching;

        public int TotalCommandsProcessed { get; private set; }
        public int SuccessfulMatches { get; private set; }
        public int LearnedPatternCount => _learnedIntents.Count;

    public LearnableNaturalLanguageProcessor()
    {
        _baseIntents = InitializeBaseIntents();
        _learnedIntents = new Dictionary<string, List<string>>(); // Make sure this is initialized
        _intentHistory = new Queue<Dictionary<string, object>>();
        LearningMode = false;
        _pendingTeaching = null;
        TotalCommandsProcessed = 0;
        SuccessfulMatches = 0;
        
        // Load any previously learned intents
        LoadLearnedIntents();
    }

    // Add this method to load saved intents
    private void LoadLearnedIntents()
    {
        try
        {
            if (System.IO.File.Exists("mcp_learned_patterns.json"))
            {
                string json = System.IO.File.ReadAllText("mcp_learned_patterns.json");
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                if (data != null)
                {
                    _learnedIntents = data;
                    Console.WriteLine($"Loaded {_learnedIntents.Count} learned patterns");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load learned patterns: {ex.Message}");
        }
    }

        private Dictionary<string, List<string>> InitializeBaseIntents()
        {
            return new Dictionary<string, List<string>>
            {
                // System commands
                ["status"] = new List<string> { "status", "system status", "how is system", "system report", "check system" },
                ["help"] = new List<string> { "help", "what can i do", "commands", "show commands", "list commands" },
                ["exit"] = new List<string> { "exit", "quit", "shutdown", "leave", "goodbye" },

                // Program management
                ["add_user_program"] = new List<string> { "add user program", "create user program", "make user program", "spawn user program" },
                ["add_mcp_program"] = new List<string> { "add mcp program", "create mcp program", "make mcp program" },
                ["remove_bug"] = new List<string> { "remove bug", "delete bug", "eliminate bug", "kill bug" },
                ["quarantine_bug"] = new List<string> { "quarantine bug", "isolate bug", "contain bug" },

                // System operations
                ["boost_energy"] = new List<string> { "boost energy", "add energy", "increase power", "power up" },
                ["repair_system"] = new List<string> { "repair system", "fix system", "restore system", "heal system" },
                ["scan_area"] = new List<string> { "scan", "scan area", "analyze", "examine" },
                ["optimize_loop"] = new List<string> { "optimize loop", "improve efficiency", "enhance loop", "optimize calculation" },

                // Special programs
                ["list_programs"] = new List<string> { "list programs", "show programs", "view programs", "special programs" },
                ["create_scanner"] = new List<string> { "create scanner", "build scanner", "make scanner", "deploy scanner" },
                ["create_defender"] = new List<string> { "create defender", "build defender", "make defender" },
                ["create_repair"] = new List<string> { "create repair program", "build repair", "make repair" },
                ["create_fibonacci_calculator"] = new List<string> { "create fibonacci calculator", "build fibonacci", "deploy calculator" },

                // MCP interaction
                ["who_are_you"] = new List<string> { "who are you", "what are you", "identify yourself", "your name" },
                ["what_should_i_do"] = new List<string> { "what should i do", "what do you suggest", "recommend something", "advise me" },
                ["why_did_you"] = new List<string> { "why did you", "why did mcp", "explain that action", "why that action" },

                // Cell repurpose
                ["delete_cell"] = new List<string> { "delete cell", "remove cell", "erase cell", "clear cell" },
                ["repurpose_cell"] = new List<string> { "repurpose cell", "convert cell", "transform cell", "change cell type" },
                ["optimize_cells"] = new List<string> { "optimize cells", "improve cells", "enhance cells", "tune cells for calculation" },

                // Learning specific
                ["learning_status"] = new List<string> { "learning status", "mcp learning", "personality status", "learning report" },
                ["cell_cooperation"] = new List<string> { "cell cooperation", "cooperation level", "cell collaboration" },
                ["perfect_loop"] = new List<string> { "perfect loop", "optimal state", "ideal loop", "perfect calculation" },
                ["loop_efficiency"] = new List<string> { "loop efficiency", "calculation efficiency", "efficiency status" },

                // Fibonacci calculation
                ["calculate_fibonacci"] = new List<string> { "calculate fibonacci", "compute fibonacci", "next fibonacci", "fibonacci number" },
                ["deploy_processors"] = new List<string> { "deploy processors", "add processors", "create processors", "fibonacci processors" },
            };
        }

        public (string intent, Dictionary<string, object> parameters) ProcessCommand(string command)
        {
            TotalCommandsProcessed++;

            command = command.Trim();
            if (string.IsNullOrEmpty(command))
                return ("UNKNOWN", new Dictionary<string, object>());

            if (LearningMode && _pendingTeaching != null)
                return HandleTeachingResponse(command);

            if (command.ToLower().StartsWith("teach:"))
                return HandleTeachingCommand(command);

            var (intent, matchedPattern) = FindBestMatch(command);

            if (intent != null)
            {
                SuccessfulMatches++;
                _intentHistory.Enqueue(new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.Now.ToString("o"),
                    ["command"] = command,
                    ["intent"] = intent,
                    ["pattern"] = matchedPattern
                });
                while (_intentHistory.Count > 100) _intentHistory.Dequeue();

                var parameters = ExtractParameters(command, intent);
                parameters["original_command"] = command;

                return (intent, parameters);
            }
            else
            {
                _intentHistory.Enqueue(new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.Now.ToString("o"),
                    ["command"] = command,
                    ["intent"] = "UNKNOWN",
                    ["pattern"] = null
                });
                while (_intentHistory.Count > 100) _intentHistory.Dequeue();

                if (command.Split(' ').Length >= 2)
                    return ("REQUEST_TEACHING", new Dictionary<string, object> { ["original_command"] = command });
                else
                    return ("UNKNOWN", new Dictionary<string, object> { ["original_command"] = command });
            }
        }

        private (string? intent, string? pattern) FindBestMatch(string command)
        {
            string normalizedCommand = NormalizeInput(command);

            // Check learned intents first
            foreach (var kvp in _learnedIntents)
            {
                foreach (string pattern in kvp.Value)
                {
                    if (MatchesPattern(normalizedCommand, pattern))
                        return (kvp.Key, pattern);
                }
            }

            // Check base intents
            foreach (var kvp in _baseIntents)
            {
                foreach (string pattern in kvp.Value)
                {
                    if (MatchesPattern(normalizedCommand, pattern))
                        return (kvp.Key, pattern);
                }
            }

            // Try fuzzy matching for learned intents
            foreach (var kvp in _learnedIntents)
            {
                foreach (string pattern in kvp.Value)
                {
                    double similarity = CalculateSimilarity(normalizedCommand, pattern);
                    if (similarity > 0.7)
                        return (kvp.Key, pattern);
                }
            }

            // Try fuzzy matching for base intents
            string? bestIntent = null;
            string? bestPattern = null;
            double bestSimilarity = 0.5;

            foreach (var kvp in _baseIntents)
            {
                foreach (string pattern in kvp.Value)
                {
                    double similarity = CalculateSimilarity(normalizedCommand, pattern);
                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestIntent = kvp.Key;
                        bestPattern = pattern;
                    }
                }
            }

            if (bestIntent != null)
                return (bestIntent, bestPattern);

            return (null, null);
        }

        private bool MatchesPattern(string command, string pattern)
        {
            string[] patternWords = pattern.ToLower().Split(' ');
            string[] commandWords = command.ToLower().Split(' ');

            foreach (string word in patternWords)
            {
                if (!command.Contains(word))
                    return false;
            }

            double matchRatio = patternWords.Length / (double)Math.Max(commandWords.Length, 1);
            return matchRatio > 0.3;
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            var words1 = new HashSet<string>(text1.Split(' '));
            var words2 = new HashSet<string>(text2.Split(' '));

            if (words1.Count == 0 || words2.Count == 0)
                return 0;

            int intersection = words1.Intersect(words2).Count();
            int union = words1.Union(words2).Count();

            return intersection / (double)union;
        }

        private string NormalizeInput(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            text = text.ToLower().Trim();
            text = Regex.Replace(text, @"\s+", " ");

            string[] fillerWords = { "please", "could you", "would you", "can you", "maybe", "i want to", "i need to" };
            foreach (string word in fillerWords)
            {
                string pattern = @"^\s*" + Regex.Escape(word) + @"\s+";
                text = Regex.Replace(text, pattern, "");
            }

            return text.Trim();
        }

        private Dictionary<string, object> ExtractParameters(string command, string intent)
        {
            var parameters = new Dictionary<string, object>();
            string commandLower = command.ToLower();

            // Location extraction
            var locationMatch = Regex.Match(commandLower, @"(\d+)\s*,\s*(\d+)");
            if (locationMatch.Success)
            {
                parameters["x"] = int.Parse(locationMatch.Groups[1].Value);
                parameters["y"] = int.Parse(locationMatch.Groups[2].Value);
            }

            // Program type extraction
            if (intent == "add_user_program" || intent == "add_mcp_program" || intent == "ADD_PROGRAM")
            {
                if (commandLower.Contains("user"))
                    parameters["program_type"] = "USER";
                else if (commandLower.Contains("mcp"))
                    parameters["program_type"] = "MCP";
            }

            // Cell repurpose
            if (intent == "delete_cell" || intent == "repurpose_cell" || intent == "DELETE_CELL" || intent == "REPURPOSE_CELL")
            {
                if (commandLower.Contains("to"))
                {
                    string[] parts = commandLower.Split(new[] { "to" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        string newType = parts[1].Trim().ToUpper();
                        parameters["new_type"] = newType;
                    }
                }
            }

            // Special program type
            if (intent == "create_scanner" || intent == "create_defender" || intent == "create_repair" || 
                intent == "create_fibonacci_calculator" || intent == "CREATE_SPECIAL")
            {
                if (commandLower.Contains("scanner"))
                    parameters["special_type"] = "SCANNER";
                else if (commandLower.Contains("defender"))
                    parameters["special_type"] = "DEFENDER";
                else if (commandLower.Contains("repair"))
                    parameters["special_type"] = "REPAIR";
                else if (commandLower.Contains("fibonacci") || commandLower.Contains("calculator"))
                    parameters["special_type"] = "FIBONACCI_CALCULATOR";
            }

            // Name extraction
            var nameMatch = Regex.Match(commandLower, @"named?\s+[""']?([^""'\s]+)[""']?");
            if (nameMatch.Success)
                parameters["name"] = nameMatch.Groups[1].Value;

            // Scope extraction
            if (commandLower.Contains("all"))
                parameters["scope"] = "ALL";
            else if (commandLower.Contains("area") || commandLower.Contains("region"))
                parameters["scope"] = "AREA";

            return parameters;
        }

        private (string intent, Dictionary<string, object> parameters) HandleTeachingCommand(string command)
        {
            command = command.Substring(6).Trim();

            if (command.Contains(" means "))
            {
                string[] parts = command.Split(new[] { " means " }, StringSplitOptions.None);
                string userCommand = parts[0].Trim();
                string intent = parts[1].Trim().ToUpper();

                bool success = TeachNewCommand(userCommand, intent);

                if (success)
                {
                    return ("TEACHING_SUCCESS", new Dictionary<string, object>
                    {
                        ["original_command"] = command,
                        ["learned_command"] = userCommand,
                        ["intent"] = intent,
                        ["message"] = $"Learned: '{userCommand}' → {intent}"
                    });
                }
                else
                {
                    return ("TEACHING_FAILED", new Dictionary<string, object>
                    {
                        ["original_command"] = command,
                        ["message"] = "Could not learn that command. It might already exist."
                    });
                }
            }
            else if (command.Contains(" as "))
            {
                string[] parts = command.Split(new[] { " as " }, StringSplitOptions.None);
                string userCommand = parts[0].Trim();
                string intent = parts[1].Trim().ToUpper();

                bool success = TeachNewCommand(userCommand, intent);

                if (success)
                {
                    return ("TEACHING_SUCCESS", new Dictionary<string, object>
                    {
                        ["original_command"] = command,
                        ["learned_command"] = userCommand,
                        ["intent"] = intent,
                        ["message"] = $"Learned: '{userCommand}' → {intent}"
                    });
                }
                else
                {
                    return ("TEACHING_FAILED", new Dictionary<string, object>
                    {
                        ["original_command"] = command,
                        ["message"] = "Could not learn that command. It might already exist."
                    });
                }
            }
            else
            {
                LearningMode = true;
                _pendingTeaching = new Dictionary<string, object>
                {
                    ["user_command"] = command,
                    ["step"] = "ask_intent"
                };
                return ("TEACHING_STARTED", new Dictionary<string, object>
                {
                    ["original_command"] = command,
                    ["message"] = $"What intent should '{command}' map to? (e.g., ADD_PROGRAM, SCAN_AREA, etc.)"
                });
            }
        }

        private (string intent, Dictionary<string, object> parameters) HandleTeachingResponse(string response)
        {
            string step = _pendingTeaching?["step"]?.ToString() ?? "";

            if (step == "ask_intent")
            {
                string intent = response.Trim().ToUpper();
                string userCommand = _pendingTeaching?["user_command"]?.ToString() ?? "";

                bool success = TeachNewCommand(userCommand, intent);

                LearningMode = false;
                var teachingData = new Dictionary<string, object>(_pendingTeaching);
                _pendingTeaching = null;

                if (success)
                {
                    return ("TEACHING_SUCCESS", new Dictionary<string, object>
                    {
                        ["original_command"] = userCommand,
                        ["learned_command"] = userCommand,
                        ["intent"] = intent,
                        ["message"] = $"Successfully learned: '{userCommand}' → {intent}"
                    });
                }
                else
                {
                    return ("TEACHING_FAILED", new Dictionary<string, object>
                    {
                        ["original_command"] = userCommand,
                        ["message"] = $"Could not learn '{userCommand}' as {intent}. It might already exist."
                    });
                }
            }

            LearningMode = false;
            _pendingTeaching = null;
            return ("UNKNOWN", new Dictionary<string, object> { ["original_command"] = response });
        }

        public bool TeachNewCommand(string userInput, string intent, List<string>? examples = null)
        {
            intent = intent.ToUpper();

            if (!_learnedIntents.ContainsKey(intent))
                _learnedIntents[intent] = new List<string>();

            string normalizedInput = NormalizeInput(userInput);

            if (!_learnedIntents[intent].Contains(normalizedInput))
            {
                _learnedIntents[intent].Add(normalizedInput);

                if (examples != null)
                {
                    foreach (string example in examples)
                    {
                        string exampleNormalized = NormalizeInput(example);
                        if (!_learnedIntents[intent].Contains(exampleNormalized))
                            _learnedIntents[intent].Add(exampleNormalized);
                    }
                }

                return true;
            }

            return false;
        }

        public Dictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["total_commands"] = TotalCommandsProcessed,
                ["successful_matches"] = SuccessfulMatches,
                ["success_rate"] = SuccessfulMatches / (double)Math.Max(1, TotalCommandsProcessed),
                ["learned_patterns"] = _learnedIntents.Count,
                ["base_patterns"] = _baseIntents.Sum(kvp => kvp.Value.Count),
                ["recent_history"] = _intentHistory.TakeLast(5).ToList()
            };
        }

        public List<Dictionary<string, object>> GetSuggestedIntents(string command)
        {
            var suggestions = new List<Dictionary<string, object>>();
            string normalizedCommand = NormalizeInput(command);
            var commandWords = new HashSet<string>(normalizedCommand.Split(' '));

            var allIntents = new Dictionary<string, List<string>>();
            foreach (var kvp in _baseIntents)
                allIntents[kvp.Key] = kvp.Value;
            foreach (var kvp in _learnedIntents)
                allIntents[kvp.Key] = kvp.Value;

            foreach (var kvp in allIntents)
            {
                foreach (string pattern in kvp.Value)
                {
                    var patternWords = new HashSet<string>(pattern.Split(' '));
                    double similarity = commandWords.Intersect(patternWords).Count() / 
                                       (double)commandWords.Union(patternWords).Count();

                    if (similarity > 0.3)
                    {
                        suggestions.Add(new Dictionary<string, object>
                        {
                            ["intent"] = kvp.Key,
                            ["pattern"] = pattern,
                            ["similarity"] = similarity
                        });
                    }
                }
            }

            return suggestions.OrderByDescending(s => (double)s["similarity"]).Take(3).ToList();
        }
    }
}