﻿using System;
using UnityEngine;
using System.Collections.Generic;


///<summary>
/// Entity the root of the system, any game object that interacts with
/// Abilities, Status Effects, or AI needs to have an entity component
/// </summary>

[SelectionBase]
[DisallowMultipleComponent]
public partial class Entity : MonoBehaviour {

    public string factionId;

    [HideInInspector] public string id;
    [HideInInspector] public AbilityManager abilityManager;
    [HideInInspector] public ResourceManager resourceManager;
    [HideInInspector] public StatusEffectManager statusManager;

    private Vector3 lastPosition;
    private bool movedThisFrame = false;
	protected EventEmitter emitter;
    private SerializedPropertyX rootProperty;

	//handle progression of entity, attributes, and resources
    public void Awake() {
        if (!string.IsNullOrEmpty(source)) {
            new AssetDeserializer(source, false).DeserializeInto("__default__", this);
        } else {
            new SerializedObjectX(this).ApplyModifiedProperties();
        }
        resourceManager = resourceManager ?? new ResourceManager(this);
        statusManager = statusManager ?? new StatusEffectManager(this);
        abilityManager = abilityManager ?? new AbilityManager(this);
		emitter = new EventEmitter();
        EntityManager.Instance.Register(this);
        //gameObject.layer = LayerMask.NameToLayer("Entity");
    }

    public virtual void Update() {
                lastPosition = transform.position;
        if (abilityManager != null) {
            abilityManager.Update();
        }
        if (statusManager != null) {
            statusManager.Update();
        }
        if (resourceManager != null) {
            //resourceManager.Update();
        }
        if (emitter != null) {
            emitter.FlushQueue();
        }
    }

    public void LateUpdate() {
        movedThisFrame = lastPosition != transform.position;
    }

    #region Properties

    public bool IsMoving {
        get { return movedThisFrame; }
    }

    public bool IsPlayer {
        get { return tag == "Player"; }
    }

    public bool IsCasting {
        get { return abilityManager.IsCasting; }
    }

    public Ability ActiveAbility {
        get { return abilityManager.ActiveAbility; }
    }

    public bool IsChanneling {
        get { return abilityManager.ActiveAbility != null && abilityManager.ActiveAbility.IsChanneled; }
    }
		
	public EventEmitter EventEmitter {
		get { 
			return emitter;
		}
	}
    #endregion
}