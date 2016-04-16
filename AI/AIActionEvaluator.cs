﻿using UnityEngine;
using System.Collections.Generic;
using AbilitySystem;
using System;

/*Infinite Axis Utility System is all about DECISION MAKING not ACTION TAKING*/
//this means I'll need to handle actions seperately from deciding when to do what
//this also means that we throw away the concept of 'planning' and just 'do whats best' 
//but planning can be done at a high level for group coordination, IE there can be a 
//high level planner (also controlled by utility most likely) that can issue orders
//to individuals. the individual agents can optionally have a goal
//(strictly defined as an action type, probably with considerations). Also, actions
//should be stateless 

//actions should all be done in isolation, long running actions should be given a bonus 
//score but still get re-evaluated. things like scripted actions can be either checked for
//or given very high weights

public class AIActionManager {

    // todo divide into 'packages' that can be dynamically added / removed

    private Entity entity;
    private Decision[] decisions;
    private AIAction currentAction;
    public Timer nextDecisionTimer;
    private List<AIDecisionResult> decisionResults;

    public AIDecisionLog decisionLog;

    //for now assume a single implicit behavior, explore having more behaviors later

    public AIActionManager(Entity entity, Decision[] actions = null) {
        this.entity = entity;
        SetDecisionPackage(actions);
        nextDecisionTimer = new Timer(0.5f);
        decisionResults = new List<AIDecisionResult>();
        decisionLog = new AIDecisionLog(entity.name);
    }

    //todo this should really be json so we dont re-use action instances across AI Entities
    public void SetDecisionPackage(Decision[] decisionPackage) {
        decisions = decisionPackage ?? new Decision[0];
    }

    public void Update() {
        if (decisions.Length == 0) return;
        if (currentAction == null || nextDecisionTimer.ReadyWithReset()) {
            SelectAction();
            return;
        }

        ActionStatus status = currentAction.OnUpdate();

        if (status == ActionStatus.Success) {
            currentAction.OnSuccess();
            currentAction.OnEnd();
            currentAction = null;
        }
        else if (status == ActionStatus.Failure) {
            currentAction.OnFailure();
            currentAction.OnEnd();
            currentAction = null;
        }
    }

    public void Unload() {
        if (currentAction != null) {
            currentAction.OnFailure();
            currentAction.OnEnd();
        }
        currentAction = null;
        decisions = null;
    }

    private void SelectAction() {
        if (decisions == null) return;
        decisionResults.Clear();

        AIDecisionLogEntry diagLog = decisionLog.AddEntry();
        for (int i = 0; i < decisions.Length; i++) {
            Decision decision = decisions[i];
            Context[] contexts = decision.contextCreator.GetContexts(entity);
            for (int j = 0; j < contexts.Length; j++) {
                Context context = contexts[j];
                context.entity = entity;
                AIDecisionEvaluatorLogEntry logEntry = diagLog.AddDecision(decision, context);
                AIDecisionResult result = decision.Score(context, logEntry);
                decisionResults.Add(result);
            }
            //todo weight each result
        }

        if (decisionResults.Count == 0) {
            return;
        }

        decisionResults.Sort();
        AIDecisionResult best = decisionResults[0];
        diagLog.SetSelectedAction(best);

        if (currentAction != null && best.action != currentAction) {
            currentAction.OnInterrupt();
            currentAction.OnEnd();
        }
        //todo dont re-invoke start if action/context pair doesnt change
        currentAction = best.action;
        currentAction.Execute(best.context);
    }

    public string GetCurrentActionName() {
        if (currentAction == null) return "No Current Action";
        return currentAction.GetType().Name;
    }

    //protected AIDecisionResult Score(AIAction action, Context context, AIDecisionEvaluatorLogEntry deciscionLog) {
    //    var considerations = action.considerations;
    //    var requirements = action.requirements;

    //    float modFactor = 1f - (1f / considerations.Length);
    //    float total = 1f;

    //    bool passedRequirements = true;

    //    if (requirements != null) {
    //        for (int i = 0; i < requirements.Length; i++) {
    //            var requirement = requirements[i];
    //            passedRequirements = requirement.Check(context);
    //            deciscionLog.RecordRequirement(requirement.name, passedRequirements);
    //            if (!passedRequirements) {
    //                break;
    //            }
    //        }
    //    }

    //    if (passedRequirements) {
    //        //score and scale score according to total # of considerations
    //        for (int i = 0; i < considerations.Length; i++) {
    //            var consideration = considerations[i];
    //            var curve = consideration.curve;
    //            var input = considerations[i].Score(context);
    //            var score = curve.Evaluate(input);
    //            if (score == 0) {
    //                total = 0;
    //                deciscionLog.RecordConsideration(consideration, Mathf.Clamp01(input), score);
    //                break;
    //            }
    //            float makeUpValue = (1 - score) * modFactor;
    //            float final = score + (makeUpValue * score);
    //            total *= final;
    //            deciscionLog.RecordConsideration(consideration, Mathf.Clamp01(input), score);
    //        }
    //    }

    //    AIDecisionResult result = new AIDecisionResult() {
    //        score = total,
    //        action = action,
    //        context = context
    //    };

    //    deciscionLog.RecordResult(total);
    //    return result;
    //}
}

/*
Action spawns 1 decision per context evaluated
    each decision runs its considerations to get a final score
        considerations take an input which is parameterized
    decision result is added to list of executable actions

Action can be executed if selected

*/


/*
Actions are pre-created and generally shared between ai agents
Action considerations are added per ai-agent(can be shared in stuff like templates)
Actions should maybe have a base weight that is 1 - (their index / total actions considered) where no considerations are added

{
    "Actions": [
        {
            "Name": "Use Skill [Frostbolt]",
            "typeName": "",
            "AbilityId": "FrostBolt",
            "Considerations": [
                {
                    "name": "Not when I'm too close",
                    "typeName": "AIConsideration_DistanceToTarget"
                    "Range": [0, 1],
                    "Min": 0.2, 
                    "Tags": ["Tag"],
                    "Curve": {
                        "Type": "Linear", 
                        "M":0, 
                        "K":0, 
                        "B":0, 
                        "C":0
                    },
                },
                {
                    "Name": "Not for [n] seconds after [action]",
                    "Input": "AIInput_DelayAfterAction",
                    "Parameters": { "ActionName": "SomeAction", "Time": 10|Formula? }
                }
            ]
        }
    ]
}
*/
