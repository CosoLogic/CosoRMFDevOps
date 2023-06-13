namespace RMF.DevOps.Utilities
{
    using System.IO;
    using System.Threading.Tasks;

    public static class FileDownloader
    {
        private static string BaseUrl = "https://github.com/CosoLogic/RMFSourceDocs/raw/main/";
        private static string LocalDirectory = "SourceDocs";

        public static async Task<string> DownloadFileFromGitHub(string fileName)
        {
            // Create the local directory if it doesn't exist
            Directory.CreateDirectory(LocalDirectory);

            string fileUrl = BaseUrl + fileName;
            string localFilePath = Path.Combine(LocalDirectory, fileName);

            using var httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(fileUrl);
            response.EnsureSuccessStatusCode();

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            {
                using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);

                await contentStream.CopyToAsync(fileStream);
            }

            return localFilePath;
        }
    }
}
