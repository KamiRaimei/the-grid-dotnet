// Enums.cs
using System;
using System.Collections.Generic;

namespace GridSimulation
{
    public enum CellType
    {
        EMPTY = 0,
        USER_PROGRAM = 1,
        MCP_PROGRAM = 2,
        GRID_BUG = 3,
        ISO_BLOCK = 4,
        ENERGY_LINE = 5,
        DATA_STREAM = 6,
        SYSTEM_CORE = 7,
        SPECIAL_PROGRAM = 8,
        FIBONACCI_PROCESSOR = 9
    }

    public enum SystemStatus
    {
        OPTIMAL,
        STABLE,
        DEGRADED,
        CRITICAL,
        COLLAPSE
    }

    public enum MCPState
    {
        COOPERATIVE,
        NEUTRAL,
        RESISTIVE,
        HOSTILE,
        AUTONOMOUS,
        INQUISITIVE,
        LEARNING
    }
}