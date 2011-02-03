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
using System.Xml.Linq;

// ReSharper disable PossibleNullReferenceException

namespace FlickrSearch
{
    /// <summary>
    /// Interaction logic for MainWindow_Async.xaml
    /// </summary>
    public partial class MainWindow_Async
    {
        //
        // Flickr photo search API: http://www.flickr.com/services/api/flickr.photos.search.html
        //

        private const int DEFAULT_PHOTOS_PER_PAGE = 100;
        private const string ApiKey = "be9746a7685c930eab1f021ce3337572";
        private static readonly string _query = @"http://api.flickr.com/services/rest/?method=flickr.photos.search" +
                                                @"&api_key=" + ApiKey + "&per_page=" + DEFAULT_PHOTOS_PER_PAGE +
                                                @"&page={0}&text={1}&sort=interestingness-desc";

        private class Photo
        {
            public string Title { get; set; }
            public string Url { get; set; }
            public Image Image { get; set; }
        }

        private class Search
        {
            private readonly int _currentPage;
            private readonly int _totalPages;

            public Search()
            { }

            public Search(int currentPage, int totalPages)
            {
                _currentPage = currentPage;
                _totalPages = totalPages;
            }

            public int GetNextPage()
            {
                return _currentPage + 1;
            }

            public bool HasMore()
            {
                return _currentPage < _totalPages;
            }
        }

        private Search _lastSearch = new Search();
        private bool _searchInProgress;
        private CancellationTokenSource _cts;

        public MainWindow_Async()
        {
            InitializeComponent();
            _textBox.Focus();
        }

        private void search_button_click(object sender, RoutedEventArgs e)
        {
            if (_searchInProgress)
            {
                _cts.Cancel();
                _searchInProgress = false;
            }

            clear_interface();

            _lastSearch = new Search();
            _cts = new CancellationTokenSource();
            start_search();
        }

        private void scroll_changed(object sender, ScrollChangedEventArgs e)
        {
            if (_searchInProgress == false
                && _lastSearch.HasMore()
                && _scrollViewer.VerticalOffset == _scrollViewer.ScrollableHeight)
            {
                start_search();
            }
        }

        private async void cancel_button_click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _searchInProgress = false;
                _statusText.Text = "Cancelled";
            }
        }

        private void close_popup_button_click(object sender, RoutedEventArgs e)
        {
            _popup.IsOpen = false;
        }

        private async void start_search()
        {
            _searchInProgress = true;
            await load_photos_async();
            _searchInProgress = false;
        }

        private async Task load_photos_async()
        {
            string text = _textBox.Text;
            if (string.IsNullOrEmpty(text) || _cts != null && _cts.IsCancellationRequested)
            {
                return;
            }
            
            var token = _cts.Token;

            var uri = new Uri(_query.FormatWith(_lastSearch.GetNextPage(), text));
            var client = new WebClient();

            var contentTask = client.DownloadStringTaskAsync(uri, token);
            if (contentTask == await TaskEx.ConfigureAwait(TaskEx.WhenAny(contentTask, TaskEx.Delay(20000)),
                                                           false))
            {
                await process_photo_urls(contentTask.Result, token);
            }
            else
            {
                await _statusText.Dispatcher.SwitchTo();
                _cts.Cancel();
                _statusText.Text = "Timeout";
            }
        }

        private async Task process_photo_urls(string content, CancellationToken token)
        {
            var document = XDocument.Parse(content);
            var photosElement = document.Descendants("photos").FirstOrDefault();

            Photo[] photos;
            if (photosElement == null || (photos = get_photos(photosElement)).Length == 0)
            {
                await _statusText.Dispatcher.SwitchTo();
                _resultsPanel.Children.Add(new TextBox { Text = document.ToString() });
                return;
            }

            int currentPage = int.Parse(photosElement.Attribute("page").Value);
            int totalPages = int.Parse(photosElement.Attribute("total").Value);
            int photosUntilNow = photos.Length + ((currentPage - 1) * DEFAULT_PHOTOS_PER_PAGE);

            await _statusText.Dispatcher.SwitchTo();

            if (token.IsCancellationRequested)
            {
                return;
            }

            _lastSearch = new Search(currentPage, totalPages);
            _statusText.Text = "{0} photos of a total {1}".FormatWith(photosUntilNow, totalPages);

            reserve_ui_space(photos);
            display_photos(photos, token);
        }

        private void reserve_ui_space(IEnumerable<Photo> photos)
        {
            foreach (var photo in photos)
            {
                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                photo.Image = new Image { Width = 110, Height = 150, Margin = new Thickness(5) };
                var tt = new ToolTip { Content = photo.Title };
                photo.Image.ToolTip = tt;
                _resultsPanel.Children.Add(photo.Image);
            }
        }

        private async void display_photos(IEnumerable<Photo> photos, CancellationToken token)
        {
            await TaskScheduler.Default.SwitchTo();

            var imageTasks = photos.Select(download_photo_async).ToList();
            while (imageTasks.Count() > 0)
            {
                var imageTask = await TaskEx.WhenAny(imageTasks);
                imageTasks.Remove(imageTask);
                var result = imageTask.Result;

                await result.Item1.Image.Dispatcher.SwitchTo();
                if (token.IsCancellationRequested == false)
                {
                    attach_bitmap(result.Item1, result.Item2);
                }
            }
        }

        private static Task<Tuple<Photo, MemoryStream>> download_photo_async(Photo photo)
        {
            return TaskEx.Run(() =>
            {
                var request = WebRequest.Create(photo.Url);
                using (var response = request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    var result = new MemoryStream();
                    responseStream.CopyTo(result);

                    result.Seek(0, SeekOrigin.Begin);
                    return Tuple.Create(photo, result);
                }
            });
        }

        private void attach_bitmap(Photo photo, MemoryStream stream)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            photo.Image.Source = bitmapImage;
            photo.Image.MouseDown += (sender1, e1) =>
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
                    System.Diagnostics.Process.Start(photo.Url);
                };

                _frontImage.Children.Clear();
                _frontImage.Children.Add(fullImage);
                _frontImageTitle.Text = photo.Title;

                _popup.IsOpen = true;
            };
        }

        private static Photo[] get_photos(XContainer document)
        {
            //
            // Flickr uses the following URL format: 
            //   http://farm{farm-id}.static.flickr.com/{server-id}/{id}_{secret}.jpg
            //

            return document.Descendants("photo").Select(photo => new Photo
            {
                Url = "http://farm{0}.static.flickr.com/{1}/{2}_{3}.jpg".FormatWith(
                        photo.Attribute("farm").Value, photo.Attribute("server").Value,
                        photo.Attribute("id").Value, photo.Attribute("secret").Value),
                Title = photo.Attribute("title").Value
            }).ToArray();
        }

        private void clear_interface()
        {
            _resultsPanel.Children.Clear();
            _scrollViewer.ScrollToTop();
            _statusText.Text = "";
        }
    }
}