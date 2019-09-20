using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Device.Location;
using System.Diagnostics;
using static System.Math;
using CoordinateSharp;

namespace Eyes_Guardian
{
	class LightAdjuster
	{
		private int lightMode = -1;
		private int brightness = -1;
        private readonly GeoCoordinate location;
        private readonly Mutex mutex = new Mutex();

		public LightAdjuster()
		{
            GeoCoordinateWatcher geoCoordinateWatcher = new GeoCoordinateWatcher();
			geoCoordinateWatcher.TryStart(true, TimeSpan.FromSeconds(10));
            location = geoCoordinateWatcher.Position.Location;
            geoCoordinateWatcher.Stop();
            geoCoordinateWatcher.Dispose();
            if (location.IsUnknown)
            {
                location = new GeoCoordinate(52.0, 17.5);
            }
        }

		private int LightMode
		{
			get => lightMode;
			set
			{
				if (value != lightMode)
				{
					lightMode = value;
					RunCommand("reg add HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize /v AppsUseLightTheme /t REG_DWORD /d " + value + " /f");
				}
			}
		}

        private int Brightness
		{
			get => brightness;
			set
			{
				if (value != brightness)
				{
					brightness = value;
					RunCommand("PowerShell -Command (Get-WmiObject -Namespace root/WMI -Class WmiMonitorBrightnessMethods).WmiSetBrightness(1, " + value + ")");
				}
			}
		}

        private void RunCommand(string command)
		{
			Process process = new Process
			{
				StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = "/C " + command
                }
			};
			process.Start();
            process.WaitForExit();
            process.Dispose();
		}

		public void Adjust(bool force)
		{
            mutex.WaitOne();

            if (force)
            {
                lightMode = -1;
                brightness = -1;
            }

			Celestial celestial = Celestial.CalculateCelestialTimes
				(location.Latitude, location.Longitude, DateTime.UtcNow);

			if (celestial.IsSunUp)
			{
				LightMode = 1;
			}
			else
			{
				LightMode = 0;
			}

			double factor = (Sin(celestial.SunAltitude * PI / 180.0) + 0.3) * 1.0 / 1.3;
			if (factor > 0.0)
			{
				Brightness = (int)(Sqrt(factor) * 100.0);
			}
			else
			{
				Brightness = 0;
			}

            mutex.ReleaseMutex();
		}
	}
}
