﻿using MDP_PPG.ViewModels;
using PPG_Database;
using PPG_Database.KeepingModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MDP_PPG.PagedViews
{
	/// <summary>
	/// Логика взаимодействия для UC_SignalDataViewer.xaml
	/// </summary>
	public partial class UC_SignalDataViewer : UserControl, INotifyPropertyChanged
	{
		public UC_SignalDataViewer()
		{
			InitializeComponent();

			DataContext = this;

			SampleWidthStr = "100";
			Y_ScaleStr = "1";

			Min_X = 0;
			Max_X = 100;

			Min_Y = 0;
			Max_Y = 100;
		}

		public bool IsLoadingData
		{
			get => isLoadingData;
			set
			{
				isLoadingData = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoadingData)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInterfaceEnabled)));
			}
		}
		public bool IsInterfaceEnabled => !isLoadingData;

		public SignalDataGV Plot
		{
			get => plot;
			set
			{
				plot = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Plot)));
				//svX.ScrollToLeftEnd();
				//svY.ScrollToBottom();
			}
		}
		public Recording Recording
		{
			get => recording;
			set
			{
				recording = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Recording)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecordingIsNotNull)));
			}
		}
		public bool RecordingIsNotNull => Recording != null;

		public double Min_X
		{
			get => min_X;
			set
			{
				min_X = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Min_X)));
			}
		}
		public double Max_X
		{
			get => max_X;
			set
			{
				max_X = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Max_X)));
			}
		}
		public double Min_Y
		{
			get => min_Y;
			set
			{
				min_Y = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Min_Y)));
			}
		}
		public double Max_Y
		{
			get => max_Y;
			set
			{
				max_Y = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Max_Y)));
			}
		}

		public string X_Value => sbX.Value.ToString("G3");
		public string Y_Value => sbY.Value.ToString("G3");
		public string PlotRect => $"{plotGrid.ActualWidth.ToString("G3")} x {plotGrid.ActualHeight.ToString("G3")}";

		private void sbX_Scroll(object sender, ScrollEventArgs e)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X_Value)));
			TryUpdatePlot();
		}

		private void sbY_Scroll(object sender, ScrollEventArgs e)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y_Value)));
			TryUpdatePlot();
		}

		public void Freeze()
		{
			IsLoadingData = true;
		}

		public async Task LoadData(ModelBase recording)
		{
			Recording = (Recording)recording ?? null;

			if (Recording == null)
			{
				Plot = null;
				IsLoadingData = false;
				return;
			}

			SignalData sd;

			using (var context = new PPG_Context())
			{
				sd = await context.SignalDatas.FirstOrDefaultAsync(d => d.RecordingId == recording.Id);
			}

			if (sd != null)
			{
				Dispatcher.Invoke(delegate {
					Plot = new SignalDataGV();
					Plot.SetData(sd);
					TryUpdateScrollBars();
					sbY.Value = Max_Y;
					sbX.Value = Min_X;
					TryUpdatePlot();
				});
			}

			IsLoadingData = false;
		}

		public void OnWindowResized()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlotRect)));
			TryUpdatePlot();
		}
		private void TryUpdatePlot()
		{
			if (Plot == null) return;

			Plot.UpdatePlot(RectWindow, CurrentScale);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlotClip)));
		}
		public RectangleGeometry PlotClip => 
			new RectangleGeometry(new Rect(0.0, 0.0, plotGrid.ActualWidth, plotGrid.ActualHeight));
		private void TryUpdateScrollBars()
		{
			if (Plot == null) return;

			Max_X = Plot.SignalContainer.X_Range * SampleWidthGlobal;
			Max_Y = Plot.SignalContainer.Y_Range * Y_ScaleGlobal;
		}
		
		private Size CurrentScale => new Size(SampleWidthGlobal, Y_ScaleGlobal);
		private Rect RectWindow => new Rect(sbX.Value, Max_Y - sbY.Value, plotGrid.ActualWidth, plotGrid.ActualHeight);

		public string Y_ScaleStr
		{
			get => y_ScaleStr;
			set
			{
				y_ScaleStr = value;

				double v = -1.0;
				if (double.TryParse(value, out v) && v > 0)
				{
					Y_ScaleGlobal = v;
					if (Plot != null)
						Plot.SetYScale(Y_ScaleGlobal);
					TryUpdateScrollBars();
				}

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y_ScaleStr)));
			}
		}
		public string SampleWidthStr
		{
			get => sampleWidthStr;
			set
			{
				sampleWidthStr = value;

				double v = -1.0;
				if (double.TryParse(value, out v) && v > 0)
				{
					SampleWidthGlobal = v;
					if (Plot != null)
						Plot.SetXScale(SampleWidthGlobal);
					TryUpdateScrollBars();
				}

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SampleWidthStr)));
			}
		}

		public string MousePos
		{
			get => mousePos;
			set
			{
				mousePos = value;
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(MousePos)));
			}
		}
		private void Plot_MouseMove(object sender, MouseEventArgs e)
		{
			if (Plot == null) return;

			var p = e.GetPosition(plotGrid);

			MousePos = Plot.OnMouseMove(p);
		}
		private void Plot_MouseLeave(object sender, MouseEventArgs e)
		{
			MousePos = string.Empty;
		}

		private void Plot_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (Plot == null) return;

			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
			{
				double deltaScale = 1.0 + Math.Sign(e.Delta) * 0.1;

				var newScale = new Size(CurrentScale.Width * deltaScale, CurrentScale.Height * deltaScale);

				Plot.Change_XY_Scale(newScale);

				SampleWidthGlobal = newScale.Width;
				Y_ScaleGlobal = newScale.Height;

				sampleWidthStr = SampleWidthGlobal.ToString();
				y_ScaleStr = Y_ScaleGlobal.ToString();

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y_ScaleStr)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SampleWidthStr)));
			}
			else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
			{
				if (e.Delta > 0)
					sbX.Value += Max_X * 0.05;
				else
					sbX.Value -= Max_X * 0.05;

				e.Handled = true;
			}
			else
			{
				if (e.Delta > 0)
					sbY.Value -= Max_Y * 0.05;
				else
					sbY.Value += Max_Y * 0.05;

				e.Handled = true;
			}

			TryUpdatePlot();
		}


		public event PropertyChangedEventHandler PropertyChanged;


		private bool isLoadingData;
		private SignalDataGV plot;
		private Recording recording;
		private string sampleWidthStr;
		private string y_ScaleStr;
		private double SampleWidthGlobal;
		private double Y_ScaleGlobal;
		private string mousePos;
		private double min_X;
		private double max_X;
		private double min_Y;
		private double max_Y;
	}
}
