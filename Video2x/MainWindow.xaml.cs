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
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Threading;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Diagnostics;
using Microsoft.Win32;

namespace Video2x
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {


            var dialog = new OpenFileDialog
            {
                Filter = "mp4|*.mp4"
            };
            dialog.ShowDialog();
            textbox_folder.Text = dialog.FileName;
            
            
            
        }

        private void Save_button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog {
                Filter = "mp4|*.mp4"+"|all files|*.*"
            };
            saveFileDialog.ShowDialog();


            textbox_save.Text = saveFileDialog.FileName;

        }

        private async void Button_enhance_Click(object sender, RoutedEventArgs e)
        {
            
            string framerate;
            string risoluzione;
            float tempo_stimato;
            int frames_count=0;
            int compression_rate;
            bool debug;
            string application_path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);


            //exceptions checking

            if (textbox_save.Text == "")
            {
                MessageBox.Show("You haven't selected where to save the converted file");
                return;
            }

            if (textbox_folder.Text == "")
            {
                MessageBox.Show("You haven't selected the file to convert");
                return;
            }

            if (textbox_folder.Text == textbox_save.Text)
            {
                MessageBox.Show("You can't overwrite the source file!");
                return;
            }


            if (checkbox_loseless.IsChecked == true)
            {
                compression_rate = 0;
            }
            else
            {
                compression_rate = 15;
            }

            if (checkbox_debug.IsChecked == true)
            {
                debug = true;
            }
            else
            {
                debug = false;
            }

            //code
            
            if (Directory.Exists(System.IO.Path.GetTempPath() + "\\videoframes\\"))

            {
                try
                {
                    Directory.Delete(System.IO.Path.GetTempPath() + "\\videoframes\\", true);
                }
                catch (System.IO.IOException)
                {

                    MessageBox.Show("An error happened, retry");
                    return;
                }


                 } 

            Directory.CreateDirectory(System.IO.Path.GetTempPath() + "\\videoframes\\"); 

            string temp_dir = System.IO.Path.GetTempPath()+ "\\videoframes\\";

            progress_bar.Visibility = System.Windows.Visibility.Visible;


            //just because threads hates me :(
            string input_file = textbox_folder.Text;

            await Task.Run(() => Esegui_console(temp_dir, "ffmpeg -i '" + input_file  + "' -vsync 0 img-%d.png", debug));

            progress_bar.Value++;

           

            DirectoryInfo directoryInfo = new DirectoryInfo(temp_dir);
            foreach (var item in directoryInfo.GetFiles())
            {
                frames_count++;
            }

            progress_bar.Maximum = frames_count + 2;

            string result_file = textbox_save.Text;

            ShellObject obj = ShellObject.FromParsingName(input_file);
            ShellProperty<uint?> rateProp = obj.Properties.GetProperty<uint?>("System.Video.FrameRate");
            ShellProperty<uint?> shellProperty_width = obj.Properties.GetProperty<uint?>("System.Video.FrameWidth");
            ShellProperty<uint?> shellProperty_height = obj.Properties.GetProperty<uint?>("System.Video.FrameHeight");

            framerate = (rateProp.Value / 1000).ToString();
            risoluzione = shellProperty_width.Value.ToString() + "x" + shellProperty_height.Value.ToString();


            var lista_files = directoryInfo.GetFiles();
            Funzioni_utili.DirectoryCopy(System.IO.Path.Combine(System.IO.Path.Combine(application_path, "waifu2x"), "models_rgb"), System.IO.Path.Combine(temp_dir, "models_rgb"));



            foreach (FileInfo file in lista_files)
            {
                await Task.Run(() => Esegui_console(temp_dir         ,".'"+System.IO.Path.Combine(System.IO.Path.Combine(application_path, "waifu2x"),@".\waifu2x-converter-cpp.exe")+ "' -i " + file.Name + " -o " + file.Name, debug));


            }

            progress_bar.Value++;


            

            await Task.Run(() => Esegui_console(temp_dir, "ffmpeg -r "+framerate+" -f image2 -s "+risoluzione+" -start_number 1 -i img-%d.png -vframes "+frames_count +" -vcodec libx264 -crf "+ compression_rate+ " -pix_fmt yuv420p '"+result_file+"'",       debug));

            progress_bar.Value++;

            MessageBox.Show("Finished!");

            progress_bar.Value = 0;
            progress_bar.Visibility = System.Windows.Visibility.Hidden;
            Directory.Delete(System.IO.Path.GetTempPath() + "\\videoframes\\", true);










        }

        public void Esegui_console(string cartella, string command, bool visualizza_console = false)
        {
            // Perform a long running work...
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            if (!visualizza_console)
            {
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            }
            startInfo.FileName = "powershell.exe";
            startInfo.Arguments = "cd '" + cartella + "';" + command;
            //startInfo.Arguments = "cd '" + cartella + "';dir;PAUSE";

            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }


        public static class Funzioni_utili
        {
            public static string AddQuotesIfRequired(string path)
            {
                return !string.IsNullOrWhiteSpace(path) ?
                    path.Contains(" ") && (!path.StartsWith("\"") && !path.EndsWith("\"")) ?
                        "\"" + path + "\"" : path :
                        string.Empty;
            }
            public static string GetClipBoradData()
            {
                try
                {
                    string clipboardData = null;
                    Exception threadEx = null;
                    Thread staThread = new Thread(
                        delegate ()
                        {
                            try
                            {
                                clipboardData = Clipboard.GetText(TextDataFormat.Text);
                            }

                            catch (Exception ex)
                            {
                                threadEx = ex;
                            }
                        });
                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.Start();
                    staThread.Join();
                    return clipboardData;
                }
                catch (Exception exception)
                {
                    return string.Empty;
                }
            }
            public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs=false)
            {
                // Get the subdirectories for the specified directory.
                DirectoryInfo dir = new DirectoryInfo(sourceDirName);

                if (!dir.Exists)
                {
                    throw new DirectoryNotFoundException(
                        "Source directory does not exist or could not be found: "
                        + sourceDirName);
                }

                DirectoryInfo[] dirs = dir.GetDirectories();
                // If the destination directory doesn't exist, create it.
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                }

                // Get the files in the directory and copy them to the new location.
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string temppath = System.IO.Path.Combine(destDirName, file.Name);
                    file.CopyTo(temppath, false);
                }

                // If copying subdirectories, copy them and their contents to new location.
                if (copySubDirs)
                {
                    foreach (DirectoryInfo subdir in dirs)
                    {
                        string temppath = System.IO.Path.Combine(destDirName, subdir.Name);
                        DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                    }
                }
            }
        }
    }
}
