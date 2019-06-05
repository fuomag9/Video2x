using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;

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
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "mp4|*.mp4" + "|all files|*.*"
            };
            saveFileDialog.ShowDialog();


            textbox_save.Text = saveFileDialog.FileName;

        }

        private async void Button_enhance_Click(object sender, RoutedEventArgs e)
        {

            string framerate;
            string risoluzione;
            float tempo_stimato;
            long frames_count;
            int compression_rate=0; //default= no compression
            bool debug;
            string application_path;
            //bool uwp_mode = false;
            string temp_dir_path;

            #region exceptions checking

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
            #endregion

            application_path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            try
            {
                temp_dir_path = Path.Combine(Windows.Storage.ApplicationData.Current.TemporaryFolder.Path, "videoframes");
                //uwp_mode = true;

            }
            catch (Exception)
            {

                temp_dir_path = Path.Combine(Path.GetTempPath(), "videoframes");

            }
            if (Directory.Exists(temp_dir_path))

            {
                try
                {
                    Directory.Delete(temp_dir_path, true);
                }
                catch (IOException)
                {

                    MessageBox.Show("An error happened, retry");
                    return;
                }


            }


            Directory.CreateDirectory(temp_dir_path); //create folder for frames
            string waifu_2x_folder = Path.Combine(application_path, "waifu2x");
            string waifu_2x_folder_temp = Path.Combine(temp_dir_path, "waifu2x");
            string models_rgb_folder = Path.Combine(waifu_2x_folder, "models_rgb");
            string models_rgb_folder_temp = Path.Combine(temp_dir_path, "models_rgb");
            Funzioni_utili.DirectoryCopy(waifu_2x_folder, waifu_2x_folder_temp); //copia waifu2x nella temp
            Funzioni_utili.DirectoryCopy(models_rgb_folder, models_rgb_folder_temp); //copia modelli rete neurale
            string ffmpeg_file = Path.Combine(Path.Combine(application_path, "ffmpeg"), "ffmpeg.exe");
            File.Copy(ffmpeg_file, Path.Combine(temp_dir_path, "ffmpeg.exe")); //copia ffmpeg.exe



            if (checkbox_loseless.IsChecked == false)
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

            progress_bar.Visibility = Visibility.Visible;


            //just because threads hates me :(
            string input_file = textbox_folder.Text;

            Console.WriteLine(@".\ffmpeg.exe -i '" + input_file + "' -vsync 0 img-%d.png");

            await Task.Run(() => Funzioni_utili.Esegui_console(temp_dir_path, @".\ffmpeg.exe -i '" + input_file + "' -vsync 0 img-%d.png", debug));

            progress_bar.Value++;



            DirectoryInfo temp_dir = new DirectoryInfo(temp_dir_path);
            frames_count = temp_dir.GetFiles().LongLength; //conta i frame totali




            progress_bar.Maximum = frames_count + 1;

            string result_file = textbox_save.Text;

            ShellObject obj = ShellObject.FromParsingName(input_file);
            ShellProperty<uint?> rateProp = obj.Properties.GetProperty<uint?>("System.Video.FrameRate"); //framerate del file
            ShellProperty<uint?> shellProperty_width = obj.Properties.GetProperty<uint?>("System.Video.FrameWidth"); //width del file
            ShellProperty<uint?> shellProperty_height = obj.Properties.GetProperty<uint?>("System.Video.FrameHeight"); //height del file

            framerate = (rateProp.Value / 1000).ToString();
            risoluzione = shellProperty_width.Value.ToString() + "x" + shellProperty_height.Value.ToString();


            var lista_files_temp_dir = temp_dir.GetFiles(); //aka i frame nella cartella
            time_textblock.Visibility = Visibility.Visible; //show estimate time (blank at the time)


            int frame_index = 1; //foreach is <3 but no index :<
            foreach (FileInfo frame in lista_files_temp_dir)
            {
                var start_time = DateTime.Now; 
                await Task.Run(() => Funzioni_utili.Esegui_console(temp_dir_path, ".'" + Path.Combine(waifu_2x_folder_temp, @".\waifu2x-converter-cpp.exe") + "' -i " + frame.Name + " -o " + frame.Name, debug));
                progress_bar.Value++;
                var delta_time = Math.Floor((DateTime.Now - start_time).TotalSeconds * (frames_count - frame_index));
                time_textblock.Text = Properties.Resources.remaining_time+" "+ delta_time.ToString()+"s";
                frame_index++;
            }
            time_textblock.Visibility = Visibility.Hidden; //can't predict estimate time for ffmpeg yet :(


            await Task.Run(() => Funzioni_utili.Esegui_console(temp_dir_path, @".\ffmpeg.exe -r " + framerate + " -f image2 -s " + risoluzione + " -start_number 1 -i img-%d.png -vframes " + frames_count + " -vcodec libx264 -crf " + compression_rate + " -pix_fmt yuv420p '" + result_file + "'", debug));

            progress_bar.Value++;

            MessageBox.Show(Properties.Resources.Finished);

            progress_bar.Value = 0;
            progress_bar.Visibility = Visibility.Hidden;
            Directory.Delete(temp_dir_path, true); //clean temp files

        }




    
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Properties.Resources.info_text);
        }
    }
}
