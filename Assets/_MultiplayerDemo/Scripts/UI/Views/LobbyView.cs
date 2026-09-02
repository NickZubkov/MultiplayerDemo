using System.Collections.Generic;
using Game.Core;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class LobbyView : MonoBehaviour, ILobbyView
    {
        [SerializeField] private TMP_InputField playerName;
        [SerializeField] private TMP_InputField manualAddress;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinManualButton;
        [SerializeField] private Button hostEntryTemplate;
        [SerializeField] private Transform hostListRoot;
        [SerializeField] private TMP_Text emptyListHint;

        private readonly Subject<string> _hostRequested = new();
        private readonly Subject<HostEntry> _joinRequested = new();

        public Observable<string> HostRequested => _hostRequested;
        public Observable<HostEntry> JoinRequested => _joinRequested;

        private void Awake()
        {
            hostButton.onClick.AddListener(() => _hostRequested.OnNext(playerName.text));
            joinManualButton.onClick.AddListener(() =>
                _joinRequested.OnNext(new HostEntry("вручную", 0, 0, "manual", manualAddress.text)));
        }

        private void OnDestroy()
        {
            _hostRequested.Dispose();
            _joinRequested.Dispose();
        }

        /// Ручной ввод адреса — план Б: широковещание режется гостевым Wi-Fi,
        /// и остаться без способа подключиться на собеседовании недопустимо.
        public void ShowHosts(IReadOnlyList<HostEntry> hosts)
        {
            foreach (Transform child in hostListRoot)
            {
                Destroy(child.gameObject);
            }

            emptyListHint.gameObject.SetActive(hosts.Count == 0);

            foreach (var host in hosts)
            {
                var row = Instantiate(hostEntryTemplate, hostListRoot);
                row.GetComponentInChildren<TMP_Text>().text = $"{host.Name} — {host.Players}/{host.MaxPlayers}";
                var captured = host;
                row.onClick.AddListener(() => _joinRequested.OnNext(captured));
                row.gameObject.SetActive(true);
            }
        }

        public void SetEmptyHint(string text) => emptyListHint.text = text;
    }
}
