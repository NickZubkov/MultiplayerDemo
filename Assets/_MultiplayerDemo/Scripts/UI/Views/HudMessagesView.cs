using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class HudMessagesView : MonoBehaviour, IHudMessages
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float holdSeconds = 3f;

        private float _hideAt;

        public void Show(string message)
        {
            label.text = message;
            label.gameObject.SetActive(true);
            _hideAt = Time.time + holdSeconds;
        }

        private void Update()
        {
            if (label.gameObject.activeSelf && Time.time >= _hideAt)
            {
                label.gameObject.SetActive(false);
            }
        }
    }
}
