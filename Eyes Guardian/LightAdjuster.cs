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
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Eyes_Guardian
{
	class LightAdjuster
	{
        private const string cursorsKey = @"HKEY_CURRENT_USER\Control Panel\Cursors\";
        private const string cursorsPath = @"%SystemRoot%\cursors\";
        private const string personalizeKey = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize\";
        private const string accessibilityKey = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Accessibility\";

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

        private void SetWindowsAeroCursor()
        {
            Registry.SetValue(cursorsKey, "", "Windows Aero");
            Registry.SetValue(cursorsKey, "AppStarting", cursorsPath + "aero_working.ani");
            Registry.SetValue(cursorsKey, "Arrow", cursorsPath + "aero_arrow.cur");
            Registry.SetValue(cursorsKey, "Crosshair", "");
            Registry.SetValue(cursorsKey, "Hand", cursorsPath + "aero_link.cur");
            Registry.SetValue(cursorsKey, "Help", cursorsPath + "aero_helpsel.cur");
            Registry.SetValue(cursorsKey, "IBeam", "");
            Registry.SetValue(cursorsKey, "No", cursorsPath + "aero_unavail.cur");
            Registry.SetValue(cursorsKey, "NWPen", cursorsPath + "aero_pen.cur");
            Registry.SetValue(cursorsKey, "SizeAll", cursorsPath + "aero_move.cur");
            Registry.SetValue(cursorsKey, "SizeNESW", cursorsPath + "aero_nesw.cur");
            Registry.SetValue(cursorsKey, "SizeNS", cursorsPath + "aero_ns.cur");
            Registry.SetValue(cursorsKey, "SizeNWSE", cursorsPath + "aero_nwse.cur");
            Registry.SetValue(cursorsKey, "SizeWE", cursorsPath + "aero_ew.cur");
            Registry.SetValue(cursorsKey, "UpArrow", cursorsPath + "aero_up.cur");
            Registry.SetValue(cursorsKey, "AppStarting", cursorsPath + "aero_busy.ani");
            Registry.SetValue(accessibilityKey, "CursorType", 0);
            SystemParametersInfo(SPI_SETCURSORS, 0, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        private void SetWindowsBlackCursor()
        {
            Registry.SetValue(cursorsKey, "", "Windows Black");
            Registry.SetValue(cursorsKey, "AppStarting", cursorsPath + "wait_r.cur");
            Registry.SetValue(cursorsKey, "Arrow", cursorsPath + "arrow_r.cur");
            Registry.SetValue(cursorsKey, "Crosshair", cursorsPath + "cross_r.cur");
            Registry.SetValue(cursorsKey, "Hand", "");
            Registry.SetValue(cursorsKey, "Help", cursorsPath + "help_r.cur");
            Registry.SetValue(cursorsKey, "IBeam", cursorsPath + "beam_r.cur");
            Registry.SetValue(cursorsKey, "No", cursorsPath + "no_r.cur");
            Registry.SetValue(cursorsKey, "NWPen", cursorsPath + "pen_r.cur");
            Registry.SetValue(cursorsKey, "SizeAll", cursorsPath + "move_r.cur");
            Registry.SetValue(cursorsKey, "SizeNESW", cursorsPath + "size1_r.cur");
            Registry.SetValue(cursorsKey, "SizeNS", cursorsPath + "size4_r.cur");
            Registry.SetValue(cursorsKey, "SizeNWSE", cursorsPath + "size2_r.cur");
            Registry.SetValue(cursorsKey, "SizeWE", cursorsPath + "size3_r.cur");
            Registry.SetValue(cursorsKey, "UpArrow", cursorsPath + "up_r.cur");
            Registry.SetValue(cursorsKey, "AppStarting", cursorsPath + "busy_r.cur");
            Registry.SetValue(accessibilityKey, "CursorType", 1);
            SystemParametersInfo(SPI_SETCURSORS, 0, 0, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        private void SetLightMode(int value)
		{
			if (value != lightMode)
			{
				lightMode = value;
                Registry.SetValue(personalizeKey, "AppsUseLightTheme", value);
                Registry.SetValue(personalizeKey, "SystemUsesLightTheme", value);
                if (value == 1)
                {
                    SetWindowsAeroCursor();
                }
                else
                {
                    SetWindowsBlackCursor();
                }
            }
		}

        private void SetBrightness(int value)
		{
			if (value != brightness)
			{
				brightness = value;
				RunCommand("PowerShell -Command (Get-WmiObject -Namespace root/WMI -Class WmiMonitorBrightnessMethods).WmiSetBrightness(1, " + value + ")");
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
                SetLightMode(1);
			}
			else
			{
                SetLightMode(0);
			}

			double factor = (Sin(celestial.SunAltitude * PI / 180.0) + 0.3) * 1.0 / 1.3;
			if (factor > 0.0)
			{
                SetBrightness((int)(Sqrt(factor) * 100.0));
			}
			else
			{
				SetBrightness(0);
			}

            mutex.ReleaseMutex();
		}

        const int SPI_SETCURSORS = 0x0057;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);
    }
}
