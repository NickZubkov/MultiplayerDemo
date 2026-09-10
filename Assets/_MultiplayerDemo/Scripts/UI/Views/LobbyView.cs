using System;
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
        [SerializeField] private Button backToStacksButton;
        [SerializeField] private Button hostEntryTemplate;
        [SerializeField] private Transform hostListRoot;
        [SerializeField] private Button arenaEntryTemplate;
        [SerializeField] private Transform arenaListRoot;
        [SerializeField] private TMP_Text emptyListHint;
        [SerializeField] private TMP_Text manualHint;

        private readonly Subject<string> _hostRequested = new();
        private readonly Subject<HostEntry> _joinRequested = new();
        private readonly Subject<ArenaDefinition> _arenaChosen = new();
        private readonly Subject<Unit> _backToStacksRequested = new();
        private readonly List<ArenaRow> _arenaRows = new();

        private IReadOnlyList<ArenaDefinition> _arenas = Array.Empty<ArenaDefinition>();

        public Observable<string> HostRequested => _hostRequested;
        public Observable<HostEntry> JoinRequested => _joinRequested;
        public Observable<ArenaDefinition> ArenaChosen => _arenaChosen;
        public Observable<Unit> BackToStacksRequested => _backToStacksRequested;

        private void Awake()
        {
            hostButton.onClick.AddListener(() => _hostRequested.OnNext(playerName.text));
            backToStacksButton.onClick.AddListener(() => _backToStacksRequested.OnNext(Unit.Default));

            /// Уровень пустой: маяка не было, и какой уровень у того хоста — неизвестно.
            /// Презентер подставит отмеченный в лобби.
            joinManualButton.onClick.AddListener(() =>
                _joinRequested.OnNext(new HostEntry("вручную", 0, 0, "manual", manualAddress.text, null)));

            /// Начальное состояние задаём кодом, а не галочками в сцене: панель верстают
            /// включённой, шаблоны строк остаются как придётся, и любая забытая галочка
            /// вылезла бы игроку лишней строкой или экраном поверх выбора стека. Лобби
            /// покажет презентер, когда сцена стека загрузится.
            hostEntryTemplate.gameObject.SetActive(false);
            arenaEntryTemplate.gameObject.SetActive(false);
            emptyListHint.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _hostRequested.Dispose();
            _joinRequested.Dispose();
            _arenaChosen.Dispose();
            _backToStacksRequested.Dispose();
        }

        public void Show() => SetVisible(true);

        public void Hide() => SetVisible(false);

        /// В строке хоста стоит его уровень: клиент грузит именно тот, что выбрал хост,
        /// и должен видеть, куда идёт.
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
                row.GetComponentInChildren<TMP_Text>().text =
                    $"{host.Name} — {host.Players}/{host.MaxPlayers} — {ArenaName(host.ArenaId)}";
                var captured = host;
                row.onClick.AddListener(() => _joinRequested.OnNext(captured));
                row.gameObject.SetActive(true);
            }
        }

        public void ShowArenas(IReadOnlyList<ArenaDefinition> arenas)
        {
            _arenas = arenas;
            _arenaRows.Clear();

            foreach (Transform child in arenaListRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (var arena in arenas)
            {
                var row = Instantiate(arenaEntryTemplate, arenaListRoot);
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

        public void SetEmptyHint(string text) => emptyListHint.text = text;

        public void SetManualHint(string text) => manualHint.text = text;

        /// В маяке едет короткий id, а игрок читает название. Чужой id — это сборка
        /// с другим набором уровней: подключиться к ней всё равно не выйдет.
        private string ArenaName(string arenaId)
        {
            if (string.IsNullOrEmpty(arenaId)) return "уровень неизвестен";

            foreach (var arena in _arenas)
            {
                if (arena.ArenaId == arenaId) return arena.DisplayName;
            }

            return "чужой уровень";
        }

        /// Показывает и прячет этот экран презентер из сцены стека — он умирает вместе
        /// с ней и гасит лобби за собой, в том числе при выходе из Play Mode, когда сам
        /// вид Unity уже могла снести. Порядок разрушения между сценами не обещан никем,
        /// поэтому вид обязан пережить обращение к себе после смерти.
        private void SetVisible(bool visible)
        {
            if (this == null) return;

            gameObject.SetActive(visible);
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
