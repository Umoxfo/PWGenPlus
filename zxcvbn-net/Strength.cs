using System;
using System.Collections.Generic;
using System.Text;

namespace Zxcvbn
{
    public class Strength
    {
        public string Password { get; set; }

        public double Guesses { get; set; }

        public double GuessesLog10 { get; set; }

        public AttackTimes.CrackTimeSeconds CrackTimeSeconds { get; set; }

        public AttackTimes.CrackTimesDisplay CrackTimesDisplay { get; set; }

        public int Score { get; set; }

        public Feedback Feedback { get; set; }

        public IEnumerable<Match> Sequence { get; set; }

        public long CalcTime { get; set; }
    }
}
