using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

        private const int DEFAULT_PHOTOS_PER_PAGE = 10;
        private const string ApiKey = "be9746a7685c930eab1f021ce3337572";
        private static readonly string _query = "http://api.flickr.com/services/rest/?method=flickr.photos.search" +
                                                "&api_key=" + ApiKey + "&per_page=" + DEFAULT_PHOTOS_PER_PAGE +
                                                "&sort=interestingness-desc&page={0}&text={1}";

        private struct Photo
        {
            public string Title { get; set; }
            public string Url { get; set; }
        }

        private struct Search
        {
            private readonly int _currentPage;
            private readonly int _totalPages;

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

            public bool Equals(Search other)
            {
                return other._currentPage == _currentPage && other._totalPages == _totalPages;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if (obj.GetType() != typeof(Search))
                {
                    return false;
                }
                return Equals((Search)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_currentPage * 397) ^ _totalPages;
                }
            }

            public static bool operator ==(Search search1, Search search2)
            {
                return search1.Equals(search2);
            }

            public static bool operator !=(Search search1, Search search2)
            {
                return !(search1 == search2);
            }
        }

        private Search _lastSearch;
        private bool _scrollPending;
        private CancellationTokenSource _cts;
        private Task _currentSearch;

        public MainWindow_Async()
        {
            InitializeComponent();
            _textBox.Focus();
        }

        private async void search_button_click(object sender, RoutedEventArgs e)
        {
            if (_currentSearch != null && _currentSearch.IsCompleted == false && _cts.IsCancellationRequested == false)
            {
                _cts.Cancel();
            }

            _resultsPanel.Children.Clear();
            _scrollViewer.ScrollToTop();
            _statusText.Text = "";

            _lastSearch = new Search();
            _cts = new CancellationTokenSource();
            _currentSearch = load_photos_async();
        }

        private async void scroll_changed(object sender, ScrollChangedEventArgs e)
        {
            if (_scrollPending == false
                && _lastSearch.HasMore()
                && _scrollViewer.VerticalOffset == _scrollViewer.ScrollableHeight)
            {
                _scrollPending = true;
                await load_photos_async();
                _scrollPending = false;
            }
        }

        private async void cancel_button_click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                await TaskScheduler.Default.SwitchTo();
                _cts.Cancel();
                await _statusText.Dispatcher.SwitchTo();
                _statusText.Text = "Cancelled";
            }
        }

        private void close_popup_button_click(object sender, RoutedEventArgs e)
        {
            _popup.IsOpen = false;
        }

        private async Task load_photos_async()
        {
            string text = _textBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            await TaskEx.Delay(20000);
            if (_cts != null)
            {
                _cts.Cancel();
                _statusText.Text = "Timeout";
            }

            var client = new WebClient();
            string content = await client.DownloadStringTaskAsync(new Uri(_query.FormatWith(_lastSearch.GetNextPage(), text)),
                                                                  _cts.Token);
            var document = XDocument.Parse(content);
            var photosElement = document.Descendants("photos").FirstOrDefault();

            Photo[] photos;
            if (photosElement == null || (photos = get_photos(photosElement)).Length == 0)
            {
                _resultsPanel.Children.Add(new TextBox { Text = document.ToString() });
                return;
            }

            int currentPage = int.Parse(photosElement.Attribute("page").Value);
            int totalPages = int.Parse(photosElement.Attribute("total").Value);
            int photosUntilNow = photos.Length + ((currentPage - 1) * DEFAULT_PHOTOS_PER_PAGE);

            _lastSearch = new Search(currentPage, totalPages);

            _statusText.Text = "{0} photos of a total {1}".FormatWith(photosUntilNow, totalPages);
            display_photos(photos);
        }

        void display_photos(IEnumerable<Photo> photos)
        {
            foreach (var photo in photos)
            {
                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                var bitmapImage = download_photo(photo.Url);
                var image = new Image { Source = bitmapImage, Width = 110, Height = 150, Margin = new Thickness(5) };

                string title = photo.Title;
                var tt = new ToolTip { Content = photo.Title };
                image.ToolTip = tt;

                var url = photo.Url;

                image.MouseDown += (sender1, e1) =>
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
                        System.Diagnostics.Process.Start(url);
                    };

                    _frontImage.Children.Clear();
                    _frontImage.Children.Add(fullImage);
                    _frontImageTitle.Text = title;

                    _popup.IsOpen = true;
                };

                _resultsPanel.Children.Add(image);
            }
        }

        private static BitmapImage download_photo(string url)
        {
            var request = WebRequest.Create(url);
            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            {
                var result = new MemoryStream();
                responseStream.CopyTo(result);

                result.Seek(0, SeekOrigin.Begin);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = result;
                bitmapImage.EndInit();
                return bitmapImage;
            }
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
    }
}