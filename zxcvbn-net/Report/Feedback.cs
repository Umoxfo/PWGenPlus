using System;
using System.Collections.Generic;
using System.Linq;

using Umoxfo.Zxcvbn.Matcher;

namespace Umoxfo.Zxcvbn.Report
{
    /// <summary>
    /// Verbal feedback to help choose better passwords.
    /// </summary>
    public class Feedback
    {
        /// <summary>
        /// Explains what's wrong, not always set.
        /// </summary>
        /// <value>Explains what's wrong, e.g. 'this is a top-10 common password'.
        /// Not always set -- sometimes an empty string</value>
        public string Warning { get; }

        /// <summary>
        /// A possibly-empty list of suggestions to help choose a less guessable password.
        /// </summary>
        /// <value>A possibly-empty list of suggestions to help
        /// choose a less guessable password. e.g. 'Add another word or two'</value>
        public IReadOnlyCollection<string> Suggestions { get; }

        public Feedback(string warning, params string[] suggestions)
        {
            Warning = warning;
            Suggestions = Array.AsReadOnly(suggestions);
        }
    }

    internal class PasswordFeedback
    {
        internal static Feedback GetFeedback(int score, IEnumerable<Match> sequence, in Translation translation = Translation.English)
        {
            Utility.SetTranslation(translation);

            // Starting feedback
            if (sequence == null || !sequence.Any())
            {
                return new Feedback(string.Empty, Properties.Resources.Suggestion_Default_UseFewWords);
            }

            // No feedback if score is good or great.
            if (score > 2) return new Feedback(string.Empty);

            // Tie feedback to the longest match for longer sequences
            Match longestMatch =
                sequence.Aggregate(sequence.First(), (longest, match) => match.Token.Length > longest.Token.Length ? match : longest);

            return GetMatchFeedback(longestMatch, sequence.Count() == 1);
        }//GetFeedback

        private static Feedback GetMatchFeedback(Match match, bool isSoleMatch, in Translation translation = Translation.English)
        {
            Utility.SetTranslation(translation);

            switch (match.Pattern)
            {
                case Pattern.Dictionary:
                    return GetDictionaryMatchFeedback((DictionaryMatch)match, isSoleMatch);
                case Pattern.Spatial:
                    return new Feedback(((SpatialMatch)match).Turns == 1
                            ? Properties.Resources.Warning_Spatial_StraightRowOfKey
                            : Properties.Resources.Warning_Spatial_ShortKeyboardPatterns,
                            // Suggestions
                            Properties.Resources.ExtraSuggestion_AddAnotherWord,
                            Properties.Resources.Suggestion_Spatial_UseLongerKeyboardPattern);
                case Pattern.Repeat:
                    //ToDo: add support for repeated sequences longer than 1 char
                    return new Feedback(((RepeatMatch)match).BaseToken.Length == 1
                            ? Properties.Resources.Warning_Repeat_LikeAAA
                            : Properties.Resources.Warning_Repeat_LikeABCABCABC,
                            // Suggestions
                            Properties.Resources.ExtraSuggestion_AddAnotherWord,
                            Properties.Resources.Suggestion_Repeat_AvoidRepeatedWords);
                case Pattern.Sequence:
                    return new Feedback(Properties.Resources.Warning_Sequence_SequenceABC6543,
                            // Suggestions
                            Properties.Resources.ExtraSuggestion_AddAnotherWord,
                            Properties.Resources.Suggestion_Sequence_AvoidSequences);
                case Pattern.Regex:
                    string warning;
                    List<string> suggestions = new List<string> { Properties.Resources.ExtraSuggestion_AddAnotherWord };
                    switch (((RegexMatch)match).RxName)
                    {
                        case "recent_year":
                            warning = Properties.Resources.Warning_Regex_RecentYears;
                            suggestions.Add(Properties.Resources.Suggestion_Regex_AvoidRecentYears);
                            break;
                        case "digits":
                            warning = Properties.Resources.Warning_Regex_Digits;
                            break;
                        default: warning = string.Empty; break;
                    }

                    return new Feedback(warning, suggestions.ToArray());
                case Pattern.Date:
                    return new Feedback(Properties.Resources.Warning_Date_Dates,
                        Properties.Resources.ExtraSuggestion_AddAnotherWord,
                        Properties.Resources.Suggestion_Date_AvoidDates);
                default: return new Feedback(string.Empty, Properties.Resources.ExtraSuggestion_AddAnotherWord);
            }
        }//GetMatchFeedback

        private static Feedback GetDictionaryMatchFeedback(in DictionaryMatch match, bool isSoleMatch)
        {
            string warning = string.Empty;
            switch (match.DictionaryName)
            {
                case "passwords":
                    if (isSoleMatch && !match.L33t && !match.Reversed)
                    {
                        if (match.Rank <= 10)
                            warning = Properties.Resources.Warning_Dictionary_Top10Passwords;
                        else if (match.Rank <= 100)
                            warning = Properties.Resources.Warning_Dictionary_Top100Passwords;
                        else warning = Properties.Resources.Warning_Dictionary_VeryCommonPasswords;
                    }
                    else if (match.GuessesLog10 <= 4 || PasswordScoring.EntropyToScore(match.Entropy) <= 1)
                    {
                        warning = Properties.Resources.Warning_Dictionary_SimilarCommonPasswords;
                    }
                    break;
                case "english" when isSoleMatch:
                    warning = Properties.Resources.Warning_Dictionary_EasyEnglishWord; break;
                case "surnames":
                case "male_names":
                case "female_names":
                    warning = isSoleMatch ? Properties.Resources.Warning_Dictionary_EasyNames
                                          : Properties.Resources.Warning_Dictionary_CommonNames;
                    break;
                default: warning = string.Empty; break;
            }//switch

            List<string> suggestions = new List<string> { Properties.Resources.ExtraSuggestion_AddAnotherWord };
            string word = match.Token;
            if (word.FirstOrDefault() >= 'A' && word.FirstOrDefault() <= 'Z')
            {
                suggestions.Add(Properties.Resources.Suggestion_Dictionary_CapsEasy);
            }
            else if (word == word.ToUpperInvariant() && word != word.ToLowerInvariant())
            {
                suggestions.Add(Properties.Resources.Suggestion_Dictionary_AllCapsEasy);
            }

            if (match.Reversed && word.Length >= 4)
            {
                suggestions.Add(Properties.Resources.Suggestion_Dictionary_ReversedEasy);
            }

            if (match.L33t)
            {
                suggestions.Add(Properties.Resources.Suggestion_Dictionary_PredictableSubstitutionsEasy);
            }

            return new Feedback(warning, suggestions.ToArray());
        }//GetDictionaryMatchFeedback
    }
}
