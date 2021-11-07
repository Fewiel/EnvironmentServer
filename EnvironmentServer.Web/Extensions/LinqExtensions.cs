using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Extensions
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> OrderByNatural<T>(
          this IEnumerable<T> source, string propertyName)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (propertyName == null)
                throw new ArgumentNullException("propertyName");

            try
            {
                return source.OrderBy(x => x.GetReflectedPropertyValue(propertyName), new NaturalSortComparer<T>());
            }
            catch
            { }

            return source;
        }

        public static string GetReflectedPropertyValue(this object subject, string field)
        {
            object reflectedValue = subject.GetType().GetProperty(field).GetValue(subject, null);
            return reflectedValue != null ? reflectedValue.ToString() : "";
        }
    }

    public class NaturalSortComparer<T> : IComparer<string>, IDisposable
    {
        /// <summary>
        /// Regular expression used to identify which part of the string may be a number (decimal).
        /// </summary>
        private const string RegexString = @"([0-9]+(\.[0-9]+)?([Ee][+-]?[0-9]+)?)";

        /// <summary>
        /// Type of comparaison used to compare strings between them
        /// </summary>
        private static StringComparison StringComparison = StringComparison.Ordinal;

        private Dictionary<string, string[]> table = new Dictionary<string, string[]>();
        private readonly bool isAscending;

        public NaturalSortComparer(bool isAscendingOrder = true)
        {
            this.isAscending = isAscendingOrder;
        }

        int IComparer<string>.Compare(string x, string y)
        {
            if (x == y)
                return 0;

            string[] x1, y1;

            if (!table.TryGetValue(x, out x1))
            {
                x1 = Regex.Split(x.Replace(" ", ""), RegexString);
                table.Add(x, x1);
            }

            if (!table.TryGetValue(y, out y1))
            {
                y1 = Regex.Split(y.Replace(" ", ""), RegexString);
                table.Add(y, y1);
            }

            int returnVal;

            for (int i = 0; i < x1.Length && i < y1.Length; i++)
            {
                if (x1[i] != y1[i])
                {
                    returnVal = PartCompare(x1[i], y1[i]);
                    return isAscending ? returnVal : -returnVal;
                }
            }

            if (y1.Length > x1.Length)
            {
                returnVal = 1;
            }
            else if (x1.Length > y1.Length)
            {
                returnVal = -1;
            }
            else
            {
                returnVal = 0;
            }

            return isAscending ? returnVal : -returnVal;
        }

        private static int PartCompare(string left, string right)
        {
            decimal x, y;

            if (!decimal.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out x))
                return String.Compare(left, right, StringComparison);

            if (!decimal.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out y))
                return String.Compare(left, right, StringComparison);

            return x.CompareTo(y);
        }

        public void Dispose()
        {
            table.Clear();
            table = null;
        }
    }
}
