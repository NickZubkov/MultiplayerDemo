using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class HudMessagesView : MonoBehaviour, IHudView
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _holdSeconds = 3f;

        private float _hideAt;

        /// Метку гасим кодом, а не галочкой в сцене: после вёрстки она нередко остаётся
        /// включённой, и игрок увидел бы текст-заглушку.
        private void Awake() => _label.gameObject.SetActive(false);

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        public void ShowMessage(string text)
        {
            _label.text = text;
            _label.gameObject.SetActive(true);
            _hideAt = Time.time + _holdSeconds;
        }

        private void Update()
        {
            if (_label.gameObject.activeSelf && Time.time >= _hideAt)
            {
                _label.gameObject.SetActive(false);
            }
        }
    }
}
