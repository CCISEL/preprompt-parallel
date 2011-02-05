using System.Linq;
using System.Xml.Linq;

namespace FlickrSearch
{
    public class PhotoSearchResult
    {
        public int Page { get; private set; }
        public int TotalPages { get; private set; }
        public Photo[] Photos { get; private set; }

        public static PhotoSearchResult TryParse(string content)
        {
            var document = XDocument.Parse(content);
            var photosElement = document.Descendants("photos").FirstOrDefault();

            return new PhotoSearchResult
            {
                Page = int.Parse(photosElement.Attribute("page").Value),
                TotalPages = int.Parse(photosElement.Attribute("total").Value),
                Photos = get_photos(photosElement)
            };
        }

        private static Photo[] get_photos(XContainer document)
        {
            //
            // Flickr uses the following URL format: 
            //   http://farm{farm-id}.static.flickr.com/{server-id}/{id}_{secret}.jpg
            //

            return document.Descendants("photo").AsParallel().Select(photo => new Photo
            {
                Url = "http://farm{0}.static.flickr.com/{1}/{2}_{3}.jpg".FormatWith(
                    photo.Attribute("farm").Value, photo.Attribute("server").Value,
                    photo.Attribute("id").Value, photo.Attribute("secret").Value),
                Title = photo.Attribute("title").Value
            }).ToArray();
        }
    }
}