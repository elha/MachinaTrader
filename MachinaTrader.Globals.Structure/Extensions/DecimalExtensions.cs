using System;
using System.Collections.Generic;
using System.Linq;

namespace MachinaTrader.Globals.Structure.Extensions
{
	public static class DecimalExtensions
	{
		public static List<decimal> Lowest(this List<decimal> source, int length)
		{
			var result = new List<decimal>();

			for (int i = 1; i <= source.Count; i++)
			{
				if (i < length)
				{
					result.Add(source.Take(i).Min());
				}
				else
				{
					result.Add(source.Skip(i - length).Take(length).Min());
				}
			}

			return result;
		}

		public static List<decimal?> Lowest(this List<decimal?> source, int length)
		{
			var result = new List<decimal?>();

			for (int i = 1; i <= source.Count; i++)
			{
				if (i < length)
				{
					result.Add(source.Take(i).Min());
				}
				else
				{
					result.Add(source.Skip(i - length).Take(length).Min());
				}
			}

			return result;
		}

		public static List<decimal> Avg(this List<decimal> source, int length)
		{
			var result = new List<decimal>();

			for (int i = 1; i <= source.Count; i++)
			{
				if (i < length)
				{
					result.Add(source.Take(i).Average());
				}
				else
				{
					result.Add(source.Skip(i - length).Take(length).Average());
				}
			}

			return result;
		}

		public static List<decimal?> Avg(this List<decimal?> source, int length)
		{
			var result = new List<decimal?>();

			for (int i = 1; i <= source.Count; i++)
			{
				if (i < length)
				{
					result.Add(source.Take(i).Average());
				}
				else
				{
					result.Add(source.Skip(i - length).Take(length).Average());
				}
			}

			return result;
		}

		public static List<decimal> Highest(this List<decimal> source, int length)
		{
			var result = new List<decimal>();

			for (int i = 1; i <= source.Count; i++)
			{
				if (i < length)
				{
					result.Add(source.Take(i).Max());
				}
				else
				{
					result.Add(source.Skip(i - length).Take(length).Max());
				}
			}

			return result;
		}

		public static List<decimal?> Highest(this List<decimal?> source, int length)
		{
			var result = new List<decimal?>();

			for (int i = 1; i <= source.Count; i++)
			{
				if (i < length)
				{
					result.Add(source.Take(i).Max());
				}
				else
				{
					result.Add(source.Skip(i - length).Take(length).Max());
				}
			}

			return result;
		}

        public static decimal Median(this List<decimal> source)
        {
            var aNew = new List<decimal>(source);
            aNew.Sort();
            return aNew[aNew.Count/2];
        }

        #region crossunders

        public static List<bool> Crossunder(this List<decimal?> source, decimal value)
		{
			var result = new List<bool>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i == 0)
					result.Add(false);
				else
				{
					result.Add(source[i] < value && source[i - 1] >= value);
				}
			}

			return result;
		}
        
		public static List<bool> Crossunder(this List<decimal?> source, List<decimal?> value)
		{
			var result = new List<bool>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i == 0)
					result.Add(false);
				else
				{
					result.Add(source[i] < value[i] && source[i - 1] >= value[i - 1]);
				}
			}

			return result;
		}

		public static List<bool> Crossunder(this List<decimal> source, decimal value)
		{
			var result = new List<bool>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i == 0)
					result.Add(false);
				else
				{
					result.Add(source[i] < value && source[i - 1] >= value);
				}
			}

			return result;
		}
        
		public static List<bool> Crossunder(this List<decimal> source, List<decimal?> value)
		{
			var result = new List<bool>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i == 0)
					result.Add(false);
				else
				{
					result.Add(source[i] < value[i] && source[i - 1] >= value[i - 1]);
				}
			}

			return result;
		}
        
        public static List<bool> Crossunder(this List<decimal?> source, List<decimal> value)
        {
            var result = new List<bool>();

            for (int i = 0; i < source.Count; i++)
            {
                if (i == 0)
                    result.Add(false);
                else
                {
                    result.Add(source[i] < value[i] && source[i - 1] >= value[i - 1]);
                }
            }

            return result;
        }

		#endregion

		#region crossovers

		public static List<bool> Crossover(this List<decimal?> source, decimal value)
		{
			var result = new List<bool>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i == 0)
					result.Add(false);
				else
				{
					result.Add(source[i] > value && source[i - 1] <= value);
				}
			}

			return result;
		}


		public static List<bool> Crossover(this List<decimal?> source, List<decimal?> value)
		{
			var result = new List<bool>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i == 0)
					result.Add(false);
				else
				{
					result.Add(source[i] > value[i] && source[i - 1] <= value[i - 1]);
				}
			}

			return result;
		}


		public static List<bool> Crossover(this List<decimal> source, decimal value)
		{
			var result = new List<bool>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i == 0)
					result.Add(false);
				else
				{
					result.Add(source[i] > value && source[i - 1] <= value);
				}
			}

			return result;
		}


		public static List<bool> Crossover(this List<decimal> source, List<decimal?> value)
		{
			var result = new List<bool>();

			for (int i = 0; i < source.Count; i++)
			{
				if (i == 0)
					result.Add(false);
				else
				{
					result.Add(source[i] > value[i] && source[i - 1] <= value[i - 1]);
				}
			}

			return result;
		}

        #endregion

        #region Rises

        public static List<bool> Rises(this List<decimal?> source)
        {
            var result = new List<bool>();

            for (int i = 0; i < source.Count; i++)
            {
                if (i == 0)
                    result.Add(false);
                else if (!source[i].HasValue || !source[i - 1].HasValue)
                    result.Add(false);
                else
                    result.Add(source[i].Value > source[i - 1].Value);
            }

            return result;
        }
        public static List<bool> Rises(this List<decimal> source)
        {
            var result = new List<bool>();

            for (int i = 0; i < source.Count; i++)
            {
                if (i == 0)
                    result.Add(false);
                else
                    result.Add(source[i] > source[i - 1]);
            }

            return result;
        }
        #endregion

        #region Derive
        public static List<decimal> Derive(this List<decimal?> source)
        {
            var result = new List<decimal>();

            for (int i = 0; i < source.Count; i++)
            {
                if (i == 0 || !source[i - 1].HasValue || source[i - 1].Value == 0)
                    result.Add(0);
                else if (!source[i].HasValue)
                    result.Add(0);
                else
                    result.Add((source[i].Value - source[i - 1].Value) / source[i - 1].Value * 100.0m);
            }

            return result;
        }

        public static List<decimal> Derive(this List<decimal> source)
        {
            var result = new List<decimal>();

            for (int i = 0; i < source.Count; i++)
            {
                if (i == 0 || source[i - 1] == 0)
                    result.Add(0);
                else
                    result.Add((source[i] - source[i - 1]) / source[i - 1]);
            }

            return result;
        }
        #endregion

        #region Normalize
        public static List<decimal> Normalize(this List<decimal> source)
        {
            var result = new List<decimal>();
            var smin = source.Min();
            var smax = source.Max();

            if (smax == 0) throw new Exception("Normalize");

            for (int i = 0; i < source.Count; i++)
            {
                result.Add((source[i] - smin) / (smax-smin));
            }
            var c = result.Min();
            var c2 = result.Max();
            return result;
        }
        public static double[] Normalize(this double[] source)
        {
            var result = new List<double>();
            var smin = source.Min();
            var smax = source.Max();

            if (smax == 0) throw new Exception("Normalize");

            for (int i = 0; i < source.Length; i++)
            {
                result.Add((source[i] - smin) / (smax - smin));
            }
            var c = result.Min();
            var c2 = result.Max();
            return result.ToArray();
        }
        #endregion

        #region GetArray
        public static List<decimal> GetArray(this List<List<decimal>> source, int index)
        {
            var result = new List<decimal>();

            for (int i = 0; i < source.Count; i++)
            {
                result.Add(source[i][index]);
            }

            return result;
        }
        #endregion

        #region FillGaps
        public static List<decimal> FillGaps(this List<decimal?> source)
        {
            var result = new List<decimal>();
            var running = 0m;
            for (int i = 0; i < source.Count; i++)
            {
               
                if (source[i].HasValue)
                    running = source[i].Value;

                result.Add(running);
            }

            return result;
        }
        #endregion
    }
}
