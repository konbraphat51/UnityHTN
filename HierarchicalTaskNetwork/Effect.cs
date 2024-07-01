namespace Htn
{
    /// <summary>
    /// Task that can be executed directly
    /// </summary>
    /// <typeparam name="WorldStateType">
    /// Struct that represents the world state, data that this planner uses to sense the world
    /// </typeparam>
    public abstract class Effect<WorldState>
        where WorldState : struct
    {
        /// <summary>
        /// Update WorldState with the effect, the original WorldState is not modified
        /// </summary>
        /// <param name="worldState">
        /// data that this planner uses to sense the worlds
        /// </param>
        /// <returns>Applied WorldState</returns>
        public WorldState Apply(WorldState worldState)
        {
            //copy the world state
            WorldState clonedWorldState = worldState;

            //apply the effect
            return ApplyEffect(clonedWorldState);
        }

        protected abstract WorldState ApplyEffect(WorldState worldState);
    }
}
