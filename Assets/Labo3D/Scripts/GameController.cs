using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Labo3D
{
    /// <summary>
    /// Orchestre la démo. Il ne déplace pas le joueur : il écoute deux touches
    /// et pousse des propriétés de shader via un MaterialPropertyBlock, pour
    /// ne pas dupliquer le matériau.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public enum DemoState
        {
            Exploration,
            Hologramme,
            Dissolution
        }

        public static GameController Instance { get; private set; }

        public DemoState State { get; private set; } = DemoState.Exploration;

        [SerializeField] Renderer hologramRenderer;
        [SerializeField] Renderer dissolveRenderer;
        [SerializeField] TMP_Text hintLabel;
        [SerializeField] float hologramPulseDuration = 1.2f;
        [SerializeField] float dissolveDuration = 1.6f;

        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int RimPowerId = Shader.PropertyToID("_RimPower");

        const string Controls =
            "ZQSD : se déplacer (touches physiques WASD)    Clic : capturer la souris\n" +
            "Souris : orbiter    Espace : sauter    Échap : libérer la souris\n" +
            "H : pulse holographique    K : jouer la dissolution";

        MaterialPropertyBlock hologramBlock;
        MaterialPropertyBlock dissolveBlock;
        Coroutine hologramRoutine;
        Coroutine dissolveRoutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            hologramBlock = new MaterialPropertyBlock();
            dissolveBlock = new MaterialPropertyBlock();
            SetDissolve(0f);
            SetHologram(0.35f, 2.5f);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.hKey.wasPressedThisFrame)
                    PulseHologram();

                if (keyboard.kKey.wasPressedThisFrame)
                    PlayDissolve();
            }

            RefreshHint();
        }

        public void PulseHologram()
        {
            State = DemoState.Hologramme;
            if (hologramRoutine != null)
                StopCoroutine(hologramRoutine);

            hologramRoutine = StartCoroutine(HologramPulse());
        }

        public void PlayDissolve()
        {
            State = DemoState.Dissolution;
            if (dissolveRoutine != null)
                StopCoroutine(dissolveRoutine);

            dissolveRoutine = StartCoroutine(DissolveSequence());
        }

        IEnumerator HologramPulse()
        {
            float elapsed = 0f;
            while (elapsed < hologramPulseDuration)
            {
                elapsed += Time.deltaTime;
                float wave = Mathf.Sin(Mathf.Clamp01(elapsed / hologramPulseDuration) * Mathf.PI);
                SetHologram(Mathf.Lerp(0.35f, 0.9f, wave), Mathf.Lerp(2.5f, 1.1f, wave));
                yield return null;
            }

            SetHologram(0.35f, 2.5f);
            hologramRoutine = null;
            if (State == DemoState.Hologramme)
                State = DemoState.Exploration;
        }

        IEnumerator DissolveSequence()
        {
            yield return AnimateDissolve(0f, 1f, dissolveDuration);
            yield return new WaitForSeconds(0.35f);
            yield return AnimateDissolve(1f, 0f, dissolveDuration);
            dissolveRoutine = null;
            if (State == DemoState.Dissolution)
                State = DemoState.Exploration;
        }

        IEnumerator AnimateDissolve(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                SetDissolve(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetDissolve(to);
        }

        void SetHologram(float alpha, float rimPower)
        {
            if (hologramRenderer == null)
                return;

            if (hologramBlock == null)
                hologramBlock = new MaterialPropertyBlock();

            hologramRenderer.GetPropertyBlock(hologramBlock);
            hologramBlock.SetFloat(AlphaId, alpha);
            hologramBlock.SetFloat(RimPowerId, rimPower);
            hologramRenderer.SetPropertyBlock(hologramBlock);
        }

        void SetDissolve(float amount)
        {
            if (dissolveRenderer == null)
                return;

            if (dissolveBlock == null)
                dissolveBlock = new MaterialPropertyBlock();

            dissolveRenderer.GetPropertyBlock(dissolveBlock);
            dissolveBlock.SetFloat(DissolveId, amount);
            dissolveRenderer.SetPropertyBlock(dissolveBlock);
        }

        void RefreshHint()
        {
            if (hintLabel == null)
                return;

            hintLabel.text = Controls + "\n\nÉtat : " + State;
        }
    }
}
