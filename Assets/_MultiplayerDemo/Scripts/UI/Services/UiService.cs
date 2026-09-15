using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using R3;
using VContainer.Unity;

namespace Game.UI
{
    /// Общее поведение экранов (Ц4): вытеснение в слое Base, глушение ввода, курсор.
    /// Экранов по имени не знает — получает всех зарегистрированных, поэтому новый экран —
    /// это презентер, вид и строка регистрации, а этот класс не трогается.
    ///
    /// Сервис зависит от экранов, а не наоборот: экран сообщает желание через Wanted.
    /// Иначе вышло бы кольцо — сервис просит экраны, экраны просят сервис.
    public sealed class UiService : IStartable, IDisposable
    {
        private readonly IReadOnlyList<IScreen> _screens;
        private readonly IInputGate _gate;
        private readonly ICursor _cursor;
        private readonly List<IScreen> _open = new();

        private DisposableBag _subscriptions;
        private IDisposable _inputBlock;

        public UiService(IReadOnlyList<IScreen> screens, IInputGate gate, ICursor cursor)
        {
            _screens = screens;
            _gate = gate;
            _cursor = cursor;
        }

        /// Сначала всё спрятано, потом открывается то, что хочет: начальное состояние задаёт
        /// сервис, а не галочки, с которыми сцену сохранили после вёрстки.
        public void Start()
        {
            foreach (var screen in _screens)
            {
                screen.Hide();
            }

            foreach (var screen in _screens)
            {
                var captured = screen;
                screen.Wanted.Subscribe(wanted => OnWanted(captured, wanted)).AddTo(ref _subscriptions);
            }
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _inputBlock?.Dispose();
            _inputBlock = null;
        }

        private void OnWanted(IScreen screen, bool wanted)
        {
            if (wanted)
            {
                Open(screen);
            }
            else
            {
                Close(screen);
            }

            ApplyPolicies();
        }

        /// В слое Base одновременно открыт один экран: новый вытесняет прежний.
        private void Open(IScreen screen)
        {
            if (_open.Contains(screen)) return;

            if (screen.Layer == ScreenLayer.Base)
            {
                foreach (var other in _open.Where(open => open.Layer == ScreenLayer.Base).ToArray())
                {
                    Close(other);
                }
            }

            _open.Add(screen);
            screen.Show();
        }

        private void Close(IScreen screen)
        {
            if (_open.Remove(screen)) screen.Hide();
        }

        private void ApplyPolicies()
        {
            var captures = _open.Any(screen => screen.CapturesInput);

            if (captures && _inputBlock == null)
            {
                _inputBlock = _gate.Block();
            }
            else if (!captures && _inputBlock != null)
            {
                _inputBlock.Dispose();
                _inputBlock = null;
            }

            _cursor.SetFree(_open.Any(screen => screen.NeedsCursor));
        }
    }
}
