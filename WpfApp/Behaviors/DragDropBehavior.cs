using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp.Behaviors;

public static class DragDropBehavior
{
  public static readonly DependencyProperty ReorderCommandProperty =
      DependencyProperty.RegisterAttached(
          "ReorderCommand",
          typeof(ICommand),
          typeof(DragDropBehavior),
          new PropertyMetadata(null, OnAttach));

  public static void SetReorderCommand(DependencyObject obj, ICommand value)
      => obj.SetValue(ReorderCommandProperty, value);

  public static ICommand GetReorderCommand(DependencyObject obj)
      => (ICommand)obj.GetValue(ReorderCommandProperty);

  public static readonly DependencyProperty IsDropAboveProperty =
      DependencyProperty.RegisterAttached("IsDropAbove", typeof(bool), typeof(DragDropBehavior));

  public static void SetIsDropAbove(DependencyObject obj, bool value)
      => obj.SetValue(IsDropAboveProperty, value);

  public static bool GetIsDropAbove(DependencyObject obj)
      => (bool)obj.GetValue(IsDropAboveProperty);

  public static readonly DependencyProperty IsDropBelowProperty =
      DependencyProperty.RegisterAttached("IsDropBelow", typeof(bool), typeof(DragDropBehavior));

  public static void SetIsDropBelow(DependencyObject obj, bool value)
      => obj.SetValue(IsDropBelowProperty, value);

  public static bool GetIsDropBelow(DependencyObject obj)
      => (bool)obj.GetValue(IsDropBelowProperty);

  static Point _start;
  static object? _dragged;

  static void OnAttach(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is ListBox lb)
    {
      lb.AllowDrop = true;
      lb.PreviewMouseLeftButtonDown += OnMouseDown;
      lb.MouseMove += OnMouseMove;
      lb.DragOver += OnDragOver;
      lb.Drop += OnDrop;
      lb.DragLeave += (_, __) => ClearIndicators(lb);
    }
  }

  static void OnMouseDown(object sender, MouseButtonEventArgs e)
  {
    _start = e.GetPosition(null);
    _dragged = (e.OriginalSource as DependencyObject)
        ?.FindAncestor<ListBoxItem>()?.DataContext;
  }

  static void OnMouseMove(object sender, MouseEventArgs e)
  {
    if (e.LeftButton == MouseButtonState.Pressed && _dragged is not null)
    {
      var pos = e.GetPosition(null);
      if (Math.Abs(pos.X - _start.X) > SystemParameters.MinimumHorizontalDragDistance ||
          Math.Abs(pos.Y - _start.Y) > SystemParameters.MinimumVerticalDragDistance)
      {
        DragDrop.DoDragDrop((DependencyObject)sender, _dragged, DragDropEffects.Move);
      }
    }
  }

  static void OnDragOver(object sender, DragEventArgs e)
  {
    var lb = (ListBox)sender;
    ClearIndicators(lb);

    var item = (e.OriginalSource as DependencyObject)
        ?.FindAncestor<ListBoxItem>();
    if (item is null)
      return;

    var pos = e.GetPosition(item);
    bool above = pos.Y < item.ActualHeight / 2;

    SetIsDropAbove(item, above);
    SetIsDropBelow(item, !above);
  }

  static void OnDrop(object sender, DragEventArgs e)
  {
    var lb = (ListBox)sender;
    var cmd = GetReorderCommand(lb);

    var target = (e.OriginalSource as DependencyObject)
        ?.FindAncestor<ListBoxItem>()?.DataContext;

    if (cmd is not null && _dragged is not null && target is not null && target != _dragged)
    {
      bool above = GetIsDropAbove(
          (e.OriginalSource as DependencyObject)?.FindAncestor<ListBoxItem>()!
      );

      cmd.Execute(new ReorderRequest(_dragged, target, above));
    }

    ClearIndicators(lb);
    _dragged = null;
  }

  static void ClearIndicators(ListBox lb)
  {
    foreach (var obj in lb.Items)
    {
      if (lb.ItemContainerGenerator.ContainerFromItem(obj) is ListBoxItem item)
      {
        SetIsDropAbove(item, false);
        SetIsDropBelow(item, false);
      }
    }
  }
}

public record ReorderRequest(object Source, object Target, bool DropAbove);

public static class VisualTreeHelpers
{
  public static T? FindAncestor<T>(this DependencyObject obj) where T : DependencyObject
  {
    while (obj is not null)
    {
      if (obj is T t)
        return t;
      obj = VisualTreeHelper.GetParent(obj);
    }
    return null;
  }
}
