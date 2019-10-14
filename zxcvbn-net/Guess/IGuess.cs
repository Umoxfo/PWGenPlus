using System;
using System.Collections.Generic;
using System.Text;

namespace Zxcvbn.Guess
{
    public interface IGuess
    {
        double Guess(Match match);
    }
}
