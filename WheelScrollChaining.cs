using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.DeepLTranslate
{
    /// <summary>
    /// WPF's <see cref="ScrollViewer"/> marks every <see cref="UIElement.MouseWheelEvent"/> as handled,
    /// even when it has nothing left to scroll (see <c>ScrollViewer.OnMouseWheel</c>). Flow Launcher
    /// hosts a plugin's settings panel inside an <c>Expander</c> in the Plugins list, so the list owns
    /// the page scrolling. A panel scroll viewer with <c>ScrollableHeight == 0</c> would therefore
    /// swallow the wheel and freeze the page. These helpers let the panel chain the wheel outward to
    /// the next scrollable ancestor instead.
    /// </summary>
    internal static class WheelScrollChaining
    {
        /// <summary>
        /// True when <paramref name="scrollViewer"/> can still move vertically in the wheel's direction.
        /// A negative delta means "wheel down".
        /// </summary>
        public static bool CanScrollBy(ScrollViewer? scrollViewer, int delta)
        {
            if (scrollViewer is null)
                return false;

            if (scrollViewer.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
                return false;

            return delta < 0
                ? scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight
                : scrollViewer.VerticalOffset > 0;
        }

        /// <summary>
        /// Walks up from <paramref name="first"/> (inclusive) and returns the first scroll viewer that
        /// can still scroll in the wheel's direction. The walk stops after testing
        /// <paramref name="stopAfter"/> when it is supplied, so a caller can bound the search to its own
        /// subtree.
        /// </summary>
        public static bool TryFindScrollableAncestor(
            DependencyObject? first,
            int delta,
            out ScrollViewer? target,
            DependencyObject? stopAfter = null)
        {
            target = null;

            for (var current = first; current is not null; current = GetParent(current))
            {
                if (current is ScrollViewer scrollViewer && CanScrollBy(scrollViewer, delta))
                {
                    target = scrollViewer;
                    return true;
                }

                if (stopAfter is not null && ReferenceEquals(current, stopAfter))
                    break;
            }

            return false;
        }

        /// <summary>
        /// Re-raises the wheel event on <paramref name="target"/> so its own wheel handling runs.
        /// The new event starts at the target, which keeps intermediate controls (for example a
        /// <see cref="ComboBox"/>) out of the route.
        /// </summary>
        public static void Forward(ScrollViewer target, MouseWheelEventArgs e)
        {
            e.Handled = true;

            target.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = target
            });
        }

        /// <summary>
        /// True when <paramref name="source"/> sits inside a <see cref="ComboBox"/> below
        /// <paramref name="boundary"/> (the panel root).
        /// </summary>
        public static bool IsWithinComboBox(DependencyObject? source, DependencyObject? boundary)
        {
            for (var current = source; current is not null; current = GetParent(current))
            {
                if (current is ComboBox)
                    return true;

                if (boundary is not null && ReferenceEquals(current, boundary))
                    break;
            }

            return false;
        }

        private static DependencyObject? GetParent(DependencyObject child)
        {
            return child is Visual || child is System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(child)
                : LogicalTreeHelper.GetParent(child);
        }
    }
}
