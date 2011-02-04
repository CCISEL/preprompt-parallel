using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

// ReSharper disable PossibleNullReferenceException

namespace FlickrSearch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        //
        // Flickr photo search API: http://www.flickr.com/services/api/flickr.photos.search.html
        //

        private const int DEFAULT_PHOTOS_PER_PAGE = 100;
        private const string ApiKey = "be9746a7685c930eab1f021ce3337572";
        private static readonly string _query = @"http://api.flickr.com/services/rest/?method=flickr.photos.search" +
                                                @"&api_key=" + ApiKey + "&per_page=" + DEFAULT_PHOTOS_PER_PAGE +
                                                @"&page={0}&text={1}&sort=interestingness-desc";

        //
        // All fields are confined to the UI thread. 
        //

        private int _lastRequestedPage;
        private int _totalPages;
        private string _currentSearchTerm;

        //
        // This object is used to ensure that when a request for a photo page is triggered,  
        // no superfluous requests for the same page will be issued. The object is set to 
        // null when the photo URLs have been retrieved and the photo area expanded. The  
        // user can then scroll to the new bottom in order to request more photos, although  
        // the previously requested photos may still be downloading.
        //

        private object _loadingPhotoUrls;

        //
        // All cancellation related operations are performed on the UI thread. A cancelation 
        // request stops the global search so that no more photo pages can be requested. 
        //

        private CancellationTokenSource _searchCts;

        public MainWindow()
        {
            InitializeComponent();
            _textBox.Focus();
        }

        private void search_button_click(object sender, RoutedEventArgs e)
        {
            clear_interface();

            //
            // Stop the previous search.
            //

            if (_searchCts != null && _searchCts.IsCancellationRequested == false)  
            {
                _searchCts.Cancel();
                _searchCts.Dispose();
            }

            //
            // Start a new search.
            //

            if ((_currentSearchTerm = _textBox.Text).IsNullOrEmpty())
            {
                return;
            }

            _searchCts = new CancellationTokenSource();
            start_search();
        }

        private void scroll_changed(object sender, ScrollChangedEventArgs e)
        {
            //
            // We request another page of photos from Flickr if:
            //  1) there are more pages to be requested;
            //  2) the user scrolled to the bottom of the scrollable area;
            //  3) the search has not been cancelled; and
            //  4) there is no ongoing web request for photo urls.
            //

            if (_totalPages > _lastRequestedPage
                && _scrollViewer.VerticalOffset == _scrollViewer.ScrollableHeight
                && _searchCts != null && _searchCts.IsCancellationRequested == false
                && _loadingPhotoUrls == null)
            {
                start_search();
            }
        }

        private void cancel_button_click(object sender, RoutedEventArgs e)
        {
            if (_searchCts != null)
            {
                cancel();
            }
        }

        private void close_popup_button_click(object sender, RoutedEventArgs e)
        {
            _popup.IsOpen = false;
        }

        private void cancel()
        {
            _searchCts.Cancel();
            _statusText.Text = "Cancelled";
        }

        private async void start_search()
        {
            //
            // The search can be cancelled because:
            //  1) the user explicitly issued a cancellation request;
            //  2) the user entered another search term; or
            //  3) the web request timed out.
            //
            // When the search is cancelled, we have to be careful not to interfere with 
            // a new search that may have started in the meanwhile. This means we cannot 
            // update any fields without making sure no new search has started. For that
            // purpose we keep a generation object to detect when a new search is started.
            //

            var currentSearch = _loadingPhotoUrls = new object();

            //
            // Load the photo URLs.
            //

            await load_photo_urls_async(_searchCts.Token);
            
            //
            // If no new search has started, enable requests for more photos.
            //

            if (currentSearch == _loadingPhotoUrls)
            {
                _loadingPhotoUrls = null;
            }
        }
        
        private async Task load_photo_urls_async(CancellationToken token)
        {
            var client = new WebClient();
            var uri = new Uri(_query.FormatWith(_lastRequestedPage + 1, _currentSearchTerm));

            var photoUrlsTask = client.DownloadStringTaskAsync(uri, token);
            if (photoUrlsTask != await TaskEx.WhenAny(photoUrlsTask, TaskEx.Delay(20000)))
            {
                cancel();
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            var searchResult = await TaskEx.Run(() => PhotoSearchResult.TryParse(photoUrlsTask.Result));
            if (searchResult.Photos.Length == 0)
            {
                _statusText.Text = "No results found";
                return;
            }

            display_photos(update_ui(searchResult).ToList(), token);
        }

        private IEnumerable<Tuple<Photo, Image>> update_ui(PhotoSearchResult searchResult)
        {
            int photosUntilNow = searchResult.Photos.Length + _lastRequestedPage * DEFAULT_PHOTOS_PER_PAGE;
            _lastRequestedPage = searchResult.Page;
            _totalPages = searchResult.TotalPages;
            _statusText.Text = "{0} photos of a total {1}".FormatWith(photosUntilNow, _totalPages);

            foreach (var photo in searchResult.Photos)
            {
                var image = new Image
                {
                    Width = 110,
                    Height = 150,
                    Margin = new Thickness(5),
                    ToolTip = new ToolTip {Content = photo.Title}
                };
                _resultsPanel.Children.Add(image);
                yield return Tuple.Create(photo, image);
            }
        }

        private async void display_photos(IEnumerable<Tuple<Photo, Image>> photos, CancellationToken token)
        {
            await TaskScheduler.Default.SwitchTo();

            var imageTasks = photos.Select(download_photo_async).ToList();
            while (imageTasks.Count() > 0)
            {
                var imageTask = await TaskEx.WhenAny(imageTasks);
                imageTasks.Remove(imageTask);
                var result = imageTask.Result;

                await result.Item2.Dispatcher.SwitchTo();
                if (token.IsCancellationRequested == false)
                {
                    attach_bitmap(result);
                }
            }
        }

        private static async Task<Tuple<Photo, Image, MemoryStream>> download_photo_async(Tuple<Photo, Image> photoUi)
        {
            var photo = photoUi.Item1;
            var request = WebRequest.Create(photo.Url);
            using (var response = await request.GetResponseAsync())
            using (var responseStream = response.GetResponseStream())
            {
                var result = new MemoryStream();
                await responseStream.CopyToAsync(result);

                result.Seek(0, SeekOrigin.Begin);
                return Tuple.Create(photo, photoUi.Item2, result);
            }
        }

        private void attach_bitmap(Tuple<Photo, Image, MemoryStream> photoUi)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = photoUi.Item3;
            bitmapImage.EndInit();
            photoUi.Item2.Source = bitmapImage;
            photoUi.Item2.MouseDown += (sender1, e1) =>
            {
                var fullImage = new Image
                {
                    Source = bitmapImage,
                    Width = bitmapImage.Width,
                    Height = bitmapImage.Height,
                    Margin = new Thickness(20)
                };
                fullImage.MouseDown += (sender2, e2) =>
                {
                    _popup.IsOpen = false;
                    System.Diagnostics.Process.Start(photoUi.Item1.Url);
                };

                _frontImage.Children.Clear();
                _frontImage.Children.Add(fullImage);
                _frontImageTitle.Text = photoUi.Item1.Title;

                _popup.IsOpen = true;
            };
        }
        
        private void clear_interface()
        {
            _resultsPanel.Children.Clear();
            _scrollViewer.ScrollToTop();
            _statusText.Text = "";
        }
    }
}