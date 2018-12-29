using DiscordRPC;
using DiscordRPC.Logging;
using MahApps.Metro.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using System;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Text;

namespace YTMEdge
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		private string CustomStyle;
		private static int DiscordPipe = -1;
		private static string ClientID = "528593482051223564";
		private static DiscordRpcClient client = new DiscordRpcClient(ClientID, true, DiscordPipe);
		private static StringBuilder styleBuilder = new StringBuilder();

		public MainWindow()
		{
			InitializeComponent();
			YTWebView.Navigate(new Uri("https://music.youtube.com/"));

			if (File.Exists("style.css"))
			{
				foreach (var line in File.ReadAllLines("style.css"))
				{
					styleBuilder.Append(line);
				}

				CustomStyle = styleBuilder.ToString();
			}

			#region Initialize RPC
			client.Logger = new FileLogger("discord-rpc.log") { Level = LogLevel.Warning };

			client.OnReady += (sender, msg) =>
			{
				using (System.IO.StreamWriter sw = System.IO.File.AppendText("discord-rpc.log"))
				{
					sw.WriteLine($"Connected to Discord with user {msg.User.Username}");
				}
			};

			client.OnPresenceUpdate += (sender, msg) =>
			{
				using (System.IO.StreamWriter sw = System.IO.File.AppendText("discord-rpc.log"))
				{
					sw.WriteLine("Presence has been updated");
				}
			};

			var timer = new System.Timers.Timer(3000);
			timer.Elapsed += (sender, evt) =>
			{
				client.Invoke();
			};
			timer.Start();

			client.Initialize();
			client.SetPresence(new RichPresence()
			{
				Details = $"Listening to nothing",
				Timestamps = Timestamps.Now,
				Assets = new Assets()
				{
					LargeImageKey = "youtube",
					LargeImageText = "YouTube Music"
				}
			});
			#endregion
		}

		private void YTWebView_ScriptNotify(object sender, WebViewControlScriptNotifyEventArgs e)
		{
			// Shows messagebox if script notifications are needed
			MessageBox.Show(e.Value, e.Uri?.ToString() ?? string.Empty);
		}

		private void YTWebView_NavigationCompleted(object sender, WebViewControlNavigationCompletedEventArgs e)
		{
			// Display error dialog if navigation fails
			if (!e.IsSuccess)
				MessageBox.Show($"Navigation to {e.Uri?.ToString() ?? "NULL"}", $"Error: {e.WebErrorStatus}", MessageBoxButton.OK, MessageBoxImage.Error);

			// Synchronously call Update
			Update();
		}

		private void YTWebView_PermissionRequested(object sender, WebViewControlPermissionRequestedEventArgs e)
		{
			// Permissions
			if (e.PermissionRequest.State == WebViewControlPermissionState.Allow)
				return;
			if (e.PermissionRequest.State == WebViewControlPermissionState.Defer)
				YTWebView.GetDeferredPermissionRequestById(e.PermissionRequest.Id)?.Allow();
			else
				e.PermissionRequest.Allow();
		}

		private void YTWebView_DOMContentLoaded(object sender, WebViewControlDOMContentLoadedEventArgs e)
		{
			// Custom JS function for custom CSS
			string CustomCSSScript = "(function(){" +
				"var style=document.getElementById('gmusic_custom_css');" +
				"if(!style){ style = document.createElement('STYLE');" +
				"style.type='text/css';" +
				"style.id='gmusic_custom_css'; " +
				"style.innerText = \"" + CustomStyle + "\";" +
				"document.getElementsByTagName('HEAD')[0].appendChild(style);" +
				"} } )()";
			// Execute the script asychronously
			YTWebView.InvokeScriptAsync("eval", new string[] { CustomCSSScript });
		}

		private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Dispose the RPC client
			client.Dispose();
		}

		/// <summary>
		/// Updates RPC content (DO NOT AWAIT)
		/// </summary>
		/// <returns></returns>
		async Task Update()
		{
			while (true)
			{
				try
				{
					string songName = await YTWebView.InvokeScriptAsync("eval", new string[] { "document.getElementsByClassName('title style-scope ytmusic-player-bar')[0].innerText" });
					string artist = await YTWebView.InvokeScriptAsync("eval", new string[] { "document.getElementsByClassName('byline style-scope ytmusic-player-bar complex-string')[0].innerText" });
					if (artist.Contains("•"))
						artist = artist.Split('•')[0];

					Title = $"{songName} - YouTube Music";
					client.SetPresence(new RichPresence()
					{
						Details = $"Listening to {songName}",
						State = $"by {artist}",
						Assets = new Assets()
						{
							LargeImageKey = "youtube",
							LargeImageText = "YouTube Music"
						}
					});
				}
				catch
				{
					Title = $"Google Play Music";
					client.SetPresence(new RichPresence()
					{
						Details = $"Listening to nothing",
						Assets = new Assets()
						{
							LargeImageKey = "youtube",
							LargeImageText = "YouTube Music"
						}
					});
				}
				await Task.Delay(200);
			}
		}
	}
}