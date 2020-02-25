using System;
using System.Collections.Generic;
using System.Linq;

namespace WokLearner.WebApp.Utils
{
    public static class LinqUtils
    {
        //Copied from stack overflow (https://stackoverflow.com/users/1402749/masoud-darvishian)
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
        {
            var rnd = new Random();
            return source.OrderBy(item => rnd.Next());
        }
    }
}