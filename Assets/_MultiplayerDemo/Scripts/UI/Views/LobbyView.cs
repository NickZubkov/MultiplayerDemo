using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Net;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class LobbyView : MonoBehaviour, ILobbyView
    {
        [SerializeField] private TMP_InputField _playerName;
        [SerializeField] private TMP_InputField _manualAddress;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinManualButton;
        [SerializeField] private Button _backToStacksButton;
        [SerializeField] private Button _hostEntryTemplate;
        [SerializeField] private Transform _hostListRoot;
        [SerializeField] private Button _arenaEntryTemplate;
        [SerializeField] private Transform _arenaListRoot;
        [SerializeField] private TMP_Text _emptyListHint;
        [SerializeField] private TMP_Text _manualHint;
        [SerializeField] private TMP_Text _hostButtonLabel;

        /// Всё, что про подключение: у «Без сети» подключаться не к кому, и этого на экране
        /// быть не должно.
        [SerializeField] private GameObject[] _joinOnly;

        private readonly Subject<string> _hostRequested = new();
        private readonly Subject<HostEntry> _joinRequested = new();
        private readonly Subject<string> _manualJoinRequested = new();
        private readonly Subject<ArenaDefinition> _arenaChosen = new();
        private readonly Subject<Unit> _backToStacksRequested = new();
        private readonly List<ArenaRow> _arenaRows = new();
        private readonly Dictionary<string, Button> _hostRows = new();

        public Observable<string> HostRequested => _hostRequested;
        public Observable<HostEntry> JoinRequested => _joinRequested;
        public Observable<string> ManualJoinRequested => _manualJoinRequested;
        public Observable<ArenaDefinition> ArenaChosen => _arenaChosen;
        public Observable<Unit> BackToStacksRequested => _backToStacksRequested;

        private void Awake()
        {
            _hostButton.onClick.AddListener(() => _hostRequested.OnNext(_playerName.text));
            _backToStacksButton.onClick.AddListener(() => _backToStacksRequested.OnNext(Unit.Default));

            /// Текст уходит как есть: разбирать его будет стек — у LAN это адрес,
            /// у Fusion имя сессии.
            _joinManualButton.onClick.AddListener(() => _manualJoinRequested.OnNext(_manualAddress.text));

            /// Шаблоны строк и подсказку гасим кодом, а не галочками в сцене: панель верстают
            /// включённой, и забытая галочка вылезла бы игроку лишней строкой. Сам экран
            /// здесь не трогаем — начальное состояние задаёт UiService.
            _hostEntryTemplate.gameObject.SetActive(false);
            _arenaEntryTemplate.gameObject.SetActive(false);
            _emptyListHint.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _hostRequested.Dispose();
            _joinRequested.Dispose();
            _manualJoinRequested.Dispose();
            _arenaChosen.Dispose();
            _backToStacksRequested.Dispose();
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        /// Строки обновляются по разнице, а не пересобираются: хосты приходят и уходят по TTL
        /// постоянно, и пересборка съедала бы клик по строке в момент обновления (И-21).
        public void ShowHosts(IReadOnlyList<HostRow> hosts)
        {
            var alive = new HashSet<string>();

            foreach (var host in hosts)
            {
                var token = host.Entry.JoinToken;
                alive.Add(token);

                if (!_hostRows.TryGetValue(token, out var row))
                {
                    row = Instantiate(_hostEntryTemplate, _hostListRoot);
                    row.gameObject.SetActive(true);
                    _hostRows[token] = row;
                }

                row.GetComponentInChildren<TMP_Text>().text = host.Label;
                row.onClick.RemoveAllListeners();
                var captured = host.Entry;
                row.onClick.AddListener(() => _joinRequested.OnNext(captured));
            }

            foreach (var token in _hostRows.Keys.Where(token => !alive.Contains(token)).ToArray())
            {
                Destroy(_hostRows[token].gameObject);
                _hostRows.Remove(token);
            }

            _emptyListHint.gameObject.SetActive(hosts.Count == 0);
        }

        public void ShowArenas(IReadOnlyList<ArenaDefinition> arenas)
        {
            _arenaRows.Clear();

            foreach (Transform child in _arenaListRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (var arena in arenas)
            {
                var row = Instantiate(_arenaEntryTemplate, _arenaListRoot);
                var label = row.GetComponentInChildren<TMP_Text>();
                label.text = arena.DisplayName;
                var captured = arena;
                row.onClick.AddListener(() => _arenaChosen.OnNext(captured));
                row.gameObject.SetActive(true);
                _arenaRows.Add(new ArenaRow(arena, label));
            }
        }

        /// Отметка текстом, а не цветом: цвет пришлось бы держать в двух состояниях
        /// и сверять с темой кнопки, а стрелка читается на любой.
        public void MarkArena(ArenaDefinition arena)
        {
            foreach (var row in _arenaRows)
            {
                row.Label.text = row.Arena == arena ? "▸ " + row.Arena.DisplayName : row.Arena.DisplayName;
            }
        }

        public void SetEmptyHint(string text) => _emptyListHint.text = text;

        public void SetManualHint(string text) => _manualHint.text = text;

        public void SetHostLabel(string text) => _hostButtonLabel.text = text;

        /// Прячется всё, что про подключение: у «Без сети» подключаться не к кому.
        public void SetJoinVisible(bool visible)
        {
            foreach (var go in _joinOnly)
            {
                go.SetActive(visible);
            }
        }

        public void SetInteractable(bool interactable)
        {
            _hostButton.interactable = interactable;
            _joinManualButton.interactable = interactable;
            _backToStacksButton.interactable = interactable;

            foreach (var row in _hostRows.Values)
            {
                row.interactable = interactable;
            }
        }

        private readonly struct ArenaRow
        {
            public readonly ArenaDefinition Arena;
            public readonly TMP_Text Label;

            public ArenaRow(ArenaDefinition arena, TMP_Text label)
            {
                Arena = arena;
                Label = label;
            }
        }
    }
}
