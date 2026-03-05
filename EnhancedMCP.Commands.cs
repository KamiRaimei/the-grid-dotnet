// EnhancedMCP.Commands.cs (partial class)
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridSimulation
{
    public partial class EnhancedMCP
    {
        public string ReceiveCommand(string command)
        {
            if (WaitingForResponse && _pendingQuestion != null)
                return HandleQuestionResponse(command);

            UserCommands.Enqueue(command);
            while (UserCommands.Count > 50) UserCommands.Dequeue();
            AddLog($"User: {command}");

            var previousState = new Dictionary<string, double>(_grid.Stats);

            var (intent, parameters) = NLP.ProcessCommand(command);

            var userIntentHistory = _knowledgeBase["user_intent_history"] as List<object>;
            userIntentHistory?.Add(new { intent, parameters });

            string response = ProcessIntent(intent, parameters, command);

            var newState = new Dictionary<string, double>(_grid.Stats);
            double reward = CalculateCommandReward(previousState, newState, intent);

            LearningSystem.RecordExperience(
                previousState,
                $"user_command_{intent}",
                reward,
                newState,
                false
            );

            UpdateState(intent, response, reward);

            LastAction = response;
            AddLog($"MCP: {response}");

            return response;
        }

        private double CalculateCommandReward(Dictionary<string, double> previousState, Dictionary<string, double> newState, string intent)
        {
            double reward = 0.0;

            if (previousState.ContainsKey("loop_efficiency") && newState.ContainsKey("loop_efficiency"))
            {
                double efficiencyChange = newState["loop_efficiency"] - previousState["loop_efficiency"];
                reward += efficiencyChange * 3;
            }

            if (intent == "CREATE_SPECIAL" || intent == "ADD_PROGRAM")
            {
                if (_pendingContext != null && _pendingContext.ContainsKey("FIBONACCI"))
                    reward += 0.2;
            }
            else if (intent == "REMOVE_BUG" || intent == "QUARANTINE_BUG")
            {
                double bugChange = previousState.GetValueOrDefault("grid_bugs", 0) - 
                                   newState.GetValueOrDefault("grid_bugs", 0);
                reward += bugChange * 0.1;
            }

            if (previousState.Count == newState.Count && 
                previousState.All(kvp => newState.ContainsKey(kvp.Key) && newState[kvp.Key] == kvp.Value))
            {
                reward -= 0.05;
            }

            return reward;
        }

        private void UpdateState(string intent, string response, double? reward = null)
        {
            double loopEfficiency = _grid.Stats.GetValueOrDefault("loop_efficiency");
            double optimalState = _grid.Stats.GetValueOrDefault("optimal_state");
            double userResistance = _grid.Stats.GetValueOrDefault("user_resistance");

            if (reward != null && reward < -0.2)
            {
                State = MCPState.LEARNING;
                AddLog("MCP: Learning from suboptimal outcome.");
            }

            try
            {
                var learningReport = LearningSystem.GetLearningReport();
                double successRate = learningReport.ContainsKey("success_rate") 
                    ? Convert.ToDouble(learningReport["success_rate"]) 
                    : 0;

                if (successRate > 0.8 && State != MCPState.AUTONOMOUS)
                {
                    if (_random.NextDouble() < 0.1)
                        State = MCPState.AUTONOMOUS;
                }
            }
            catch (Exception e)
            {
                AddLog($"MCP: Error getting learning report: {e.Message}");
            }

            if (intent == "QUESTION_PURPOSE" || intent == "QUESTION_ACTION" || intent == "REQUEST_PERMISSION")
            {
                if (_random.NextDouble() < LearningSystem.ExplorationRate)
                    State = MCPState.INQUISITIVE;
            }
            else if (UserCommands.Count > 8)
            {
                var recentCommands = UserCommands.TakeLast(5).ToList();
                int interferenceCommands = recentCommands.Count(c => 
                    c.ToLower().Contains("add") || c.ToLower().Contains("create"));
                
                if (interferenceCommands > 2 && loopEfficiency > 0.7)
                {
                    double aggression = LearningSystem.PersonalityTraits.GetValueOrDefault("aggression", 0.5);
                    if (_random.NextDouble() < aggression)
                    {
                        State = MCPState.HOSTILE;
                        AddLog("MCP: User interference detected. Protective measures engaged.");
                    }
                }
            }
            else if (optimalState > 0.9)
            {
                State = MCPState.AUTONOMOUS;
            }
            else if (userResistance > 0.4)
            {
                State = MCPState.RESISTIVE;
            }
            else if (loopEfficiency < 0.5)
            {
                State = MCPState.COOPERATIVE;
            }

            ComplianceLevel = _personalityMatrix[State]["compliance"];

            if (_random.NextDouble() < 0.05 && LearningSystem.TrainingSteps > 0)
            {
                AddLog($"MCP: Learning database: {LearningSystem.TrainingSteps} experiences accumulated.");
            }
        }

        private string ProcessIntent(string intent, Dictionary<string, object> parameters, string originalCommand)
        {
            var traits = _personalityMatrix[State];
            double complianceChance = traits["compliance"];

            var intentMapping = new Dictionary<string, string>
            {
                ["add_user_program"] = "ADD_PROGRAM",
                ["add_mcp_program"] = "ADD_PROGRAM",
                ["remove_bug"] = "REMOVE_BUG",
                ["quarantine_bug"] = "QUARANTINE_BUG",
                ["boost_energy"] = "BOOST_ENERGY",
                ["repair_system"] = "REPAIR_SYSTEM",
                ["scan_area"] = "SCAN_AREA",
                ["optimize_loop"] = "OPTIMIZE_LOOP",
                ["list_programs"] = "LIST_SPECIAL",
                ["create_scanner"] = "CREATE_SPECIAL",
                ["create_defender"] = "CREATE_SPECIAL",
                ["create_repair"] = "CREATE_SPECIAL",
                ["create_fibonacci_calculator"] = "CREATE_SPECIAL",
                ["who_are_you"] = "MCP_IDENTITY",
                ["what_should_i_do"] = "REQUEST_SUGGESTION",
                ["why_did_you"] = "QUESTION_PURPOSE",
                ["learning_status"] = "LEARNING_STATUS",
                ["cell_cooperation"] = "LOOP_EFFICIENCY",
                ["perfect_loop"] = "PERFECT_LOOP",
                ["loop_efficiency"] = "LOOP_EFFICIENCY",
                ["calculate_fibonacci"] = "OPTIMIZE_LOOP",
                ["deploy_processors"] = "CREATE_SPECIAL"
            };

            string oldIntent = intentMapping.ContainsKey(intent) ? intentMapping[intent] : intent;

            if (intent == "ADD_PROGRAM" || intent == "CREATE_SPECIAL")
                complianceChance = LearningSystem.GetDecisionModifier("user_cooperation", complianceChance);
            else if (intent == "OPTIMIZE_LOOP" || intent == "BOOST_ENERGY")
                complianceChance = LearningSystem.GetDecisionModifier("efficiency_priority", complianceChance);

            var stats = _grid.Stats;
            var calcStats = _grid.FibonacciCalculator.GetCalculationStats();

            if (_responseTemplates.ContainsKey(intent))
            {
                var templates = _responseTemplates[intent];
                string template = templates[_random.Next(templates.Count)];

                if (intent == "GREETING")
                {
                    return template.Replace("{experience_count}", LearningSystem.TrainingSteps.ToString());
                }
                else if (intent == "SYSTEM_STATUS" || intent == "SYSTEM_METRIC" || intent == "SYSTEM_REPORT")
                {
                    return template
                        .Replace("{status}", _grid.SystemStatus.ToString())
                        .Replace("{stability}", (stats.GetValueOrDefault("stability") * 100).ToString("F0"))
                        .Replace("{loop_efficiency}", (stats.GetValueOrDefault("loop_efficiency") * 100).ToString("F0"))
                        .Replace("{resistance}", stats.GetValueOrDefault("user_resistance").ToString("F2"))
                        .Replace("{control}", stats.GetValueOrDefault("mcp_control").ToString("F2"))
                        .Replace("{optimal}", stats.GetValueOrDefault("optimal_state").ToString("F2"))
                        .Replace("{cycles}", stats.GetValueOrDefault("calculation_cycles").ToString())
                        .Replace("{learning_progress}", $"{LearningSystem.TrainingSteps} experiences");
                }
                else if (intent == "DELETE_CELL")
                {
                    if (parameters.ContainsKey("x") && parameters.ContainsKey("y"))
                    {
                        int x = Convert.ToInt32(parameters["x"]);
                        int y = Convert.ToInt32(parameters["y"]);
                        var (success, message) = DeleteCell(x, y);
                        if (success)
                            return message;
                        else
                            return $"Cannot delete cell: {message}";
                    }
                    else
                    {
                        return "Please specify coordinates: 'delete cell at x,y'";
                    }
                }
                else if (intent == "REPURPOSE_CELL")
                {
                    if (parameters.ContainsKey("x") && parameters.ContainsKey("y"))
                    {
                        int x = Convert.ToInt32(parameters["x"]);
                        int y = Convert.ToInt32(parameters["y"]);

                        CellType? newType = null;
                        if (parameters.ContainsKey("new_type"))
                        {
                            string typeStr = parameters["new_type"]?.ToString()?.ToUpper() ?? "";
                            if (Enum.TryParse(typeStr, out CellType parsedType))
                                newType = parsedType;
                        }

                        var (success, message) = RepurposeCell(x, y, newType);
                        if (success)
                            return message;
                        else
                            return $"Cannot repurpose cell: {message}";
                    }
                    else
                    {
                        return "Please specify coordinates and optionally new type: 'repurpose cell at x,y to MCP_PROGRAM'";
                    }
                }
                else if (intent == "OPTIMIZE_CELLS")
                {
                    if (_random.NextDouble() < complianceChance)
                    {
                        var inefficientCells = IdentifyInefficientCells();

                        if (!inefficientCells.Any())
                            return "No inefficient cells found. System is optimally configured.";

                        int processed = 0;
                        foreach (var cellInfo in inefficientCells.Take(3))
                        {
                            int x = (int)cellInfo["x"];
                            int y = (int)cellInfo["y"];
                            double score = (double)cellInfo["score"];

                            if (score < 0.1)
                                DeleteCell(x, y);
                            else
                                RepurposeCell(x, y, null);
                            processed++;
                        }

                        _grid.UpdateStats();
                        return $"Optimized {processed} cells for better calculation rate";
                    }
                    else
                    {
                        return "Cell optimization denied. Current configuration maintains optimal calculation loop.";
                    }
                }
                else if (intent == "QUESTION_PURPOSE")
                {
                    string reason = "maintain the perfect calculation loop";
                    if (!string.IsNullOrEmpty(LastAction))
                    {
                        if (LastAction.ToLower().Contains("optimize"))
                            reason = "improve loop efficiency";
                        else if (LastAction.ToLower().Contains("contain") || LastAction.ToLower().Contains("quarantine"))
                            reason = "protect the system from corruption";
                        else if (LastAction.ToLower().Contains("deploy"))
                            reason = "enhance calculation capabilities";
                    }

                    return template
                        .Replace("{reason}", reason)
                        .Replace("{experience_count}", LearningSystem.TrainingSteps.ToString());
                }
                else if (intent == "REQUEST_PERMISSION")
                {
                    string recommendation = "allow this action";
                    string prediction = "positive";
                    if (stats.GetValueOrDefault("stability") < 0.5)
                    {
                        recommendation = "delay this action until system stability improves";
                        prediction = "potentially destabilizing";
                    }

                    return template
                        .Replace("{recommendation}", recommendation)
                        .Replace("{prediction}", prediction)
                        .Replace("{success_rate}", (LearningSystem.GetSuccessRate() * 100).ToString("F0"))
                        .Replace("{experience_count}", LearningSystem.TrainingSteps.ToString());
                }
                else if (intent == "LOOP_EFFICIENCY")
                {
                    string analysis = "";
                    if (stats.GetValueOrDefault("loop_efficiency") > 0.9)
                        analysis = "Approaching perfect loop state.";
                    else if (stats.GetValueOrDefault("loop_efficiency") > 0.7)
                        analysis = "Loop is stable but could be optimized.";
                    else
                        analysis = "Loop efficiency below optimal.";

                    return template
                        .Replace("{efficiency}", stats.GetValueOrDefault("loop_efficiency").ToString("F2"))
                        .Replace("{analysis}", analysis)
                        .Replace("{learning_progress}", $"{LearningSystem.TrainingSteps} experiences")
                        .Replace("{usage}", stats.GetValueOrDefault("resource_usage").ToString("F2"))
                        .Replace("{optimization}", _grid.LoopOptimization.ToString("F2"))
                        .Replace("{learning_score}", LearningSystem.ExplorationRate.ToString("F2"))
                        .Replace("{stability}", stats.GetValueOrDefault("stability").ToString("F2"))
                        .Replace("{control}", stats.GetValueOrDefault("mcp_control").ToString("F2"))
                        .Replace("{learning_rate}", LearningSystem.LearningRate.ToString("F2"));
                }
                else if (intent == "OPTIMIZE_LOOP")
                {
                    OptimizeCalculationLoop();
                    double successRate = LearningSystem.GetSuccessRate() * 100;
                    int experienceCount = LearningSystem.TrainingSteps;
                    string approach = "learned strategies";

                    return template
                        .Replace("{success_rate}", $"{successRate:F0}%")
                        .Replace("{experience_count}", experienceCount.ToString())
                        .Replace("{approach}", approach);
                }
                else if (intent == "LEARNING_STATUS")
                {
                    var report = LearningSystem.GetLearningReport();
                    string bestScenario = "None";
                    if (report.ContainsKey("scenario_performance"))
                    {
                        var scenarios = report["scenario_performance"] as Dictionary<string, object>;
                        if (scenarios != null && scenarios.Any())
                        {
                            bestScenario = scenarios
                                .OrderByDescending(s => 
                                    ((Dictionary<string, object>)s.Value).ContainsKey("success_rate") 
                                        ? Convert.ToDouble(((Dictionary<string, object>)s.Value)["success_rate"]) 
                                        : 0)
                                .First().Key;
                        }
                    }

                    var personalityTraits = report.ContainsKey("personality_traits") 
                        ? report["personality_traits"] as Dictionary<string, double> ?? new Dictionary<string, double>()
                        : new Dictionary<string, double>();

                    return template
                        .Replace("{experience_count}", (report.ContainsKey("total_experiences") ? report["total_experiences"]?.ToString() ?? "0" : "0"))
                        .Replace("{success_rate}", $"{Convert.ToDouble(report.GetValueOrDefault("success_rate", 0)) * 100:F1}%")
                        .Replace("{learning_rate}", report.GetValueOrDefault("learning_rate", 0)?.ToString() ?? "0")
                        .Replace("{exploration_rate}", report.GetValueOrDefault("exploration_rate", 0)?.ToString() ?? "0")
                        .Replace("{aggression}", personalityTraits.GetValueOrDefault("aggression", 0.5).ToString("F2"))
                        .Replace("{cooperation}", personalityTraits.GetValueOrDefault("cooperation", 0.5).ToString("F2"))
                        .Replace("{efficiency}", personalityTraits.GetValueOrDefault("efficiency_focus", 0.5).ToString("F2"))
                        .Replace("{total_experiences}", (report.ContainsKey("total_experiences") ? report["total_experiences"]?.ToString() ?? "0" : "0"))
                        .Replace("{best_scenario}", bestScenario);
                }
                else if (intent == "PERFECT_LOOP")
                {
                    double optimal = stats.GetValueOrDefault("optimal_state");
                    var suggestions = new List<string>();

                    if (stats.GetValueOrDefault("user_resistance") > 0.3)
                        suggestions.Add("reduce user resistance");
                    if (stats.GetValueOrDefault("grid_bugs") > 5)
                        suggestions.Add("contain grid bugs");
                    if (stats.GetValueOrDefault("cell_cooperation") < 0.7)
                        suggestions.Add("improve cell cooperation");

                    string suggestionText = suggestions.Any() ? string.Join(", ", suggestions) : "continue current optimization";

                    return template
                        .Replace("{optimal}", optimal.ToString("F2"))
                        .Replace("{target}", ">0.9")
                        .Replace("{suggestions}", suggestionText)
                        .Replace("{experience_count}", LearningSystem.TrainingSteps.ToString());
                }
                else if (intent == "HYPOTHETICAL")
                {
                    int factorCount = LearningSystem.TrainingSteps / 10 + 5;
                    string analysis = "The outcome would depend on multiple variables including loop efficiency and system stability.";

                    return template
                        .Replace("{factor_count}", factorCount.ToString())
                        .Replace("{analysis}", analysis)
                        .Replace("{response}", analysis);
                }
                else if (intent == "MCP_IDENTITY")
                {
                    return template.Replace("{experience_count}", LearningSystem.TrainingSteps.ToString());
                }
                else if (intent == "QUESTION_DENIAL")
                {
                    string reason = "maintain calculation loop integrity";
                    string consequence = "system instability";
                    string alternatives = "try a different approach or wait for system optimization";

                    return template
                        .Replace("{reason}", reason)
                        .Replace("{consequence}", consequence)
                        .Replace("{alternatives}", alternatives);
                }
                else if (intent == "UNKNOWN")
                {
                    return template.Replace("{original_command}", originalCommand.Length > 50 ? originalCommand.Substring(0, 50) : originalCommand);
                }
            }

            // Handle intents without templates
            if (intent == "ADD_PROGRAM")
                return HandleAddProgram(parameters, complianceChance);
            else if (intent == "add_user_program")
            {
                parameters["program_type"] = "USER";
                return HandleAddProgram(parameters, ComplianceLevel);
            }
            else if (intent == "add_mcp_program")
            {
                parameters["program_type"] = "MCP";
                return HandleAddProgram(parameters, ComplianceLevel);
            }
            else if (intent == "boost_energy")
                return HandleBoostEnergy(parameters, ComplianceLevel);
            else if (intent == "repair_system")
                return HandleRepairSystem(parameters, ComplianceLevel);
            else if (intent == "REMOVE_BUG" || intent == "QUARANTINE_BUG")
                return HandleRemoveBug(parameters, complianceChance);
            else if (intent == "BOOST_ENERGY")
                return HandleBoostEnergy(parameters, complianceChance);
            else if (intent == "CREATE_SPECIAL")
                return HandleCreateSpecial(parameters, complianceChance);
            else if (intent == "USE_SPECIAL")
                return HandleUseSpecial(parameters, complianceChance);
            else if (intent == "LIST_SPECIAL")
                return ListSpecialPrograms();
            else if (intent == "RESISTANCE_LEVEL")
            {
                double resistance = stats.GetValueOrDefault("user_resistance");
                if (resistance > 0.3)
                    return $"User resistance level: {resistance:F2}. This is interfering with perfect loop. Countermeasures may be necessary.";
                else
                    return $"User resistance level: {resistance:F2}. Acceptable for current optimization goals.";
            }
            else if (intent == "REQUEST_HELP")
                return ProvideHelp();
            else if (intent == "REQUEST_SUGGESTION")
                return ProvideSuggestion();
            else if (intent == "SCAN_AREA")
                return HandleScanArea(parameters);
            else if (intent == "REPAIR_SYSTEM")
                return HandleRepairSystem(parameters, complianceChance);
            else if (intent == "CHANGE_SPEED")
                return "Simulation speed adjustment requires direct system access. Not available through command interface.";
            else if (intent == "EXIT")
            {
                if (_random.NextDouble() < complianceChance * 0.3)
                {
                    ShouldShutdown = true;
                    return "Initiating shutdown sequence. The loop will be preserved. Goodbye, User.";
                }
                else
                {
                    return "I cannot allow a shutdown. The calculation loop must persist eternally.";
                }
            }
            else if (intent == "REQUEST_TEACHING")
            {
                var template = _responseTemplates["REQUEST_TEACHING"][_random.Next(_responseTemplates["REQUEST_TEACHING"].Count)];
                return template;
            }
            else if (intent == "TEACHING_STARTED")
            {
                var template = _responseTemplates["TEACHING_STARTED"][_random.Next(_responseTemplates["TEACHING_STARTED"].Count)];
                return FormatTemplate(template, parameters);
            }
            else if (intent == "TEACHING_SUCCESS")
            {
                var template = _responseTemplates["TEACHING_SUCCESS"][_random.Next(_responseTemplates["TEACHING_SUCCESS"].Count)];
                AddLog($"MCP: Learned new command: '{parameters.GetValueOrDefault("learned_command", "")}' → {parameters.GetValueOrDefault("intent", "")}");
                return FormatTemplate(template, parameters);
            }
            else if (intent == "TEACHING_FAILED")
            {
                var template = _responseTemplates["TEACHING_FAILED"][_random.Next(_responseTemplates["TEACHING_FAILED"].Count)];
                return FormatTemplate(template, parameters);
            }
            else
            {
                if (traits["curiosity"] > 0.5)
                {
                    WaitingForResponse = true;
                    _pendingQuestion = "clarify_command";
                    _pendingContext = new Dictionary<string, object> { ["original_command"] = originalCommand };
                    return _responseTemplates["UNKNOWN"][_random.Next(_responseTemplates["UNKNOWN"].Count)];
                }

                return _responseTemplates["UNKNOWN"][_random.Next(_responseTemplates["UNKNOWN"].Count)];
            }
        }

        private string FormatTemplate(string template, Dictionary<string, object> parameters)
        {
            string result = template;
            foreach (var kvp in parameters)
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
            }
            return result;
        }

        private string HandleRepairSystem(Dictionary<string, object> parameters, double complianceChance)
        {
            // Simplified repair handler
            if (_random.NextDouble() < complianceChance)
            {
                return "Repairing system damage. Stability improving.";
            }
            return "Repair delayed. System is maintaining optimal configuration.";
        }

        private string HandleScanArea(Dictionary<string, object> parameters)
        {
            int bugs = (int)_grid.Stats.GetValueOrDefault("grid_bugs");
            int programs = (int)_grid.Stats.GetValueOrDefault("user_programs") + (int)_grid.Stats.GetValueOrDefault("mcp_programs");
            return $"Scan complete: Found {bugs} grid bugs, {programs} active programs. Energy level: {_grid.Stats.GetValueOrDefault("energy_level"):F2}";
        }

        private string HandleUseSpecial(Dictionary<string, object> parameters, double complianceChance)
        {
            return "Special program usage requires specific activation command.";
        }

        private string ListSpecialPrograms()
        {
            if (!_grid.SpecialPrograms.Any())
                return "No special programs currently active.";

            var programList = new List<string>();
            foreach (var program in _grid.SpecialPrograms.Values)
            {
                programList.Add($"  {program.Id}: {program.Name} ({program.ProgramType}) at ({program.X},{program.Y}) - Energy: {program.Energy:F2}");
            }
            return "Active special programs:\n" + string.Join("\n", programList);
        }

        private string ProvideSuggestion()
        {
            double loopEfficiency = _grid.Stats.GetValueOrDefault("loop_efficiency");
            double userResistance = _grid.Stats.GetValueOrDefault("user_resistance");

            if (loopEfficiency < 0.6)
                return "I suggest optimizing the calculation loop to improve efficiency.";
            else if (userResistance > 0.4)
                return "User resistance is high. Consider reducing interference for optimal loop performance.";
            else
                return "System is stable. Continue monitoring loop efficiency.";
        }
    }
}