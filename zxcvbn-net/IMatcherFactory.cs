using System.Collections.Generic;

using Zxcvbn.Matcher;

namespace Zxcvbn
{
    /// <summary>
    /// Interface that matcher factories must implement. Matcher factories return a list of the matchers
    /// that will be used to evaluate the password
    /// </summary>
    public interface IMatcherFactory
    {
        /// <summary>
        /// Match password against the combined matchers.
        /// </summary>
        /// <param name="password">The password to match</param>
        /// <returns>An enumerable of combine matches</returns>
        IEnumerable<Match> Omnimatch(string password);
    }
}
