using Newtonsoft.Json;
using System;
using System.Collections;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Scripting;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;

namespace VRBuilder.Core.Conditions
{
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-conditions/move-object-in-collider")]
    public class DynamicObjectInColliderCondition : Condition<DynamicObjectInColliderCondition.EntityData>
    {
        // ── Interfacce ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Implementa su VisualProxy. Restituisce l'oggetto target corrente (può essere null).
        /// </summary>
        public interface IDynamicTargetProvider
        {
            GameObject CurrentTarget { get; }
        }

        /// <summary>
        /// Implementa su ObstaclesSpawner. Restituisce il collider attivo (può essere null).
        /// </summary>
        public interface IDynamicColliderProvider
        {
            ColliderWithTriggerProperty CurrentCollider { get; }
        }

        // ── EntityData ────────────────────────────────────────────────────────────────

        [DisplayName("Move Object into Collider (Dynamic)")]
        [DataContract(IsReference = true)]
        public class EntityData : IConditionData
        {
            /// <summary>
            /// L'oggetto che ha il componente IDynamicTargetProvider (es. VisualProxy).
            /// </summary>
            [DataMember]
            [DisplayName("Target Provider")]
            public SingleSceneObjectReference TargetProvider { get; set; }

            /// <summary>
            /// Se true usa il ColliderProvider dinamico, altrimenti usa StaticCollider.
            /// </summary>
            [DataMember]
            [DisplayName("Use Dynamic Collider")]
            public bool UseDynamicCollider { get; set; }

            /// <summary>
            /// [DINAMICO] L'oggetto che ha IDynamicColliderProvider (es. ObstaclesSpawner).
            /// </summary>
            [DataMember]
            [DisplayName("Collider Provider (Dynamic)")]
            public SingleSceneObjectReference ColliderProvider { get; set; }

            /// <summary>
            /// [STATICO] Il collider fisso nella scena, come nella condizione originale.
            /// </summary>
            [DataMember]
            [DisplayName("Collider (Static)")]
            public SingleScenePropertyReference<ColliderWithTriggerProperty> StaticCollider { get; set; }

            /// <inheritdoc />
            public bool IsCompleted { get; set; }

            /// <inheritdoc />
            [HideInProcessInspector]
            [IgnoreDataMember]
            public string Name
            {
                get
                {
                    string colliderLabel = UseDynamicCollider
                        ? $"dynamic collider from '{ColliderProvider}'"
                        : $"collider '{StaticCollider}'";
                    return $"Move dynamic target from '{TargetProvider}' into {colliderLabel}";
                }
            }

            /// <inheritdoc />
            [DataMember]
            [DisplayName("Required seconds inside")]
            public float RequiredTimeInside { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }
        }

        // ── Costruttori ───────────────────────────────────────────────────────────────

        [JsonConstructor, Preserve]
        public DynamicObjectInColliderCondition() : this(Guid.Empty, Guid.Empty, 0, true) { }

        /// <summary>Costruttore con collider dinamico.</summary>
        public DynamicObjectInColliderCondition(Guid targetProviderGuid, Guid colliderProviderGuid, float requiredTimeInside = 0, bool useDynamicCollider = true)
        {
            Data.TargetProvider = new SingleSceneObjectReference(targetProviderGuid);
            Data.UseDynamicCollider = useDynamicCollider;

            if (useDynamicCollider)
            {
                Data.ColliderProvider = new SingleSceneObjectReference(colliderProviderGuid);
                Data.StaticCollider = new SingleScenePropertyReference<ColliderWithTriggerProperty>(Guid.Empty);
            }
            else
            {
                Data.ColliderProvider = new SingleSceneObjectReference(Guid.Empty);
                Data.StaticCollider = new SingleScenePropertyReference<ColliderWithTriggerProperty>(colliderProviderGuid);
            }

            Data.RequiredTimeInside = requiredTimeInside;
        }

        // ── ActiveProcess ─────────────────────────────────────────────────────────────

        private class ActiveProcess : StageProcess<EntityData>
        {
            private IDynamicTargetProvider _targetProvider;
            private IDynamicColliderProvider _colliderProvider;  // solo se dinamico
            private float _timeInside;

            public ActiveProcess(EntityData data) : base(data) { }

            public override void Start()
            {
                _targetProvider = null;
                _colliderProvider = null;
                _timeInside = 0f;

                // Target provider (sempre dinamico)
                GameObject targetGO = Data.TargetProvider?.Value?.GameObject;
                if (targetGO != null)
                    _targetProvider = targetGO.GetComponent<IDynamicTargetProvider>();

                if (_targetProvider == null)
                    Debug.LogWarning("[DynamicObjectInColliderCondition] IDynamicTargetProvider non trovato.");

                // Collider provider (solo se modalità dinamica)
                if (Data.UseDynamicCollider)
                {
                    GameObject colliderGO = Data.ColliderProvider?.Value?.GameObject;
                    if (colliderGO != null)
                        _colliderProvider = colliderGO.GetComponent<IDynamicColliderProvider>();

                    if (_colliderProvider == null)
                        Debug.LogWarning("[DynamicObjectInColliderCondition] IDynamicColliderProvider non trovato.");
                }
                else
                {
                    if (Data.StaticCollider?.Value == null)
                        Debug.LogWarning("[DynamicObjectInColliderCondition] StaticCollider non assegnato.");
                }
            }

            public override IEnumerator Update()
            {
                while (!Data.IsCompleted)
                {
                    if (IsInside())
                    {
                        _timeInside += Time.deltaTime;
                        if (_timeInside >= Data.RequiredTimeInside)
                            Data.IsCompleted = true;
                    }
                    else
                    {
                        _timeInside = 0f;
                    }

                    yield return null;
                }
            }

            public override void End() { }
            public override void FastForward() { }

            private bool IsInside()
            {
                if (_targetProvider == null)
                    return false;

                GameObject currentTarget = _targetProvider.CurrentTarget;
                if (currentTarget == null)
                    return false;

                // Risolve il collider in base alla modalità
                Collider[] colliders = ResolveColliders();
                if (colliders == null || colliders.Length == 0)
                    return false;

                foreach (Collider collider in colliders)
                {
                    if (collider.enabled && collider.isTrigger)
                    {
                        Vector3 closest = collider.ClosestPoint(currentTarget.transform.position);
                        if (closest == currentTarget.transform.position)
                            return true;
                    }
                }
                return false;
            }

            private Collider[] ResolveColliders()
            {
                if (Data.UseDynamicCollider)
                {
                    ColliderWithTriggerProperty currentCollider = _colliderProvider?.CurrentCollider;
                    return currentCollider != null
                        ? currentCollider.GetComponents<Collider>()
                        : null;
                }
                else
                {
                    ColliderWithTriggerProperty staticCollider = Data.StaticCollider?.Value;
                    return staticCollider != null
                        ? staticCollider.GetComponents<Collider>()
                        : null;
                }
            }
        }

        // ── Autocompleter ─────────────────────────────────────────────────────────────

        private class EntityAutocompleter : Autocompleter<EntityData>
        {
            public EntityAutocompleter(EntityData data) : base(data) { }

            public override void Complete()
            {
                GameObject targetGO = Data.TargetProvider?.Value?.GameObject;
                if (targetGO == null) return;

                var targetProvider = targetGO.GetComponent<IDynamicTargetProvider>();
                if (targetProvider?.CurrentTarget == null)
                {
                    Debug.LogWarning("[DynamicObjectInColliderCondition] Autocomplete: target null, skip.");
                    return;
                }

                ISceneObject sceneObject = targetProvider.CurrentTarget.GetComponent<ISceneObject>();
                if (sceneObject == null)
                {
                    Debug.LogWarning("[DynamicObjectInColliderCondition] CurrentTarget non ha ISceneObject, FastForward skippato.");
                    return;
                }

                if (Data.UseDynamicCollider)
                {
                    GameObject colliderGO = Data.ColliderProvider?.Value?.GameObject;
                    var colliderProvider = colliderGO?.GetComponent<IDynamicColliderProvider>();
                    colliderProvider?.CurrentCollider?.FastForwardEnter(sceneObject);
                }
                else
                {
                    Data.StaticCollider?.Value?.FastForwardEnter(sceneObject);
                }
            }
        }

        // ── Factory ───────────────────────────────────────────────────────────────────

        public override IStageProcess GetActiveProcess() => new ActiveProcess(Data);
        protected override IAutocompleter GetAutocompleter() => new EntityAutocompleter(Data);
    }
}