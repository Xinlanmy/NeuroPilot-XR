using System;
using UnityEngine;
using NeuroPilotXR.Navigation;

namespace NeuroPilotXR.Training
{
    /// <summary>
    /// 装配式传输桥（issue #4 P0-1）：把 v1.2.0 两个 SSVEP 缝接到 FusionLink WebSocket。
    /// 挂在与缝同 GameObject 上（TrainingRoom / MultiTargetRoom 的 SessionManager），
    /// Awake 自举端口引用与 FusionLink；缝文件保持「本地事件、无网络」承诺不动。
    /// 地址源 = CommunicationSettings（设置页保存后免重启生效）；断线期间上行事件按
    /// FusionLink 语义丢弃不补发；每次新连接复位两侧下行 seq 基线。
    /// </summary>
    public sealed class FusionEegBridge : MonoBehaviour
    {
        private FusionLink _link;
        private TrainingEegPort _single;
        private TargetPracticeSession _multi;
        private float _offlineSince = -1f;
        private bool _warnedOffline;
        private Func<long> _nextSequence;

        private void Awake()
        {
            _single = GetComponent<TrainingEegPort>();
            _multi = GetComponent<TargetPracticeSession>();
            _link = GetComponent<FusionLink>();
            if (_link == null)
            {
                _link = gameObject.AddComponent<FusionLink>();
            }

            // 先按共享通信设置定向，避免首连打向 FusionLink 默认的 127.0.0.1。
            // 自举路径时序确定：AddComponent 同步执行 FusionLink.Awake（读 PlayerPrefs）后
            // 返回本行赋值，Endpoint 胜出；FusionLink.Start(autoConnect) 在所有 Awake 之后
            // 才发起首连。Update 的比较仅作设置页改址后的免重启同步兜底。
            _link.ServerUrl = NeuroPilotXR.Navigation.CommunicationSettings.Endpoint;
            _nextSequence = _link.NextSequence;
        }

        private void OnEnable()
        {
            _link.Connected += OnConnected;
            _link.CommandEnvelopeReceived += OnRawMessage;
            if (_single != null)
            {
                _single.SequenceProvider = _nextSequence;
                _single.OutgoingMessage += _link.SendRaw;
            }
            if (_multi != null)
            {
                _multi.SequenceProvider = _nextSequence;
                _multi.OutgoingEvent += _link.SendRaw;
            }
            if (_link.IsConnected)
            {
                OnConnected();
            }
        }

        private void OnDisable()
        {
            _link.Connected -= OnConnected;
            _link.CommandEnvelopeReceived -= OnRawMessage;
            if (_single != null)
            {
                _single.OutgoingMessage -= _link.SendRaw;
                if (_single.SequenceProvider == _nextSequence) _single.SequenceProvider = null;
            }
            if (_multi != null)
            {
                _multi.OutgoingEvent -= _link.SendRaw;
                if (_multi.SequenceProvider == _nextSequence) _multi.SequenceProvider = null;
            }
        }

        private void OnConnected()
        {
            _warnedOffline = false;
            _offlineSince = -1f;
            // 契约：新连接由传输层复位下行 seq 基线（过期确认不得击中新球）。
            if (_single != null) _single.ResetConnectionSequence();
            if (_multi != null) _multi.ResetConnectionSequence();
            Debug.Log("[FusionEegBridge] 已连接 " + _link.ServerUrl + "，下行 seq 基线已复位（单球=" + (_single != null) + " 多球=" + (_multi != null) + "）");
        }

        private void OnRawMessage(string json)
        {
            // FusionLink await 续体回 Unity 主线程同步上下文，此处天然主线程。
            // 两侧都试：TryReceiveCommand 内部校验 type/target_id/seq，不匹配即 false。
            if (_single != null && _single.isActiveAndEnabled && _single.TryReceiveCommand(json))
            {
                return;
            }

            if (_multi != null && _multi.isActiveAndEnabled && _multi.TryReceiveCommand(json))
            {
                return;
            }
        }

        private void Update()
        {
            // 设置页保存新地址后免重启生效（ApplyServerUrl 幂等：相同 URL 直接返回）。
            string endpoint = NeuroPilotXR.Navigation.CommunicationSettings.Endpoint;
            if (_link.ServerUrl != endpoint)
            {
                _link.ApplyServerUrl(endpoint);
            }

            // 3s 失联告警（1.2.0 文档遗留项的最小实现）。
            if (_link.IsConnected)
            {
                _offlineSince = -1f;
                return;
            }

            if (_offlineSince < 0f)
            {
                _offlineSince = Time.unscaledTime;
            }
            else if (!_warnedOffline && Time.unscaledTime - _offlineSince >= 3f)
            {
                _warnedOffline = true;
                Debug.LogWarning("[FusionEegBridge] 融合层失联超 3s（" + _link.ServerUrl + "），上行事件丢弃中；SSVEP 房间等待确认不降级假命中");
            }
        }
    }
}
