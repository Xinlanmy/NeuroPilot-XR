using TMPro;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    public sealed class CommunicationSettingsPage : MonoBehaviour
    {
        public TMP_Text address, status;
        private string draft;
        private void OnEnable() { draft = CommunicationSettings.Host; Refresh(); }
        public void Append(string value)
        {
            if (draft.Length + value.Length > 15) return;
            foreach (char c in value) if ((c < '0' || c > '9') && c != '.') return;
            draft += value; Refresh();
        }
        public void Backspace() { if (draft.Length > 0) draft = draft.Substring(0, draft.Length - 1); Refresh(); }
        public void Clear() { draft = ""; Refresh(); }
        public void RestoreDefault() { draft = CommunicationSettings.DefaultHost; Refresh(); }
        public void Save()
        {
            if (!CommunicationSettings.TrySave(draft))
            {
                status.text = "地址格式有误，请输入完整 IPv4 地址";
                status.color = new Color(1, .65f, .4f); return;
            }
            draft = CommunicationSettings.Host; Refresh();
            status.text = "已保存 · 单球 / 多球共用 · 重启后保留";
        }
        private void Refresh()
        {
            if (address == null || status == null) return;
            address.text = string.IsNullOrEmpty(draft) ? "请输入 IP 地址" : draft;
            status.color = new Color(.5f, .85f, 1);
            status.text = "当前已保存：" + CommunicationSettings.Host + "  ·  端口 " + CommunicationSettings.Port;
        }
    }
}
