using System;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Economy
{
    /// <summary>
    /// All cash/diamond mutations go through here. Purchases require real cash; operating costs never block
    /// production — if cash runs out they become <see cref="GameState.Debt"/>, repaid first from the next income.
    /// This guarantees the factory can never soft-lock at zero cash.
    /// </summary>
    public sealed class Wallet
    {
        private readonly GameState _state;

        public Wallet(GameState state) => _state = state ?? throw new ArgumentNullException(nameof(state));

        public event Action CashChanged;
        public event Action DiamondsChanged;

        public BigDouble Cash => _state.Cash;
        public BigDouble Debt => _state.Debt;
        public long Diamonds => _state.Diamonds;

        public bool CanAfford(BigDouble amount) => amount <= _state.Cash;

        /// <summary>Income. Repays debt first.</summary>
        public void Earn(BigDouble amount)
        {
            if (!amount.IsPositive) return;
            _state.LifetimeEarnings += amount;
            _state.RunEarnings += amount;
            if (_state.Debt.IsPositive)
            {
                if (amount >= _state.Debt)
                {
                    amount -= _state.Debt;
                    _state.Debt = BigDouble.Zero;
                }
                else
                {
                    _state.Debt -= amount;
                    amount = BigDouble.Zero;
                }
            }
            _state.Cash += amount;
            CashChanged?.Invoke();
        }

        /// <summary>Operating cost. Never fails; shortfall becomes debt.</summary>
        public void PayOperating(BigDouble amount)
        {
            if (!amount.IsPositive) return;
            if (_state.Cash >= amount)
            {
                _state.Cash -= amount;
            }
            else
            {
                _state.Debt += amount - _state.Cash;
                _state.Cash = BigDouble.Zero;
            }
            CashChanged?.Invoke();
        }

        /// <summary>Purchase (upgrade, unlock). Only succeeds with enough cash.</summary>
        public bool TrySpend(BigDouble amount)
        {
            if (amount.IsNegative) return false;
            if (_state.Cash < amount) return false;
            _state.Cash -= amount;
            CashChanged?.Invoke();
            return true;
        }

        public void AddDiamonds(long amount)
        {
            if (amount <= 0) return;
            _state.Diamonds = checked(_state.Diamonds + amount);
            DiamondsChanged?.Invoke();
        }

        /// <summary>Cash reward (missions, orders, ads...). Counts as earnings.</summary>
        public void Grant(BigDouble amount) => Earn(amount);

        public bool TrySpendDiamonds(long amount)
        {
            if (amount < 0 || _state.Diamonds < amount) return false;
            _state.Diamonds -= amount;
            DiamondsChanged?.Invoke();
            return true;
        }
    }
}
