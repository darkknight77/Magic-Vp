using MyToolkit.Multimedia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using YoutubeExtractor;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Magic_Video_Player
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        MediaPlayer mediaPlayer;
        MediaPlaybackItem playbackItem;
        ListView files;
        Dictionary<String, StorageFile> fileData;
        private Dictionary<TimedTextSource, Uri> ttsMap = new Dictionary<TimedTextSource, Uri>();
        public List<listContent> ContentList { get; set; }
        String[] videoType = {  "video/mp4", "video/x-matroska", "video/webm", "video/avi", "video/mpeg", "video/mpg", "video/msvideo"
       , "video/quicktime", "video/x-mpeg", "video/x-mpeg2a"
       , "video/x-ms-asf", "video/x-ms-asf-plugin", "video/x-ms-wm"
       , "video/x-ms-wmv", "video/x-ms-wmx", "video/x-ms-wvx","video/vnd.dlna.mpeg-tts"
       , "video/x-msvideo"  };

        String[] audioType = { "audio/mp3", "audio/mp4", "audio/aiff", "audio/basic", "audio/mid", "audio/midi"
        , "audio/mpeg", "audio/mpegurl", "audio/mpg", "audio/wav"
        , "audio/x-aiff", "audio/x-mid", "audio/x-midi", "audio/x-mp3"
        , "audio/x-mpeg", "audio/x-mpegurl" , "audio/x-mpg", "audio/x-ms-wax"
        , "audio/x-ms-wma", "audio/x-wav" };
        String[] videoAudioTypeFilter = { ".wmv", ".mp4", ".wma", ".mp3", ".mkv", ".m4a" };
        String[] subtitleType = { ".srt", ".vtt" };
        public MainPage()
        {
            this.InitializeComponent();
            mediaPlayer = new MediaPlayer();

            mediaPlayerElement.SetMediaPlayer(mediaPlayer);

            //files = playlist;
            fileData = new Dictionary<String, StorageFile>();
            playlist.DataContext = ContentList;
            
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
            base.OnNavigatedTo(e);
            var args = e.Parameter as Windows.ApplicationModel.Activation.IActivatedEventArgs;
            if (args != null)
            {
                if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
                {
                    var fileArgs = args as Windows.ApplicationModel.Activation.FileActivatedEventArgs;
                    string strFilePath = fileArgs.Files[0].Path;
                    Debug.WriteLine(strFilePath);
                    var file = (StorageFile)fileArgs.Files[0];
                    Debug.WriteLine(file.Path);
                    openMedia(file);
                }
            }
        }

        void openMedia(StorageFile file) {

            MediaSource source = MediaSource.CreateFromStorageFile(file);

            playbackItem = new MediaPlaybackItem(source);
            mediaPlayerElement.Source = playbackItem;
            mediaPlayerElement.AutoPlay = true;


            if (isVideoType(file) || isAudioType(file))
            {
                AddItemsToListView(file);
            }
            else
            {
                Debug.WriteLine(file.Name + " cant be added. Type: " + file.ContentType);
            }
        }

        async private void openSubtitle_Click(object sender, RoutedEventArgs e)
        {
            await openSubtitlePicker(subtitleType);

        }

        async private System.Threading.Tasks.Task openSubtitlePicker(String[] arr)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            foreach (string type in arr)
            {
                openPicker.FileTypeFilter.Add(type);
            }

            var pickedfiles = await openPicker.PickMultipleFilesAsync();

            if (pickedfiles.Count > 0)
            {
                foreach (Windows.Storage.StorageFile file in pickedfiles)
                {
                    AddSubtitle(file);

                }

            }

        }

       
        private void TtsEn_Resolved(TimedTextSource sender, TimedTextSourceResolveResultEventArgs args)
        {
            var ttsUri = ttsMap[sender];
            Debug.WriteLine($"{ttsUri} ; {ttsUri.AbsoluteUri}  {ttsUri.ToString()} ");
            // Handle errors
            if (args.Error != null)
            {
                var ignoreAwaitWarning = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                        Debug.WriteLine("Error resolving track " + ttsUri + " due to error " +args.Error +  args.Error.ErrorCode +args.Error.ExtendedError);
                    });
                return;
            }

            // Update label manually since the external SRT does not contain it
            var ttsUriString = ttsUri.AbsoluteUri;
            if (ttsUriString.Contains("_en"))
            {
                Debug.WriteLine("Label : english");
                args.Tracks[0].Label = "English";
            }
            else if (ttsUriString.Contains("_pt"))
            {
                Debug.WriteLine("Label : portugese");
                args.Tracks[0].Label = "Portuguese";
            }
            else if (ttsUriString.Contains("_sv"))
            {
                Debug.WriteLine("Label : swedish");
                args.Tracks[0].Label = "Swedish";
            }


    
    }

    async private void OpenUrl(object sender, RoutedEventArgs e)
        {

            string url = await InputTextDialogAsync("Enter URL");
            Debug.WriteLine(url);
            mediaPlayer.Source = null;
            //youtubePlay(url);
            var source = MediaSource.CreateFromUri(new Uri(url));

            var playbackItem = new MediaPlaybackItem(source);

            mediaPlayerElement.Source = playbackItem;


            
        }
        async private void youtubePlay(String link) {
        
            string[] arr = link.Split("=");
            var youtubeid = arr[1];
            Debug.WriteLine(youtubeid);
            try
            {
                YouTubeUri uri = await YouTube.GetVideoUriAsync(youtubeid, YouTubeQuality.Quality1080P);
                Debug.WriteLine(uri);
                mediaPlayer.SetUriSource(uri.Uri);
            }
            catch (Exception e) {
                e.ToString();
            }
        }

        private string convertLink(string link)
        {
            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(link, false);
            VideoInfo video = videoInfos
                .First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 720
                 );
            return video.DownloadUrl;
        }
        private async Task<string> InputTextDialogAsync(string title)
        {
            TextBox inputTextBox = new TextBox();
            inputTextBox.AcceptsReturn = false;
            inputTextBox.Height = 32;
            ContentDialog dialog = new ContentDialog();
            dialog.Content = inputTextBox;
            dialog.Title = title;
            dialog.IsSecondaryButtonEnabled = true;
            dialog.PrimaryButtonText = "Ok";
            dialog.SecondaryButtonText = "Cancel";
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                return inputTextBox.Text;
            else
                return "";
        }


        private async void PickMultiFile(object sender, RoutedEventArgs e)
        {
           
            await getMultiFile(videoAudioTypeFilter);
        }

        private async void PickFolder(object sender, RoutedEventArgs e)
        {
            await getFolder();
        }
        async private System.Threading.Tasks.Task getMultiFile(String[] arr)
        {
            
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            foreach(string type in arr)
            {
                openPicker.FileTypeFilter.Add(type);
            }

           
            var pickedfiles = await openPicker.PickMultipleFilesAsync();

            if (pickedfiles.Count > 0)
            {
                foreach (Windows.Storage.StorageFile file in pickedfiles)
                {
                    if (isVideoType(file) || isAudioType(file))
                    {
                       
                       
                        AddItemsToListView(file);
                    }
                    else
                    {
                        Debug.WriteLine(file.Name + " cant be added. Type: " + file.ContentType);
                    }

                }
                MediaSource source = MediaSource.CreateFromStorageFile(pickedfiles[0]);

                playbackItem = new MediaPlaybackItem(source);
                mediaPlayerElement.Source = playbackItem;
                mediaPlayerElement.AutoPlay = true;

                //Add subs automatically if matching subs present in the same directory
                getSubtitle(pickedfiles[0]);



            }
        }

        private async void getSubtitle(StorageFile file)
        {
            StorageFolder parentFolder = await file.GetParentAsync();
            Windows.Storage.AccessCache.StorageApplicationPermissions.
       FutureAccessList.AddOrReplace("PickedFolder", parentFolder);
            IReadOnlyList<StorageFile> sortedItems = await parentFolder.GetFilesAsync();
            foreach (Windows.Storage.StorageFile subtitlefile in sortedItems)
            {
                Debug.WriteLine(subtitlefile.DisplayName);
                Debug.WriteLine(file.DisplayName + ".srt");
                Debug.WriteLine(subtitlefile.Name.Equals(file.DisplayName + ".srt"));
                if (subtitlefile.Name.Contains( file.DisplayName) && (subtitlefile.Name.EndsWith(".srt") || subtitlefile.Name.EndsWith(".vtt"))) {
                    Debug.WriteLine("Got a srt match. Trying to add sub to playback");
                    //IRandomAccessStream strSource = await subtitlefile.OpenReadAsync();
                    AddSubtitle(subtitlefile);
                }
             }
        }

        private async void AddSubtitle(StorageFile file)
        {
            IRandomAccessStream strSource = await file.OpenReadAsync();
            var ttsUri = new Uri(file.Path);
            var ttsPicked = TimedTextSource.CreateFromStream(strSource);
            ttsMap[ttsPicked] = ttsUri;
            ttsPicked.Resolved += TtsEn_Resolved;
            playbackItem.Source.ExternalTimedTextSources.Add(ttsPicked);
            // Present the first track
            playbackItem.TimedMetadataTracksChanged += (item, args) =>
            {
                Debug.WriteLine($"TimedMetadataTracksChanged, Number of tracks: {item.TimedMetadataTracks.Count}");
                uint changedTrackIndex = args.Index;
                TimedMetadataTrack changedTrack = playbackItem.TimedMetadataTracks[(int)changedTrackIndex];
              // keeping 0 as index in below line will make the first subtitle that was found to be the activated on playback 
                playbackItem.TimedMetadataTracks.SetPresentationMode(0, TimedMetadataTrackPresentationMode.PlatformPresented);
            };

        }

        private Boolean isVideoType(StorageFile file) {

            
            foreach (String type in videoType) {
                if (file.ContentType.Equals(type)) return true;
                
            }
            
            return false;
        }
        private Boolean isAudioType(StorageFile file)
        {


            foreach (String type in audioType)
            {
                if (file.ContentType.Equals(type)) return true;
                
            }


            return false;
        }
        private Boolean isSubtitleType(StorageFile file)
        {
            
            foreach (String type in videoType)
            {
                if (file.ContentType.Equals(type)) return true;

            }

            return false;
        }
        async private System.Threading.Tasks.Task getFolder()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");


            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageFile tempFile = null;
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                IReadOnlyList<StorageFile> sortedItems = await folder.GetFilesAsync();
                foreach (Windows.Storage.StorageFile file in sortedItems)
                {

                    if (isVideoType(file) || isAudioType(file))
                    {
                        if (tempFile == null) tempFile = file;
                        AddItemsToListView(file);
                    }
                    else
                    {
                        Debug.WriteLine(file.Name + "cant be added. Type: " + file.ContentType);
                    }
                   
                }

                
                mediaPlayer.Source = MediaSource.CreateFromStorageFile(tempFile);
                
            }

        }

        
        private void playlist_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

            //ListViewItem item = ((FrameworkElement)sender).DataContext as ListViewItem;

            //Debug.WriteLine("Double click"+e.GetPosition(files));
            //Debug.WriteLine(item.XamlRoot);
            if (files.SelectedItems.Count == 1)

            {//display the text of selected item

                Debug.WriteLine(files.SelectedItems[0] + "double");

                if (fileData.ContainsKey(files.SelectedItems[0].ToString()))
                {
                    StorageFile file = fileData[files.SelectedItems[0].ToString()];
                    mediaPlayer.Source = MediaSource.CreateFromStorageFile(file);
                }

            }
        }

        async private void AddItemsToListView(StorageFile file ) {
            listContent listItem = new listContent();
            listItem.name = file.Name;
            VideoProperties properties = await file.Properties.GetVideoPropertiesAsync();
            if (properties.Duration.ToString().Split(".") != null)
            {
                string[] trimTimeSpan = properties.Duration.ToString().Split(".");
                listItem.duration = trimTimeSpan[0];
                Debug.WriteLine("Trimmed : "+trimTimeSpan[0]);
            }
            else {
                listItem.duration = properties.Duration.ToString();
                Debug.WriteLine("Not: " + listItem.duration);
            }
            playlist.Items.Add(listItem);
           // files.Items.Add(file.Name);
            if (!fileData.ContainsKey(file.Name))
                fileData.Add(file.Name, file);
        }

        private async void playlist_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {

                    foreach (var item in items)
                    {

                        if (item is IStorageFile)
                        {
                            StorageFile file = item as StorageFile;
                            if (isVideoType(file) || isAudioType(file))
                            {

                                AddItemsToListView(file);

                            }
                            else
                            {
                                Debug.WriteLine("cant add file of format " + file.ContentType);
                            }

                        }

                        else if (item is IStorageFolder)
                        {
                            var folder = await ((StorageFolder)item).GetFilesAsync();
                            foreach (var file in folder)
                            {
                                if (file is StorageFile && isVideoType(file) || isAudioType(file))
                                {
                                    Debug.WriteLine("From folder " + file.ContentType);
                                    AddItemsToListView(file); ;
                                }

                                else
                                {
                                    Debug.WriteLine("cant add file of format " + file.ContentType);
                                }
                            }

                        }
                        else
                        {
                            Debug.WriteLine("cant add  " + item.ToString());
                        }

                    }

                }
            }


        }

        private void playlist_container_DragOver(object sender, DragEventArgs e)
        {

            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "drop here";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;

        }

        private void FullScreenToggle(object sender, DoubleTappedRoutedEventArgs e)
        {

            mediaPlayerElement.IsFullWindow = !mediaPlayerElement.IsFullWindow;

        }
    }

    public class listContent
    {
        public string name { get; set; }
        public String duration { get; set; }
    }
}
