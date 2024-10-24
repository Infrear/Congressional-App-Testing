using System.Windows;
using System.Windows.Media;

namespace Congressional_App_Test1.Utilities
{
    public static class VisualTreeHelperExtensions
    {
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            // Search through all the children of the parent
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Check if the child is of the type T
                if (child is T typedChild && (child as FrameworkElement)?.Name == childName)
                {
                    return typedChild;
                }

                // Recursively search in the child's children
                T foundChild = FindChild<T>(child, childName);
                if (foundChild != null)
                {
                    return foundChild;
                }
            }

            return null;
        }
    }
}