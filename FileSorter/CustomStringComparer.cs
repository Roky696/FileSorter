namespace FileSorter
{
    public class CustomStringComparer : IComparer<string>
    {
        private const char _delimiter = '.';

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (string.IsNullOrEmpty(x)) return 1;
            if (string.IsNullOrEmpty(y)) return -1;

            ReadOnlySpan<char> spanX = x.AsSpan();
            ReadOnlySpan<char> spanY = y.AsSpan();

            var indexX = spanX.IndexOf(_delimiter);
            var indexY = spanY.IndexOf(_delimiter);

            var r = spanX[(indexX + 1)..].CompareTo(spanY[(indexY + 1)..], StringComparison.Ordinal);

            if (r == 0 && int.TryParse(spanX[..indexX], out int valX) && int.TryParse(spanY[..indexY], out int valY))
            {
                return valX.CompareTo(valY);
            }

            return r;
        }
    }
}