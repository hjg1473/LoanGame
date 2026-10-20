namespace LoadGame.Economy
{
    public readonly struct StockCandle
    {
        public readonly double Open;
        public readonly double High;
        public readonly double Low;
        public readonly double Close;

        public StockCandle(double open, double high, double low, double close)
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }
    }
}
