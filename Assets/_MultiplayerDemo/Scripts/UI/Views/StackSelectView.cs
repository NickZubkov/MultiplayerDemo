using System.Collections.Generic;
using Game.Core;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class StackSelectView : MonoBehaviour, IStackSelectView
    {
        [SerializeField] private Button buttonTemplate;
        [SerializeField] private Transform buttonRoot;

        private readonly Subject<NetworkStackDefinition> _chosen = new();

        public Observable<NetworkStackDefinition> StackChosen => _chosen;

        private void OnDestroy() => _chosen.Dispose();

        public void Show(IReadOnlyList<NetworkStackDefinition> stacks)
        {
            gameObject.SetActive(true);

            foreach (Transform child in buttonRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (var stack in stacks)
            {
                var button = Instantiate(buttonTemplate, buttonRoot);
                button.GetComponentInChildren<TMP_Text>().text = stack.DisplayName;
                var captured = stack;
                button.onClick.AddListener(() => _chosen.OnNext(captured));
                button.gameObject.SetActive(true);
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
