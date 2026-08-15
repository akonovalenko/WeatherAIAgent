# Copilot Instructions

## Project Guidelines
- User prefers moving local helper methods (e.g., TryGetInt in AgentService.SaveUsage) to shared helper classes for reuse.
- Store token counts and estimated cost between requests; keep EstimatedCost in AgentContext and aggregate across requests.