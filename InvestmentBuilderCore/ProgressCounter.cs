using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvestmentBuilderCore
{
    public class ProgressCounter
    {
        public int Count { get; private set; }
        public string Section { get; private set; }

        private int _increment;

        public void ResetCounter(string section, int stages)
        {
            Count = 0;
            Section = section;
            _increment = stages > 0 ? 100 / stages : 100;
        }

        public void IncrementCounter()
        {
            Count += _increment;
        }
    }

    public static class ProgresCounterHelper
    {
        public static void Initialise(this ProgressCounter counter, string section, int stages)
        {
            if(counter != null)
            {
                counter.ResetCounter(section, stages);
            }
        }

        public static void Increment(this ProgressCounter counter)
        {
            if(counter != null)
            {
                counter.IncrementCounter();
            }
        }
    }
}