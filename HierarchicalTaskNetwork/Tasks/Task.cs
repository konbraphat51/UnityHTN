namespace Htn
{
    /// <summary>
    /// Task for the HTN planner
    /// </summary>
    /// <typeparam name="WorldStateType">
    /// Struct that represents the world state, data that this planner uses to sense the world
    /// </typeparam>
    public abstract class Task<AgentType, WorldStateType>
        where WorldStateType : struct
    {
        public abstract string name { get; }
    }
}
