using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using System.Xml.Linq;

namespace FlickrSearch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //
        // Flickr photo search API: http://www.flickr.com/services/api/flickr.photos.search.html
        //

        private int _currentPage = 1;

        private class Photo
        {
            public string Title { get; set; }
            public string Url { get; set; }
        }

        CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
            textBox.Focus();
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            load_photos(textBox.Text);
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();
                statusText.Text = "Cancelled";
            }
        }

        private void load_photos(string text)
        {
            const string apiKey = "";
            const string query = "http://api.flickr.com/services/rest/?method=flickr.photos.search" +
                                 "&api_key=" + apiKey + "&sort=interestingness-desc&page={0}&text={1}";

            var webClient = new WebClient();
            string content = webClient.DownloadString(query.FormatWith(_currentPage, text));

            var document = XDocument.Parse(content);
            var photos = get_photos(document);
            //int totalPhotos = int.Parse(root.Attribute("total").Value);
            display_photos(photos);
        }

        private static Photo[] get_photos(XDocument document)
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

        void display_photos(IEnumerable<Photo> photos)
        {
            foreach (var photo in photos)
            {
                var bitmap = new BitmapImage(new Uri(photo.Url));
                var image = new Image();
                image.Source = bitmap;
                image.Width = 110;
                image.Height = 150;
                image.Margin = new Thickness(5);
                var tt = new ToolTip();
                tt.Content = photo.Title;
                image.ToolTip = tt;
                var url = photo.Url;
                image.MouseDown += (sender, e) => System.Diagnostics.Process.Start(url);
                resultsPanel.Children.Add(image);
            }
        }
    }
}
