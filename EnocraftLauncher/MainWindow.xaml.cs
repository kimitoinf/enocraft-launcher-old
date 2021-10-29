using System;
using System.Net;
using System.Windows;
using CmlLib.Core;
using CmlLib.Core.Installer.FabricMC;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft.UI.Wpf;
using CmlLib.Core.Downloader;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using System.IO;

namespace EnocraftLauncher
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static MSession session;
		public static String fabricversion;
		public static String[] ModsURL =
		{
			"https://media.forgecdn.net/files/3494/349/fabric-api-0.41.0%2B1.17.jar",
			"https://media.forgecdn.net/files/3365/545/CocoaInput-1.17-fabric-4.0.4.jar",
			"https://media.forgecdn.net/files/3497/888/wthit-fabric-3.10.0.jar",
			"https://media.forgecdn.net/files/3502/8/Xaeros_Minimap_21.20.0_Fabric_1.17.1.jar"
		};
		public static String[] ModsName =
		{
			"fabric-api.jar",
			"CocoaInput.jar",
			"wthit.jar",
			"Xaeros_Minimap.jar"
		};

		public MainWindow()
		{
			InitializeComponent();
		}

		public void Login()
		{
			var login = new MLogin();
			var email = emailtext.Text;
			var password = passwordtext.Password;
			var response = login.TryAutoLogin();
			if (!response.IsSuccess)
			{
				response = login.Authenticate(email, password);
				if (!response.IsSuccess)
				{
					MessageBox.Show("로그인에 실패했습니다.");
					return;
				}
			}
			MessageBox.Show("로그인에 성공했습니다.");
			session = response.Session;
		}

		public void MSLogin()
		{
			MicrosoftLoginWindow loginwindow = new MicrosoftLoginWindow();
			session = loginwindow.ShowLoginDialog();
			if (session == null) MessageBox.Show("로그인에 실패했습니다.");
			else MessageBox.Show("로그인에 성공했습니다.");
		}

		public async Task Launch()
		{
			loginbutton.IsEnabled = false;
			loginwithmsbutton.IsEnabled = false;
			System.Net.ServicePointManager.DefaultConnectionLimit = 256;
			var path = new MinecraftPath(MinecraftPath.GetOSDefaultPath() + "/enocraft");
			var launcher = new CMLauncher(path);
			launcher.FileChanged += FileProgressBar_ProgressChanged;
			launcher.ProgressChanged += ProgressBar_ProgressChanged;
			var modspath = path.BasePath + "\\mods";
			var fabricversionloader = new FabricVersionLoader();
			var fabricversions = await fabricversionloader.GetVersionMetadatasAsync();
			foreach (var loop in fabricversions)
			{
				if (loop.Name.Contains("1.17.1"))
				{
					fabricversion = loop.Name;
					break;
				}
			}
			var fabric = fabricversions.GetVersionMetadata(fabricversion);
			await fabric.SaveAsync(path);
			DirectoryInfo modsfolder = new DirectoryInfo(modspath);
			if (modsfolder.Exists == false) modsfolder.Create();
			WebClient webclient = new WebClient();
			webclient.DownloadProgressChanged += ProgressBar_ProgressChanged;
			for (int loop = 0; loop < 4; loop++)
			{
				while (true)
				{
					if (!webclient.IsBusy)
					{
						webclient.DownloadFileAsync(new Uri(ModsURL[loop]), modspath + "\\" + ModsName[loop]);
						break;
					}
				}
			}
			var launchoption = new MLaunchOption
			{
				MaximumRamMb = 1024,
				Session = session,
				ServerIp = "168.138.6.9",
			};
			var process = await launcher.CreateProcessAsync(fabricversion, launchoption);
			process.Start();
			Environment.Exit(0);
		}

		private void loginbutton_Click(object sender, RoutedEventArgs e)
		{
			Login();
			if (session != null) Launch();
		}

		private void Window_Closed(object sender, System.EventArgs e)
		{
			Environment.Exit(0);
		}

		private void loginwithmsbutton_Click(object sender, RoutedEventArgs e)
		{
			MSLogin();
			if (session != null) Launch();
		}

		private void ProgressBar_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progress.Value = e.ProgressPercentage;
		}

		private void FileProgressBar_ProgressChanged(DownloadFileChangedEventArgs e)
		{
			fileprogress.Maximum = e.TotalFileCount;
			fileprogress.Value = e.ProgressedFileCount;
			filelabel.Content = $"{e.FileKind} : {e.FileName} ({e.ProgressedFileCount}/{e.TotalFileCount})";
		}
	}
}