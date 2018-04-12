using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X.TryATry.Test
{
    public class ThrowTester
    {
        private int Do1()
        {
            int a = 1;
            int b = 0;
            int c = a / b;
            return c;
        }

        private int Do2()
        {
            try
            {
                return Do1();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public int Do3()
        {
            try
            {
                return Do2();
            }
            catch (Exception ex)
            {
                log4net.LogManager.GetLogger(typeof(ThrowTester)).Error("出错了", ex);
                return 0;
            }
        }
    }
}
