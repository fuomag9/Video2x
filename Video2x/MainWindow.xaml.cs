using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

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
            float tempoStimato;
            long framesCount;
            int compressionRate = 0; //default= no compression
            bool debug;
            string applicationPath;
            //bool uwp_mode = false;
            string tempDirPath;

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

            applicationPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            try
            {
                tempDirPath = Path.Combine(Windows.Storage.ApplicationData.Current.TemporaryFolder.Path,
                    "videoframes");
                //uwp_mode = true;
            }
            catch (Exception)
            {
                tempDirPath = Path.Combine(Path.GetTempPath(), "videoframes");
            }

            if (Directory.Exists(tempDirPath))

            {
                try
                {
                    Directory.Delete(tempDirPath, true);
                }
                catch (IOException)
                {
                    MessageBox.Show("An error happened, retry");
                    return;
                }
            }


            Directory.CreateDirectory(tempDirPath); //create folder for frames
            var waifu2XFolder = Path.Combine(applicationPath, "waifu2x");
            var waifu2XFolderTemp = Path.Combine(tempDirPath, "waifu2x");
            var modelsRgbFolder = Path.Combine(waifu2XFolder, "models_rgb");
            var modelsRgbFolderTemp = Path.Combine(tempDirPath, "models_rgb");
            FunzioniUtili.DirectoryCopy(waifu2XFolder, waifu2XFolderTemp); //copia waifu2x nella temp
            FunzioniUtili.DirectoryCopy(modelsRgbFolder, modelsRgbFolderTemp); //copia modelli rete neurale
            var ffmpegFile = Path.Combine(Path.Combine(applicationPath, "ffmpeg"), "ffmpeg.exe");
            File.Copy(ffmpegFile, Path.Combine(tempDirPath, "ffmpeg.exe")); //copia ffmpeg.exe


            if (checkbox_loseless.IsChecked == false)
            {
                compressionRate = 15;
            }

            debug = checkbox_debug.IsChecked == true;


            //code

            progress_bar.Visibility = Visibility.Visible;


            //just because threads hates me :(
            string input_file = textbox_folder.Text;

            Console.WriteLine($@".\ffmpeg.exe -i '{input_file}' -vsync 0 img-%d.png");

            await Task.Run(() => FunzioniUtili.Esegui_console(tempDirPath,
                $@".\ffmpeg.exe -i '{input_file}' -vsync 0 img-%d.png", debug));

            progress_bar.Value++;


            DirectoryInfo temp_dir = new DirectoryInfo(tempDirPath);
            framesCount = temp_dir.GetFiles().LongLength; //conta i frame totali

            progress_bar.Maximum = framesCount + 1;

            string resultFile = textbox_save.Text;

            ShellObject obj = ShellObject.FromParsingName(input_file);
            ShellProperty<uint?>
                rateProp = obj.Properties.GetProperty<uint?>("System.Video.FrameRate"); //framerate del file
            ShellProperty<uint?>
                shellPropertyWidth = obj.Properties.GetProperty<uint?>("System.Video.FrameWidth"); //width del file
            ShellProperty<uint?>
                shellPropertyHeight = obj.Properties.GetProperty<uint?>("System.Video.FrameHeight"); //height del file

            framerate = (rateProp.Value / 1000).ToString();
            risoluzione = $"{shellPropertyWidth.Value}x{shellPropertyHeight.Value}";


            var listaFilesTempDir = temp_dir.GetFiles(); //aka i frame nella cartella
            time_textblock.Visibility = Visibility.Visible; //show estimate time (blank at the time)


            var frameIndex = 1; //foreach is <3 but no index :<
            foreach (var frame in listaFilesTempDir)
            {
                var start_time = DateTime.Now;
                await Task.Run(() => FunzioniUtili.Esegui_console(tempDirPath,
                    $".'{Path.Combine(waifu2XFolderTemp, @".\waifu2x-converter-cpp.exe")}' -i {frame.Name} -o {frame.Name}", debug));
                progress_bar.Value++;
                var deltaTime = Math.Floor((DateTime.Now - start_time).TotalSeconds * (framesCount - frameIndex));
                time_textblock.Text = $"{Properties.Resources.remaining_time} {deltaTime}s";
                frameIndex++;
            }

            time_textblock.Visibility = Visibility.Hidden; //can't predict estimate time for ffmpeg yet :(


            await Task.Run(() => FunzioniUtili.Esegui_console(tempDirPath,
                $@".\ffmpeg.exe -r {framerate} -f image2 -s {risoluzione} -start_number 1 -i img-%d.png -vframes {framesCount} -vcodec libx264 -crf {compressionRate} -pix_fmt yuv420p '{resultFile}'", debug));

            progress_bar.Value++;

            MessageBox.Show(Properties.Resources.Finished);

            progress_bar.Value = 0;
            progress_bar.Visibility = Visibility.Hidden;
            Directory.Delete(tempDirPath, true); //clean temp files
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Properties.Resources.info_text);
        }
    }
}