using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using CMS.CustomTables;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EventLog;
using CMS.Helpers;
using CMS.MacroEngine;
using CMS.MediaLibrary;
using CMS.Membership;
using CMS.SiteProvider;

namespace Custom.Helpers
{
    /// <summary>
    /// Summary description for MediaFileResizeHelper
    /// </summary>
    public class MediaFileResizeHelper
    {
        #region Properties

        /// <summary>
        /// The default cache minutes
        /// </summary>
        public static Int32 DefaultCacheMinutes = 60;

        #endregion

        #region Images

        public static String GetResponsiveImageURL(String imageURL, Int32 width = 0, Int32 height = 0)
        {
            if (String.IsNullOrWhiteSpace(imageURL))
                return imageURL;

            try
            {
                var imagePath = imageURL.Replace("~", String.Empty).Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                MediaLibraryInfo mediaLibrary;
                
                if (!imagePath[2].Equals("assets") && !imageURL.Contains("getmedia"))
                    mediaLibrary = GetCachedMediaLibriaryInfo(imagePath[2]);
                else
                {
                    mediaLibrary = null;
                    EventLogProvider.LogInformation("GetResponsiveImageURL", EventType.INFORMATION, "Could not get media library for file: " + imageURL);
                }

                if (mediaLibrary != null)
                {
                    // get the file now by the media library and the path
                    var imageName = imagePath.Last().Split(new[] { "?" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    var file = GetCachedMediaFileInfo(mediaLibrary.LibraryID, GetImagePath(imagePath, imageName));

                    if (file != null && width == 0)
                        return String.Format("/getmedia/{0}/{1}", file.FileGUID, file.FileName);

                    if (file != null && width > 0 && height == 0)
                        return String.Format("/getmedia/{0}/{1}?width={2}&ext={3}", file.FileGUID, file.FileName, width, file.FileExtension);

                    if (file != null && width > 0 && height > 0)
                        return String.Format("/getmedia/{0}/{1}?width={2}&height={3}&ext={4}", file.FileGUID, file.FileName, width, height, file.FileExtension);
                }
            }
            catch (Exception ex)
            {
                EventLogProvider.LogException("GetResponsiveImageURL", EventType.ERROR, ex, SiteContext.CurrentSiteID, "Unabled to resize image");
            }

            return imageURL;
        }

        private static String GetImagePath(String[] imagePath, String imageName)
        {
            var result = new List<String>();

            for (var i = 3; i < imagePath.Length - 1; i++)
            {
                result.Add(imagePath[i]);
            }

            return URLHelper.URLDecode(String.Join("/", result) + "/" + imageName);
        }

        #endregion

        #region Media Libraries

        public static MediaLibraryInfo GetCachedMediaLibriaryInfo(String name)
        {
            var mediaLibraryInfo = CacheHelper.Cache(cs =>
            {
                try
                {
                    var lib = MediaLibraryInfoProvider.GetMediaLibraryInfo(name, SiteContext.CurrentSiteName);

                    if (cs.Cached)
                        cs.CacheDependency = CacheHelper.GetCacheDependency("media.library|byguid|" + lib.LibraryGUID);

                    return lib;
                }
                catch (Exception ex)
                {
                    EventLogProvider.LogException("GetCachedMediaLibriaryInfo", EventType.INFORMATION, ex, SiteContext.CurrentSiteID, "Unabled to get media library: " + name);
                }

                return null;
            }, new CacheSettings(DefaultCacheMinutes, "custom|getcachedmedialibrary|" + name));

            return mediaLibraryInfo;
        }

        public static MediaFileInfo GetCachedMediaFileInfo(Int32 libraryID, String path)
        {
            var mediaInfo = CacheHelper.Cache(cs =>
            {
                try
                {
                    var file = MediaFileInfoProvider.GetMediaFiles()
                                                    .WhereEquals("FileLibraryID", libraryID)
                                                    .WhereEquals("FilePath", path)
                                                    .Columns("FileGUID", "FileName", "FileExtension")
                                                    .FirstObject;

                    if (cs.Cached)
                        cs.CacheDependency = CacheHelper.GetCacheDependency("mediafile|" + file.FileGUID);

                    return file;
                }
                catch (Exception ex)
                {
                    EventLogProvider.LogException("GetCachedMediaFileInfo", EventType.INFORMATION, ex, SiteContext.CurrentSiteID, "Unabled to get media file: " + path);
                }

                return null;
            }, new CacheSettings(DefaultCacheMinutes, "custom|getcachedmediafile|" + path));

            return mediaInfo;
        }

        #endregion
    }
}