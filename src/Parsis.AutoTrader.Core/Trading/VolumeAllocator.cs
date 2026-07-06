namespace Parsis.AutoTrader.Core.Trading;

public static class VolumeAllocator
{
    public static (decimal tp1, decimal tp2, decimal tp3) Split(decimal total, decimal step, decimal min)
    {
        if (total <= 0 || step <= 0 || min <= 0) throw new ArgumentOutOfRangeException();
        decimal Round(decimal value) => Math.Max(min, Math.Floor(value / step) * step);
        var first = Round(total * 0.50m);
        var remaining = total - first;
        var second = Round(remaining * 0.50m);
        var third = total - first - second;
        if (third < min)
        {
            first = Round(total / 3m);
            second = Round(total / 3m);
            third = total - first - second;
        }
        return (decimal.Round(first, 8), decimal.Round(second, 8), decimal.Round(third, 8));
    }
}
