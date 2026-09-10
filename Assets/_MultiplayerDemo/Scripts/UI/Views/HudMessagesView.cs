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

        /// Метку гасим кодом, а не галочкой в сцене: после вёрстки она нередко остаётся
        /// включённой, и игрок увидел бы в HUD текст-заглушку.
        private void Awake() => label.gameObject.SetActive(false);

        /// Сообщения приходят из сетевых сервисов, а те живут в другой сцене и завершают
        /// свои операции когда придётся — в том числе в момент, когда Unity уже снёс этот
        /// HUD. Порядок разрушения между сценами не обещан никем, поэтому вид обязан
        /// пережить обращение к себе после смерти, а не полагаться на дисциплину звонящих.
        public void Show(string message)
        {
            if (label == null) return;

            label.text = message;
            label.gameObject.SetActive(true);
            _hideAt = Time.time + holdSeconds;
        }

        private void Update()
        {
            if (label == null) return;

            if (label.gameObject.activeSelf && Time.time >= _hideAt)
            {
                label.gameObject.SetActive(false);
            }
        }
    }
}
