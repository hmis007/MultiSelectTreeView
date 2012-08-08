﻿namespace System.Windows.Controls
{
	#region

	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Windows.Automation.Peers;
	using System.Windows.Input;
	using System.Windows.Media;

	#endregion

	public class TreeViewEx : ItemsControl
	{
		#region Constants and Fields

		public event EventHandler<PreviewSelectionChangedEventArgs> PreviewSelectionChanged;
		
		// TODO: Provide more details. Fire once for every single change and once for all groups of changes, with different flags
		public event EventHandler SelectionChanged;

		public static readonly DependencyProperty LastSelectedItemProperty;

		public static DependencyProperty BackgroundSelectionRectangleProperty = DependencyProperty.Register(
			"BackgroundSelectionRectangle",
			typeof(Brush),
			typeof(TreeViewExItem),
			new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x60, 0x33, 0x99, 0xFF)), null));

		public static DependencyProperty BorderBrushSelectionRectangleProperty = DependencyProperty.Register(
			"BorderBrushSelectionRectangle",
			typeof(Brush),
			typeof(TreeViewExItem),
			new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF)), null));

		public static DependencyProperty HoverHighlightingProperty = DependencyProperty.Register(
			"HoverHighlighting",
			typeof(bool),
			typeof(TreeViewEx),
			new FrameworkPropertyMetadata(true, null));

		public static DependencyProperty VerticalRulersProperty = DependencyProperty.Register(
			"VerticalRulers",
			typeof(bool),
			typeof(TreeViewEx),
			new FrameworkPropertyMetadata(false, null));

		public static DependencyProperty IsKeyboardModeProperty = DependencyProperty.Register(
			"IsKeyboardMode",
			typeof(bool),
			typeof(TreeViewEx),
			new FrameworkPropertyMetadata(false, null));

		public static DependencyPropertyKey LastSelectedItemPropertyKey = DependencyProperty.RegisterReadOnly(
			"LastSelectedItem",
			typeof(object),
			typeof(TreeViewEx),
			new FrameworkPropertyMetadata(null));

		public static DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
			"SelectedItems",
			typeof(IList),
			typeof(TreeViewEx),
			new FrameworkPropertyMetadata(
				null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsPropertyChanged));

		public static DependencyProperty AllowEditItemsProperty = DependencyProperty.Register(
			"AllowEditItems",
			typeof(bool),
			typeof(TreeViewEx),
			new FrameworkPropertyMetadata(false, null));

		#endregion

		#region Constructors and Destructors

		static TreeViewEx()
		{
			LastSelectedItemProperty = LastSelectedItemPropertyKey.DependencyProperty;
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeViewEx), new FrameworkPropertyMetadata(typeof(TreeViewEx)));
		}

		public TreeViewEx()
		{
			SelectedItems = new ObservableCollection<object>();
			Selection = new SelectionMultiple(this);
			Selection.PreviewSelectionChanged += (s, e) => { OnPreviewSelectionChanged(e); };
		}

		#endregion

		#region Public Properties

		public Brush BackgroundSelectionRectangle
		{
			get
			{
				return (Brush) GetValue(BackgroundSelectionRectangleProperty);
			}
			set
			{
				SetValue(BackgroundSelectionRectangleProperty, value);
			}
		}

		public Brush BorderBrushSelectionRectangle
		{
			get
			{
				return (Brush) GetValue(BorderBrushSelectionRectangleProperty);
			}
			set
			{
				SetValue(BorderBrushSelectionRectangleProperty, value);
			}
		}

		public bool HoverHighlighting
		{
			get
			{
				return (bool) GetValue(HoverHighlightingProperty);
			}
			set
			{
				SetValue(HoverHighlightingProperty, value);
			}
		}

		public bool VerticalRulers
		{
			get
			{
				return (bool) GetValue(VerticalRulersProperty);
			}
			set
			{
				SetValue(VerticalRulersProperty, value);
			}
		}

		public bool IsKeyboardMode
		{
			get
			{
				return (bool) GetValue(IsKeyboardModeProperty);
			}
			set
			{
				SetValue(IsKeyboardModeProperty, value);
			}
		}

		public bool AllowEditItems
		{
			get
			{
				return (bool) GetValue(AllowEditItemsProperty);
			}
			set
			{
				SetValue(AllowEditItemsProperty, value);
			}
		}

		/// <summary>
		///    Gets the last selected item.
		/// </summary>
		public object LastSelectedItem
		{
			get
			{
				return GetValue(LastSelectedItemProperty);
			}
			private set
			{
				SetValue(LastSelectedItemPropertyKey, value);
			}
		}

		private TreeViewExItem lastFocusedItem;
		/// <summary>
		/// Gets the last focused item.
		/// </summary>
		internal TreeViewExItem LastFocusedItem
		{
			get
			{
				return lastFocusedItem;
			}
			set
			{
				// Only the last focused TreeViewExItem may have IsTabStop = true
				// so that the keyboard focus only stops a single time for the TreeViewEx control.
				if (lastFocusedItem != null)
				{
					lastFocusedItem.IsTabStop = false;
				}
				lastFocusedItem = value;
				if (lastFocusedItem != null)
				{
					lastFocusedItem.IsTabStop = true;
				}
				// The TreeViewEx control only has the tab stop if none of its items has it.
				IsTabStop = lastFocusedItem == null;
			}
		}

		/// <summary>
		/// Gets or sets a list of selected items and can be bound to another list. If the source list
		/// implements <see cref="INotifyPropertyChanged"/> the changes are automatically taken over.
		/// </summary>
		public IList SelectedItems
		{
			get
			{
				return (IList) GetValue(SelectedItemsProperty);
			}
			set
			{
				SetValue(SelectedItemsProperty, value);
			}
		}

		internal ISelectionStrategy Selection { get; private set; }

		#endregion

		#region Public Methods and Operators

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Selection.ApplyTemplate();

			Unloaded += OnUnLoaded;
		}

		public bool ClearSelection()
		{
			if (SelectedItems.Count > 0)
			{
				foreach (var selItem in SelectedItems)
				{
					var e = new PreviewSelectionChangedEventArgs(false, selItem);
					OnPreviewSelectionChanged(e);
					if (e.CancelAny)
					{
						return false;
					}
				}

				SelectedItems.Clear();
			}
			return true;
		}

		#endregion

		#region Methods

		internal bool ClearSelectionByRectangle()
		{
			foreach (var item in SelectedItems)
			{
				var e = new PreviewSelectionChangedEventArgs(false, item);
				OnPreviewSelectionChanged(e);
				if (e.CancelAny) return false;
			}
			
			SelectedItems.Clear();
			return true;
		}

		internal TreeViewExItem GetNextItem(TreeViewExItem item, List<TreeViewExItem> items)
		{
			int indexOfCurrent = item != null ? items.IndexOf(item) : -1;
			for (int i = indexOfCurrent + 1; i < items.Count; i++)
			{
				if (items[i].IsVisible)
				{
					return items[i];
				}
			}
			return null;
		}

		internal TreeViewExItem GetPreviousItem(TreeViewExItem item, List<TreeViewExItem> items)
		{
			int indexOfCurrent = item != null ? items.IndexOf(item) : -1;
			for (int i = indexOfCurrent - 1; i >= 0; i--)
			{
				if (items[i].IsVisible)
				{
					return items[i];
				}
			}
			return null;
		}

		internal TreeViewExItem GetFirstItem(List<TreeViewExItem> items)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i].IsVisible)
				{
					return items[i];
				}
			}
			return null;
		}

		internal TreeViewExItem GetLastItem(List<TreeViewExItem> items)
		{
			for (int i = items.Count - 1; i >= 0; i--)
			{
				if (items[i].IsVisible)
				{
					return items[i];
				}
			}
			return null;
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new TreeViewExItem();
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is TreeViewExItem;
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TreeViewExAutomationPeer(this);
		}

		private static void OnSelectedItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TreeViewEx treeView = (TreeViewEx) d;
			if (e.OldValue != null)
			{
				INotifyCollectionChanged collection = e.OldValue as INotifyCollectionChanged;
				if (collection != null)
				{
					collection.CollectionChanged -= treeView.OnSelectedItemsChanged;
				}
			}

			if (e.NewValue != null)
			{
				INotifyCollectionChanged collection = e.NewValue as INotifyCollectionChanged;
				if (collection != null)
				{
					collection.CollectionChanged += treeView.OnSelectedItemsChanged;
				}
			}
		}

		internal static IEnumerable<TreeViewExItem> RecursiveTreeViewItemEnumerable(ItemsControl parent, bool includeInvisible)
		{
			return RecursiveTreeViewItemEnumerable(parent, includeInvisible, true);
		}

		internal static IEnumerable<TreeViewExItem> RecursiveTreeViewItemEnumerable(ItemsControl parent, bool includeInvisible, bool includeDisabled)
		{
			foreach (var item in parent.Items)
			{
				TreeViewExItem tve = (TreeViewExItem) parent.ItemContainerGenerator.ContainerFromItem(item);
				if (tve == null)
				{
					// Container was not generated, therefore it is probably not visible, so we can ignore it.
					continue;
				}
				if (!includeInvisible && !tve.IsVisible)
				{
					continue;
				}
				if (!includeDisabled && !tve.IsEnabled)
				{
					continue;
				}

				yield return tve;
				if (includeInvisible || tve.IsExpanded)
				{
					foreach (var childItem in RecursiveTreeViewItemEnumerable(tve, includeInvisible, includeDisabled))
					{
						yield return childItem;
					}
				}
			}
		}

		internal IEnumerable<TreeViewExItem> GetNodesToSelectBetween(TreeViewExItem firstNode, TreeViewExItem lastNode)
		{
			var allNodes = RecursiveTreeViewItemEnumerable(this, false, false).ToList();
			var firstIndex = allNodes.IndexOf(firstNode);
			var lastIndex = allNodes.IndexOf(lastNode);

			if (firstIndex >= allNodes.Count)
			{
				throw new InvalidOperationException(
				   "First node index " + firstIndex + "greater or equal than count " + allNodes.Count + ".");
			}

			if (lastIndex >= allNodes.Count)
			{
				throw new InvalidOperationException(
				   "Last node index " + lastIndex + " greater or equal than count " + allNodes.Count + ".");
			}

			var nodesToSelect = new List<TreeViewExItem>();

			if (lastIndex == firstIndex)
			{
				return new List<TreeViewExItem> { firstNode };
			}

			if (lastIndex > firstIndex)
			{
				for (int i = firstIndex; i <= lastIndex; i++)
				{
					if (allNodes[i].IsVisible)
					{
						nodesToSelect.Add(allNodes[i]);
					}
				}
			}
			else
			{
				for (int i = firstIndex; i >= lastIndex; i--)
				{
					if (allNodes[i].IsVisible)
					{
						nodesToSelect.Add(allNodes[i]);
					}
				}
			}

			return nodesToSelect;
		}

		internal IEnumerable<TreeViewExItem> GetTreeViewItemsFor(IEnumerable objects)
		{
			if (objects == null)
			{
				yield break;
			}

			foreach (var newItem in objects)
			{
				foreach (var treeViewExItem in RecursiveTreeViewItemEnumerable(this, true))
				{
					if (newItem == treeViewExItem.DataContext)
					{
						yield return treeViewExItem;
						break;
					}
				}
			}
		}

		// this eventhandler reacts on the firing control to, in order to update the own status
		private void OnSelectedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
#if DEBUG
					// Make sure we don't confuse TreeViewExItems and their DataContexts while development
					if (e.NewItems.OfType<TreeViewExItem>().Any())
						throw new ArgumentException("A TreeViewExItem instance was added to the SelectedItems collection. Only their DataContext instances must be added to this list!");
#endif
					object last = null;
					foreach (var item in GetTreeViewItemsFor(e.NewItems))
					{
						if (!item.IsSelected)
						{
							item.IsSelected = true;
						}

						last = item.DataContext;
					}

					LastSelectedItem = last;
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (var item in GetTreeViewItemsFor(e.OldItems))
					{
						item.IsSelected = false;
						if (item.DataContext == LastSelectedItem)
						{
							if (SelectedItems.Count > 0)
							{
								LastSelectedItem = SelectedItems[SelectedItems.Count - 1];
							}
							else
							{
								LastSelectedItem = null;
							}
						}
					}

					break;
				case NotifyCollectionChangedAction.Reset:
					foreach (var item in RecursiveTreeViewItemEnumerable(this, true))
					{
						if (item.IsSelected)
						{
							item.IsSelected = false;
						}
					}

					LastSelectedItem = null;
					break;
				default:
					throw new InvalidOperationException();
			}

			OnSelectionChanged();
		}

		private void OnUnLoaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= OnUnLoaded;
			if (Selection != null)
				Selection.Dispose();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (!e.Handled)
			{
				// Basically, this should not be needed anymore. It allows selecting an item with
				// the keyboard when the TreeViewEx control has the focus. If there were already
				// items when the control was focused, an item has already been focused (and
				// subsequent key presses won't land here but at the item).
				Key key = e.Key;
				switch (key)
				{
					case Key.Up:
						// Select last item
						var lastNode = RecursiveTreeViewItemEnumerable(this, false).LastOrDefault();
						if (lastNode != null)
						{
							Selection.Select(lastNode);
							e.Handled = true;
						}
						break;
					case Key.Down:
						// Select first item
						var firstNode = RecursiveTreeViewItemEnumerable(this, false).FirstOrDefault();
						if (firstNode != null)
						{
							Selection.Select(firstNode);
							e.Handled = true;
						}
						break;
				}
			}
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (!IsKeyboardMode)
			{
				IsKeyboardMode = true;
				//System.Diagnostics.Debug.WriteLine("Changing to keyboard mode from PreviewKeyDown");
			}
		}

		protected override void OnPreviewKeyUp(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (!IsKeyboardMode)
			{
				IsKeyboardMode = true;
				//System.Diagnostics.Debug.WriteLine("Changing to keyboard mode from PreviewKeyUp");
			}
		}

		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDown(e);
			if (IsKeyboardMode)
			{
				IsKeyboardMode = false;
				//System.Diagnostics.Debug.WriteLine("Changing to mouse mode");
			}
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			// If the TreeViewEx control has gotten the focus, it needs to pass it to an item
			// instead. If there was an item focused before, return to that. Otherwise just focus
			// this first item in the list if any. If there are no items at all, the TreeViewEx
			// control just keeps the focus.
			if (LastFocusedItem != null)
			{
				FocusHelper.Focus(LastFocusedItem);
			}
			else
			{
				var firstNode = RecursiveTreeViewItemEnumerable(this, false).FirstOrDefault();
				if (firstNode != null)
				{
					FocusHelper.Focus(firstNode);
				}
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			// This happens when a mouse button was pressed in an area which is not covered by an
			// item. Then, it should be focused which in turn passes on the focus to an item.
			Focus();
		}

		protected void OnPreviewSelectionChanged(PreviewSelectionChangedEventArgs e)
		{
			var handler = PreviewSelectionChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		protected void OnSelectionChanged()
		{
			var handler = SelectionChanged;
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		#endregion
	}
}