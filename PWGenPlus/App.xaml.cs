using System.Windows;
using NGettext.Wpf;
using NGettext.Wpf.EnumTranslation;

namespace PWGenPlus
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            CompositionRoot();
            base.OnStartup(e);
        }

        private static void CompositionRoot()
        {
            var cultureTracker = new CultureTracker();
            ChangeCultureCommand.CultureTracker = cultureTracker;
            var localizer = new Localizer(cultureTracker, "PWGenPlus");
            GettextExtension.Localizer = localizer;
            TrackCurrentCultureBehavior.CultureTracker = cultureTracker;
            LocalizeEnumConverter.EnumLocalizer = new EnumLocalizer(localizer);
            Translation.Localizer = localizer;
        }
    }
}
