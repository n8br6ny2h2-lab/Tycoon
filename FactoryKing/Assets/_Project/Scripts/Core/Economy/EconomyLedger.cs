using System;

namespace FactoryKing.Core.Economy
{
    public enum LedgerCategory
    {
        Revenue = 0,
        RawMaterials = 1,
        Electricity = 2,
        Wages = 3,
        Maintenance = 4,
    }

    /// <summary>
    /// Rolling-window income statement (1-second buckets) used for "income per second", "per minute production" and
    /// "hourly profit" displays. Values are doubles because they are presentation statistics, not balances.
    /// </summary>
    public sealed class EconomyLedger
    {
        public const int CategoryCount = 5;

        private readonly int _window;
        private readonly double[,] _amounts;
        private readonly double[] _unitsSold;
        private int _head;
        private double _bucketTime;
        private double _elapsed;

        public EconomyLedger(int windowSeconds = 60)
        {
            if (windowSeconds < 1) throw new ArgumentOutOfRangeException(nameof(windowSeconds));
            _window = windowSeconds;
            _amounts = new double[windowSeconds, CategoryCount];
            _unitsSold = new double[windowSeconds];
        }

        public int WindowSeconds => _window;

        public void Record(LedgerCategory category, double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount)) return;
            _amounts[_head, (int)category] += amount;
        }

        public void RecordUnitsSold(long units) => _unitsSold[_head] += units;

        public void Advance(double dt)
        {
            if (dt <= 0 || double.IsNaN(dt)) return;
            _elapsed += dt;
            _bucketTime += dt;
            // A huge dt (e.g. after resume) clears the whole window at most once.
            int rotations = 0;
            while (_bucketTime >= 1d && rotations <= _window)
            {
                _bucketTime -= 1d;
                _head = (_head + 1) % _window;
                for (int c = 0; c < CategoryCount; c++) _amounts[_head, c] = 0;
                _unitsSold[_head] = 0;
                rotations++;
            }
            if (_bucketTime >= 1d) _bucketTime %= 1d;
        }

        private double Span => Math.Max(1d, Math.Min(_elapsed, _window));

        public double PerSecond(LedgerCategory category)
        {
            double sum = 0;
            for (int i = 0; i < _window; i++) sum += _amounts[i, (int)category];
            return sum / Span;
        }

        public double RevenuePerSecond => PerSecond(LedgerCategory.Revenue);

        public double CostsPerSecond =>
            PerSecond(LedgerCategory.RawMaterials) + PerSecond(LedgerCategory.Electricity) +
            PerSecond(LedgerCategory.Wages) + PerSecond(LedgerCategory.Maintenance);

        public double NetPerSecond => RevenuePerSecond - CostsPerSecond;
        public double NetPerHour => NetPerSecond * 3600d;

        public double UnitsSoldPerMinute
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < _window; i++) sum += _unitsSold[i];
                return sum / Span * 60d;
            }
        }

        public void Reset()
        {
            Array.Clear(_amounts, 0, _amounts.Length);
            Array.Clear(_unitsSold, 0, _unitsSold.Length);
            _head = 0;
            _bucketTime = 0;
            _elapsed = 0;
        }
    }
}
