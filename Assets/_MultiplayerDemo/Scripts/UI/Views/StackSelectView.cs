using System.Collections.Generic;
using Game.Net;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class StackSelectView : MonoBehaviour, IStackSelectView
    {
        [SerializeField] private Button _buttonTemplate;
        [SerializeField] private Transform _buttonRoot;

        private readonly Subject<NetworkStackDefinition> _chosen = new();

        public Observable<NetworkStackDefinition> StackChosen => _chosen;

        /// Шаблон кнопки гасим кодом, а не галочкой в сцене: оставленный включённым, он
        /// встал бы в список лишней кнопкой без стека. Сам экран здесь не трогаем:
        /// начальное состояние задаёт UiService, а презентер показывает этот экран
        /// в фазе SelectingStack.
        private void Awake() => _buttonTemplate.gameObject.SetActive(false);

        private void OnDestroy() => _chosen.Dispose();

        public void Show(IReadOnlyList<NetworkStackDefinition> stacks)
        {
            gameObject.SetActive(true);

            foreach (Transform child in _buttonRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (var stack in stacks)
            {
                var button = Instantiate(_buttonTemplate, _buttonRoot);
                button.GetComponentInChildren<TMP_Text>().text = stack.DisplayName;
                var captured = stack;
                button.onClick.AddListener(() => _chosen.OnNext(captured));
                button.gameObject.SetActive(true);
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
