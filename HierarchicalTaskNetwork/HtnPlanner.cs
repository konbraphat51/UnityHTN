using System;
using System.Collections.Generic;
using System.Linq;

namespace Htn
{
    /// <summary>
    /// Plan tasks sequence towards the root task's goal
    /// </summary>
    /// <typeparam name="AgentType">
    /// Type of the agent contact with to control
    /// </typeparam>
    /// <typeparam name="WorldStateType">
    /// Struct that represents the world state, data that this planner uses to sense the world
    /// </typeparam>
    public class HtnPlanner<AgentType, WorldStateType>
        where WorldStateType : struct
    {
        /// <summary>
        /// Result of the action conducted by this class
        /// </summary>
        public enum ActionResult
        {
            Running,
            AllFinishedSuccessfully,
            Failed,
            NoPlan
        }

        /// <summary>
        /// History for roll back
        /// </summary>
        private struct DecompositionHistory
        {
            public TaskCompound<AgentType, WorldStateType> taskDecomposed;
            public WorldStateType worldStateThen;
            public List<TaskPrimitive<AgentType, WorldStateType>> planThen;
            public Stack<Task<AgentType, WorldStateType>> tasksToProcessThen;
            public int trial;
        }

        public bool isRunningPlan { get; protected set; } = false;
        private Queue<TaskPrimitive<AgentType, WorldStateType>> plan;

        /// <summary>
        /// Start the root task
        /// </summary>
        /// <param name="taskRoot">
        /// Root task to start
        /// </param>
        /// <param name="agent">
        /// Agent to Control
        /// </param>
        /// <param name="worldState">
        /// World state to start from
        /// </param>
        /// <returns>
        /// True if planned successfully.
        /// False if no plan found.
        /// </returns>
        public bool StartRootTask(
            AgentType agent,
            Task<AgentType, WorldStateType> taskRoot,
            WorldStateType worldState
        )
        {
            // make a plan
            plan = new Queue<TaskPrimitive<AgentType, WorldStateType>>(Plan(taskRoot, worldState));

            //if planned successfully...
            if (plan.Count > 0)
            {
                //...start the plan

                //turn on the flag
                isRunningPlan = true;

                //initialize first task
                plan.Peek().Initialize(agent);

                return true;
            }
            //if no plan found...
            else
            {
                //...turn off the flag
                isRunningPlan = false;

                return false;
            }
        }

        /// <summary>
        /// Control the agent according on the plan
        /// </summary>
        /// <param name="agent">
        /// Agent to control
        /// </param>
        /// <returns>
        /// result of the action
        /// </returns>
        public ActionResult ActOnPlan(AgentType agent)
        {
            //if no plan to act on...
            if ((!isRunningPlan) || (plan == null) || (plan.Count == 0))
            {
                //...skip
                return ActionResult.NoPlan;
            }

            //act on the plan
            TaskPrimitive<AgentType, WorldStateType> topTask = plan.Peek();
            TaskPrimitive<AgentType, WorldStateType>.OperationResult result = topTask.Operate(
                agent
            );

            switch (result)
            {
                //successfully finished
                case TaskPrimitive<AgentType, WorldStateType>.OperationResult.Success:
                    plan.Dequeue();

                    //if no more plan left...
                    if (plan.Count == 0)
                    {
                        //...turn off the flag
                        isRunningPlan = false;

                        return ActionResult.AllFinishedSuccessfully;
                    }
                    //if there's more plan...
                    else
                    {
                        //...initialize next task
                        plan.Peek().Initialize(agent);

                        return ActionResult.Running;
                    }

                //not finished yet
                case TaskPrimitive<AgentType, WorldStateType>.OperationResult.Running:
                    return ActionResult.Running;

                //failed
                case TaskPrimitive<AgentType, WorldStateType>.OperationResult.Failure:
                    //turn off the flag
                    isRunningPlan = false;

                    return ActionResult.Failed;

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Plan tasks sequence towards the root task's goal
        ///
        /// ref: https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf
        /// </summary>
        /// <param name="taskRoot">
        /// Root task to start
        /// </param>
        /// <param name="worldState">
        /// World state to start from
        /// </param>
        /// <returns>
        /// A plan of primitive tasks.
        /// Null if no plan found.
        /// </returns>
        private List<TaskPrimitive<AgentType, WorldStateType>> Plan(
            Task<AgentType, WorldStateType> taskRoot,
            WorldStateType worldState
        )
        {
            //if interrupting a plan...
            if (isRunningPlan)
            {
                //...interrupt handler
                InterruptTask();
            }

            List<TaskPrimitive<AgentType, WorldStateType>> result =
                new List<TaskPrimitive<AgentType, WorldStateType>>();

            //need to decompose all of this stack
            Stack<Task<AgentType, WorldStateType>> tasksToProcess =
                new Stack<Task<AgentType, WorldStateType>>();

            //add the root task
            tasksToProcess.Push(taskRoot);

            //until `tasksToProcess` is all decomposed
            Stack<DecompositionHistory> decompositionHistory = new Stack<DecompositionHistory>();
            int decompositionTrial = 0; //set outside the loop for ediing by history
            while (tasksToProcess.Count > 0)
            {
                //A task to process
                //not popping here because to save decomposition history
                Task<AgentType, WorldStateType> task = tasksToProcess.Peek();

                //if it's a primitive task...
                if (task is TaskPrimitive<AgentType, WorldStateType> taskPrimitive)
                {
                    //...consider adding

                    tasksToProcess.Pop();

                    //if condition met...
                    if (taskPrimitive.conditions.All(condition => condition.IsMet(worldState)))
                    {
                        //...add to the result
                        result.Add(taskPrimitive);

                        //update world state
                        foreach (Effect<WorldStateType> effect in taskPrimitive.effects)
                        {
                            worldState = effect.Apply(worldState);
                        }
                    }
                    //if not met...
                    else
                    {
                        //...roll back
                        bool rollBackResult = RollBackPlan(
                            decompositionHistory,
                            ref tasksToProcess,
                            ref result,
                            ref worldState,
                            ref decompositionTrial
                        );

                        //if no history left...
                        if (!rollBackResult)
                        {
                            //...no plan possible
                            return null;
                        }
                    }
                }
                //if it's a compound task...
                else if (task is TaskCompound<AgentType, WorldStateType> taskCompound)
                {
                    //...try to decompose

                    //while there's a method to decompose...
                    while (taskCompound.methods.Length > decompositionTrial)
                    {
                        TaskCompound<AgentType, WorldStateType>.Method method =
                            taskCompound.methods[decompositionTrial];

                        //if conditions met...
                        if (method.conditions.All(condition => condition.IsMet(worldState)))
                        {
                            //...decompose

                            //save history
                            DecompositionHistory history = new DecompositionHistory
                            {
                                taskDecomposed = taskCompound,
                                worldStateThen = worldState,
                                planThen = new List<TaskPrimitive<AgentType, WorldStateType>>(
                                    result
                                ),
                                tasksToProcessThen = new Stack<Task<AgentType, WorldStateType>>(
                                    tasksToProcess
                                ),
                                trial = decompositionTrial
                            };
                            decompositionHistory.Push(history);

                            tasksToProcess.Pop();

                            //add subtasks to decompose
                            foreach (
                                Task<AgentType, WorldStateType> subtask in method.subtasks.Reverse()
                            )
                            {
                                tasksToProcess.Push(subtask);
                            }

                            //reset trial
                            decompositionTrial = 0;

                            //stop planing
                            break;
                        }
                        //if not met...
                        else
                        {
                            //...try another method
                            decompositionTrial++;
                        }
                    }

                    //if decomposed all and everything failed...
                    if (taskCompound.methods.Length <= decompositionTrial)
                    {
                        //...roll back
                        bool rollBackResult = RollBackPlan(
                            decompositionHistory,
                            ref tasksToProcess,
                            ref result,
                            ref worldState,
                            ref decompositionTrial
                        );

                        //if no history left...
                        if (!rollBackResult)
                        {
                            //...no plan possible
                            return null;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Roll back task planning in `Plan()`
        /// </summary>
        /// <returns>
        /// True if rolled back successfully.
        /// If false, no history to roll back.
        /// </returns>
        private bool RollBackPlan(
            Stack<DecompositionHistory> decompositionHistory,
            ref Stack<Task<AgentType, WorldStateType>> tasksToProcess,
            ref List<TaskPrimitive<AgentType, WorldStateType>> planMaking,
            ref WorldStateType worldState,
            ref int decompositionTrial
        )
        {
            //if no history to roll back...
            if (decompositionHistory.Count == 0)
            {
                //...finish as failed
                return false;
            }

            DecompositionHistory historyToRollBack = decompositionHistory.Pop();

            //roll back data
            tasksToProcess = historyToRollBack.tasksToProcessThen;
            planMaking = historyToRollBack.planThen;
            worldState = historyToRollBack.worldStateThen;
            decompositionTrial = historyToRollBack.trial + 1;

            return true;
        }

        private void InterruptTask()
        {
            TaskPrimitive<AgentType, WorldStateType> topTask = plan.Peek();

            topTask.OnInterrupted();
        }
    }
}
