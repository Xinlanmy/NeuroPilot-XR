using System.Collections;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    public sealed class SuccessReward : MonoBehaviour
    {
        public Material starMaterial;
        [Range(0, 1)] public float volume = .22f;
        public bool soundEnabled = true;
        private Mesh star;
        private AudioClip chime;
        public int PlayCount { get; private set; }

        private void Awake()
        {
            star = new Mesh { name = "Reward five-point star" };
            var vertices = new Vector3[11]; var triangles = new int[60];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI * .5f + i * Mathf.PI / 5f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (i % 2 == 0 ? .5f : .22f);
                int next = (i + 1) % 10 + 1, k = i * 6;
                triangles[k] = triangles[k + 3] = 0;
                triangles[k + 1] = triangles[k + 5] = i + 1;
                triangles[k + 2] = triangles[k + 4] = next;
            }
            star.vertices = vertices; star.triangles = triangles; star.RecalculateNormals(); star.RecalculateBounds();
            const int rate = 24000;
            var samples = new float[(int)(rate * .38f)];
            float[] notes = { 1046.5f, 1318.5f, 1568f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                for (int n = 0; n < notes.Length; n++)
                {
                    float phase = t - n * .045f;
                    if (phase < 0) continue;
                    float envelope = Mathf.Min(1, phase / .006f) * Mathf.Exp(-phase * 17f) * Mathf.Clamp01((.38f - t) / .03f);
                    samples[i] += Mathf.Sin(phase * notes[n] * 2 * Mathf.PI) * envelope * .25f;
                }
            }
            chime = AudioClip.Create("Soft reward chime", samples.Length, 1, rate, false);
            chime.SetData(samples, 0);
        }
        public void Play(Vector3 position)
        {
            if (!isActiveAndEnabled || starMaterial == null) return;
            PlayCount++; StartCoroutine(Burst(position));
        }
        public void PlayGaze(Vector3 position, Color color, float diameter)
        {
            if (!isActiveAndEnabled || starMaterial == null) return;
            PlayCount++; StartCoroutine(GazeFlash(position, color, diameter));
        }
        private IEnumerator GazeFlash(Vector3 position, Color color, float diameter)
        {
            var root = new GameObject("Gaze Confirmation Flash");
            root.transform.SetParent(transform, true);
            root.transform.position = position;
            if (soundEnabled)
            {
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = .65f; source.minDistance = 3; source.maxDistance = 20;
                source.volume = volume; source.clip = chime; source.Play();
            }
            var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "Gaze Dot Flash";
            flash.transform.SetParent(root.transform, false);
            flash.GetComponent<Collider>().enabled = false;
            Destroy(flash.GetComponent<Collider>());
            var material = new Material(starMaterial);
            flash.GetComponent<Renderer>().sharedMaterial = material;
            const float flashTime = .24f;
            float elapsed = 0f;
            while (elapsed < flashTime)
            {
                float progress = Mathf.Clamp01(elapsed / flashTime);
                flash.transform.localScale = Vector3.one * diameter *
                    (progress < .35f ? Mathf.Lerp(1f, 1.45f, progress / .35f) :
                    Mathf.Lerp(1.45f, 0f, (progress - .35f) / .65f));
                material.color = Color.Lerp(Color.white, color, progress);
                elapsed += Time.deltaTime;
                yield return null;
            }
            flash.GetComponent<Renderer>().enabled = false;
            yield return new WaitForSeconds(.38f - flashTime);
            Destroy(material);
            Destroy(root);
        }
        private IEnumerator Burst(Vector3 position)
        {
            var root = new GameObject("Success Star Burst");
            root.transform.SetParent(transform, true); root.transform.position = position;
            if (soundEnabled)
            {
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = .65f; source.minDistance = 3; source.maxDistance = 20;
                source.volume = volume; source.clip = chime; source.Play();
            }
            var pieces = new Transform[12]; var velocities = new Vector3[12];
            var facing = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
            for (int i = 0; i < pieces.Length; i++)
            {
                var obj = new GameObject("Star", typeof(MeshFilter), typeof(MeshRenderer));
                obj.GetComponent<MeshFilter>().sharedMesh = star; obj.GetComponent<MeshRenderer>().sharedMaterial = starMaterial;
                pieces[i] = obj.transform; pieces[i].SetParent(root.transform, false); pieces[i].rotation = facing;
                velocities[i] = Random.onUnitSphere * Random.Range(.3f, .75f) + Vector3.up * .15f;
            }
            float elapsed = 0;
            while (elapsed < .55f)
            {
                elapsed += Time.deltaTime;
                float scale = .12f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(elapsed / .55f));
                for (int i = 0; i < pieces.Length; i++)
                {
                    pieces[i].localPosition = velocities[i] * elapsed + Vector3.down * elapsed * elapsed * .2f;
                    pieces[i].localScale = Vector3.one * scale;
                    pieces[i].Rotate(0, 0, (i % 2 == 0 ? 150 : -150) * Time.deltaTime);
                }
                yield return null;
            }
            Destroy(root);
        }
        private void OnDestroy() { if (star != null) Destroy(star); if (chime != null) Destroy(chime); }
    }
}
