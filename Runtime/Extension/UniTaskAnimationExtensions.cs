namespace Extension
{
    // UniTaskAnimatorExtensions.cs
//
// Requires: Cysharp.Threading.Tasks (UniTask)
// Usage examples:
// await animator.PlayAndWait("Run", layer:0, startNormalizedTime:0f, ct);
// await animator.SetTriggerAndWait("Attack", waitStateName:"Attack", layer:0, ct:ct);
// await animator.WaitForNormalizedTime("Attack", 0, 0.5f, ct); // дождаться половины
// await legacyAnimation.PlayAndWait("Take 001", ct);
// await animator.CrossFadeAndWait("Die", 0.1f, 0, 0f, ct);

    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public static class UniTaskAnimatorExtensions
    {
        // ------------- Animator (Mecanim) -------------

        /// <summary>
        /// Play state immediately and await until this state exits (or clip finishes if non-loop).
        /// </summary>
        public static UniTask PlayAndWait(this Animator animator,
            string stateName, int layer = 0, float startNormalizedTime = 0f,
            CancellationToken ct = default)
        {
            if (!animator) return UniTask.CompletedTask;
            animator.Play(stateName, layer, startNormalizedTime);
            int hash = Animator.StringToHash(stateName);
            return animator.WaitForStateExit(hash, layer, ct);
        }

        /// <summary>
        /// CrossFade to state and await until this state exits.
        /// </summary>
        public static async UniTask CrossFadeAndWait(this Animator animator,
            string stateName, float transitionDuration, int layer = 0, float normalizedTimeOffset = 0f,
            CancellationToken ct = default)
        {
            if (!animator) return;
            int hash = Animator.StringToHash(stateName);
            animator.CrossFade(hash, transitionDuration, layer, normalizedTimeOffset);
            await animator.WaitForStateEnter(hash, layer, includeTransition: true, ct);
            await animator.WaitForStateExit(hash, layer, ct);
        }

        /// <summary>
        /// Set trigger and await until specified state is entered (optional) then exited.
        /// </summary>
        public static async UniTask SetTriggerAndWait(this Animator animator,
            string triggerName, string waitStateName, int layer = 0, CancellationToken ct = default)
        {
            if (!animator) return;
            var hash = Animator.StringToHash(waitStateName);
            animator.ResetTrigger(triggerName); // на всякий
            animator.SetTrigger(triggerName);
            await animator.WaitForStateEnter(hash, layer, includeTransition: true, ct);
            await animator.WaitForStateExit(hash, layer, ct);
        }


        /// <summary>
        /// Await until the animator enters given state (optionally count transition).
        /// </summary>
        public static async UniTask WaitForStateEnter(this Animator animator,
            int fullPathHash, int layer, bool includeTransition = false, CancellationToken ct = default)
        {
            if (!animator) return;

            // Ждём появления состояния в текущем или следующем апдейте
            while (!ct.IsCancellationRequested && animator)
            {
                var s = animator.GetCurrentAnimatorStateInfo(layer);
                var t = animator.GetAnimatorTransitionInfo(layer);

                bool inState = s.fullPathHash == fullPathHash;
                bool inTransitionToState = includeTransition && animator.IsInTransition(layer) &&
                                           (t.userNameHash == 0 && t.fullPathHash == 0
                                               ? false
                                               : (animator.GetNextAnimatorStateInfo(layer).fullPathHash ==
                                                  fullPathHash));

                if (inState || inTransitionToState)
                    break;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        /// <summary>
        /// Await until the given state reaches target normalizedTime. Assumes the state is current.
        /// </summary>
        public static async UniTask WaitForNormalizedTime(this Animator animator,
            string stateName, int layer, float normalizedTime, CancellationToken ct = default)
        {
            if (!animator) return;
            int hash = Animator.StringToHash(stateName);
            await animator.WaitForNormalizedTime(hash, layer, normalizedTime, ct);
        }

        public static async UniTask WaitForNormalizedTime(this Animator animator,
            int fullPathHash, int layer, float normalizedTime, CancellationToken ct = default)
        {
            if (!animator) return;
            normalizedTime = Mathf.Max(0f, normalizedTime);

            // Ждём входа в состояние
            await animator.WaitForStateEnter(fullPathHash, layer, includeTransition: true, ct);

            // Ждём нормализованного времени
            while (!ct.IsCancellationRequested && animator)
            {
                var s = animator.GetCurrentAnimatorStateInfo(layer);
                if (s.fullPathHash != fullPathHash) break; // вышли из состояния
                // Для лупов normalizedTime растёт бесконечно: сравним с целым числом циклов
                float t = s.normalizedTime;
                if (!s.loop)
                {
                    if (t >= normalizedTime) break;
                }
                else
                {
                    if (t >= normalizedTime) break; // пусть пользователь сам задаёт >1f, если нужно несколько циклов
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        /// <summary>
        /// Await until current state changes from fullPathHash OR (if non-loop) completes first cycle.
        /// </summary>
        public static async UniTask WaitForStateExit(this Animator animator,
            int fullPathHash, int layer, CancellationToken ct = default)
        {
            if (!animator) return;

            // На случай если вызвали до входа — дождёмся входа
            await animator.WaitForStateEnter(fullPathHash, layer, includeTransition: true, ct);

            bool wasNonLoop = false;
            float enterCycle = 0f;

            // Захватываем свойства на входе в состояние
            {
                var s = animator.GetCurrentAnimatorStateInfo(layer);
                wasNonLoop = !s.loop;
                enterCycle = Mathf.Floor(s.normalizedTime);
            }

            while (!ct.IsCancellationRequested && animator)
            {
                var s = animator.GetCurrentAnimatorStateInfo(layer);

                if (s.fullPathHash != fullPathHash) // в другое состояние перешли
                    break;

                if (wasNonLoop)
                {
                    // ждём завершения первого проигрывания (>= 1 цикла)
                    if (s.normalizedTime >= (enterCycle + 1f))
                        break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        // ------------- Legacy Animation -------------

        /// <summary>
        /// Play legacy Animation clip and await until it stops playing.
        /// </summary>
        public static async UniTask PlayAndWait(this Animation animation, string clipName,
            CancellationToken ct = default)
        {
            if (!animation || string.IsNullOrEmpty(clipName)) return;
            animation.Play(clipName);
            // ждём старта, если клип ещё не активен
            while (!ct.IsCancellationRequested && animation && !animation.IsPlaying(clipName))
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

            while (!ct.IsCancellationRequested && animation && animation.IsPlaying(clipName))
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // ------------- Utility -------------

        /// <summary>Await until animator reference is destroyed/disabled (optional helper).</summary>
        public static async UniTask WaitUntilDisabled(this Animator animator, CancellationToken ct = default)
        {
            if (!animator) return;
            await UniTask.WaitUntil(() => !animator || !animator.isActiveAndEnabled, PlayerLoopTiming.Update, ct);
        }
    }
}