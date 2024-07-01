namespace Htn
{
    /// <summary>
    /// Task that compounds other tasks
    /// </summary>
    /// <typeparam name="WorldStateType">
    /// Struct that represents the world state, data that this planner uses to sense the world
    /// </typeparam>
    public abstract class TaskCompound<AgentType, WorldStateType> : Task<AgentType, WorldStateType>
        where WorldStateType : struct
    {
        public struct Method
        {
            public string name;
            public Condition<WorldStateType>[] conditions;
            public Task<AgentType, WorldStateType>[] subtasks;
        }

        public Method[] methods { get; protected set; }

        public TaskCompound()
        {
            SetMethods();
        }

        protected abstract void SetMethods();
    }
}
