namespace Htn
{
    /// <summary>
    /// Task that can be executed directly
    /// </summary>
    /// <typeparam name="WorldStateType">
    /// Struct that represents the world state, data that this planner uses to sense the world
    /// </typeparam>
    public abstract class TaskPrimitive<AgentType, WorldStateType> : Task<AgentType, WorldStateType>
        where WorldStateType : struct
    {
        public enum OperationResult
        {
            Running, //not finished yet
            Success, //finished successfully
            Failure //finished unsuccessfully
        }

        public TaskPrimitive()
        {
            SetConditions();
            SetEffects();
        }

        public Condition<WorldStateType>[] conditions { get; protected set; }
        public Effect<WorldStateType>[] effects { get; protected set; }

        public abstract OperationResult Operate(AgentType agentHtn);
        public abstract void Initialize(AgentType agentHtn);
        public abstract void OnInterrupted();
        protected abstract void SetConditions();
        protected abstract void SetEffects();
    }
}
