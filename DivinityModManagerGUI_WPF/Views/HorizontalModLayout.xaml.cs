﻿using DivinityModManager.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DivinityModManager.Models;
using DynamicData.Binding;

namespace DivinityModManager.Views
{
	public interface ModViewLayout
	{
		void UpdateViewSelection(IEnumerable<DivinityModData> dataList, ListView listView = null);
	}
	/// <summary>
	/// Interaction logic for HorizonalModLayout.xaml
	/// </summary>
	public partial class HorizontalModLayout : UserControl, IViewFor<MainWindowViewModel>, ModViewLayout
	{
		public MainWindowViewModel ViewModel
		{
			get { return (MainWindowViewModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register("ViewModel", typeof(MainWindowViewModel), typeof(HorizontalModLayout), new PropertyMetadata(null));

		object IViewFor.ViewModel { get; set; }

		private MainWindowViewModel _lastVM;

		private void SetupListView(ListView listView)
		{
			listView.InputBindings.Add(new KeyBinding(ApplicationCommands.SelectAll, new KeyGesture(Key.A, ModifierKeys.Control)));
			listView.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (_sender, _e) =>
			{
				//listView.SelectAll();
				if(listView.ItemsSource is IEnumerable<ISelectable> mods)
				{
					foreach(var mod in mods)
					{
						mod.IsSelected = true;
					}
				}
			}));

			listView.InputBindings.Add(new KeyBinding(ReactiveCommand.Create(() =>
			{
				//listView.SelectedItems.Clear();
				if (listView.ItemsSource is IEnumerable<ISelectable> mods)
				{
					foreach (var mod in mods)
					{
						mod.IsSelected = false;
					}
				}
			}), new KeyGesture(Key.D, ModifierKeys.Control)));

			listView.ItemContainerStyle = this.FindResource("ListViewItemMouseEvents") as Style;
		}

		public void UpdateViewSelection(IEnumerable<DivinityModData> dataList, ListView listView = null)
		{
			if(listView == null)
			{
				if(dataList == ViewModel.ActiveMods)
				{
					listView = ActiveModsListView;
				}
				else
				{
					listView = InactiveModsListView;
				}
			}
			foreach (var mod in dataList)
			{
				var listItem = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(mod);
				if (listItem != null)
				{
					listItem.IsSelected = mod.IsSelected;
				}
			}
		}

		private void UpdateIsSelected(SelectionChangedEventArgs e, ObservableCollectionExtended<DivinityModData> list)
		{
			if (e.RemovedItems != null && e.RemovedItems.Count > 0)
			{
				foreach (var removedItem in e.RemovedItems.Cast<DivinityModData>())
				{
					if (list.Contains(removedItem)) removedItem.IsSelected = false;
				}
			}
			if (e.AddedItems != null && e.AddedItems.Count > 0)
			{
				foreach (var addedItem in e.AddedItems.Cast<DivinityModData>())
				{
					addedItem.IsSelected = true;
				}
			}
		}

		public HorizontalModLayout()
		{
			InitializeComponent();

			SetupListView(ActiveModsListView);
			SetupListView(InactiveModsListView);

			this.WhenActivated((d) =>
			{
				_lastVM = ViewModel;

				if(ViewModel != null)
				{
					//ViewModel.OnOrderChanged += AutoSizeNameColumn_ActiveMods;
					ViewModel.OnOrderChanged += AutoSizeNameColumn_InactiveMods;

					ViewModel.Layout = this;

					ActiveModsListView.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
					{
						UpdateIsSelected(e, ViewModel.ActiveMods);
					};

					InactiveModsListView.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
					{
						UpdateIsSelected(e, ViewModel.InactiveMods);
					};

					ActiveModsListView.ItemContainerGenerator.StatusChanged += (s, e) =>
					{
						if(ActiveModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
						{
							UpdateViewSelection(ViewModel.ActiveMods, ActiveModsListView);
						}
					};

					InactiveModsListView.ItemContainerGenerator.StatusChanged += (s, e) =>
					{
						if (InactiveModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
						{
							UpdateViewSelection(ViewModel.InactiveMods, InactiveModsListView);
						}
					};
				}

				// when the ViewModel gets deactivated
				Disposable.Create(() =>
				{
					if(_lastVM != null)
					{
						_lastVM.OnOrderChanged -= AutoSizeNameColumn_ActiveMods;
						_lastVM.OnOrderChanged -= AutoSizeNameColumn_InactiveMods;

						_lastVM = null;
					}
				}).DisposeWith(d);
			});
		}

		GridViewColumnHeader _lastHeaderClicked = null;
		ListSortDirection _lastDirection = ListSortDirection.Ascending;

		private void ListView_Click(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
			ListSortDirection direction;

			if (headerClicked != null)
			{
				if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
				{
					if (headerClicked != _lastHeaderClicked)
					{
						direction = ListSortDirection.Ascending;
					}
					else
					{
						if (_lastDirection == ListSortDirection.Ascending)
						{
							direction = ListSortDirection.Descending;
						}
						else
						{
							direction = ListSortDirection.Ascending;
						}
					}

					string header = "";

					if (headerClicked.Column.Header is TextBlock textBlock)
					{
						header = textBlock.Text;
					}
					else if (headerClicked.Column.Header is string gridHeader)
					{
						header = gridHeader;
					}

					Sort(header, direction, sender);

					_lastHeaderClicked = headerClicked;
					_lastDirection = direction;
				}
			}
		}

		private void Sort(string sortBy, ListSortDirection direction, object sender)
		{
			if (sortBy == "Version") sortBy = "Version.Version";
			if (sortBy == "#") sortBy = "Index";
			if (sortBy == "Name") sortBy = "DisplayName";
			ListView lv = sender as ListView;
			ICollectionView dataView =
			  CollectionViewSource.GetDefaultView(lv.ItemsSource);

			dataView.SortDescriptions.Clear();
			SortDescription sd = new SortDescription(sortBy, direction);
			dataView.SortDescriptions.Add(sd);
			dataView.Refresh();
		}

		public void AutoSizeNameColumn_ActiveMods(object sender, EventArgs e)
		{
			if (ActiveModsListView.View is GridView gridView)
			{
				if (gridView.Columns.Count >= 2)
				{
					if (ViewModel.ActiveMods.Count > 0)
					{
						RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () =>
						{
							var longestName = ViewModel.ActiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
							//Trace.WriteLine($"Autosizing active mods grid for name {longestName}");
							gridView.Columns[1].Width = MeasureText(longestName,
								ActiveModsListView.FontFamily,
								ActiveModsListView.FontStyle,
								ActiveModsListView.FontWeight,
								ActiveModsListView.FontStretch,
								ActiveModsListView.FontSize).Width + 12;
						});
					}
				}
			}
		}

		public void AutoSizeNameColumn_InactiveMods(object sender, EventArgs e)
		{
			if (InactiveModsListView.View is GridView gridView)
			{
				if (gridView.Columns.Count >= 2)
				{
					if (ViewModel.InactiveMods.Count > 0)
					{
						var longestName = ViewModel.InactiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
						//Trace.WriteLine($"Autosizing inactive mods grid for name {longestName}");
						gridView.Columns[0].Width = MeasureText(longestName,
							ActiveModsListView.FontFamily,
							ActiveModsListView.FontStyle,
							ActiveModsListView.FontWeight,
							ActiveModsListView.FontStretch,
							ActiveModsListView.FontSize).Width + 12;
					}
				}
			}
		}

		// Source: https://stackoverflow.com/a/22420728
		private static Size MeasureTextSize(string text, FontFamily fontFamily, FontStyle fontStyle,
			FontWeight fontWeight, FontStretch fontStretch, double fontSize)
		{
			var typeFace = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			FormattedText ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black);
			return new Size(ft.Width, ft.Height);
		}

		private static Size MeasureText(string text,
			FontFamily fontFamily,
			FontStyle fontStyle,
			FontWeight fontWeight,
			FontStretch fontStretch, double fontSize)
		{
			Typeface typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			GlyphTypeface glyphTypeface;

			if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
			{
				return MeasureTextSize(text, fontFamily, fontStyle, fontWeight, fontStretch, fontSize);
			}

			double totalWidth = 0;
			double height = 0;

			for (int n = 0; n < text.Length; n++)
			{
				ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

				double width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;

				double glyphHeight = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

				if (glyphHeight > height)
				{
					height = glyphHeight;
				}

				totalWidth += width;
			}

			return new Size(totalWidth, height);
		}
	}
}
