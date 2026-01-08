using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace Playnite.SDK.Controls
{
    /// <summary>
    ///
    /// </summary>
    public class PluginUserControl : UserControl
    {
        private static ILogger logger = LogManager.GetLogger();

        /// <summary>
        ///
        /// </summary>
        public Game GameContext
        {
            get
            {
                return (Game)GetValue(GameContextProperty);
            }

            set
            {
                SetValue(GameContextProperty, value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static readonly StyledProperty<Game> GameContextProperty =
            AvaloniaProperty.Register<PluginUserControl, Game>(nameof(GameContext));

        static PluginUserControl()
        {
            GameContextProperty.Changed.AddClassHandler<PluginUserControl>(OnGameContextPropertyChanged);
        }

        private static void OnGameContextPropertyChanged(PluginUserControl sender, AvaloniaPropertyChangedEventArgs e)
        {
            var newContext = e.NewValue as Game;
            var oldContext = e.OldValue as Game;
            try
            {
                sender.GameContextChanged(oldContext, newContext);
            }
            catch (Exception exc)
            {
                logger.Error(exc, $"GameContextChanged from {sender.GetType().Name} plugin control failed.");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="oldContext"></param>
        /// <param name="newContext"></param>
        public virtual void GameContextChanged(Game oldContext, Game newContext)
        {
        }
    }
}
