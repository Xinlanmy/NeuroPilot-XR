using UnityEngine;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// Small in-VR telemetry panel driven by attention_update / fatigue_update /
    /// cognitive_profile envelopes. It is intentionally code-created so no scene
    /// YAML changes are required; FusionEegBridge adds it at runtime.
    /// </summary>
    public sealed class VrTelemetryPanel : MonoBehaviour
    {
        private GameObject root;
        private TextMesh attentionText;
        private TextMesh fatigueText;
        private TextMesh profileText;

        private float attentionScore = 0.5f;
        private float fatigueScore;
        private float quality = 1f;
        private string state = "NORMAL";
        private int validSamples;
        private double attentionSum;
        private double fatigueDuration;

        public float AttentionScore => attentionScore;
        public bool IsProfileVisible => profileText != null && profileText.gameObject.activeSelf;
        public bool IsLiveTelemetryVisible => attentionText != null && attentionText.gameObject.activeSelf;

        private void Awake()
        {
            Build();
        }

        private void Update()
        {
            Camera cam = Camera.main;
            if (cam == null || root == null) return;
            root.transform.position = cam.transform.position + cam.transform.rotation * new Vector3(0f, 0.12f, 1.55f);
            root.transform.rotation = cam.transform.rotation;
        }

        public void SetAttention(FusionJson payload)
        {
            if (payload == null) return;
            attentionScore = Read01(payload, "score", attentionScore);
            quality = Read01(payload, "quality", quality);
            double valid = payload.Num("valid", 0.0);
            if (valid > 0.5)
            {
                attentionSum += attentionScore;
                validSamples++;
            }
            attentionText.text = string.Format("注意力  {0:0.00}   质量  {1:0.00}", attentionScore, quality);
        }

        public void SetFatigue(FusionJson payload)
        {
            if (payload == null) return;
            fatigueScore = Read01(payload, "score", fatigueScore);
            state = payload.Str("state", state);
            fatigueDuration = payload.Num("low_duration_s", fatigueDuration);
            fatigueText.text = string.Format("疲劳值  {0:0.00}   {1}", fatigueScore, state);
        }

        public void ShowCognitiveProfile(FusionJson payload)
        {
            double hits = payload.Num("hits", double.NaN);
            double misses = payload.Num("misses", double.NaN);
            double hitRate = payload.Num("hit_rate", double.NaN);
            double avgReaction = payload.Num("avg_reaction_s", double.NaN);
            double attentionMean = payload.Num("attention_mean", double.NaN);
            double fatigueSeconds = payload.Num("fatigue_total_s", double.NaN);
            ShowProfile(hits, misses, hitRate, avgReaction, attentionMean, fatigueSeconds);
        }

        public void ShowSessionResult(int hit, int miss, float rate, float avgReaction)
        {
            ShowProfile(hit, miss, rate, avgReaction, validSamples > 0 ? attentionSum / validSamples : double.NaN, fatigueDuration);
        }

        public void HideProfile()
        {
            if (profileText != null) profileText.gameObject.SetActive(false);
            if (attentionText != null) attentionText.gameObject.SetActive(true);
            if (fatigueText != null) fatigueText.gameObject.SetActive(true);
            validSamples = 0;
            attentionSum = 0.0;
            fatigueDuration = 0.0;
        }

        private void ShowProfile(double hits, double misses, double hitRate, double avgReaction,
            double attentionMean, double fatigueSeconds)
        {
            if (profileText == null) return;
            attentionText.gameObject.SetActive(false);
            fatigueText.gameObject.SetActive(false);
            profileText.gameObject.SetActive(true);
            profileText.text =
                "<size=32><color=#8DA9BA>认知画像</color></size>\n\n" +
                (double.IsNaN(hits) ? "" : $"命中  {hits:0}   漏失  {misses:0}\n") +
                (double.IsNaN(hitRate) ? "" : $"命中率  {hitRate:0.0}%\n") +
                (double.IsNaN(avgReaction) ? "" : $"平均反应时  {avgReaction:0.00} s\n") +
                (double.IsNaN(attentionMean) ? "" : $"平均注意力  {attentionMean:0.00}\n") +
                (double.IsNaN(fatigueSeconds) ? "" : $"低注意持续  {fatigueSeconds:0.0} s\n") +
                $"结束状态  {state}";
        }

        private static float Read01(FusionJson payload, string key, float fallback)
        {
            double value = payload.Num(key, fallback);
            return double.IsNaN(value) || double.IsInfinity(value)
                ? fallback
                : Mathf.Clamp01((float)value);
        }

        private void Build()
        {
            root = new GameObject("VrTelemetryPanel");
            root.transform.SetParent(transform, false);
            attentionText = CreateText("Attention", new Vector3(-0.34f, 0.20f, 0f), 20, TextAnchor.MiddleLeft);
            fatigueText = CreateText("Fatigue", new Vector3(-0.34f, 0.10f, 0f), 20, TextAnchor.MiddleLeft);
            profileText = CreateText("Profile", Vector3.zero, 24, TextAnchor.MiddleCenter);
            profileText.gameObject.SetActive(false);
            attentionText.text = "注意力  --   质量  --";
            fatigueText.text = "疲劳值  --   NORMAL";
        }

        private TextMesh CreateText(string name, Vector3 localPosition, int fontSize, TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = localPosition;
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.fontSize = fontSize;
            mesh.characterSize = 0.008f;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Left;
            mesh.color = new Color(0.72f, 0.95f, 1f, 0.95f);
            mesh.richText = true;
            return mesh;
        }
    }
}
