using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuroPilotXR.Navigation
{
    public sealed class TrainingRoomMenu : MonoBehaviour
    {
        private bool leaving;
        public void ReturnToModes()
        {
            if (leaving) return;
            leaving = true;
            TrainingSession.ReturnToModes = true;
            SceneManager.LoadSceneAsync("NeuroPilotNavigation");
        }
    }
}
