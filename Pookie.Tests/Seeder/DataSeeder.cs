using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Seeder
{
    public class DataSeeder
    {
        private readonly Random random;

        public DataSeeder()
        {
            random = new Random();
        }

        public DataSeeder(object data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            random = new Random(data.GetHashCode());
        }

        public T GetRandom<T>(params T[] values)
        {
            if (!values.Any())
            {
                throw new ArgumentException("There needs to be at least one value in the list");
            }

            return values[random.Next(0, values.Length)];
        }
    }
}