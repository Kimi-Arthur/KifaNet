using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BilibiliAssGeneratorTests
{
    public static class Utils
    {
        public static Exception GetException(Action code)
        {
            try
            {
                code.Invoke();
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
    }
}
