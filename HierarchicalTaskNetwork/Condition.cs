namespace Htn
{
    /// <summary>
    /// Task that can be executed directly
    /// </summary>
    /// <typeparam name="WorldStateType">
    /// Struct that represents the world state, data that this planner uses to sense the world
    /// </typeparam>
    public abstract class Condition<WorldState>
        where WorldState : struct
    {
        public bool IsMet(WorldState worldState)
        {
            //copy the world state
            WorldState clonedWorldState = worldState;

            //validate the condition
            return ValidateCondition(clonedWorldState);
        }

        protected abstract bool ValidateCondition(WorldState worldState);
    }
}
