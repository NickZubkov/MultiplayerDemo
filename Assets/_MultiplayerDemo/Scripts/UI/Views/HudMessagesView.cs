using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class HudMessagesView : MonoBehaviour, IHudMessages
    {
        [SerializeField] private Text label;
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
