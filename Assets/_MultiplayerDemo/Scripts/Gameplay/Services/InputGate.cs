using System;
using Game.Core;

namespace Game.Gameplay
{
    public sealed class InputGate : IInputGate
    {
        private int _blocks;

        public bool IsBlocked => _blocks > 0;

        public IDisposable Block()
        {
            _blocks++;
            return new Token(this);
        }

        /// Повторный Dispose одного токена не должен отпускать чужую блокировку.
        private sealed class Token : IDisposable
        {
            private InputGate _gate;

            public Token(InputGate gate)
            {
                _gate = gate;
            }

            public void Dispose()
            {
                if (_gate == null) return;

                _gate._blocks--;
                _gate = null;
            }
        }
    }
}
