﻿using Esri.ArcGISRuntime.Location;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
#if NETFX_CORE
using Esri.ArcGISRuntime.UI;
using Windows.UI.Xaml;
#elif __IOS__ || __ANDROID__
using Xamarin.Forms;
using Esri.ArcGISRuntime.Xamarin.Forms;
using DependencyObject = Xamarin.Forms.BindableObject;
using DependencyProperty = Xamarin.Forms.BindableProperty;
#else
using Esri.ArcGISRuntime.UI;
using System.Windows.Threading;
#endif

namespace RoutingSample
{
	public class RestoreAutoPanMode
	{
		private class DelayTimer
		{
			private Action m_action;
			DispatcherTimer m_timer;
			public DelayTimer(Action action)
			{
				m_timer = new DispatcherTimer();
				m_timer.Tick += m_timer_Tick;
				m_action = action;
			}

#if NETFX_CORE
			void m_timer_Tick(object sender, object e)
#else
			void m_timer_Tick(object sender, EventArgs e)
#endif
			{
				m_timer.Stop();
				if (m_action != null)
					m_action();				
			}
			public void Invoke(TimeSpan delay)
			{
				m_timer.Stop();
				m_timer.Interval = delay;
				m_timer.Start();
			}
			public void Cancel()
			{
				m_timer.Stop();
			}
		}

		private MapView m_mapView;
		private DelayTimer m_timer;

		public RestoreAutoPanMode()
		{
			m_timer = new DelayTimer(ResetPanMode);
		}

		private void ResetPanMode()
		{
			if (m_mapView != null && m_mapView.LocationDisplay != null)
				m_mapView.LocationDisplay.AutoPanMode = this.PanMode;
		}

		internal void AttachToMapView(MapView mv)
		{
			if (m_mapView != null && m_mapView != mv)
				throw new InvalidOperationException("RestoreAutoPanMode can only be assigned to one mapview");
			m_mapView = mv;
            m_mapView.NavigationCompleted += M_mapView_NavigationCompleted;
        }

		internal void DetachFromMapView(MapView mv)
		{
            m_mapView.NavigationCompleted -= M_mapView_NavigationCompleted;
			m_mapView = null;
		}

        private void M_mapView_NavigationCompleted(object sender, EventArgs e)
        {
			//If user stopped navigating and we're not in the correct autopan mode,
			//restore autopan after the set delay.
			if(IsEnabled && m_mapView.IsNavigating)
			{
				if(m_mapView.LocationDisplay != null && 
					m_mapView.LocationDisplay.AutoPanMode != PanMode)
				{
					if (!m_mapView.IsNavigating)
						m_timer.Invoke(TimeSpan.FromSeconds(DelayInSeconds));
					else
						m_timer.Cancel();
				}
			}
		}

		public bool IsEnabled { get; set; }

		public int DelayInSeconds { get; set; }

		public LocationDisplayAutoPanMode PanMode { get; set; }	
	
		public static RestoreAutoPanMode GetRestoreAutoPanSettings(DependencyObject obj)
		{
			return (RestoreAutoPanMode)obj.GetValue(RestoreAutoPanSettingsProperty);
		}

		public static void SetRestoreAutoPanSettings(DependencyObject obj, RestoreAutoPanMode value)
		{
			obj.SetValue(RestoreAutoPanSettingsProperty, value);
		}

        public static readonly DependencyProperty RestoreAutoPanSettingsProperty =
#if __IOS__ || __ANDROID__
            DependencyProperty.CreateAttached("RestoreAutoPanSettings", typeof(RestoreAutoPanMode), typeof(RestoreAutoPanMode),
                null, BindingMode.OneWay, null, OnRestoreAutoPanSettingsChanged);
#else
            DependencyProperty.RegisterAttached("RestoreAutoPanSettings", typeof(RestoreAutoPanMode), typeof(RestoreAutoPanMode),
            new PropertyMetadata(null, OnRestoreAutoPanSettingsChanged));
#endif

        private static void OnRestoreAutoPanSettingsChanged(DependencyObject d,
#if __IOS__ || __ANDROID__
            object oldValue, object newValue)
        {
#else
            DependencyPropertyChangedEventArgs e)
		{
            var oldValue = e.OldValue;
            var newValue = e.NewValue;
#endif
            if (!(d is MapView))
				throw new InvalidOperationException("This property must be attached to a mapview");

			MapView mv = (MapView)d;
			if (oldValue is RestoreAutoPanMode)
				((RestoreAutoPanMode)oldValue).DetachFromMapView(mv);
			if (newValue is RestoreAutoPanMode)
                ((RestoreAutoPanMode)newValue).AttachToMapView(mv);
		}		
	}
}
