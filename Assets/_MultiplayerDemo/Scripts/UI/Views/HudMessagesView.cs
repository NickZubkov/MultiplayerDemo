using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public sealed class HudMessagesView : MonoBehaviour, IHudMessages
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _holdSeconds = 3f;

        private float _hideAt;

        /// Метку гасим кодом, а не галочкой в сцене: после вёрстки она нередко остаётся
        /// включённой, и игрок увидел бы в HUD текст-заглушку.
        private void Awake() => _label.gameObject.SetActive(false);

        /// Сообщения приходят из сетевых сервисов, а те живут в другой сцене и завершают
        /// свои операции когда придётся — в том числе в момент, когда Unity уже снёс этот
        /// HUD. Порядок разрушения между сценами не обещан никем, поэтому вид обязан
        /// пережить обращение к себе после смерти, а не полагаться на дисциплину звонящих.
        public void Show(string message)
        {
            if (_label == null) return;

            _label.text = message;
            _label.gameObject.SetActive(true);
            _hideAt = Time.time + _holdSeconds;
        }

        private void Update()
        {
            if (_label == null) return;

            if (_label.gameObject.activeSelf && Time.time >= _hideAt)
            {
                _label.gameObject.SetActive(false);
            }
        }
    }
}
