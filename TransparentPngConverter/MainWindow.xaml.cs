using System;
using System.Collections.Generic;
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
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

namespace TransparentPngConverter
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				//変換処理
				string path = PathTextBox.Text;

				var directoryName = "generated/" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
				Directory.CreateDirectory("generated");

				var files = Directory.GetFiles(path, "*.pna", SearchOption.TopDirectoryOnly);
				foreach (var pnaPath in files)
				{
					var pngPath = Path.Combine(Path.GetDirectoryName(pnaPath), Path.GetFileNameWithoutExtension(pnaPath)) + ".png";

					//pngがあるかチェック
					if (!File.Exists(pngPath))
						continue;

					var pngBitmap = (Bitmap)Bitmap.FromFile(pngPath);
					var pnaBitmap = (Bitmap)Bitmap.FromFile(pnaPath);
					var resultBitmap = ConbineBitmap(pngBitmap, pnaBitmap);

					var relativePath = new Uri(path+@"\").MakeRelativeUri(new Uri(pngPath)).ToString();
					Directory.CreateDirectory(directoryName + @"\" + Path.GetDirectoryName(relativePath));
					resultBitmap.Save(directoryName + @"\" + relativePath);
				}

				Process.Start(Environment.CurrentDirectory + @"\" + directoryName);
			}
			catch
			{
				MessageBox.Show("失敗しました。");
			}
		}

		private Bitmap ConbineBitmap(Bitmap png, Bitmap pna)
		{
			var lockedPng = png.LockBits(new Rectangle(0, 0, png.Width, png.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			var lockedPna = pna.LockBits(new Rectangle(0, 0, pna.Width, pna.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			var result = new Bitmap(png.Width, png.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			var lockedResult = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			byte[] color = new byte[4];
			byte[] alpha = new byte[1];

			//変換
			for(int y = 0; y < lockedPng.Height; y++)
			{
				for(int x = 0; x < lockedPng.Width; x++)
				{
					//座標決定
					IntPtr resultAddress = lockedResult.Scan0 + lockedResult.Stride * y + x * 4;
					IntPtr pngAddress = lockedPng.Scan0 + lockedPng.Stride * y + x * 3;
					Marshal.Copy(pngAddress, color, 0, 3);

					//pnaのほうが小さい場合は透過にしておく
					if (x < lockedPna.Width && y < lockedPna.Height)
					{
						IntPtr pnaAddress = lockedPna.Scan0 + lockedPna.Stride * y + x * 3;
						Marshal.Copy(pnaAddress, alpha, 0, 1);
						color[3] = alpha[0];
					}
					else
					{
						color[3] = 0;
					}

					Marshal.Copy(color, 0, resultAddress, 4);
				}
			}

			png.UnlockBits(lockedPng);
			pna.UnlockBits(lockedPna);
			result.UnlockBits(lockedResult);
			return result;
		}
	}
}
