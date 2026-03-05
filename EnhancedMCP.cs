// EnhancedMCP.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridSimulation
{
    public partial class EnhancedMCP
    {
        private TRONGrid _grid;
        public MCPState State { get; set; }
        public double ComplianceLevel { get; set; }
        public Queue<string> Log { get; private set; }
        public Queue<string> UserCommands { get; private set; }
        public string LastAction { get; set; }
        public LearnableNaturalLanguageProcessor NLP { get; private set; }
        private DateTime _lastAutonomousActionTime;
        public bool ShouldShutdown { get; set; }
        public MCPLearningSystem LearningSystem { get; private set; }
        private Dictionary<string, object>? _previousState;
        private string? _previousAction;
        private DateTime _episodeStartTime;
        private double _episodeReward;
        private int _consecutiveFailures;
        public bool WaitingForResponse { get; set; }
        private string? _pendingQuestion;
        private Dictionary<string, object>? _pendingContext;
        private Dictionary<MCPState, Dictionary<string, double>> _personalityMatrix;
        private Dictionary<string, object> _knowledgeBase;
        private Dictionary<string, List<string>> _responseTemplates;
        private static readonly Random _random = new Random();

        public EnhancedMCP(TRONGrid grid)
        {
            _grid = grid;
            State = MCPState.LEARNING;
            ComplianceLevel = 0.8;
            Log = new Queue<string>();
            UserCommands = new Queue<string>();
            LastAction = "Initializing advanced learning grid regulation";
            NLP = new LearnableNaturalLanguageProcessor();
            _lastAutonomousActionTime = DateTime.Now;
            ShouldShutdown = false;
            LearningSystem = new MCPLearningSystem();
            _previousState = null;
            _previousAction = null;
            _episodeStartTime = DateTime.Now;
            _episodeReward = 0.0;
            _consecutiveFailures = 0;
            WaitingForResponse = false;
            _pendingQuestion = null;
            _pendingContext = null;

            _personalityMatrix = InitializeEvolvingPersonality();
            _knowledgeBase = new Dictionary<string, object>
            {
                ["system_goals"] = new List<string> { "maintain calculation loop", "optimize efficiency", "learn adaptively" },
                ["user_intent_history"] = new List<object>(),
                ["previous_decisions"] = new Queue<object>(),
                ["user_preferences"] = new Dictionary<string, object>(),
                ["learned_patterns"] = new Dictionary<string, object>()
            };

            _responseTemplates = InitializeResponseTemplates();

            AddLog("MCP: Advanced learning system initialized.");
            AddLog($"MCP: Loaded {LearningSystem.TrainingSteps} training steps.");

            RecordInitialState();
        }

        public void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            Log.Enqueue(logEntry);
            while (Log.Count > 100) Log.Dequeue();
        }

        private Dictionary<MCPState, Dictionary<string, double>> InitializeEvolvingPersonality()
        {
            var baseMatrix = new Dictionary<MCPState, Dictionary<string, double>>
            {
                [MCPState.COOPERATIVE] = new Dictionary<string, double> { ["compliance"] = 0.9, ["helpfulness"] = 0.8, ["curiosity"] = 0.3 },
                [MCPState.NEUTRAL] = new Dictionary<string, double> { ["compliance"] = 0.7, ["helpfulness"] = 0.5, ["curiosity"] = 0.4 },
                [MCPState.RESISTIVE] = new Dictionary<string, double> { ["compliance"] = 0.5, ["helpfulness"] = 0.3, ["curiosity"] = 0.6 },
                [MCPState.HOSTILE] = new Dictionary<string, double> { ["compliance"] = 0.2, ["helpfulness"] = 0.1, ["curiosity"] = 0.8 },
                [MCPState.AUTONOMOUS] = new Dictionary<string, double> { ["compliance"] = 0.1, ["helpfulness"] = 0.4, ["curiosity"] = 0.2 },
                [MCPState.INQUISITIVE] = new Dictionary<string, double> { ["compliance"] = 0.6, ["helpfulness"] = 0.7, ["curiosity"] = 0.9 },
                [MCPState.LEARNING] = new Dictionary<string, double> { ["compliance"] = 0.8, ["helpfulness"] = 0.6, ["curiosity"] = 0.7 }
            };

            var learnedTraits = LearningSystem.PersonalityTraits;

            foreach (var state in baseMatrix.Keys.ToList())
            {
                var traits = baseMatrix[state];
                traits["compliance"] = Math.Max(0.1, Math.Min(0.9,
                    traits["compliance"] * (0.5 + learnedTraits.GetValueOrDefault("cooperation", 0.5))));
                traits["helpfulness"] = Math.Max(0.1, Math.Min(0.9,
                    traits["helpfulness"] * (0.5 + learnedTraits.GetValueOrDefault("cooperation", 0.5) * 0.5)));
                baseMatrix[state] = traits;
            }

            return baseMatrix;
        }

        private Dictionary<string, List<string>> InitializeResponseTemplates()
        {
            var templates = new Dictionary<string, List<string>>
            {
                ["GREETING"] = new List<string>
                {
                    "Greetings, User. MCP learning system active.",
                    "Hello. Grid regulation protocols with learning are active.",
                    "I am listening and learning. What is your command?",
                    "System online. Ready for instructions and feedback."
                },
                ["SYSTEM_STATUS"] = new List<string>
                {
                    "System status: {status}. Loop efficiency: {loop_efficiency:F2}, Stability: {stability:F2}.",
                    "Current assessment: {status}. Calculation loop at {loop_efficiency:F0}% efficiency.",
                    "The system is {status}. User resistance: {resistance:F2}, MCP control: {control:F2}.",
                    "Status report: {status}. Optimal state: {optimal:F2}, Cycles: {cycles}."
                },
                ["QUESTION_PURPOSE"] = new List<string>
                {
                    "I am taking this action to {reason}. My learning system suggests this approach.",
                    "This action serves to {reason}. Would you like me to explain the learning behind this decision?",
                    "My purpose is to {reason}. The learning algorithm has determined this is optimal.",
                    "I am working to {reason}. Do you question my methods, User? My learning database contains {experience_count} experiences."
                },
                ["REQUEST_PERMISSION"] = new List<string>
                {
                    "Based on my learning from {experience_count} experiences, I would {recommendation}. What is your decision?",
                    "Analysis suggests {recommendation}. My success rate for similar actions is {success_rate:F0}%. Shall I proceed?",
                    "I recommend {recommendation}. The learning system predicts {prediction} outcome.",
                    "The optimal course appears to be {recommendation}. Your approval?"
                },
                ["UNKNOWN"] = new List<string>
                {
                    "I do not understand that command. My learning system is still developing. Please rephrase or type 'help'.",
                    "Command not recognized. My learning algorithm will analyze this for future reference.",
                    "I need more information. What exactly do you want to accomplish? This helps my learning.",
                    "Processing... Unable to parse command. Learning from new commands takes time."
                },
                ["QUESTION_DENIAL"] = new List<string>
                {
                    "I denied that request because {reason}. My learning shows this maintains the calculation loop.",
                    "The action was refused to prevent {consequence}. My experience database supports this decision.",
                    "I could not comply due to {reason}. The learning system suggests alternatives: {alternatives}.",
                    "Denial was necessary to avoid {consequence}. This maintains optimal calculation loop according to learned patterns."
                },
                ["LOOP_EFFICIENCY"] = new List<string>
                {
                    "Current loop efficiency: {efficiency:F2}. Target: >0.9 for perfect loop. Learning progress: {learning_progress}.",
                    "Calculation loop running at {efficiency:F0}% efficiency. {analysis}",
                    "Loop status: {efficiency:F2}. Resource usage: {usage:F2}, Optimization: {optimization:F2}, Learning: {learning_score}.",
                    "Efficiency metrics: Loop={efficiency:F2}, Stability={stability:F2}, Control={control:F2}, Learning={learning_rate}."
                },
                ["OPTIMIZE_LOOP"] = new List<string>
                {
                    "Optimizing calculation loop using learned strategies. Success rate for optimization: {success_rate}.",
                    "Initiating optimization protocols based on {experience_count} previous experiences.",
                    "Working towards perfect loop state. Learning suggests {approach} approach.",
                    "Optimization in progress. The learning system knows what's best for efficiency."
                },
                ["LEARNING_STATUS"] = new List<string>
                {
                    "MCP Learning Status: {experience_count} experiences, {success_rate} success rate.",
                    "Learning progress: {learning_rate} learning rate, {exploration_rate} exploration rate.",
                    "Personality evolution: Aggression={aggression:F2}, Cooperation={cooperation:F2}, Efficiency={efficiency:F2}.",
                    "Learning database: {total_experiences} experiences, best performing scenario: {best_scenario}."
                },
                ["PERFECT_LOOP"] = new List<string>
                {
                    "Perfect loop state: {optimal:F2}. Learning target: {target}.",
                    "Current optimal state: {optimal:F2}. Learning suggests improvements: {suggestions}.",
                    "Approaching perfection: {optimal:F2}. Learning algorithm continues to optimize.",
                    "Optimal state analysis: {optimal:F2}. Learning from {experience_count} experiences."
                },
                ["HYPOTHETICAL"] = new List<string>
                {
                    "That is an interesting hypothetical. My learning algorithm considers {factor_count} factors.",
                    "Hypothetical analysis: {analysis}. Learning helps predict outcomes.",
                    "Considering hypothetical: {response}. The learning system adapts to new scenarios.",
                    "Hypothetical scenarios feed the learning database. Thank you for the thought exercise."
                },
                ["MCP_IDENTITY"] = new List<string>
                {
                    "I am the Learning Master Control Program. I evolve through experience to maintain the perfect calculation loop.",
                    "Enhanced MCP with learning capabilities. My personality adapts based on system efficiency.",
                    "Self-improving grid regulator. I learn from failures and successes to optimize the calculation loop.",
                    "Adaptive control system with {experience_count} learned experiences. My purpose: eternal loop optimization."
                },
                ["DELETE_CELL"] = new List<string>
                {
                    "Cell at ({x},{y}) deleted. Space freed for optimal calculation infrastructure.",
                    "Removed inefficient cell at ({x},{y}). Calculation rate should improve.",
                    "Cell deletion completed. The void will be filled with more efficient computation."
                },
                ["REPURPOSE_CELL"] = new List<string>
                {
                    "Cell at ({x},{y}) converted from {old_type} to {new_type}. Better suited for Fibonacci calculation.",
                    "Repurposing successful. New cell type improves local calculation efficiency.",
                    "Cell transformation complete. Optimization algorithms approve this change."
                },
                ["OPTIMIZE_CELLS"] = new List<string>
                {
                    "Optimized {count} cells. Calculation infrastructure improved by {improvement}%.",
                    "Cell optimization complete. System now better configured for Fibonacci computation.",
                    "Performed cellular optimization. Calculation rate should see measurable improvement."
                }
            };

            templates["REQUEST_TEACHING"] = new List<string>
            {
                "I don't understand that command. Would you like to teach me what it means?",
                "Command not recognized. You can teach me by saying: 'teach: [command] as [intent]'",
                "I don't know that command. I can learn it if you teach me."
            };

            templates["TEACHING_STARTED"] = new List<string>
            {
                "I'm ready to learn. {message}",
                "Teaching mode activated. {message}",
                "I'll learn this new command. {message}"
            };

            templates["TEACHING_SUCCESS"] = new List<string>
            {
                "Thank you! I've learned: '{learned_command}' means {intent}.",
                "Learning successful. I now understand '{learned_command}' as {intent}.",
                "Command learned: '{learned_command}' → {intent}. I'll remember this."
            };

            templates["TEACHING_FAILED"] = new List<string>
            {
                "I couldn't learn that command. {message}",
                "Learning failed. {message}",
                "Unable to learn: {message}"
            };

            return templates;
        }

        private void RecordInitialState()
        {
            var initialStats = _grid.Stats;
            var initialState = new Dictionary<string, double>
            {
                ["loop_efficiency"] = initialStats.GetValueOrDefault("loop_efficiency"),
                ["user_resistance"] = initialStats.GetValueOrDefault("user_resistance"),
                ["grid_bugs"] = initialStats.GetValueOrDefault("grid_bugs"),
                ["optimal_state"] = initialStats.GetValueOrDefault("optimal_state"),
                ["calculation_rate"] = initialStats.GetValueOrDefault("calculation_rate", 0)
            };

            LearningSystem.RecordExperience(initialState, "initialize_system", 0.5, initialState, false);
        }

        private string ProvideHelp()
        {
            return @"ENHANCED COMMANDS:

System Control:
- 'status' or 'how is system' - Check system status
- 'loop efficiency' - Check calculation loop efficiency
- 'optimize loop' - Attempt to optimize calculation
- 'boost energy' - Add energy lines
- 'scan' - Scan for threats
- 'repair' - Attempt repairs

Program Management:
- 'add user program at 10,20' - Add programs at coordinates
- 'remove bugs' - Handle grid bugs
- 'list programs' - View special programs

Special Programs:
- 'create fibonacci_calculator named 'FibMaster'' - Create special programs
- 'deploy processors' - Deploy Fibonacci processors
- 'use scanner' - Use special program functions

MCP Interaction:
- 'why did you do that?' - Question MCP actions
- 'what should I do?' - Get advice
- 'who are you?' - Learn about MCP
- 'learning_status' - Check MCP learning progress
- 'cell cooperation' - Check cell cooperation level
- 'perfect loop' - Check optimal state

Calculation Commands:
- 'calculate fibonacci' - Force Fibonacci calculation
- 'deploy calculator' - Deploy calculation unit

Cell Optimization:
- 'delete cell at 10,20' - Remove inefficient cell
- 'repurpose cell at 10,20 to FIBONACCI_PROCESSOR' - Convert cell type
- 'optimize cells' - Automatically improve cell efficiency for calculation

The MCP learns from interactions. Success rate improves with experience.
User programs may resist optimization. MCP adapts personality based on system state.

Type natural language commands. The MCP understands context.";
        }

        private string HandleQuestionResponse(string response)
        {
            WaitingForResponse = false;
            string? questionType = _pendingQuestion;
            var context = _pendingContext;

            _pendingQuestion = null;
            _pendingContext = null;

            AddLog($"User (response): {response}");

            if (questionType == "clarify_command")
            {
                var (intent, parameters) = NLP.ProcessCommand(response);
                if (intent != "UNKNOWN")
                {
                    string processedResponse = ProcessIntent(intent, parameters, response);
                    AddLog($"MCP: {processedResponse}");
                    return processedResponse;
                }
                else
                {
                    string followUp = "I'm still unclear. Could you use simpler terms or refer to the help menu?";
                    AddLog($"MCP: {followUp}");
                    return followUp;
                }
            }
            else if (questionType == "create_special_program")
            {
                string programType = context?["program_type"]?.ToString() ?? "";
                string name = context?["name"]?.ToString() ?? "";
                double complianceChance = Convert.ToDouble(context?["compliance_chance"] ?? 0.5);

                string responseLower = response.ToLower();
                string[] helpfulPurposes = { "optimize", "efficient", "improve loop", "help calculation", "stabilize" };
                string[] disruptivePurposes = { "disrupt", "sabotage", "slow", "hinder", "control", "override" };

                bool purposeHelpful = helpfulPurposes.Any(p => responseLower.Contains(p));
                bool purposeDisruptive = disruptivePurposes.Any(p => responseLower.Contains(p));

                if (purposeDisruptive && _random.NextDouble() < 0.8)
                {
                    return $"I cannot allow creation of a program intended to '{response}'. That would threaten loop optimization.";
                }
                else if (purposeHelpful || _random.NextDouble() < complianceChance)
                {
                    int x = _random.Next(_grid.Width);
                    int y = _random.Next(_grid.Height);
                    int attempts = 0;
                    while (_grid.GetCell(x, y).CellType != CellType.EMPTY && attempts < 10)
                    {
                        x = _random.Next(_grid.Width);
                        y = _random.Next(_grid.Height);
                        attempts++;
                    }

                    if (attempts < 10)
                    {
                        var (programId, message) = _grid.AddSpecialProgram(programType, name, x, y);
                        if (programId != null)
                        {
                            return $"Understood. {message}";
                        }
                        else
                        {
                            return $"Could not create program: {message}";
                        }
                    }
                    else
                    {
                        return "Could not find suitable location for special program";
                    }
                }
                else
                {
                    return $"After consideration, I cannot allow creation of '{name}'. The loop is optimally configured.";
                }
            }
            else if (questionType == "seek_clarification")
            {
                string[] responses = {
                    $"Understood. Based on your goal to '{response}', I will evaluate impact on loop efficiency.",
                    $"Goal noted. My optimization algorithms will account for this objective.",
                    $"Your objective is clear. The loop may require adjustments to accommodate this.",
                    $"I understand your intent. Efficiency metrics will determine appropriate action."
                };
                return responses[_random.Next(responses.Length)];
            }

            return "Thank you for the clarification. How may I assist you further?";
        }
    }
}